﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;

namespace Nebula.Roles.ImpostorRoles
{
    public class Cleaner : Role
    {
        /* オプション */
        private Module.CustomOption cleanCoolDownOption;
        public override void LoadOptionData()
        {
            cleanCoolDownOption = CreateOption(Color.white, "cleanCoolDown", 30f, 10f, 60f, 5f);
        }

        /* ボタン */
        static private CustomButton cleanButton;
        public override void ButtonInitialize(HudManager __instance)
        {
            if (cleanButton != null)
            {
                cleanButton.Destroy();
            }
            cleanButton = new CustomButton(
                () =>
                {
                    byte targetId = deadBodyId;

                    MessageWriter eatWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CleanDeadBody, Hazel.SendOption.Reliable, -1);
                    eatWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                    eatWriter.Write(targetId);
                    eatWriter.Write(byte.MaxValue);
                    AmongUsClient.Instance.FinishRpcImmediately(eatWriter);
                    RPCEvents.CleanDeadBody(targetId);

                    cleanButton.Timer = cleanButton.MaxTimer;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return deadBodyId != Byte.MaxValue && PlayerControl.LocalPlayer.CanMove; },
                () => { cleanButton.Timer = cleanButton.MaxTimer; },
                getCleanButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            cleanButton.MaxTimer=cleanCoolDownOption.getFloat();
            cleanButton.Timer = cleanButton.MaxTimer;
        }

        public override void ButtonActivate()
        {
            cleanButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            cleanButton.setActive(false);
        }

        public byte deadBodyId;


        /* 画像 */
        private Sprite cleanButtonSprite = null;
        public Sprite getCleanButtonSprite()
        {
            if (cleanButtonSprite) return cleanButtonSprite;
            cleanButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.CleanButton.png", 115f);
            return cleanButtonSprite;
        }
        public override void MyPlayerControlUpdate()
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return;

            /* 消去対象の探索 */

            {
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
        }

        public override void Initialize(PlayerControl __instance)
        {

        }

        public override void GlobalInitialize(PlayerControl __instance)
        {

        }

        public override void ButtonCleanUp()
        {
            if (cleanButton != null)
            {
                cleanButton.Destroy();
                cleanButton = null;
            }
        }

        public Cleaner()
            : base("Cleaner", "cleaner", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet,
                 Impostor.impostorEndSet,
                 false, true, true, true, true)
        {
            cleanButton = null;
        }
    }
}

