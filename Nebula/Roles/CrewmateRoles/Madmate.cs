using System.Collections.Generic;
using UnityEngine;
using Nebula.Utilities;

namespace Nebula.Roles.CrewmateRoles
{
    public class Madmate : Role
    {
        public override RoleCategory oracleCategory { get { return RoleCategory.Impostor; } }
        

        private Module.CustomOption CanUseVentsOption;
        public Module.CustomOption CanFixSabotageOption;
        private Module.CustomOption HasImpostorVisionOption;
        private Module.CustomOption CanInvokeSabotageOption;
        private Module.CustomOption CanKnowImpostorsByTasksOption;
        private Module.CustomOption NumOfMaxImpostorsCanKnowOption;
        private Module.CustomOption[] NumOfTasksRequiredToKnowImpostorsOption;
        public Module.CustomOption SecondoryRoleOption;

        //Local
        private HashSet<byte> knownImpostors=new HashSet<byte>();

        public override List<ExtraRole> GetImplicateExtraRoles() { return new List<ExtraRole>(new ExtraRole[]{ Roles.SecondaryMadmate }); }

        public override bool IsSecondaryGenerator { get { return SecondoryRoleOption.getBool(); } }
        public override void LoadOptionData()
        {
            base.LoadOptionData();

            TopOption.tab = Module.CustomOptionTab.CrewmateRoles | Module.CustomOptionTab.Modifiers;
            TopOption.SetYellowCondition((tab) =>
            {
                return (tab == Module.CustomOptionTab.Modifiers) == SecondoryRoleOption.getBool() && RoleChanceOption.selection == RoleChanceOption.selections.Length - 1;
            });

            SecondoryRoleOption = CreateOption(Color.white, "isSecondaryRole", false);

            CanUseVentsOption = CreateOption(Color.white, "canUseVents", true).AddInvPrerequisite(SecondoryRoleOption);
            CanInvokeSabotageOption = CreateOption(Color.white, "canInvokeSabotage", true).AddInvPrerequisite(SecondoryRoleOption);
            CanFixSabotageOption = CreateOption(Color.white, "canFixLightsAndComms", true);

            HasImpostorVisionOption = CreateOption(Color.white, "hasImpostorVision", false).AddInvPrerequisite(SecondoryRoleOption);

            CanKnowImpostorsByTasksOption = CreateOption(Color.white, "canKnowImpostorsByTasks", true).AddInvPrerequisite(SecondoryRoleOption);
            NumOfMaxImpostorsCanKnowOption = CreateOption(Color.white, "numOfMaxImpostorsCanKnow", 1f,1f,5f,1f).AddPrerequisite(CanKnowImpostorsByTasksOption);
            NumOfTasksRequiredToKnowImpostorsOption = new Module.CustomOption[5];
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                NumOfTasksRequiredToKnowImpostorsOption[i] =
                    CreateOption(Color.white, "numOfTasksRequiredToKnowImpostors" + (i + 1), (float)(2*(1+i)), 1f, 25f, 1f)
                    .AddPrerequisite(CanKnowImpostorsByTasksOption)
                    .AddCustomPrerequisite(() => { return ((int)NumOfMaxImpostorsCanKnowOption.getFloat()) >= index + 1; });
            }

            CanBeGuesserOption?.AddInvPrerequisite(SecondoryRoleOption);
            CanBeDrunkOption?.AddInvPrerequisite(SecondoryRoleOption);
            CanBeBloodyOption?.AddInvPrerequisite(SecondoryRoleOption);
            CanBeLoversOption?.AddInvPrerequisite(SecondoryRoleOption);
            CanBeMadmateOption?.AddInvPrerequisite(SecondoryRoleOption);
        }

        //適切なタイミングでインポスターを発見する
        public override void OnTaskComplete()
        {
            UpdateKnownImpostors();
        }

