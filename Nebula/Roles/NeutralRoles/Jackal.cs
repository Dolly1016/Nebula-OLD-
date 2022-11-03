using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Utilities;

namespace Nebula.Roles.NeutralRoles
{
    public class Jackal : Role
    {
        static public Color RoleColor = new Color(0f, 162f/255f, 211f/255f);

        static private CustomButton killButton;
        static private CustomButton sidekickButton;

        static public Module.CustomOption CanCreateSidekickOption;
        static public Module.CustomOption NumOfKillingToCreateSidekickOption;
        static public Module.CustomOption KillCoolDownOption;

        private SpriteLoader sidekickButtonSprite = new SpriteLoader("Nebula.Resources.SidekickButton.png", 115f);

        public override HelpSprite[] helpSprite => new HelpSprite[]
        {
            new HelpSprite(sidekickButtonSprite,"role.jackal.help.sidekick",0.3f)
        };

        public int jackalDataId { get; private set; }
        public int leftSidekickDataId { get; private set; }
        public int killingDataId { get; private set; }
        public override RelatedRoleData[] RelatedRoleDataInfo
        {
            get => new RelatedRoleData[] {
            new RelatedRoleData(killingDataId, "Jackal Kill", 0, 15),
            new RelatedRoleData(jackalDataId, "Jackal Identifier", 0, 15),
            new RelatedRoleData(leftSidekickDataId, "Can Create Sidekick", 0, 1,new string[]{ "False","True"})};
        }


        public override void LoadOptionData()
        {
            CanCreateSidekickOption = CreateOption(Color.white, "canCreateSidekick", true);
            KillCoolDownOption = CreateOption(Color.white, "killCoolDown", 20f, 10f, 60f, 2.5f);
            KillCoolDownOption.suffix = "second";

            NumOfKillingToCreateSidekickOption = CreateOption(Color.white, "numOfKillingToCreateSidekick", 3,0,10,1).AddPrerequisite(CanCreateSidekickOption);
        }

        public override IEnumerable<Assignable> GetFollowRoles()
        {
            yield return Roles.Sidekick;
        }

