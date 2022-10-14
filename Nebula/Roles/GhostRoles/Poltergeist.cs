using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nebula.Objects;
using Nebula.Utilities;

namespace Nebula.Roles.GhostRoles
{
    public class Poltergeist : GhostRole
    {
        static public Color RoleColor = new Color(210f / 255f, 220f / 255f, 234f/255f);

        CustomButton poltergeistButton;
        SpriteLoader buttonSprite = new SpriteLoader("Nebula,Resources.PoltergeistButton.png",115f);
        public override void ButtonInitialize(HudManager __instance)
        {
            if (poltergeistButton != null)
            {
                poltergeistButton.Destroy();
            }
            poltergeistButton = new CustomButton(
                () =>
                {

                },
                () => { return PlayerControl.LocalPlayer.Data.IsDead; },
                () =>
                {
                    return deadBodyId!=Byte.MaxValue;
                },
                () => { poltergeistButton.Timer = 20f; },
                buttonSprite.GetSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.poltergeist"
            ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
            poltergeistButton.MaxTimer = 20f;
        }

        public override void CleanUp()
        {
            if (poltergeistButton != null)
            {
                poltergeistButton.Destroy();
                poltergeistButton = null;
            }
        }

        public byte deadBodyId;

        public override void MyPlayerControlUpdate()
        {
            if (poltergeistButton == null) return;
            if (Game.GameData.data.myData.getGlobalData() == null) return;

            DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget(3f);

            if (body != null)
            {
                deadBodyId = body.ParentId;
                Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
            }
            else
            {
                deadBodyId = byte.MaxValue;
            }
        }

        public Poltergeist():base("Poltergeist","poltergeist",RoleColor)
        {

        }
    }
}
