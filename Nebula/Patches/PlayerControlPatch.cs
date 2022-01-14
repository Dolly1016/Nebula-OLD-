using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public class PlayerControlPatch
    {
        static private bool CheckTargetable(Vector3 position, Vector3 myPosition, ref float distanceCondition)
        {
            Vector3 vector = position - PlayerControl.LocalPlayer.transform.position;
            float magnitude = vector.magnitude;

            if(magnitude <= distanceCondition && !PhysicsHelpers.AnyNonTriggersBetween(myPosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
            {
                distanceCondition=magnitude;
                return true;
            }
            return false;
        }

        static public PlayerControl SetMyTarget(bool onlyWhiteNames = false, bool targetPlayersInVents = false, List<PlayerControl> untargetablePlayers = null, PlayerControl targetingPlayer = null)
        {
            PlayerControl result = null;
            float num = GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
            if (!ShipStatus.Instance) return result;
            if (targetingPlayer == null) targetingPlayer = PlayerControl.LocalPlayer;
            if (targetingPlayer.Data.IsDead) return result;

            Vector2 truePosition = targetingPlayer.GetTruePosition();
            Il2CppSystem.Collections.Generic.List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                GameData.PlayerInfo playerInfo = allPlayers[i];

                if (PlayerControl.LocalPlayer.PlayerId == playerInfo.PlayerId)
                {
                    continue;
                }

                if (!playerInfo.Disconnected && !playerInfo.IsDead && (!onlyWhiteNames || !(playerInfo.Role.IsImpostor||Game.GameData.data.players[playerInfo.PlayerId].role.deceiveImpostorInNameDisplay)))
                {
                    PlayerControl @object = playerInfo.Object;
                    if (untargetablePlayers != null && untargetablePlayers.Any(x => x == @object))
                    {
                        // if that player is not targetable: skip check
                        continue;
                    }

                    if (@object && (!@object.inVent || targetPlayersInVents))
                    {
                        if(CheckTargetable(@object.GetTruePosition(),truePosition,ref num))
                        {
                            result = @object;
                        }
                    }
                }
            }
            return result;
        }

        static public void SetPlayerOutline(PlayerControl target, Color color)
        {
            if (target == null || target.myRend == null) return;

            target.myRend.material.SetFloat("_Outline", 1f);
            target.myRend.material.SetColor("_OutlineColor", color);
        }

        static public DeadBody SetMyDeadTarget()
        {
            DeadBody result = null;
            float num = GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
            if (!ShipStatus.Instance) return result;

            Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();

            foreach (DeadBody deadBody in Helpers.AllDeadBodies())
            {
                if (!deadBody.bodyRenderer.enabled)
                {
                    continue;
                }

                if (CheckTargetable(deadBody.transform.position, truePosition,ref num) ||
                    CheckTargetable(deadBody.transform.position + new Vector3(0.1f,0.1f), truePosition, ref num))
                {
                    result = deadBody;
                }
            }
            return result;
        }

        static public void SetDeadBodyOutline(DeadBody target, Color color)
        {
            if (target == null || target.bodyRenderer == null) return;

            target.bodyRenderer.material.SetFloat("_Outline", 1f);
            target.bodyRenderer.material.SetColor("_OutlineColor", color);
        }


        static void ResetPlayerOutlines()
        {
            foreach (PlayerControl target in PlayerControl.AllPlayerControls)
            {
                if (target == null || target.myRend == null) continue;

                target.myRend.material.SetFloat("_Outline", 0f);
            }
        }

        static void ResetDeadBodyOutlines()
        {
            foreach (DeadBody deadBody in Helpers.AllDeadBodies())
            {
                if (deadBody == null || deadBody.bodyRenderer == null) continue;

                deadBody.bodyRenderer.material.SetFloat("_Outline", 0f);
            }
        }

        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
            if (Game.GameData.data == null)
            {
                return;
            }
            if (!Game.GameData.data.players.ContainsKey(__instance.PlayerId))
            {
                return;
            }

            if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                ResetPlayerOutlines();
                ResetDeadBodyOutlines();
                Game.GameData.data.players[__instance.PlayerId].role.MyPlayerControlUpdate();
            }

        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static class MurderPlayerPatch
    {
        public static bool resetToCrewmate = false;
        public static bool resetToDead = false;

        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            // キル用の設定にする
            resetToCrewmate = !__instance.Data.Role.IsImpostor;
            resetToDead = __instance.Data.IsDead;
            __instance.Data.Role.TeamType = RoleTeamTypes.Impostor;
            __instance.Data.IsDead = false;
        }

        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            // キル用の設定を元に戻す
            if (resetToCrewmate) __instance.Data.Role.TeamType = RoleTeamTypes.Crewmate;
            if (resetToDead) __instance.Data.IsDead = true;

            if (Game.GameData.data.players[target.PlayerId].role.hasFakeTask)
            {
                target.clearAllTasks();
            }

            //GlobalMethod
            target.GetModData().role.OnMurdered(__instance.PlayerId,target.PlayerId);
            target.GetModData().role.OnDied(target.PlayerId);

            //LocalMethod (自身が死んだとき)
            if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                target.GetModData().role.OnMurdered(__instance.PlayerId);
                target.GetModData().role.OnDied();
            }
            
            Game.GameData.data.players[target.PlayerId].Die(__instance.PlayerId);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
    class PlayerControlSetCoolDownPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] float time)
        {
            if (PlayerControl.GameOptions.KillCooldown <= 0f) return false;
            float multiplier = 1f;
            float addition = 0f;

            //キルクールを設定する
            if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Game.GameData.data.myData.getGlobalData().role.SetKillCoolDown(ref multiplier,ref addition);    
            }

            __instance.killTimer = Mathf.Clamp(time, 0f, PlayerControl.GameOptions.KillCooldown * multiplier + addition);
            DestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(__instance.killTimer, PlayerControl.GameOptions.KillCooldown * multiplier + addition);
            return false;
        }
    }

    [HarmonyPatch(typeof(KillAnimation), nameof(KillAnimation.CoPerformKill))]
    class KillAnimationCoPerformKillPatch
    {
        public static bool hideNextAnimation = true;
        public static void Prefix(KillAnimation __instance, [HarmonyArgument(0)] ref PlayerControl source, [HarmonyArgument(1)] ref PlayerControl target)
        {
            if (hideNextAnimation)
                source = target;
            hideNextAnimation = false;
        }
    }

}