        public override void MyPlayerControlUpdate()
        {
            int jackalId = Game.GameData.data.AllPlayers[PlayerControl.LocalPlayer.PlayerId].GetRoleData(jackalDataId);

            Game.MyPlayerData data = Game.GameData.data.myData;

            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(
                (player) => { 
                    if(player.Object.inVent)return false;
                    if (player.GetModData().role == Roles.Sidekick)
                    {
                        return player.GetModData().GetRoleData(jackalDataId) != jackalId;
                    }
                    else if (player.GetModData().HasExtraRole(Roles.SecondarySidekick))
                    {
                        return player.GetModData().GetExtraRoleData(Roles.SecondarySidekick) != (ulong)jackalId;
                    }
                    return true;
                });

            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            __instance.GetModData().SetRoleData(jackalDataId,__instance.PlayerId);
            __instance.GetModData().SetRoleData(leftSidekickDataId, CanCreateSidekickOption.getBool() ? 1 : 0);
            __instance.GetModData().SetRoleData(killingDataId, 0);
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
                    var r = Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, Game.PlayerData.PlayerStatus.Dead, true);
                    if (r == Helpers.MurderAttemptResult.PerformKill) RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, killingDataId, 1);
                    killButton.Timer = killButton.MaxTimer;
                    Game.GameData.data.myData.currentTarget = null;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                new Vector3(0f, 1f, 0),
                __instance,
                Module.NebulaInputManager.modKillInput.keyCode
            ).SetTimer(CustomOptionHolder.InitialKillCoolDownOption.getFloat());
            killButton.MaxTimer = KillCoolDownOption.getFloat();
            killButton.SetButtonCoolDownOption(true);

            if (sidekickButton != null)
            {
                sidekickButton.Destroy();
            }
            sidekickButton = new CustomButton(
                () =>
                {
                    //Sidekick生成
                    int jackalId = PlayerControl.LocalPlayer.GetModData().GetRoleData(jackalDataId);
                    RPCEventInvoker.CreateSidekick(Game.GameData.data.myData.currentTarget.PlayerId, (byte)jackalId);
                    RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftSidekickDataId, -1);

                    Game.GameData.data.myData.currentTarget = null;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetRoleData(leftSidekickDataId)>0; },
                () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove && PlayerControl.LocalPlayer.GetModData().GetRoleData(killingDataId) >= NumOfKillingToCreateSidekickOption.getFloat(); },
                () => { sidekickButton.Timer = sidekickButton.MaxTimer; },
                sidekickButtonSprite.GetSprite(),
                new Vector3(0f, 0, 0),
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                true,
                "button.label.sidekick"
            );
            sidekickButton.MaxTimer = 20;
        }

        public override void CleanUp()
        {
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }
        }

        private void ChangeSidekickToJackal(byte playerId)
        {
            //SidekickをJackalに昇格

            //対象のJackalID
            int jackalId = Game.GameData.data.AllPlayers[playerId].GetRoleData(jackalDataId);

            foreach (Game.PlayerData player in Game.GameData.data.AllPlayers.Values)
            {
                if (Sidekick.SidekickTakeOverOriginalRoleOption.getBool())
                {
                    //Jackalに変化できるプレイヤーを抽出

                    if (player.role.id != Roles.Sidekick.id) continue;
                    if (player.GetRoleData(jackalDataId) != jackalId) continue;
                }
                else
                {
                    //プレイヤーを抽出し、追加役職としてのSidekickを除去

                    if (!player.HasExtraRole(Roles.SecondarySidekick)) continue;
                    if (player.GetExtraRoleData(Roles.SecondarySidekick) != (ulong)jackalId) continue;

                    RPCEvents.ImmediatelyUnsetExtraRole(Roles.SecondarySidekick, player.id);
                }

                RPCEvents.ImmediatelyChangeRole(player.id, id);
                RPCEvents.UpdateRoleData(player.id, jackalDataId, jackalId);
                RPCEvents.UpdateRoleData(player.id, leftSidekickDataId, Sidekick.SidekickCanCreateSidekickOption.getBool() ? 1 : 0);
            }
        }

        public override void FinalizeInGame(PlayerControl __instance)
        {
            base.FinalizeInGame(__instance);

            ChangeSidekickToJackal(__instance.PlayerId);

        }

        public override void OnDied(byte playerId)
        {
            ChangeSidekickToJackal(playerId);
        }

        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (PlayerControl.LocalPlayer.GetModData().role == Roles.Sidekick || PlayerControl.LocalPlayer.GetModData().role == Roles.Jackal)
            {
                if(PlayerControl.LocalPlayer.GetModData().GetRoleData(jackalDataId)== Helpers.playerById(playerId).GetModData().GetRoleData(jackalDataId))
                {
                    displayColor = RoleColor;
                }
            }else if (PlayerControl.LocalPlayer.GetModData().HasExtraRole(Roles.SecondarySidekick))
            {
                if (PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(Roles.SecondarySidekick) == (ulong)Helpers.playerById(playerId).GetModData().GetRoleData(jackalDataId))
                {
                    displayColor = RoleColor;
                }
            }
        }

        public Jackal()
            : base("Jackal", "jackal", RoleColor, RoleCategory.Neutral, Side.Jackal, Side.Jackal,
                 new HashSet<Side>() { Side.Jackal }, new HashSet<Side>() { Side.Jackal },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JackalWin },
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            killButton = null;
            jackalDataId = Game.GameData.RegisterRoleDataId("jackal.identifier");
            leftSidekickDataId = Game.GameData.RegisterRoleDataId("jackal.leftSidekick");
            killingDataId = Game.GameData.RegisterRoleDataId("jackal.killing");
        }
    }
}
