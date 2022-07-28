using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.CrewmateRoles
{
    public class Sheriff : Role
    {
        static public Color RoleColor = new Color(240f/255f, 191f/255f, 0f);

        private CustomButton killButton;

        private Module.CustomOption killCooldownOption;
        private Module.CustomOption canKillMadmateOption;
        private Module.CustomOption canKillSpyOption;
        private Module.CustomOption canKillNecromancerOption;
        private Module.CustomOption canKillSheriffOption;
        private Module.CustomOption canKillOpportunistOption;

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }

        //キルできる相手かどうか調べる
        private bool CanKill(PlayerControl target)
        {
            //Madmateなら確定で自殺する
            if (PlayerControl.LocalPlayer.IsMadmate()) return false;

            //個別に設定したい非クルー陣営
            if (target.GetModData().role == Roles.Opportunist && canKillOpportunistOption.getBool()) return true;

            //非クルーおよび個別に設定するクルー陣営
            if (Game.GameData.data.players[target.PlayerId].role.category != RoleCategory.Crewmate) return true;
            if (target.GetModData().role == Roles.Madmate && canKillMadmateOption.getBool()) return true;
            if (target.GetModData().role == Roles.Spy && canKillSpyOption.getBool()) return true;
            if (target.GetModData().role == Roles.Necromancer && canKillNecromancerOption.getBool()) return true;
            if (target.GetModData().role == Roles.Sheriff && canKillSheriffOption.getBool()) return true;

            return false;
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
                    if (!CanKill(target))target = PlayerControl.LocalPlayer;

                    var res = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, (target == PlayerControl.LocalPlayer) ? Game.PlayerData.PlayerStatus.Misfire : Game.PlayerData.PlayerStatus.Dead, false, true);
                    if (res != Helpers.MurderAttemptResult.SuppressKill)
                        killButton.Timer = killButton.MaxTimer;
                    Game.GameData.data.myData.currentTarget = null;

                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.Q
            );
            killButton.MaxTimer = killCooldownOption.getFloat();
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
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Jackal);
        }

        public override void LoadOptionData()
        {
            killCooldownOption = CreateOption(Color.white, "killCoolDown", 30f, 10f, 60f, 2.5f);
            killCooldownOption.suffix = "second";

            canKillMadmateOption = CreateOption(Color.white, "canKillMadmate", true);

            canKillSpyOption = CreateOption(Color.white, "canKillSpy", false);

            canKillNecromancerOption = CreateOption(Color.white, "canKillNecromancer", false);

            canKillSheriffOption = CreateOption(Color.white, "canKillSheriff", false);

            canKillOpportunistOption = CreateOption(Color.white, "canKillOpportunist", false);
        }

        public Sheriff()
            : base("Sheriff", "sheriff", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
            killButton = null;
        }
    }
}