        private void UpdateKnownImpostors()
        {
            int completedTasks = Game.GameData.data.myData.getGlobalData().Tasks.Completed;

            if (knownImpostors.Count >= NumOfMaxImpostorsCanKnowOption.getFloat()) return;
            while ((int)NumOfTasksRequiredToKnowImpostorsOption[knownImpostors.Count].getFloat() <= completedTasks)
            {
                List<byte> candidates = new List<byte>();
                foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (player.GetModData().role.category != RoleCategory.Impostor) continue;
                    if (knownImpostors.Contains(player.PlayerId)) continue;
                    candidates.Add(player.PlayerId);
                }

                if (candidates.Count == 0) return;
                knownImpostors.Add(candidates[NebulaPlugin.rnd.Next(candidates.Count)]);

                if (knownImpostors.Count >= NumOfMaxImpostorsCanKnowOption.getFloat()) return;
            }
        }

        public override void EditOthersDisplayNameColor(byte playerId, ref Color displayColor)
        {
            if (knownImpostors.Contains(playerId)) displayColor = Palette.ImpostorRed;
        }

        public override void Initialize(PlayerControl __instance)
        {
            knownImpostors.Clear();

            UpdateKnownImpostors();
        }

        //カットするタスクの数を計上したうえで初期化
        public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo> actualTasks)
        {
            int impostors = 0;
            foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                if (player.GetModData().role.category == RoleCategory.Impostor) impostors++;
            if (impostors > NumOfMaxImpostorsCanKnowOption.getFloat()) impostors = (int)NumOfMaxImpostorsCanKnowOption.getFloat();

            int requireTasks = 0;
            for (int i = 0; i < impostors; i++)
                if (requireTasks < NumOfTasksRequiredToKnowImpostorsOption[i].getFloat()) requireTasks = (int)NumOfTasksRequiredToKnowImpostorsOption[i].getFloat();

            int taskNum = PlayerControl.GameOptions.NumCommonTasks + PlayerControl.GameOptions.NumLongTasks + PlayerControl.GameOptions.NumShortTasks;

            while (initialTasks.Count > taskNum && taskNum > 0)
            {
                if (initialTasks.Count == 0) break;
                initialTasks.RemoveAt(NebulaPlugin.rnd.Next(initialTasks.Count));
            }
        }

        public override void GlobalIntroInitialize(PlayerControl __instance)
        {
            canMoveInVents = CanUseVentsOption.getBool();
            VentPermission = CanUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
            canInvokeSabotage = CanInvokeSabotageOption.getBool();
            canFixSabotage = CanFixSabotageOption.getBool();
            UseImpostorLightRadius = HasImpostorVisionOption.getBool();
        }

        public override bool IsUnsuitable { get { return SecondoryRoleOption.getBool(); } }

        public override bool HasExecutableFakeTask(byte playerId) => CanKnowImpostorsByTasksOption.getBool();

        public Madmate()
                : base("Madmate", "madmate", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, ImpostorRoles.Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, false, false)
        {
        }
    }

    public class SecondaryMadmate : ExtraRole
    {
        //インポスターはModで操作するFakeTaskは所持していない
        public SecondaryMadmate()
                : base("Madmate", "madmate", Palette.ImpostorRed, 0)
        {
            IsHideRole = true;
        }

        private void _sub_Assignment(Patches.AssignMap assignMap, List<byte> players, int count)
        {
            int chance = Roles.Madmate.RoleChanceOption.getSelection() + 1;

            byte playerId;
            for (int i = 0; i < count; i++)
            {
                //割り当てられない場合終了
                if (players.Count == 0) return;

                if (chance <= NebulaPlugin.rnd.Next(10)) continue;

                playerId = players[NebulaPlugin.rnd.Next(players.Count)];
                assignMap.AssignExtraRole(playerId, id, 0);
                players.Remove(playerId);
            }
        }

        public override void Assignment(Patches.AssignMap assignMap)
        {
            if (!Roles.Madmate.SecondoryRoleOption.getBool()) return;

            List<byte> crewmates = new List<byte>();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!player.GetModData()?.role.CanHaveExtraAssignable(this) ?? true) continue;

                switch (player.GetModData()?.role.category)
                {
                    case RoleCategory.Crewmate:
                        crewmates.Add(player.PlayerId);
                        break;
                }
            }

            _sub_Assignment(assignMap, crewmates, (int)Roles.Madmate.RoleCountOption.getFloat());
        }

        public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro)
        => EditDisplayRoleNameForcely(playerId,ref roleName);

        public override void EditDisplayRoleNameForcely(byte playerId, ref string displayName)
        {
            displayName = Helpers.cs(Palette.ImpostorRed, Language.Language.GetString("role.madmate.secondaryPrefix")) + displayName;
        }

        /// <summary>
        /// この役職が発生しうるかどうか調べます
        /// </summary>
        public override bool IsSpawnable()
        {
            if (!Roles.Madmate.SecondoryRoleOption.getBool()) return false;

            return true;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            canFixSabotage = Roles.Madmate.CanFixSabotageOption.getBool();
        }

        public override bool HasCrewmateTask(byte playerId)
        {
            return false;
        }

        public override bool CanFixSabotage => Roles.Madmate.CanFixSabotage;
        

        public override bool CheckAdditionalWin(PlayerControl player, Patches.EndCondition condition)
        {
            return Roles.Impostor.winReasons.Contains(condition);
        }

        public override void EditSpawnableRoleShower(ref string suffix, Role role)
        {
            if (IsSpawnable() && role.CanHaveExtraAssignable(this)) suffix += Helpers.cs(Color, "*");
        }

        public override Module.CustomOption? RegisterAssignableOption(Role role)
        {
            if (role.category != RoleCategory.Crewmate) return null;

            Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeMadmate", role.DefaultExtraAssignableFlag(this), true).HiddenOnDisplay(true).SetIdentifier("role." + role.LocalizeName + ".canBeMadmate");
            option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
            option.AddCustomPrerequisite(() => { return Roles.SecondaryMadmate.IsSpawnable(); });
            return option;
        }
    }


    public static class MadmateHelper
    {
        static public bool IsMadmate(this PlayerControl player)
        {
            return player.GetModData()?.HasExtraRole(Roles.SecondaryMadmate) ?? false;
        }

        static public bool IsMadmate(this Game.PlayerData player)
        {
            return player.HasExtraRole(Roles.SecondaryMadmate);
        }
    }
}
