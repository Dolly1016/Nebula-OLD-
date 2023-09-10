using AmongUs.GameOptions;
using Assets.CoreScripts;
using Il2CppSystem.Net.NetworkInformation;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using static Rewired.UI.ControlMapper.ControlMapper;
using static UnityEngine.GraphicsBuffer;

namespace Nebula.Game;

public enum NebulaGameStates
{
    NotStarted,
    Initialized,
    WaitGameResult,
    Finished
}

public class NebulaEndState {
    public ulong WinnersMask;
    public byte ConditionId;
    public CustomEndCondition? EndCondition => CustomEndCondition.GetEndCondition(ConditionId);

    public NebulaEndState(byte conditionId, ulong winnersMask)
    {
        WinnersMask = winnersMask;
        ConditionId = conditionId;
    }
}

public class RuntimeGameAsset
{
    AsyncOperationHandle<GameObject>? handle = null;
    public MapBehaviour MinimapPrefab;
    public float MapScale;
    public GameObject MinimapObjPrefab => MinimapPrefab.transform.GetChild(1).gameObject;
    public void SetHandle(AsyncOperationHandle<GameObject> handle) => this.handle = handle;
    public void Abandon()
    {
        if (handle?.IsValid() ?? false) handle.Release();
    }
}

[NebulaRPCHolder]
public class NebulaGameManager
{
    static private NebulaGameManager? instance = null;
    static public NebulaGameManager? Instance { get => instance; }

    private Dictionary<byte, PlayerModInfo> allModPlayers;

    private HashSet<INebulaScriptComponent> allScripts = new HashSet<INebulaScriptComponent>();

    //ゲーム開始時からの経過時間
    public float CurrentTime { get; private set; } = 0f;

    //各種進行状況
    public NebulaGameStates GameState { get; private set; } = NebulaGameStates.NotStarted;
    public NebulaEndState? EndState { get; set; } = null;

    //ゲーム内アセット
    public RuntimeGameAsset RuntimeAsset { get; private init; }

    //各種モジュール
    public HudGrid HudGrid { get; private set; }
    public GameStatistics GameStatistics { get; private set; } = new();
    public CriteriaManager CriteriaManager { get; private set; } = new();
    public Synchronizer Syncronizer { get; private set; } = new();
    public LobbySlideManager LobbySlideManager { get; private set; } = new();
    //天界視点フラグ
    public bool CanSeeAllInfo { get; set; }

    public NebulaGameManager()
    {
        allModPlayers = new Dictionary<byte, PlayerModInfo>();
        instance = this;
        HudGrid = HudManager.Instance.gameObject.AddComponent<HudGrid>();
        RuntimeAsset = new();
    }


    public void Abandon()
    {
        RuntimeAsset.Abandon();
        LobbySlideManager.Abandon();

        foreach (var script in allScripts) script.Release();
        allScripts.Clear();

        instance = null;
    }

    public void RegisterComponent(INebulaScriptComponent component)
    {
        allScripts.Add(component);
    }

    public bool ReleaseComponent(INebulaScriptComponent component) => allScripts.Remove(component);

    public PlayerModInfo RegisterPlayer(PlayerControl player)
    {
        if(allModPlayers.ContainsKey(player.PlayerId))return allModPlayers[player.PlayerId];

        Debug.Log("Registered: " + player.name);
        var info = new PlayerModInfo(player);
        allModPlayers.Add(player.PlayerId, info);
        return info;
    }

    public void RpcPreSpawn(byte playerId,Vector2 spawnPos)
    {
        CombinedRemoteProcess.CombinedRPC.Invoke(
            GameStatistics.RpcPoolPosition.GetInvoker(new(GameStatisticsGatherTag.Spawn, playerId, spawnPos)),
            Modules.Synchronizer.RpcSync.GetInvoker(new(SynchronizeTag.PreSpawnMinigame, PlayerControl.LocalPlayer.PlayerId))
            );
    }

    private void CheckAndEndGame(CustomEndCondition? endCondition,int winnersMask = 0)
    {
        if(endCondition == null) return;
        if (GameState != NebulaGameStates.Initialized) return;

        HashSet<byte> winners = new();
        foreach(var p in allModPlayers)
        {
            if (p.Value.AllAssigned().Any(a=>a.CheckWins(endCondition))) winners.Add(p.Key);
            else if (((1 << p.Value.PlayerId) & winnersMask) != 0) winners.Add(p.Key);
        }
        NebulaGameEnd.RpcSendGameEnd(endCondition!, winners);
    }

    public void OnTaskUpdated()
    {
        CheckAndEndGame(CriteriaManager.OnTaskUpdated());
    }

