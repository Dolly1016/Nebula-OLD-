using Hazel;
using Nebula.Module;
using UnityEngine;

namespace Nebula;

public enum CustomData
{
    CurrentTask
}

public enum CustomRPC
{
    // Main Controls

    ResetVaribles = 60,
    RequireCustomData,
    SendCustomData,
    SetRandomMap,
    VersionHandshake,
    Synchronize,
    SetMyColor,
    ShareColor,
    SynchronizeTimer,
    UpdatePlayerControl,
    WinTrigger,
    ShareOptions,
    SendPreMeetingPosition,
    SetPlayerStatus,
    SetRoles,
    SetExtraRole,
    UnsetExtraRole,
    SwapExtraRole,
    ChangeExtraRole,
    SetGhostRole,
    PlayStaticSound,
    PlayDynamicSound,
    UncheckedMurderPlayer,
    UncheckedExilePlayer,
    UncheckedCmdReportDeadBody,
    Guard,
    CloseUpKill,
    UpdateRoleData,
    UpdateExtraRoleData,
    GlobalEvent,
    DragAndDropPlayer,
    ChangeRole,
    ImmediatelyChangeRole,
    SwapRole,
    RevivePlayer,
    EmitSpeedFactor,
    EmitPlayerAttributeFactor,
    CleanDeadBody,
    SnapToDeadBody,
    FixLights,
    RequireUniqueRPC,
    SetTasks,
    ChangeTasks,
    RefreshTasks,
    CompleteTask,
    ExemptAllTask,
    ObjectInstantiate,
    ObjectUpdate,
    ObjectDestroy,
    CountDownMessage,
    UpdateRestrictTimer,
    UndergroundAction,
    DeathGuage,
    UpdatePlayerVisibility,
    EditCoolDown,
    KillGuard,

    // Role functionality

    SealVent = 129,
    MultipleVote,
    SniperSettleRifle,
    SniperShot,
    RaiderSettleAxe,
    RaiderThrow,
    Morph,
    MorphCancel,
    CreateSidekick,
    DisturberInvoke,
    UpdatePlayersIconInfo,

    InitializeRitualData,
    RitualSharePerks,
    RitualUpdate,
    DecoySwap,
    Paint,
    Poltergeist,
    InstantiateDeadBody
}

