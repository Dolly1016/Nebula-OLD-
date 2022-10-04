using HarmonyLib;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Nebula.NebulaPlugin;

namespace Nebula.Patches
{
    public class AssignMap
    {
        public Dictionary<byte, byte> RoleMap;
        public List<Tuple<byte,Tuple<byte,ulong>>> ExtraRoleList;

        public AssignMap()
        {
            RoleMap = new Dictionary<byte, byte>();
            ExtraRoleList = new List<Tuple<byte, Tuple<byte, ulong>>>();
        }

        public void AssignRole(byte playerId,byte roleId)
        {
            RoleMap[playerId] = roleId;
            RPCEvents.SetRole(playerId, Roles.Role.GetRoleById(roleId),0);
        }

        public void AssignRole(byte playerId, byte roleId,int initializeId)
        {
            RoleMap[playerId] = roleId;
            RPCEvents.SetRole(playerId, Roles.Role.GetRoleById(roleId), initializeId);
        }

        public void AssignExtraRole(byte playerId,byte extraRoleId,ulong initializeValue)
        {
            ExtraRoleList.Add(new Tuple<byte, Tuple<byte, ulong>>(playerId,new Tuple<byte, ulong>(extraRoleId,initializeValue)));
            RPCEvents.SetExtraRole(playerId,Roles.ExtraRole.GetRoleById(extraRoleId),initializeValue);
        }
    }

    [HarmonyPatch(typeof(RoleOptionsData), nameof(RoleOptionsData.GetNumPerGame))]
    class RoleOptionsDataGetNumPerGamePatch
    {
        public static void Postfix(ref int __result)
        {
            //バニラロールの無効化設定
            __result = 0;
        }
    }

    public class AssignRoles
    {
        public class RoleAllocation
        {
            public Role role { get; }
            public int expected { get; }

            public RoleAllocation(Role role, int expected)
            {
                this.role = role;
                this.expected = expected;
            }
        }

        public class CategoryData
        {
            public AssignRoles assignRoles { get; }
            protected int roles { get; }
            protected List<Role> firstRoles { get; }
            protected List<RoleAllocation> secondaryRoles { get; }

            public CategoryData(AssignRoles assignRoles, int min, int max, RoleCategory category)
            {
                this.assignRoles = assignRoles;
                this.roles = (min < max) ? NebulaPlugin.rnd.Next(min, max) : max;
                this.firstRoles = new List<Role>();
                this.secondaryRoles = new List<RoleAllocation>();

                foreach (Role role in Roles.Roles.AllRoles)
                {
                    //対象外のロールと非表示ロールはスキップする
                    //無効なロールは入れない
                    if ((int)(CustomOptionHolder.GetCustomGameMode() & role.ValidGamemode) == 0) continue;

                    if (role.category != category)
                    {
                        continue;
                    }

                    if (role.IsHideRole || role.ExceptBasicOption)
                    {
                        continue;
                    }

                    if (role.IsUnsuitable) continue;

                    //ロールの湧き数
                    int roleCount = role.FixedRoleCount ? role.GetCustomRoleCount() : (int)role.RoleCountOption.getFloat();

                    if (role.RoleChanceOption.getSelection() < 10)
                    {
                        if ((int)role.RoleChanceOption.getSelection() > 0)
                        {
                            //ランダムロール
                            for (int i = 0; i < roleCount; i++)
                            {
                                secondaryRoles.Add(new RoleAllocation(role, (int)role.RoleChanceOption.getSelection()));
                            }
                        }
                    }
                    else
                    {
                        //100%ロール
                        for (int i = 0; i < roleCount; i++)
                        {
                            firstRoles.Add(role);
                        }
                    }
                }
            }

            public void RegisterRoleChance(RoleAllocation allocation)
            {
                if (allocation.expected < 10)
                {
                    secondaryRoles.Add(allocation);
                }
                else
                {
                    firstRoles.Add(allocation.role);
                }


            }

            //そのロールは排出される可能性があるかどうか
            public bool Contains(Role role)
            {
                if (firstRoles.Contains(role)) return true;
                if (secondaryRoles.Any((allocation) => { return allocation.role == role; })) return true;
                return false;
            }

            public void Remove(Role role)
            {
                firstRoles.RemoveAll((r) => { return r == role; });
                secondaryRoles.RemoveAll((allocation) => { return allocation.role == role; });
            }
        }

        public class MultiCategoryData : CategoryData
        {
            public MultiCategoryData(AssignRoles assignRoles, int min, int max, RoleCategory category) : base(assignRoles, min, max, category)
            {
            }
        }

        public class SingleCategoryData : CategoryData
        {

            public SingleCategoryData(AssignRoles assignRoles, int min, int max, RoleCategory category) : base(assignRoles, min, max, category)
            {
            }

