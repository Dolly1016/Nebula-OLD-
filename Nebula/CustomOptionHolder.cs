using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Module;
using UnityEngine;

namespace Nebula
{
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

        public static CustomOption uselessOptions;
        public static CustomOption dynamicMap;

        public static CustomOption specialOptions;
        public static CustomOption maxNumberOfMeetings;
        public static CustomOption blockSkippingInEmergencyMeetings;
        public static CustomOption noVoteIsSelfVote;
        public static CustomOption allowParallelMedBayScans;
        public static CustomOption hidePlayerNames;

        public static CustomOption hideSettings;
        public static CustomOption restrictDevices;
        public static CustomOption restrictAdmin;
        public static CustomOption restrictCameras;
        public static CustomOption restrictVents;


        public static void Load()
        {
            presetSelection = CustomOption.Create(0, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.preset", presets, null, true).HiddenOnDisplay(true);

            activateRoles = CustomOption.Create(5, Color.white, "option.activeRoles", true,null,true);
            crewmateRolesCountMin = CustomOption.Create(301, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumCrewmateRoles", 0f, 0f, 15f, 1f, activateRoles,false).HiddenOnDisplay(true);
            crewmateRolesCountMax = CustomOption.Create(302, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumCrewmateRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            neutralRolesCountMin = CustomOption.Create(303, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumNeutralRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            neutralRolesCountMax = CustomOption.Create(304, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumNeutralRoles", 0f, 0f, 15f, 1f, activateRoles, false).HiddenOnDisplay(true);
            impostorRolesCountMin = CustomOption.Create(305, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.minimumImpostorRoles", 0f, 0f, 3f, 1f, activateRoles, false).HiddenOnDisplay(true);
            impostorRolesCountMax = CustomOption.Create(306, new Color(204f / 255f, 204f / 255f, 0, 1f), "option.maximumImpostorRoles", 0f, 0f, 3f, 1f, activateRoles, false).HiddenOnDisplay(true);

            specialOptions = new CustomOptionBlank(null);
            maxNumberOfMeetings = CustomOption.Create(3, Color.white,"option.maxNumberOfMeetings", 10, 0, 15, 1, specialOptions, true);
            blockSkippingInEmergencyMeetings = CustomOption.Create(4, Color.white, "option.blockSkippingInEmergencyMeetings", false, specialOptions);
            noVoteIsSelfVote = CustomOption.Create(5, Color.white, "option.noVoteIsSelfVote", false, specialOptions);
            allowParallelMedBayScans = CustomOption.Create(540, Color.white, "option.parallelMedbayScans", false, specialOptions);
            hideSettings = CustomOption.Create(520, Color.white, "option.hideSettings", false, specialOptions);

            uselessOptions = CustomOption.Create(530, Color.white, "option.uselessOptions", false, null, isHeader: true);
            dynamicMap = CustomOption.Create(8, Color.white, "option.playRandomMaps", false, uselessOptions);

            //ロールのオプションを読み込む
            Roles.Role.LoadAllOptionData();
            Roles.ExtraRole.LoadAllOptionData();
        }
    }
}
