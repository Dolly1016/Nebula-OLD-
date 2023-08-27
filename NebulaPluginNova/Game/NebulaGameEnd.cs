using Discord;
using HarmonyLib;
using Hazel;
using System.Collections;
using InnerNet;
using Nebula.Modules;
using UnityEngine;

namespace Nebula.Game;

public class CustomEndCondition
{
    static private HashSet<CustomEndCondition> AllEndConditions= new HashSet<CustomEndCondition>();
    static public CustomEndCondition? GetEndCondition(byte id) => AllEndConditions.FirstOrDefault(end => end.Id == id);
    
    public byte Id { get; init; }
    public string LocalizedName { get; init; }
    public string DisplayText => Language.Translate("end." + LocalizedName);
    public Color Color { get; init; }
    public int Priority { get; init; }

    //優先度が高いほど他の勝利を無視して勝利する
    public CustomEndCondition(byte id,string localizedName,Color color,int priority)
    {
        Id = id;
        LocalizedName = localizedName;
        Color = color;
        Priority = priority;

        AllEndConditions.Add(this);
    }
}

[NebulaRPCHolder]
public class NebulaGameEnd
{
    static private Color InvalidColor = new Color(72f / 255f, 78f / 255f, 84f / 255f);
    static public CustomEndCondition CrewmateWin = new(16, "crewmate", Palette.CrewmateBlue, 16);
    static public CustomEndCondition ImpostorWin = new(17, "impostor", Palette.ImpostorRed, 16);
    static public CustomEndCondition VultureWin = new(24, "vulture", Roles.Neutral.Vulture.MyRole.RoleColor, 32);
    static public CustomEndCondition JesterWin = new(25, "jester", Roles.Neutral.Jester.MyRole.RoleColor, 32);
    static public CustomEndCondition NoGame = new(128, "nogame", InvalidColor, 128);

    private readonly static RemoteProcess<NebulaEndState> RpcEndGame = new RemoteProcess<NebulaEndState>(
       "EndGame",
       (writer, message) =>
       {
           writer.Write(message.ConditionId);
           writer.Write(message.WinnersMask);
       },
       (reader) =>
       {
           return new NebulaEndState(reader.ReadByte(),reader.ReadUInt64());
       },
       (message, isCalledByMe) =>
       {
           if (NebulaGameManager.Instance != null)
           {
               NebulaGameManager.Instance.EndState ??= message;
               NebulaGameManager.Instance.ToGameEnd();
               NebulaGameManager.Instance.OnGameEnd();
           }
       }
       );

    public static bool RpcSendGameEnd(CustomEndCondition winCondition,HashSet<byte> winners)
    {
        if (NebulaGameManager.Instance.EndState != null) return false;
        ulong winnersMask = 0;
        foreach (byte w in winners) winnersMask |= (ulong)(1 << w);
        RpcEndGame.Invoke(new NebulaEndState(winCondition.Id, winnersMask));
        return true;
    }

    public static void RpcSendNoGame()
    {
        RpcEndGame.Invoke(new NebulaEndState(NoGame.Id, 1));
    }
}


[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
public class EndGameManagerSetUpPatch
{

    public static void Postfix(EndGameManager __instance)
    {
        if (NebulaGameManager.Instance == null) return;
        var endState = NebulaGameManager.Instance.EndState;
        var endCondition = endState.EndCondition;

        if(endState==null) return;

        //元の勝利チームを削除する
        foreach (PoolablePlayer pb in __instance.transform.GetComponentsInChildren<PoolablePlayer>()) UnityEngine.Object.Destroy(pb.gameObject);

        
        //勝利メンバーを載せる
        List<byte> winners = new List<byte>();
        bool amWin = false;
        for (byte i= 0;i < 32; i++)
        {
            if ((endState.WinnersMask & (ulong)(1 << i)) != 0)
            {
                if (NebulaGameManager.Instance.GetModPlayerInfo(i)?.AmOwner ?? false)
                {
                    amWin = true;
                    winners.Insert(0, i);
                }
                else
                    winners.Add(i);
            }
        }

        int num = Mathf.CeilToInt(7.5f);
        for (int i = 0; i < winners.Count; i++)
        {
            int num2 = (i % 2 == 0) ? -1 : 1;
            int num3 = (i + 1) / 2;
            float num4 = (float)num3 / (float)num;
            float num5 = Mathf.Lerp(1f, 0.75f, num4);
            float num6 = (float)((i == 0) ? -8 : -1);
            PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate<PoolablePlayer>(__instance.PlayerPrefab, __instance.transform);
            poolablePlayer.transform.localPosition = new Vector3(1f * (float)num2 * (float)num3 * num5, FloatRange.SpreadToEdges(-1.125f, 0f, num3, num), num6 + (float)num3 * 0.01f) * 0.9f;
            float num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
            Vector3 vector = new Vector3(num7, num7, 1f);
            poolablePlayer.transform.localScale = vector;

            var player = NebulaGameManager.Instance.GetModPlayerInfo(winners[i]);

            if (false)//死んでいる場合
            {
                poolablePlayer.SetBodyAsGhost();
                poolablePlayer.SetDeadFlipX(i % 2 == 0);
            }
            else
            {
                poolablePlayer.SetFlipX(i % 2 == 0);
            }
            poolablePlayer.UpdateFromPlayerOutfit(player.DefaultOutfit, PlayerMaterial.MaskType.None, false, true);

            poolablePlayer.SetName(player.DefaultName, new Vector3(1f / vector.x, 1f / vector.y, 1f / vector.z), Color.white, -15f); ;
            poolablePlayer.SetNamePosition(new Vector3(0f, -1.31f, -0.5f));
        }

        // テキストを追加する
        GameObject bonusText = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        bonusText.transform.SetParent(null);
        bonusText.transform.position = new Vector3(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
        bonusText.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        TMPro.TMP_Text textRenderer = bonusText.GetComponent<TMPro.TMP_Text>();
        textRenderer.text = endCondition?.DisplayText;
        textRenderer.color = endCondition?.Color ?? Color.white;

        __instance.BackgroundBar.material.SetColor("_Color", endCondition?.Color ?? new Color(1f, 1f, 1f));

        __instance.WinText.text = DestroyableSingleton<TranslationController>.Instance.GetString(amWin ? StringNames.Victory : StringNames.Defeat);
        __instance.WinText.color = amWin ? new Color(0f, 0.549f, 1f, 1f) : Color.red;

        IEnumerator CoShowStatistics()
        {
            yield return new WaitForSeconds(0.4f);
            var viewer = UnityHelper.CreateObject<GameStatisticsViewer>("Statistics", __instance.transform, new Vector3(0f, 2.5f, -20f),LayerExpansion.GetUILayer());
            viewer.PlayerPrefab = __instance.PlayerPrefab;
            viewer.GameEndText = __instance.WinText;
        }
        __instance.StartCoroutine(CoShowStatistics().WrapToIl2Cpp());
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoEndGame))]
public class EndGamePatch
{

    public static bool Prefix(AmongUsClient __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        Debug.Log("Test");
        if (NebulaGameManager.Instance == null) return true;
        NebulaGameManager.Instance.ReceiveVanillaGameResult();
        NebulaGameManager.Instance.ToGameEnd();

        __result = Effects.Wait(0.1f);
        return false;
    }
}