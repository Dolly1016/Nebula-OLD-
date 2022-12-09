namespace Nebula.Patches;

[HarmonyPatch(typeof(GameData), nameof(GameData.SetTasks))]
public class PlayerControlSetTaskPatch
{
    public static bool Prefix(GameData __instance, [HarmonyArgument(0)] byte playerId, [HarmonyArgument(1)] UnhollowerBaseLib.Il2CppStructArray<byte> taskTypeIds)
    {
        if (playerId != PlayerControl.LocalPlayer.PlayerId) return true;

        GameData.PlayerInfo playerById = __instance.GetPlayerById(playerId);
        if (playerById == null || playerById.Disconnected || !playerById.Object) return false;

        //Ritualモード
        if (Game.GameData.data.GameMode == Module.CustomGameMode.Ritual)
        {
            RitualPatch.SetTasks(__instance, playerById, taskTypeIds);
            return false;
        }


        var initialTasks = new List<GameData.TaskInfo>();
        List<GameData.TaskInfo>? actualTasks = null;

        for (int i = 0; i < taskTypeIds.Length; i++) initialTasks.Add(new GameData.TaskInfo(taskTypeIds[i], (uint)i));

        Game.GameData.data.myData.InitialTasks = initialTasks;

        Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) => r.OnSetTasks(ref initialTasks, ref actualTasks));

        if (actualTasks == null) actualTasks = initialTasks;
        playerById.SetLocalTask(actualTasks);

        //フェイクタスクでなければタスクを持つ
        bool hasCrewmateTask = true;
        bool fakeTaskIsExecutable = false;
        bool isInfiniteTask = false;
        Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) =>
        {
            hasCrewmateTask &= r.HasCrewmateTask(PlayerControl.LocalPlayer.PlayerId);
            fakeTaskIsExecutable |= r.HasExecutableFakeTask(PlayerControl.LocalPlayer.PlayerId);
            isInfiniteTask |= r.HasInfiniteCrewTaskQuota(PlayerControl.LocalPlayer.PlayerId);
        });
        if (fakeTaskIsExecutable || hasCrewmateTask) RPCEventInvoker.SetTasks(PlayerControl.LocalPlayer.PlayerId, actualTasks.Count, hasCrewmateTask, isInfiniteTask);

        return false;
    }
}

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
        if (Game.GameData.data.GetPlayerData(__instance.PlayerId) == null)
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


        foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
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
                    if (onlyWhiteNames && (player.Role.IsImpostor || Game.GameData.data.playersArray[player.PlayerId].role.DeceiveImpostorInNameDisplay)) return false;
                    if (player.Object.inVent && !targetPlayersInVents) return false;
                    if (untargetablePlayers != null && untargetablePlayers.Any(x => x == player.Object.PlayerId)) return false;
                    return true;
                }, targetingPlayer);
    }

    static public PlayerControl? SetMyTarget(bool onlyWhiteNames = false, bool targetPlayersInVents = false, List<byte> untargetablePlayers = null, PlayerControl targetingPlayer = null)
    {
        return SetMyTarget(GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.Cast<NormalGameOptionsV07>().KillDistance, 0, 2)],
                onlyWhiteNames, targetPlayersInVents, untargetablePlayers, targetingPlayer);
    }

    static public PlayerControl? SetMyTarget(System.Predicate<GameData.PlayerInfo> targetablePlayers, PlayerControl targetingPlayer = null)
    {
        return SetMyTarget(GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.Cast<NormalGameOptionsV07>().KillDistance, 0, 2)],
            targetablePlayers);
    }

    static public PlayerControl? SetMyTarget(float range, System.Predicate<GameData.PlayerInfo> targetablePlayers, PlayerControl? targetingPlayer = null)
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

            if (playerInfo == null || (PlayerControl.LocalPlayer.PlayerId == playerInfo.PlayerId) || (playerInfo.Object == null))
                continue;


            if (playerInfo.GetModData().Attribute.HasAttribute(Game.PlayerAttribute.Invisible)) continue;
            if (playerInfo.GetModData().Property.UnderTheFloor) continue;

            if (!playerInfo.Disconnected && !playerInfo.IsDead)
            {
                PlayerControl @object = playerInfo.Object;
                if (@object && targetablePlayers.Invoke(playerInfo))
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

    static public void SetPlayerOutline(PlayerControl? target, Color color)
    {
        if (target == null || target.cosmetics.currentBodySprite.BodySprite == null) return;

        target.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 1f);
        target.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", color);
    }

    static public DeadBody? SetMyDeadTarget() => SetMyDeadTarget(GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.Cast<NormalGameOptionsV07>().KillDistance, 0, 2)]);

    static public DeadBody? SetMyDeadTarget(float num)
    {
        DeadBody? result = null;
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
            foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
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
        foreach (PlayerControl target in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            if (target == null || target.cosmetics.currentBodySprite.BodySprite == null) continue;

            target.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 0f);
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

        foreach (PlayerControl p in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            try
            {
                var data = p.GetModData();
                if (p == PlayerControl.LocalPlayer || data.RoleInfo != "" || Game.GameData.data.myData.CanSeeEveryoneInfo)
                {
                    Transform playerInfoTransform = p.cosmetics.nameText.transform.FindChild("Info");
                    TMPro.TextMeshPro playerInfo = playerInfoTransform != null ? playerInfoTransform.GetComponent<TMPro.TextMeshPro>() : null;
                    if (playerInfo == null)
                    {
                        playerInfo = UnityEngine.Object.Instantiate(p.cosmetics.nameText, p.cosmetics.nameText.transform);
                        playerInfo.fontSize *= 0.75f;
                        playerInfo.gameObject.name = "Info";
                        playerInfo.enabled = true;
                    }

                    // Set the position every time bc it sometimes ends up in the wrong place due to camoflauge
                    playerInfo.transform.localPosition = new Vector3(0f, 0.5f / 2.8f, 0f);
                    playerInfo.transform.localScale = new Vector3(1, 1, 0f);

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

                    if (Game.GameData.data.myData.CanSeeEveryoneInfo || data.RoleInfo == "")
                    {
                        if (p.Data.IsDead && data.role.CanHaveGhostRole && data.ghostRole != null)
                            roleNames = Helpers.cs(data.ghostRole.Color, Language.Language.GetString("role." + data.ghostRole.LocalizeName + ".name"));
                        else
                            roleNames = Helpers.cs(data.role.Color, Language.Language.GetString("role." + data.role.LocalizeName + ".name"));

                        Helpers.RoleAction(p.PlayerId, (role) => { role.EditDisplayRoleName(p.PlayerId, ref roleNames, false); });
                    }
                    else
                        //カモフラージュ中は表示しない
                        roleNames = p.GetModData().currentName.Length == 0 ? "" : p.GetModData().RoleInfo;

                    var completedStr = commsActive ? "?" : tasksCompleted.ToString();
                    string taskInfo = "";

                    if (Game.GameModeProperty.GetProperty(Game.GameData.data.GameMode).CountTasks)
                    {
                        if (p == PlayerControl.LocalPlayer || Game.GameData.data.myData.CanSeeEveryoneInfo)
                        {
                            bool hasFakeTask = false;
                            Helpers.RoleAction(p.PlayerId, (role) =>
                            {
                                hasFakeTask |= !role.HasCrewmateTask(p.PlayerId);
                            });

                            if (hasFakeTask)
                                taskInfo = tasksTotal > 0 ? $"<color=#868686FF>({completedStr}/{tasksTotal})</color>" : "";
                            else
                                taskInfo = tasksTotal > 0 ? $"<color=#FAD934FF>({completedStr}/{tasksTotal})</color>" : "";
                        }
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

                    if (meetingInfo != null) meetingInfo.text = MeetingHud.Instance.state == MeetingHud.VoteStates.Results ? "" : meetingInfoText;
                }
            }
            catch (NullReferenceException exp)
            {
                continue;
            }
        }
    }


    public static void UpdatePlayerVisibility(PlayerControl player)
    {
        var data = player.GetModData();
        if (data == null) return;

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

        if (player.cosmetics.currentBodySprite.BodySprite != null)
            player.cosmetics.currentBodySprite.BodySprite.color = data.TransColor;

        if (player.cosmetics.skin.layer != null)
            player.cosmetics.skin.layer.color = data.TransColor;

        if (player.cosmetics.hat)
        {
            if (player.cosmetics.hat.FrontLayer != null)
                player.cosmetics.hat.FrontLayer.color = data.TransColor;
            if (player.cosmetics.hat.BackLayer != null)
                player.cosmetics.hat.BackLayer.color = data.TransColor;
        }

        if (player.cosmetics.currentPet)
        {
            if (player.cosmetics.currentPet.rend != null)
                player.cosmetics.currentPet.rend.color = data.TransColor;

            if (player.cosmetics.currentPet.shadowRend != null)
                player.cosmetics.currentPet.shadowRend.color = data.TransColor;
        }

        if (player.cosmetics.visor != null)
            player.cosmetics.visor.Image.color = data.TransColor;

    }


    public static void Postfix(PlayerControl __instance)
    {
        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
        if (Game.GameData.data == null)
        {
            return;
        }
        var pData = __instance.GetModData();
        if (pData == null) return;

        //全員に対して実行
        pData.role.GlobalUpdate(__instance.PlayerId);

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

        pData.Speed.Update();
        pData.Attribute.Update();
    }

}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
class PlayerPhysicsHandleAnimationPatch
{
    public static bool Prefix(PlayerPhysics __instance)
    {
        var currentAnim = __instance.Animations.group.SpriteAnimator.m_currAnim;
        return currentAnim != __instance.Animations.group.ExitVentAnim && currentAnim != __instance.Animations.group.EnterVentAnim;
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.HandleDisconnect), typeof(PlayerControl), typeof(DisconnectReasons))]
class PlayerDisconnectPatch
{
    public static void Postfix(GameData __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        try
        {
            if (!AmongUsClient.Instance.IsGameStarted) return;
            if (Game.GameData.data == null) return;
            if (player.GetModData() == null) return;
            player.GetModData().Die(Game.PlayerData.PlayerStatus.Disconnected);
        }
        catch { }
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
        try
        {
            if (Game.GameData.data == null) return true;

            if (GameOptionsManager.Instance.CurrentGameOptions.Cast<NormalGameOptionsV07>().KillCooldown <= 0f) return false;
            float multiplier = 1f;
            float addition = 0f;

            //キルクールを設定する
            if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Game.GameData.data.myData.getGlobalData().role.SetKillCoolDown(ref multiplier, ref addition);
            }

            __instance.killTimer = Mathf.Clamp(time, 0f, GameOptionsManager.Instance.CurrentGameOptions.Cast<NormalGameOptionsV07>().KillCooldown * multiplier + addition);
            HudManager.Instance.KillButton.SetCoolDown(__instance.killTimer, GameOptionsManager.Instance.CurrentGameOptions.Cast<NormalGameOptionsV07>().KillCooldown * multiplier + addition);
            return false;
        }
        catch (NullReferenceException excep) { return true; }
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
        GameData.Instance.RecomputeTaskCounts();

