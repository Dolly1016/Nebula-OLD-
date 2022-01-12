using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;

namespace Nebula.Roles.Impostor
{
    public class Reaper : Role
    {
        private CustomButton dragButton;


        public byte deadBodyId;


        private Sprite dragButtonSprite = null;
        public Sprite getDragButtonSprite()
        {
            if (dragButtonSprite) return dragButtonSprite;
            dragButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DragButton.png", 115f);
            return dragButtonSprite;
        }

        private Sprite dropButtonSprite = null;
        public Sprite getDropButtonSprite()
        {
            if (dropButtonSprite) return dropButtonSprite;
            dropButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DropButton.png", 115f);
            return dropButtonSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            if (Game.GameData.data.myData.getGlobalData() == null) return;

            if (Game.GameData.data.myData.getGlobalData().dragPlayerId == byte.MaxValue)
            {
                dragButton.Sprite = getDragButtonSprite();

                DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget();
                if (body)
                {
                    deadBodyId = body.ParentId;
                }
                else
                {
                    deadBodyId = byte.MaxValue;
                }
                Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
            }
            else
            {
                dragButton.Sprite = getDropButtonSprite();
            }
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (dragButton != null)
            {
                dragButton.Destroy();
            }
            dragButton = new CustomButton(
                () =>
                {
                    byte target;
                    if (Game.GameData.data.myData.getGlobalData().dragPlayerId!=Byte.MaxValue)
                    {
                        target = Byte.MaxValue;
                    }
                    else
                    {
                        target = (byte)deadBodyId;
                    }

                    MessageWriter dragAndDropWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DragAndDropPlayer, Hazel.SendOption.Reliable, -1);
                    dragAndDropWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                    dragAndDropWriter.Write(target);
                    AmongUsClient.Instance.FinishRpcImmediately(dragAndDropWriter);
                    RPCEvents.DragAndDropPlayer(PlayerControl.LocalPlayer.PlayerId, target);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { },
                getDragButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            
            dragButton.MaxTimer = 1;
            dragButton.Timer = 0;
        }

        public override void CleanUp()
        {
            if (dragButton != null)
            {
                dragButton.Destroy();
                dragButton = null;
            }
        }

        //インポスターはModで操作するFakeTaskは所持していない
        public Reaper()
                : base("Reaper", "reaper", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     false, true, true, false, true)
        {
            dragButton = null;
        }
    }
}
