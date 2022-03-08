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

    public enum VentPermission
    {
        CanUseUnlimittedVent,
        CanUseLimittedVent,
        CanNotUse
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
        public VentPermission VentPermission { get; set; }
        public float VentDurationMaxTimer { get; set; }
        public float VentCoolDownMaxTimer { get; set; }
        public bool CanMoveInVents { get; set; }
        public bool canInvokeSabotage { get; set; }
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

        public virtual bool CanBeLovers
        {
            get
            {
                return CustomOptionHolder.advanceRoleOptions.getBool() ?
                    (CanBeLoversOption != null ? CanBeLoversOption.getBool() : true) : DefaultCanBeLovers;
            }
        }
        public virtual bool CanBeGuesser
        {
            get
            {
                return CustomOptionHolder.advanceRoleOptions.getBool() ?
                    (CanBeGuesserOption!=null ? CanBeGuesserOption.getBool():true) : DefaultCanBeGuesser;
            }
        }
        public virtual bool CanBeDrunk { get {
                return CustomOptionHolder.advanceRoleOptions.getBool() ?
                     (CanBeDrunkOption != null ? CanBeDrunkOption.getBool() : true) : DefaultCanBeDrunk;
            } }

        public bool DefaultCanBeLovers { get; set; }
        public bool DefaultCanBeGuesser { get; set; }
        public bool DefaultCanBeDrunk { get; set; }


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

        private Module.CustomOption CanBeLoversOption=null;
        private Module.CustomOption CanBeGuesserOption=null;
        private Module.CustomOption CanBeDrunkOption=null;
        sealed public override void SetupRoleOptionData()
        {
            base.SetupRoleOptionData();

            CanBeLoversOption = CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeLovers", DefaultCanBeLovers, true).HiddenOnDisplay(true);
            CanBeLoversOption.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
            CanBeLoversOption.AddCustomPrerequisite(() => { return Roles.Lover.IsSpawnable(); });

            CanBeGuesserOption = CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeGuesser", DefaultCanBeGuesser, true).HiddenOnDisplay(true);
            CanBeGuesserOption.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
            CanBeGuesserOption.AddCustomPrerequisite(() => { return Roles.SecondaryGuesser.IsSpawnable(); });

            CanBeDrunkOption = CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeDrunk", DefaultCanBeDrunk, true).HiddenOnDisplay(true);
            CanBeDrunkOption.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
            CanBeDrunkOption.AddCustomPrerequisite(() => { return Roles.Drunk.IsSpawnable(); });

            RoleChanceOption.Decorator = new Module.CustomOptionDecorator((original, option) =>
            {
                string suffix = "";
                if (Roles.Lover.IsSpawnable() && CanBeLovers) suffix += Helpers.cs(Roles.Lover.Color, "♥");
                if (Roles.SecondaryGuesser.IsSpawnable() && CanBeGuesser) suffix += Helpers.cs(Roles.SecondaryGuesser.Color, "⊕");
                if (Roles.Drunk.IsSpawnable() && CanBeDrunk) suffix += Helpers.cs(Roles.Drunk.Color, "〻");

                return suffix == "" ? original : (original + " " + suffix);
                
            }
            );
        }

        /// <summary>
        /// この役職が発生しうるかどうか調べます
        /// </summary>
        public override bool IsSpawnable()
        {
            if (category == RoleCategory.Complex) return false;

            return base.IsSpawnable();
        }

        public virtual void SpawnableTest(ref Dictionary<Role,int> DefinitiveRoles ,ref HashSet<Role> SpawnableRoles)
        {
            if (!IsSpawnable()) return;

            if (RoleChanceOption == null) return;

            if (RoleChanceOption.getSelection() == 10f)
                DefinitiveRoles.Add(this, (int)RoleCountOption.getFloat());
            else
                SpawnableRoles.Add(this);
        }

        //Complexなロールカテゴリーについてのみ呼ばれます。
        public virtual AssignRoles.RoleAllocation[] GetComplexAllocations()
        {
            return null;
        }

        protected Role(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
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

            this.VentPermission = canUseVents;
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

            this.VentDurationMaxTimer = 10f;
            this.VentCoolDownMaxTimer = 20f;
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
