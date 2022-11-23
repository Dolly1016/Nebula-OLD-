using Nebula.Patches;
using Nebula.Roles.CrewmateRoles;

namespace Nebula.Roles
{
    public delegate EndCondition? EndCriteriaChecker(Patches.PlayerStatistics statistics, ShipStatus status);
    public delegate EndCondition? EndTakeoverChecker(EndCondition endCondition,Patches.PlayerStatistics statistics, ShipStatus status);

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
        public virtual RoleCategory oracleCategory { get { return category; } }
        public virtual Role oracleRole { get { return this; } }

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
        public HashSet<Side> introDisplaySides { get; private set; }
        /// <summary>
        /// 自身が同じチームとして表示される陣営
        /// </summary>
        public HashSet<Side> introInfluenceSides { get; private set; }

        /// <summary>
        /// 関連性のあるロール
        /// </summary>
        public HashSet<Role> RelatedRoles { get; set; }

        public class RelatedRoleData
        {
            public int id { get; }
            public string display { get; }
            public int min { get; }
            public int max { get; }
            public string suffix { get; }
            public string[]? replaceArray { get; }
            public RelatedRoleData(int id,string display,int min,int max,string suffix="")
            {
                this.id = id;
                this.display = display;
                this.min = min;
                this.max = max;
                this.suffix = suffix;
                this.replaceArray = null;
            }

