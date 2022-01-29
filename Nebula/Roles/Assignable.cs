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
        public string Name { get; private set; }
        public string LocalizeName { get; private set; }
        public Color Color { get; private set; }

        public Module.CustomOption RoleChanceOption { get; private set; }
        public Module.CustomOption RoleCountOption { get; private set; }

        /// <summary>
        /// FixedRoleCountが有効な場合この関数が呼び出されます。
        /// </summary>
        /// <returns></returns>
        public virtual int GetCustomRoleCount() { return 0; }
        /// <summary>
        /// 配役人数を標準設定に準じさせたくない場合はtrueにしてください。
        /// </summary>
        public bool FixedRoleCount { get; protected set; }

        private int OptionId { get; set; }

        //オプションで使用するID
        static private int OptionAvailableId = 10;

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        public void SetupRoleOptionData()
        {
            RoleChanceOption = Module.CustomOption.Create(OptionAvailableId, Color, "role." + LocalizeName + ".name", CustomOptionHolder.rates, CustomOptionHolder.rates[0], null, true);
            OptionId = OptionAvailableId + 1;
            OptionAvailableId += 10;

            if (!FixedRoleCount)
            {
                RoleCountOption = Module.CustomOption.Create(OptionId, Color.white, "option.roleCount", 0f, 0f, 15f, 1f, RoleChanceOption, false);
                OptionId++;
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
            if (OptionAvailableId == -1)
            {
                return null;
            }
            Module.CustomOption option = new Module.CustomOption(OptionId, color, "role." + this.LocalizeName + "." + name, selections, defaultValue, RoleChanceOption, false, false, "");
            OptionId++;
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

        /// <summary>
        /// 初期化時に呼び出されます。
        /// </summary>
        /// <param name="__instance"></param>
        [RoleLocalMethod]
        public virtual void Initialize(PlayerControl __instance) { }

        /// <summary>
        /// ゲーム開始時にのみ呼ばれる初期化
        /// </summary>
        [RoleLocalMethod]
        public virtual void IntroInitialize(PlayerControl __instance) { }

        /// <summary>
        /// ゲーム中にロールが変更される際に呼ばれます。
        /// </summary>
        /// <param name="__instance"></param>
        [RoleLocalMethod]
        public virtual void FinalizeInGame(PlayerControl __instance) { }

        /// <summary>
        /// プレイヤーが更新されるたびに呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void MyPlayerControlUpdate() { }

        /// <summary>
        /// 毎回呼び出されます。ボタンの処理はこちらで行います。
        /// </summary>
        [RoleLocalMethod]
        public virtual void MyUpdate() { }

        /// <summary>
        ///ゲーム終了時に呼び出されます。 
        /// </summary>
        [RoleLocalMethod]
        public virtual void CleanUp() { }

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
        /// 会議で表示するボタンを設定します。
        /// </summary>
        [RoleLocalMethod]
        public virtual void SetupMeetingButton(MeetingHud __instance)
        {

        }

        /// <summary>
        /// 会議が開始した際に呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnMeetingStart() { }

        /// <summary>
        /// 投票した際に呼び出されます。
        /// </summary>
        [RoleIndefiniteMethod]
        public virtual void OnVote(byte playerId,byte targetId) { }

        [RoleLocalMethod]
        public virtual void MeetingUpdate(MeetingHud __instance, TMPro.TextMeshPro meetingInfo) { }

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
        /// 殺害されて死ぬときに呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnMurdered(byte murderId) { }

        /// <summary>
        /// 誰かをキルしたときに呼び出されます。
        /// </summary>
        /// <param name="targetId"></param>
        [RoleLocalMethod]
        public virtual void OnKillPlayer(byte targetId) { }

        /// <summary>
        /// 理由に関わらず、死んだときに呼び出されます。
        /// OnExiledやOnMurderedの後に呼ばれます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnDied() { }

        /// <summary>
        /// 表示名を編集します。
        /// </summary>
        /// <param name="displayName"></param>
        [RoleGlobalMethod]
        public virtual void EditDisplayName(byte playerId,ref string displayName,bool hideFlag)
        {
        }

        /// <summary>
        /// 表示名を編集します。ゲーム終了時などに使用します。
        /// </summary>
        /// <param name="displayName"></param>
        [RoleGlobalMethod]
        public virtual void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
        }

        /// <summary>
        /// プレイヤー速度に掛ける倍率を設定します。
        /// </summary>
        /// <param name="playerId"></param>
        [RoleGlobalMethod]
        public virtual void MoveSpeedUpdate(byte playerId,ref float speed) { }

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// プレイヤー全員から呼び出されます。
        /// </summary>
        [RoleGlobalMethod]
        public virtual void GlobalUpdate(byte playerId) { }

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
        /// 理由に関わらず、その役職のプレイヤーが死んだときに呼び出されます。
        /// OnExiledやOnMurderedの後に呼ばれます。
        /// </summary>
        [RoleGlobalMethod]
        public virtual void OnDied(byte playerId) { }


        //全てのプレイヤーに対して呼び出されます。
        [RoleGlobalMethod]
        public virtual void GlobalInitialize(PlayerControl __instance) { }

        /// <summary>
        /// ゲーム開始時にのみ呼ばれる初期化
        /// </summary>
        [RoleGlobalMethod]
        public virtual void GlobalIntroInitialize(PlayerControl __instance) { }

        /// <summary>
        /// プレイヤーごとの追加勝利を確認します。
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        [RoleGlobalMethod]
        public virtual bool CheckWin(PlayerControl player, Patches.EndCondition condition) { return false; }

        /// <summary>
        /// どのゲームでも必ず最初に呼び出されます。
        /// </summary>
        /// <returns></returns>
        public virtual void StaticInitialize(){}

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// その役職のプレイヤーが殺害されて死んだときに呼び出されます。
        /// </summary>
        /// <param name="murderId">殺害者のID</param>
        /// <param name="playerId">死亡者のID</param>
        /// <returns>キルが実行されるかどうか</returns>
        [RoleIndefiniteMethod]
        public virtual Helpers.MurderAttemptResult OnMurdered(byte murderId, byte playerId) { return Helpers.MurderAttemptResult.PerformKill; }


        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        //ComplexRoleの子ロールなど、オプション画面で隠したいロールはtrueにしてください。
        public bool IsHideRole { get; protected set; }

        protected Assignable(string name, string localizeName, Color color)
        {

            this.Name = name;
            this.LocalizeName = localizeName;
            this.Color = color;

            //未設定
            this.OptionId = -1;

            this.IsHideRole = false;
        }
    }
}
