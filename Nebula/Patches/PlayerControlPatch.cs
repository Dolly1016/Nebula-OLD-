using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace Nebula.Patches
{

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetHatAndVisorAlpha))]
    public class PlayerControlSetAlphaPatch
    {
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

            PlayerControlPatch.UpdatePlayerVisibility(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public class PlayerControlPatch
    {
        static private bool CheckTargetable(Vector2 position, Vector2 myPosition, ref float distanceCondition)
        {
            Vector2 vector = (Vector2)position - myPosition;
            float magnitude = vector.magnitude;

            if (magnitude <= distanceCondition && !PhysicsHelpers.AnyNonTriggersBetween(myPosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
            {
                distanceCondition = magnitude;
                return true;
            }
            return false;
        }

        static public PlayerControl GetTarget(Vector3 position, float distance, bool onlyWhiteNames = false, List<byte>? untargetablePlayers = null)
        {
            PlayerControl result = null;
            float num;
            if (!ShipStatus.Instance) return result;


            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (onlyWhiteNames && (player.Data.Role.IsImpostor || player.GetModData().role.DeceiveImpostorInNameDisplay)) continue;
                if (untargetablePlayers != null && untargetablePlayers.Contains(player.PlayerId)) continue;

                num = player.transform.position.Distance(position);
                if (distance > num)
                {
                    result = player;
                    distance = num;
                }
            }

            return result;
        }

        static public PlayerControl? SetMyTarget(float range, bool onlyWhiteNames = false, bool targetPlayersInVents = false, List<byte>? untargetablePlayers = null, PlayerControl? targetingPlayer = null)
        {
            return SetMyTarget(range,
                    (player) =>
                    {
                        if (onlyWhiteNames && (player.Role.IsImpostor || Game.GameData.data.players[player.PlayerId].role.DeceiveImpostorInNameDisplay)) return false;
                        if (player.Object.inVent && !targetPlayersInVents) return false;
                        if (untargetablePlayers != null && untargetablePlayers.Any(x => x == player.Object.PlayerId)) return false;
                        return true;
                    }, targetingPlayer);
        }

        static public PlayerControl? SetMyTarget(bool onlyWhiteNames = false, bool targetPlayersInVents = false, List<byte> untargetablePlayers = null, PlayerControl targetingPlayer = null)
        {
            return SetMyTarget(GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)],
                    onlyWhiteNames, targetPlayersInVents, untargetablePlayers, targetingPlayer);
        }

        static public PlayerControl? SetMyTarget(System.Predicate<GameData.PlayerInfo> untargetablePlayers, PlayerControl targetingPlayer = null)
        {
            return SetMyTarget(GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)],
                untargetablePlayers);
        }

        static public PlayerControl? SetMyTarget(float range, System.Predicate<GameData.PlayerInfo> untargetablePlayers, PlayerControl? targetingPlayer = null)
        {
            PlayerControl result = null;
            float num = range;
            if (!ShipStatus.Instance) return result;
            if (targetingPlayer == null) targetingPlayer = PlayerControl.LocalPlayer;
            if (targetingPlayer.Data.IsDead) return result;

            Vector2 truePosition = targetingPlayer.GetTruePosition();
            Il2CppSystem.Collections.Generic.List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                GameData.PlayerInfo playerInfo = allPlayers[i];

                if (playerInfo==null || (PlayerControl.LocalPlayer.PlayerId == playerInfo.PlayerId) || (playerInfo.Object==null))
                    continue;
                

                if (playerInfo.GetModData().Attribute.HasAttribute(Game.PlayerAttribute.Invisible)) continue;
                if (playerInfo.GetModData().Property.UnderTheFloor) continue;

                if (!playerInfo.Disconnected && !playerInfo.IsDead)
                {
                    PlayerControl @object = playerInfo.Object;
                    if (@object && untargetablePlayers.Invoke(playerInfo))
                    {
                        if (CheckTargetable(@object.GetTruePosition(), truePosition, ref num))
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
            if (target == null || target.MyRend == null) return;

            target.MyRend.material.SetFloat("_Outline", 1f);
            target.MyRend.material.SetColor("_OutlineColor", color);
        }

        static public DeadBody SetMyDeadTarget()
        {
            DeadBody result = null;
            float num = GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
            if (!ShipStatus.Instance) return result;

            Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();

            bool invalidFlag;
            foreach (DeadBody deadBody in Helpers.AllDeadBodies())
            {
                if (!deadBody.bodyRenderer.enabled)
                {
                    continue;
                }

                invalidFlag = false;
                foreach (Game.PlayerData data in Game.GameData.data.players.Values)
                {
                    if (data.dragPlayerId == deadBody.ParentId) { invalidFlag = true; break; }
                }
                if (invalidFlag) continue;

                if (CheckTargetable(deadBody.transform.position, truePosition, ref num) ||
                    CheckTargetable(deadBody.transform.position + new Vector3(0.1f, 0.1f), truePosition, ref num))
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
                if (target == null || target.MyRend == null) continue;

                target.MyRend.material.SetFloat("_Outline", 0f);
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
                try
                {
                    if (p == PlayerControl.LocalPlayer || p.GetModData().RoleInfo != "" || Game.GameData.data.myData.CanSeeEveryoneInfo)
                    {
                        Transform playerInfoTransform = p.nameText.transform.parent.FindChild("Info");
                        TMPro.TextMeshPro playerInfo = playerInfoTransform != null ? playerInfoTransform.GetComponent<TMPro.TextMeshPro>() : null;
                        if (playerInfo == null)
                        {
                            playerInfo = UnityEngine.Object.Instantiate(p.nameText, p.nameText.transform.parent);
                            playerInfo.fontSize *= 0.75f;
                            playerInfo.gameObject.name = "Info";
                            playerInfo.enabled = true;
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
                        string roleNames;

                        if (Game.GameData.data.myData.CanSeeEveryoneInfo || p.GetModData().RoleInfo == "")
                            roleNames = Helpers.cs(p.GetModData().role.Color, Language.Language.GetString("role." + p.GetModData().role.LocalizeName + ".name"));
                        else
                            //カモフラージュ中は表示しない
                            roleNames = p.GetModData().currentName.Length == 0 ? "" : p.GetModData().RoleInfo;

                        var completedStr = commsActive ? "?" : tasksCompleted.ToString();
                        string taskInfo = "";
                        if (p == PlayerControl.LocalPlayer || Game.GameData.data.myData.CanSeeEveryoneInfo)
                        {
                            if (p.GetModData().role.HasFakeTask)
                                taskInfo = tasksTotal > 0 ? $"<color=#868686FF>({completedStr}/{tasksTotal})</color>" : "";
                            else
                                taskInfo = tasksTotal > 0 ? $"<color=#FAD934FF>({completedStr}/{tasksTotal})</color>" : "";
                        }

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
                }catch(NullReferenceException exp)
                {
                    continue;
                }
            }
        }


        public static void UpdatePlayerVisibility(PlayerControl player)
        {
            var data = player.GetModData();
            float alpha = data.TransColor.a;
            if (data.Attribute.HasAttribute(Game.PlayerAttribute.Invisible))
                alpha -= 0.75f * Time.deltaTime;
            else
                alpha += 0.75f * Time.deltaTime;

            float min = 0f, max = 1f;
            if (player == PlayerControl.LocalPlayer || Game.GameData.data.myData.CanSeeEveryoneInfo) min = 0.25f;
            alpha = Mathf.Clamp(alpha, min, max);
            if (alpha != data.TransColor.a)
            {
                data.TransColor = new Color(1f, 1f, 1f, alpha);
            }

            if (player.MyPhysics?.rend != null)
                player.MyPhysics.rend.color = data.TransColor;

            if (player.MyPhysics?.Skin?.layer != null)
                player.MyPhysics.Skin.layer.color = data.TransColor;

            if (player.HatRenderer != null)
                player.HatRenderer.color = data.TransColor;

            if (player.CurrentPet?.rend != null)
                player.CurrentPet.rend.color = data.TransColor;

            if (player.CurrentPet?.shadowRend != null)
                player.CurrentPet.shadowRend.color = data.TransColor;

            if (player.VisorSlot != null)
                player.VisorSlot.color = data.TransColor;

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

            //全員に対して実行
            __instance.GetModData().role.GlobalUpdate(__instance.PlayerId);
            UpdatePlayerVisibility(__instance);

            if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Objects.CustomObject.Update();

                UpdateAllPlayersInfo();
                ResetPlayerOutlines();
                ResetDeadBodyOutlines();

                Helpers.RoleAction(__instance, (role) =>
                 {
                     role.MyPlayerControlUpdate();
                 });
            }

            __instance.GetModData().Speed.Update();
            __instance.GetModData().Attribute.Update();
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
    class PlayerPhysicsHandleAnimationPatch
    {
        public static bool Prefix(PlayerPhysics __instance)
        {
            if (
                __instance.Animator.IsPlaying(__instance.CurrentAnimationGroup.ExitVentAnim) ||
                __instance.Animator.IsPlaying(__instance.CurrentAnimationGroup.EnterVentAnim))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.HandleDisconnect),typeof(PlayerControl),typeof(DisconnectReasons))]
    class PlayerDisconnectPatch
    {
        public static void Postfix(GameData __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            if (!AmongUsClient.Instance.IsGameStarted) return;
            if (Game.GameData.data == null) return;
            if (player.GetModData() == null) return;
            player.GetModData().Die(Game.PlayerData.PlayerStatus.Disconnected);
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
                Game.GameData.data.myData.getGlobalData().role.SetKillCoolDown(ref multiplier, ref addition);
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

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    public static class CompleteTaskPatch
    {

        public static void Postfix(PlayerControl __instance)
        {
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId,(role)=>role.OnTaskComplete());
        }
    }

    //ベント移動その他
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.WalkPlayerTo))]
    class WalkPatch
    {
        public static void Prefix(PlayerPhysics __instance)
        {
            if (Helpers.HasModData(__instance.myPlayer.PlayerId))
            {
                __instance.myPlayer.GetModData().Speed.Reflect();
            }
            else
            {
                __instance.Speed = 2.5f;
            }
            if (__instance.Speed < 0f) __instance.Speed *= -1f;
        }

        public static void Postfix(PlayerPhysics __instance)
        {
            if (Helpers.HasModData(__instance.myPlayer.PlayerId))
            {
                __instance.myPlayer.GetModData().Speed.Reflect();
            }
            else
            {
                __instance.Speed = 2.5f;
            }
        }
    }

    //入力による移動
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    class MyWalkPatch
    {
        public static void Prefix(PlayerPhysics __instance)
        {
            if (Helpers.HasModData(__instance.myPlayer.PlayerId))
            {
                __instance.myPlayer.GetModData().Speed.Reflect();
            }
            else
            {
                __instance.Speed = 2.5f;
            }
        }
    }

    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
    class WalkMagnitudePatch
    {
        public static void Prefix(CustomNetworkTransform __instance)
        {
            if (Game.GameData.data != null)
            {
                var player = __instance.gameObject.GetComponent<PlayerControl>();
                if (Game.GameData.data.players.ContainsKey(player.PlayerId))
                {
                    Game.GameData.data.players[player.PlayerId].Speed.Reflect();
                    PlayerControl.LocalPlayer.MyPhysics.Speed = Helpers.playerById(player.PlayerId).MyPhysics.Speed;
                }
                else
                {
                    PlayerControl.LocalPlayer.MyPhysics.Speed = 2.5f;
                }


               
                if (PlayerControl.LocalPlayer.MyPhysics.Speed < 0f) PlayerControl.LocalPlayer.MyPhysics.Speed *= -1f;
            }
        }

        public static void Postfix(CustomNetworkTransform __instance)
        {
            if (Game.GameData.data != null)
            {
                if (Game.GameData.data.players.Count > 0)
                {
                    Game.GameData.data.myData.getGlobalData().Speed.Reflect();
                }
            }
        }
    }
}
