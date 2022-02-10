using HarmonyLib;
using System.Collections.Generic;

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
                __instance.RoleText.text = Language.Language.GetString("role." + data.role.LocalizeName + ".name");
                __instance.RoleText.color = data.role.Color;
                __instance.RoleBlurbText.text = Language.Language.GetString("role."+data.role.LocalizeName+".description");
                __instance.RoleBlurbText.color = data.role.Color;
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    static class HudManagerStartPatch
    {
        static public HudManager Manager;

        public static void Postfix(HudManager __instance)
        {
            Manager = __instance;
            foreach (Roles.Role role in Roles.Roles.AllRoles)
            {
                role.CleanUp();
            }
            foreach (Roles.ExtraRole role in Roles.Roles.AllExtraRoles)
            {
                role.CleanUp();
            }
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    class IntroCutsceneOnDestroyPatch
    {
        public static PoolablePlayer PlayerPrefab=null;
        public static void Postfix(IntroCutscene __instance)
        {
            PlayerPrefab = __instance.PlayerPrefab;

            if (CustomOptionHolder.limiterOptions.getBool())
            {
                Game.GameData.data.Timer = CustomOptionHolder.timeLimitOption.getFloat() * 60;
                Game.GameData.data.LimitRenderer = new Module.TimeLimit(HudManager.Instance);
                RPCEventInvoker.SynchronizeTimer();
            }

            
            Game.GameData.data.LoadMapData();

            Roles.Roles.StaticInitialize();

            foreach (Game.PlayerData player in Game.GameData.data.players.Values)
            {
                Helpers.RoleAction(player, (role) => {
                    PlayerControl pc = Helpers.playerById(player.id);
                    role.GlobalInitialize(pc);
                    role.GlobalIntroInitialize(pc);
                });
            }

            Helpers.RoleAction(PlayerControl.LocalPlayer, (role) => {
                role.Initialize(PlayerControl.LocalPlayer);
                role.IntroInitialize(PlayerControl.LocalPlayer);
                role.ButtonInitialize(HudManagerStartPatch.Manager);
                role.ButtonActivate();
            });            
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

            __instance.ImpostorText.text = "";
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

                __instance.RoleText.text = Language.Language.GetString("role." + role.LocalizeName + ".name");
                __instance.RoleText.color = role.Color;
                __instance.RoleBlurbText.text = Language.Language.GetString("role." + role.LocalizeName + ".description");
                __instance.RoleBlurbText.color = role.Color;


                //追加ロールの情報を付加
                string description=__instance.RoleBlurbText.text;
                foreach (Roles.ExtraRole exRole in Game.GameData.data.myData.getGlobalData().extraRole)
                {
                    exRole.EditDescriptionString(ref description);
                }
                __instance.RoleBlurbText.text = description;
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
