using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace Nebula.Roles
{
    [Flags]
    public enum CoolDownType
    {
        ImpostorsKill = 0x01,
        ImpostorsAbility = 0x02,
        CrewmatesKill = 0x04,
        CrewmatesAbility = 0x08,
    }
    public class Assignable
    {
        public string Name { get; private set; }
        public string LocalizeName { get; private set; }
        public Color Color { get; private set; }

        public Module.CustomOption TopOption { get; private set; }
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
        /// <summary>
        /// 割り当て確率を設定しない場合はtrueにしてください。
        /// </summary>
        public bool ExceptBasicOption { get; protected set; }

        public virtual bool IsUnsuitable { get { return false; } }

        private int OptionId { get; set; }

        private int OptionCapacity { get; set; }

        //オプションで使用するID
        static private int OptionAvailableId = 10;

        public Module.CustomGameMode ValidGamemode { get; set; }

        protected bool canFixSabotage { get; set; }
        public virtual bool CanFixSabotage { get { return canFixSabotage; } }

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        protected void SetupRoleOptionData(Module.CustomOptionTab tab)
        {
            if (ExceptBasicOption)
            {
                TopOption = Module.CustomOption.Create(OptionAvailableId, Color, "role." + LocalizeName + ".name", new string[] { "option.empty" }, "option.empty", null, true, false, "", tab);
            }
            else
            {
                RoleChanceOption = Module.CustomOption.Create(OptionAvailableId, Color, "role." + LocalizeName + ".name", CustomOptionHolder.rates, CustomOptionHolder.rates[0], null, true,false,"",tab);
                TopOption = RoleChanceOption;
            }
            TopOption.GameMode = ValidGamemode | Module.CustomGameMode.FreePlay;

            OptionId = OptionAvailableId + 1;
            OptionAvailableId += OptionCapacity;

            if (!FixedRoleCount && !ExceptBasicOption)
            {
                RoleCountOption = Module.CustomOption.Create(OptionId, Color.white, "option.roleCount", 0f, 0f, 15f, 1f, TopOption, false).SetIdentifier("role." + LocalizeName + ".roleCount");
                RoleCountOption.GameMode = ValidGamemode|Module.CustomGameMode.FreePlay;
                OptionId++;
            }
        }

        public virtual void SetupRoleOptionData()
        {
            SetupRoleOptionData(Module.CustomOptionTab.None);
        }

        /// <summary>
        /// ここでコンフィグ設定を行います。
        /// CreateOptionメソッドを使用してください。
        /// </summary>
        public virtual void LoadOptionData()
        {

        }

        private Module.CustomOption CreateOption(Color color, string name, object[] selections, System.Object defaultValue,bool isGeneral=false)
        {
            if (OptionAvailableId == -1)
            {
                return null;
            }
            Module.CustomOption option = new Module.CustomOption(OptionId, color, (isGeneral ? "" : "role." + this.LocalizeName + ".") + name, selections, defaultValue, TopOption, false, false, "",Module.CustomOptionTab.None);
            option.GameMode = ValidGamemode | Module.CustomGameMode.FreePlay;

            OptionId++;
            return option;
        }

        protected Module.CustomOption CreateOption(Color color, string name, string[] selections, bool isGeneral = false)
        {
            return CreateOption(color, name, selections, "", isGeneral);
        }

        protected Module.CustomOption CreateOption(Color color, string name, float defaultValue, float min, float max, float step, bool isGeneral = false)
        {
            List<float> selections = new List<float>();
            for (float s = min; s <= max; s += step)
                selections.Add(s);
            return CreateOption(color, name, selections.Cast<object>().ToArray(), defaultValue,isGeneral);
        }

        protected Module.CustomOption CreateOption(Color color, string name, bool defaultValue, bool isGeneral = false)
        {
            return CreateOption(color, name, new string[] { "option.switch.off", "option.switch.on" }, defaultValue ? "option.switch.on" : "option.switch.off", isGeneral);
        }


        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// バイタルを開いた時に呼び出されます。
        /// </summary>
        /// <param name="__instance"></param>
        [RoleLocalMethod]
        public virtual void OnVitalsOpen(VitalsMinigame __instance)
        {

        }

        /// <summary>
        /// バイタルを開いている時に呼び出されます。
        /// </summary>
        /// <param name="__instance"></param>
        [RoleLocalMethod]
        public virtual void VitalsUpdate(VitalsMinigame __instance)
        {

        }

        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// ボタン設定を初期化します。全ロールに対して行います。
        /// </summary>
        [RoleLocalMethod]
        public virtual void ButtonInitialize(HudManager __instance) { }


        /*--------------------------------------------------------------------------------------*/

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

        [RoleLocalMethod]
        public virtual void OnSetTasks(Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> tasks){ }

        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// ゲーム中にロールが変更される際に呼ばれます。
        /// </summary>
        /// <param name="__instance"></param>
        [RoleLocalMethod]
        public virtual void FinalizeInGame(PlayerControl __instance) { }

        /// <summary>
        ///ゲーム終了時に呼び出されます。 
        /// </summary>
        [RoleLocalMethod]
        public virtual void CleanUp() { }

        /*--------------------------------------------------------------------------------------*/

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
        /// マップを開いている間のみ呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void MyMapUpdate(MapBehaviour mapBehaviour) { }

        /// <summary>
        /// タスクを完了すると呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnTaskComplete() { }

        public virtual void EditCoolDown(CoolDownType type,float count)
        {

        }

        /// <summary>
        /// 自身がサボタージュを引き起こすと呼ばれます。
        /// </summary>
        /// <param name="systemType"></param>
        [RoleLocalMethod]
        public virtual void OnInvokeSabotage(SystemTypes systemType)
        {

        }
        
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// ベントに入るときに呼び出されます。
        /// </summary>
        /// <param name="vent"></param>
        [RoleLocalMethod]
        public virtual void OnEnterVent(Vent vent) { }

        /// <summary>
        /// ベントから出るときに呼び出されます。
        /// </summary>
        /// <param name="vent"></param>
        [RoleLocalMethod]
        public virtual void OnExitVent(Vent vent) { }

        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// //明かりの大きさを調整します。毎ティック呼び出されます。
        /// </summary>
        /// <param name="radius">現行の明かりの大きさ</param>
        [RoleLocalMethod]
        public virtual void GetLightRadius(ref float radius) { }

        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// キルクールダウンを設定する際に呼び出されます。
        /// </summary>
        /// <param name="multiplier">クールダウンに掛け合わせる値</param>
        /// <param name="addition">クールダウンに足しこむ値</param>
        [RoleLocalMethod]
        public virtual void SetKillCoolDown(ref float multiplier, ref float addition) { }

        /*--------------------------------------------------------------------------------------*/

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

        [RoleLocalMethod]
        public virtual void MeetingUpdate(MeetingHud __instance, TMPro.TextMeshPro meetingInfo) { }

        /// <summary>
        /// 投票した際に呼び出されます。
        /// </summary>
        [RoleIndefiniteMethod]
        public virtual void OnVote(byte playerId, byte targetId) { }

        /// <summary>
        /// 会議が終了した際に呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public virtual void OnMeetingEnd() { }

        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// 誰かがガードされたときに呼び出されます。
        /// プレイヤー自身のロールについてのみ呼び出されます。
        /// </summary>
        /// <param name="murderId">殺害者のプレイヤーID</param>
        /// <param name="targetId">被害者のプレイヤーID</param>
        [RoleLocalMethod]
        public virtual void OnAnyoneGuarded(byte murderId, byte targetId) { }


        /// <summary>
        /// 誰かが殺害されたときに呼び出されます。
        /// プレイヤー自身のロールについてのみ呼び出されます。
        /// </summary>
        /// <param name="murderId">殺害者のプレイヤーID</param>
        /// <param name="targetId">被害者のプレイヤーID</param>
        [RoleLocalMethod]
        public virtual void OnAnyoneMurdered(byte murderId, byte targetId) { }

        /// <summary>
        /// 誰かが死んだときに呼び出されます。
        /// プレイヤー自身のロールについてのみ呼び出されます。
        /// </summary>
        /// <param name="playerId">死者のプレイヤーID</param>
        [RoleLocalMethod]
        public virtual void OnAnyoneDied(byte playerId) { }

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

        /*--------------------------------------------------------------------------------------*/

        //その役職をもつプレイヤーがクルー勝利に与するTaskを持っているかどうか調べます。
        public virtual bool HasCrewmateTask(byte playerId)
        {
            return true;
        }

        //その役職をもつプレイヤーが実行可能Taskを持っているかどうか調べます。
        public virtual bool HasExecutableFakeTask(byte playerId)
        {
            return false;
        }

        /// <summary>
        /// 役職の表示名を編集します。
        /// </summary>
        [RoleGlobalMethod]
        public virtual void EditDisplayRoleName(ref string roleName)
        {
        }

        /// <summary>
        /// 名前の色を編集します。
        /// </summary>
        [RoleGlobalMethod]
        public virtual void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
        }

        /// <summary>
        /// 他人の名前の色を編集します。
        /// </summary>
        [RoleLocalMethod]
        public virtual void EditOthersDisplayNameColor(byte playerId, ref Color displayColor)
        {
        }

        /// <summary>
        /// 表示名を編集します。
        /// </summary>
        /// <param name="displayName"></param>
        [RoleGlobalMethod]
        public virtual void EditDisplayName(byte playerId,ref string displayName,bool hideFlag)
        {
        }

        /// <summary>
        /// 他人の表示名を編集します。
        /// </summary>
        /// <param name="displayName"></param>
        [RoleLocalMethod]
        public virtual void EditOthersDisplayName(byte playerId, ref string displayName, bool hideFlag)
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
        /// 誰かが復活したときに呼び出されます。
        /// </summary>
        /// <param name="displayName"></param>
        [RoleGlobalMethod]
        public virtual void onRevived(byte playerId)
        {
        }

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
        /// ゲーム中にロールが変更される際に呼ばれます。
        /// 全てのプレイヤーに対して呼び出されます。
        /// </summary>
        /// <param name="__instance"></param>
        [RoleGlobalMethod]
        public virtual void GlobalFinalizeInGame(PlayerControl __instance) { }

        /// <summary>
        /// プレイヤーごとの追加勝利を確認します。
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        [RoleGlobalMethod]
        public virtual bool CheckAdditionalWin(PlayerControl player, Patches.EndCondition condition) { return false; }

        /// <summary>
        /// どのゲームでも必ず最初に呼び出されます。
        /// </summary>
        /// <returns></returns>
        public virtual void StaticInitialize(){}

        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// その役職のプレイヤーが殺害されるときに呼び出されます。
        /// </summary>
        /// <param name="murderId">殺害者のID</param>
        /// <param name="playerId">死亡者のID</param>
        /// <returns>キルが実行されるかどうか</returns>
        [RoleIndefiniteMethod]
        public virtual Helpers.MurderAttemptResult OnMurdered(byte murderId, byte playerId) { return Helpers.MurderAttemptResult.PerformKill; }


        /*--------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------------------------------*/

        /// <summary>
        /// この役職が発生しうるかどうか調べます
        /// </summary>
        public virtual bool IsSpawnable()
        {
            try
            {
                if (RoleChanceOption.getSelection() == 0) return false;
                if (!FixedRoleCount && RoleCountOption.getFloat() == 0f) return false;
            }
            catch (Exception e) { return false; }

            return true;
        }

        //ComplexRoleの子ロールなど、オプション画面で隠したいロールはtrueにしてください。
        public bool IsHideRole { get; protected set; }

        protected bool CreateOptionFollowingRelatedRole { get; set; }

        private bool CreateOptionFlag { get; set; }

        public void CreateRoleOption()
        {
            if (CreateOptionFlag) return;

            if (!IsHideRole)
            {
                SetupRoleOptionData();
            }
            LoadOptionData();

            CreateOptionFlag = true;

            foreach (var assignable in GetFollowRoles())
            {
                assignable.CreateRoleOption();
            }
        }

        /// <summary>
        /// 関連したロールを返します。
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Assignable> GetFollowRoles() { yield break; }

        protected Assignable(string name, string localizeName, Color color)
        {

            this.Name = name;
            this.LocalizeName = localizeName;
            this.Color = color;

            //未設定
            this.OptionId = -1;

            this.IsHideRole = false;
            this.ExceptBasicOption = false;
            this.CreateOptionFollowingRelatedRole = false;
            this.CreateOptionFlag = false;


            this.ValidGamemode = Module.CustomGameMode.Standard;

            this.OptionCapacity = 20;

            this.canFixSabotage = true;
        }
    }
}
