using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nebula.Utilities;

namespace Nebula.Roles.ExtraRoles
{
    public class Confused : Template.StandardExtraRole
    {
        static public Color RoleColor = new Color(133f / 255f, 161f / 255f, 190f / 255f);

        protected override bool IsAssignableTo(Role role) => role.CanBeConfused;
        public override Assignable AssignableOnHelp { get => null; }

        private Module.CustomOption chanceOfShuffleOption;
        private Module.CustomOption numOfShuffledPlayersOption;

        public override void OnMeetingEnd()
        {
            //変化が起こらない場合
            if (NebulaPlugin.rnd.NextDouble() * 100f > chanceOfShuffleOption.getFloat()) return;

            //見た目を入れ替える

            List<PlayerControl> alivePlayers = new List<PlayerControl>();

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                if (!p.Data.IsDead && p.PlayerId != PlayerControl.LocalPlayer.PlayerId) alivePlayers.Add(p);

            int shuffled = (int)numOfShuffledPlayersOption.getFloat();
            if (shuffled > alivePlayers.Count) shuffled = alivePlayers.Count;

            while (shuffled < alivePlayers.Count) alivePlayers.RemoveAt(NebulaPlugin.rnd.Next(alivePlayers.Count));

            var randomArray = Helpers.GetRandomArray(alivePlayers.Count);

            Game.PlayerData.PlayerOutfitData[] outfitArray = new Game.PlayerData.PlayerOutfitData[alivePlayers.Count];
            for (int i = 0; i < alivePlayers.Count; i++) outfitArray[i] = alivePlayers[i].GetModData().GetOutfitData(50).Clone(5);

            for (int i = 0; i < alivePlayers.Count; i++) alivePlayers[i].AddOutfit(outfitArray[randomArray[i]]);
            
        }

        public override void EditDisplayRoleName(byte playerId, ref string roleName, bool isIntro) { }

        public override void EditDisplayRoleNameForcely(byte playerId, ref string roleName)
        {
            roleName += Language.Language.GetString("role.confused.name");
        }

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            chanceOfShuffleOption = CreateOption(Color.white, "chanceOfShuffle", 50f, 0f,100f,10f);
            chanceOfShuffleOption.suffix = "percent";
            numOfShuffledPlayersOption = CreateOption(Color.white, "numOfShuffledPlayers", 2f, 1f, 14f, 1f);
        }

        public Confused() : base("Confused", "confused", RoleColor, 0)
        {
        }
    }
}
