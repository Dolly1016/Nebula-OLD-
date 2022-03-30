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

            if (CustomOptionHolder.emergencyOptions.getBool())
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
        public static string[] gamemodes = new string[] { "gamemode.standard", "gamemode.minigame", "gamemode.parlour", "gamemode.investigators", "gamemode.freePlay" };

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

        public static CustomOption emergencyOptions;
        public static CustomOption maxNumberOfMeetings;
        public static CustomOption deathPenaltyForDiscussionTime;
        public static CustomOption canUseEmergencyWithoutDeath;
        public static CustomOption canUseEmergencyWithoutSabotage;
        public static CustomOption canUseEmergencyWithoutReport;
        public static CustomOption severeEmergencyLock;

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

        public static CustomOption advanceRoleOptions;

        public static CustomOption exclusiveAssignmentParent;
        public static List<Tuple<CustomOption,List<CustomOption>>> exclusiveAssignmentList;
        public static List<Roles.Role> exclusiveAssignmentRoles;

        public static CustomOption escapeHunterOption;

        public static void AddExclusiveAssignment(ref List<ExclusiveAssignment> exclusiveAssignments)
        {
            if (!exclusiveAssignmentParent.getBool()) return;

            foreach(var tuple in exclusiveAssignmentList)
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
            presetSelection = CustomOption.Create(1, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.preset", presets, presets[0], null, true).HiddenOnDisplay(true).SetGameMode(CustomGameMode.All);
            gameMode = CustomOption.Create(2, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.gameMode", gamemodes, gamemodes[0], null, true).SetGameMode(CustomGameMode.All);

            crewmateRolesCountMin = CustomOption.Create(10001, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumCrewmateRoles", 0f, 0f, 15f, 1f, null, true).HiddenOnDisplay(true);
            crewmateRolesCountMax = CustomOption.Create(10002, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumCrewmateRoles", 0f, 0f, 15f, 1f, null, false).HiddenOnDisplay(true);
            neutralRolesCountMin = CustomOption.Create(10003, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumNeutralRoles", 0f, 0f, 15f, 1f, null, false).HiddenOnDisplay(true);
            neutralRolesCountMax = CustomOption.Create(10004, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumNeutralRoles", 0f, 0f, 15f, 1f, null, false).HiddenOnDisplay(true);
            impostorRolesCountMin = CustomOption.Create(10005, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumImpostorRoles", 0f, 0f, 5f, 1f, null, false).HiddenOnDisplay(true);
            impostorRolesCountMax = CustomOption.Create(10006, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumImpostorRoles", 0f, 0f, 5f, 1f, null, false).HiddenOnDisplay(true);


            emergencyOptions = CustomOption.Create(10100, Color.white, "option.emergencyOptions", false, null, true).SetGameMode(~CustomGameMode.Minigame);
            maxNumberOfMeetings = CustomOption.Create(10101, Color.white, "option.maxNumberOfMeetings", 10, 0, 15, 1, emergencyOptions).SetGameMode(~CustomGameMode.Minigame);
            deathPenaltyForDiscussionTime = CustomOption.Create(10102, Color.white, "option.deathPenaltyForDiscussionTime", 5f, 0f, 30f, 5f, emergencyOptions).SetGameMode(~CustomGameMode.Minigame);
            deathPenaltyForDiscussionTime.suffix = "second";
            canUseEmergencyWithoutDeath = CustomOption.Create(10103, Color.white, "option.canUseEmergencyWithoutDeath", false, emergencyOptions).SetGameMode(~CustomGameMode.Minigame);
            canUseEmergencyWithoutSabotage = CustomOption.Create(10104, Color.white, "option.canUseEmergencyWithoutSabotage", false, emergencyOptions).SetGameMode(~CustomGameMode.Minigame);
            canUseEmergencyWithoutReport = CustomOption.Create(10105, Color.white, "option.canUseEmergencyWithoutReport", false, emergencyOptions).SetGameMode(~CustomGameMode.Minigame);
            severeEmergencyLock = CustomOption.Create(10109, Color.white, "option.severeEmergencyLock", false, emergencyOptions).SetGameMode(~CustomGameMode.Minigame);

            mapOptions = CustomOption.Create(10120, Color.white, "option.mapOptions", false, null, true).SetGameMode(CustomGameMode.All);
            dynamicMap = CustomOption.Create(10121, Color.white, "option.playRandomMaps", false, mapOptions).SetGameMode(CustomGameMode.All);
            exceptSkeld = CustomOption.Create(10122, Color.white, "option.exceptSkeld", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptMIRA = CustomOption.Create(10123, Color.white, "option.exceptMIRA", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptPolus = CustomOption.Create(10124, Color.white, "option.exceptPolus", false, dynamicMap).SetGameMode(CustomGameMode.All);
            exceptAirship = CustomOption.Create(10125, Color.white, "option.exceptAirship", false, dynamicMap).SetGameMode(CustomGameMode.All);
            additionalVents = CustomOption.Create(10130, Color.white, "option.additionalVents", false, mapOptions).SetGameMode(~CustomGameMode.Minigame);
            multipleSpawnPoints = CustomOption.Create(10132, Color.white, "option.multipleSpawnPoints", false, mapOptions).SetGameMode(~CustomGameMode.Minigame);
            synchronizedSpawning = CustomOption.Create(10133, Color.white, "option.synchronizedSpawning", false, mapOptions).SetGameMode(~CustomGameMode.Minigame);

            limiterOptions = CustomOption.Create(10140, Color.white, "option.limitOptions", false, null, true).SetGameMode(CustomGameMode.All);
            timeLimitOption = CustomOption.Create(10141, Color.white, "option.timeLimitOption", 20f, 1f, 80f, 1f, limiterOptions).SetGameMode(CustomGameMode.All);
            timeLimitSecondOption = CustomOption.Create(10142, Color.white, "option.timeLimitSecondOption", 0f, 0f, 55f, 5f, limiterOptions).SetGameMode(CustomGameMode.All);
            timeLimitOption.suffix = "minute";
            timeLimitSecondOption.suffix = "second";

            DevicesOption = CustomOption.Create(10150, Color.white, "option.devicesOption", new string[] {"option.switch.off" , "option.devicesOption.perDiscussion" , "option.devicesOption.perGame"}, "option.switch.off",null,true).SetGameMode(CustomGameMode.All);
            AdminLimitOption = CustomOption.Create(10151, Color.white, "option.devicesOption.Admin", 30f, 5f, 600f, 5f, DevicesOption).SetGameMode(CustomGameMode.All);
            AdminLimitOption.suffix = "second";
            VitalsLimitOption = CustomOption.Create(10152, Color.white, "option.devicesOption.Vitals", 30f, 5f, 600f, 5f, DevicesOption).SetGameMode(CustomGameMode.All);
            VitalsLimitOption.suffix = "second";
            CameraAndDoorLogLimitOption = CustomOption.Create(10153, Color.white, "option.devicesOption.CameraAndDoorLog", 30f, 5f, 600f, 5f, DevicesOption).SetGameMode(CustomGameMode.All);
            CameraAndDoorLogLimitOption.suffix = "second";

            TasksOption = CustomOption.Create(10160, Color.white, "option.tasksOption", false, null, true).SetGameMode(CustomGameMode.All);
            additionalWirings = CustomOption.Create(10161, Color.white, "option.additionalWirings", false, TasksOption).SetGameMode(CustomGameMode.All);
            RandomizedWiringOption = CustomOption.Create(10162, Color.white, "option.randomizedWiring", false, TasksOption).SetGameMode(CustomGameMode.All);
            StepsOfWiringOption = CustomOption.Create(10163, Color.white, "option.stepsOfWiring", 3f, 3f, 10f, 1f, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersManifoldsOption = CustomOption.Create(10164, Color.white, "option.meistersManifolds", false, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersFilterOption = CustomOption.Create(10165, Color.white, "option.meistersO2Filter", false, TasksOption).SetGameMode(CustomGameMode.All);
            MeistersFuelEnginesOption = CustomOption.Create(10166, Color.white, "option.meistersFuelEngines", false, TasksOption).SetGameMode(CustomGameMode.All);

            advanceRoleOptions = CustomOption.Create(10190, Color.white, "option.advanceRoleOptions", false, null, true).SetGameMode(CustomGameMode.Standard);

            List<string> hunters = new List<string>();
            foreach(Roles.Role role in Roles.Roles.AllRoles)
            {
                if (role.ValidGamemode == CustomGameMode.Minigame && role.winReasons.Contains(Patches.EndCondition.MinigameHunterWin))
                    hunters.Add("role."+role.LocalizeName+".name");
            }
            escapeHunterOption = CustomOption.Create(10191, Color.white, "option.escapeHunter", hunters.ToArray(), hunters[0], null, true).SetGameMode(CustomGameMode.Minigame);

            //ロールのオプションを読み込む
            Roles.Role.LoadAllOptionData();
            Roles.ExtraRole.LoadAllOptionData();

            exclusiveAssignmentParent = CustomOption.Create(10200, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.exclusiveAssignment", false, null, true);
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
                    exclusiveAssignmentList[exclusiveAssignmentList.Count - 1].Item2.Add(CustomOption.Create(10210 + i * 5 + 1 + r, Color.white, "option.exclusiveAssignmentRole" + (r + 1), roleList, "option.exclusiveAssignmentRole.none", exclusiveAssignmentList[exclusiveAssignmentList.Count - 1].Item1, false));
                }
            }


        }

    }

}
