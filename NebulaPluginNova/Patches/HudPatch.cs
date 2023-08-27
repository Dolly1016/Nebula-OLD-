using HarmonyLib;
using Nebula.Game;
using System;
using System.Collections;

namespace Nebula.Patches;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
public static class HudManagerStartPatch
{
    static void Prefix(HudManager __instance)
    {
        NebulaGameManager.Instance?.Abandon();
    }
    static void Postfix(HudManager __instance)
    {
        new NebulaGameManager();

        var renderer = UnityHelper.CreateObject<SpriteRenderer>("Light(Dummy)", Camera.main.transform, new Vector3(0, 0, -1.5f), LayerExpansion.GetDrawShadowsLayer());
        renderer.sprite = VanillaAsset.FullScreenSprite;
        renderer.material.shader = NebulaAsset.StoreBackShader;
        renderer.color = Color.clear;
    }
}


[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudManagerUpdatePatch
{
    static void Postfix(HudManager __instance)
    {
        NebulaGameManager.Instance?.OnUpdate();

        if (NebulaInput.GetKeyDown(KeyCode.H))
        {
            var window = MetaScreen.GenerateWindow(new Vector2(7.5f, 4f), HudManager.Instance.transform, new Vector3(0, 0, -400f), true, false);
            //window.gameObject.AddComponent<SortingGroup>();

            var roleTitleAttr = new TextAttribute(TextAttribute.BoldAttr) { Size = new Vector2(1.4f, 0.26f), FontMaterial = VanillaAsset.StandardMaskedFontMaterial };
            MetaContext scrollInnner = new();
            MetaContext.ScrollView scrollView = new(new(7.4f,4f),scrollInnner);
            scrollInnner.Append(Roles.Roles.AllRoles, (role) => new MetaContext.Button(() => { }, roleTitleAttr)
            {
                RawText = role.DisplayName.Color(role.RoleColor),
                PostBuilder = (button, renderer, text) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask,
                Alignment = IMetaContext.AlignmentOption.Center
            }, 4, -1, 0, 0.6f); 

            window.SetContext(scrollView);
        }
    }
}


[HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
public static class HudManagerCoStartGamePatch
{
    static bool Prefix(HudManager __instance,ref Il2CppSystem.Collections.IEnumerator __result)
    {
        IEnumerator GetEnumerator(){
            while (!ShipStatus.Instance)
            {
                yield return null;
            }
            __instance.IsIntroDisplayed = true;
            DestroyableSingleton<HudManager>.Instance.FullScreen.transform.localPosition = new Vector3(0f, 0f, -250f);
            yield return DestroyableSingleton<HudManager>.Instance.ShowEmblem(true);
            IntroCutscene introCutscene = GameObject.Instantiate<IntroCutscene>(__instance.IntroPrefab, __instance.transform);
            yield return introCutscene.CoBegin();
            PlayerControl.LocalPlayer.SetKillTimer(10f);
            ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>().ForceSabTime(10f);

            yield return ModPreSpawnInPatch.ModPreSpawnIn(__instance.transform);
            NebulaGameManager.Instance.GameStatistics.RecordEvent(new GameStatistics.Event(GameStatistics.EventVariation.GameStart, null, 0, GameStatisticsGatherTag.Spawn) { RelatedTag = EventDetail.GameStart });

            PlayerControl.LocalPlayer.AdjustLighting();
            yield return __instance.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false);
            __instance.FullScreen.transform.localPosition = new Vector3(0f, 0f, -500f);
            __instance.IsIntroDisplayed = false;
            __instance.CrewmatesKilled.gameObject.SetActive(GameManager.Instance.ShowCrewmatesKilled());
            GameManager.Instance.StartGame();
        }
        __result = GetEnumerator().WrapToIl2Cpp();

        return false;
    }

}