            public RelatedRoleData(int id, string display, int min, int max, string[] replaceArray)
            {
                this.id = id;
                this.display = display;
                this.min = min;
                this.max = max;
                this.suffix = "";
                this.replaceArray = replaceArray;
            }
        }
        /// <summary>
        /// 関連のあるRoleDataのIdを返します。
        /// </summary>
        public virtual RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[0]; }

        /// <summary>
        /// 追加で割り当てるRoleを返します。
        /// </summary>
        public virtual Role[] AssignedRoles { get => new Role[] { this }; }

        /// <summary>
        /// 割り当て時に消費するコストを返します。
        /// </summary>
        public virtual int AssignmentCost { get => 1; }

        public HashSet<Patches.EndCondition> winReasons { get; }
        public virtual bool CheckWin(PlayerControl player, Patches.EndCondition winReason)
        {
            //Madmateの場合は元陣営の勝利を無効化する
            if (player.IsMadmate()) return false;

            //単独勝利ロール
            if (winReason.TriggerRole != null)
                return winReason.TriggerRole.Winner == player.PlayerId;


            return winReasons.Contains(winReason);
        }

        public Color VentColor { get; set; }
        public VentPermission VentPermission { get; set; }
        public float VentDurationMaxTimer { get; set; }
        public float VentCoolDownMaxTimer { get; set; }
        protected bool canMoveInVents { get; set; }
        public virtual bool CanMoveInVents { get { return canMoveInVents; } }
        protected bool canInvokeSabotage { get; set; }
        public virtual bool CanInvokeSabotage { get { return canInvokeSabotage; } }
        public bool RemoveAllTasksOnDead { get; set; }
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
        //Modで管理するFakeTaskを所持しているかどうか
        protected bool hasFakeTask { get; }
        public bool HasFakeTask(byte playerId) => hasFakeTask;
        public bool DeceiveImpostorInNameDisplay { get; set; }
        public virtual bool IsGuessableRole { get; protected set; }

        public bool CanCallEmergencyMeeting { get; protected set; }

        public bool HideKillButtonEvenImpostor { get; protected set; }

        public virtual bool CanHaveExtraAssignable(ExtraAssignable extraRole)
        {
            Module.CustomOption? option;
            if(extraAssignableOptions.TryGetValue(extraRole,out option) && option != null)
            {
                return CustomOptionHolder.advanceRoleOptions.getBool() ? option.getBool() : !defaultUnassignable.Contains(extraRole);
            }
            return false;
        }

        public virtual bool CanHaveGhostRole
        {
            get => true;
        }

        public virtual bool IsSecondaryGenerator { get { return false; } }


        public virtual List<Role> GetImplicateRoles() { return new List<Role>(); }
        public virtual List<ExtraRole> GetImplicateExtraRoles() { return new List<ExtraRole>(); }
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
        /// 実質的なロールを取得します。
        /// </summary>
        /// <returns></returns>
        public virtual Role GetActualRole(Game.PlayerData player) => this;
        public override bool HasCrewmateTask(byte playerId)
        {
            return side != Side.Impostor && !HasFakeTask(playerId);
        }

        public override bool HasExecutableFakeTask(byte playerId)
        {
            return false;
        }

        public override void EditCoolDown(CoolDownType type, float count)
        {
            if ((type & CoolDownType.ImpostorsKill) != 0)
            {
                if (FastDestroyableSingleton<HudManager>.Instance.KillButton.gameObject.active)
                {
                    PlayerControl.LocalPlayer.killTimer -= count;
                    if (PlayerControl.LocalPlayer.killTimer < 0f) PlayerControl.LocalPlayer.killTimer = 0f;

                    FastDestroyableSingleton<HudManager>.Instance.KillButton.ShowButtonText("+" + count + "s");
                }
            }
        }

        protected Dictionary<ExtraAssignable, Module.CustomOption?> extraAssignableOptions = new Dictionary<ExtraAssignable, Module.CustomOption?>();
        protected HashSet<ExtraAssignable> defaultUnassignable = new HashSet<ExtraAssignable>();

        public bool DefaultExtraAssignableFlag(ExtraAssignable role)
        {
            return !defaultUnassignable.Contains(role);
        }

        protected Module.CustomOption? CanBeLoversOption=null;
        protected Module.CustomOption? CanBeGuesserOption=null;
        protected Module.CustomOption? CanBeDrunkOption=null;
        protected Module.CustomOption? CanBeBloodyOption = null;
        protected Module.CustomOption? CanBeMadmateOption = null;
        protected Module.CustomOption? CanBeSecretOption = null;

        sealed public override void SetupRoleOptionData()
        {
            Module.CustomOptionTab tab = Module.CustomOptionTab.None;
            if (this == Roles.VOID) tab = Module.CustomOptionTab.AdvancedSettings;
            else if (this == Roles.F_Crewmate) tab = Module.CustomOptionTab.CrewmateRoles;
            else if (this == Roles.Avenger) tab = Module.CustomOptionTab.Modifiers;
            else if (this.side == Side.GamePlayer) tab = Module.CustomOptionTab.EscapeRoles;
            else
            {
                switch (category)
                {
                    case RoleCategory.Crewmate:
                        tab = Module.CustomOptionTab.CrewmateRoles;
                        break;
                    case RoleCategory.Impostor:
                        tab = Module.CustomOptionTab.ImpostorRoles;
                        break;
                    case RoleCategory.Neutral:
                        tab = Module.CustomOptionTab.NeutralRoles;
                        break;
                    case RoleCategory.Complex:
                        tab = Module.CustomOptionTab.NeutralRoles;
                        break;
                }
            }
            SetupRoleOptionData(tab);

            if (Allocation == AllocationType.None) return;

            Module.CustomOption? option;

            foreach(var extraRole in Roles.AllExtraAssignable)
            {
                if (extraAssignableOptions.ContainsKey(extraRole)) continue;

                option = extraRole.RegisterAssignableOption(this);
                if (option != null) extraAssignableOptions.Add(extraRole, option);
            }

            TopOption.NameDecorator = new Module.CustomOptionDecorator((original, option) =>
            {
                //追加役職化した場合は何もしない
                if (IsSecondaryGenerator) return original;

                string suffix = "";

                foreach(var extra in Roles.AllExtraAssignable)
                {
                    extra.EditSpawnableRoleShower(ref suffix,this);
                }

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

            if (RoleChanceOption.getSelection() == 9)
                DefinitiveRoles.Add(this, (int)(RoleCountOption?.getFloat()??GetCustomRoleCount()));
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
            this.canMoveInVents = canMoveInVents;
            this.IgnoreBlackout = ignoreBlackout;

            this.UseImpostorLightRadius = useImpostorLightRadius;

            this.hasFakeTask = hasFakeTask;

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
            this.canFixSabotage = true;

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
                        data = Game.GameData.data.AllPlayers[player.PlayerId];

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
        public void ReflectRoleEyesight(RoleBehaviour role)
        {
            role.AffectedByLightAffectors = !UseImpostorLightRadius;
        }

        static public void LoadAllOptionData()
        {
            foreach (Role role in Roles.AllRoles)
            {
                if(!role.CreateOptionFollowingRelatedRole)
                    role.CreateRoleOption();
            }
        }

        public virtual bool BlocksShowTaskOverlay { get => false; }

        public virtual void OnShowMapTaskOverlay(MapTaskOverlay mapTaskOverlay,Action<Vector2,bool> iconGenerator) {}
    }
}
