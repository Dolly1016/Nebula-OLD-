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
        UncheckedMurderPlayer,
        UpdateRoleData,
        GlobalEvent,
        DragAndDropPlayer,

        // Role functionality

        SealVent = 91,
        CleanDeadBody

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
                case (byte)CustomRPC.UncheckedMurderPlayer:
                    RPCEvents.UncheckedMurderPlayer(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                    break;
                case (byte)CustomRPC.UpdateRoleData:
                    RPCEvents.UpdataRoleData(reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32());
                    break;
                case (byte)CustomRPC.GlobalEvent:
                    RPCEvents.GlobalEvent(reader.ReadByte(), reader.ReadSingle());
                    break;
                case (byte)CustomRPC.DragAndDropPlayer:
                    RPCEvents.DragAndDropPlayer(reader.ReadByte(), reader.ReadByte());
                    break;


                case (byte)CustomRPC.SealVent:
                    RPCEvents.SealVent(reader.ReadInt32());
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
            Objects.CustomMessage.Initialize();
            Roles.Role.AllCleanUp();
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

        public static void UpdataRoleData(byte playerId, int dataId, int newData)
        {
            Game.GameData.data.players[playerId].SetRoleData(dataId, newData);
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

        public static void SealVent(int ventId)
        {
            Vent vent = ShipStatus.Instance.AllVents.FirstOrDefault((x) => x != null && x.Id == ventId);
            if (vent == null) return;

            //if (PlayerControl.LocalPlayer == SecurityGuard.securityGuard)
            //{
            vent.EnterVentAnim = vent.ExitVentAnim = null;
            if (PlayerControl.GameOptions.MapId == 2)
            {
                //Polus
                vent.myRend.sprite = Roles.Roles.SecurityGuard.getCaveSealedSprite();
            }
            else
            {
                PowerTools.SpriteAnim animator = vent.GetComponent<PowerTools.SpriteAnim>();
                animator?.Stop();
                vent.myRend.sprite = Roles.Roles.SecurityGuard.getVentSealedSprite();
            }
            vent.myRend.color = new Color(1f, 1f, 1f, 1f);
            vent.name = "%SEALED%" + vent.name;
            //}

            //MapOptions.ventsToSeal.Add(vent);
        }

        public static void CleanDeadBody(byte deadBodyId)
        {
            foreach (DeadBody deadBody in Helpers.AllDeadBodies())
            {
                if (deadBody.ParentId == deadBodyId)
                {
                    UnityEngine.Object.Destroy(deadBody.gameObject);
                }
            }
        }
    }
}