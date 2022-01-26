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

        public static CustomOption presetSelection;
        public static CustomOption activateRoles;

        public static CustomOption crewmateRolesCountMin;
        public static CustomOption crewmateRolesCountMax;
        public static CustomOption neutralRolesCountMin;
        public static CustomOption neutralRolesCountMax;
        public static CustomOption impostorRolesCountMin;
        public static CustomOption impostorRolesCountMax;

        public static CustomOption mapOptions;
        public static CustomOption dynamicMap;
        public static CustomOption additionalVents;

        public static CustomOption emergencyOptions;
        public static CustomOption maxNumberOfMeetings;
        public static CustomOption deathPenaltyForDiscussionTime;
        public static CustomOption canUseEmergencyWithoutDeath;
        public static CustomOption canUseEmergencyWithoutSabotage;
        public static CustomOption canUseEmergencyWithoutReport;
        public static CustomOption severeEmergencyLock;


        public static void Load()
        {
            presetSelection = CustomOption.Create(0, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.preset", presets, null, true).HiddenOnDisplay(true);

            activateRoles = CustomOption.Create(5, Color.white, "option.activeRoles", true, null, true);
            crewmateRolesCountMin = CustomOption.Create(1001, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumCrewmateRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            crewmateRolesCountMax = CustomOption.Create(1002, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumCrewmateRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            neutralRolesCountMin = CustomOption.Create(1003, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumNeutralRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            neutralRolesCountMax = CustomOption.Create(1004, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumNeutralRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            impostorRolesCountMin = CustomOption.Create(1005, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumImpostorRoles", 0f, 0f, 3f, 1f, activateRoles, false).HiddenOnDisplay(true);
            impostorRolesCountMax = CustomOption.Create(1006, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumImpostorRoles", 0f, 0f, 3f, 1f, activateRoles, false).HiddenOnDisplay(true);

           
            emergencyOptions = CustomOption.Create(1100, Color.white, "option.emergencyOptions", true, null, false);
            maxNumberOfMeetings = CustomOption.Create(1101, Color.white, "option.maxNumberOfMeetings", 10, 0, 15, 1, emergencyOptions);
            deathPenaltyForDiscussionTime = CustomOption.Create(1102, Color.white, "option.deathPenaltyForDiscussionTime", 5f, 0f, 30f, 5f, emergencyOptions);
            deathPenaltyForDiscussionTime.suffix = "second";
            canUseEmergencyWithoutDeath = CustomOption.Create(1103, Color.white, "option.canUseEmergencyWithoutDeath", false, emergencyOptions);
            canUseEmergencyWithoutSabotage = CustomOption.Create(1104, Color.white, "option.canUseEmergencyWithoutSabotage", false, emergencyOptions);
            canUseEmergencyWithoutReport = CustomOption.Create(1105, Color.white, "option.canUseEmergencyWithoutReport", false, emergencyOptions);
            severeEmergencyLock = CustomOption.Create(1109, Color.white, "option.severeEmergencyLock", false, emergencyOptions);

            mapOptions = CustomOption.Create(1120, Color.white, "option.mapOptions", false, null, isHeader: true);
            dynamicMap = CustomOption.Create(1121, Color.white, "option.playRandomMaps", false, mapOptions);
            additionalVents = CustomOption.Create(1122, Color.white, "option.additionalVents", false, mapOptions);

            //ロールのオプションを読み込む
            Roles.Role.LoadAllOptionData();
            Roles.ExtraRole.LoadAllOptionData();
        }

    }

}
