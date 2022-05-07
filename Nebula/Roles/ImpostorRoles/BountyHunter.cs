using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nebula.Objects;

namespace Nebula.Roles.ImpostorRoles
{
    public class BountyHunter : Template.HasHologram
    {
        private Module.CustomOption ChangingBountyIntervalOption;
        private Module.CustomOption killCoolDownMultiplierAfterKillingBountyOption;
        private Module.CustomOption killCoolDownMultiplierAfterKillingOthersOption;
        private Module.CustomOption showArrowPointingTowardsTheBountyOption;
        private Module.CustomOption bountyArrowUpdateIntervalOption;

        /* 矢印 */
        private Arrow? Arrow=null;
        private float noticeInterval = 0f;
        private Vector2 noticePos = Vector2.zero;

        //Local
        private float bountyDuration = 0f;
        private byte currentBounty = 0;

        public override void LoadOptionData()
        {
            ChangingBountyIntervalOption = CreateOption(Color.white, "changingBountyInterval", 20f, 10f, 80f, 5f);
            ChangingBountyIntervalOption.suffix = "second";

            killCoolDownMultiplierAfterKillingBountyOption = CreateOption(Color.white, "killCoolDownMultiplierAfterKillingBounty", 0.25f, 0.125f, 1f, 0.125f);
            killCoolDownMultiplierAfterKillingBountyOption.suffix = "cross";
            killCoolDownMultiplierAfterKillingOthersOption = CreateOption(Color.white, "killCoolDownMultiplierAfterKillingOthers", 2f, 1f, 2.5f, 0.125f);
            killCoolDownMultiplierAfterKillingOthersOption.suffix = "cross";

            showArrowPointingTowardsTheBountyOption = CreateOption(Color.white, "showArrowPointingTowardsTheBounty",true);
            bountyArrowUpdateIntervalOption = CreateOption(Color.white, "bountyArrowUpdateInterval", 7.5f, 5f, 25f, 2.5f);
            bountyArrowUpdateIntervalOption.suffix = "second";
        }

        /* ボタン */
        static private CustomButton killButton;
        public override void ButtonInitialize(HudManager __instance)
        {
            if (killButton != null)
            {
                killButton.Destroy();
            }
            killButton = new CustomButton(
                () =>
                {
                    PlayerControl target = Game.GameData.data.myData.currentTarget;

                    var res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, (target == PlayerControl.LocalPlayer) ? Game.PlayerData.PlayerStatus.Misfire : Game.PlayerData.PlayerStatus.Dead, false, true);
                    if (res != Helpers.MurderAttemptResult.SuppressKill)
                        killButton.Timer = killButton.MaxTimer;
                    Game.GameData.data.myData.currentTarget = null;

                    killButton.Timer = PlayerControl.GameOptions.killCooldown * ((target.PlayerId == currentBounty) ? killCoolDownMultiplierAfterKillingBountyOption.getFloat() : killCoolDownMultiplierAfterKillingOthersOption.getFloat());
                    ChangeBounty();
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.Q
            ).SetTimer(10f);
            killButton.MaxTimer = PlayerControl.GameOptions.KillCooldown;
        }

        public override void ButtonActivate()
        {
            killButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            killButton.setActive(false);
        }

        public override void CleanUp()
        {
            base.CleanUp();
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }

            if (Arrow != null)
            {
                UnityEngine.Object.Destroy(Arrow.arrow);
                Arrow = null;
            }
        }

        private void ChangeBounty()
        {
            try
            {
                List<byte> candidates = new List<byte>();
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player.GetModData().role.category == RoleCategory.Impostor) continue;
                    if (player.PlayerId == currentBounty) continue;
                    if (player.Data.IsDead) continue;
                    candidates.Add(player.PlayerId);
                }

                currentBounty = candidates[NebulaPlugin.rnd.Next(candidates.Count)];

                foreach (var icon in PlayerIcons.Values)
                    icon.gameObject.SetActive(false);
                PlayerIcons[currentBounty].gameObject.SetActive(true);
                bountyDuration = ChangingBountyIntervalOption.getFloat() + 0.9f;
                noticeInterval = -1f;
            }catch{ }
        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);

            bountyDuration -= Time.deltaTime;
            if (bountyDuration < 1f) ChangeBounty();

            try
            {
                PlayerIcons[currentBounty].NameText.text = ((int)bountyDuration).ToString();
            }
            catch{ }

            if (!showArrowPointingTowardsTheBountyOption.getBool()) return;

            if (Arrow == null)
            {
                Arrow = new Arrow(Palette.ImpostorRed);
                Arrow.arrow.SetActive(true);
                noticeInterval = 0f;
            }
            noticeInterval -= Time.deltaTime;

            if (noticeInterval < 0f)
            {
                noticePos = Helpers.playerById(currentBounty).transform.position;
                noticeInterval = bountyArrowUpdateIntervalOption.getFloat();
                Arrow.arrow.SetActive(true);
            }

            Arrow.Update(noticePos);
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);
            currentBounty = PlayerControl.LocalPlayer.PlayerId;
            ChangeBounty();
        }

        public override void OnMeetingEnd()
        {
            base.OnMeetingEnd();
            if (Arrow != null) Arrow.arrow.SetActive(false);
            noticeInterval = bountyArrowUpdateIntervalOption.getFloat();
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Jackal);
            RelatedRoles.Add(Roles.Jester);
            RelatedRoles.Add(Roles.EvilAce);
            RelatedRoles.Add(Roles.Sheriff);
        }

        public override void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index)
        {
            base.InitializePlayerIcon(player, PlayerId, index);

            player.NameText.transform.localScale *= 5f;
            player.NameText.transform.position += new Vector3(0, 0.25f);
        }

        public BountyHunter()
                : base("BountyHunter", "bountyHunter", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            //通常のキルボタンは使用しない
            HideKillButtonEvenImpostor = true;
            Arrow = null;
        }

    }
}
