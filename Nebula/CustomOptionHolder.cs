using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Module;
using UnityEngine;

namespace Nebula
{

    public class GameRule
    {
        public bool dynamicMap { get; }
        public bool canUseEmergencyWithoutDeath { get; }
        public bool canUseEmergencyWithoutSabotage { get; }
        public bool canUseEmergencyWithoutReport { get; }
        public bool severeEmergencyLock { get; }
        public float deathPenaltyForDiscussionTime { get; }
        public int maxMeetingsCount { get; }
        public int vanillaVotingTime { get; }

        public GameRule()
        {
            vanillaVotingTime = PlayerControl.GameOptions.VotingTime;

            if (CustomOptionHolder.meetingOptions.getBool())
            {
                deathPenaltyForDiscussionTime = CustomOptionHolder.deathPenaltyForDiscussionTime.getFloat();
                canUseEmergencyWithoutDeath = CustomOptionHolder.canUseEmergencyWithoutDeath.getBool();
                canUseEmergencyWithoutSabotage = CustomOptionHolder.canUseEmergencyWithoutSabotage.getBool();
                canUseEmergencyWithoutReport = CustomOptionHolder.canUseEmergencyWithoutReport.getBool();
                severeEmergencyLock = CustomOptionHolder.severeEmergencyLock.getBool();
                maxMeetingsCount = (int)CustomOptionHolder.maxNumberOfMeetings.getFloat();
            }
            else
            {
                deathPenaltyForDiscussionTime = 0f;
                canUseEmergencyWithoutDeath = false;
                canUseEmergencyWithoutSabotage = false;
                canUseEmergencyWithoutReport = false;
                severeEmergencyLock = false;
                maxMeetingsCount = 15;
            }

            if (CustomOptionHolder.mapOptions.getBool())
            {
                dynamicMap = CustomOptionHolder.dynamicMap.getBool();
            }
            else
            {
                dynamicMap = false;
            }
        }
    }

    public class CustomOptionHolder
    {
        public static string[] rates = new string[] {
            "option.display.percentage.0" , "option.display.percentage.10", "option.display.percentage.20", "option.display.percentage.30", "option.display.percentage.40",
            "option.display.percentage.50", "option.display.percentage.60", "option.display.percentage.70", "option.display.percentage.80", "option.display.percentage.90", "option.display.percentage.100" };
        public static string[] presets = new string[] { "option.display.preset.1", "option.display.preset.2", "option.display.preset.3", "option.display.preset.4", "option.display.preset.5" };
        public static string[] gamemodes = new string[] { "gamemode.standard", "gamemode.minigame", "gamemode.ritual", "gamemode.investigators", "gamemode.freePlay" };

        private static byte ToByte(float f)
        {
            f = Mathf.Clamp01(f);
            return (byte)(f * 255);
        }

