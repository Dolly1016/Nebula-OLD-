using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Roles.ExtraRoles
{
    public class Drunk : ExtraRole
    {
        static public Color RoleColor = new Color(133f / 255f, 161f / 255f, 190f / 255f);

        private List<Tuple<Module.CustomOption,Module.CustomOption>> detailChanceOption;
        private string[] categories = { "roles.category.default", "roles.category.crewmate", "roles.category.impostor", "roles.category.neutral", "roles.category.lover", "roles.category.vulture", "roles.category.jackal" };

        private bool CheckPlayerCondition(Game.PlayerData player,int conditionIndex)
        {
            switch (conditionIndex)
            {
                case 0:
                    //Default
                    return true;
                case 1:
                    //Crewmate
                    return player.role.category == RoleCategory.Crewmate;
                case 2:
                    //Impostor
                    return player.role.category == RoleCategory.Impostor;
                case 3:
                    //Neutral
                    return player.role.category == RoleCategory.Neutral;
                case 4:
                    //Lover
                    return player.HasExtraRole(Roles.Lover);
                case 5:
                    //Jackal
                    return player.role == Roles.Jackal;
                case 6:
                    //Vulture
                    return player.role == Roles.Vulture;
            }
            return false;
        }

        private bool CheckAndAssign(Patches.AssignMap assignMap,List<byte> playerArray,int conditionIndex)
        {
            for(int i = 0; i < playerArray.Count; i++)
            {
                if (CheckPlayerCondition(Game.GameData.data.playersArray[playerArray[i]], conditionIndex))
                {
                    assignMap.Assign(playerArray[i], this.id, 0);
                    playerArray.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public override void Assignment(Patches.AssignMap assignMap)
        {
            List<byte> playerArray = new List<byte>(Helpers.GetRandomArray(Game.GameData.data.AllPlayers.Keys));
            playerArray.RemoveAll((id) => { return !Game.GameData.data.playersArray[id].role.CanBeDrunk; });

            int leftPlayers = RoleCountOption.getSelection();

            float probability = (float)RoleChanceOption.getSelection() / 10;

            while(leftPlayers>0)
            {
                if (playerArray.Count == 0) break;

                if (NebulaPlugin.rnd.NextDouble() < probability)
                {
                    foreach(var condition in detailChanceOption)
                    {
                        if (condition.Item1.getSelection() == 0 || NebulaPlugin.rnd.NextDouble() * 10f < condition.Item2.getSelection())
                        {
                            //割り当てられたらループをぬける
                            if (CheckAndAssign(assignMap, playerArray, condition.Item1.getSelection())) break;
                        }
                    }
                }
                leftPlayers--;
            }
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);
            RPCEvents.EmitSpeedFactor(__instance.PlayerId, new Game.SpeedFactor(0, 99999f, -1f, true));
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    RoleColor, "〻");
        }

        public override void LoadOptionData()
        {
            for (int i = 0; i < 5; i++)
            {
                detailChanceOption.Add(new Tuple<Module.CustomOption, Module.CustomOption>(
                    CreateOption(Color.white, "chanceCategory" + (i + 1), categories),
                    CreateOption(Color.white, "chanceRate", CustomOptionHolder.rates)
                    ));

                detailChanceOption[i].Item2.SetParent(detailChanceOption[i].Item1);
                if (i != 0) detailChanceOption[i].Item1.AddPrerequisite(detailChanceOption[i - 1].Item1);

            }
        }

        public Drunk() : base("Drunk", "drunk", RoleColor,1)
        {
            detailChanceOption = new List<Tuple<Module.CustomOption, Module.CustomOption>>();
        }
    }
}
