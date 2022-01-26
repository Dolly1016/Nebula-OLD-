using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Nebula.Patches;

namespace Nebula.Roles
{
    public delegate EndCondition EndCriteriaChecker(Patches.PlayerStatistics statistics, ShipStatus status);

    public enum RoleCategory
    {
        Crewmate,
        Impostor,
        Neutral,
        Complex
    }

    public abstract class Role : Assignable
    {
        public byte id { get; private set; }
        public RoleCategory category { get; }
        /// <summary>
        /// 勝ち負け判定に使用する陣営
        /// </summary>
        public Side side { get; private set; }
        /// <summary>
        /// ゲーム開始時に自身の陣営として表示される陣営
        /// </summary>
        public Side introMainDisplaySide { get; private set; }
        /// <summary>
        /// ゲーム開始時に表示する陣営群
        /// </summary>
        private HashSet<Side> introDisplaySides { get; set; }
        /// <summary>
        /// 自身が同じチームとして表示される陣営
        /// </summary>
        private HashSet<Side> introInfluenceSides { get; }

        protected HashSet<Patches.EndCondition> winReasons { get; }
        public virtual bool CheckWin(Patches.EndCondition winReason)
        {
            return winReasons.Contains(winReason);
        }

        public Color ventColor { get; set; }
        public bool canUseVents { get; set; }
        public bool canMoveInVents { get; set; }
        /// <summary>
        /// 停電が効かない場合true
        /// </summary>
        public bool ignoreBlackout { get; set; }
        /// <summary>
        /// ライトの最小範囲　停電が効かない場合無効
        /// </summary>
        public float lightRadiusMin { get; set; }
        /// <summary>
        /// 通常のライト範囲
        /// </summary>
        public float lightRadiusMax { get; set; }
        public bool useImpostorLightRadius { get; set; }
        //Modで管理するFakeTaskを所持しているかどうか(Impostorは対象外)
        public bool hasFakeTask { get; }
        public bool deceiveImpostorInNameDisplay { get; set; }
        public bool IsGuessableRole { get; protected set; }

        public bool CanCallEmergencyMeeting { get; protected set; }

        public bool HideKillButtonEvenImpostor { get; protected set; }

        //使用済みロールID
        static private byte maxId = 0;

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// ホストが受け取ったイベントをイベントに関わるプレイヤーに委譲します
        /// </summary>
        /// <param name="actionId"></param>
        [RoleLocalMethod]
        public virtual void UniqueAction(byte actionId) { }

        /// <summary>
        /// 正常にロールデータが更新された際に呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnUpdateRoleData(int dataId, int newData) { }

        //Complexなロールカテゴリーについてのみ呼ばれます。
        public virtual AssignRoles.RoleAllocation[] GetComplexAllocations()
        {
            return null;
        }

        protected Role(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, bool canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius):
            base(name,localizeName,color)
        {
            this.id = maxId;
            maxId++;

            this.category = category;

            this.side = side;
            this.introMainDisplaySide = introMainDisplaySide;
            this.introDisplaySides = introDisplaySides;
            this.introInfluenceSides = introInfluenceSides;

            this.canUseVents = canUseVents;
            this.canMoveInVents = canMoveInVents;
            this.ignoreBlackout = ignoreBlackout;

            this.useImpostorLightRadius = useImpostorLightRadius;

            this.hasFakeTask = hasFakeTask;

            this.lightRadiusMin = 1.0f;
            this.lightRadiusMax = 1.0f;

            this.deceiveImpostorInNameDisplay = false;
            this.IsGuessableRole = true;
            this.ventColor = Palette.ImpostorRed;

            this.winReasons = winReasons;

            this.CanCallEmergencyMeeting = true;
            this.HideKillButtonEvenImpostor = false;
        }

        public static Role GetRoleById(byte id)
        {
            foreach(Role role in Roles.AllRoles){
                if (role.id == id)
                {
                    return role;
                }
            }
            return null;
        }

        static public void ExtractDisplayPlayers(ref Il2CppSystem.Collections.Generic.List<PlayerControl> players)
        {
            players.Clear();

            players.Add(PlayerControl.LocalPlayer);

            Game.PlayerData data;
            Game.PlayerData myData = Game.GameData.data.myData.getGlobalData();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (myData.role.introMainDisplaySide.showFullMemberAtIntro)
                {
                    if (!players.Contains(player))
                    {
                        players.Add(player);
                    }
                }
                else
                {
                    data = Game.GameData.data.players[player.PlayerId];

                    foreach (Side side in data.role.introInfluenceSides)
                    {
                        if (myData.role.introDisplaySides.Contains(side))
                        {
                            if (!players.Contains(player))
                            {
                                players.Add(player);
                            }
                            break;
                        }
                    }
                }
            }
        }

        static public void LoadAllOptionData()
        {
            foreach (Role role in Roles.AllRoles)
            {
                if (!role.IsHideRole)
                {
                    role.SetupRoleOptionData();
                    role.LoadOptionData();
                }
            }
        }
    }
}
