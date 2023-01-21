using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.ImpostorRoles
{
    public class Covert : Role
    {
        public int canKillId { get; private set; }
        public override void LoadOptionData()
        {
        }

        /* ボタン */
        private CustomButton killButton;
        private SpriteRenderer lockedButtonRenderer;

        public override RelatedRoleData[] RelatedRoleDataInfo
        {
            get => new RelatedRoleData[] {
            new RelatedRoleData(canKillId, "Can Kill", 0, 1,new string[]{ "False","True"})};
        }

        public override void PreloadOptionData()
        {
            extraAssignableOptions.Add(Roles.SideRoles[Side.Impostor].Secret!, null);
            defaultUnassignable.Add(Roles.SecondaryGuesser);
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
                    PlayerControl target = Game.GameData.data.myData.currentTarget;

                    var res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, Game.PlayerData.PlayerStatus.Dead, false, true);
                    if (res != Helpers.MurderAttemptResult.SuppressKill)
                        killButton.Timer = killButton.MaxTimer;
                    Game.GameData.data.myData.currentTarget = null;

                    killButton.Timer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && Game.GameData.data.myData.currentTarget != null; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent,
                __instance,
                Module.NebulaInputManager.modKillInput.keyCode
            ).SetTimer(CustomOptionHolder.InitialKillCoolDownOption.getFloat());
            killButton.MaxTimer = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
            killButton.SetButtonCoolDownOption(true);

            lockedButtonRenderer = Game.GameData.data.myData.getGlobalData().GetRoleData(canKillId) == 1 ? null : killButton.AddOverlay(CustomButton.lockedButtonSprite.GetSprite(), 0f);

        }

        public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
        {
            if (playerId==PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo) EditDisplayRoleNameForcely(playerId, ref roleName);
        }

        public override void EditDisplayRoleNameForcely(byte playerId, ref string roleName)
        {
            if (Game.GameData.data.playersArray[playerId].GetRoleData(canKillId)==1)
                roleName = Helpers.cs(Palette.ImpostorRed, "☉") + roleName;
        }

        public override void CleanUp()
        {
            base.CleanUp();
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }
            lockedButtonRenderer = null;
        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            if (data.getGlobalData().GetRoleData(canKillId) == 1)
                data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
            else
                data.currentTarget = null;
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);
            RPCEvents.UpdateRoleData(__instance.PlayerId, canKillId, 0);
        }

        public override void OnUpdateRoleData(int dataId, int newData)
        {
            if (dataId != canKillId) return;

            if (lockedButtonRenderer == null) return;

            if (newData == 1)
            {
                GameObject.Destroy(lockedButtonRenderer.gameObject);
                lockedButtonRenderer = null;
            }
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Jackal);
            RelatedRoles.Add(Roles.Jester);
            RelatedRoles.Add(Roles.Sheriff);
            RelatedRoles.Add(Roles.Psychic);
            RelatedRoles.Add(Roles.Alien);
        }

        public Covert()
                : base("Covert", "covert", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                     Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            //通常のキルボタンは使用しない
            HideKillButtonEvenImpostor = true;

            canKillId = Game.GameData.RegisterRoleDataId("covert.canKill");
        }

    }
}
