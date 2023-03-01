using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.ImpostorRoles
{
    public class Banshee : Template.HasHologram
    {

        List<PlayerControl> semiActivePlayers;
        List<PlayerControl> activePlayers;
        List<byte> bytePlayers;

        private CustomButton bansheeButton;
        private CustomButton killButton;
        private SpriteLoader bansheeSprite = new SpriteLoader("Nebula.Resources.BansheeButton.png",100f, "ui.button.banshee.weep");

        public SpriteLoader bansheeArrowSprite = new SpriteLoader("Nebula.Resources.BansheeArrow.png", 200f);

        private Module.CustomOption weepCoolDownOption;
        private Module.CustomOption weepDurationOption;
        private Module.CustomOption killRootTimeOption;
        public Module.CustomOption minWeepNoticeRangeOption;
        public Module.CustomOption fuzzinessWeepNoticeOption;

        public override void LoadOptionData()
        {
            weepCoolDownOption = CreateOption(Color.white, "weepCoolDown", 20f, 5f, 60f, 2.5f);
            weepCoolDownOption.suffix = "second";
            weepDurationOption = CreateOption(Color.white, "weepDuration", 2f, 0f, 10f, 0.5f);
            weepDurationOption.suffix = "second";
            killRootTimeOption = CreateOption(Color.white, "killRootTime", 2f, 0f, 5f, 0.5f);
            killRootTimeOption.suffix = "second";

            minWeepNoticeRangeOption = CreateOption(Color.white, "minWeepNoticeRange", 7.5f, 5f, 30f, 2.5f);
            minWeepNoticeRangeOption.suffix = "cross";
            fuzzinessWeepNoticeOption = CreateOption(Color.white, "fuzzinessWeepNotice", 2.5f, 0f, 5f, 0.5f);
            fuzzinessWeepNoticeOption.suffix = "cross";
        }

        public override Tuple<string, Action>[] helpButton => new Tuple<string, Action>[]
   {
        new Tuple<string, Action>("role.banshee.help.weepRange",()=>{new Objects.EffectCircle(PlayerControl.LocalPlayer.gameObject.transform.position, Palette.White, 1f,16f,false,Palette.ImpostorRed);})
   };

        public override void CleanUp()
        {
            base.CleanUp();
            if (bansheeButton != null)
            {
                bansheeButton.Destroy();
                bansheeButton = null;
            }
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }
        }

        public override void OnMeetingEnd()
        {
            foreach (var semiactive in semiActivePlayers) activePlayers.Add(semiactive);
            semiActivePlayers.Clear();

            activePlayers.RemoveAll((p) => (p.Data.IsDead || p.Data.Disconnected));

            foreach (PlayerControl p in PlayerControl.AllPlayerControls) PlayerIcons[p.PlayerId].gameObject.SetActive(false);
            int counter = 0;
            foreach(var p in activePlayers)
            {
                var icon = PlayerIcons[p.PlayerId];
                icon.gameObject.SetActive(true);
                icon.setSemiTransparent(false);
                icon.transform.localScale = Vector3.one * 0.25f;
                icon.transform.localPosition = new Vector3(-0.25f, -0.25f, 0) + Vector3.right * counter * 0.3f;

                counter++;
            }

        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            semiActivePlayers.Clear();
            activePlayers.Clear();
            bytePlayers.Clear();
        }

        public override void MyPlayerControlUpdate()
        {
            base.MyPlayerControlUpdate();

            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f, true, false, bytePlayers);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);

            /*
            infoUpdateCounter += Time.deltaTime;
            if (infoUpdateCounter > 0.5f)
            {
                RPCEventInvoker.UpdatePlayersIconInfo(this, activePlayers, null);
                infoUpdateCounter = 0f;
            }
            */
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (killButton != null)
            {
                killButton.Destroy();
            }
            killButton = new CustomButton(
                () =>
                {
                    List<byte> targets = new List<byte>();
                    foreach (var p in activePlayers) if (!p.Data.IsDead) targets.Add(p.PlayerId);
                    RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId,targets.ToArray(),Game.PlayerData.PlayerStatus.Withered.Id);
                    activePlayers.Clear();
                    RPCEventInvoker.EmitSpeedFactor(PlayerControl.LocalPlayer, new Game.SpeedFactor(2,killRootTimeOption.getFloat(), 0f, false));
                    killButton.Timer = killButton.MaxTimer;

                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && activePlayers.Count>0; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
                __instance,
                Module.NebulaInputManager.modKillInput.keyCode
            ).SetTimer(CustomOptionHolder.InitialKillCoolDownOption.getFloat());
            killButton.MaxTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
            killButton.SetButtonCoolDownOption(true);

            if (bansheeButton != null)
            {
                bansheeButton.Destroy();
            }
            bansheeButton = new CustomButton(
                () =>
                {
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () =>
                {
                    if (bansheeButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                    {
                        bansheeButton.Timer = 0f;
                        bansheeButton.isEffectActive = false;
                    }
                    return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null;
                },
                () =>
                {
                    bansheeButton.Timer = bansheeButton.MaxTimer;
                    bansheeButton.isEffectActive = false;
                    bansheeButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                bansheeSprite.GetSprite(),
                Expansion.GridArrangeExpansion.GridArrangeParameter.None,
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                true,
                weepDurationOption.getFloat(),
                () =>
                {
                    if (Game.GameData.data.myData.currentTarget != null && !MeetingHud.Instance)
                    {
                        var icon = PlayerIcons[Game.GameData.data.myData.currentTarget.PlayerId];
                        icon.gameObject.SetActive(true);
                        icon.setSemiTransparent(true);
                        icon.transform.localScale = Vector3.one * 0.25f;
                        icon.transform.localPosition = new Vector3(-0.25f, -0.25f, 0) + Vector3.right * (activePlayers.Count+ semiActivePlayers.Count) * 0.3f;

                        semiActivePlayers.Add(Game.GameData.data.myData.currentTarget);
                        bytePlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);

                        RPCEventInvoker.BansheeWeep(PlayerControl.LocalPlayer.transform.position);
                    }
                    Game.GameData.data.myData.currentTarget = null;
                },
                "button.label.weep"
            ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
            bansheeButton.MaxTimer = weepCoolDownOption.getFloat();
            bansheeButton.EffectDuration = weepDurationOption.getFloat();
        }

        public Banshee()
            : base("Banshee", "banshee", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            //通常のキルボタンは使用しない
            HideKillButtonEvenImpostor = true;

            semiActivePlayers = new List<PlayerControl>();
            activePlayers = new List<PlayerControl>();
            bytePlayers = new List<byte>();
        }
    }
}
