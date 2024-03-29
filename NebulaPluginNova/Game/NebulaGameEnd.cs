﻿using Discord;
using HarmonyLib;
using Hazel;
using System.Collections;
using InnerNet;
using Nebula.Modules;
using UnityEngine;
using UnityEngine.TextCore;

namespace Nebula.Game;

public class CustomEndCondition
{
    static private HashSet<CustomEndCondition> allEndConditions= new();
    static public CustomEndCondition? GetEndCondition(byte id) => allEndConditions.FirstOrDefault(end => end.Id == id);
    
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

        allEndConditions.Add(this);
    }
}

public class CustomExtraWin
{
    static private HashSet<CustomExtraWin> allExtraWin = new();
    static public CustomExtraWin? GetEndCondition(byte id) => allExtraWin.FirstOrDefault(end => end.Id == id);
    static public IEnumerable<CustomExtraWin> AllExtraWins => allExtraWin;
    public byte Id { get; private set; }
    public string LocalizedName { get; init; }
    public string DisplayText => Language.Translate("end.extra." + LocalizedName);
    public Color Color { get; init; }

    public CustomExtraWin(byte id,string localizedName,Color color)
    {
        Id = id;
        LocalizedName = localizedName;
        Color = color;

        allExtraWin.Add(this);
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
    static public CustomEndCondition JackalWin = new(26, "jackal", Roles.Neutral.Jackal.MyRole.RoleColor, 16);
    static public CustomEndCondition ArsonistWin = new(27, "arsonist", Roles.Neutral.Arsonist.MyRole.RoleColor, 32);
    static public CustomEndCondition LoversWin = new(28, "lover", Roles.Modifier.Lover.MyRole.RoleColor, 18);
    static public CustomEndCondition NoGame = new(128, "nogame", InvalidColor, 128);

    static public CustomExtraWin ExtraLoversWin = new(0, "lover", Roles.Modifier.Lover.MyRole.RoleColor);

    private readonly static RemoteProcess<(byte conditionId, int winnersMask,ulong extraWinMask)> RpcEndGame = new(
       "EndGame",
       (message, isCalledByMe) =>
       {
           if (NebulaGameManager.Instance != null)
           {
               NebulaGameManager.Instance.EndState ??= new NebulaEndState(message.conditionId,message.winnersMask,message.extraWinMask);
               NebulaGameManager.Instance.ToGameEnd();
               NebulaGameManager.Instance.OnGameEnd();
           }
       }
       );

    public static bool RpcSendGameEnd(CustomEndCondition winCondition, int winnersMask, ulong extraWinMask)
    {
        if (NebulaGameManager.Instance?.EndState != null) return false;
        RpcEndGame.Invoke((winCondition.Id, winnersMask, extraWinMask));
        return true;
    }

    public static bool RpcSendGameEnd(CustomEndCondition winCondition,HashSet<byte> winners, ulong extraWinMask)
    {
        int winnersMask = 0;
        foreach (byte w in winners) winnersMask |= ((int)1 << w);
        return RpcSendGameEnd(winCondition, winnersMask, extraWinMask);
    }
}


[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
public class EndGameManagerSetUpPatch
{
    static SpriteLoader InfoButtonSprite = SpriteLoader.FromResource("Nebula.Resources.InformationButton.png", 100f);
    private static IMetaContext GetRoleContent(TMPro.TMP_FontAsset font)
    {
        MetaContext context = new();
        string text = "";
        foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo())
        {
            //Name Text
            string nameText = p.DefaultName;
            string stateText = p.MyState?.Text ?? "";
            string taskText = p.Tasks.Quota > 0 ? $" ({p.Tasks.ToString(true)})".Color(p.Tasks.IsCrewmateTask ? PlayerModInfo.CrewTaskColor : PlayerModInfo.FakeTaskColor) : "";

            //Role Text
            string roleText = "";
            var entries = NebulaGameManager.Instance.RoleHistory.EachMoment(history => history.PlayerId == p.PlayerId,
                (role, modifiers) => (RoleHistoryHelper.ConvertToRoleName(role, modifiers, true), RoleHistoryHelper.ConvertToRoleName(role, modifiers, false))).ToArray();

            if (entries.Length < 8)
            {
                for (int i = 0; i < entries.Length - 1; i++)
                {
                    if (roleText.Length > 0) roleText += " → ";
                    roleText += entries[i].Item1;
                }
            }
            else
            {
                roleText = entries[0].Item1 + " → ...";
            }

            if (roleText.Length > 0) roleText += " → ";
            roleText += entries[entries.Length - 1].Item2;

            text += $"{nameText}<indent=15px>{taskText}</indent><indent=24px>{stateText}</indent><indent=34px>{roleText}</indent>\n";
        }

        context.Append(new MetaContext.VariableText(new TextAttribute(TextAttribute.BoldAttr) { Font = font, Size = new(6f, 4.2f), Alignment = TMPro.TextAlignmentOptions.Left }.EditFontSize(1.4f, 1f, 1.4f))
        { Alignment = IMetaContext.AlignmentOption.Left, RawText = text });

        return context;
    }

    public static void Postfix(EndGameManager __instance)
    {
        if (NebulaGameManager.Instance == null) return;
        var endState = NebulaGameManager.Instance.EndState;
        var endCondition = endState?.EndCondition;

        if(endState==null) return;

        //元の勝利チームを削除する
        foreach (PoolablePlayer pb in __instance.transform.GetComponentsInChildren<PoolablePlayer>()) UnityEngine.Object.Destroy(pb.gameObject);

        
        //勝利メンバーを載せる
        List<byte> winners = new List<byte>();
        bool amWin = false;
        for (byte i= 0;i < 32; i++)
        {
            if ((endState.WinnersMask & (1 << i)) != 0)
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

            var player = NebulaGameManager.Instance.GetModPlayerInfo(winners[i])!;

            if (player.IsDead)//死んでいる場合
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

        string extraText = "";
        foreach (var extraWin in NebulaGameManager.Instance!.EndState!.ExtraWins) extraText += extraWin.DisplayText;
        textRenderer.text = endCondition?.DisplayText.Replace("%EXTRA%", extraText) ?? "Error";
        textRenderer.color = endCondition?.Color ?? Color.white;

        __instance.BackgroundBar.material.SetColor("_Color", endCondition?.Color ?? new Color(1f, 1f, 1f));

        __instance.WinText.text = DestroyableSingleton<TranslationController>.Instance.GetString(amWin ? StringNames.Victory : StringNames.Defeat);
        __instance.WinText.color = amWin ? new Color(0f, 0.549f, 1f, 1f) : Color.red;

        GameStatisticsViewer? viewer;

        IEnumerator CoShowStatistics()
        {
            yield return new WaitForSeconds(0.4f);
            viewer = UnityHelper.CreateObject<GameStatisticsViewer>("Statistics", __instance.transform, new Vector3(0f, 2.5f, -20f),LayerExpansion.GetUILayer());
            viewer.PlayerPrefab = __instance.PlayerPrefab;
            viewer.GameEndText = __instance.WinText;
        }
        __instance.StartCoroutine(CoShowStatistics().WrapToIl2Cpp());

        var buttonRenderer = UnityHelper.CreateObject<SpriteRenderer>("InfoButton", __instance.transform, new Vector3(-2.9f, 2.5f, -50f), LayerExpansion.GetUILayer());
        buttonRenderer.sprite = InfoButtonSprite.GetSprite();
        var button = buttonRenderer.gameObject.SetUpButton(false, buttonRenderer);
        button.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpContext(button, GetRoleContent(__instance.WinText.font)));
        button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpContextIf(button));
        button.gameObject.AddComponent<BoxCollider2D>().size = new(0.3f, 0.3f);
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoEndGame))]
public class EndGamePatch
{

    public static bool Prefix(AmongUsClient __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (NebulaGameManager.Instance == null) return true;
        NebulaGameManager.Instance.ReceiveVanillaGameResult();
        NebulaGameManager.Instance.ToGameEnd();

        __result = Effects.Wait(0.1f);
        return false;
    }
}