            public void Assign(AssignMap assignMap, List<PlayerControl> players)
            {
                int left = roles;

                int rand;

                //割り当てられるだけ100%ロールを割り当てる
                while ((left > 0) && (firstRoles.Count > 0) && (players.Count > 0))
                {
                    rand = NebulaPlugin.rnd.Next(firstRoles.Count);
                    Role role = firstRoles[rand];
                    RoleAssignmentPatch.setRoleToRandomPlayer(assignMap, role, players, true);
                    firstRoles.RemoveAt(rand);
                    assignRoles.exclusiveAssignments.RemoveAll((ex) => ex.Exclusive(assignRoles, role));
                    left--;
                }

                //確率で付与されるロールを割り当てる
                int sum;
                while ((left > 0) && (secondaryRoles.Count > 0) && (players.Count > 0))
                {
                    sum = 0;
                    foreach (RoleAllocation allocation in secondaryRoles)
                    {
                        sum += allocation.expected;
                    }

                    if (sum == 0)
                    {
                        break;
                    }

                    rand = NebulaPlugin.rnd.Next(sum);


                    for (int i = 0; i < secondaryRoles.Count; i++)
                    {
                        if (secondaryRoles[i].expected > rand)
                        {
                            Role role = secondaryRoles[i].role;
                            RoleAssignmentPatch.setRoleToRandomPlayer(assignMap, role, players, true);
                            secondaryRoles.RemoveAt(i);
                            assignRoles.exclusiveAssignments.RemoveAll((ex)=>ex.Exclusive(assignRoles,role));
                            left--;
                            sum = 0;
                            break;
                        }
                        rand -= secondaryRoles[i].expected;
                    }
                }
            }
        }

        public SingleCategoryData neutralData { get; }
        public SingleCategoryData crewmateData { get; }
        public SingleCategoryData impostorData { get; }
        public MultiCategoryData multiData { get; }
        public List<Module.ExclusiveAssignment> exclusiveAssignments;

        public AssignRoles(int crewmates, int impostors)
        {
            //カテゴリごとの人数決定とロール割り当て
            int min, max;

            min = (int)CustomOptionHolder.crewmateRolesCountMin.getFloat();
            max = (int)CustomOptionHolder.crewmateRolesCountMax.getFloat();
            crewmateData = new SingleCategoryData(this,min,max,RoleCategory.Crewmate);

            min = (int)CustomOptionHolder.impostorRolesCountMin.getFloat();
            max = (int)CustomOptionHolder.impostorRolesCountMax.getFloat();
            impostorData = new SingleCategoryData(this, min, max, RoleCategory.Impostor);

            min = (int)CustomOptionHolder.neutralRolesCountMin.getFloat();
            max = (int)CustomOptionHolder.neutralRolesCountMax.getFloat();
            neutralData = new SingleCategoryData(this, min, max, RoleCategory.Neutral);

            //ComplexRoleの割り当て
            RoleAllocation[] allocations;
            foreach(Role role in Roles.Roles.AllRoles)
            {
                //無効なロールは入れない
                if ((int)(CustomOptionHolder.GetCustomGameMode() & role.ValidGamemode) == 0) continue;

                if (role.category != RoleCategory.Complex) continue;

                allocations = role.GetComplexAllocations();
                if (allocations == null)  continue;
                foreach (RoleAllocation allocation in allocations)
                {
                    if (allocation.role.category == RoleCategory.Crewmate)
                    {
                        crewmateData.RegisterRoleChance(allocation);
                    }else if (allocation.role.category == RoleCategory.Impostor)
                    {
                        impostorData.RegisterRoleChance(allocation);
                    }
                    else if (allocation.role.category == RoleCategory.Neutral)
                    {
                        neutralData.RegisterRoleChance(allocation);
                    }
                }
            }

            //排他的割り当て
            exclusiveAssignments = new List<Module.ExclusiveAssignment>();
            CustomOptionHolder.AddExclusiveAssignment(ref exclusiveAssignments);
        }

        public bool Contains(Role role)
        {
            switch (role.category)
            {
                case RoleCategory.Crewmate:
                    return crewmateData.Contains(role);
                    break;
                case RoleCategory.Impostor:
                    return impostorData.Contains(role);
                    break;
                case RoleCategory.Neutral:
                    return neutralData.Contains(role);
                    break;
            }
            return false;
        }

        public bool FuzzyContains(Role role)
        {
            if (Contains(role)) return true;

            foreach(Role fuzzy in role.GetImplicateRoles())
            {
                if (Contains(fuzzy)) return true;
            }
            return false;
        }

