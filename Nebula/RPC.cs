using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Hazel;
using Nebula.Module;

namespace Nebula
{
    public enum CustomRPC
    {
        // Main Controls

        ResetVaribles = 60,
        VersionHandshake,
        ForceEnd,
        ShareOptions,
        SetRole,
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
        SwapRole,
        RevivePlayer,
        EmitSpeedFactor,
        CleanDeadBody,

        // Role functionality

        SealVent = 91,

    }

    //RPCを受け取ったときのイベント
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class RPCHandlerPatch
    {
        static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            byte packetId = callId;
            switch (packetId)
            {

                case (byte)CustomRPC.ResetVaribles:
                    RPCEvents.ResetVaribles();
                    break;
                case (byte)CustomRPC.VersionHandshake:
                    int length = reader.ReadInt32();
                    byte[] version = new byte[length];
                    for(byte i = 0; i < length; i++)
                    {
                        version[i] = reader.ReadByte();
                    }
                    int clientId = reader.ReadPackedInt32();
                    UnityEngine.Debug.Log("Received Handshake:"+version[0]+"." + version[1] + "." + version[2] + "." + version[3] );
                    RPCEvents.VersionHandshake(version, new Guid(reader.ReadBytes(16)), clientId);
                    break;
                case (byte)CustomRPC.ForceEnd:
                    RPCEvents.ForceEnd(reader.ReadByte());
                    break;
                case (byte)CustomRPC.ShareOptions:
                    RPCEvents.ShareOptions((int)reader.ReadPackedUInt32(), reader);
                    break;
                case (byte)CustomRPC.SetRole:
                    RPCEvents.SetRole(Roles.Role.GetRoleById(reader.ReadByte()), reader.ReadByte());
                    break;
                case (byte)CustomRPC.SetExtraRole:
                    RPCEvents.SetExtraRole(Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.UnsetExtraRole:
                    RPCEvents.UnsetExtraRole(Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadByte());
                    break;
                case (byte)CustomRPC.ChangeExtraRole:
                    RPCEvents.ChangeExtraRole(Roles.ExtraRole.GetRoleById(reader.ReadByte()), Roles.ExtraRole.GetRoleById(reader.ReadByte()), reader.ReadUInt64(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.UncheckedMurderPlayer:
                    RPCEvents.UncheckedMurderPlayer(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.UncheckedExilePlayer:
                    RPCEvents.UncheckedExilePlayer(reader.ReadByte());
                    break;
                case (byte)CustomRPC.UncheckedCmdReportDeadBody:
                    RPCEvents.UncheckedCmdReportDeadBody(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.CloseUpKill:
                    RPCEvents.CloseUpKill(reader.ReadByte(), reader.ReadByte());
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
                case (byte)CustomRPC.SwapRole:
                    RPCEvents.SwapRole(reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.RevivePlayer:
                    RPCEvents.RevivePlayer(reader.ReadByte());
                    break;
                case (byte)CustomRPC.EmitSpeedFactor:
                    RPCEvents.EmitSpeedFactor(reader.ReadByte(), new Game.SpeedFactor(reader.ReadBoolean(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean()));
                    break;


                case (byte)CustomRPC.SealVent:
                    RPCEvents.SealVent(reader.ReadByte(), reader.ReadInt32());
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
            Patches.MeetingHudPatch.Initialize();
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

        public static void SetRole(Roles.Role role, byte playerId)
        {
            Game.GameData.data.RegisterPlayer(playerId, role);
        }

        /// <summary>
        /// 初期化時に使用。ゲーム中では使わないこと
        /// </summary>
        /// <param name="role"></param>
        /// <param name="initializeValue"></param>
        /// <param name="playerId"></param>
        public static void SetExtraRole(Roles.ExtraRole role, ulong initializeValue,byte playerId)
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
            SetExtraRole(addRole, initializeValue, playerId);


            PlayerControl player = Helpers.playerById(playerId);
            addRole.GlobalInitialize(player);
            if (playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                addRole.Initialize(player);
                addRole.ButtonInitialize(Patches.HudManagerStartPatch.Manager);
                addRole.ButtonActivate();
            }
        }

        public static void UncheckedMurderPlayer(byte murdererId, byte targetId, byte showAnimation)
        {
            PlayerControl source = Helpers.playerById(murdererId);
            PlayerControl target = Helpers.playerById(targetId);
            if (source != null && target != null)
            {
                if (showAnimation == 0) Patches.KillAnimationCoPerformKillPatch.hideNextAnimation = true;
                source.MurderPlayer(target);
            }
        }

        public static void UncheckedExilePlayer(byte playerId)
        {
            PlayerControl player = Helpers.playerById(playerId);
            if (player != null)
            {
                player.Exiled();

                if (player.GetModData().role.OnExiledPost(new byte[0], playerId))
                {
                    player.GetModData().role.OnDied(playerId);

                    if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        player.GetModData().role.OnExiledPost(new byte[0]);
                        player.GetModData().role.OnDied();
                    }

                    Game.GameData.data.players[playerId].Die(Game.DeadPlayerData.DeathReason.Exiled);
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

        public static void CloseUpKill(byte murdererId, byte targetId)
        {
            UncheckedMurderPlayer(murdererId, targetId, 0);
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
                data.role.ButtonCleanUp();
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
                NebulaPlugin.Instance.Logger.Print("Start Changing Role");

                Game.PlayerData data = Game.GameData.data.players[playerId];

                if (playerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    data.role.FinalizeInGame(PlayerControl.LocalPlayer);
                }

                //ロールを変更
                SetUpRole(data, Helpers.playerById(playerId), Roles.Role.GetRoleById(roleId));

                NebulaPlugin.Instance.Logger.Print("Role Change Finished!");
            });
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
                Game.GameData.data.deadPlayers.Remove(playerId);

                UnityEngine.Object.Destroy(body.gameObject);
            }
        }

        public static void EmitSpeedFactor(byte playerId,Game.SpeedFactor speedFactor)
        {
            Game.GameData.data.players[playerId].Speed.Register(speedFactor);
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
                UnityEngine.Debug.Log("Error while deserializing options: " + e.Message);
            }
        }

        public static void SealVent(byte playerId,int ventId)
        {
            Events.Schedule.RegisterPostMeetingAction(() =>
            {
                Vent vent = ShipStatus.Instance.AllVents.FirstOrDefault((x) => x != null && x.Id == ventId);
                if (vent == null) return;

                Roles.Roles.SecurityGuard.SetSealedVentSprite(vent,1f);
                vent.GetVentData().Sealed = true;
            });

            Game.GameData.data.players[playerId].AddRoleData(Roles.Roles.SecurityGuard.remainingScrewsDataId, -1);
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
    }

    public class RPCEventInvoker
    {
        public static void UncheckedMurderPlayer(byte murdererId, byte targetId, bool showAnimation)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
            writer.Write(murdererId);
            writer.Write(targetId);
            writer.Write(showAnimation ? Byte.MaxValue : (byte)0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.UncheckedMurderPlayer(murdererId, targetId, showAnimation ? Byte.MaxValue : (byte)0);
        }

        public static void UncheckedExilePlayer(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedExilePlayer, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.UncheckedExilePlayer(playerId);
        }

        public static void UncheckedCmdReportDeadBody(byte reporterId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedCmdReportDeadBody, Hazel.SendOption.Reliable, -1);
            writer.Write(reporterId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.UncheckedCmdReportDeadBody(reporterId, targetId);
        }

        public static void UpdateRoleData(byte playerId, int dataId,int newData)
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
            writer.Write(murder.PlayerId);
            writer.Write(target.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.CloseUpKill(murder.PlayerId, target.PlayerId);
        }

        public static void AddAndUpdateRoleData(byte playerId, int dataId, int addData)
        {
            int newData = Game.GameData.data.players[playerId].GetRoleData(dataId) + addData;
            UpdateRoleData(playerId,dataId,newData);
        }

        public static void ChangeRole(PlayerControl player,Roles.Role role)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ChangeRole, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(role.id);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.ChangeRole(player.PlayerId,role.id);
        }

        public static void SwapRole(PlayerControl player1, PlayerControl player2)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapRole, Hazel.SendOption.Reliable, -1);
            writer.Write(player1.PlayerId);
            writer.Write(player2.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.SwapRole(player1.PlayerId, player2.PlayerId);
        }

        public static void SetExtraRole(PlayerControl player, Roles.ExtraRole role,ulong initializeValue)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExtraRole, Hazel.SendOption.Reliable, -1);
            writer.Write(role.id);
            writer.Write(initializeValue);
            writer.Write(player.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.SetExtraRole(role, initializeValue, player.PlayerId);
        }

        public static void UnsetExtraRole(PlayerControl player,Roles.ExtraRole role)
        {
            if (!player.GetModData().extraRole.Contains(role)) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UnsetExtraRole, Hazel.SendOption.Reliable, -1);
            writer.Write(role.id);
            writer.Write(player.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.UnsetExtraRole(role,player.PlayerId);
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

        public static void EmitSpeedFactor(PlayerControl player,Game.SpeedFactor speedFactor)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EmitSpeedFactor, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(speedFactor.IsPermanent);
            writer.Write(speedFactor.Duration);
            writer.Write(speedFactor.SpeedRate);
            writer.Write(speedFactor.CanCrossOverMeeting);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCEvents.EmitSpeedFactor(player.PlayerId, speedFactor);
        }
    }
}