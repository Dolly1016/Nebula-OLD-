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

        static public PlayerControl SetMyTarget(bool onlyWhiteNames = false, bool targetPlayersInVents = false, List<byte> untargetablePlayers = null, PlayerControl targetingPlayer = null)
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
                    if (untargetablePlayers != null && untargetablePlayers.Any(x => x == @object.PlayerId))
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

        public static void UpdateAllPlayersInfo()
        {
            bool commsActive = false;
            foreach (PlayerTask t in PlayerControl.LocalPlayer.myTasks)
            {
                if (t.TaskType == TaskTypes.FixComms)
                {
                    commsActive = true;
                    break;
                }
            }

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p == PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Transform playerInfoTransform = p.nameText.transform.parent.FindChild("Info");
                    TMPro.TextMeshPro playerInfo = playerInfoTransform != null ? playerInfoTransform.GetComponent<TMPro.TextMeshPro>() : null;
                    if (playerInfo == null)
                    {
                        playerInfo = UnityEngine.Object.Instantiate(p.nameText, p.nameText.transform.parent);
                        playerInfo.fontSize *= 0.75f;
                        playerInfo.gameObject.name = "Info";
                    }

                    // Set the position every time bc it sometimes ends up in the wrong place due to camoflauge
                    playerInfo.transform.localPosition = p.nameText.transform.localPosition + Vector3.up * 0.5f;

                    PlayerVoteArea playerVoteArea = MeetingHud.Instance?.playerStates?.FirstOrDefault(x => x.TargetPlayerId == p.PlayerId);
                    Transform meetingInfoTransform = playerVoteArea != null ? playerVoteArea.NameText.transform.parent.FindChild("Info") : null;
                    TMPro.TextMeshPro meetingInfo = meetingInfoTransform != null ? meetingInfoTransform.GetComponent<TMPro.TextMeshPro>() : null;
                    if (meetingInfo == null && playerVoteArea != null)
                    {
                        meetingInfo = UnityEngine.Object.Instantiate(playerVoteArea.NameText, playerVoteArea.NameText.transform.parent);
                        meetingInfo.transform.localPosition += Vector3.down * 0.10f;
                        meetingInfo.fontSize *= 0.60f;
                        meetingInfo.gameObject.name = "Info";
                    }

                    // Set player name higher to align in middle
                    if (meetingInfo != null && playerVoteArea != null)
                    {
                        var playerName = playerVoteArea.NameText;
                        playerName.transform.localPosition = new Vector3(0.3384f, (0.0311f + 0.0683f), -0.1f);
                    }

                    var (tasksCompleted, tasksTotal) = TasksHandler.taskInfo(p.Data);
                    string roleNames = Helpers.cs(p.GetModData().role.color, Language.Language.GetString("role." + p.GetModData().role.localizeName + ".name"));

                    var completedStr = commsActive ? "?" : tasksCompleted.ToString();
                    string taskInfo = tasksTotal > 0 ? $"<color=#FAD934FF>({completedStr}/{tasksTotal})</color>" : "";

                    string playerInfoText = "";
                    string meetingInfoText = "";
                    if (p == PlayerControl.LocalPlayer)
                    {
                        playerInfoText = $"{roleNames}";
                        if (DestroyableSingleton<TaskPanelBehaviour>.InstanceExists)
                        {
                            TMPro.TextMeshPro tabText = DestroyableSingleton<TaskPanelBehaviour>.Instance.tab.transform.FindChild("TabText_TMP").GetComponent<TMPro.TextMeshPro>();
                            tabText.SetText($"{TranslationController.Instance.GetString(StringNames.Tasks)} {taskInfo}");
                        }
                        meetingInfoText = $"{roleNames} {taskInfo}".Trim();
                    }
                    playerInfoText = $"{roleNames} {taskInfo}".Trim();
                    meetingInfoText = playerInfoText;
                    
                    playerInfo.text = playerInfoText;
                    playerInfo.gameObject.SetActive(p.Visible);
                    if (meetingInfo != null) meetingInfo.text = MeetingHud.Instance.state == MeetingHud.VoteStates.Results ? "" : meetingInfoText;
                }
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
                UpdateAllPlayersInfo();
                ResetPlayerOutlines();
                ResetDeadBodyOutlines();


                Helpers.RoleAction(__instance, (role) =>
                 {
                     role.MyPlayerControlUpdate();
                 });
            }

            //全てのプレイヤーに対して実行
            __instance.GetModData().Speed.Update();
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
            EmergencyPatch.KillUpdate();

            // キル用の設定を元に戻す
            if (resetToCrewmate) __instance.Data.Role.TeamType = RoleTeamTypes.Crewmate;
            if (resetToDead) __instance.Data.IsDead = true;
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
