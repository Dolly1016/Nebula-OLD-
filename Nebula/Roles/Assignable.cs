using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace Nebula.Roles
{
    public class Assignable
    {
        public string name { get; private set; }
        public string localizeName { get; private set; }
        public Color color { get; private set; }
        
        protected HashSet<Patches.EndCondition> winReasons { get; }
        public virtual bool CheckWin(Patches.EndCondition winReason)
        {
            return winReasons.Contains(winReason);
        }

        public Module.CustomOption roleChanceOption { get; private set; }
        public Module.CustomOption roleCountOption { get; private set; }
        /// <summary>
        /// 配役人数を標準設定に準じさせたくない場合はtrueにしてください。
        /// </summary>
        protected bool FixedRoleCount { get; set; }

        private int optionId { get; set; }

        //オプションで使用するID
        static private int optionAvailableId = 10;

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        public void SetupRoleOptionData()
        {
            roleChanceOption = Module.CustomOption.Create(optionAvailableId, color, "role." + localizeName + ".name", CustomOptionHolder.rates, null, true);
            optionId = optionAvailableId + 1;
            optionAvailableId += 10;

            if (!FixedRoleCount)
            {
                roleCountOption = Module.CustomOption.Create(optionId, Color.white, "option.roleCount", 0f, 0f, 15f, 1f, roleChanceOption, false);
                optionId++;
            }
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
        /// 追放画面が始まるときに呼び出されます。
        /// </summary>
        /// <param name="voters"></param>
        [RoleLocalMethod]
        public virtual void OnExiledPre(byte[] voters) { }

        /// <summary>
        /// 追放されたときに呼び出されます。
        /// 追放画面が終了するときに呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnExiledPost(byte[] voters) { }

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
        [RoleGlobalMethod]
        public virtual void OnExiledPre(byte[] voters, byte playerId) { }

        /// <summary>
        /// その役職のプレイヤーが追放されたときに呼び出されます。
        /// </summary>
        /// <returns>falseの場合死を回避します。</returns>
        [RoleGlobalMethod]
        public virtual bool OnExiledPost(byte[] voters, byte playerId)
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


        //全てのプレイヤーに対して呼び出されます。
        [RoleGlobalMethod]
        public virtual void GlobalInitialize(PlayerControl __instance) { }


        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        //ComplexRoleの子ロールなど、オプション画面で隠したいロールはtrueにしてください。
        protected bool IsHideRole { get; set; }

        protected Assignable(string name, string localizeName, Color color, 
            HashSet<Patches.EndCondition> winReasons)
        {

            this.name = name;
            this.localizeName = localizeName;
            this.color = color;

         

            this.winReasons = winReasons;


            //未設定
            this.optionId = -1;

            this.IsHideRole = false;
        }
    }
}
