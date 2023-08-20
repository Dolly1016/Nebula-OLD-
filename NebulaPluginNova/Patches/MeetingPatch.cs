using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using Nebula.Modules;

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