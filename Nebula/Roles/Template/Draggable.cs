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
using Nebula.Game;

namespace Nebula.Roles.Template
{
    public class Draggable : Role
    {
        /* ボタン */
        private CustomButton dragButton;
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
                    if (Game.GameData.data.myData.getGlobalData().dragPlayerId != Byte.MaxValue)
                    {
                        target = Byte.MaxValue;
                        OnDropPlayer();
                    }
                    else
                    {
                        target = (byte)deadBodyId;
                        OnDragPlayer(target);
                    }

                    MessageWriter dragAndDropWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DragAndDropPlayer, Hazel.SendOption.Reliable, -1);
                    dragAndDropWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                    dragAndDropWriter.Write(target);
                    AmongUsClient.Instance.FinishRpcImmediately(dragAndDropWriter);
                    RPCEvents.DragAndDropPlayer(PlayerControl.LocalPlayer.PlayerId, target);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.getGlobalData().dragPlayerId != Byte.MaxValue; },
                () => { },
                getDragButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );

            dragButton.MaxTimer = 1;
            dragButton.Timer = 0;
        }

        public override void ButtonActivate()
        {
            dragButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            dragButton.setActive(false);
        }

        [RoleLocalMethod]
        public virtual void OnDragPlayer(byte playerId)
        {
        }

        [RoleLocalMethod]
        public virtual void OnDropPlayer()
        {
        }

        /* 画像 */

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

        /* 各種変数 */
        public byte deadBodyId;


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

        public override void CleanUp()
        {
            if (dragButton != null)
            {
                dragButton.Destroy();
                dragButton = null;
            }
        }

        //インポスターはModで操作するFakeTaskは所持していない
        protected Draggable(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, bool canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius) :
            base(name, localizeName, color,category,
                side,introMainDisplaySide,introDisplaySides,introInfluenceSides,
                winReasons,
                hasFakeTask,canUseVents,canMoveInVents,
                ignoreBlackout,useImpostorLightRadius)
        {
            dragButton = null;
        }
    }
}
