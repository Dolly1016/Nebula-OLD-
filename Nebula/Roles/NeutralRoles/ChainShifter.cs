using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Hazel;
using Nebula.Objects;
using Nebula.Patches;


namespace Nebula.Roles.NeutralRoles
{
    public class ChainShifter : Role
    {
        /* 陣営色 */
        static public Color RoleColor = new Color(115f / 255f, 115f / 255f, 115f / 255f);

        /* オプション */
        private Module.CustomOption isGuessableOption;

        private PlayerControl? shiftPlayer;

        public override void LoadOptionData()
        {
            isGuessableOption = CreateOption(Color.white, "isGuessable", false);
        }

        public override bool IsGuessableRole { get => isGuessableOption.getBool(); protected set => base.IsGuessableRole = value; }

        public override void MyPlayerControlUpdate()
        {
            if (shiftPlayer == null)
            {
                Game.MyPlayerData data = Game.GameData.data.myData;
                data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
                Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
            }
        }

        /* ボタン */
        static private CustomButton shiftButton;
        public override void ButtonInitialize(HudManager __instance)
        {
            if (shiftButton != null)
            {
                shiftButton.Destroy();
            }
            shiftButton = new CustomButton(
                () =>
                {
                    if (Game.GameData.data.myData.currentTarget != null)
                    {
                        shiftPlayer = Game.GameData.data.myData.currentTarget;
                    }
                },
                () => { return shiftPlayer == null && !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return Game.GameData.data.myData.currentTarget != null && PlayerControl.LocalPlayer.CanMove; },
                () => { shiftButton.Timer = shiftButton.MaxTimer; },
                getButtonSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.chainShift"
            );
            shiftButton.MaxTimer = shiftButton.Timer = 10.0f;
        }

        public override void OnMeetingStart()
        {
            if (shiftPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                RPCEventInvoker.SwapRole(PlayerControl.LocalPlayer, shiftPlayer);
                shiftPlayer = null;
            }
        }

        public override void ButtonActivate()
        {
            shiftButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            shiftButton.setActive(false);
        }

        /* 矢印 */
        Dictionary<byte, Arrow> Arrows;

        public byte deadBodyId;

        public int eatLeftId;

        /* 画像 */
        private Sprite buttonSprite = null;
        public Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ChainShiftButton.png", 115f);
            return buttonSprite;
        }

        public override void Initialize(PlayerControl __instance)
        {
            shiftPlayer = null;
        }


        public override void CleanUp()
        {
            if (shiftButton != null)
            {
                shiftButton.Destroy();
                shiftButton = null;
            }
        }

        public ChainShifter()
            : base("ChainShifter", "chainShifter", RoleColor, RoleCategory.Neutral, Side.ChainShifter, Side.ChainShifter,
                 new HashSet<Side>() { Side.ChainShifter }, new HashSet<Side>() { Side.ChainShifter },
                 new HashSet<Patches.EndCondition>() { },
                 true, VentPermission.CanNotUse, false, false, false)
        {
        }
    }
}
