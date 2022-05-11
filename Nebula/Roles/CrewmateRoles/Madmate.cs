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
    public class Madmate : Template.ExemptTasks
    {
        public override RoleCategory oracleCategory { get { return RoleCategory.Impostor; } }
        

        private Module.CustomOption CanUseVentsOption;
        private Module.CustomOption HasImpostorVisionOption;
        private Module.CustomOption CanInvokeSabotageOption;
        private Module.CustomOption CanKnowImpostorsByTasksOption;
        private Module.CustomOption NumOfMaxImpostorsCanKnowOption;
        private Module.CustomOption[] NumOfTasksRequiredToKnowImpostorsOption;

        //Local
        private int completedTasks=0;
        private HashSet<byte> knownImpostors=new HashSet<byte>();

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            CanUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            CanInvokeSabotageOption = CreateOption(Color.white, "canInvokeSabotage", true);

            HasImpostorVisionOption = CreateOption(Color.white, "hasImpostorVision", false);

            CanKnowImpostorsByTasksOption = CreateOption(Color.white, "canKnowImpostorsByTasks", true);
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
        }

        //適切なタイミングでインポスターを発見する
        public override void OnTaskComplete()
        {
            completedTasks++;

            if (knownImpostors.Count >= NumOfMaxImpostorsCanKnowOption.getFloat()) return;
            while ((int)NumOfTasksRequiredToKnowImpostorsOption[knownImpostors.Count].getFloat() <= completedTasks)
            {
                List<byte> candidates = new List<byte>();
                foreach (var player in PlayerControl.AllPlayerControls)
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
            completedTasks = 0;
            knownImpostors.Clear();
        }

        //カットするタスクの数を計上したうえで初期化
        public override void IntroInitialize(PlayerControl __instance)
        {
            int impostors = 0;
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player.GetModData().role.category == RoleCategory.Impostor) impostors++;
            if (impostors > NumOfMaxImpostorsCanKnowOption.getFloat()) impostors = (int)NumOfMaxImpostorsCanKnowOption.getFloat();

            int requireTasks = 0;
            for(int i = 0; i < impostors; i++)
                if (requireTasks < NumOfTasksRequiredToKnowImpostorsOption[i].getFloat()) requireTasks = (int)NumOfTasksRequiredToKnowImpostorsOption[i].getFloat();

            int tasks=PlayerControl.GameOptions.NumCommonTasks + PlayerControl.GameOptions.NumLongTasks + PlayerControl.GameOptions.NumShortTasks;
            CustomExemptTasks = tasks - requireTasks;
            if (CustomExemptTasks < 0) CustomExemptTasks = 0;

            base.IntroInitialize(__instance);

        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            CanMoveInVents = CanUseVentsOption.getBool();
            VentPermission = CanUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
            canInvokeSabotage = CanInvokeSabotageOption.getBool();
            UseImpostorLightRadius = HasImpostorVisionOption.getBool();
        }

        public Madmate()
                : base("Madmate", "madmate", Palette.ImpostorRed, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                     Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, ImpostorRoles.Impostor.impostorEndSet,
                     true, VentPermission.CanUseUnlimittedVent, true, false, false)
        {
            FakeTaskIsExecutable = true;
            UseExemptTasksOption = false;
        }
    }
}
