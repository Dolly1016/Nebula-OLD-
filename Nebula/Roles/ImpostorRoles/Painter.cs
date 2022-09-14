using System;
using System.Collections.Generic;
using System.Text;
using Nebula.Objects;

namespace Nebula.Roles.ImpostorRoles
{
    public class Painter : Role
    {
        private CustomButton paintButton;

        private Module.CustomOption sampleCoolDownOption;
        private Module.CustomOption paintCoolDownOption;

        private Game.PlayerData.PlayerOutfitData? paintOutfit;

        private Sprite sampleButtonSprite = null;
        public Sprite getSampleButtonSprite()
        {
            if (sampleButtonSprite) return sampleButtonSprite;
            sampleButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SampleButton.png", 115f);
            return sampleButtonSprite;
        }

        private Sprite paintButtonSprite = null;
        public Sprite getPaintButtonSprite()
        {
            if (paintButtonSprite) return paintButtonSprite;
            paintButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.PaintButton.png", 115f);
            return paintButtonSprite;
        }

        public override void LoadOptionData()
        {
            sampleCoolDownOption = CreateOption(Color.white, "sampleCoolDown", 10f, 2.5f, 30f, 2.5f);
            sampleCoolDownOption.suffix = "second";

            paintCoolDownOption = CreateOption(Color.white, "paintCoolDown", 10f, 2.5f, 30f, 2.5f);
            paintCoolDownOption.suffix = "second";
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            paintOutfit = null;

            if (paintButton != null)
            {
                paintButton.Destroy();
            }
            paintButton = new CustomButton(
                () =>
                {
                    if (paintOutfit == null)
                    {
                        morphButton.Timer = 3f;
                        morphButton.isEffectActive = false;
                        paintOutfit = Game.GameData.data.myData.currentTarget.GetModData().GetOutfitData(50);
                        morphButton.Sprite = getPaintButtonSprite();
                        morphButton.SetLabel("button.label.paint");
                        morphOutfit = morphTarget.GetModData().GetOutfitData(50);
                    }
                    else
                    {
                        RPCEventInvoker.Paint(Game.GameData.data.myData.currentTarget, new Game.PlayerData.PlayerOutfitData(10, paintOutfit));
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null; },
                () =>
                {
                    paintButton.Timer = sampleCoolDownOption.getFloat();
                },
                getSampleButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.sample"
            );
            paintButton.MaxTimer = paintCoolDownOption.getFloat();
            paintButton.Timer = sampleCoolDownOption.getFloat();

        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }

        public override void OnMeetingEnd()
        {
            paintOutfit = null;
            paintButton.Sprite = getSampleButtonSprite();
            paintButton.SetLabel("button.label.sample");
        }

        public override void CleanUp()
        {
            if (paintButton != null)
            {
                paintButton.Destroy();
                paintButton = null;
            }
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Arsonist);
            RelatedRoles.Add(Roles.Morphing);
        }

        public Painter()
                : base("Painter", "painter", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            paintButton = null;
        }
    }
}
