using HarmonyLib;
using Hazel;
using Nebula.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nebula
{
    public enum CustomRPC
    {
        // Main Controls

        ResetVaribles = 60,
        SetRandomMap,
        VersionHandshake,
        SetMyColor,
        SynchronizeTimer,
        UpdatePlayerControl,
        ForceEnd,
        WinTrigger,
        ShareOptions,
        SetRoles,
        SetExtraRole,
        UnsetExtraRole,
        ChangeExtraRole,
        UncheckedMurderPlayer,
        UncheckedExilePlayer,
        UncheckedCmdReportDeadBody,
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
        CleanDeadBody,
        FixLights,
        RequireUniqueRPC,
        ExemptTasks,
        RefreshTasks,
        CompleteTask,
        ObjectInstantiate,

        // Role functionality

        SealVent = 101,
        MultipleVote,
        SniperSettleRifle,
        SniperShot,
        Morph,
        CreateSidekick
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

                case (byte)CustomRPC.ResetVaribles:
                    RPCEvents.ResetVaribles();
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
                    RPCEvents.SetMyColor(reader.ReadByte(), new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),1f),reader.ReadByte());
                    break;
                case (byte)CustomRPC.SynchronizeTimer:
                    RPCEvents.SynchronizeTimer(reader.ReadSingle());
                    break;
                case (byte)CustomRPC.UpdatePlayerControl:
                    RPCEvents.UpdatePlayerControl(reader.ReadByte(),reader.ReadSingle());
                    break;
                case (byte)CustomRPC.ForceEnd:
                    RPCEvents.ForceEnd(reader.ReadByte());
                    break;
                case (byte)CustomRPC.WinTrigger:
                    RPCEvents.WinTrigger(reader.ReadByte());
                    break;
                case (byte)CustomRPC.ShareOptions:
                    RPCEvents.ShareOptions((int)reader.ReadPackedUInt32(), reader);
                    break;
                case (byte)CustomRPC.SetRoles:
                    RPCEvents.ResetVaribles();
                    int num=reader.ReadInt32();
                    for (int i= 0;i<num;i++)
                    {
                        RPCEvents.SetRole(reader.ReadByte(),Roles.Role.GetRoleById(reader.ReadByte()));
                    }
                    num = reader.ReadInt32();
                    for (int i= 0; i < num; i++)
                    {
                        RPCEvents.SetExtraRole(reader.ReadByte(), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64());
                    }
                    break;
                case (byte)CustomRPC.SetExtraRole:
                    RPCEvents.SetExtraRole(reader.ReadByte(),Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64());
                    break;
                case (byte)CustomRPC.UnsetExtraRole:
                    RPCEvents.UnsetExtraRole(Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadByte());
                    break;
                case (byte)CustomRPC.ChangeExtraRole:
                    RPCEvents.ChangeExtraRole(Roles.ExtraRole.GetRoleById(reader.ReadByte()), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64(), reader.ReadByte());
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
                case (byte)CustomRPC.CloseUpKill:
                    RPCEvents.CloseUpKill(reader.ReadByte());
                    break;
                case (byte)CustomRPC.UpdateRoleData:
                    RPCEvents.UpdateRoleData(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                    break;
                case (byte)CustomRPC.UpdateExtraRoleData:
                    RPCEvents.UpdateExtraRoleData(reader.ReadByte(), reader.ReadByte(), reader.ReadUInt64());
                    break;
                case (byte)CustomRPC.GlobalEvent:
                    RPCEvents.GlobalEvent(reader.ReadByte(), reader.ReadSingle());
                    break;
                case (byte)CustomRPC.DragAndDropPlayer:
                    RPCEvents.DragAndDropPlayer(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.ChangeRole:
                    RPCEvents.ChangeRole(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.ImmediatelyChangeRole:
                    RPCEvents.ImmediatelyChangeRole(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.SwapRole:
                    RPCEvents.SwapRole(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.RevivePlayer:
                    RPCEvents.RevivePlayer(reader.ReadByte());
                    break;
                case (byte)CustomRPC.EmitSpeedFactor:
                    RPCEvents.EmitSpeedFactor(reader.ReadByte(), new Game.SpeedFactor(reader.ReadBoolean(), reader.ReadByte(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean()));
                    break;
                case (byte)CustomRPC.FixLights:
                    RPCEvents.FixLights();
                    break;
                case (byte)CustomRPC.RequireUniqueRPC:
                    RPCEvents.RequireUniqueRPC(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.ExemptTasks:
                    RPCEvents.ExemptTasks(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                    break;
                case (byte)CustomRPC.RefreshTasks:
                    RPCEvents.RefreshTasks(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                    break;
                case (byte)CustomRPC.CompleteTask:
                    RPCEvents.CompleteTask(reader.ReadByte());
                    break;
                case (byte)CustomRPC.ObjectInstantiate:
                    RPCEvents.ObjectInstantiate(reader.ReadByte(), reader.ReadByte(), reader.ReadUInt64(), reader.ReadSingle(), reader.ReadSingle());
                    break;
                case (byte)CustomRPC.CleanDeadBody:
                    RPCEvents.CleanDeadBody(reader.ReadByte());
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
                case (byte)CustomRPC.Morph:
                    RPCEvents.Morph(reader.ReadByte(),reader.ReadByte());
                    break;
                case (byte)CustomRPC.CreateSidekick:
                    RPCEvents.CreateSidekick(reader.ReadByte(), reader.ReadByte());
                    break;
            }
        }
    }

    static class RPCEvents
    {
        public static void ResetVaribles()
        {
            Game.GameData.Initialize();
            Events.GlobalEvent.Initialize();
            Events.LocalEvent.Initialize();
            Events.Schedule.Initialize();
            Objects.CustomMessage.Initialize();
            Objects.CustomObject.Initialize();
            Patches.MeetingHudPatch.Initialize();
            Patches.EmergencyPatch.Initialize();
            Objects.Ghost.Initialize();
        }
            
        public static void SetMyColor(byte playerId, Color color, byte shadowType)
        {
            DynamicColors.SetOthersColor(color, DynamicColors.GetShadowColor(color, shadowType), playerId);
        }
        public static void SynchronizeTimer(float timer)
        {
            if(Game.GameData.data!=null)
                Game.GameData.data.Timer = timer;
        }


        public static void SetRandomMap(byte mapId)
        {
            PlayerControl.GameOptions.MapId = mapId;
        }

        public static void VersionHandshake(byte[] version, Guid guid, int clientId)
        {
            Patches.GameStartManagerPatch.playerVersions[clientId] = new Patches.GameStartManagerPatch.PlayerVersion(version, guid);
        }

        public static void ForceEnd(byte playerId)
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId != playerId)
                {
                    player.RemoveInfected();
                    player.MurderPlayer(player);
                    player.Data.IsDead = true;
                }
            }
        }

        public static void WinTrigger(byte roleId)
        {
            Roles.Role role = Roles.Role.GetRoleById(roleId);
            if(role is Roles.Template.HasWinTrigger)
            {
                ((Roles.Template.HasWinTrigger)role).WinTrigger=true;
            }
        }

        public static void UpdatePlayerControl(byte playerId,float mouseAngle)
        {
            Helpers.playerById(playerId).GetModData().MouseAngle = mouseAngle;
        }

        public static void SetRole(byte playerId,Roles.Role role)
        {
            if (role.category == Roles.RoleCategory.Impostor)
            {
                DestroyableSingleton<RoleManager>.Instance.SetRole(Helpers.playerById(playerId), RoleTypes.Impostor);
            }
            else
            {
                DestroyableSingleton<RoleManager>.Instance.SetRole(Helpers.playerById(playerId), RoleTypes.Crewmate);
            }
            Game.GameData.data.RegisterPlayer(playerId, role);
        }

        /// <summary>
        /// 初期化時に使用。ゲーム中では使わないこと
        /// </summary>
        /// <param name="role"></param>
        /// <param name="initializeValue"></param>
        /// <param name="playerId"></param>
        public static void SetExtraRole(byte playerId,Roles.ExtraRole role, ulong initializeValue)
        {
            Game.GameData.data.players[playerId].extraRole.Add(role);
            Game.GameData.data.players[playerId].SetExtraRoleData(role.id,initializeValue);

            role.Setup(Game.GameData.data.players[playerId]);
        }

        public static void UnsetExtraRole(Roles.ExtraRole role, byte playerId)
        {
            role.OnUnset(playerId);

            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                role.ButtonDeactivate();
            }

            Game.GameData.data.players[playerId].extraRole.Remove(role);
        }

        public static void ChangeExtraRole(Roles.ExtraRole removeRole, Roles.ExtraRole addRole, ulong initializeValue, byte playerId)
        {
            if (removeRole!=null && Helpers.GetModData(playerId).extraRole.Contains(removeRole))
            {
                UnsetExtraRole(removeRole, playerId);
            }
            SetExtraRole(playerId,addRole, initializeValue);


            PlayerControl player = Helpers.playerById(playerId);
            addRole.GlobalInitialize(player);
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                addRole.Initialize(player);
                addRole.ButtonInitialize(Patches.HudManagerStartPatch.Manager);
                addRole.ButtonActivate();
            }
        }

        public static void UncheckedMurderPlayer(byte murdererId, byte targetId, byte statusId, byte showAnimation)
        {
            PlayerControl source = Helpers.playerById(murdererId);
            PlayerControl target = Helpers.playerById(targetId);
            if (source != null && target != null)
            {
                if (showAnimation == 0) Patches.KillAnimationCoPerformKillPatch.hideNextAnimation = true;
                source.MurderPlayer(target);

                if (Game.GameData.data.players[target.PlayerId].role.hasFakeTask)
                {
                    target.clearAllTasks();
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

                //LocalMethod (自身が死んだとき)
                if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    Helpers.RoleAction(target, (role) => { role.OnDied(); });

                    Events.Schedule.RegisterPreMeetingAction(() => {
                        if (!PlayerControl.LocalPlayer.GetModData().IsAlive)
                            Game.GameData.data.myData.CanSeeEveryoneInfo = true;
                    });
                }

                Game.GameData.data.players[target.PlayerId].Die(Game.PlayerData.PlayerStatus.GetStatusById(statusId), source.PlayerId);
            }
        }

        public static void UncheckedExilePlayer(byte playerId,byte statusId)
        {
            PlayerControl player = Helpers.playerById(playerId);
            if (player != null)
            {
                player.Exiled();

                player.GetModData().role.OnDied(playerId);

                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    player.GetModData().role.OnDied();

                    Events.Schedule.RegisterPostMeetingAction(() =>
                    {
                        if (!PlayerControl.LocalPlayer.GetModData().IsAlive)
                            Game.GameData.data.myData.CanSeeEveryoneInfo = true;
                    });
                }

                if (MeetingHud.Instance != null)
                {
                    foreach (PlayerVoteArea pva in MeetingHud.Instance.playerStates)
                    {
                        if (pva.TargetPlayerId == playerId)
                        {
                            pva.SetDead(pva.DidReport, true);
                            pva.Overlay.gameObject.SetActive(true);
                        }

                        //Give players back their vote if target is shot dead
                        if (pva.VotedFor != playerId) continue;
                        pva.UnsetVote();
                        var voteAreaPlayer = Helpers.playerById(pva.TargetPlayerId);
                        if (!voteAreaPlayer.AmOwner) continue;
                        MeetingHud.Instance.ClearVote();
                    }
                }

                Game.GameData.data.players[playerId].Die(Game.PlayerData.PlayerStatus.GetStatusById(statusId));

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

        public static void CloseUpKill(byte targetId)
        {
            UncheckedExilePlayer(targetId,Game.PlayerData.PlayerStatus.Guessed.Id);

            if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(Helpers.playerById(targetId).KillSfx, false, 0.8f);
        }

        public static void UpdateRoleData(byte playerId, int dataId, int newData)
        {
            Game.GameData.data.players[playerId].SetRoleData(dataId, newData);

            //自身のロールデータ更新時に呼ぶメソッド群
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                Game.GameData.data.myData.getGlobalData().role.OnUpdateRoleData(dataId,newData);
            }
        }

        public static void UpdateExtraRoleData(byte playerId, byte roleId, ulong newData)
        {
            Game.GameData.data.players[playerId].SetExtraRoleData(roleId, newData);

            //自身のロールデータ更新時に呼ぶメソッド群
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                foreach (Roles.ExtraRole role in Game.GameData.data.myData.getGlobalData().extraRole)
                {
                    if (role.id == roleId) role.OnUpdateRoleData(newData);
                }
            }
        }

        public static void GlobalEvent(byte eventId, float duration)
        {
            Events.GlobalEvent.Activate(Events.GlobalEvent.Type.GetType(eventId), duration);
        }

        public static void DragAndDropPlayer(byte playerId, byte targetId)
        {
            if (targetId != Byte.MaxValue)
            {
                Game.GameData.data.players[playerId].DragPlayer(targetId);
            }
            else
            {
                Game.GameData.data.players[playerId].DropPlayer();
            }
        }

        private static void SetUpRole(Game.PlayerData data,PlayerControl player,Roles.Role role,Dictionary<int,int>? roleData=null)
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
            data.CleanRoleDataInGame(roleData);

            data.role = role;
            if (data.role.category == Roles.RoleCategory.Impostor)
            {
                DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Impostor);
            }
            else
            {
                DestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Crewmate);
            }

            if (roleData == null)
            {
                data.role.GlobalInitialize(player);
            }

            if (isMe)
            {
                data.role.Initialize(player);
                data.role.ButtonInitialize(Patches.HudManagerStartPatch.Manager);
                data.role.ButtonActivate();
            }
        }

        public static void ChangeRole(byte playerId, byte roleId)
        {
            Events.Schedule.RegisterPostMeetingAction(() =>
            {
                ImmediatelyChangeRole(playerId,roleId);
            });
        }

        public static void ImmediatelyChangeRole(byte playerId, byte roleId)
        {
            Game.PlayerData data = Game.GameData.data.players[playerId];

            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                data.role.FinalizeInGame(PlayerControl.LocalPlayer);
                data.role.CleanUp();
            }

            //ロールを変更
            SetUpRole(data, Helpers.playerById(playerId), Roles.Role.GetRoleById(roleId));
        }

        public static void SwapRole(byte playerId_1, byte playerId_2)
        {
            Events.Schedule.RegisterPostMeetingAction(() =>
            {
                Game.PlayerData data1 = Game.GameData.data.players[playerId_1];
                Game.PlayerData data2 = Game.GameData.data.players[playerId_2];

                if (playerId_1 == PlayerControl.LocalPlayer.PlayerId)
                {
                    data1.role.FinalizeInGame(PlayerControl.LocalPlayer);
                }
                if (playerId_2 == PlayerControl.LocalPlayer.PlayerId)
                {
                    data2.role.FinalizeInGame(PlayerControl.LocalPlayer);
                }

                Dictionary<int, int> roleData1 = data1.ExtractRoleData(), roleData2 = data2.ExtractRoleData();
                Roles.Role role1 = data1.role, role2 = data2.role;

                //ロールを変更
                SetUpRole(data1, Helpers.playerById(playerId_1), role2, roleData2);
                SetUpRole(data2, Helpers.playerById(playerId_2), role1, roleData1);
            });
        }

        public static void RevivePlayer(byte playerId)
        {
            foreach(DeadBody body in Helpers.AllDeadBodies())
            {
                if (body.ParentId != playerId) continue;

                Game.GameData.data.players[playerId].Revive();
                PlayerControl player = Helpers.playerById(playerId);
                player.transform.position = body.transform.position;
                player.Revive(false);
                player.Data.IsDead = false;
                Game.GameData.data.deadPlayers.Remove(playerId);

                UnityEngine.Object.Destroy(body.gameObject);
            }
        }

        public static void EmitSpeedFactor(byte playerId,Game.SpeedFactor speedFactor)
        {
            Game.GameData.data.players[playerId].Speed.Register(speedFactor);
        }

        //ホストのイベントを本人に受け継ぐ
        public static void RequireUniqueRPC(byte playerId,byte actionId)
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
                    CustomOption option = CustomOption.options.FirstOrDefault(opt => opt.id == (int)optionId);
                    option.updateSelection((int)selection);
                }
            }
            catch (Exception e)
            {
              
            }
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
        }

        public static void ExemptTasks(byte playerId, int cutTasks, int cutQuota)
        {
            Game.GameData.data.players[playerId].Tasks.AllTasks -= cutTasks;
            Game.GameData.data.players[playerId].Tasks.DisplayTasks -= cutTasks;
            Game.GameData.data.players[playerId].Tasks.Quota -= cutQuota;

            if (Game.GameData.data.players[playerId].Tasks.AllTasks < 0)
                Game.GameData.data.players[playerId].Tasks.AllTasks = 0;
            if (Game.GameData.data.players[playerId].Tasks.DisplayTasks < 0)
                Game.GameData.data.players[playerId].Tasks.DisplayTasks = 0;
            if (Game.GameData.data.players[playerId].Tasks.Quota < 0)
                Game.GameData.data.players[playerId].Tasks.Quota = 0;
        }

        public static void RefreshTasks(byte playerId, int displayTasks, int addQuota)
        {
            Game.GameData.data.players[playerId].Tasks.AllTasks += displayTasks;
            Game.GameData.data.players[playerId].Tasks.DisplayTasks = displayTasks;
            Game.GameData.data.players[playerId].Tasks.Quota += addQuota;
        }

        public static void CompleteTask(byte playerId)
        {
            Game.GameData.data.players[playerId].Tasks.Completed++;
        }

        public static void FixLights()
        {
            SwitchSystem switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
            switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
        }

        public static void ObjectInstantiate(byte ownerId,byte objectTypeId,ulong objectId,float positionX,float positionY)
        {
            Objects.CustomObject obj=new Objects.CustomObject(ownerId,Objects.CustomObject.Type.AllTypes[objectTypeId],objectId,new Vector3(positionX,positionY));
            obj.ObjectType.UpdateFunction.Invoke(obj);
        }

        public static void SealVent(byte playerId, int ventId)
        {
            Events.Schedule.RegisterPostMeetingAction(() =>
            {
                Vent vent = ShipStatus.Instance.AllVents.FirstOrDefault((x) => x != null && x.Id == ventId);
                if (vent == null) return;

                Roles.Roles.SecurityGuard.SetSealedVentSprite(vent, 1f);
                vent.GetVentData().Sealed = true;
            });

            Game.GameData.data.players[playerId].AddRoleData(Roles.Roles.SecurityGuard.remainingScrewsDataId, -1);
        }

        public static void MultipleVote(byte playerId, byte count)
        {
            Patches.MeetingHudPatch.SetVoteWeight(playerId, count);
        }

        public static void SniperSettleRifle(byte playerId)
        {
            List<Objects.CustomObject> objList=new List<Objects.CustomObject>();
            foreach(Objects.CustomObject obj in Objects.CustomObject.Objects.Values)
            {
                if (obj.OwnerId != playerId) continue;
                if (obj.ObjectType != Objects.CustomObject.Type.SniperRifle) continue;

                objList.Add(obj);
            }

            foreach(var obj in objList)
            {
                obj.Destroy();
            }
        }


        public static void SniperShot(byte murderId)
        {
            //通知距離を超えていたら何もしない
            if (Helpers.playerById(murderId).transform.position.Distance(PlayerControl.LocalPlayer.transform.position) > Roles.Roles.Sniper.noticeRangeOption.getFloat()) 
                return;

            Objects.Arrow arrow=new Objects.Arrow(Color.white);
            arrow.image.sprite = Roles.Roles.Sniper.getSnipeArrowSprite();

            Vector3 pos = Helpers.playerById(murderId).transform.position;

            HudManager.Instance.StartCoroutine(Effects.Lerp(10f, new Action<float>((p) =>
            {
                arrow.Update(pos);
                arrow.arrow.transform.eulerAngles = new Vector3(0f, 0f, 0f);
                if (p > 0.8f)
                {
                    arrow.image.color = new Color(1f,1f,1f, (1f - p) * 5f);
                }
                if (p == 1f)
                {
                    //矢印を消す
                    UnityEngine.Object.Destroy(arrow.arrow);
                }
            })));
        }

        public static void Morph(byte playerId,byte targetId)
        {
            Events.LocalEvent.Activate(new Roles.ImpostorRoles.Morphing.MorphEvent(playerId,targetId));
        }

        public static void CreateSidekick(byte playerId, byte jackalId)
        {
            if (Roles.NeutralRoles.Jackal.SidekickTakeOverOriginalRoleOption.getBool())
            {
                RPCEvents.ImmediatelyChangeRole(playerId, Roles.Roles.Sidekick.id);
                RPCEvents.UpdateRoleData(playerId, Roles.Roles.Jackal.jackalDataId, jackalId);
            }
            else
            {
                RPCEvents.SetExtraRole(playerId, Roles.Roles.SecondarySidekick, (ulong)jackalId);
            }
            
        }
    }

    public class RPCEventInvoker
    {
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
        public static void SetRoles(Patches.AssignMap assignMap)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRoles, Hazel.SendOption.Reliable, -1);
            writer.Write(assignMap.RoleMap.Count);
            foreach(var entry in assignMap.RoleMap)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
            writer.Write(assignMap.ExtraRoleList.Count);
            foreach (var tuple in assignMap.ExtraRoleList)
            {
                writer.Write(tuple.Item1);
                writer.Write(tuple.Item2.Item1);
                writer.Write(tuple.Item2.Item2);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            //自分自身はもう割り当て済みなので何もしない
        }

        public static void SetMyColor()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMyColor, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write(DynamicColors.MyColor.Color.r);
            writer.Write(DynamicColors.MyColor.Color.g);
            writer.Write(DynamicColors.MyColor.Color.b);
            writer.Write(DynamicColors.MyColor.GetShadowType());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.SetMyColor(PlayerControl.LocalPlayer.PlayerId, DynamicColors.MyColor.Color, DynamicColors.MyColor.GetShadowType());
        }

        public static void WinTrigger(Roles.Role role)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.WinTrigger, Hazel.SendOption.Reliable, -1);
            writer.Write(role.id);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.WinTrigger(role.id);
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

        public static void UncheckedExilePlayer(byte playerId,byte statusId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedExilePlayer, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(statusId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.UncheckedExilePlayer(playerId,statusId);
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

        public static void CloseUpKill(PlayerControl murder, PlayerControl target)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CloseUpKill, Hazel.SendOption.Reliable, -1);
            writer.Write(target.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.CloseUpKill(target.PlayerId);
        }

        public static void AddAndUpdateRoleData(byte playerId, int dataId, int addData)
        {
            int newData = Game.GameData.data.players[playerId].GetRoleData(dataId) + addData;
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

        public static void ImmediatelyChangeRole(PlayerControl player, Roles.Role role)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ImmediatelyChangeRole, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(role.id);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.ImmediatelyChangeRole(player.PlayerId, role.id);
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

        public static void UnsetExtraRole(PlayerControl player, Roles.ExtraRole role)
        {
            if (!player.GetModData().extraRole.Contains(role)) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UnsetExtraRole, Hazel.SendOption.Reliable, -1);
            writer.Write(role.id);
            writer.Write(player.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.UnsetExtraRole(role, player.PlayerId);
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

        public static void RevivePlayer(PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RevivePlayer, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.RevivePlayer(player.PlayerId);
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

        public static void ExemptTasks(byte playerId, int cutTasks, int cutQuota)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ExemptTasks, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(cutTasks);
            writer.Write(cutQuota);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.ExemptTasks(playerId, cutTasks, cutQuota);

            var tasks = new Il2CppSystem.Collections.Generic.List<GameData.TaskInfo>();
            GameData.TaskInfo taskInfo;

            int allTasks = Game.GameData.data.players[playerId].Tasks.DisplayTasks;
            int commonTasks = PlayerControl.GameOptions.NumCommonTasks;
            int longTasks = PlayerControl.GameOptions.NumLongTasks;
            int shortTasks = PlayerControl.GameOptions.NumShortTasks;
            int surplus = commonTasks + longTasks + shortTasks - allTasks;

            int phase = 0;
            while (surplus > 0)
            {
                NebulaPlugin.Instance.Logger.Print("surplus:"+surplus);
                switch (phase)
                {
                    case 0:
                        if (shortTasks > 0) { shortTasks--; surplus--; }
                        break;
                    case 1:
                        if (longTasks > 0) { longTasks--; surplus--; }
                        break;
                    case 2:
                        if (commonTasks > 0) { commonTasks--; surplus--; }
                        break;
                }
                phase=(phase+1)%3;
            }

            int[] array;

            array = Helpers.GetRandomArray(Map.MapData.MapDatabase[PlayerControl.GameOptions.MapId].CommonTaskIdList.Count);
            for(int i = 0; i < commonTasks; i++)
            {
                if (i < array.Length)
                {
                    tasks.Add(new GameData.TaskInfo(Map.MapData.MapDatabase[PlayerControl.GameOptions.MapId].CommonTaskIdList[array[i]], (uint)(tasks.Count + 1)));
                }
                else
                {
                    tasks.Add(new GameData.TaskInfo(Map.MapData.GetRandomCommonTaskId(PlayerControl.GameOptions.MapId), (uint)(tasks.Count + 1)));
                }
            }

            array = Helpers.GetRandomArray(Map.MapData.MapDatabase[PlayerControl.GameOptions.MapId].LongTaskIdList.Count);
            for (int i = 0; i < longTasks; i++)
            {
                if (i < array.Length)
                {
                    tasks.Add(new GameData.TaskInfo(Map.MapData.MapDatabase[PlayerControl.GameOptions.MapId].LongTaskIdList[array[i]], (uint)(tasks.Count + 1)));
                }
                else
                {
                    tasks.Add(new GameData.TaskInfo(Map.MapData.GetRandomLongTaskId(PlayerControl.GameOptions.MapId), (uint)(tasks.Count + 1)));
                }
            }

            array = Helpers.GetRandomArray(Map.MapData.MapDatabase[PlayerControl.GameOptions.MapId].ShortTaskIdList.Count);
            for (int i = 0; i < shortTasks; i++)
            {
                if (i < array.Length)
                {
                    tasks.Add(new GameData.TaskInfo(Map.MapData.MapDatabase[PlayerControl.GameOptions.MapId].ShortTaskIdList[array[i]], (uint)(tasks.Count + 1)));
                }
                else
                {
                    tasks.Add(new GameData.TaskInfo(Map.MapData.GetRandomShortTaskId(PlayerControl.GameOptions.MapId), (uint)(tasks.Count + 1)));
                }
            }

            PlayerControl.LocalPlayer.clearAllTasks();
            PlayerControl.LocalPlayer.SetTasks(tasks);
            PlayerControl.LocalPlayer.Data.Tasks = tasks;
        }

        public static void RefreshTasks(byte playerId, int newTasks, int addQuota, float longTaskChance)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RefreshTasks, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(newTasks);
            writer.Write(addQuota);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.RefreshTasks(playerId, newTasks, addQuota);

            var tasks = new Il2CppSystem.Collections.Generic.List<GameData.TaskInfo>();
            GameData.TaskInfo taskInfo;

            while (tasks.Count < newTasks)
            {
                byte task = 0;
                if (NebulaPlugin.rnd.NextDouble() < longTaskChance)
                {
                    task = Map.MapData.GetRandomLongTaskId(PlayerControl.GameOptions.MapId);
                }
                else
                {
                    task = Map.MapData.GetRandomShortTaskId(PlayerControl.GameOptions.MapId);
                }
                tasks.Add(new GameData.TaskInfo(task, (uint)tasks.Count+1));                
            }

            PlayerControl.LocalPlayer.clearAllTasks();
            PlayerControl.LocalPlayer.SetTasks(tasks);
            PlayerControl.LocalPlayer.Data.Tasks = tasks;
        }

        public static void CompleteTask(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CompleteTask, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.CompleteTask(playerId);
        }

        public static void ObjectInstantiate(Objects.CustomObject.Type objectType,Vector3 position)
        {
            ulong id;
            while (true) {
                id = (ulong)NebulaPlugin.rnd.Next(64);
                if (!Objects.CustomObject.Objects.ContainsKey((id + (ulong)PlayerControl.LocalPlayer.PlayerId * 64))) break;
            }
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ObjectInstantiate, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write(objectType.Id);
            writer.Write(id);
            writer.Write(position.x);
            writer.Write(position.y);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.ObjectInstantiate(PlayerControl.LocalPlayer.PlayerId,objectType.Id,id,position.x,position.y);
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

        public static void Morph(byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Morph, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.Morph(PlayerControl.LocalPlayer.PlayerId, targetId);
        }

        public static void CreateSidekick(byte targetId,byte jackalId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CreateSidekick, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(jackalId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.CreateSidekick(targetId, jackalId);
        }
    }
}