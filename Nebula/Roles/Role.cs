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

    public abstract class Role
    {
        public byte id { get; private set; }
        public string name { get; private set; }
        public string localizeName { get; private set; }
        public Color color { get; private set; }
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

        private HashSet<Patches.EndCondition> winReasons { get; }
        public bool CheckWin(Patches.EndCondition winReason)
        {
            return winReasons.Contains(winReason);
        }

        public Module.CustomOption roleChanceOption { get; private set; }
        public Module.CustomOption roleCountOption { get; private set; }

        private int optionId { get; set; }

        //使用済みロールID
        static private byte maxId = 0;

        //オプションで使用するID
        static private int optionAvailableId = 10;

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        public void SetupRoleOptionData()
        {
            roleChanceOption = Module.CustomOption.Create(optionAvailableId, color, "role." + localizeName + ".name", CustomOptionHolder.rates, null, true);
            optionId = optionAvailableId + 1;
            optionAvailableId += 10;

            roleCountOption = Module.CustomOption.Create(optionId, Color.white, "option.roleCount", 0f, 0f, 15f, 1f, roleChanceOption, false);
            optionId++;
        }

        /// <summary>
        /// ここでコンフィグ設定を行います。
        /// CreateOptionメソッドを使用してください。
        /// </summary>
        public virtual void LoadOptionData()
        {

        }

        private Module.CustomOption CreateOption(Color color, string name, object[] selections, System.Object defaultValue)
        {
            if (optionAvailableId == -1)
            {
                return null;
            }
            Module.CustomOption option = new Module.CustomOption(optionId, color, "role." + this.localizeName + "." + name, selections, defaultValue, roleChanceOption, false, false, "");
            optionId++;
            return option;
        }

        protected Module.CustomOption CreateOption(Color color, string name, string[] selections)
        {
            return CreateOption(color, name, selections, "");
        }

        protected Module.CustomOption CreateOption(Color color, string name, float defaultValue, float min, float max, float step)
        {
            List<float> selections = new List<float>();
            for (float s = min; s <= max; s += step)
                selections.Add(s);
            return CreateOption(color, name, selections.Cast<object>().ToArray(), defaultValue);
        }

        protected Module.CustomOption CreateOption(Color color, string name, bool defaultValue)
        {
            return CreateOption(color, name, new string[] { "option.switch.off", "option.switch.on" }, defaultValue ? "option.switch.on" : "option.switch.off");
        }


        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/


        /// <summary>
        /// ボタン設定を初期化します。全ロールに対して行います。
        /// </summary>
        [RoleLocalMethod]
        public virtual void ButtonInitialize(HudManager __instance) { }

        /// <summary>
        /// ボタンを有効化します。自身のロールについてのみ行います。
        /// </summary>
        [RoleLocalMethod]
        public virtual void ButtonActivate() { }

        /// <summary>
        /// ボタンを無効化します。自身のロールについてのみ行います。
        /// </summary>
        [RoleLocalMethod]
        public virtual void ButtonDeactivate() { }

        //
        [RoleLocalMethod]
        public virtual void Initialize(PlayerControl __instance) { }

        /// <summary>
        /// 毎ティック呼び出されます
        /// </summary>
        [RoleLocalMethod]
        public virtual void MyPlayerControlUpdate() { }

        /// <summary>
        ///ゲーム終了時に呼び出されます。 
        /// </summary>
        [RoleLocalMethod]
        public virtual void ButtonCleanUp() { }

        /// <summary>
        /// //明かりの大きさを調整します。毎ティック呼び出されます。
        /// </summary>
        /// <param name="radius">現行の明かりの大きさ</param>
        [RoleLocalMethod]
        public virtual void GetLightRadius(ref float radius) { }

        /// <summary>
        /// キルクールダウンを設定する際に呼び出されます。
        /// </summary>
        /// <param name="multiplier">クールダウンに掛け合わせる値</param>
        /// <param name="addition">クールダウンに足しこむ値</param>
        [RoleLocalMethod]
        public virtual void SetKillCoolDown(ref float multiplier, ref float addition) { }

        /// <summary>
        /// 会議が終了した際に呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnMeetingEnd() { }

        /// <summary>
        /// 誰かが殺害されたときに呼び出されます。
        /// プレイヤー自身のロールについてのみ呼び出されます。
        /// </summary>
        /// <param name="murderId">殺害者のプレイヤーID</param>
        /// <param name="targetId">被害者のプレイヤーID</param>
        [RoleLocalMethod]
        public virtual void OnAnyoneMurdered(byte murderId, byte targetId) { }

        /// <summary>
        /// 追放されたときに呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnExiled(byte[] voters) { }

        /// <summary>
        /// 殺害されて死んだときに呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnMurdered(byte murderId) { }

        /// <summary>
        /// 理由に関わらず、死んだときに呼び出されます。
        /// OnExiledやOnMurderedの後に呼ばれます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnDied() { }

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/


        /// <summary>
        /// その役職のプレイヤーが追放されたときに呼び出されます。
        /// </summary>
        /// <returns>falseの場合死を回避します。</returns>
        [RoleGlobalMethod]
        public virtual bool OnExiled(byte[] voters,byte playerId)
        {
            return true;
        }

        /// <summary>
        /// その役職のプレイヤーが殺害されて死んだときに呼び出されます。
        /// </summary>
        [RoleGlobalMethod]
        public virtual void OnMurdered(byte murderId, byte playerId) { }

        /// <summary>
        /// 理由に関わらず、その役職のプレイヤーが死んだときに呼び出されます。
        /// OnExiledやOnMurderedの後に呼ばれます。
        /// </summary>
        [RoleGlobalMethod]
        public virtual void OnDied(byte playerId) { }


        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        //ComplexRoleの子ロールなど、オプション画面で隠したいロールはtrueにしてください。
        protected bool IsHideRole { get; set; }

        //Complexなロールカテゴリーについてのみ呼ばれます。
        public virtual AssignRoles.RoleAllocation[] GetComplexAllocations()
        {
            return null;
        }

        protected Role(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, bool canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius)
        {
            this.id = maxId;
            maxId++;

            this.name = name;
            this.localizeName = localizeName;
            this.color = color;

            this.category = category;

            this.side = side;
            this.introMainDisplaySide = introMainDisplaySide;
            this.introDisplaySides = introDisplaySides;
            this.introInfluenceSides = introInfluenceSides;

            this.canUseVents = canUseVents;
            this.canMoveInVents = canMoveInVents;
            this.ignoreBlackout = ignoreBlackout;

            this.winReasons = winReasons;
            this.useImpostorLightRadius = useImpostorLightRadius;

            this.hasFakeTask = hasFakeTask;

            this.lightRadiusMin = 1.0f;
            this.lightRadiusMax = 1.0f;

            this.deceiveImpostorInNameDisplay = false;

            this.ventColor = Palette.ImpostorRed;

            //未設定
            this.optionId = -1;

            this.IsHideRole = false;
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
