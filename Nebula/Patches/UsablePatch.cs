using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nebula.Utilities;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    class KillButtonDoClickPatch
    {
        public static bool Prefix(KillButton __instance)
        {
            if (__instance.isActiveAndEnabled && __instance.currentTarget && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.CanMove)
            {
                Helpers.MurderAttemptResult res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, __instance.currentTarget, Game.PlayerData.PlayerStatus.Dead);
                if (res == Helpers.MurderAttemptResult.BlankKill)
                {
                    PlayerControl.LocalPlayer.killTimer = PlayerControl.GameOptions.KillCooldown;
                }
                __instance.SetTarget(null);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.NextStep))]
    class TaskCompletePatch
    {
        static void Prefix(NormalPlayerTask __instance)
        {
            if (__instance.MaxStep-1 == __instance.TaskStep)
                if (__instance.Owner.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    RPCEventInvoker.CompleteTask(__instance.Owner.PlayerId);
        }
    }

    [HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.Refresh))]
    class SabotageButtonRefreshPatch
    {
        static void Postfix(SabotageButton __instance)
        {
            if (!HudManager.InstanceExists) return;
            if (Game.GameData.data == null) return;
            if (Game.GameData.data.myData.getGlobalData() == null) return;

            if (!Game.GameData.data.myData.getGlobalData().role.canInvokeSabotage)
            {
                __instance.SetDisabled();
            }
        }
    }

    [HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
    public static class SabotageButtonDoClickPatch
    {
        public static bool Prefix(SabotageButton __instance)
        {
            //インポスターなら特段何もしない
            if (PlayerControl.LocalPlayer.Data.Role.TeamType == RoleTeamTypes.Impostor) return true;

            HudManager.Instance.ShowMap((Il2CppSystem.Action<MapBehaviour>)((m) => { m.ShowSabotageMap(); }));
            return false;
        }
    }

    //コミュを直せない役職からミニゲームをブロックする
    [HarmonyPatch(typeof(TuneRadioMinigame), nameof(TuneRadioMinigame.Begin))]
    class CommsMinigameBeginPatch
    {
        static void Postfix(TuneRadioMinigame __instance)
        {
            bool cannotFixSabotage = false;
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixSabotage(PlayerControl.LocalPlayer.PlayerId); });
            if (cannotFixSabotage) __instance.Close();
        }
    }

    //停電を直せない役職からミニゲームをブロックする
    [HarmonyPatch(typeof(SwitchMinigame), nameof(SwitchMinigame.Begin))]
    class LightsMinigameBeginPatch
    {
        static void Postfix(SwitchMinigame __instance)
        {
            bool cannotFixSabotage = false;
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { cannotFixSabotage |= !role.CanFixSabotage(PlayerControl.LocalPlayer.PlayerId); });
            if (cannotFixSabotage) __instance.Close();
        }
    }
}
