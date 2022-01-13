using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.SetUpRoleText))]
    public class SetUpRoleTextPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            Game.PlayerData data = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId];
            if (data == null)
            {
                return;
            }

            if (data.role != null)
            {
                __instance.RoleText.text = Language.Language.GetString("role." + data.role.localizeName + ".name");
                __instance.RoleText.color = data.role.color;
                __instance.RoleBlurbText.text = Language.Language.GetString("role."+data.role.localizeName+".description");
                __instance.RoleBlurbText.color = data.role.color;
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    static class HudManagerStartPatch
    {
        public static HudManager Manager;

        public static void Postfix(HudManager __instance)
        {
            Manager = __instance;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    class IntroCutsceneOnDestroyPatch
    {
        public static void Prefix(IntroCutscene __instance)
        {
            Roles.Role role = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role;

            role.ButtonInitialize(HudManagerStartPatch.Manager);
            role.Initialize(PlayerControl.LocalPlayer);
        }
    }

    [HarmonyPatch]
    class IntroPatch
    {
        public static void setupIntroTeamText(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            Roles.Role role = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role;

            __instance.BackgroundBar.material.color = role.introMainDisplaySide.color;
            __instance.TeamTitle.text = Language.Language.GetString("side." + role.introMainDisplaySide.localizeSide + ".name");
            __instance.TeamTitle.color = role.introMainDisplaySide.color;
        }

        public static void setupIntroTeamMembers(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            Roles.Role role = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role;

            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            Roles.Role.ExtractDisplayPlayers(ref yourTeam);
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.SetUpRoleText))]
        class SetUpRoleTextPatch
        {
            public static void Postfix(IntroCutscene __instance)
            {
                Roles.Role role = Game.GameData.data.players[PlayerControl.LocalPlayer.PlayerId].role;

                __instance.RoleText.text = Language.Language.GetString("role." + role.localizeName + ".name");
                __instance.RoleText.color = role.color;
                __instance.RoleBlurbText.text = Language.Language.GetString("role." + role.localizeName + ".description");
                __instance.RoleBlurbText.color = role.color;
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        class BeginCrewmatePatch
        {
            public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroTeamMembers(__instance, ref yourTeam);
            }
            public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroTeamText(__instance, ref yourTeam);
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
        class BeginImpostorPatch
        {
            public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroTeamMembers(__instance, ref yourTeam);
            }
            public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
            {
                setupIntroTeamText(__instance, ref yourTeam);
            }
        }
    }
}