        public static string cs(Color c, string s)
        {
            return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a), s);
        }

        public static int optionsPage = 0;

        public static CustomOption gameMode;

        public static CustomOption presetSelection;

        public static CustomOption crewmateRolesCountMin;
        public static CustomOption crewmateRolesCountMax;
        public static CustomOption neutralRolesCountMin;
        public static CustomOption neutralRolesCountMax;
        public static CustomOption impostorRolesCountMin;
        public static CustomOption impostorRolesCountMax;

        public static CustomOption mapOptions;
        public static CustomOption dynamicMap;
        public static CustomOption exceptSkeld;
        public static CustomOption exceptMIRA;
        public static CustomOption exceptPolus;
        public static CustomOption exceptAirship;
        public static CustomOption additionalVents;
        public static CustomOption additionalWirings;
        public static CustomOption multipleSpawnPoints;
        public static CustomOption synchronizedSpawning;
        public static CustomOption optimizedMaps;
        public static CustomOption invalidatePrimaryAdmin;
        public static CustomOption invalidateSecondaryAdmin;
        public static CustomOption useClassicAdmin;
        public static CustomOption allowParallelMedBayScans;


        public static CustomOption RitualOption;
        public static CustomOption NumOfMissionsOption;
        public static CustomOption LengthOfMissionOption;
        public static CustomOption RitualKillCoolDownOption;
        public static CustomOption RitualKillFailedPenaltyOption;
        public static CustomOption RitualSearchCoolDownOption;
        public static CustomOption RitualSearchableDistanceOption;
        

        public static CustomOption meetingOptions;
        public static CustomOption maxNumberOfMeetings;
        public static CustomOption deathPenaltyForDiscussionTime;
        public static CustomOption canUseEmergencyWithoutDeath;
        public static CustomOption canUseEmergencyWithoutSabotage;
        public static CustomOption canUseEmergencyWithoutReport;
        public static CustomOption severeEmergencyLock;
        public static CustomOption canSkip;
        public static CustomOption dealAbstentionAsSelfVote;
        public static CustomOption hideVotedIcon;

        public static CustomOption limiterOptions;
        public static CustomOption timeLimitOption;
        public static CustomOption timeLimitSecondOption;

        public static CustomOption DevicesOption;
        public static CustomOption AdminLimitOption;
        public static CustomOption VitalsLimitOption;
        public static CustomOption CameraAndDoorLogLimitOption;

        public static CustomOption TasksOption;
        public static CustomOption RandomizedWiringOption;
        public static CustomOption StepsOfWiringOption;
        public static CustomOption MeistersManifoldsOption;
        public static CustomOption MeistersFilterOption;
        public static CustomOption MeistersFuelEnginesOption;

        public static CustomOption SabotageOption;
        public static CustomOption SabotageCoolDownOption;
        public static CustomOption SkeldReactorTimeLimitOption;
        public static CustomOption SkeldO2TimeLimitOption;
        public static CustomOption MIRAReactorTimeLimitOption;
        public static CustomOption MIRAO2TimeLimitOption;
        public static CustomOption SeismicStabilizersTimeLimitOption;
        public static CustomOption AvertCrashTimeLimitOption;
        public static CustomOption BlackOutStrengthOption;
        public static CustomOption CanUseDoorDespiteSabotageOption;

        public static CustomOption SoloFreePlayOption;
        public static CustomOption CountOfDummiesOption;

        public static CustomOption advanceRoleOptions;

        public static CustomOption exclusiveAssignmentParent;
        public static CustomOption exclusiveAssignmentMorphingAndPainter;
        public static CustomOption exclusiveAssignmentRaiderAndSniper;
        public static CustomOption exclusiveAssignmentArsonistAndEmpiric;
        public static CustomOption exclusiveAssignmentAlienAndNavvy;
        public static CustomOption exclusiveAssignmentBaitAndProvocateur;
        public static CustomOption exclusiveAssignmentPsychicAndSeer;
        public static List<Tuple<CustomOption,List<CustomOption>>> exclusiveAssignmentList;
        public static List<Roles.Role> exclusiveAssignmentRoles;

        public static CustomOption CoolDownOption;
        public static CustomOption InitialKillCoolDownOption;
        public static CustomOption InitialAbilityCoolDownOption;
        public static CustomOption InitialForcefulAbilityCoolDownOption;
        public static CustomOption InitialModestAbilityCoolDownOption;

        public static CustomOption SecretRoleOption;
        public static CustomOption NumOfSecretCrewmateOption;
        public static CustomOption NumOfSecretImpostorOption;
        public static CustomOption ChanceOfSecretCrewmateOption;
        public static CustomOption ChanceOfSecretImpostorOption;
        public static CustomOption RequiredTasksForArousal;
        public static CustomOption RequiredNumOfKillingForArousal;

        public static CustomOption escapeHunterOption;


        public static void AddExclusiveAssignment(ref List<ExclusiveAssignment> exclusiveAssignments)
        {
            if (!exclusiveAssignmentParent.getBool()) return;

            if (exclusiveAssignmentMorphingAndPainter.getBool())
                exclusiveAssignments.Add(new ExclusiveAssignment(Roles.Roles.Morphing, Roles.Roles.Painter));
            if (exclusiveAssignmentRaiderAndSniper.getBool())
                exclusiveAssignments.Add(new ExclusiveAssignment(Roles.Roles.Raider,Roles.Roles.Sniper));
            if (exclusiveAssignmentArsonistAndEmpiric.getBool())
                exclusiveAssignments.Add(new ExclusiveAssignment(Roles.Roles.Arsonist, Roles.Roles.Empiric));
            if (exclusiveAssignmentAlienAndNavvy.getBool())
                exclusiveAssignments.Add(new ExclusiveAssignment(Roles.Roles.Alien, Roles.Roles.Navvy));
            if (exclusiveAssignmentBaitAndProvocateur.getBool())
                exclusiveAssignments.Add(new ExclusiveAssignment(Roles.Roles.Bait, Roles.Roles.Provocateur));
            if (exclusiveAssignmentPsychicAndSeer.getBool())
                exclusiveAssignments.Add(new ExclusiveAssignment(Roles.Roles.Psychic, Roles.Roles.Seer));

            foreach (var tuple in exclusiveAssignmentList)
            {
                if (!tuple.Item1.getBool()) continue;

                exclusiveAssignments.Add(new ExclusiveAssignment(
                    tuple.Item2[0].getSelection() > 0 ? exclusiveAssignmentRoles[tuple.Item2[0].getSelection() - 1] : null,
                    tuple.Item2[1].getSelection() > 0 ? exclusiveAssignmentRoles[tuple.Item2[1].getSelection() - 1] : null,
                    tuple.Item2[2].getSelection() > 0 ? exclusiveAssignmentRoles[tuple.Item2[2].getSelection() - 1] : null
                    ));
            }
        }

        public static CustomGameMode GetCustomGameMode()
        {
            return CustomGameModes.GetGameMode(gameMode.getSelection());
        }

        public static void Load()
        {
            presetSelection = CustomOption.Create(1, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.preset", presets, presets[0], null, true, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true).SetGameMode(CustomGameMode.All).Protect();
            gameMode = CustomOption.Create(2, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.gameMode", gamemodes, gamemodes[0], null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);

            crewmateRolesCountMin = CustomOption.Create(10001, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumCrewmateRoles", 0f, 0f, 15f, 1f, null, true, false, "", CustomOptionTab.Settings | CustomOptionTab.CrewmateRoles).HiddenOnDisplay(true);
            crewmateRolesCountMax = CustomOption.Create(10002, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumCrewmateRoles", 0f, 0f, 15f, 1f, null, false, false, "", CustomOptionTab.Settings | CustomOptionTab.CrewmateRoles).HiddenOnDisplay(true);
            neutralRolesCountMin = CustomOption.Create(10003, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumNeutralRoles", 0f, 0f, 15f, 1f, null, false, false, "", CustomOptionTab.Settings | CustomOptionTab.NeutralRoles).HiddenOnDisplay(true);
            neutralRolesCountMax = CustomOption.Create(10004, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumNeutralRoles", 0f, 0f, 15f, 1f, null, false, false, "", CustomOptionTab.Settings | CustomOptionTab.NeutralRoles).HiddenOnDisplay(true);
            impostorRolesCountMin = CustomOption.Create(10005, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumImpostorRoles", 0f, 0f, 5f, 1f, null, false, false, "", CustomOptionTab.Settings | CustomOptionTab.ImpostorRoles).HiddenOnDisplay(true);
            impostorRolesCountMax = CustomOption.Create(10006, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumImpostorRoles", 0f, 0f, 5f, 1f, null, false, false, "", CustomOptionTab.Settings | CustomOptionTab.ImpostorRoles).HiddenOnDisplay(true);

            SoloFreePlayOption = CustomOption.Create(10007, Color.white, "option.soloFreePlayOption", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.FreePlay).AddCustomPrerequisite(() => { return PlayerControl.AllPlayerControls.Count == 1; });
            CountOfDummiesOption = CustomOption.Create(10008, Color.white, "option.countOfDummies", 0, 0, 14, 1, SoloFreePlayOption).SetGameMode(CustomGameMode.All);

            RitualOption = CustomOption.Create(10020, Color.white, "option.ritualOption", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            NumOfMissionsOption = CustomOption.Create(10021, Color.white, "option.numOfMissions", 3, 1, 8, 1, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            LengthOfMissionOption = CustomOption.Create(10022, Color.white, "option.lengthOfMission", 10, 4, 20, 1, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualKillCoolDownOption = CustomOption.Create(10023, Color.white, "option.killCoolDown", 15f, 7.5f, 30f, 2.5f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualKillFailedPenaltyOption = CustomOption.Create(10024, Color.white, "option.killFailedPenalty", 3f, 0f, 20f, 0.5f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualSearchCoolDownOption = CustomOption.Create(10025, Color.white, "option.searchCoolDown", 10f, 2.5f, 30f, 2.5f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualSearchableDistanceOption = CustomOption.Create(10026, Color.white, "option.searchableDistance", 2.5f, 1.25f, 10f, 1.25f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);

            meetingOptions = CustomOption.Create(10100, Color.white, "option.meetingOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(~(CustomGameMode.Minigame | CustomGameMode.Ritual));
            maxNumberOfMeetings = CustomOption.Create(10101, Color.white, "option.maxNumberOfMeetings", 10, 0, 15, 1, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            deathPenaltyForDiscussionTime = CustomOption.Create(10102, Color.white, "option.deathPenaltyForDiscussionTime", 5f, 0f, 30f, 1f, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            deathPenaltyForDiscussionTime.suffix = "second";
            canUseEmergencyWithoutDeath = CustomOption.Create(10103, Color.white, "option.canUseEmergencyWithoutDeath", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            canUseEmergencyWithoutSabotage = CustomOption.Create(10104, Color.white, "option.canUseEmergencyWithoutSabotage", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            canUseEmergencyWithoutReport = CustomOption.Create(10105, Color.white, "option.canUseEmergencyWithoutReport", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            severeEmergencyLock = CustomOption.Create(10109, Color.white, "option.severeEmergencyLock", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            canSkip = CustomOption.Create(10110, Color.white, "option.canSkip", true, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            dealAbstentionAsSelfVote = CustomOption.Create(10111, Color.white, "option.dealAbstentionAsSelfVote", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            hideVotedIcon = CustomOption.Create(10112, Color.white, "option.hideVotedIcon", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);

            mapOptions = CustomOption.Create(10120, Color.white, "option.mapOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);
            dynamicMap = CustomOption.Create(10121, Color.white, "option.playRandomMaps", false, mapOptions).SetGameMode(CustomGameMode.All);
            exceptSkeld = CustomOption.Create(10122, Color.white, "option.exceptSkeld", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptMIRA = CustomOption.Create(10123, Color.white, "option.exceptMIRA", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptPolus = CustomOption.Create(10124, Color.white, "option.exceptPolus", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptAirship = CustomOption.Create(10125, Color.white, "option.exceptAirship", false, dynamicMap).SetGameMode(CustomGameMode.All);
            additionalVents = CustomOption.Create(10130, Color.white, "option.additionalVents", false, mapOptions).SetGameMode(~CustomGameMode.Minigame);
            multipleSpawnPoints = CustomOption.Create(10132, Color.white, "option.multipleSpawnPoints", false, mapOptions).SetGameMode(~(CustomGameMode.Minigame | CustomGameMode.Ritual));
            synchronizedSpawning = CustomOption.Create(10133, Color.white, "option.synchronizedSpawning", false, mapOptions).SetGameMode(~(CustomGameMode.Minigame | CustomGameMode.Ritual));
            optimizedMaps = CustomOption.Create(10134, Color.white, "option.optimizedMaps", true, mapOptions).SetGameMode(CustomGameMode.All);
            invalidatePrimaryAdmin = CustomOption.Create(10135, Color.white, "option.invalidatePrimaryAdmin", new string[] { "option.switch.off", "option.invalidatePrimaryAdmin.onlyAirship", "option.switch.on" }, "option.empty", mapOptions).SetGameMode(CustomGameMode.All);
            invalidateSecondaryAdmin = CustomOption.Create(10136, Color.white, "option.invalidateSecondaryAdmin", true, mapOptions).SetGameMode(CustomGameMode.All);
            useClassicAdmin = CustomOption.Create(10137, Color.white, "option.useClassicAdmin", false, mapOptions).SetGameMode(CustomGameMode.All);
            allowParallelMedBayScans = CustomOption.Create(10138, Color.white, "option.allowParallelMedBayScans", false, mapOptions).SetGameMode(CustomGameMode.All);

            limiterOptions = CustomOption.Create(10140, Color.white, "option.limitOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);
            timeLimitOption = CustomOption.Create(10141, Color.white, "option.timeLimitOption", 20f, 1f, 80f, 1f, limiterOptions).SetGameMode(CustomGameMode.All);
            timeLimitSecondOption = CustomOption.Create(10142, Color.white, "option.timeLimitSecondOption", 0f, 0f, 55f, 5f, limiterOptions).SetGameMode(CustomGameMode.All);
            timeLimitOption.suffix = "minute";
            timeLimitSecondOption.suffix = "second";

            DevicesOption = CustomOption.Create(10150, Color.white, "option.devicesOption", new string[] { "option.switch.off", "option.devicesOption.perDiscussion", "option.devicesOption.perGame" }, "option.switch.off", null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);
            AdminLimitOption = CustomOption.Create(10151, Color.white, "option.devicesOption.Admin", 30f, 5f, 600f, 5f, DevicesOption).SetGameMode(CustomGameMode.All);
            AdminLimitOption.suffix = "second";
            VitalsLimitOption = CustomOption.Create(10152, Color.white, "option.devicesOption.Vitals", 30f, 5f, 600f, 5f, DevicesOption).SetGameMode(CustomGameMode.All);
            VitalsLimitOption.suffix = "second";
            CameraAndDoorLogLimitOption = CustomOption.Create(10153, Color.white, "option.devicesOption.CameraAndDoorLog", 30f, 5f, 600f, 5f, DevicesOption).SetGameMode(CustomGameMode.All);
            CameraAndDoorLogLimitOption.suffix = "second";

            TasksOption = CustomOption.Create(10160, Color.white, "option.tasksOption", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(~CustomGameMode.Ritual);
            additionalWirings = CustomOption.Create(10161, Color.white, "option.additionalWirings", false, TasksOption).SetGameMode(CustomGameMode.All);
            RandomizedWiringOption = CustomOption.Create(10162, Color.white, "option.randomizedWiring", false, TasksOption).SetGameMode(CustomGameMode.All);
            StepsOfWiringOption = CustomOption.Create(10163, Color.white, "option.stepsOfWiring", 3f, 1f, 10f, 1f, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersManifoldsOption = CustomOption.Create(10164, Color.white, "option.meistersManifolds", false, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersFilterOption = CustomOption.Create(10165, Color.white, "option.meistersO2Filter", false, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersFuelEnginesOption = CustomOption.Create(10166, Color.white, "option.meistersFuelEngines", false, TasksOption).SetGameMode(CustomGameMode.All);

            SabotageOption = CustomOption.Create(10170, Color.white, "option.sabotageOption", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(~(CustomGameMode.Ritual | CustomGameMode.Minigame));
            SabotageCoolDownOption = CustomOption.Create(10171, Color.white, "option.sabotageCoolDown", 30f, 5f, 60f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SabotageCoolDownOption.suffix = "second";
            SkeldReactorTimeLimitOption = CustomOption.Create(10172, Color.white, "option.skeldReactorTimeLimit", 30f, 15f, 60f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SkeldReactorTimeLimitOption.suffix = "second";
            SkeldO2TimeLimitOption = CustomOption.Create(10173, Color.white, "option.skeldO2TimeLimit", 30f, 15f, 60f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SkeldO2TimeLimitOption.suffix = "second";
            MIRAReactorTimeLimitOption = CustomOption.Create(10174, Color.white, "option.MIRAReactorTimeLimit", 45f, 20f, 80f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            MIRAReactorTimeLimitOption.suffix = "second";
            MIRAO2TimeLimitOption = CustomOption.Create(10175, Color.white, "option.MIRAO2TimeLimit", 45f, 20f, 80f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            MIRAO2TimeLimitOption.suffix = "second";
            SeismicStabilizersTimeLimitOption = CustomOption.Create(10176, Color.white, "option.seismicStabilizersTimeLimit", 60f, 20f, 120f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SeismicStabilizersTimeLimitOption.suffix = "second";
            AvertCrashTimeLimitOption = CustomOption.Create(10177, Color.white, "option.avertCrashTimeLimit", 90f, 20f, 180f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            AvertCrashTimeLimitOption.suffix = "second";
            BlackOutStrengthOption = CustomOption.Create(10178, Color.white, "option.blackOutStrength", 1f, 0.125f, 2f, 0.125f, SabotageOption).SetGameMode(CustomGameMode.All);
            BlackOutStrengthOption.suffix = "cross";
            CanUseDoorDespiteSabotageOption = CustomOption.Create(10179, Color.white, "option.canUseDoorDespiteSabotage",false, SabotageOption).SetGameMode(CustomGameMode.All);

            SecretRoleOption = CustomOption.Create(10180, Color.white, "option.secretRole", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Standard);
            NumOfSecretCrewmateOption = CustomOption.Create(10181, Color.white, "option.secretCrewmate", 2f, 0f, 15f, 1f, SecretRoleOption);
            ChanceOfSecretCrewmateOption = Module.CustomOption.Create(10182, Color.white, "option.chanceOfSecretCrewmate", CustomOptionHolder.rates, CustomOptionHolder.rates[0], SecretRoleOption);
            NumOfSecretImpostorOption = CustomOption.Create(10183, Color.white, "option.secretImpostor", 2f, 0f, 5f, 1f, SecretRoleOption);
            ChanceOfSecretImpostorOption = Module.CustomOption.Create(10184, Color.white, "option.chanceOfSecretImpostor", CustomOptionHolder.rates, CustomOptionHolder.rates[0], SecretRoleOption);
            RequiredTasksForArousal = CustomOption.Create(10185, Color.white, "option.requiredTasksForArousal", 3f, 1f, 6f, 1f, SecretRoleOption).AddPrerequisite(NumOfSecretCrewmateOption);
            RequiredNumOfKillingForArousal = CustomOption.Create(10186, Color.white, "option.requiredNumOfKillingForArousal", 2f, 1f, 5f, 1f, SecretRoleOption).AddPrerequisite(NumOfSecretImpostorOption);

            advanceRoleOptions = CustomOption.Create(10990, Color.white, "option.advanceRoleOptions", false, null, true, false, "", CustomOptionTab.Settings | CustomOptionTab.CrewmateRoles | CustomOptionTab.ImpostorRoles | CustomOptionTab.NeutralRoles | CustomOptionTab.Modifiers).SetGameMode(CustomGameMode.Standard);
        
            List<string> hunters = new List<string>();
            foreach(Roles.Role role in Roles.Roles.AllRoles)
            {
                if (role.ValidGamemode == CustomGameMode.Minigame && role.winReasons.Contains(Patches.EndCondition.MinigameHunterWin))
                    hunters.Add("role."+role.LocalizeName+".name");
            }
            escapeHunterOption = CustomOption.Create(10991, Color.white, "option.escapeHunter", hunters.ToArray(), hunters[0], null, true, false, "", CustomOptionTab.EscapeRoles).SetGameMode(CustomGameMode.Minigame);

            //ロールのオプションを読み込む
            Roles.Role.LoadAllOptionData();
            Roles.GhostRole.LoadAllOptionData();
            Roles.ExtraRole.LoadAllOptionData();

            CoolDownOption = CustomOption.Create(11010, Color.white, "option.coolDownOption", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.AdvancedSettings);
            InitialKillCoolDownOption = CustomOption.Create(11011, Color.white, "option.initialKillCoolDown", 10f, 5f, 30f, 2.5f, CoolDownOption);
            InitialKillCoolDownOption.suffix = "second";
            InitialAbilityCoolDownOption = CustomOption.Create(11012, Color.white, "option.initialAbilityCoolDown", 15f, 5f, 30f, 2.5f, CoolDownOption);
            InitialAbilityCoolDownOption.suffix = "second";
            InitialForcefulAbilityCoolDownOption = CustomOption.Create(11013, Color.white, "option.initialForcefulAbilityCoolDown", 20f, 5f, 30f, 2.5f, CoolDownOption);
            InitialForcefulAbilityCoolDownOption.suffix = "second";
            InitialModestAbilityCoolDownOption = CustomOption.Create(11014, Color.white, "option.initialModestAbilityCoolDown", 10f, 5f, 30f, 2.5f, CoolDownOption);
            InitialModestAbilityCoolDownOption.suffix = "second";

            exclusiveAssignmentParent = CustomOption.Create(11100, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.exclusiveAssignment", false, null, true, false, "", CustomOptionTab.AdvancedSettings).SetGameMode(CustomGameMode.Standard | CustomGameMode.FreePlay);
            exclusiveAssignmentMorphingAndPainter = CustomOption.Create(11101, Color.white, "option.exclusiveAssignment.MorphingAndPainter", true, exclusiveAssignmentParent);
            exclusiveAssignmentRaiderAndSniper = CustomOption.Create(11102, Color.white, "option.exclusiveAssignment.RaiderAndSniper", true, exclusiveAssignmentParent);
            exclusiveAssignmentArsonistAndEmpiric = CustomOption.Create(11103, Color.white, "option.exclusiveAssignment.ArsonistAndEmpiric", true, exclusiveAssignmentParent);
            exclusiveAssignmentAlienAndNavvy = CustomOption.Create(11104, Color.white, "option.exclusiveAssignment.AlienAndNavvy", true, exclusiveAssignmentParent);
            exclusiveAssignmentBaitAndProvocateur = CustomOption.Create(11105, Color.white, "option.exclusiveAssignment.BaitAndProvocateur", true, exclusiveAssignmentParent);
            exclusiveAssignmentPsychicAndSeer = CustomOption.Create(11106, Color.white, "option.exclusiveAssignment.PsychicAndSeer", false, exclusiveAssignmentParent);
            exclusiveAssignmentRoles = new List<Roles.Role>();
            foreach(Roles.Role role in Roles.Roles.AllRoles)
            {
                if (!role.HideInExclusiveAssignmentOption)
                {
                    exclusiveAssignmentRoles.Add(role);
                }
            }
            string[] roleList = new string[exclusiveAssignmentRoles.Count + 1];
            for(int i = 0; i < roleList.Length - 1; i++)
            {
                roleList[1 + i] = "role." + exclusiveAssignmentRoles[i].LocalizeName + ".name";
            }
            roleList[0] = "option.exclusiveAssignmentRole.none";

            exclusiveAssignmentList = new List<Tuple<CustomOption, List<CustomOption>>>();
            for(int i = 0; i < 5; i++)
            {
                exclusiveAssignmentList.Add(new Tuple<CustomOption, List<CustomOption>>(CustomOption.Create(10210+i*5, new Color(180f / 255f, 180f / 255f, 0, 1f), "option.exclusiveAssignment"+(i+1), false, exclusiveAssignmentParent, false), new List<CustomOption>()));

                for (int r = 0; r < 3; r++)
                {
                    exclusiveAssignmentList[exclusiveAssignmentList.Count - 1].Item2.Add(
                        CustomOption.Create(11110 + i * 5 + 1 + r, Color.white, "option.exclusiveAssignmentRole" + (r + 1), roleList, "option.exclusiveAssignmentRole.none", exclusiveAssignmentList[exclusiveAssignmentList.Count - 1].Item1, false)
                        .SetIdentifier("option.exclusiveAssignment"+(i+1)+".Role" + (r + 1))
                        );
                }
            }


        }

    }

}