//RPCを受け取ったときのイベント
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
class RPCHandlerPatch
{
    static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        byte packetId = callId;
        int length;
        switch (packetId)
        {
            case 2:
                //標準の設定同期
                GameOptionsDataPatch.dirtyFlag = true;
                break;
            case (byte)CustomRPC.ResetVaribles:
                RPCEvents.ResetVaribles();
                break;
            case (byte)CustomRPC.RequireCustomData:
                if (reader.ReadByte() == PlayerControl.LocalPlayer.PlayerId)
                {
                    RPCEventInvoker.SendCustomData((CustomData)reader.ReadByte());
                }
                break;
            case (byte)CustomRPC.SendCustomData:
                Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) => r.OnReceiveCustomData(reader.ReadByte(), (CustomData)reader.ReadByte(), reader));
                break;
            case (byte)CustomRPC.SetRandomMap:
                RPCEvents.SetRandomMap(reader.ReadByte());
                break;
            case (byte)CustomRPC.VersionHandshake:
                length = reader.ReadInt32();
                byte[] version = new byte[length];
                for (byte i = 0; i < length; i++)
                {
                    version[i] = reader.ReadByte();
                }
                int clientId = reader.ReadPackedInt32();
                RPCEvents.VersionHandshake(version, new Guid(reader.ReadBytes(16)), clientId);
                break;
            case (byte)CustomRPC.SetMyColor:
                RPCEvents.SetMyColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 1f), new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 1f));
                break;
            case (byte)CustomRPC.ShareColor:
                RPCEvents.ShareColor(reader.ReadByte(), new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 1f), reader.ReadSingle(), reader.ReadByte(), reader.ReadByte(), new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), 1f), reader.ReadSingle(), reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.Synchronize:
                RPCEvents.Synchronize(reader.ReadByte(), reader.ReadInt32());
                break;
            case (byte)CustomRPC.SynchronizeTimer:
                RPCEvents.SynchronizeTimer(reader.ReadSingle());
                break;
            case (byte)CustomRPC.SetPlayerStatus:
                RPCEvents.SetPlayerStatus(reader.ReadByte(), Game.PlayerData.PlayerStatus.GetStatusById(reader.ReadByte()));
                break;
            case (byte)CustomRPC.UpdatePlayerControl:
                RPCEvents.UpdatePlayerControl(reader.ReadByte(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.SendPreMeetingPosition:
                RPCEvents.SendPreMeetingPosition(reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                break;
            case (byte)CustomRPC.WinTrigger:
                RPCEvents.WinTrigger(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.ShareOptions:
                RPCEvents.ShareOptions((int)reader.ReadPackedUInt32(), reader);
                break;
            case (byte)CustomRPC.SetRoles:
                int num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    RPCEvents.SetRole(reader.ReadByte(), Roles.Role.GetRoleById(reader.ReadByte()), reader.ReadInt32(), reader.ReadInt32());
                }
                num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    RPCEvents.SetExtraRole(reader.ReadByte(), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64());
                }
                break;
            case (byte)CustomRPC.SetExtraRole:
                RPCEvents.SetExtraRole(reader.ReadByte(), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64());
                break;
            case (byte)CustomRPC.UnsetExtraRole:
                RPCEvents.UnsetExtraRole(Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadByte(), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.SwapExtraRole:
                RPCEvents.SwapExtraRole(Helpers.playerById(reader.ReadByte()), Helpers.playerById(reader.ReadByte()), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.ChangeExtraRole:
                byte removeRole = reader.ReadByte();
                RPCEvents.ChangeExtraRole(removeRole == byte.MaxValue ? null : Roles.ExtraRole.GetRoleById(removeRole), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64(), reader.ReadByte());
                break;
            case (byte)CustomRPC.SetGhostRole:
                RPCEvents.SetGhostRole(reader.ReadByte(), Roles.GhostRole.GetRoleById(reader.ReadByte()));
                break;
            case (byte)CustomRPC.PlayStaticSound:
                RPCEvents.PlayStaticSound((Module.AudioAsset)reader.ReadByte());
                break;
            case (byte)CustomRPC.PlayDynamicSound:
                RPCEvents.PlayDynamicSound(new Vector2(reader.ReadSingle(), reader.ReadSingle()), (Module.AudioAsset)reader.ReadByte(), reader.ReadSingle(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.UncheckedMurderPlayer:
                RPCEvents.UncheckedMurderPlayer(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.UncheckedExilePlayer:
                RPCEvents.UncheckedExilePlayer(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.UncheckedCmdReportDeadBody:
                RPCEvents.UncheckedCmdReportDeadBody(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.Guard:
                RPCEvents.Guard(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.CloseUpKill:
                RPCEvents.CloseUpKill(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.UpdateRoleData:
                RPCEvents.UpdateRoleData(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                break;
            case (byte)CustomRPC.UpdateExtraRoleData:
                RPCEvents.UpdateExtraRoleData(reader.ReadByte(), reader.ReadByte(), reader.ReadUInt64());
                break;
            case (byte)CustomRPC.GlobalEvent:
                RPCEvents.GlobalEvent(reader.ReadByte(), reader.ReadSingle(), reader.ReadUInt64());
                break;
            case (byte)CustomRPC.DragAndDropPlayer:
                RPCEvents.DragAndDropPlayer(reader.ReadByte(), reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                break;
            case (byte)CustomRPC.ChangeRole:
                RPCEvents.ChangeRole(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.ImmediatelyChangeRole:
                byte playerId = reader.ReadByte();
                byte roleId = reader.ReadByte();
                int exRoles = reader.ReadInt32();
                var exRoleTuples = new List<Tuple<Tuple<Roles.ExtraRole, ulong>, bool>>();
                for (int i = 0; i < exRoles; i++)
                {
                    exRoleTuples.Add(new Tuple<Tuple<Roles.ExtraRole, ulong>, bool>(
                        new Tuple<Roles.ExtraRole, ulong>(Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64()), reader.ReadBoolean()
                        ));
                }
                RPCEvents.ImmediatelyChangeRole(playerId, roleId, exRoleTuples.ToArray());
                break;
            case (byte)CustomRPC.SwapRole:
                RPCEvents.SwapRole(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.RevivePlayer:
                RPCEvents.RevivePlayer(reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()), reader.ReadBoolean(), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.EmitSpeedFactor:
                RPCEvents.EmitSpeedFactor(reader.ReadByte(), new Game.SpeedFactor(reader.ReadBoolean(), reader.ReadByte(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean()));
                break;
            case (byte)CustomRPC.EmitPlayerAttributeFactor:
                RPCEvents.EmitPlayerAttributeFactor(reader.ReadByte(), new Game.PlayerAttributeFactor(Game.PlayerAttribute.AllAttributes[reader.ReadByte()], reader.ReadBoolean(), reader.ReadSingle(), reader.ReadByte(), reader.ReadBoolean()));
                break;
            case (byte)CustomRPC.FixLights:
                RPCEvents.FixLights();
                break;
            case (byte)CustomRPC.RequireUniqueRPC:
                RPCEvents.RequireUniqueRPC(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.SetTasks:
                RPCEvents.SetTasks(reader.ReadByte(), reader.ReadInt32(), reader.ReadBoolean(), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.ChangeTasks:
                RPCEvents.ChangeTasks(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                break;
            case (byte)CustomRPC.RefreshTasks:
                RPCEvents.RefreshTasks(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                break;
            case (byte)CustomRPC.CompleteTask:
                RPCEvents.CompleteTask(reader.ReadByte());
                break;
            case (byte)CustomRPC.ExemptAllTask:
                RPCEvents.ExemptAllTask(reader.ReadByte());
                break;
            case (byte)CustomRPC.ObjectInstantiate:
                RPCEvents.ObjectInstantiate(reader.ReadByte(), reader.ReadByte(), reader.ReadUInt64(), reader.ReadSingle(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.ObjectUpdate:
                RPCEvents.ObjectUpdate(reader.ReadUInt64(), reader.ReadInt32());
                break;
            case (byte)CustomRPC.ObjectDestroy:
                RPCEvents.ObjectDestroy(reader.ReadUInt32());
                break;
            case (byte)CustomRPC.CleanDeadBody:
                RPCEvents.CleanDeadBody(reader.ReadByte());
                break;
            case (byte)CustomRPC.SnapToDeadBody:
                RPCEvents.SnapToDeadBody(reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                break;
            case (byte)CustomRPC.CountDownMessage:
                RPCEvents.CountDownMessage(reader.ReadByte());
                break;
            case (byte)CustomRPC.UpdateRestrictTimer:
                RPCEvents.UpdateRestrictTimer(reader.ReadByte(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.UndergroundAction:
                RPCEvents.UndergroundAction(reader.ReadByte(), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.DeathGuage:
                RPCEvents.DeathGuage(reader.ReadByte(), reader.ReadByte(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.EditCoolDown:
                RPCEvents.EditCoolDown((Roles.CoolDownType)reader.ReadByte(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.KillGuard:
                RPCEvents.KillGuard(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                break;

            case (byte)CustomRPC.SealVent:
                RPCEvents.SealVent(reader.ReadByte(), reader.ReadInt32());
                break;
            case (byte)CustomRPC.MultipleVote:
                RPCEvents.MultipleVote(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.SniperSettleRifle:
                RPCEvents.SniperSettleRifle(reader.ReadByte());
                break;
            case (byte)CustomRPC.SniperShot:
                RPCEvents.SniperShot(reader.ReadByte());
                break;
            case (byte)CustomRPC.RaiderSettleAxe:
                RPCEvents.RaiderSettleAxe(reader.ReadByte());
                break;
            case (byte)CustomRPC.RaiderThrow:
                RPCEvents.RaiderThrow(reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()), reader.ReadSingle());
                break;
            case (byte)CustomRPC.Morph:
                RPCEvents.Morph(reader.ReadByte(), new Game.PlayerData.PlayerOutfitData(reader));
                break;
            case (byte)CustomRPC.MorphCancel:
                RPCEvents.MorphCancel(reader.ReadByte());
                break;
            case (byte)CustomRPC.CreateSidekick:
                RPCEvents.CreateSidekick(reader.ReadByte(), reader.ReadByte());
                break;
            case (byte)CustomRPC.DisturberInvoke:
                RPCEvents.DisturberInvoke(reader.ReadByte(), reader.ReadUInt64(), reader.ReadUInt64());
                break;
            case (byte)CustomRPC.UpdatePlayerVisibility:
                RPCEvents.UpdatePlayerVisibility(reader.ReadByte(), reader.ReadBoolean());
                break;
            case (byte)CustomRPC.UpdatePlayersIconInfo:
                RPCEvents.UpdatePlayersIconInfo(reader);
                break;

            case (byte)CustomRPC.InitializeRitualData:
                RPCEvents.InitializeRitualData(reader);
                break;
            case (byte)CustomRPC.RitualSharePerks:
                RPCEvents.RitualSharePerks(reader.ReadByte(), new int[] { reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32() });
                break;
            case (byte)CustomRPC.RitualUpdate:
                RPCEvents.RitualUpdate(reader);
                break;
            case (byte)CustomRPC.DecoySwap:
                RPCEvents.DecoySwap(Helpers.playerById(reader.ReadByte()), Objects.CustomObject.GetObject(reader.ReadUInt64()), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                break;
            case (byte)CustomRPC.Paint:
                RPCEvents.Paint(Helpers.playerById(reader.ReadByte()), new Game.PlayerData.PlayerOutfitData(reader));
                break;
            case (byte)CustomRPC.Poltergeist:
                RPCEvents.Poltergeist(reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                break;
            case (byte)CustomRPC.InstantiateDeadBody:
                RPCEvents.InstantiateDeadBody(reader.ReadByte(), new Vector3(reader.ReadSingle(), reader.ReadSingle()));
                break;
        }
    }
}

static class RPCEvents
{
    public static void ResetVaribles()
    {
        Game.GameData.Initialize();
        MetaDialog.Initialize();
        Events.GlobalEvent.Initialize();
        Events.LocalEvent.Initialize();
        Events.Schedule.Initialize();
        Objects.CustomMessage.Initialize();
        Objects.CustomObject.Initialize();
        Objects.EffectCircle.Initialize();
        Patches.MeetingHudPatch.Initialize();
        Patches.EmergencyPatch.Initialize();
        Objects.Ghost.Initialize();
        Objects.SoundPlayer.Initialize();
        Patches.LightPatch.Initialize();
    }

    public static void SetMyColor(byte playerId, byte hue,byte dis,Color mainColor,Color shadowColor)
    {
        DynamicColors.SetOthersColor(hue, dis, mainColor, shadowColor, playerId);
    }

    public static void ShareColor(byte shadowType,Color mainOriginalColor,float mainLum,byte mainHue,byte mainDis, Color shadowOriginalColor, float shadowLum, byte shadowHue, byte shadowDis)
    {
        DynamicColors.ReceiveSharedColor(shadowType,mainOriginalColor,mainLum,mainHue,mainDis,shadowOriginalColor,shadowLum,shadowHue,shadowDis);
    }

    public static void SynchronizeTimer(float timer)
    {
        if (Game.GameData.data != null)
            Game.GameData.data.Timer = timer;
    }

    public static void SetPlayerStatus(byte playerId, Game.PlayerData.PlayerStatus status)
    {
        Game.GameData.data.playersArray[playerId].Status = status;
    }

    public static void SetRandomMap(byte mapId)
    {
        GameOptionsManager.Instance.CurrentGameOptions.SetByte(ByteOptionNames.MapId, mapId);
    }

    public static void VersionHandshake(byte[] version, Guid guid, int clientId)
    {
        Patches.GameStartManagerPatch.playerVersions[clientId] = new Patches.GameStartManagerPatch.PlayerVersion(version, guid);
    }

    public static void WinTrigger(byte roleId, byte winnerId)
    {
        Roles.Role role = Roles.Role.GetRoleById(roleId);
        if (role is Roles.Template.HasWinTrigger)
        {
            ((Roles.Template.HasWinTrigger)role).WinTrigger = true;
            ((Roles.Template.HasWinTrigger)role).Winner = winnerId;
        }
    }

    public static void UpdatePlayerControl(byte playerId, float mouseAngle)
    {
        Helpers.playerById(playerId).GetModData().MouseAngle = mouseAngle;
    }

    public static void SendPreMeetingPosition(byte playerId, Vector2 pos)
    {
        Game.GameData.data.playersArray[playerId].preMeetingPosition = pos;
    }

    public static void SetRole(byte playerId, Roles.Role role, int roleDataId, int roleData)
    {
        if (role.category == Roles.RoleCategory.Impostor)
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(Helpers.playerById(playerId), RoleTypes.Impostor);
        }
        else
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(Helpers.playerById(playerId), RoleTypes.Crewmate);
        }

        role.ReflectRoleEyesight(Helpers.playerById(playerId).Data.Role);
        Game.GameData.data.RegisterPlayer(playerId, role, roleDataId, roleData);
    }

    /// <summary>
    /// 初期化時に使用。ゲーム中では使わないこと
    /// </summary>
    /// <param name="role"></param>
    /// <param name="initializeValue"></param>
    /// <param name="playerId"></param>
    public static void SetExtraRole(byte playerId, Roles.ExtraRole role, ulong initializeValue)
    {
        Game.GameData.data.playersArray[playerId]?.extraRole.Add(role);
        Game.GameData.data.playersArray[playerId]?.SetExtraRoleData(role.id, initializeValue);

        role.Setup(Game.GameData.data.playersArray[playerId]);

        Game.GameData.data.playersArray[playerId]?.extraRole.Sort((a, b) => a.assignmentPriority - b.assignmentPriority);
    }

    public static void UnsetExtraRole(Roles.ExtraRole role, byte playerId, bool onMeetingEnd)
    {
        if (onMeetingEnd)
        {
            Events.Schedule.RegisterPostMeetingAction(() =>
            {
                ImmediatelyUnsetExtraRole(role, playerId);
            }, 0);
        }
        else
        {
            ImmediatelyUnsetExtraRole(role, playerId);
        }
    }

    public static void ImmediatelyUnsetExtraRole(Roles.ExtraRole role, byte playerId)
    {
        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            role.FinalizeInGame(PlayerControl.LocalPlayer);
            role.CleanUp();
        }
        role.GlobalFinalizeInGame(Helpers.playerById(playerId));

        role.OnUnset(playerId);

        Game.GameData.data.playersArray[playerId].extraRole.Remove(role);
    }

    public static void ChangeExtraRole(Roles.ExtraRole? removeRole, Roles.ExtraRole addRole, ulong initializeValue, byte playerId)
    {
        if (removeRole != null && Helpers.GetModData(playerId).extraRole.Contains(removeRole))
        {
            ImmediatelyUnsetExtraRole(removeRole, playerId);
        }
        SetExtraRole(playerId, addRole, initializeValue);


        PlayerControl player = Helpers.playerById(playerId);
        addRole.GlobalInitialize(player);
        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            addRole.Initialize(player);
            addRole.ButtonInitialize(HudManager.Instance);
            Objects.CustomButton.ButtonActivate();
        }
    }

    public static void SwapExtraRole(PlayerControl player1, PlayerControl player2, Roles.ExtraRole role, bool onMeetingEnd)
    {
        if (!onMeetingEnd)
        {
            ImmediatelySwapExtraRole(player1, player2, role);
        }
        else
        {
            Events.Schedule.RegisterPostMeetingAction(() =>
            {
                ImmediatelySwapExtraRole(player1, player2, role);
            }, 0);
        }
    }

    public static void ImmediatelySwapExtraRole(PlayerControl player1, PlayerControl player2, Roles.ExtraRole role)
    {
        var modData1 = player1.GetModData();
        var modData2 = player2.GetModData();
        ulong extra1 = 0, extra2 = 0;
        bool hasRole1 = modData1.HasExtraRole(role);
        bool hasRole2 = modData2.HasExtraRole(role);
        if (hasRole1) extra1 = modData1.GetExtraRoleData(role.id);
        if (hasRole2) extra2 = modData2.GetExtraRoleData(role.id);

        if (hasRole1) SetExtraRole(player2.PlayerId, role, extra1); else ImmediatelyUnsetExtraRole(role, player2.PlayerId);
        if (hasRole2) SetExtraRole(player1.PlayerId, role, extra2); else ImmediatelyUnsetExtraRole(role, player1.PlayerId);

    }

    public static void SetGhostRole(byte playerId, Roles.GhostRole? ghostRole)
    {
        var data = Game.GameData.data.playersArray[playerId];
        PlayerControl player = Helpers.playerById(playerId);

        if (data.ghostRole != null)
        {
            var prevRole = data.ghostRole;

            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                prevRole.FinalizeInGame(player);
                prevRole.CleanUp();
            }
        }
        data.ghostRole = ghostRole;

        if (ghostRole != null)
        {
            ghostRole.GlobalInitialize(player);
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                ghostRole.Initialize(player);
                ghostRole.ButtonInitialize(HudManager.Instance);
                Objects.CustomButton.ButtonActivate();
            }


        }
        if (!data.IsAlive && data.role.CanHaveGhostRole) data.AddRoleHistory();
    }

    public static void PlayStaticSound(Module.AudioAsset id)
    {
        Objects.SoundPlayer.PlaySound(id);
    }

    public static void PlayDynamicSound(Vector2 pos, Module.AudioAsset id, float maxDistance, float minDistance)
    {
        Objects.SoundPlayer.PlaySound(pos, id, maxDistance, minDistance);
    }

    public static void UncheckedMurderPlayer(byte murdererId, byte targetId, byte statusId, byte showAnimation, bool cutOverlay = false)
    {
        PlayerControl source = Helpers.playerById(murdererId);
        PlayerControl target = Helpers.playerById(targetId);
        if (source != null && target != null)
        {
            Patches.KillAnimationCoPerformKillPatch.hideNextAnimation = (showAnimation == 0);

            if (cutOverlay)
            {
                //MurderPlayerから必要な処理を抜粋
                GameData.PlayerInfo data = target.Data;

                target.gameObject.layer = LayerMask.NameToLayer("Ghost");
                if (source.AmOwner)
                {
                    if (Constants.ShouldPlaySfx())
                    {
                        SoundManager.Instance.PlaySound(source.KillSfx, false, 0.8f);
                    }
                }
                if (target.AmOwner)
                {
                    StatsManager.Instance.IncrementStat(StringNames.StatsTimesMurdered);
                    if (Minigame.Instance)
                    {
                        try
                        {
                            Minigame.Instance.Close();
                            Minigame.Instance.Close();
                        }
                        catch
                        {
                        }
                    }
                    FastDestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
                    target.cosmetics.SetNameMask(false);
                    target.RpcSetScanner(false);
                }
                source.MyPhysics.StartCoroutine(source.KillAnimations.First().CoPerformKill(source, target));
            }
            else
            {
                //ふつうにMurderPlayerを呼んで問題ない場合
                source.MurderPlayer(target);
            }

            Game.GameData.data.playersArray[target.PlayerId]?.Die(Game.PlayerData.PlayerStatus.GetStatusById(statusId), source.PlayerId);

            if (Game.GameData.data.playersArray[target.PlayerId].role.RemoveAllTasksOnDead)
            {
                target.clearAllTasks();
                var taskData = target.GetModData().Tasks;
                if (taskData != null)
                {
                    taskData.AllTasks = 0;
                    taskData.Completed = 0;
                    taskData.DisplayTasks = 0;
                    taskData.Quota = 0;
                }
            }

            //LocalMethod（自身が殺したとき）
            if (source.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Helpers.RoleAction(source, (role) => { role.OnKillPlayer(target.PlayerId); });
            }
            //LocalMethod (自身が死んだとき)
            if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Helpers.RoleAction(target, (role) => { role.OnMurdered(source.PlayerId); });
            }

            Helpers.RoleAction(PlayerControl.LocalPlayer, (role) => { role.OnAnyoneMurdered(source.PlayerId, target.PlayerId); });


            //GlobalMethod
            Helpers.RoleAction(target, (role) => { role.OnDied(target.PlayerId); });
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { role.OnAnyoneDied(target.PlayerId); });

            //LocalMethod (自身が死んだとき)
            if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Helpers.RoleAction(target, (role) => { role.OnDied(); });


                Events.Schedule.RegisterPreMeetingAction(() =>
                {
                    if (!PlayerControl.LocalPlayer.GetModData().IsAlive)
                        Game.GameData.data.myData.CanSeeEveryoneInfo = true;
                }, 0);
            }
        }
    }

    public static void UncheckedExilePlayer(byte playerId, byte statusId, byte murderId = byte.MaxValue)
    {
        PlayerControl player = Helpers.playerById(playerId);
        if (player != null)
        {
            player.Exiled();
            player.Data.IsDead = true;
            Game.GameData.data.playersArray[playerId]?.Die(Game.PlayerData.PlayerStatus.GetStatusById(statusId), murderId);

            Helpers.RoleAction(player.PlayerId, (role) => role.OnDied(playerId));
            Helpers.RoleAction(PlayerControl.LocalPlayer.PlayerId, (role) => { role.OnAnyoneDied(player.PlayerId); });

            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Helpers.RoleAction(player.PlayerId, (role) => role.OnDied());

                Game.GameData.data.myData.CanSeeEveryoneInfo = true;
            }

            if (MeetingHud.Instance != null)
            {
                MeetingHud.Instance.RecheckPlayerState();

                //ホストは投票終了を今一度調べる
                if (AmongUsClient.Instance.AmHost)
                    MeetingHud.Instance.CheckForEndVoting();
            }
        }
    }

    public static void UncheckedCmdReportDeadBody(byte reporterId, byte targetId)
    {
        PlayerControl reporter = Helpers.playerById(reporterId);
        PlayerControl target = Helpers.playerById(targetId);
        if (reporter != null && target != null)
        {
            reporter.ReportDeadBody(target.Data);
        }
    }


    public static void Guard(byte killerId, byte targetId)
    {
        Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (r) =>
         {
             r.OnAnyoneGuarded(killerId, targetId);
         });
    }

    public static void CloseUpKill(byte killerId, byte targetId, byte statusId, bool playSoundForEveryone)
    {
        UncheckedExilePlayer(targetId, statusId, killerId);

        if (Constants.ShouldPlaySfx() && (playSoundForEveryone || targetId == PlayerControl.LocalPlayer.PlayerId)) SoundManager.Instance.PlaySound(Helpers.playerById(targetId).KillSfx, false, 0.8f);

        if (PlayerControl.LocalPlayer.PlayerId == targetId)
            FastDestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(Helpers.playerById(killerId).Data, PlayerControl.LocalPlayer.Data);

        if (!Roles.Roles.F_Guesser.secondoryRoleOption.getBool())
        {
            //Guesserを考慮に入れる
            Game.GameData.data.EstimationAI.DetermineMultiply(new Roles.Role[] { Roles.Roles.NiceGuesser, Roles.Roles.EvilGuesser });
        }
    }

    public static void UpdateRoleData(byte playerId, int dataId, int newData)
    {
        Game.GameData.data.playersArray[playerId].SetRoleData(dataId, newData);

        //自身のロールデータ更新時に呼ぶメソッド群
        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            Game.GameData.data.myData.getGlobalData().role.OnUpdateRoleData(dataId, newData);
        }
    }

    public static void UpdateExtraRoleData(byte playerId, byte roleId, ulong newData)
    {
        Game.GameData.data.playersArray[playerId].SetExtraRoleData(roleId, newData);

        //自身のロールデータ更新時に呼ぶメソッド群
        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            foreach (Roles.ExtraRole role in Game.GameData.data.myData.getGlobalData().extraRole)
            {
                if (role.id == roleId) role.OnUpdateRoleData(newData);
            }
        }
    }

    public static void GlobalEvent(byte eventId, float duration, ulong option)
    {
        Events.GlobalEvent.Activate(Events.GlobalEvent.Type.GetType(eventId), duration, option);
    }

    public static void DragAndDropPlayer(byte playerId, byte targetId, Vector2 pos)
    {
        if (targetId != Byte.MaxValue)
        {
            Game.GameData.data.playersArray[playerId]?.DragPlayer(targetId);
        }
        else
        {
            byte deadPlayer = Game.GameData.data.playersArray[playerId]?.dragPlayerId ?? byte.MaxValue;
            SnapToDeadBody(deadPlayer, pos);
            Game.GameData.data.playersArray[playerId]?.DropPlayer();
        }
    }

    private static void SetUpRole(Game.PlayerData data, PlayerControl player, Roles.Role role, Dictionary<int, int>? roleData = null)
    {
        bool isMe = false;
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            isMe = true;
        }

        if (isMe)
        {
            data.role.CleanUp();
        }

        data.role = role;
        if (data.role.category == Roles.RoleCategory.Impostor)
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Impostor);
        }
        else
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Crewmate);
        }
        role.ReflectRoleEyesight(player.Data.Role);

        data.CleanRoleDataInGame(null);

        data.role.GlobalInitialize(player);

        if (isMe)
        {
            data.role.Initialize(player);
            data.role.ButtonInitialize(HudManager.Instance);
            Objects.CustomButton.ButtonActivate();
            Game.GameData.data.myData.VentCoolDownTimer = data.role.VentCoolDownMaxTimer;
        }

        if (roleData != null) data.CleanRoleDataInGame(roleData);

        //役職遍歴を追加する
        data.AddRoleHistory();
    }

    public static void ChangeRole(byte playerId, byte roleId)
    {
        Events.Schedule.RegisterPostMeetingAction(() =>
        {
            ImmediatelyChangeRole(playerId, roleId, new Tuple<Tuple<Roles.ExtraRole, ulong>, bool>[0]);
        }, 16);
    }

    public static void ImmediatelyChangeRole(byte playerId, byte roleId)
    {
        ImmediatelyChangeRole(playerId, roleId, new Tuple<Tuple<Roles.ExtraRole, ulong>, bool>[0]);
    }

    public static void ImmediatelyChangeRole(byte playerId, byte roleId, Tuple<Tuple<Roles.ExtraRole, ulong>, bool>[] extraRoles)
    {
        Game.PlayerData data = Game.GameData.data.GetPlayerData(playerId);

        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            data.role.FinalizeInGame(PlayerControl.LocalPlayer);
            data.role.CleanUp();
        }
        data.role.GlobalFinalizeInGame(Helpers.playerById(playerId));

        foreach (var t in extraRoles)
        {
            if (t.Item2)
            {
                RPCEvents.SetExtraRole(playerId, t.Item1.Item1, t.Item1.Item2);
            }
            else
            {
                RPCEvents.ImmediatelyUnsetExtraRole(t.Item1.Item1, playerId);
            }
        }

        //ロールを変更
        SetUpRole(data, Helpers.playerById(playerId), Roles.Role.GetRoleById(roleId));

        Game.GameData.data.myData.getGlobalData().role.OnAnyoneRoleChanged(playerId);
    }

    public static void SwapRole(byte playerId_1, byte playerId_2)
    {
        Events.Schedule.RegisterPostMeetingAction(() =>
        {
            Game.PlayerData? data1 = Game.GameData.data.GetPlayerData(playerId_1);
            Game.PlayerData? data2 = Game.GameData.data.GetPlayerData(playerId_2);

            if (data1 == null || data2 == null) return;

            if (playerId_1 == PlayerControl.LocalPlayer.PlayerId)
            {
                data1.role.FinalizeInGame(PlayerControl.LocalPlayer);
            }
            data1.role.GlobalFinalizeInGame(Helpers.playerById(playerId_1));

            if (playerId_2 == PlayerControl.LocalPlayer.PlayerId)
            {
                data2.role.FinalizeInGame(PlayerControl.LocalPlayer);
            }
            data2.role.GlobalFinalizeInGame(Helpers.playerById(playerId_2));

            Dictionary<int, int> roleData1 = data1.ExtractRoleData(), roleData2 = data2.ExtractRoleData();
            Roles.Role role1 = data1.role, role2 = data2.role;

                //ロールを変更
                SetUpRole(data1, Helpers.playerById(playerId_1), role2, roleData2);
            SetUpRole(data2, Helpers.playerById(playerId_2), role1, roleData1);
        }, 16);
    }

    public static void RevivePlayer(byte playerId, Vector2 pos, bool changeStatus, bool gushOnRevive)
    {
        if (Game.GameData.data.GameMode == CustomGameMode.Standard)
        {
            //NecromancerやBuskerを確定させる
            Game.GameData.data.EstimationAI.Determine(changeStatus ? (Roles.Role)Roles.Roles.Necromancer : (Roles.Role)Roles.Roles.Busker);

            foreach (DeadBody body in Helpers.AllDeadBodies())
            {
                if (body.ParentId != playerId) continue;

                UnityEngine.Object.Destroy(body.gameObject);
                break;
            }

            Game.GameData.data.playersArray[playerId]?.Revive(changeStatus);
            PlayerControl player = Helpers.playerById(playerId);
            if (pos != null) player.NetTransform.SnapTo(pos);
            player.Revive();
            player.Data.IsDead = false;
            Game.GameData.data.deadPlayers.Remove(playerId);
        }
        else
        {
            foreach (DeadBody body in Helpers.AllDeadBodies())
            {
                if (body.ParentId != playerId) continue;
                UnityEngine.Object.Destroy(body.gameObject);
            }

            Game.GameData.data.playersArray[playerId]?.Revive();
            PlayerControl player = Helpers.playerById(playerId);
            player.Revive();
            player.Data.IsDead = false;
            Game.GameData.data.deadPlayers.Remove(playerId);
        }

        if (gushOnRevive)
        {
            var data = Game.GameData.data.playersArray[playerId];
            data.Property.SetUnderTheFloorForcely(true);
            data.Property.UnderTheFloor = false;
        }

        Game.GameData.data.myData.getGlobalData().role.onRevived(playerId);
    }

    public static void EmitSpeedFactor(byte playerId, Game.SpeedFactor speedFactor)
    {
        Game.GameData.data.playersArray[playerId]?.Speed.Register(speedFactor);
    }

    public static void EmitPlayerAttributeFactor(byte playerId, Game.PlayerAttributeFactor attributeFactor)
    {
        Game.GameData.data.playersArray[playerId]?.Attribute.Register(attributeFactor);
    }

    //ホストのイベントを本人に受け継ぐ
    public static void RequireUniqueRPC(byte playerId, byte actionId)
    {
        //自分自身に対する要求の場合
        if (PlayerControl.LocalPlayer.PlayerId == playerId)
        {
            Game.GameData.data.myData.getGlobalData().role.UniqueAction(actionId);
        }
    }

    //送信元と受信先で挙動が異なる（以下は受信側）
    public static void ShareOptions(int numberOfOptions, MessageReader reader)
    {
        try
        {
            for (int i = 0; i < numberOfOptions; i++)
            {
                uint optionId = reader.ReadPackedUInt32();
                uint selection = reader.ReadPackedUInt32();

                if (optionId == uint.MaxValue)
                {
                    GameOptionsManager.Instance.CurrentGameOptions.SetInt(Int32OptionNames.NumImpostors, (int)selection);
                }
                else
                {
                    CustomOption option = CustomOption.AllOptions.FirstOrDefault(opt => opt.id == (int)optionId);
                    option.updateSelection((int)selection);
                }
            }
        }
        catch (Exception e)
        {

        }
        GameOptionsDataPatch.dirtyFlag = true;
    }

    public static void CleanDeadBody(byte deadBodyId)
    {
        foreach (DeadBody deadBody in Helpers.AllDeadBodies())
        {
            if (deadBody.ParentId == deadBodyId)
            {
                UnityEngine.Object.Destroy(deadBody.gameObject);
                Game.GameData.data.deadPlayers[deadBodyId].EraseBody();
                break;
            }
        }

        //Cleaner,Vultureを考慮に入れる
        Game.GameData.data.EstimationAI.DetermineMultiply(new Roles.Role[] { Roles.Roles.Vulture, Roles.Roles.Cleaner });
    }

    public static void SnapToDeadBody(byte deadBodyId, Vector2 pos)
    {
        foreach (DeadBody deadBody in Helpers.AllDeadBodies())
        {
            if (deadBody.ParentId == deadBodyId)
            {
                deadBody.transform.position = new Vector3(pos.x, pos.y, pos.y / 1000f);
                break;
            }
        }
    }


    public static void CountDownMessage(byte count)
    {
        if (Game.GameData.data == null) return;

        string text = "";
        if (count > 0)
        {
            text = Language.Language.GetString("game.message.countDown").Replace("%COUNT%", count.ToString());
        }
        else
        {
            text = Language.Language.GetString("game.message.start");
        }


        if (Game.GameData.data.CountDownMessage == null)
        {
            Game.GameData.data.CountDownMessage =
                Objects.CustomMessage.Create(text,
                (float)count + 1f, 0f, 1f, Color.white);
        }
        else
        {
            Game.GameData.data.CountDownMessage.SetText(text);
        }
    }

    private static IEnumerator GetTasksEnumerator(byte playerId, int allTasks, bool isCrewmateTask, bool isInfiniteQuota)
    {
        while (true)
        {
            var p = Game.GameData.data.GetPlayerData(playerId);
            if (p != null)
            {
                p.Tasks = new Game.TaskData(allTasks, allTasks, allTasks, isCrewmateTask, isInfiniteQuota);
                break;
            }
            yield return null;
        }
    }

    public static void SetTasks(byte playerId, int allTasks, bool isCrewmateTask, bool isInfiniteQuota)
    {
        var p = Game.GameData.data.GetPlayerData(playerId);
        if (p == null)
        {
            HudManager.Instance.StartCoroutine(GetTasksEnumerator(playerId, allTasks, isCrewmateTask, isInfiniteQuota).WrapToIl2Cpp());
        }
        else
        {
            p.Tasks = new Game.TaskData(allTasks, allTasks, allTasks, isCrewmateTask, isInfiniteQuota);
        }
    }

    public static void ChangeTasks(byte playerId, int allTasks, int allQuota)
    {
        var p = Game.GameData.data.playersArray[playerId];
        if (p == null) return;

        p.Tasks = new Game.TaskData(allTasks, allTasks, allQuota, true, false);
    }

    public static void RefreshTasks(byte playerId, int displayTasks, int addQuota)
    {
        var p = Game.GameData.data.playersArray[playerId];
        if (p == null) return;

        if (p.Tasks != null)
        {
            p.Tasks.AllTasks += displayTasks;
            p.Tasks.DisplayTasks = displayTasks;
            p.Tasks.Quota += addQuota;
        }
        else
        {
            p.Tasks = new Game.TaskData(displayTasks, displayTasks, addQuota, true, false);
        }
    }

    public static void CompleteTask(byte playerId)
    {
        if (Game.GameData.data.playersArray[playerId].Tasks == null) return;
        Game.GameData.data.playersArray[playerId].Tasks.Completed++;
    }

    public static void ExemptAllTask(byte playerId)
    {
        if (Game.GameData.data.playersArray[playerId].Tasks == null) return;
        Game.GameData.data.playersArray[playerId].Tasks.Quota = 0;
    }

    public static void FixLights()
    {
        SwitchSystem switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
        switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
    }

    public static Objects.CustomObject ObjectInstantiate(byte ownerId, byte objectTypeId, ulong objectId, float positionX, float positionY)
    {
        Objects.CustomObject obj = new Objects.CustomObject(ownerId, Objects.CustomObject.Type.AllTypes[objectTypeId], objectId, new Vector3(positionX, positionY));
        obj.ObjectType.Update(obj);
        return obj;
    }

    public static void ObjectUpdate(ulong objectId, int command)
    {
        if (Objects.CustomObject.Objects.ContainsKey(objectId))
        {
            Objects.CustomObject.Objects[objectId].Update(command);
        }
    }

    public static void ObjectDestroy(ulong objectId)
    {
        if (Objects.CustomObject.Objects.ContainsKey(objectId))
        {
            Objects.CustomObject.Objects[objectId].Destroy();
        }
    }

    private static IEnumerator GetSynchronizeEnumrator(byte playerId, int tag)
    {
        while (Game.GameData.data == null)
        {
            yield return null;
        }
        Game.GameData.data.SynchronizeData.Synchronize((Game.SynchronizeTag)tag, playerId);
    }

    public static void Synchronize(byte playerId, int tag)
    {
        HudManager.Instance.StartCoroutine(GetSynchronizeEnumrator(playerId, tag).WrapToIl2Cpp());
    }

    public static void KillGuard(byte playerId, byte type, byte value)
    {
        switch (type)
        {
            case 0:
                //Guardian追加
                Game.GameData.data.GetPlayerData(playerId).guardStatus.AddGuardian(value);
                break;
            case 1:
                //SingleUseGuard追加
                Game.GameData.data.GetPlayerData(playerId).guardStatus.AddSingleUseGuardian(value);
                break;
            case 2:
                //SingleUseGuard消費
                Game.GameData.data.GetPlayerData(playerId).guardStatus.AddSingleUseGuardian(-value);
                break;
            case 3:
                //Guardian削除
                Game.GameData.data.GetPlayerData(playerId).guardStatus.RemoveGuardian(value);
                break;
        }
    }

    public static void SealVent(byte playerId, int ventId)
    {
        Events.Schedule.RegisterPostMeetingAction(() =>
        {
            Vent vent = ShipStatus.Instance.AllVents.FirstOrDefault((x) => x != null && x.Id == ventId);
            if (vent == null) return;

            Roles.Roles.Navvy.SetSealedVentSprite(vent, 1f);
            vent.GetVentData().Sealed = true;

                //Navvyを確定させる
                Game.GameData.data.EstimationAI.Determine(Roles.Roles.Navvy);
        }, 1);

        Game.GameData.data.playersArray[playerId]?.AddRoleData(Roles.Roles.Navvy.remainingScrewsDataId, -1);
    }

    public static void MultipleVote(byte playerId, byte count)
    {
        Patches.MeetingHudPatch.SetVoteWeight(playerId, count);
    }

    public static void SniperSettleRifle(byte playerId)
    {
        List<Objects.CustomObject> objList = new List<Objects.CustomObject>();
        foreach (Objects.CustomObject obj in Objects.CustomObject.Objects.Values)
        {
            if (obj.OwnerId != playerId) continue;
            if (obj.ObjectType != Objects.ObjectTypes.SniperRifle.Rifle) continue;

            objList.Add(obj);
        }

        foreach (var obj in objList)
        {
            obj.Destroy();
        }
    }

    public static void RaiderSettleAxe(byte playerId)
    {
        List<Objects.CustomObject> objList = new List<Objects.CustomObject>();
        foreach (Objects.CustomObject obj in Objects.CustomObject.Objects.Values)
        {
            if (obj.OwnerId != playerId) continue;
            if (obj.ObjectType != Objects.ObjectTypes.RaidAxe.Axe) continue;
            if (obj.Data[0] != (int)Objects.ObjectTypes.RaidAxe.AxeState.Static) continue;

            objList.Add(obj);
        }

        foreach (var obj in objList)
        {
            obj.Destroy();
        }
    }

    public static void UpdateRestrictTimer(byte device, float timer)
    {
        switch (device)
        {
            case 0:
                Game.GameData.data.UtilityTimer.AdminTimer -= timer;
                break;
            case 1:
                Game.GameData.data.UtilityTimer.VitalsTimer -= timer;
                break;
            case 2:
                Game.GameData.data.UtilityTimer.CameraTimer -= timer;
                break;
        }
    }

    public static void UndergroundAction(byte playerId, bool underground)
    {
        PlayerControl pc = Helpers.playerById(playerId);
        if (!pc) return;

        pc.GetModData().Property.UnderTheFloor = underground;
    }

    public static void DeathGuage(byte attackerId, byte playerId, float value)
    {
        PlayerControl pc = Helpers.playerById(playerId);
        if (!pc) return;

        pc.GetModData().DeathGuage += value;

        if (pc == PlayerControl.LocalPlayer)
        {
            if (pc.GetModData().DeathGuage < 1f)
            {
                Objects.SoundPlayer.PlaySound(Module.AudioAsset.HadarFear);
            }
        }

        if (attackerId == PlayerControl.LocalPlayer.PlayerId)
        {
            if (pc.GetModData().DeathGuage >= 1f)
            {
                RPCEventInvoker.CloseUpKill(PlayerControl.LocalPlayer, pc, Game.PlayerData.PlayerStatus.Dead, true);
            }
        }
    }

    public static void SniperShot(byte murderId)
    {
        //Sniperを確定させる
        Game.GameData.data.EstimationAI.Determine(Roles.Roles.Sniper);

        //通知距離を超えていたら何もしない
        if (Helpers.playerById(murderId).transform.position.Distance(PlayerControl.LocalPlayer.transform.position) > Roles.Roles.Sniper.noticeRangeOption.getFloat())
            return;

        Objects.Arrow arrow = new Objects.Arrow(Color.white,false);
        arrow.image.sprite = Roles.Roles.Sniper.getSnipeArrowSprite();

        Vector3 pos = Helpers.playerById(murderId).transform.position;

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(10f, new Action<float>((p) =>
        {
            arrow.Update(pos);
            arrow.arrow.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            if (p > 0.8f)
            {
                arrow.image.color = new Color(1f, 1f, 1f, (1f - p) * 5f);
            }
            if (p == 1f)
            {
                    //矢印を消す
                    UnityEngine.Object.Destroy(arrow.arrow);
            }
        })));
    }

    public static void RaiderThrow(byte murderId, Vector2 pos, float angle)
    {
        //Sniperを確定させる
        Game.GameData.data.EstimationAI.Determine(Roles.Roles.Raider);

        //Axeを投げ状態にする
        foreach (Objects.CustomObject obj in Objects.CustomObject.Objects.Values)
        {
            if (obj.OwnerId != murderId) continue;
            if (obj.ObjectType != Objects.ObjectTypes.RaidAxe.Axe) continue;

            if (obj.Data[0] == (int)Objects.ObjectTypes.RaidAxe.AxeState.Static)
            {
                Objects.ObjectTypes.RaidAxe.Axe.UpdateState(obj, Objects.ObjectTypes.RaidAxe.AxeState.Thrown);
                Objects.ObjectTypes.RaidAxe.Axe.SetAngle(obj, angle);
                obj.GameObject.transform.position = pos;
            }
        }
    }

    public static void Morph(byte playerId, Game.PlayerData.PlayerOutfitData outfit)
    {
        //Morphingを確定させる
        Game.GameData.data.EstimationAI.Determine(Roles.Roles.Morphing);

        Events.LocalEvent.Activate(new Roles.ImpostorRoles.Morphing.MorphEvent(playerId, outfit));
    }

    public static void MorphCancel(byte playerId)
    {
        Events.LocalEvent.Inactivate((Events.LocalEvent e) =>
        {
            if (e is Roles.ImpostorRoles.Morphing.MorphEvent)
            {
                return ((Roles.ImpostorRoles.Morphing.MorphEvent)e).PlayerId == playerId;
            }
            return false;
        });
    }

    public static void CreateSidekick(byte playerId, byte jackalId)
    {
        if (Roles.NeutralRoles.Sidekick.SidekickTakeOverOriginalRoleOption.getBool())
        {
            RPCEvents.ImmediatelyChangeRole(playerId, Roles.Roles.Sidekick.id, new Tuple<Tuple<Roles.ExtraRole, ulong>, bool>[0]);
            RPCEvents.UpdateRoleData(playerId, Roles.Roles.Jackal.jackalDataId, jackalId);
        }
        else
        {
            RPCEvents.SetExtraRole(playerId, Roles.Roles.SecondarySidekick, (ulong)jackalId);
        }
    }

    public static void DisturberInvoke(byte playerId, ulong objectId1, ulong objectId2)
    {
        Vector2 pos1 = (Vector2)Objects.CustomObject.Objects[objectId1].GameObject.transform.position + new Vector2(0, -0.3f);
        Vector2 pos2 = (Vector2)Objects.CustomObject.Objects[objectId2].GameObject.transform.position + new Vector2(0, -0.3f);
        Vector2 pos1Upper = pos1 + new Vector2(0, 0.5f);
        Vector2 pos2Upper = pos2 + new Vector2(0, 0.5f);
        Vector2 center = (pos1 + pos2) / 2f;

        bool vertical = Mathf.Abs(pos1.x - pos2.x) < 0.8f;

        var obj = new GameObject("ElecBarrior");
        var MeshFilter = obj.AddComponent<MeshFilter>();
        var MeshRenderer = obj.AddComponent<MeshRenderer>();
        var Collider = obj.AddComponent<EdgeCollider2D>();
        
        obj.transform.localPosition = new Vector3(center.x, center.y, center.y / 1000f);

        var mesh = new Mesh();
        var vertextList = new Il2CppSystem.Collections.Generic.List<Vector3>();
        var uvList = new Il2CppSystem.Collections.Generic.List<Vector2>();

        vertextList.Add(pos1Upper - center + new Vector2(vertical ? 0.22f : 0f, 0.2f));
        vertextList.Add(pos2Upper - center + new Vector2(vertical ? 0.22f : 0f, 0.2f));
        vertextList.Add(pos1 - center + new Vector2(vertical ? -0.22f : 0f, vertical ? 0.7f : 0.2f));
        vertextList.Add(pos2 - center + new Vector2(vertical ? -0.22f : 0f, vertical ? 0.7f : 0.2f));

        uvList.Add(new Vector2(0, 0));
        uvList.Add(new Vector2(1f / 3f, 0));
        uvList.Add(new Vector2(0, 1));
        uvList.Add(new Vector2(1f / 3f, 1));

        mesh.SetVertices(vertextList);
        mesh.SetUVs(0, uvList);
        mesh.SetIndices(new int[6] { 0, 2, 1, 1, 2, 3 }, MeshTopology.Triangles, 0);
        
        Collider.points = new Vector2[5] { pos1 - center, pos1Upper - center, pos2Upper - center, pos2 - center, pos1 - center };
        Collider.edgeRadius = 0.2f;

        MeshRenderer.material = new Material(FastDestroyableSingleton<HudManager>.Instance.MapButton.HeldButtonSprite.material);
        MeshRenderer.material.mainTexture = vertical ? Roles.Roles.Disturber.getElecAnimSubTexture() : Roles.Roles.Disturber.getElecAnimTexture();

        MeshFilter.mesh = mesh;

        float timer = 0.1f;
        int num = 0;
        new Objects.DynamicCollider(Collider, Roles.Roles.Disturber.disturbDurationOption.getFloat(), false, (c) =>
        {
            timer -= Time.deltaTime;

            if (timer < 0f)
            {
                timer = 0.1f;
                num = (num + 1) % 3;

                uvList[0] = new Vector2((float)num / 3f, 0);
                uvList[1] = new Vector2((float)(num + 1) / 3f, 0);
                uvList[2] = new Vector2((float)num / 3f, 1);
                uvList[3] = new Vector2((float)(num + 1) / 3f, 1);
                mesh.SetUVs(0, uvList);
            }
        }, Roles.Roles.Disturber.ignoreBarriorsOption.getSelection() == 1,
        (ulong)(Roles.Roles.Disturber.ignoreBarriorsOption.getSelection() == 2 ? 1 << playerId : 0));
    }

    public static void UpdatePlayerVisibility(byte player, bool flag)
    {
        Game.GameData.data.playersArray[player].isInvisiblePlayer = !flag;
    }

    public static void EditCoolDown(Roles.CoolDownType coolDownType, float time)
    {
        Helpers.RoleAction(Game.GameData.data.myData.getGlobalData(), (role) =>
         {
             role.EditCoolDown(coolDownType, time);
         });
    }

    private static IEnumerator GetRitualSharePerksEnumerator(byte playerId, int[] perks)
    {
        while (Game.GameData.data == null) yield return null;
        Game.GameData.data.RitualData.RegisterPlayerData(playerId, perks);
        Game.GameData.data.RitualData.CheckAndSynchronize(AmongUsClient.Instance.AmHost);
    }

    public static void RitualSharePerks(byte playerId, int[] perks)
    {
        HudManager.Instance.StartCoroutine(GetRitualSharePerksEnumerator(playerId, perks).WrapToIl2Cpp());
    }

    public static void RitualUpdateTaskProgress(int taskNum)
    {
        PlayerControl.LocalPlayer.myTasks[taskNum].CastFast<Tasks.NebulaPlayerTask>().NebulaData[2]++;
    }

    public static void RitualUpdate(MessageReader reader)
    {
        switch (reader.ReadInt32())
        {
            case 0:
                RitualUpdateTaskProgress(reader.ReadInt32());
                break;

        }
    }

    public static void InitializeRitualData(MessageReader reader)
    {
        int n = reader.ReadInt32();
        for (int i = 0; i < n; i++)
        {
            Game.GameData.data.RitualData.AddTaskData(Game.RitualData.TaskData.Deserialize(reader));
        }
        Game.GameData.data.RitualData.CheckAndSynchronize(AmongUsClient.Instance.AmHost);

        n = reader.ReadInt32();
        for (int i = 0; i < n; i++)
        {
            Game.GameData.data.RitualData.RegisterPlayerSpawnData(reader.ReadByte(), new Vector2(reader.ReadSingle(), reader.ReadSingle()));
        }
    }

    public static void DecoySwap(PlayerControl player, Objects.CustomObject? decoy, float playerX, float playerY, float decoyX, float decoyY)
    {
        bool playerFlip = player.cosmetics.FlipX;
        bool decoyFlip = decoy.Renderer.flipX;

        player.NetTransform.SnapTo(new Vector2(decoyX, decoyY));
        if (decoy) decoy.GameObject.transform.position = new Vector3(playerX, playerY);

        player.cosmetics.SetFlipX(decoyFlip);
        if (decoy) decoy.Renderer.flipX = playerFlip;
    }

    public static void UpdatePlayersIconInfo(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        Roles.Role role = Roles.Role.GetRoleById(reader.ReadByte());

        var info = (Module.Information.PlayersIconInformation?)Module.Information.UpperInformationManager.GetInformation((i) =>
        {
            return i is Module.Information.PlayersIconInformation &&
            ((Module.Information.PlayersIconInformation)i).relatedPlayerId == playerId &&
            ((Module.Information.PlayersIconInformation)i).relatedRole == role;
        });

        if (info == null) return;

        int length = reader.ReadInt32();
        byte id, data;
        for (int i = 0; i < length; i++)
        {
            id = reader.ReadByte();
            data = reader.ReadByte();

            if (data == byte.MaxValue) info.SetActive(id, false);
            else
            {
                info.SetActive(id, true);
                if (data == 100)
                {
                    info.SetText(id, "");
                    info.SetSemitransparent(id, false);
                }
                else
                {
                    info.SetSemitransparent(id, true);

                    if (data == 0)
                    {
                        info.SetText(id, "");
                    }
                    else
                    {
                        info.SetText(id, data.ToString() + "%");
                    }
                }
            }
        }
    }

    public static void Paint(PlayerControl player, Game.PlayerData.PlayerOutfitData outfit)
    {
        if (player == PlayerControl.LocalPlayer) return;

        if (Roles.Roles.Painter.changeLookImmediatelyOption.getBool())
        {
            player.GetModData().AddOutfit(outfit);
        }
        else
        {
            Events.Schedule.RegisterPostMeetingAction(
                () =>
                {
                    player.GetModData().AddOutfit(outfit);
                }, 50);
        }
    }

    static public void Poltergeist(byte deadBodyId, Vector2 vector)
    {
        Events.LocalEvent.Activate(new Roles.GhostRoles.Poltergeist.PoltergeistEvent(deadBodyId, vector));
    }

    static public void InstantiateDeadBody(byte targetId, Vector3 position)
    {
        var p = Helpers.playerById(targetId);
        DeadBody deadBody = GameObject.Instantiate<DeadBody>(p.KillAnimations[0].bodyPrefab);
        deadBody.enabled = false;
        deadBody.ParentId = targetId;
        p.SetPlayerMaterialColors(deadBody.bodyRenderer);
        p.SetPlayerMaterialColors(deadBody.bloodSplatter);

        position.z = position.y / 1000f;
        deadBody.transform.position = position;
        deadBody.enabled = true;
    }
}

public class RPCEventInvoker
{

    public static void SendCustomData(CustomData customData)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendCustomData, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write((byte)customData);
        switch (customData)
        {
            case CustomData.CurrentTask:
                List<Tuple<Vector2, bool>> locList = new List<Tuple<Vector2, bool>>();
                foreach (var task in PlayerControl.LocalPlayer.myTasks)
                {
                    if (task.HasLocation && !task.IsComplete)
                        foreach (var loc in task.Locations)
                            locList.Add(new Tuple<Vector2, bool>(loc, task.TaskStep > 0));
                }
                writer.Write((int)locList.Count);
                foreach (var tuple in locList)
                {
                    writer.Write(tuple.Item1.x);
                    writer.Write(tuple.Item1.y);
                    writer.Write(tuple.Item2);
                }
                break;
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RequireCustomData(byte playerId, CustomData customData)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequireCustomData, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)customData);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SetPlayerStatus(byte playerId, Game.PlayerData.PlayerStatus status)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPlayerStatus, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(status.Id);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SetPlayerStatus(playerId, status);
    }

    public static void SynchronizeTimer()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SynchronizeTimer, Hazel.SendOption.Reliable, -1);
        writer.Write(Game.GameData.data.Timer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        //自信は何もしなくてよい
    }

    public static void SetRandomMap(byte mapId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRandomMap, Hazel.SendOption.Reliable, -1);
        writer.Write(mapId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SetRandomMap(mapId);
    }

    public static void SendPreMeetingPosition(Vector2 pos)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendPreMeetingPosition, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SendPreMeetingPosition(PlayerControl.LocalPlayer.PlayerId, pos);
    }


    private static void WriteRolesData(MessageWriter writer, Patches.AssignMap assignMap)
    {
        writer.Write(assignMap.RoleMap.Count);
        foreach (var entry in assignMap.RoleMap)
        {
            writer.Write(entry.Key);
            writer.Write(entry.Value);
            var data = Game.GameData.data.GetPlayerData(entry.Key);
            int initId = data.GetInitializeRoleData();
            writer.Write(initId);
            writer.Write(initId != -1 ? data.GetRoleData(initId) : 0);
        }
        writer.Write(assignMap.ExtraRoleList.Count);
        foreach (var tuple in assignMap.ExtraRoleList)
        {
            writer.Write(tuple.Item1);
            writer.Write(tuple.Item2.Item1);
            writer.Write(tuple.Item2.Item2);
        }
    }

    public static void SetRoles(Patches.AssignMap assignMap)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRoles, Hazel.SendOption.Reliable, -1);

        WriteRolesData(writer, assignMap);

        AmongUsClient.Instance.FinishRpcImmediately(writer);
        //自分自身はもう割り当て済みなので何もしない
    }

    public static void InitializeRitualData(List<Game.RitualData.TaskData> taskDataList, Dictionary<byte, Game.RitualData.RitualPlayerData> playerData)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.InitializeRitualData, Hazel.SendOption.Reliable, -1);
        writer.Write(taskDataList.Count);
        foreach (var t in taskDataList) t.Serialize(writer);

        writer.Write(playerData.Count);
        foreach (var d in playerData)
        {
            writer.Write(d.Key);
            writer.Write(d.Value.SpawnPos.x);
            writer.Write(d.Value.SpawnPos.y);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SetMyColor()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMyColor, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(DynamicColors.MyColor.GetMainHue());
        writer.Write(DynamicColors.MyColor.GetMainDistance());
        var color = DynamicColors.MyColor.GetMainColor();
        writer.Write(color.r);
        writer.Write(color.g);
        writer.Write(color.b);
        color = DynamicColors.MyColor.GetShadowColor();
        writer.Write(color.r);
        writer.Write(color.g);
        writer.Write(color.b);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SetMyColor(PlayerControl.LocalPlayer.PlayerId, DynamicColors.MyColor.GetMainHue(), DynamicColors.MyColor.GetMainDistance(), DynamicColors.MyColor.GetMainColor(), DynamicColors.MyColor.GetShadowColor());
    }

    public static void ShareColor(DynamicColors.CustomColor customColor) 
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShareColor, Hazel.SendOption.Reliable, -1);
        writer.Write(customColor.GetShadowType());
        var color = customColor.GetMainOriginalColor();
        writer.Write(color.r);
        writer.Write(color.g);
        writer.Write(color.b);
        writer.Write(customColor.GetMainLuminosity());
        writer.Write(customColor.GetMainHue());
        writer.Write(customColor.GetMainDistance());
        color = customColor.GetShadowOriginalColor();
        writer.Write(color.r);
        writer.Write(color.g);
        writer.Write(color.b);
        writer.Write(customColor.GetShadowLuminosity());
        writer.Write(customColor.GetShadowHue());
        writer.Write(customColor.GetShadowDistance());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ShareColor(customColor.GetShadowType(), customColor.GetMainOriginalColor(), customColor.GetMainLuminosity(), customColor.GetMainHue(), customColor.GetMainDistance(), customColor.GetShadowColor(),customColor.GetShadowLuminosity(),customColor.GetShadowHue(),customColor.GetShadowDistance());
    }

    public static void WinTrigger(Roles.Role role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.WinTrigger, Hazel.SendOption.Reliable, -1);
        writer.Write(role.id);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.WinTrigger(role.id, PlayerControl.LocalPlayer.PlayerId);
    }

    public static void PlayStaticSound(Module.AudioAsset id)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayStaticSound, Hazel.SendOption.Reliable, -1);
        writer.Write((byte)id);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.PlayStaticSound(id);
    }

    public static void PlayDynamicSound(Vector2 pos, Module.AudioAsset id, float maxDistance, float minDistance)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayDynamicSound, Hazel.SendOption.Reliable, -1);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write((byte)id);
        writer.Write(maxDistance);
        writer.Write(minDistance);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.PlayDynamicSound(pos, id, maxDistance, minDistance);
    }

    public static void UpdatePlayerControl()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UpdatePlayerControl, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(Game.GameData.data.myData.getGlobalData().MouseAngle);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        //自身は別で更新しているのでなにもしない
    }

    public static void UncheckedMurderPlayer(byte murdererId, byte targetId, byte statusId, bool showAnimation)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
        writer.Write(murdererId);
        writer.Write(targetId);
        writer.Write(statusId);
        writer.Write(showAnimation ? Byte.MaxValue : (byte)0);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UncheckedMurderPlayer(murdererId, targetId, statusId, showAnimation ? Byte.MaxValue : (byte)0);
    }

    public static void Guard(byte murdererId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guard, Hazel.SendOption.Reliable, -1);
        writer.Write(murdererId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.Guard(murdererId, targetId);
    }

    public static void SuicideWithoutOverlay(byte statusId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(statusId);
        writer.Write((byte)0);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.PlayerId, statusId, (byte)0, true);
    }

    public static void UncheckedExilePlayer(byte playerId, byte statusId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedExilePlayer, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(statusId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UncheckedExilePlayer(playerId, statusId);
    }

    public static void UncheckedCmdReportDeadBody(byte reporterId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedCmdReportDeadBody, Hazel.SendOption.Reliable, -1);
        writer.Write(reporterId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UncheckedCmdReportDeadBody(reporterId, targetId);
    }

    public static void UpdateRoleData(byte playerId, int dataId, int newData)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UpdateRoleData, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(dataId);
        writer.Write(newData);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UpdateRoleData(playerId, dataId, newData);
    }

    public static void UpdateExtraRoleData(byte playerId, byte roleId, ulong newData)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UpdateExtraRoleData, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(roleId);
        writer.Write(newData);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UpdateExtraRoleData(playerId, roleId, newData);
    }

    public static void CloseUpKill(PlayerControl murder, PlayerControl target, Game.PlayerData.PlayerStatus status, bool playSoundForEveryone = true)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CloseUpKill, Hazel.SendOption.Reliable, -1);
        writer.Write(murder.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(status.Id);
        writer.Write(playSoundForEveryone);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.CloseUpKill(murder.PlayerId, target.PlayerId, status.Id, playSoundForEveryone);
    }

    public static void AddAndUpdateRoleData(byte playerId, int dataId, int addData)
    {
        int newData = Game.GameData.data.playersArray[playerId].GetRoleData(dataId) + addData;
        UpdateRoleData(playerId, dataId, newData);
    }

    public static void ChangeRole(PlayerControl player, Roles.Role role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ChangeRole, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(role.id);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ChangeRole(player.PlayerId, role.id);
    }

    public static void ImmediatelyChangeRole(PlayerControl player, Roles.Role role, params Tuple<Tuple<Roles.ExtraRole, ulong>, bool>[] extraRoles)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ImmediatelyChangeRole, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(role.id);
        writer.Write(extraRoles.Length);
        foreach (var t in extraRoles)
        {
            writer.Write(t.Item1.Item1.id);
            writer.Write(t.Item1.Item2);
            writer.Write(t.Item2);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ImmediatelyChangeRole(player.PlayerId, role.id, extraRoles);
    }

    public static void SwapRole(PlayerControl player1, PlayerControl player2)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapRole, Hazel.SendOption.Reliable, -1);
        writer.Write(player1.PlayerId);
        writer.Write(player2.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SwapRole(player1.PlayerId, player2.PlayerId);
    }

    public static void SetExtraRole(PlayerControl player, Roles.ExtraRole role, ulong initializeValue)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExtraRole, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(role.id);
        writer.Write(initializeValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SetExtraRole(player.PlayerId, role, initializeValue);
    }

    public static void ImmediatelyUnsetExtraRole(PlayerControl player, Roles.ExtraRole role)
    {
        UnsetExtraRole(player, role, false);
    }

    public static void UnsetExtraRole(PlayerControl player, Roles.ExtraRole role, bool onMeetingEnd)
    {
        if (!player.GetModData().extraRole.Contains(role)) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UnsetExtraRole, Hazel.SendOption.Reliable, -1);
        writer.Write(role.id);
        writer.Write(player.PlayerId);
        writer.Write(onMeetingEnd);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UnsetExtraRole(role, player.PlayerId, onMeetingEnd);
    }

    public static void ChangeExtraRole(PlayerControl player, Roles.ExtraRole removeRole, Roles.ExtraRole addRole, ulong initializeValue)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ChangeExtraRole, Hazel.SendOption.Reliable, -1);
        writer.Write(removeRole.id);
        writer.Write(addRole.id);
        writer.Write(initializeValue);
        writer.Write(player.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ChangeExtraRole(removeRole, addRole, initializeValue, player.PlayerId);
    }

    public static void SwapExtraRole(PlayerControl player1, PlayerControl player2, Roles.ExtraRole role, bool onMeetingEnd)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapExtraRole, Hazel.SendOption.Reliable, -1);
        writer.Write(player1.PlayerId);
        writer.Write(player2.PlayerId);
        writer.Write(role.id);
        writer.Write(onMeetingEnd);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SwapExtraRole(player1, player2, role, onMeetingEnd);
    }


    public static void AddExtraRole(PlayerControl player, Roles.ExtraRole addRole, ulong initializeValue)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ChangeExtraRole, Hazel.SendOption.Reliable, -1);
        writer.Write(byte.MaxValue);
        writer.Write(addRole.id);
        writer.Write(initializeValue);
        writer.Write(player.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ChangeExtraRole(null, addRole, initializeValue, player.PlayerId);
    }

    public static void SetGhostRole(PlayerControl player, Roles.GhostRole? ghostRole)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGhostRole, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(ghostRole != null ? ghostRole.id : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SetGhostRole(player.PlayerId, ghostRole);
    }

    public static void RevivePlayer(PlayerControl player, bool reviveOnCurrentPosition = false, bool changeStatusToRevive = true, bool gushOnRevive = false)
    {
        Vector2 pos = player.transform.position;

        if (!reviveOnCurrentPosition)
        {
            foreach (DeadBody body in Helpers.AllDeadBodies())
            {
                if (body.ParentId != player.PlayerId) continue;

                pos = body.transform.position;
                break;
            }
        }


        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevivePlayer, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write(changeStatusToRevive);
        writer.Write(gushOnRevive);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.RevivePlayer(player.PlayerId, pos, changeStatusToRevive, gushOnRevive);
    }

    public static void EmitSpeedFactor(PlayerControl player, Game.SpeedFactor speedFactor)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EmitSpeedFactor, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(speedFactor.IsPermanent);
        writer.Write(speedFactor.DupId);
        writer.Write(speedFactor.Duration);
        writer.Write(speedFactor.SpeedRate);
        writer.Write(speedFactor.CanCrossOverMeeting);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.EmitSpeedFactor(player.PlayerId, speedFactor);
    }

    public static void EmitAttributeFactor(PlayerControl player, Game.PlayerAttributeFactor attributeFactor)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EmitPlayerAttributeFactor, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(attributeFactor.Attribute.Id);
        writer.Write(attributeFactor.IsPermanent);
        writer.Write(attributeFactor.Duration);
        writer.Write(attributeFactor.DupId);
        writer.Write(attributeFactor.CanCrossOverMeeting);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.EmitPlayerAttributeFactor(player.PlayerId, attributeFactor);
    }

    public static void RequireUniqueRPC(byte playerId, byte actionId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequireUniqueRPC, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(actionId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.RequireUniqueRPC(playerId, actionId);
    }

    public static void MultipleVote(PlayerControl player, byte count)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MultipleVote, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(count);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.MultipleVote(player.PlayerId, count);
    }

    public static void ChangeTasks(List<GameData.TaskInfo> tasks, bool resetTasks)
    {
        ChangeTasks(tasks, tasks.Count, tasks.Count, resetTasks);
    }

    public static void ChangeTasks(List<GameData.TaskInfo> tasks, int allQuota)
    {
        ChangeTasks(tasks, allQuota, tasks.Count, false);
    }

    public static void SetTasks(byte playerId, int allTasks, bool isCrewmateTask, bool isInfiniteQuota)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTasks, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(allTasks);
        writer.Write(isCrewmateTask);
        writer.Write(isInfiniteQuota);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SetTasks(PlayerControl.LocalPlayer.PlayerId, allTasks, isCrewmateTask, isInfiniteQuota);
    }

    public static void ChangeTasks(List<GameData.TaskInfo> tasks, int allQuota, int allTasks, bool resetTasks)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ChangeTasks, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(allTasks);
        writer.Write(allQuota);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ChangeTasks(PlayerControl.LocalPlayer.PlayerId, allTasks, allQuota);

        if (resetTasks) PlayerControl.LocalPlayer.Data.SetLocalTask(tasks);
    }

    public static void RefreshTasks(byte playerId, int newTasks, int addQuota, float longTaskChance)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RefreshTasks, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(newTasks);
        writer.Write(addQuota);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.RefreshTasks(playerId, newTasks, addQuota);

        int shortTasks = 0, longTasks = 0;
        int sum = 0;
        for (int i = 0; i < newTasks; i++)
        {
            if (NebulaPlugin.rnd.NextDouble() < longTaskChance)
                longTasks++;
            else
                shortTasks++;
        }

        var tasks = new Il2CppSystem.Collections.Generic.List<byte>();

        int num = 0;
        var usedTypes = new Il2CppSystem.Collections.Generic.HashSet<TaskTypes>();
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unused;

        unused = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
        foreach (var t in ShipStatus.Instance.LongTasks)
            unused.Add(t);
        Extensions.Shuffle<NormalPlayerTask>(unused.Cast<Il2CppSystem.Collections.Generic.IList<NormalPlayerTask>>(), 0);
        ShipStatus.Instance.AddTasksFromList(ref num, longTasks, tasks, usedTypes, unused);

        unused = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
        foreach (var t in ShipStatus.Instance.NormalTasks)
        {
            if (t.TaskType == TaskTypes.PickUpTowels) continue;
            unused.Add(t);
        }
        Extensions.Shuffle<NormalPlayerTask>(unused.Cast<Il2CppSystem.Collections.Generic.IList<NormalPlayerTask>>(), 0);
        ShipStatus.Instance.AddTasksFromList(ref num, shortTasks, tasks, usedTypes, unused);

        GameData.PlayerInfo p = PlayerControl.LocalPlayer.Data;

        //GameData.Instance.SetTasksは初期設定のパッチが通るため使用しない
        p.Tasks = new Il2CppSystem.Collections.Generic.List<GameData.TaskInfo>(tasks.Count);

        int n = 0;
        foreach (var t in tasks)
        {
            p.Tasks.Add(new GameData.TaskInfo(t, (uint)n));
            n++;
        }
        p.Object.SetTasks(p.Tasks);
        GameData.Instance.SetDirtyBit(1U << (int)PlayerControl.LocalPlayer.PlayerId);
    }

    public static void CompleteTask(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CompleteTask, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.CompleteTask(playerId);
    }

    public static void ExemptAllTask(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ExemptAllTask, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ExemptAllTask(playerId);
    }

    public static Objects.CustomObject ObjectInstantiate(Objects.CustomObject.Type objectType, Vector3 position)
    {
        ulong id;
        while (true)
        {
            id = (ulong)NebulaPlugin.rnd.Next((int)Objects.CustomObject.MAX_PLAYER_OBJECTS);
            if (!Objects.CustomObject.Objects.ContainsKey((id + (ulong)PlayerControl.LocalPlayer.PlayerId * Objects.CustomObject.MAX_PLAYER_OBJECTS))) break;
        }
        id = id + (ulong)PlayerControl.LocalPlayer.PlayerId;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ObjectInstantiate, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(objectType.Id);
        writer.Write(id);
        writer.Write(position.x);
        writer.Write(position.y);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        return RPCEvents.ObjectInstantiate(PlayerControl.LocalPlayer.PlayerId, objectType.Id, id, position.x, position.y);
    }

    public static void ObjectUpdate(Objects.CustomObject customObject, int command)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ObjectUpdate, Hazel.SendOption.Reliable, -1);
        writer.Write(customObject.Id);
        writer.Write(command);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ObjectUpdate(customObject.Id, command);
    }

    public static void ObjectDestroy(Objects.CustomObject customObject)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ObjectDestroy, Hazel.SendOption.Reliable, -1);
        writer.Write(customObject.Id);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.ObjectDestroy(customObject.Id);
    }

    public static void Synchronize(Game.SynchronizeTag tag, byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Synchronize, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((int)tag);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.Synchronize(playerId, (int)tag);
    }

    public static void CountDownMessage(byte count)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CountDownMessage, Hazel.SendOption.Reliable, -1);
        writer.Write(count);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.CountDownMessage(count);
    }

    public static void UpdateRestrictTimer(byte deviceId, float timer)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UpdateRestrictTimer, Hazel.SendOption.Reliable, -1);
        writer.Write(deviceId);
        writer.Write(timer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UpdateRestrictTimer(deviceId, timer);
    }

    public static void UpdateAdminRestrictTimer(float timer)
    {
        UpdateRestrictTimer(0, timer);
    }

    public static void UpdateVitalsRestrictTimer(float timer)
    {
        UpdateRestrictTimer(1, timer);
    }

    public static void UpdateCameraAndDoorLogRestrictTimer(float timer)
    {
        UpdateRestrictTimer(2, timer);
    }

    public static void UndergroundAction(bool underground)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UndergroundAction, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(underground);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.UndergroundAction(PlayerControl.LocalPlayer.PlayerId, underground);
    }

    public static void DeathGuage(byte attackerId, byte playerId, float value)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DeathGuage, Hazel.SendOption.Reliable, -1);
        writer.Write(attackerId);
        writer.Write(playerId);
        writer.Write(value);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.DeathGuage(attackerId, playerId, value);
    }

    public static void SniperSettleRifle()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSettleRifle, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SniperSettleRifle(PlayerControl.LocalPlayer.PlayerId);
    }

    public static void SniperShot()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperShot, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.SniperShot(PlayerControl.LocalPlayer.PlayerId);
    }

    public static void RaiderSettleAxe()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RaiderSettleAxe, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.RaiderSettleAxe(PlayerControl.LocalPlayer.PlayerId);
    }

    public static void RaiderThrow(Vector2 pos, float angle)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RaiderThrow, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write(angle);
        AmongUsClient.Instance.FinishRpcImmediately(writer);

        RPCEvents.RaiderThrow(PlayerControl.LocalPlayer.PlayerId, pos, angle);
    }

    public static void Morph(Game.PlayerData.PlayerOutfitData outfit)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Morph, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        outfit.Serialize(writer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.Morph(PlayerControl.LocalPlayer.PlayerId, outfit);
    }

    public static void MorphCancel()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MorphCancel, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.MorphCancel(PlayerControl.LocalPlayer.PlayerId);
    }

    public static void CreateSidekick(byte targetId, byte jackalId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CreateSidekick, Hazel.SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(jackalId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.CreateSidekick(targetId, jackalId);
    }

    public static void DisturberInvoke(ulong objectId1, ulong objectId2)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DisturberInvoke, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(objectId1);
        writer.Write(objectId2);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.DisturberInvoke(PlayerControl.LocalPlayer.PlayerId, objectId1, objectId2);
    }

    public static void GlobalEvent(Events.GlobalEvent.Type type, float duration, ulong option = 0)
    {
        MessageWriter camouflageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GlobalEvent, Hazel.SendOption.Reliable, -1);
        camouflageWriter.Write(type.Id);
        camouflageWriter.Write(duration);
        camouflageWriter.Write(option);
        AmongUsClient.Instance.FinishRpcImmediately(camouflageWriter);
        RPCEvents.GlobalEvent(type.Id, duration, option);
    }

    public static void CleanDeadBody(byte targetId)
    {
        MessageWriter eatWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CleanDeadBody, Hazel.SendOption.Reliable, -1);
        eatWriter.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(eatWriter);
        RPCEvents.CleanDeadBody(targetId);
    }

    public static void SnapToDeadBody(byte targetId, Vector2 pos)
    {
        MessageWriter snapWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SnapToDeadBody, Hazel.SendOption.Reliable, -1);
        snapWriter.Write(targetId);
        snapWriter.Write(pos.x);
        snapWriter.Write(pos.y);
        AmongUsClient.Instance.FinishRpcImmediately(snapWriter);
        RPCEvents.SnapToDeadBody(targetId, pos);
    }

    public static void UpdatePlayerVisibility(byte playerId, bool visibility)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UpdatePlayerVisibility, Hazel.SendOption.Reliable, -1);
        messageWriter.Write(playerId);
        messageWriter.Write(visibility);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        RPCEvents.UpdatePlayerVisibility(playerId, visibility);
    }

    public static void EditCoolDown(Roles.CoolDownType coolDownType, float time)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EditCoolDown, Hazel.SendOption.Reliable, -1);
        messageWriter.Write((byte)coolDownType);
        messageWriter.Write(time);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        RPCEvents.EditCoolDown(coolDownType, time);
    }

    public static void RitualSharePerks(byte playerId, int[] perks)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RitualSharePerks, Hazel.SendOption.Reliable, -1);
        writer.Write(playerId);
        for (int i = 0; i < 4; i++)
        {
            writer.Write(perks[i]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.RitualSharePerks(playerId, perks);
    }

    public static void RitualUpdateTaskProgress(int taskNum)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RitualUpdate, Hazel.SendOption.Reliable, -1);
        writer.Write((int)0);
        writer.Write(taskNum);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.RitualUpdateTaskProgress(taskNum);
    }

    public static void DecoySwap(Objects.CustomObject decoy)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DecoySwap, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(decoy.Id);
        writer.Write(PlayerControl.LocalPlayer.transform.position.x);
        writer.Write(PlayerControl.LocalPlayer.transform.position.y);
        writer.Write(decoy.GameObject.transform.position.x);
        writer.Write(decoy.GameObject.transform.position.y);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.DecoySwap(PlayerControl.LocalPlayer, decoy, PlayerControl.LocalPlayer.transform.position.x, PlayerControl.LocalPlayer.transform.position.y, decoy.GameObject.transform.position.x, decoy.GameObject.transform.position.y);
    }

    public static void AddGuardian(PlayerControl player, PlayerControl guardian)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillGuard, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write((byte)0);
        writer.Write(guardian.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.KillGuard(player.PlayerId, 0, guardian.PlayerId);
    }

    public static void RemoveGuardian(PlayerControl player, PlayerControl guardian)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillGuard, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write((byte)3);
        writer.Write(guardian.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.KillGuard(player.PlayerId, 3, guardian.PlayerId);
    }

    public static void AddSingleUseGuard(PlayerControl player, int num)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillGuard, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write((byte)1);
        writer.Write((byte)num);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.KillGuard(player.PlayerId, 0, (byte)num);
    }

    public static void ConsumeSingleUseGuard(byte player, int num)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillGuard, Hazel.SendOption.Reliable, -1);
        writer.Write(player);
        writer.Write((byte)2);
        writer.Write((byte)num);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.KillGuard(player, 0, (byte)num);
    }

    public static void ConsumeSingleUseGuard(PlayerControl player, int num)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.KillGuard, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write((byte)2);
        writer.Write((byte)num);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.KillGuard(player.PlayerId, 0, (byte)num);
    }

    public static void UpdatePlayersIconInfo(Roles.Template.HasAlignedHologram role, List<byte> activePlayers, Dictionary<byte, float>? progress)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UpdatePlayersIconInfo, Hazel.SendOption.Reliable, -1);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(role.id);
        writer.Write((int)PlayerControl.AllPlayerControls.Count);

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            writer.Write(p.PlayerId);
            if (p == PlayerControl.LocalPlayer || !role.GetIconState(p.PlayerId)) writer.Write(byte.MaxValue);
            else if (activePlayers.Contains(p.PlayerId)) writer.Write((byte)100);
            else if (progress != null && progress.ContainsKey(p.PlayerId)) writer.Write((byte)(progress[p.PlayerId] * 100f));
            else writer.Write((byte)0);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void Paint(PlayerControl player, Game.PlayerData.PlayerOutfitData outfit)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Paint, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        outfit.Serialize(writer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.Paint(player, outfit);
    }

    public static void Poltergeist(byte deadBodyId, Vector2 vector)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Poltergeist, Hazel.SendOption.Reliable, -1);
        writer.Write(deadBodyId);
        writer.Write(vector.x);
        writer.Write(vector.y);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.Poltergeist(deadBodyId, vector);
    }

    public static void InstantiateDeadBody(byte targetId, Vector3 position)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.InstantiateDeadBody, Hazel.SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(position.x);
        writer.Write(position.y);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCEvents.InstantiateDeadBody(targetId, position);
    }
}