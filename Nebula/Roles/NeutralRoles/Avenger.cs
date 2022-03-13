using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;
using Nebula.Game;

namespace Nebula.Roles.NeutralRoles
{
    public class Avenger : Role
    {
        static public Color RoleColor = new Color(141f / 255f, 111f / 255f, 131f / 255f);
        
        static private CustomButton killButton;

        public int avengerCheckerId;

        public Module.CustomOption everyoneCanKnowExistenceOfAvengerOption;
        public Module.CustomOption murderCanKnowAvengerOption;
        private Module.CustomOption avengerKillCoolDownOption;
        private Module.CustomOption avengerNoticeIntervalOption;
        public Module.CustomOption murderNoticeIntervalOption;

        /* 矢印 */
        Arrow Arrow;
        private float noticeInterval = 0f;
        private Vector2 noticePos = Vector2.zero;

        public override void LoadOptionData()
        {
            TopOption.AddCustomPrerequisite(()=>Roles.Lover.loversModeOption.getSelection()==1);

            avengerKillCoolDownOption = CreateOption(Color.white, "killCoolDown", 20f, 10f, 60f, 2.5f);
            avengerKillCoolDownOption.suffix = "second";

            murderCanKnowAvengerOption = CreateOption(Color.white, "murderCanKnowAvenger", false);

            everyoneCanKnowExistenceOfAvengerOption = CreateOption(Color.white, "everyoneCanKnowExistenceOfAvenger", true);

            avengerNoticeIntervalOption = CreateOption(Color.white, "avengerNoticeIntervalOption", 10f, 2.5f, 30f, 2.5f);
            avengerNoticeIntervalOption.suffix = "second";
            murderNoticeIntervalOption = CreateOption(Color.white, "murderNoticeIntervalOption", 10f, 2.5f, 30f, 2.5f);
            murderNoticeIntervalOption.suffix = "second";
        }

        

        public override void MyPlayerControlUpdate()
        {
            var myGData = Game.GameData.data.myData.getGlobalData();

            if (myGData.GetRoleData(avengerCheckerId) == 0)
            {
                foreach(var data in Game.GameData.data.players.Values)
                {
                    if (data.GetExtraRoleData(Roles.AvengerTarget) == myGData.GetExtraRoleData(Roles.Lover))
                    {
                        if (data.IsAlive)
                        {
                            if (Helpers.playerById(data.id) != null)
                            {
                                if (Arrow == null)
                                {
                                    Arrow = new Arrow(Color);
                                    Arrow.arrow.SetActive(true);
                                    noticeInterval = 0f;
                                }
                                noticeInterval -= Time.deltaTime;

                                if (noticeInterval < 0f)
                                {
                                    noticePos = Helpers.playerById(data.id).transform.position;
                                    noticeInterval = avengerNoticeIntervalOption.getFloat();
                                }

                                Arrow.Update(noticePos);
                            }
                        }
                        break;
                    }
                }
            }
            else if(Arrow!=null)
            {
                UnityEngine.Object.Destroy(Arrow.arrow);
                Arrow = null;
            }


            Game.MyPlayerData myData = Game.GameData.data.myData;

            myData.currentTarget = Patches.PlayerControlPatch.SetMyTarget();

            Patches.PlayerControlPatch.SetPlayerOutline(myData.currentTarget, Palette.ImpostorRed);
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.GetModData().SetRoleData(avengerCheckerId, 0);
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
                    Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, Game.PlayerData.PlayerStatus.Dead, true);

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
            killButton.MaxTimer = avengerKillCoolDownOption.getFloat();
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

            if (Arrow != null) {
                UnityEngine.Object.Destroy(Arrow.arrow);
                Arrow = null;
            }
        }

        /*
        public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
        {
            if (!murderCanKnowAvengerOption.getBool()) return;

            if (Game.GameData.data.players[playerId].GetExtraRoleData(Roles.Lover)==Game.GameData.data.myData.getGlobalData().GetExtraRoleData(Roles.AvengerTarget)) 
            { displayName += Helpers.cs(Roles.Avenger.Color, "♥"); return; }
        }
        */

        public override bool CheckWin(PlayerControl player, EndCondition winReason)
        {
            if (winReason != EndCondition.AvengerWin) return false;
            if (player.Data.IsDead) return false;
            return player.GetModData().GetRoleData(avengerCheckerId) == 1;
        }

        public Avenger()
            : base("Avenger", "avenger", RoleColor, RoleCategory.Neutral, Side.Avenger, Side.Avenger,
                 new HashSet<Side>() { Side.Avenger }, new HashSet<Side>() { Side.Avenger },
                 new HashSet<Patches.EndCondition>() { },
                 true, VentPermission.CanUseUnlimittedVent, true, false, false)
        {
            avengerCheckerId = Game.GameData.RegisterRoleDataId("avenger.winChecker");

            killButton = null;

            ExceptBasicOption = true;
            CreateOptionFollowingRelatedRole = true;

            Arrow = null;
        }
    }
}
