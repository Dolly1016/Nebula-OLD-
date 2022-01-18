using System;
using System.Collections.Generic;
using System.Text;
using Nebula.Objects;
using UnityEngine;

namespace Nebula.Roles.CrewmateRoles
{
    public class Necromancer : Template.Draggable
    {
        static public Color Color = new Color(110f / 255f, 51f / 255f, 163f / 255f);

        private CustomButton reviveButton;

        private Module.CustomOption reviveCoolDownOption;
        private Module.CustomOption reviveDurationOption;

        private Sprite buttonSprite = null;

        public Arrow reviveArrow;
        public SystemTypes targetRoom;

        public Sprite getReviveButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ReviveButton.png", 115f);
            return buttonSprite;
        }

        public override void LoadOptionData()
        {
            reviveCoolDownOption = CreateOption(Color.white, "reviveCoolDown", 25f, 10f, 60f, 5f);
            reviveCoolDownOption.suffix = "second";

            reviveDurationOption = CreateOption(Color.white, "reviveDuration", 5f, 1f, 10f, 1f);
            reviveDurationOption.suffix = "second";
        }

        public override void OnDropPlayer()
        {
            CleanArrow();
        }

        public override void OnDragPlayer(byte playerId)
        {
            string room = Language.Language.GetString("role.necromancer.room." + GetReviveRooomType(playerId).ToString());
            string message = Language.Language.GetString("role.necromancer.phantomMessage").Replace("%ROOM%", room);

            Action<float> createMessageAction = null;
            createMessageAction = new Action<float>((p) =>
             {
                 if (p == 0f)
                 {
                     CustomMessage.Create(new Vector3(0, 1f, 0), false, message, 0.5f, 0.4f, 0.7f, 1.0f, Color);
                 }
                 if (p == 1f && Game.GameData.data.myData.getGlobalData().dragPlayerId == playerId)
                 {
                     HudManager.Instance.StartCoroutine(Effects.Lerp(1.6f, createMessageAction));
                 }
             });

            HudManager.Instance.StartCoroutine(Effects.Lerp(1.6f, createMessageAction));

            SpawnArrow(GetReviveRooomType(playerId));
        }

        private SystemTypes GetReviveRooomType(byte playerId)
        {

            return Game.GameData.data.deadPlayers[playerId].RespawnRoom;
        }

        private bool DeadBodyIsInReviveRoom(DeadBody deadBody)
        {

            SystemTypes roomType = Game.GameData.data.deadPlayers[deadBody.ParentId].RespawnRoom;
            PlainShipRoom room = ShipStatus.Instance.FastRooms[roomType];
            return room.roomArea.OverlapPoint(deadBody.myCollider.transform.position);
        }

        private bool DraggingPlayerIsInReviveRoom()
        {
            byte id = PlayerControl.LocalPlayer.GetModData().dragPlayerId;
            if (id == byte.MaxValue) return false;
            foreach (DeadBody body in Helpers.AllDeadBodies())
            {
                if (body.ParentId == id)
                {
                    return DeadBodyIsInReviveRoom(body);
                }
            }
            return false;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            base.ButtonInitialize(__instance);

            if (reviveButton != null)
            {
                reviveButton.Destroy();
            }
            reviveButton = new CustomButton(
                () => { },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () =>
                {
                    if (reviveButton.isEffectActive && !DraggingPlayerIsInReviveRoom())
                    {
                        reviveButton.Timer = 0f;
                        reviveButton.isEffectActive = false;
                    }
                    return PlayerControl.LocalPlayer.CanMove && DraggingPlayerIsInReviveRoom();
                },
                () =>
                {
                    reviveButton.Timer = reviveButton.MaxTimer;
                    reviveButton.isEffectActive = false;
                },
                getReviveButtonSprite(),
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.G,
                true,
                reviveDurationOption.getFloat(),
                () =>
                {
                    if (!DraggingPlayerIsInReviveRoom()) return;

                    CleanArrow();
                    RPCEventInvoker.RevivePlayer(Helpers.playerById(PlayerControl.LocalPlayer.GetModData().dragPlayerId));
                }
            );
            reviveButton.MaxTimer = reviveCoolDownOption.getFloat();
        }

        public override void ButtonActivate()
        {
            base.ButtonActivate();

            reviveButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            base.ButtonDeactivate();

            reviveButton.setActive(false);
        }

        public override void OnMeetingEnd()
        {
            CleanArrow();
        }

        public override void ButtonCleanUp()
        {
            base.ButtonCleanUp();

            if (reviveButton != null)
            {
                reviveButton.Destroy();
                reviveButton = null;
            }

            CleanArrow();
        }

        private void CleanArrow()
        {
            if (reviveArrow != null)
            {
                UnityEngine.Object.Destroy(reviveArrow.arrow);
                reviveArrow = null;
            }
        }

        private void SpawnArrow(SystemTypes roomType)
        {
            CleanArrow();

            reviveArrow = new Arrow(Color.cyan);
            reviveArrow.arrow.SetActive(true);
            reviveArrow.Update(ShipStatus.Instance.FastRooms[roomType].roomArea.transform.position);
            targetRoom = roomType;
        }

        public override void MyPlayerControlUpdate()
        {
            base.MyPlayerControlUpdate();

            if (reviveArrow != null)
            {
                reviveArrow.Update(ShipStatus.Instance.FastRooms[targetRoom].roomArea.transform.position);
            }
        }

        public Necromancer()
            : base("Necromancer", "necromancer", Color, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 CrewmateRoles.Crewmate.crewmateSideSet, CrewmateRoles.Crewmate.crewmateSideSet,
                 CrewmateRoles.Crewmate.crewmateEndSet,
                 false, false, false, false, false)
        {
            reviveArrow = null;
        }
    }
}
