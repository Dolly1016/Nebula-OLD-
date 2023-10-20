using AmongUs.GameOptions;
using Assets.CoreScripts;
using Il2CppSystem.Net.NetworkInformation;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using Nebula.VoiceChat;
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
    public int WinnersMask;
    public byte ConditionId;
    public ulong ExtraWinMask;
    public CustomEndCondition? EndCondition => CustomEndCondition.GetEndCondition(ConditionId);
    public IEnumerable<CustomExtraWin> ExtraWins => CustomExtraWin.AllExtraWins.Where(e => ((ulong)(1 << e.Id) & ExtraWinMask) != 0ul);

    public NebulaEndState(byte conditionId, int winnersMask,ulong extraWinMask)
    {
        WinnersMask = winnersMask;
        ConditionId = conditionId;
        ExtraWinMask = extraWinMask;
    }
}

public class RuntimeGameAsset
{
    AsyncOperationHandle<GameObject>? handle = null;
    public MapBehaviour MinimapPrefab = null!;
    public float MapScale;
    public GameObject MinimapObjPrefab => MinimapPrefab.transform.GetChild(1).gameObject;
    public void SetHandle(AsyncOperationHandle<GameObject> handle) => this.handle = handle;
    public void Abandon()
    {
        if (handle?.IsValid() ?? false) handle.Release();
    }
}

public record RoleHistory
{
    public float Time;
    public byte PlayerId;
    public bool IsModifier;
    public bool IsSet;
    public AssignableInstance Assignable;

    public RoleHistory(byte playerId, ModifierInstance modifier, bool isSet)
    {
        Time = NebulaGameManager.Instance!.CurrentTime;
        PlayerId = playerId;
        IsModifier = true;
        IsSet = isSet;
        Assignable = modifier;
    }

    public RoleHistory(byte playerId, RoleInstance role)
    {
        Time = NebulaGameManager.Instance!.CurrentTime;
        PlayerId = playerId;
        IsModifier = false;
        IsSet = true;
        Assignable = role;
    }
}

public static class RoleHistoryHelper { 
    static public IEnumerable<T> EachMoment<T>(this List<RoleHistory> history, Predicate<RoleHistory> predicate, Func<RoleInstance, List<AssignableInstance>, T> converter)
    {
        RoleInstance? role = null;
        List<AssignableInstance> modifiers = new();

        float lastTime = history[0].Time;
        foreach(var h in history.Append(null))
        {
            if (h != null && !predicate(h)) continue;

            if(h == null || lastTime + 1f < h.Time)
            {
                if(role != null) yield return converter.Invoke(role, modifiers);

                if (h == null) break;

                lastTime = h.Time;
            }

            if (!h.IsModifier && h.Assignable is RoleInstance ri) role = ri;
            else if (h.IsSet) modifiers.Add(h.Assignable);
            else modifiers.Remove(h.Assignable);
        }
    }

