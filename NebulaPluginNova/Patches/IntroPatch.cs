using HarmonyLib;
using Nebula.Game;
using Nebula.Modules;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public static class EndIntroPatch
{
    static void Postfix(IntroCutscene __instance)
    {
        NebulaGameManager.Instance?.OnGameStart();
        HudManager.Instance.ShowVanillaKeyGuide();
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
public static class ShowIntroPatch
{
    static bool Prefix(IntroCutscene __instance,ref Il2CppSystem.Collections.IEnumerator __result)
    {
        __result = CoBegin(__instance).WrapToIl2Cpp();
        return false;
    }

    static IEnumerator CoBegin(IntroCutscene __instance)
    {
        HudManager.Instance.HideGameLoader();

        SoundManager.Instance.PlaySound(__instance.IntroStinger, false, 1f, null);

        __instance.HideAndSeekPanels.SetActive(false);
        __instance.CrewmateRules.SetActive(false);
        __instance.ImpostorRules.SetActive(false);
        __instance.ImpostorName.gameObject.SetActive(false);
        __instance.ImpostorTitle.gameObject.SetActive(false);
        __instance.ImpostorText.gameObject.SetActive(false);

        IEnumerable<PlayerControl> shownPlayers = PlayerControl.AllPlayerControls.GetFastEnumerator().OrderBy(p => p.AmOwner ? 0 : 1);
        var myInfo = PlayerControl.LocalPlayer.GetModInfo();
        switch (myInfo?.Role.Role.Team.RevealType)
        {
            case Roles.TeamRevealType.OnlyMe:
                shownPlayers = new PlayerControl[] { PlayerControl.LocalPlayer };
                break;
            case Roles.TeamRevealType.Teams:
                shownPlayers = shownPlayers.Where(p => p.GetModInfo()?.Role.Role.Team == myInfo.Role.Role.Team);
                break;
        }

        yield return CoShowTeam(__instance,myInfo!,shownPlayers.ToArray(), 3f);
        yield return CoShowRole(__instance,myInfo!);
        GameObject.Destroy(__instance.gameObject);
    }

    static IEnumerator CoShowTeam(IntroCutscene __instance, PlayerModInfo myInfo, PlayerControl[] shownPlayers, float duration)
    {
        if (__instance.overlayHandle == null)
        {
            __instance.overlayHandle = DestroyableSingleton<DualshockLightManager>.Instance.AllocateLight();
        }
        yield return ShipStatus.Instance.CosmeticsCache.PopulateFromPlayers();

        Color c = myInfo.Role!.Role.Team.Color;

        Vector3 position = __instance.BackgroundBar.transform.position;
        position.y -= 0.25f;
        __instance.BackgroundBar.transform.position = position;
        __instance.BackgroundBar.material.SetColor("_Color", c);
        __instance.TeamTitle.text = Language.Translate(myInfo.Role.Role.Team.TranslationKey);
        __instance.TeamTitle.color = c;
        int maxDepth = Mathf.CeilToInt(7.5f);
        for (int i = 0; i < shownPlayers.Length; i++)
        {
            PlayerControl playerControl = shownPlayers[i];
            if (playerControl)
            {
                GameData.PlayerInfo data = playerControl.Data;
                if (data != null)
                {
                    PoolablePlayer poolablePlayer = __instance.CreatePlayer(i, maxDepth, data, false);
                    if (i == 0 && data.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        __instance.ourCrewmate = poolablePlayer;
                    }
                }
            }
        }

        __instance.overlayHandle.color = c;

        
        Color fade = Color.black;
        Color impColor = Color.white;
        Vector3 titlePos = __instance.TeamTitle.transform.localPosition;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float num = Mathf.Min(1f, timer / duration);
            __instance.Foreground.material.SetFloat("_Rad", __instance.ForegroundRadius.ExpOutLerp(num * 2f));
            fade.a = Mathf.Lerp(1f, 0f, num * 3f);
            __instance.FrontMost.color = fade;
            c.a = Mathf.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
            __instance.TeamTitle.color = c;
            __instance.RoleText.color = c;
            impColor.a = Mathf.Lerp(0f, 1f, (num - 0.3f) * 3f);
            __instance.ImpostorText.color = impColor;
            titlePos.y = 2.7f - num * 0.3f;
            __instance.TeamTitle.transform.localPosition = titlePos;
            __instance.overlayHandle.color = c.AlphaMultiplied(Mathf.Min(1f, timer * 2f));
            yield return null;
        }
        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            float num2 = timer / 1f;
            fade.a = Mathf.Lerp(0f, 1f, num2 * 3f);
            __instance.FrontMost.color = fade;
            __instance.overlayHandle.color = c.AlphaMultiplied(1f - fade.a);
            yield return null;
        }
        yield break;
    }

    static IEnumerator CoShowRole(IntroCutscene __instance, PlayerModInfo myInfo)
    {
        var role = myInfo.Role.Role;
        __instance.RoleText.text = role.DisplayName;
        __instance.RoleBlurbText.text = role.IntroBlurb;
        __instance.RoleBlurbText.transform.localPosition = new(0.0965f, -2.12f, -36f);
        __instance.RoleBlurbText.rectTransform.sizeDelta = new(12.8673f, 0.7f);
        __instance.RoleBlurbText.alignment = TMPro.TextAlignmentOptions.Top;

        foreach(var m in myInfo.AllModifiers)
        {
            string? mBlurb = m.IntroText;
            if (mBlurb != null) __instance.RoleBlurbText.text += "\n" + mBlurb;
        }
        __instance.RoleText.color = role.RoleColor;
        __instance.YouAreText.color = role.RoleColor;
        __instance.RoleBlurbText.color = role.RoleColor;
        SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.Data.Role.IntroSound, false, 1f, null);
        __instance.YouAreText.gameObject.SetActive(true);
        __instance.RoleText.gameObject.SetActive(true);
        __instance.RoleBlurbText.gameObject.SetActive(true);
        if (__instance.ourCrewmate == null)
        {
            __instance.ourCrewmate = __instance.CreatePlayer(0, 1, PlayerControl.LocalPlayer.Data, false);
            __instance.ourCrewmate.gameObject.SetActive(false);
        }
        __instance.ourCrewmate.gameObject.SetActive(true);
        __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
        __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);
        __instance.ourCrewmate.ToggleName(false);
        yield return new WaitForSeconds(2.5f);
        __instance.YouAreText.gameObject.SetActive(false);
        __instance.RoleText.gameObject.SetActive(false);
        __instance.RoleBlurbText.gameObject.SetActive(false);
        __instance.ourCrewmate.gameObject.SetActive(false);
        yield break;
    }
}

