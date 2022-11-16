using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nebula.Utilities;
using Nebula.Objects;

namespace Nebula.Roles.ComplexRoles
{
    public class FTracker : Template.HasBilateralness
    {
        public Module.CustomOption evilTrackerCanKnowImpostorsKillOption;

        static public Color RoleColor = new Color(114f / 255f, 163f / 255f, 207f / 255f);

        public int remainTrapsId { get; private set; }

        public static SpriteLoader trackButtonSprite = new SpriteLoader("Nebula.Resources.AccelTrapButton.png", 115f);
        public static SpriteLoader meetingButtonSprite = new SpriteLoader("Nebula.Resources.DecelTrapButton.png", 150f);

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            TopOption.tab = Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.ImpostorRoles;

            evilTrackerCanKnowImpostorsKillOption = CreateOption(Color.white, "evilTrackerCanKnowImpostorsKill", true);

            FirstRole = Roles.NiceTracker;
            SecondaryRole = Roles.EvilTracker;
        }

        public FTracker()
                : base("Tracker", "tracker", RoleColor)
        {
        }

        public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.EvilTracker, Roles.NiceTracker }; }
    }

    public class Tracker : Template.BilateralnessRole
    {
        private CustomButton trackButton;

        private Game.PlayerObject? trackTarget;
        private Objects.Arrow? arrow;

        //インポスターはModで操作するFakeTaskは所持していない
        public Tracker(string name, string localizeName, bool isImpostor)
                : base(name, localizeName,
                     isImpostor ? Palette.ImpostorRed : FTrapper.RoleColor,
                     isImpostor ? RoleCategory.Impostor : RoleCategory.Crewmate,
                     isImpostor ? Side.Impostor : Side.Crewmate, isImpostor ? Side.Impostor : Side.Crewmate,
                     isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                     isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                     isImpostor ? ImpostorRoles.Impostor.impostorEndSet : CrewmateRoles.Crewmate.crewmateEndSet,
                     false, isImpostor ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse,
                     isImpostor, isImpostor, isImpostor, () => { return Roles.F_Tracker; }, isImpostor)
        {
            IsHideRole = true;
        }

        public override Assignable AssignableOnHelp => Roles.F_Tracker;

        public override HelpSprite[] helpSprite => new HelpSprite[] {
            new HelpSprite(FTracker.trackButtonSprite,"role.tracker.help.track",0.3f),
            new HelpSprite(FTracker.meetingButtonSprite,"role.tracker.help.meeting",0.7f)
        };

        public override void GlobalInitialize(PlayerControl __instance)
        {
        }

        public override void MyUpdate()
        {
            
            
        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

            RoleSystem.TrackSystem.PlayerTrack_MyControlUpdate(ref arrow, trackTarget, Roles.F_Tracker.Color);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            arrow = null;
            trackTarget = null;

            if (trackButton != null)
            {
                trackButton.Destroy();
            }
            trackButton = new CustomButton(
                () => {
                    trackTarget = new Game.PlayerObject(Game.GameData.data.myData.currentTarget);
                    trackButton.UpperText.text = trackTarget.control.GetModData().currentName;
                    trackButton.Timer = 0f;
                },
                () => {
                    
                    return true && !PlayerControl.LocalPlayer.Data.IsDead;

                },
                () => {
                    return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null && trackTarget==null;
                },
                () => { trackButton.Timer = trackButton.MaxTimer; },
                FTrapper.accelButtonSprite.GetSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                false,
                "button.label.track"
            ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());

            trackButton.MaxTimer = 10f;
        }
        public override void CleanUp()
        {
            if (trackButton != null)
            {
                trackButton.Destroy();
                trackButton = null;
            }

            if (arrow != null)
            {
                GameObject.Destroy(arrow.arrow);
                arrow = null;
            }
            trackTarget = null;
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Psychic);
            RelatedRoles.Add(Roles.Vulture);
        }
    }
}
