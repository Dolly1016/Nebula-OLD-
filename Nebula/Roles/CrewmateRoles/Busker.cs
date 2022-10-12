﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hazel;
using UnityEngine;
using Nebula.Objects;
using Nebula.Utilities;

namespace Nebula.Roles.CrewmateRoles
{
    public class Busker : Role
    {
        static public Color RoleColor = new Color(255f / 255f, 172f / 255f, 117f / 255f);


        private CustomButton buskButton;

        private Module.CustomOption buskCoolDownOption;
        private Module.CustomOption buskDurationOption;

        private SpriteLoader pseudocideButtonSprite = new SpriteLoader("Nebula.Resources.BuskPseudocideButton.png",115f);
        private SpriteLoader reviveButtonSprite = new SpriteLoader("Nebula.Resources.BuskReviveButton.png",115f);

        public override HelpSprite[] helpSprite => new HelpSprite[]
        {
            new HelpSprite(pseudocideButtonSprite,"role.busker.help.pseudocide",0.3f),
            new HelpSprite(reviveButtonSprite,"role.busker.help.revive",0.3f)
        };

        private bool pseudocideFlag = false;

        private void dieBusker()
        {
            FastDestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
            pseudocideFlag = false;
            RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, true);
        }

        public override bool CanBeLovers { get { return false; } }
        private bool checkPseudocide()
        {
            if (!pseudocideFlag) return false;

            foreach (var deadBody in Helpers.AllDeadBodies())
            {
                if (deadBody.ParentId == PlayerControl.LocalPlayer.PlayerId)
                {
                    return true;
                }
            }
            dieBusker();
            return false;
        }

        private bool checkCanReviveOrAlive()
        {
            if (!PlayerControl.LocalPlayer.CanMove) return false;
            if (!PlayerControl.LocalPlayer.Data.IsDead) return true;
            var mapData = Map.MapData.GetCurrentMapData();
            if (mapData == null) return false;

            return mapData.isOnTheShip(PlayerControl.LocalPlayer.GetTruePosition());
        }

        public override void OnMeetingStart()
        {
            if (pseudocideFlag)
            {
                dieBusker();
            }
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            base.ButtonInitialize(__instance);

            pseudocideFlag = false;

            if (buskButton != null)
            {
                buskButton.Destroy();
            }
            buskButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, false);
                    RPCEventInvoker.SuicideWithoutOverlay(Game.PlayerData.PlayerStatus.Pseudocide.Id);
                    FastDestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(true);
                    pseudocideFlag = true;

                    buskButton.Sprite = reviveButtonSprite.GetSprite();
                    buskButton.SetLabel("button.label.busker.revive");
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead || checkPseudocide(); },
                () => { return checkCanReviveOrAlive(); },
                () =>
                {
                    buskButton.Timer = buskButton.MaxTimer;
                },
                pseudocideButtonSprite.GetSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                true,
               buskDurationOption.getFloat(),
               () => { dieBusker(); },
                false,
                "button.label.busker.pseudocide"
            ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
            buskButton.MaxTimer = buskCoolDownOption.getFloat();

            buskButton.SetSuspendAction(()=> {
                if (!checkPseudocide()) return;
                if (!checkCanReviveOrAlive()) return;
                RPCEventInvoker.UpdatePlayerVisibility(PlayerControl.LocalPlayer.PlayerId, true);
                RPCEventInvoker.RevivePlayer(PlayerControl.LocalPlayer, true, false, true);
                RPCEventInvoker.SetPlayerStatus(PlayerControl.LocalPlayer.PlayerId, Game.PlayerData.PlayerStatus.Alive);
                buskButton.Timer = buskButton.MaxTimer;
                buskButton.isEffectActive = false;
                buskButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;

                buskButton.Sprite = pseudocideButtonSprite.GetSprite();
                buskButton.SetLabel("button.label.busker.pseudocide");
                pseudocideFlag = false;
            });
        }

        public override void CleanUp()
        {
            if (buskButton != null)
            {
                buskButton.Destroy();
                buskButton = null;
            }
        }

        public override void LoadOptionData()
        {
            buskCoolDownOption = CreateOption(Color.white, "buskCoolDown", 20f, 5f, 60f, 2.5f);
            buskCoolDownOption.suffix = "second";

            buskDurationOption = CreateOption(Color.white, "buskDuration", 10f, 5f, 30f, 2.5f);
            buskDurationOption.suffix = "second";

            CanBeLoversOption.isHidden = true;
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Madmate);
            RelatedRoles.Add(Roles.Sheriff);
            RelatedRoles.Add(Roles.Avenger);
        }

        public Busker()
            : base("Busker", "busker", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
            DefaultCanBeLovers = false;
        }
    }
}
