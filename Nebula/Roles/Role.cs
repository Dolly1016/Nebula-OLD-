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
        Neutral
    }

    public abstract class Role
    {
        public byte id { get; private set; }
        public string name { get; private set; }
        public string localizeName { get; private set; }
        public Color color { get; private set; }
        public RoleCategory category{ get; }
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

        public Module.CustomOption roleChanceOption { get; private set; }
        public Module.CustomOption roleCountOption { get; private set; }

        private int optionId { get; set; }

        public bool checkWin(Patches.EndCondition winReason)
        {
            return this.winReasons.Contains(winReason);
        }

        //使用済みロールID
        static private byte maxId = 0;

        //オプションで使用するID
        static private int optionAvailableId = 10;

        public void SetupRoleOptionData()
        {
            roleChanceOption =Module.CustomOption.Create(optionAvailableId, color,"role."+localizeName+".name", CustomOptionHolder.rates, null, true);
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
            Module.CustomOption option=new Module.CustomOption(optionId, color, "role." + this.localizeName + "." + name, selections, defaultValue, roleChanceOption, false, false, "");
            optionId++;
            return option;
        }

        protected Module.CustomOption CreateOption(Color color, string name, string[] selections)
        {
            return CreateOption(color,name,selections,"");
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

        //ボタン設定を初期化します。自身のロールについてのみ初期化を行います。
        public virtual void ButtonInitialize(HudManager __instance) { }

        public virtual void Initialize(PlayerControl __instance) { }
        //
        public virtual void MyPlayerControlUpdate()　{ }

        public virtual void CleanUp() { }

        public virtual void GetLightRadius(ref float radius) {  }

        public virtual void SetKillCoolDown(ref float multiplier,ref float addition) { }

        public virtual void OnMeetingEnd() { }

        protected Role(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask,bool canUseVents,bool canMoveInVents,
            bool ignoreBlackout,bool useImpostorLightRadius)
        {
            this.id = maxId;
            maxId++;

            this.name = name;
            this.localizeName = localizeName;
            this.color = color;

            this.category = category;

            this.side = side;
            this.introMainDisplaySide=introMainDisplaySide;
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

            //未設定
            this.optionId = -1;
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

        public static void AllCleanUp()
        {
            foreach (Role role in Roles.AllRoles)
            {
                role.CleanUp();
            }
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
                    players.Add(player);
                }
                else
                {
                    data = Game.GameData.data.players[player.PlayerId];

                    foreach (Side side in data.role.introInfluenceSides)
                    {
                        if (myData.role.introDisplaySides.Contains(side))
                        {
                            players.Add(player);
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
                role.SetupRoleOptionData();
                role.LoadOptionData();
            }
        }
    }
}
