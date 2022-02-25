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

        /// <summary>
        /// 関連性のあるロール
        /// </summary>
        public HashSet<Role> RelatedRoles { get; set; }

        protected HashSet<Patches.EndCondition> winReasons { get; }
        public virtual bool CheckWin(Patches.EndCondition winReason)
        {
            //単独勝利ロール
            if (winReason.TriggerRole != null)
                return winReason.TriggerRole.Winner == PlayerControl.LocalPlayer.PlayerId;
            
            return winReasons.Contains(winReason);
        }

        public Color VentColor { get; set; }
        public bool CanUseVents { get; set; }
        public bool canInvokeSabotage { get; set; }
        public bool CanMoveInVents { get; set; }
        /// <summary>
        /// 停電が効かない場合true
        /// </summary>
        public bool IgnoreBlackout { get; set; }
        /// <summary>
        /// ライトの最小範囲　停電が効かない場合無効
        /// </summary>
        public float LightRadiusMin { get; set; }
        /// <summary>
        /// 通常のライト範囲
        /// </summary>
        public float LightRadiusMax { get; set; }
        public bool UseImpostorLightRadius { get; set; }
        //Modで管理するFakeTaskを所持しているかどうか(Impostorは対象外)
        public bool HasFakeTask { get; }
        //FakeTaskは実行可能かどうか
        public bool FakeTaskIsExecutable { get; protected set; }
        public bool DeceiveImpostorInNameDisplay { get; set; }
        public bool IsGuessableRole { get; protected set; }

        public bool CanCallEmergencyMeeting { get; protected set; }

        public bool HideKillButtonEvenImpostor { get; protected set; }


        public virtual List<Role> GetImplicateRoles() { return new List<Role>(); }
        /// <summary>
        /// 排他的割り当てオプションに表示しない場合true
        /// </summary>
        public bool HideInExclusiveAssignmentOption;

        //使用済みロールID
        static private byte maxId = 0;

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// 誰かの役職が変化したときに呼び出されます。
        /// プレイヤー自身のロールについてのみ呼び出されます。
        /// </summary>
        /// <param name="playerId">ロールが変化したプレイヤーのプレイヤーID</param>
        [RoleLocalMethod]
        public virtual void OnAnyoneRoleChanged(byte playerId) { }

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

        /// <summary>
        /// ロール読み込み後、ロールの関連性を設定する際に呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnRoleRelationSetting() { }

        /// <summary>
        /// この役職が発生しうるかどうか調べます
        /// </summary>
        public virtual bool IsSpawnable()
        {
            if (category == RoleCategory.Complex) return false;
            if (IsHideRole) return false;

            if (RoleChanceOption.getSelection() == 0) return false;
            if (!FixedRoleCount && RoleCountOption.getFloat() == 0f) return false;

            return true;
        }

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

            this.CanUseVents = canUseVents;
            this.CanMoveInVents = canMoveInVents;
            this.IgnoreBlackout = ignoreBlackout;

            this.UseImpostorLightRadius = useImpostorLightRadius;

            this.HasFakeTask = hasFakeTask;

            this.LightRadiusMin = 1.0f;
            this.LightRadiusMax = 1.0f;

            this.DeceiveImpostorInNameDisplay = false;
            this.IsGuessableRole = true;
            this.VentColor = Palette.ImpostorRed;

            this.winReasons = winReasons;

            this.CanCallEmergencyMeeting = true;
            this.HideKillButtonEvenImpostor = false;

            this.HideInExclusiveAssignmentOption = false;

            this.canInvokeSabotage = (category == RoleCategory.Impostor);

            this.RelatedRoles = new HashSet<Role>();
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

            //SHOW_ONLY_MEは自信を最初に追加しているのでこれ以上何もしない
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                switch (myData.role.introMainDisplaySide.ShowOption)
                {
                    case Side.IntroDisplayOption.STANDARD:
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
                        break;
                    case Side.IntroDisplayOption.SHOW_ALL:
                        if (!players.Contains(player))
                        {
                            players.Add(player);
                        }
                        break;
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
                }
                role.LoadOptionData();
            }
        }
    }
}