        public void RemoveRole(Role role)
        {
            switch (role.category)
            {
                case RoleCategory.Crewmate:
                    crewmateData.Remove(role);
                    break;
                case RoleCategory.Impostor:
                    impostorData.Remove(role);
                    break;
                case RoleCategory.Neutral:
                    neutralData.Remove(role);
                    break;
            }
        }
        public void FuzzyRemoveRole(Role role)
        {
            RemoveRole(role);

            foreach (Role fuzzy in role.GetImplicateRoles())
            {
                RemoveRole(fuzzy);
            }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class RoleAssignmentPatch
    {
        public static void Postfix()
        {
            AssignMap assignMap = new AssignMap();

            if (!DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                //標準ロールを割り当てるならこれ
                //assignDefaultRoles(assignMap);
                assignRoles(assignMap);
                
            }

            

            //Ghostをランダムに選択するオプションをここに付けたい
            if (Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode()).RequireGhosts)
            {
                Game.GameData.data.Ghost = new Ghost.Ghosts.TestGhost();
            }

            RPCEventInvoker.SetRoles(assignMap);
        }

        private static void ReduceImpostor(List<PlayerControl> crewmates, List<PlayerControl> impostors)
        {
            int index = NebulaPlugin.rnd.Next(impostors.Count);
            crewmates.Add(impostors[index]);
            impostors.RemoveAt(index);
        }

        private static Dictionary<string,Role> LoadMetaRoleAssignments()
        {
            if (!File.Exists("patches/MetaRoleAssignment.patch")) return new Dictionary<string, Role>();

            var result = new Dictionary<string, Role>();
            string[] strings;
            foreach (string token in System.IO.File.ReadLines("patches/MetaRoleAssignment.patch"))
            {
                strings = token.Split(":");
                if (strings.Length != 2) continue;
                foreach (var role in Roles.Roles.AllRoles)
                {
                    if (role.Name == strings[1])
                    {
                        result.Add(strings[0], role);
                        break;
                    }
                }
            }
            return result;
        }

        private static void assignRoles(AssignMap assignMap)
        {
            //メタ的な割り当てを読み込む
            var metaAssignment = LoadMetaRoleAssignments();

            Game.GameModeProperty property = Game.GameModeProperty.GetProperty(CustomOptionHolder.GetCustomGameMode());

            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();

            List<PlayerControl> impostors = new List<PlayerControl>();

            if (property.RequireImpostors)
            {
                //メタ的にインポスターを要求する場合
                foreach(var entry in metaAssignment)
                {
                    if (entry.Value.category != RoleCategory.Impostor) continue;
                    foreach(var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                    {
                        if (player.name != entry.Key) continue;
                        impostors.Add(player);
                        break;
                    }
                }

                int impostorCount = PlayerControl.GameOptions.NumImpostors;
                if (PlayerControl.AllPlayerControls.Count < 7 && impostorCount > 1) impostorCount = 1;
                else if (PlayerControl.AllPlayerControls.Count < 9 && impostorCount > 2) impostorCount = 2;
                //インポスターを決定する

                var array = Helpers.GetRandomArray(crewmates.Count);
                int i = 0;
                while (impostors.Count < impostorCount) {
                    impostors.Add(crewmates[array[i]]);
                    i++;
                }
                foreach (var imp in impostors)
                {
                    crewmates.Remove(imp);
                }
            }



            /* ロールの割り当て */

            AssignRoles roleData = new AssignRoles(crewmates.Count, impostors.Count);

            //メタ的なロールから割り当てる
            foreach (var entry in metaAssignment)
            {
                foreach (var player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (player.name != entry.Key) continue;
                    assignMap.AssignRole(player.PlayerId, entry.Value.id);

                    crewmates.RemoveAll((p)=>p.PlayerId==player.PlayerId);
                    impostors.RemoveAll((p) => p.PlayerId == player.PlayerId);
                    break;
                }
            }

            roleData.neutralData.Assign(assignMap, crewmates);
            roleData.crewmateData.Assign(assignMap, crewmates);
            roleData.impostorData.Assign(assignMap, impostors);

            //余ったプレイヤーは標準ロールを割り当てる
            while (crewmates.Count > 0)
            {
                setRoleToRandomPlayer(assignMap, property.DefaultCrewmateRole, crewmates, true);
            }
            while (impostors.Count > 0)
            {
                setRoleToRandomPlayer(assignMap, property.DefaultImpostorRole, impostors, true);
            }

            /* ExtraAssignableの割り当て */
            byte currentPriority = Byte.MinValue;
            byte nextPriority = Byte.MaxValue;
            do
            {
                nextPriority = Byte.MaxValue;

                foreach (ExtraAssignable role in Roles.Roles.AllExtraAssignable)
                {
                    //無効なロールは入れない
                    if ((int)(CustomOptionHolder.GetCustomGameMode() & role.ValidGamemode) == 0) continue;

                    if (role.assignmentPriority == currentPriority)
                    {
                        //ロールを割り当てる
                        role.Assignment(assignMap);
                    }
                    else if (role.assignmentPriority > currentPriority && role.assignmentPriority < nextPriority)
                    {
                        //次に割り当てる優先度を決定する
                        nextPriority = role.assignmentPriority;
                    }
                }

                currentPriority = nextPriority;
            } while (currentPriority != Byte.MaxValue);
        }

        public static byte setRoleToRandomPlayer(AssignMap assignMap,Role role, List<PlayerControl> playerList,bool removePlayerFlag)
        {
            if (playerList.Count == 0)
            {
                return 0;
            }

            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            if (removePlayerFlag)
            {
                playerList.RemoveAt(index);
            }

            assignMap.AssignRole(playerId, role.id);
            
            return playerId;
        }
    }
}