    public void OnMeetingStart()
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) CanSeeAllInfo = true;

        foreach (var script in allScripts) script.OnMeetingStart();

        AllRoleAction(r=>r.OnMeetingStart());
    }

    public void OnMeetingEnd(PlayerControl? player)
    {
        var tuple = CriteriaManager.OnExiled(player);
        if(tuple == null) return;
        CheckAndEndGame(tuple.Item1,tuple.Item2);
    }

    public void OnUpdate() {
        CurrentTime += Time.deltaTime;

        allScripts.RemoveWhere(script=> {
            if (!script.UpdateWithMyPlayer) script.Update();

            if (script.MarkedRelease)
            {
                script.OnReleased();
                return true;
            }
            return false;
        });

        if (!PlayerControl.LocalPlayer) return;
        //バニラボタンの更新
        var localModInfo = PlayerControl.LocalPlayer.GetModInfo();
        if (localModInfo != null)
        {
            //ベントボタン
            var ventTimer = PlayerControl.LocalPlayer.inVent ? localModInfo.Role?.VentDuration : localModInfo.Role?.VentCoolDown;
            string ventText = "";
            float ventPercentage = 0f;
            if (ventTimer != null && ventTimer.IsInProcess)
            {
                ventText = Mathf.CeilToInt(ventTimer.CurrentTime).ToString();
                ventPercentage = ventTimer.Percentage;
            }
            if (ventTimer != null && !ventTimer.IsInProcess && PlayerControl.LocalPlayer.inVent) Vent.currentVent?.ExitVent(PlayerControl.LocalPlayer);
            HudManager.Instance.ImpostorVentButton.SetCooldownFill(ventPercentage);
            CooldownHelpers.SetCooldownNormalizedUvs(HudManager.Instance.ImpostorVentButton.graphic);
            HudManager.Instance.ImpostorVentButton.cooldownTimerText.text = ventText;
            HudManager.Instance.ImpostorVentButton.cooldownTimerText.color = PlayerControl.LocalPlayer.inVent ? Color.green : Color.white;

            //サボタージュボタン
        }

        CheckAndEndGame(CriteriaManager.OnUpdate());
    }

    public void OnFixedUpdate() {
        foreach (var script in allScripts) if (script.UpdateWithMyPlayer) script.Update();
    }
    public void OnGameStart()
    {
        //マップの取得
        RuntimeAsset.MinimapPrefab = ShipStatus.Instance.MapPrefab;
        RuntimeAsset.MinimapPrefab.gameObject.MarkDontUnload();
        RuntimeAsset.MapScale = ShipStatus.Instance.MapScale;

        //ゲームモードによる終了条件を追加
        foreach(var c in GeneralConfigurations.CurrentGameMode.GameModeCriteria)CriteriaManager.AddCriteria(c);

        //無効試合の終了条件
        CriteriaManager.AddCriteria(NebulaEndCriteria.NoGameCriteria);

        foreach (var p in allModPlayers) p.Value.OnGameStart();
        NebulaGameManager.Instance?.AllScriptAction(s => s.OnGameStart());
        HudManager.Instance.UpdateHudContent();
    }

    public void OnGameEnd()
    {
        GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.GameEnd, null, 0) { RelatedTag = EventDetail.GameEnd });
    }

    public PlayerModInfo? GetModPlayerInfo(byte playerId)
    {
        return allModPlayers.TryGetValue(playerId, out var v) ? v : null;
    }

    public void CheckGameState()
    {
        switch (GameState)
        {
            case NebulaGameStates.NotStarted:
                if (PlayerControl.AllPlayerControls.Count == allModPlayers.Count)
                {
                    LobbySlideManager.Abandon();
                    DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
                    DestroyableSingleton<HudManager>.Instance.HideGameLoader();
                    GameState = NebulaGameStates.Initialized;
                }
                break;
        }
    }

    public IEnumerator CoWaitAndEndGame()
    {
        if(GameState != NebulaGameStates.Finished) GameState = NebulaGameStates.WaitGameResult;

        yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.clear, Color.black, 0.5f, false);
        if (AmongUsClient.Instance.AmHost) GameManager.Instance.RpcEndGame(EndState?.EndCondition == NebulaGameEnd.CrewmateWin ? GameOverReason.HumansByTask : GameOverReason.ImpostorByKill, false);

        while (GameState != NebulaGameStates.Finished) yield return null;

        SceneManager.LoadScene("EndGame");
        yield break;
    }

    public void ReceiveVanillaGameResult()
    {
        GameState = NebulaGameStates.Finished;
    }

    public void ToGameEnd()
    {
        if (Minigame.Instance)
        {
            try
            {
                Minigame.Instance.Close();
                Minigame.Instance.Close();
            }
            catch
            {
            }
        }

        HudManager.Instance.StartCoroutine(CoWaitAndEndGame().WrapToIl2Cpp());
    }

    public IEnumerable<PlayerModInfo> AllPlayerInfo() => allModPlayers.Values;

    public void AllRoleAction(Action<AssignableInstance> action)
    {
        foreach (var p in AllPlayerInfo()) p.RoleAction(action);
    }

    public IEnumerable<INebulaScriptComponent> AllScripts() => allScripts;

    public void AllScriptAction(Action<INebulaScriptComponent> action)
    {
        foreach (var s in AllScripts()) action.Invoke(s);
    }

    public void RpcInvokeSpecialWin(CustomEndCondition endCondition, int winnersMask)
    {
        if (GeneralConfigurations.CurrentGameMode.AllowSpecialEnd) RpcSpecialWin.Invoke(new(endCondition.Id, winnersMask));
    }

    static RemoteProcess<Tuple<int, int>> RpcSpecialWin = new RemoteProcess<Tuple<int, int>>(
        "SpecialWin",
        (writer, message) =>
        {
            writer.Write(message.Item1);
            writer.Write(message.Item2);
        },
        (reader) => new(reader.ReadInt32(),reader.ReadInt32()),
        (message, _) =>
        {
            if (!AmongUsClient.Instance.AmHost) return;
            NebulaGameManager.Instance?.CheckAndEndGame(CustomEndCondition.GetEndCondition((byte)message.Item1), message.Item2);
        }
        );

    public readonly static RemoteProcess RpcStartGame = new RemoteProcess(
        "StartGame",
        (_) => NebulaGameManager.Instance?.CheckGameState()
        );
}

public static class NebulaGameManagerExpansion
{
    static public PlayerModInfo? GetModInfo(this PlayerControl? player)
    {
        if (!player) return null;
        return NebulaGameManager.Instance?.GetModPlayerInfo(player.PlayerId);
    }


}

