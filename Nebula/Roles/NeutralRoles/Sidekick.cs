using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;

namespace Nebula.Roles.NeutralRoles
{
    static public class SidekickSystem
    {

    }
    
    public class Sidekick : Role
    {
        static private CustomButton killButton;

        static public Module.CustomOption SidekickCanKillOption;
        static public Module.CustomOption SidekickTakeOverOriginalRoleOption;
        static public Module.CustomOption SidekickKillCoolDownOption;
        static public Module.CustomOption SidekickCanCreateSidekickOption;
        static public Module.CustomOption SidekickCanUseVentsOption;

        public override void LoadOptionData()
        {
            TopOption.AddPrerequisite(Jackal.CanCreateSidekickOption).AddCustomPrerequisite(()=>Roles.Jackal.IsSpawnable());

            SidekickCanKillOption = CreateOption(Color.white, "canKill", false);
            SidekickKillCoolDownOption = CreateOption(Color.white, "killCoolDown", 20f, 10f, 60f, 2.5f);
            SidekickKillCoolDownOption.suffix = "second";
            SidekickTakeOverOriginalRoleOption = CreateOption(Color.white, "takeOverOriginalRole", true);
            SidekickCanCreateSidekickOption = CreateOption(Color.white, "canCreateSidekick", false);
            SidekickCanUseVentsOption = CreateOption(Color.white, "canUseVents", false);

            SidekickCanKillOption.AddPrerequisite(SidekickTakeOverOriginalRoleOption);
            SidekickKillCoolDownOption.AddPrerequisite(SidekickCanKillOption);
            SidekickCanUseVentsOption.AddPrerequisite(SidekickTakeOverOriginalRoleOption);
        }

        public override void MyPlayerControlUpdate()
        {
            if (killButton != null)
            {
                int jackalId = Game.GameData.data.AllPlayers[PlayerControl.LocalPlayer.PlayerId].GetRoleData(Roles.Jackal.jackalDataId);

                Game.MyPlayerData data = Game.GameData.data.myData;
                data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(
                (player) => {
                    if (player.Object.inVent) return false;
                    if (player.GetModData().role == Roles.Jackal)
                    {
                        return player.GetModData().GetRoleData(Roles.Jackal.jackalDataId) != jackalId;
                    }
                    return true;
                });
                Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Palette.ImpostorRed);
            }
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (killButton != null)
            {
                killButton.Destroy();    
            }
            killButton = null;

            if (SidekickCanKillOption.getBool())
            {
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
                killButton.MaxTimer = SidekickKillCoolDownOption.getFloat();
            }
        }
        public override void CleanUp()
        {
            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }

        }

        public override void GlobalIntroInitialize(PlayerControl __instance)
        {
            canMoveInVents = SidekickCanUseVentsOption.getBool();
            VentPermission = SidekickCanUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
        }

        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (PlayerControl.LocalPlayer.GetModData().role == Roles.Jackal)
            {
                if (PlayerControl.LocalPlayer.GetModData().GetRoleData(Roles.Jackal.jackalDataId) == Helpers.playerById(playerId).GetModData().GetRoleData(Roles.Jackal.jackalDataId))
                {
                    displayColor = Color;
                }
            }
        }

        public override bool IsSpawnable()
        {
            if (!Roles.Jackal.IsSpawnable()) return false;
            if (!Jackal.CanCreateSidekickOption.getBool()) return false;
            if (!SidekickTakeOverOriginalRoleOption.getBool()) return false;

            return true;
        }

        public Sidekick()
            : base("Sidekick", "sidekick", Jackal.RoleColor, RoleCategory.Neutral, Side.Jackal, Side.Jackal,
                 new HashSet<Side>() { Side.Jackal }, new HashSet<Side>() { Side.Jackal },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.JackalWin },
                 true, VentPermission.CanNotUse, true, true, true)
        {
            killButton = null;

            ExceptBasicOption = true;
            CreateOptionFollowingRelatedRole = true;
        }
    }

    public class SecondarySidekick : ExtraRole
    {
        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (PlayerControl.LocalPlayer.GetModData().role == Roles.Jackal)
            {
                if (PlayerControl.LocalPlayer.GetModData().GetRoleData(Roles.Jackal.jackalDataId) == (int)Helpers.playerById(playerId).GetModData().GetExtraRoleData(Roles.SecondarySidekick))
                {
                    displayColor = Color;
                }
            }
        }

        public override bool CheckAdditionalWin(PlayerControl player, Patches.EndCondition condition)
        {
            return condition == Patches.EndCondition.JackalWin;
        }

        public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
        {
            bool showFlag = Game.GameData.data.myData.CanSeeEveryoneInfo;

            if (PlayerControl.LocalPlayer.PlayerId == playerId) showFlag = true;
            else
            {
                if (PlayerControl.LocalPlayer.GetModData().role == Roles.Jackal)
                {
                    if (PlayerControl.LocalPlayer.GetModData().GetRoleData(Roles.Jackal.jackalDataId) == (int)Helpers.playerById(playerId).GetModData().GetExtraRoleData(Roles.SecondarySidekick))
                    {
                        showFlag = true;
                    }
                }
            }

            if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    Jackal.RoleColor, "#");
        }

        public SecondarySidekick() : base("Sidekick", "sidekick", Jackal.RoleColor, 0)
        {
            IsHideRole = true;
        }
    }
}
