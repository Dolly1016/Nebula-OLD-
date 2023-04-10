using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.NeutralRoles
{
    public class Immoralist : Role
    {
        private Module.CustomOption showDeathFlashOption;
        private Module.CustomOption canKnowWhereIsDeadBodiesOption;
        private Module.CustomOption canFixLightsAndComms;
        public override bool CanFixSabotage { get { return canFixLightsAndComms.getBool(); } }

        public override void LoadOptionData()
        {
            TopOption.AddCustomPrerequisite(() => Roles.Spectre.IsSpawnable() && Roles.Spectre.spawnImmoralistOption.getBool());
            showDeathFlashOption = CreateOption(Color.white, "showDeathFlash", true);
            canKnowWhereIsDeadBodiesOption = CreateOption(Color.white, "canKnowWhereIsDeadBodies", true);

            canFixLightsAndComms = CreateOption(Color.white, "canFixLightsAndComms", false);

        }

        public override bool IsSpawnable()
        {
            return Roles.Spectre.IsSpawnable() && Roles.Spectre.spawnImmoralistOption.getBool();
        }

        SpriteLoader arrowSprite = new SpriteLoader("role.immoralist.arrow");

        public override void OnDeadBodyGenerated(DeadBody deadBody)
        {
            if (!canKnowWhereIsDeadBodiesOption.getBool()) return;
            new FollowerArrow("ImmoralistArrow",true,deadBody.gameObject, Color.blue,arrowSprite.GetSprite());
        }

        public override void OnAnyoneMurdered(byte murderId, byte targetId)
        {
            if (targetId == PlayerControl.LocalPlayer.PlayerId) return;

            if(showDeathFlashOption.getBool())Helpers.PlayFlash(Color);
        }

        private CustomButton suicideButton;
        private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SuicideButton.png", 115f);

        public override void ButtonInitialize(HudManager __instance)
        {
            if (suicideButton != null)
            {
                suicideButton.Destroy();
            }
            suicideButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId,PlayerControl.LocalPlayer.PlayerId,Game.PlayerData.PlayerStatus.Suicide.Id,false);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () =>
                {
                    return PlayerControl.LocalPlayer.CanMove;
                },
                () => { suicideButton.Timer = 0; },
                buttonSprite.GetSprite(),
                Expansion.GridArrangeExpansion.GridArrangeParameter.None,
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                "button.label.suicide"
            ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
            suicideButton.MaxTimer = 0f;
        }

        public override void CleanUp()
        {
            if (suicideButton != null)
            {
                suicideButton.Destroy();
                suicideButton = null;
            }
        }

        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (PlayerControl.LocalPlayer.GetModData().role == Roles.Spectre)
                displayColor = Color;
        }

        public Immoralist()
        : base("Immoralist", "immoralist", Spectre.RoleColor, RoleCategory.Neutral, Side.Spectre, Side.Spectre,
         new HashSet<Side>() { Side.Spectre }, new HashSet<Side>() { Side.Spectre },
         new HashSet<Patches.EndCondition>() { EndCondition.SpectreWin },
         true, VentPermission.CanNotUse, false, false, false)
        {
            CreateOptionFollowingRelatedRole = true;
            Allocation = AllocationType.None;
        }
    }
}