        if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => role.OnTaskComplete());
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
            var data = Game.GameData.data.GetPlayerData(player.PlayerId);
            if (data != null)
            {
                data.Speed.Reflect();
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
            if (Game.GameData.data.AllPlayers.Count > 0)
            {
                Game.GameData.data.myData.getGlobalData().Speed.Reflect();
            }
        }
    }

}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
class PlayerCanMovePatch
{
    public static void Postfix(PlayerControl __instance, ref bool __result)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (!__result) return;

        __result &= HudManager.Instance.PlayerCam.Target == PlayerControl.LocalPlayer;
    }
}


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.IsKillTimerEnabled), MethodType.Getter)]
class PlayerIsKillTimerEnabledPatch
{
    public static void Postfix(PlayerControl __instance, ref bool __result)
    {
        __result &= (!MapBehaviour.Instance || !MapBehaviour.Instance.IsOpenStopped);

        if (__result)
        {
            if (Minigame.Instance)
            {
                if (CustomOptionHolder.CoolDownOption.getBool())
                {
                    if (CustomOptionHolder.KillCoolDownProceedIgnoringBlackOutGame.getBool() && Minigame.Instance.TryCast<SwitchMinigame>())return;
                    if (CustomOptionHolder.KillCoolDownProceedIgnoringDoorGame.getBool() && Minigame.Instance.TryCast<IDoorMinigame>()!=null) return;
                    if (CustomOptionHolder.KillCoolDownProceedIgnoringSecurityCamera.getBool() && (Minigame.Instance.TryCast<PlanetSurveillanceMinigame>()|| Minigame.Instance.TryCast<SurveillanceMinigame>())) return;
                }
                __result = false;
            }
        }
    }
}