    static public string ConvertToRoleName(RoleInstance role, List<AssignableInstance> modifier, bool isShort)
    {
        string result = isShort ? role.Role.ShortName : role.Role.DisplayName;
        Color color = role.Role.RoleColor;
        foreach (var m in modifier) m.DecoratePlayerName(ref result, ref color);
        return result.Replace(" ","").Color(color);
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
    public VoiceChatManager? VoiceChatManager { get; set; } = GeneralConfigurations.UseVoiceChatOption ? new() : null;
    public ConsoleRestriction ConsoleRestriction { get; private set; } = new();
    public AttributeShower AttributeShower { get; private set; } = new();
    public RPCScheduler Scheduler { get; private set; } = new();

    //自身のキルボタン用トラッカー
    private ObjectTracker<PlayerControl> KillButtonTracker = null!;

    //天界視点フラグ
    public bool CanSeeAllInfo { get; set; }

    //ゲーム内履歴
    public List<RoleHistory> RoleHistory = new();

    static private SpriteLoader vcConnectSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.VCReconnectButton.png", 100f);
    public NebulaGameManager()
    {
        allModPlayers = new Dictionary<byte, PlayerModInfo>();
        instance = this;
        HudGrid = HudManager.Instance.gameObject.AddComponent<HudGrid>();
        RuntimeAsset = new();

        var vcConnectButton = new ModAbilityButton(true);
        vcConnectButton.Visibility = (_) => VoiceChatManager != null && GameState == NebulaGameStates.NotStarted;
        vcConnectButton.Availability = (_) =>true;
        vcConnectButton.SetSprite(vcConnectSprite.GetSprite());
        vcConnectButton.OnClick = (_) => VoiceChatManager!.Rejoin();
        vcConnectButton.SetLabel("rejoin");
        vcConnectButton.SetLabelType(ModAbilityButton.LabelType.Standard);
    }


    public void Abandon()
    {
        RuntimeAsset.Abandon();
        LobbySlideManager.Abandon();

        foreach (var script in allScripts) script.Release();
        allScripts.Clear();

        instance = null;
    }

    public void OnSceneChanged()
    {
        VoiceChatManager?.Dispose();
        VoiceChatManager = null;
    }

    public void OnTerminal()
    {
        VoiceChatManager?.Dispose();
        VoiceChatManager = null;
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

        int extraMask = 0;
        ulong extraWinMask = 0;

        foreach(var p in allModPlayers) if (p.Value.AllAssigned().Any(a => a.CheckWins(endCondition,ref extraWinMask))) winnersMask |= (1 << p.Value.PlayerId);
        foreach (var p in allModPlayers) if (p.Value.AllAssigned().Any(a => a.CheckExtraWins(endCondition, winnersMask, ref extraWinMask))) extraMask |= (1 << p.Value.PlayerId);
        winnersMask |= extraMask;

        NebulaGameEnd.RpcSendGameEnd(endCondition!, winnersMask, extraWinMask);
    }

    public void OnTaskUpdated()
    {
        CheckAndEndGame(CriteriaManager.OnTaskUpdated());
    }

    public void OnMeetingStart()
    {
        if (PlayerControl.LocalPlayer.Data.IsDead) CanSeeAllInfo = true;

        foreach (var p in allModPlayers) p.Value.OnMeetingStart();
        foreach (var script in allScripts) script.OnMeetingStart();

        AllRoleAction(r=>r.OnMeetingStart());

        Scheduler.Execute(RPCScheduler.RPCTrigger.PreMeeting);
    }

    public void OnMeetingEnd(PlayerControl? player)
    {
        ConsoleRestriction?.OnMeetingEnd();
        Scheduler.Execute(RPCScheduler.RPCTrigger.AfterMeeting);

        var tuple = CriteriaManager.OnExiled(player);
        if(tuple == null) return;
        CheckAndEndGame(tuple.Item1,tuple.Item2);

    }

    public void OnUpdate() {
        CurrentTime += Time.deltaTime;

        if (VoiceChatManager == null && GeneralConfigurations.UseVoiceChatOption) VoiceChatManager = new();
        VoiceChatManager?.Update();

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
            if (ventTimer != null && !ventTimer.IsInProcess && PlayerControl.LocalPlayer.inVent)
            {
                Vent.currentVent.SetButtons(false);
                PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(Vent.currentVent!.Id);
            }
            HudManager.Instance.ImpostorVentButton.SetCooldownFill(ventPercentage);
            CooldownHelpers.SetCooldownNormalizedUvs(HudManager.Instance.ImpostorVentButton.graphic);
            HudManager.Instance.ImpostorVentButton.cooldownTimerText.text = ventText;
            HudManager.Instance.ImpostorVentButton.cooldownTimerText.color = PlayerControl.LocalPlayer.inVent ? Color.green : Color.white;

            //サボタージュボタン


            //ローカルモジュール
            AttributeShower.Update(localModInfo);
        }

        CheckAndEndGame(CriteriaManager.OnUpdate());
    }

    public void OnFixedUpdate() {
        foreach (var script in allScripts) if (script.UpdateWithMyPlayer) script.Update();

        if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started && HudManager.Instance.KillButton.gameObject.active)
        {
            KillButtonTracker ??= ObjectTrackers.ForPlayer(null, PlayerControl.LocalPlayer, (p) => p.PlayerId != PlayerControl.LocalPlayer.PlayerId && !p.Data.IsDead && !p.Data.Role.IsImpostor);
            KillButtonTracker.Update();
            HudManager.Instance.KillButton.SetTarget(KillButtonTracker.CurrentTarget);
        }

    }
    public void OnGameStart()
    {
        //マップの取得
        RuntimeAsset.MinimapPrefab = ShipStatus.Instance.MapPrefab;
        RuntimeAsset.MinimapPrefab.gameObject.MarkDontUnload();
        RuntimeAsset.MapScale = ShipStatus.Instance.MapScale;

        //VC
        VoiceChatManager?.OnGameStart();

        //ゲームモードによる終了条件を追加
        foreach(var c in GeneralConfigurations.CurrentGameMode.GameModeCriteria)CriteriaManager.AddCriteria(c);

        foreach (var p in allModPlayers) p.Value.OnGameStart();
        NebulaGameManager.Instance?.AllScriptAction(s => s.OnGameStart());
        HudManager.Instance.UpdateHudContent();

        ConsoleRestriction?.OnGameStart();
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

    public void RpcInvokeForcelyWin(CustomEndCondition endCondition, int winnersMask)
    {
        RpcSpecialWin.Invoke(new(endCondition.Id, winnersMask));
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
        (_) =>
        {
            NebulaGameManager.Instance?.CheckGameState();
            NebulaGameManager.Instance?.AllRoleAction(r=>r.OnActivated());
        }

        );
}

public static class NebulaGameManagerExpansion
{
    static public PlayerModInfo? GetModInfo(this PlayerControl? player)
    {
        if (!player) return null;
        return NebulaGameManager.Instance?.GetModPlayerInfo(player!.PlayerId);
    }


}

