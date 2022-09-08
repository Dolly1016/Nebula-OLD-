using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Nebula.Utilities;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    public class CoStartPatch
    {
        public static void Postfix(IntroCutscene __instance,ref Il2CppSystem.Collections.IEnumerator __result)
        {
            if (CustomOptionHolder.GetCustomGameMode() == Module.CustomGameMode.Ritual)
            {
                RPCEventInvoker.RitualSharePerks(PlayerControl.LocalPlayer.PlayerId,new int[] { 0, 0, 0, 0 });

                var enumerators = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Collections.IEnumerator>(3);
                enumerators[0] = Game.SynchronizeData.GetStaticAlignEnumeratorIl2Cpp(Game.SynchronizeTag.RitualInitialize, true, true);
                enumerators[1] = Effects.Action((Il2CppSystem.Action)(() => Game.GameData.data.RitualData.SpawnAllPlayers()));
                enumerators[2] = __result;
                __result = Effects.Sequence(enumerators);
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
            CloseSpawnGUIPatch.Actions.Clear();

            PlayerPrefab = __instance.PlayerPrefab;

            if (CustomOptionHolder.limiterOptions.getBool())
            {
                Game.GameData.data.Timer = CustomOptionHolder.timeLimitOption.getFloat() * 60 + CustomOptionHolder.timeLimitSecondOption.getFloat();
                Game.GameData.data.LimitRenderer = new Module.TimeLimit(FastDestroyableSingleton<HudManager>.Instance);
                RPCEventInvoker.SynchronizeTimer();
            }

            Roles.Roles.StaticInitialize();
            
            //役職予測を初期化
            Game.GameData.data.EstimationAI.Initialize();

            foreach (Game.PlayerData player in Game.GameData.data.AllPlayers.Values)
            {
                Helpers.RoleAction(player, (role) =>
                {
                    PlayerControl pc = Helpers.playerById(player.id);
                    role.GlobalInitialize(pc);
                    role.GlobalIntroInitialize(pc);
                });

                //遍歴に最初の役職を書き込む
                player.AddRoleHistory();
            }

            Helpers.RoleAction(PlayerControl.LocalPlayer, (role) =>
            {
                role.Initialize(PlayerControl.LocalPlayer);
                role.IntroInitialize(PlayerControl.LocalPlayer);
                role.ButtonInitialize(HudManagerStartPatch.Manager);
            });
            Objects.CustomButton.ButtonActivate();

            Game.GameData.data.myData.VentCoolDownTimer = PlayerControl.LocalPlayer.GetModData().role.VentCoolDownMaxTimer;

            if (AmongUsClient.Instance.AmHost)
            {
                if (Game.GameModeProperty.GetProperty(Game.GameData.data.GameMode).RequireStartCountDown)
                {
                    byte count = 10;
                    FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(10f, new System.Action<float>((p) =>
                    {
                        if ((byte)((1f - p) * 10f) < count)
                        {
                            RPCEventInvoker.CountDownMessage(count);
                            count = (byte)((1f - p) * 10f);
                        }
                        if (p == 1f)
                        {
                            RPCEventInvoker.CountDownMessage(0);
                            Game.GameModeProperty.GetProperty(Game.GameData.data.GameMode).OnCountFinished.Invoke();
                        }
                    })));
                }
            }
        }
    }

    [HarmonyPatch]
    class IntroPatch
    {
        public static void setupIntroTeamText(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            Roles.Role role = Game.GameData.data.playersArray[PlayerControl.LocalPlayer.PlayerId].role;

            __instance.BackgroundBar.material.color = role.introMainDisplaySide.color;
            __instance.TeamTitle.text = Language.Language.GetString("side." + role.introMainDisplaySide.localizeSide + ".name");
            __instance.TeamTitle.color = role.introMainDisplaySide.color;

            __instance.ImpostorText.text = "";
        }

        public static void setupIntroTeamMembers(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {

            Roles.Role role = Game.GameData.data.playersArray[PlayerControl.LocalPlayer.PlayerId].role;

            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            Roles.Role.ExtractDisplayPlayers(ref yourTeam);
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
        class SetUpRoleTextPatch
        {
            private static void setUpRoleText(IntroCutscene __instance)
            {
                Roles.Role role = Game.GameData.data.AllPlayers[PlayerControl.LocalPlayer.PlayerId].role;

                string roleNames = Language.Language.GetString("role." + role.LocalizeName + ".name");
                Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { role.EditDisplayRoleName(ref roleNames); });

                __instance.RoleText.text = roleNames;
                __instance.RoleText.color = role.Color;
                __instance.RoleBlurbText.text = Language.Language.GetString("role." + role.LocalizeName + ".description");
                __instance.RoleBlurbText.color = role.Color;
                __instance.YouAreText.color = role.side.color;

                //追加ロールの情報を付加
                string description = __instance.RoleBlurbText.text;
                foreach (Roles.ExtraRole exRole in Game.GameData.data.myData.getGlobalData().extraRole)
                {
                    exRole.EditDescriptionString(ref description);
                }
                __instance.RoleBlurbText.text = description;

                __instance.YouAreText.gameObject.SetActive(true);
                __instance.RoleText.gameObject.SetActive(true);
                __instance.RoleBlurbText.gameObject.SetActive(true);

                SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.Data.Role.IntroSound, false, 1f);

                if (__instance.ourCrewmate == null)
                {
                    __instance.ourCrewmate = __instance.CreatePlayer(0, 1, PlayerControl.LocalPlayer.Data, false);
                    __instance.ourCrewmate.gameObject.SetActive(false);
                }
                __instance.ourCrewmate.gameObject.SetActive(true);
                __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
                __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            public static bool Prefix(IntroCutscene __instance,ref Il2CppSystem.Collections.IEnumerator __result)
            {
                var list = new List<Il2CppSystem.Collections.IEnumerator>();

                list.Add(Effects.Action((Il2CppSystem.Action)(()=>{
                    setUpRoleText(__instance);
                })));
                list.Add(Effects.Wait(2.5f));
                list.Add(Effects.Action((Il2CppSystem.Action)(() => {
                    __instance.YouAreText.gameObject.SetActive(false);
                    __instance.RoleText.gameObject.SetActive(false);
                    __instance.RoleBlurbText.gameObject.SetActive(false);
                    __instance.ourCrewmate.gameObject.SetActive(false);
                })));

                __result = Effects.Sequence(list.ToArray());
                return false;
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowTeam))]
        class BeginPatch
        {
            public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToShow)
            {
                foreach(PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (Game.GameData.data.AllPlayers[player.PlayerId].role.category == Roles.RoleCategory.Impostor)
                    {
                        DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Impostor);
                    }
                    else
                    {
                        DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Crewmate);
                    }
                    Game.GameData.data.AllPlayers[player.PlayerId].role.ReflectRoleEyesight(player.Data.Role);
                }

                //isImpostor = (Game.GameData.data.myData.getGlobalData().role.category == Roles.RoleCategory.Impostor);
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        class BeginCrewmatePatch
        {
            public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
            {
                setupIntroTeamMembers(__instance, ref teamToDisplay);
            }
            public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
            {
                setupIntroTeamText(__instance, ref teamToDisplay);
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

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(IntroCutscene __instance,ref PoolablePlayer __result, ref int i,ref int maxDepth, ref GameData.PlayerInfo pData, ref bool impostorPositioning)
        {
            if (!impostorPositioning) return;

            __result.SetNameColor(Palette.ImpostorRed);
        }
    }

    [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Close))]
    public class CloseSpawnGUIPatch
    {
        public static HashSet<System.Action> Actions = new HashSet<System.Action>();
        public static void Postfix(SpawnInMinigame __instance)
        {
            foreach (var action in Actions)
            {
                action.Invoke();
            }
            Actions.Clear();
        }
    }
}
