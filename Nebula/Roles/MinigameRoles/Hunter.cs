using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;

namespace Nebula.Roles.MinigameRoles
{
    public class Hunter : Role
    {
        static private CustomButton arrestButton;

        private Sprite buttonSprite = null;
        public Sprite GetButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.ArrestButton.png", 115f);
            return buttonSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (arrestButton != null)
            {
                arrestButton.Destroy();
            }
            arrestButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.CloseUpKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget,Game.PlayerData.PlayerStatus.Arrested);

                    arrestButton.Timer = arrestButton.MaxTimer;
                    Game.GameData.data.myData.currentTarget = null;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
                () => { arrestButton.Timer = arrestButton.MaxTimer; },
                GetButtonSprite(),
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.Q,
                false,
                "button.label.arrest"
            );
            arrestButton.MaxTimer = arrestButton.Timer = 10f;
        }

        public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
        {
            displayColor = Palette.ImpostorRed;
        }

        public Hunter()
                : base("Hunter", "hunter", Palette.ImpostorRed, RoleCategory.Crewmate, Side.GamePlayer, Side.GamePlayer,
                     Player.minigameSideSet, Player.minigameSideSet, new HashSet<EndCondition>() { EndCondition.MinigameHunterWin },
                     true, false, false, false, true)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.Minigame;
            CanCallEmergencyMeeting = true;

            arrestButton = null;
        }
    }
}
