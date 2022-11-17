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
        public static string[] ratesWithoutZero = new string[] {
            "option.display.percentage.10", "option.display.percentage.20", "option.display.percentage.30", "option.display.percentage.40",
            "option.display.percentage.50", "option.display.percentage.60", "option.display.percentage.70", "option.display.percentage.80", "option.display.percentage.90", "option.display.percentage.100" };
        public static string[] ratesSecondary = new string[] {
            "option.display.percentage.andSoForth", "option.display.percentage.10", "option.display.percentage.20", "option.display.percentage.30", "option.display.percentage.40",
            "option.display.percentage.50", "option.display.percentage.60", "option.display.percentage.70", "option.display.percentage.80", "option.display.percentage.90" };
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

        public static CustomOption roleCountOption;
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
        public static CustomOption quietVentsInTheShadow;
        public static CustomOption oneWayMeetingRoomOption;


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
        public static CustomOption RestrictModeOption;
        public static CustomOption AdminLimitOption;
        public static CustomOption VitalsLimitOption;
        public static CustomOption CameraAndDoorLogLimitOption;
        public static CustomOption UnlimitedCameraSkeldOption;
        public static CustomOption UnlimitedCameraPolusOption;
        public static CustomOption UnlimitedCameraAirshipOption;

        public static CustomOption? GetUnlimitedCameraOption()
        {
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                    return UnlimitedCameraSkeldOption;
                case 2:
                    return UnlimitedCameraPolusOption;
                case 4:
                    return UnlimitedCameraAirshipOption;
                default:
                    return null;
            }
        }

        public static CustomOption TasksOption;
        public static CustomOption RandomizedWiringOption;
        public static CustomOption StepsOfWiringOption;
        public static CustomOption MeistersManifoldsOption;
        public static CustomOption MeistersFilterOption;
        public static CustomOption MeistersFuelEnginesOption;
        public static CustomOption DangerousDownloadSpotOption;

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

        public static IEnumerator<object> GetStringMixedSelections(string topSelection,float min,float mid,float step1,float max,float step2)
        {
            yield return topSelection;
            if (min > max)
            {
                float temp = max;
                max = min;
                min = max;
            }
            if (mid > max)
            {
                float temp = mid;
                mid = max;
                max = mid;
            }
            if (min > mid)
            {
                float temp = mid;
                mid = min;
                min = mid;
            }

            if (step1 < 0) step1 *= -1;
            if (step2 < 0) step2 *= -1;

            float t = min;
            while (t < mid)
            {
                yield return t;
                t += step1;
            }
            
            t = mid;
            while (t < max)
            {
                yield return t;
                t += step2;
            }
            yield return max;
        }

        public static void Load()
        {
            gameMode = CustomOption.Create(Color.white, "option.gameMode", gamemodes, gamemodes[0], null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);

            roleCountOption = CustomOption.Create(Color.white, "option.roleCount", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All).HiddenOnDisplay(true);
            CustomOption.RegisterTopOption(roleCountOption);
            crewmateRolesCountMin = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumCrewmateRoles", 0f, 0f, 15f, 1f, roleCountOption, true, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true);
            crewmateRolesCountMax = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumCrewmateRoles", 0f, 0f, 15f, 1f, roleCountOption, false, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true);
            neutralRolesCountMin = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumNeutralRoles", 0f, 0f, 15f, 1f, roleCountOption, false, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true);
            neutralRolesCountMax = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumNeutralRoles", 0f, 0f, 15f, 1f, roleCountOption, false, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true);
            impostorRolesCountMin = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumImpostorRoles", 0f, 0f, 5f, 1f, roleCountOption, false, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true);
            impostorRolesCountMax = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumImpostorRoles", 0f, 0f, 5f, 1f, roleCountOption, false, false, "", CustomOptionTab.Settings).HiddenOnDisplay(true);

            SoloFreePlayOption = CustomOption.Create(Color.white, "option.soloFreePlayOption", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.FreePlay).AddCustomPrerequisite(() => { return PlayerControl.AllPlayerControls.Count == 1; });
            CustomOption.RegisterTopOption(SoloFreePlayOption);
            CountOfDummiesOption = CustomOption.Create(Color.white, "option.countOfDummies", 0, 0, 14, 1, SoloFreePlayOption).SetGameMode(CustomGameMode.All);

            RitualOption = CustomOption.Create(Color.white, "option.ritualOption", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            CustomOption.RegisterTopOption(RitualOption);
            NumOfMissionsOption = CustomOption.Create(Color.white, "option.numOfMissions", 3, 1, 8, 1, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            LengthOfMissionOption = CustomOption.Create(Color.white, "option.lengthOfMission", 10, 4, 20, 1, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualKillCoolDownOption = CustomOption.Create(Color.white, "option.killCoolDown", 15f, 7.5f, 30f, 2.5f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualKillFailedPenaltyOption = CustomOption.Create(Color.white, "option.killFailedPenalty", 3f, 0f, 20f, 0.5f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualSearchCoolDownOption = CustomOption.Create(Color.white, "option.searchCoolDown", 10f, 2.5f, 30f, 2.5f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);
            RitualSearchableDistanceOption = CustomOption.Create(Color.white, "option.searchableDistance", 2.5f, 1.25f, 10f, 1.25f, RitualOption, false, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Ritual);

            meetingOptions = CustomOption.Create(Color.white, "option.meetingOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(~(CustomGameMode.Minigame | CustomGameMode.Ritual));
            CustomOption.RegisterTopOption(meetingOptions);
            maxNumberOfMeetings = CustomOption.Create(Color.white, "option.maxNumberOfMeetings", 10, 0, 15, 1, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            deathPenaltyForDiscussionTime = CustomOption.Create(Color.white, "option.deathPenaltyForDiscussionTime", 5f, 0f, 30f, 1f, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            deathPenaltyForDiscussionTime.suffix = "second";
            canUseEmergencyWithoutDeath = CustomOption.Create(Color.white, "option.canUseEmergencyWithoutDeath", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            canUseEmergencyWithoutSabotage = CustomOption.Create(Color.white, "option.canUseEmergencyWithoutSabotage", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            canUseEmergencyWithoutReport = CustomOption.Create(Color.white, "option.canUseEmergencyWithoutReport", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            severeEmergencyLock = CustomOption.Create(Color.white, "option.severeEmergencyLock", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            canSkip = CustomOption.Create(Color.white, "option.canSkip", true, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            dealAbstentionAsSelfVote = CustomOption.Create(Color.white, "option.dealAbstentionAsSelfVote", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);
            hideVotedIcon = CustomOption.Create(Color.white, "option.hideVotedIcon", false, meetingOptions).SetGameMode(~CustomGameMode.Minigame);

            mapOptions = CustomOption.Create(Color.white, "option.mapOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);
            CustomOption.RegisterTopOption(mapOptions);
            dynamicMap = CustomOption.Create(Color.white, "option.playRandomMaps", false, mapOptions).SetGameMode(CustomGameMode.All);
            exceptSkeld = CustomOption.Create(Color.white, "option.exceptSkeld", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptMIRA = CustomOption.Create(Color.white, "option.exceptMIRA", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptPolus = CustomOption.Create(Color.white, "option.exceptPolus", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptAirship = CustomOption.Create(Color.white, "option.exceptAirship", false, dynamicMap).SetGameMode(CustomGameMode.All);
            additionalVents = CustomOption.Create(Color.white, "option.additionalVents", false, mapOptions).SetGameMode(~CustomGameMode.Minigame);
            multipleSpawnPoints = CustomOption.Create(Color.white, "option.multipleSpawnPoints", false, mapOptions).SetGameMode(~(CustomGameMode.Minigame | CustomGameMode.Ritual));
            synchronizedSpawning = CustomOption.Create(Color.white, "option.synchronizedSpawning", false, mapOptions).SetGameMode(~(CustomGameMode.Minigame | CustomGameMode.Ritual));
            optimizedMaps = CustomOption.Create(Color.white, "option.optimizedMaps", true, mapOptions).SetGameMode(CustomGameMode.All);
            invalidatePrimaryAdmin = CustomOption.Create(Color.white, "option.invalidatePrimaryAdmin", new string[] { "option.switch.off", "option.invalidatePrimaryAdmin.onlyAirship", "option.switch.on" }, "option.empty", mapOptions).SetGameMode(CustomGameMode.All);
            invalidateSecondaryAdmin = CustomOption.Create(Color.white, "option.invalidateSecondaryAdmin", true, mapOptions).SetGameMode(CustomGameMode.All);
            useClassicAdmin = CustomOption.Create(Color.white, "option.useClassicAdmin", false, mapOptions).SetGameMode(CustomGameMode.All);
            allowParallelMedBayScans = CustomOption.Create(Color.white, "option.allowParallelMedBayScans", false, mapOptions).SetGameMode(CustomGameMode.All);
            quietVentsInTheShadow = CustomOption.Create(Color.white, "option.quietVentsInTheShadow", false, mapOptions).SetGameMode(CustomGameMode.All);
            oneWayMeetingRoomOption = CustomOption.Create(Color.white, "option.oneWayMeetingRoom", false, mapOptions).SetGameMode(CustomGameMode.All);

            limiterOptions = CustomOption.Create(Color.white, "option.limitOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);
            CustomOption.RegisterTopOption(limiterOptions);
            timeLimitOption = CustomOption.Create(Color.white, "option.timeLimitOption", 20f, 1f, 80f, 1f, limiterOptions).SetGameMode(CustomGameMode.All);
            timeLimitSecondOption = CustomOption.Create(Color.white, "option.timeLimitSecondOption", 0f, 0f, 55f, 5f, limiterOptions).SetGameMode(CustomGameMode.All);
            timeLimitOption.suffix = "minute";
            timeLimitSecondOption.suffix = "second";

            DevicesOption = CustomOption.Create(Color.white, "option.devicesOption", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.All);
            CustomOption.RegisterTopOption(DevicesOption);
            RestrictModeOption = CustomOption.Create(Color.white, "option.devicesOption.restrictModeOption", new string[] { "option.devicesOption.perDiscussion", "option.devicesOption.perGame" }, "option.devicesOption.perDiscussion", DevicesOption).SetGameMode(CustomGameMode.All);
            AdminLimitOption = CustomOption.Create(Color.white, "option.devicesOption.admin", GetStringMixedSelections("option.display.infinity",1f,10f,1f,100f,5f), "option.display.infinity", DevicesOption).SetGameMode(CustomGameMode.All);
            AdminLimitOption.suffix = "second";
            VitalsLimitOption = CustomOption.Create(Color.white, "option.devicesOption.vitals", GetStringMixedSelections("option.display.infinity", 1f, 10f, 1f, 100f, 5f), "option.display.infinity", DevicesOption).SetGameMode(CustomGameMode.All);
            VitalsLimitOption.suffix = "second";
            CameraAndDoorLogLimitOption = CustomOption.Create(Color.white, "option.devicesOption.cameraAndDoorLog", GetStringMixedSelections("option.display.infinity", 1f, 10f, 1f, 100f, 5f), "option.display.infinity", DevicesOption).SetGameMode(CustomGameMode.All);
            CameraAndDoorLogLimitOption.suffix = "second";
            UnlimitedCameraSkeldOption = CustomOption.Create(Color.white, "option.devicesOption.unlimitedCameraSkeld", new string[] { "option.display.none", "option.devicesOption.camera.central", "option.devicesOption.camera.east", "option.devicesOption.camera.north", "option.devicesOption.camera.west" }, "option.display.none", DevicesOption).SetGameMode(CustomGameMode.All).AddPrerequisite(CameraAndDoorLogLimitOption);
            UnlimitedCameraPolusOption = CustomOption.Create(Color.white, "option.devicesOption.unlimitedCameraPolus", new string[] { "option.display.none", "option.devicesOption.camera.east", "option.devicesOption.camera.central", "option.devicesOption.camera.northeast", "option.devicesOption.camera.south", "option.devicesOption.camera.southwest", "option.devicesOption.camera.northwest" }, "option.display.none", DevicesOption).SetGameMode(CustomGameMode.All).AddPrerequisite(CameraAndDoorLogLimitOption);
            UnlimitedCameraAirshipOption = CustomOption.Create(Color.white, "option.devicesOption.unlimitedCameraAirship", new string[] { "option.display.none", "option.devicesOption.camera.engineRoom", "option.devicesOption.camera.vault", "option.devicesOption.camera.records", "option.devicesOption.camera.security", "option.devicesOption.camera.cargoBay", "option.devicesOption.camera.meetingRoom" }, "option.display.none", DevicesOption).SetGameMode(CustomGameMode.All).AddPrerequisite(CameraAndDoorLogLimitOption);

            TasksOption = CustomOption.Create(Color.white, "option.tasksOption", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(~CustomGameMode.Ritual);
            CustomOption.RegisterTopOption(TasksOption);
            additionalWirings = CustomOption.Create(Color.white, "option.additionalWirings", false, TasksOption).SetGameMode(CustomGameMode.All);
            RandomizedWiringOption = CustomOption.Create(Color.white, "option.randomizedWiring", false, TasksOption).SetGameMode(CustomGameMode.All);
            StepsOfWiringOption = CustomOption.Create(Color.white, "option.stepsOfWiring", 3f, 1f, 10f, 1f, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersManifoldsOption = CustomOption.Create(Color.white, "option.meistersManifolds", false, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersFilterOption = CustomOption.Create(Color.white, "option.meistersO2Filter", false, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersFuelEnginesOption = CustomOption.Create(Color.white, "option.meistersFuelEngines", false, TasksOption).SetGameMode(CustomGameMode.All);
            DangerousDownloadSpotOption = CustomOption.Create(Color.white, "option.dangerousDownloadSpot", false, TasksOption).SetGameMode(CustomGameMode.All);

            SabotageOption = CustomOption.Create(Color.white, "option.sabotageOption", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(~(CustomGameMode.Ritual | CustomGameMode.Minigame));
            CustomOption.RegisterTopOption(SabotageOption);
            SabotageCoolDownOption = CustomOption.Create(Color.white, "option.sabotageCoolDown", 30f, 5f, 60f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SabotageCoolDownOption.suffix = "second";
            SkeldReactorTimeLimitOption = CustomOption.Create(Color.white, "option.skeldReactorTimeLimit", 30f, 15f, 60f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SkeldReactorTimeLimitOption.suffix = "second";
            SkeldO2TimeLimitOption = CustomOption.Create(Color.white, "option.skeldO2TimeLimit", 30f, 15f, 60f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SkeldO2TimeLimitOption.suffix = "second";
            MIRAReactorTimeLimitOption = CustomOption.Create(Color.white, "option.MIRAReactorTimeLimit", 45f, 20f, 80f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            MIRAReactorTimeLimitOption.suffix = "second";
            MIRAO2TimeLimitOption = CustomOption.Create(Color.white, "option.MIRAO2TimeLimit", 45f, 20f, 80f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            MIRAO2TimeLimitOption.suffix = "second";
            SeismicStabilizersTimeLimitOption = CustomOption.Create(Color.white, "option.seismicStabilizersTimeLimit", 60f, 20f, 120f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            SeismicStabilizersTimeLimitOption.suffix = "second";
            AvertCrashTimeLimitOption = CustomOption.Create(Color.white, "option.avertCrashTimeLimit", 90f, 20f, 180f, 5f, SabotageOption).SetGameMode(CustomGameMode.All);
            AvertCrashTimeLimitOption.suffix = "second";
            BlackOutStrengthOption = CustomOption.Create(Color.white, "option.blackOutStrength", 1f, 0.125f, 2f, 0.125f, SabotageOption).SetGameMode(CustomGameMode.All);
            BlackOutStrengthOption.suffix = "cross";
            CanUseDoorDespiteSabotageOption = CustomOption.Create(Color.white, "option.canUseDoorDespiteSabotage",false, SabotageOption).SetGameMode(CustomGameMode.All);

            SecretRoleOption = CustomOption.Create(Color.white, "option.secretRole", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Standard);
            CustomOption.RegisterTopOption(SecretRoleOption);
            NumOfSecretCrewmateOption = CustomOption.Create(Color.white, "option.secretCrewmate", 2f, 0f, 15f, 1f, SecretRoleOption);
            ChanceOfSecretCrewmateOption = Module.CustomOption.Create( Color.white, "option.chanceOfSecretCrewmate", CustomOptionHolder.rates, CustomOptionHolder.rates[0], SecretRoleOption);
            NumOfSecretImpostorOption = CustomOption.Create(Color.white, "option.secretImpostor", 2f, 0f, 5f, 1f, SecretRoleOption);
            ChanceOfSecretImpostorOption = Module.CustomOption.Create(Color.white, "option.chanceOfSecretImpostor", CustomOptionHolder.rates, CustomOptionHolder.rates[0], SecretRoleOption);
            RequiredTasksForArousal = CustomOption.Create(Color.white, "option.requiredTasksForArousal", 3f, 1f, 6f, 1f, SecretRoleOption).AddPrerequisite(NumOfSecretCrewmateOption);
            RequiredNumOfKillingForArousal = CustomOption.Create(Color.white, "option.requiredNumOfKillingForArousal", 2f, 1f, 5f, 1f, SecretRoleOption).AddPrerequisite(NumOfSecretImpostorOption);

            advanceRoleOptions = CustomOption.Create(Color.white, "option.advanceRoleOptions", false, null, true, false, "", CustomOptionTab.Settings).SetGameMode(CustomGameMode.Standard);
            CustomOption.RegisterTopOption(advanceRoleOptions);

            List<string> hunters = new List<string>();
            foreach(Roles.Role role in Roles.Roles.AllRoles)
            {
                if (role.ValidGamemode == CustomGameMode.Minigame && role.winReasons.Contains(Patches.EndCondition.MinigameHunterWin))
                    hunters.Add("role."+role.LocalizeName+".name");
            }
            escapeHunterOption = CustomOption.Create(Color.white, "option.escapeHunter", hunters.ToArray(), hunters[0], null, true, false, "", CustomOptionTab.EscapeRoles).SetGameMode(CustomGameMode.Minigame);

            //ロールのオプションを読み込む
            Roles.Role.LoadAllOptionData();
            Roles.GhostRole.LoadAllOptionData();
            Roles.ExtraRole.LoadAllOptionData();

            CoolDownOption = CustomOption.Create(Color.white, "option.coolDownOption", new string[] { "option.empty" }, "option.empty", null, true, false, "", CustomOptionTab.AdvancedSettings);
            CustomOption.RegisterTopOption(CoolDownOption);
            InitialKillCoolDownOption = CustomOption.Create(Color.white, "option.initialKillCoolDown", 10f, 5f, 30f, 2.5f, CoolDownOption);
            InitialKillCoolDownOption.suffix = "second";
            InitialAbilityCoolDownOption = CustomOption.Create(Color.white, "option.initialAbilityCoolDown", 15f, 5f, 30f, 2.5f, CoolDownOption);
            InitialAbilityCoolDownOption.suffix = "second";
            InitialForcefulAbilityCoolDownOption = CustomOption.Create(Color.white, "option.initialForcefulAbilityCoolDown", 20f, 5f, 30f, 2.5f, CoolDownOption);
            InitialForcefulAbilityCoolDownOption.suffix = "second";
            InitialModestAbilityCoolDownOption = CustomOption.Create(Color.white, "option.initialModestAbilityCoolDown", 10f, 5f, 30f, 2.5f, CoolDownOption);
            InitialModestAbilityCoolDownOption.suffix = "second";

            exclusiveAssignmentParent = CustomOption.Create(new Color(204f / 255f, 204f / 255f, 0, 1f), "option.exclusiveAssignment", false, null, true, false, "", CustomOptionTab.AdvancedSettings).SetGameMode(CustomGameMode.Standard | CustomGameMode.FreePlay);
            CustomOption.RegisterTopOption(exclusiveAssignmentParent);
            exclusiveAssignmentMorphingAndPainter = CustomOption.Create(Color.white, "option.exclusiveAssignment.MorphingAndPainter", true, exclusiveAssignmentParent);
            exclusiveAssignmentRaiderAndSniper = CustomOption.Create(Color.white, "option.exclusiveAssignment.RaiderAndSniper", true, exclusiveAssignmentParent);
            exclusiveAssignmentArsonistAndEmpiric = CustomOption.Create(Color.white, "option.exclusiveAssignment.ArsonistAndEmpiric", true, exclusiveAssignmentParent);
            exclusiveAssignmentAlienAndNavvy = CustomOption.Create(Color.white, "option.exclusiveAssignment.AlienAndNavvy", true, exclusiveAssignmentParent);
            exclusiveAssignmentBaitAndProvocateur = CustomOption.Create(Color.white, "option.exclusiveAssignment.BaitAndProvocateur", true, exclusiveAssignmentParent);
            exclusiveAssignmentPsychicAndSeer = CustomOption.Create(Color.white, "option.exclusiveAssignment.PsychicAndSeer", false, exclusiveAssignmentParent);
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
                exclusiveAssignmentList.Add(new Tuple<CustomOption, List<CustomOption>>(CustomOption.Create(new Color(180f / 255f, 180f / 255f, 0, 1f), "option.exclusiveAssignment"+(i+1), false, exclusiveAssignmentParent, false), new List<CustomOption>()));

                for (int r = 0; r < 3; r++)
                {
                    exclusiveAssignmentList[exclusiveAssignmentList.Count - 1].Item2.Add(
                        CustomOption.Create(Color.white, "option.exclusiveAssignmentRole" + (r + 1), roleList, "option.exclusiveAssignmentRole.none", exclusiveAssignmentList[exclusiveAssignmentList.Count - 1].Item1, false)
                        .SetIdentifier("option.exclusiveAssignment"+(i+1)+".Role" + (r + 1))
                        );
                }
            }


        }

    }

}
