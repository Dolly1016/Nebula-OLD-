using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using Nebula.Modules;
using UnityEngine.Rendering;
using Nebula.Behaviour;

namespace Nebula.Patches;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingStartPatch
{
    static private ISpriteLoader LightColorSprite = SpriteLoader.FromResource("Nebula.Resources.ColorLight.png", 100f);
    static private ISpriteLoader DarkColorSprite = SpriteLoader.FromResource("Nebula.Resources.ColorDark.png", 100f);

    class MeetingPlayerContent
    {
        public TMPro.TextMeshPro NameText, RoleText;
        public PlayerModInfo Player;
    }

    static private void Update(List<MeetingPlayerContent> meetingContent)
    {
        foreach(var content in meetingContent)
        {
            if(content.NameText) content.Player.UpdateNameText(content.NameText);
            if (content.RoleText) content.Player.UpdateRoleText(content.RoleText);
        }
    }

    static void Postfix(MeetingHud __instance)
    {
        NebulaManager.Instance.CloseAllUI();

        NebulaGameManager.Instance?.OnMeetingStart();

        List<MeetingPlayerContent> allContents = new();


        //色の明暗を表示
        foreach (var player in __instance.playerStates)
        {
            bool isLightColor = DynamicPalette.IsLightColor(Palette.PlayerColors[player.TargetPlayerId]);

            SpriteRenderer renderer = UnityHelper.CreateObject<SpriteRenderer>("Color", player.transform, new Vector3(1.2f, -0.18f, -1f));
            renderer.sprite = isLightColor ? LightColorSprite.GetSprite() : DarkColorSprite.GetSprite();

            player.ColorBlindName.gameObject.SetActive(false);

            var roleText = GameObject.Instantiate(player.NameText, player.transform);
            roleText.transform.localPosition = new Vector3(0.3384f, -0.15f, -0.02f);
            roleText.transform.localScale = new Vector3(0.6f,0.6f);

            allContents.Add(new() { Player = Helpers.GetPlayer(player.TargetPlayerId)!.GetModInfo()!, NameText = player.NameText, RoleText = roleText });
        }

        Update(allContents);

        IEnumerator CoUpdate()
        {
            while (true)
            {
                Update(allContents);
                yield return null;
            }
        }
        __instance.StartCoroutine(CoUpdate().WrapToIl2Cpp());
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
class MeetingClosePatch
{ 
    public static void Postfix(MeetingHud __instance)
    {
        NebulaManager.Instance.CloseAllUI();
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetMaskLayer))]
class VoteMaskPatch
{
    public static bool Prefix(PlayerVoteArea __instance)
    {
        return false;
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Start))]
class VoteAreaPatch
{
    public static void Postfix(PlayerVoteArea __instance)
    {
        try
        {
            var maskParent = UnityHelper.CreateObject<SortingGroup>("MaskedObjects", __instance.transform, new Vector3(0, 0, -0.1f));
            __instance.MaskArea.transform.SetParent(maskParent.transform);
            __instance.PlayerIcon.transform.SetParent(maskParent.transform);

            var mask = __instance.MaskArea.gameObject.AddComponent<SpriteMask>();
            mask.sprite = __instance.MaskArea.sprite;
            mask.transform.localScale = __instance.MaskArea.size;
            __instance.MaskArea.enabled = false;

            __instance.Background.material = __instance.Megaphone.material;

            __instance.PlayerIcon.cosmetics.currentBodySprite.BodySprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            __instance.PlayerIcon.cosmetics.hat.FrontLayer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.hat.BackLayer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.visor.Image.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.skin.layer.gameObject.AddComponent<ZOrderedSortingGroup>();
            __instance.PlayerIcon.cosmetics.currentBodySprite.BodySprite.gameObject.AddComponent<ZOrderedSortingGroup>();
        }
        catch { }
    }
}

