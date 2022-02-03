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

namespace Nebula.Roles.ImpostorRoles
{
    public class Sniper : Role
    {
        private bool equipRifleFlag;

        /* オプション */
        private Module.CustomOption snipeCoolDownOption;
        private Module.CustomOption shotSizeOption;
        private Module.CustomOption shotEffectiveRangeOption;
        private Module.CustomOption canKillImpostorsOption;
        public Module.CustomOption noticeRangeOption;

        public override void LoadOptionData()
        {
            snipeCoolDownOption = CreateOption(Color.white, "snipeCoolDown", 20f, 5f, 60f, 2.5f);
            snipeCoolDownOption.suffix = "second";
            shotSizeOption = CreateOption(Color.white, "shotSize", 1f, 0.5f, 4f, 0.25f);
            shotSizeOption.suffix = "cross";
            shotEffectiveRangeOption = CreateOption(Color.white, "shotEffectiveRange", 20f, 2f, 40f, 2f);
            shotEffectiveRangeOption.suffix = "cross";
            canKillImpostorsOption = CreateOption(Color.white, "canKillImpostors", false);

            noticeRangeOption = CreateOption(Color.white, "shotEffectiveRange", 20f, 2f, 50f, 2f);
        }


        private PlayerControl GetShootPlayer(float shotSize,float effectiveRange,bool onlyWhiteName=false)
        {
            PlayerControl result=null;
            float num= effectiveRange;
            Vector3 pos;
            float mouseAngle = Game.GameData.data.myData.getGlobalData().MouseAngle;
            foreach(PlayerControl player in PlayerControl.AllPlayerControls)
            {
                //自分自身は撃ち抜かれない
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                if (player.Data.IsDead) continue;

                if (onlyWhiteName)
                {
                    if (player.GetModData().role.side == Side.Impostor || player.GetModData().role.deceiveImpostorInNameDisplay) continue;
                }

                pos = player.transform.position - PlayerControl.LocalPlayer.transform.position;
                pos = new Vector3(
                    pos.x * MathF.Cos(mouseAngle) + pos.y * MathF.Sin(mouseAngle),
                    pos.y * MathF.Cos(mouseAngle) - pos.x * MathF.Sin(mouseAngle));
                if(Math.Abs(pos.y)<shotSize && (!(pos.x<0)) && pos.x < num)
                {
                    num = pos.x;
                    result = player;
                }
            }
            return result;
        }

        /* ボタン */
        static private CustomButton sniperButton;
        static private CustomButton killButton;
        public override void ButtonInitialize(HudManager __instance)
        {
            if (sniperButton != null)
            {
                sniperButton.Destroy();
            }
            sniperButton = new CustomButton(
                () =>
                {
                    if (equipRifleFlag)
                    {
                        RPCEventInvoker.SniperSettleRifle();
                    }
                    else
                    {
                        RPCEventInvoker.ObjectInstantiate(CustomObject.Type.SniperRifle, PlayerControl.LocalPlayer.transform.position);
                    }
                    equipRifleFlag = !equipRifleFlag;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => { sniperButton.Timer = sniperButton.MaxTimer; },
                getSnipeButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            sniperButton.Timer = sniperButton.MaxTimer = 0f;

            if (killButton != null)
            {
                killButton.Destroy();
            }
            killButton = new CustomButton(
                () =>
                {
                    PlayerControl target = GetShootPlayer(shotSizeOption.getFloat()*0.1f,shotEffectiveRangeOption.getFloat(), !canKillImpostorsOption.getBool());
                    if (target!=null)
                    {
                        var res=Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, target, Game.PlayerData.PlayerStatus.Sniped, false, false);
                        if (res != Helpers.MurderAttemptResult.SuppressKill)
                            killButton.Timer = killButton.MaxTimer;
                    }

                    RPCEventInvoker.SniperShot();
                    killButton.Timer = killButton.MaxTimer;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && equipRifleFlag; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.Q
            );
            killButton.MaxTimer = snipeCoolDownOption.getFloat();
        }

        public override void ButtonActivate()
        {
            sniperButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            sniperButton.setActive(false);
        }

        public byte deadBodyId;

        public override void OnMeetingStart()
        {
            RPCEventInvoker.SniperSettleRifle();
            equipRifleFlag = false;
        }

        /* 画像 */
        private Sprite snipeButtonSprite = null;
        public Sprite getSnipeButtonSprite()
        {
            if (snipeButtonSprite) return snipeButtonSprite;
            snipeButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SnipeButton.png", 115f);
            return snipeButtonSprite;
        }

        private Sprite snipeArrowSprite = null;
        public Sprite getSnipeArrowSprite()
        {
            if (snipeArrowSprite) return snipeArrowSprite;
            snipeArrowSprite = Helpers.loadSpriteFromResources("Nebula.Resources.SniperRifleArrow.png", 200f);
            return snipeArrowSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return;

            if (equipRifleFlag) RPCEventInvoker.UpdatePlayerControl();
        }

        public override void OnDied()
        {
            if (equipRifleFlag)
            {
                RPCEventInvoker.SniperSettleRifle();
                equipRifleFlag = false;
            }
        }

        public override void Initialize(PlayerControl __instance)
        {
            equipRifleFlag = false;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {

        }

        public override void CleanUp()
        {
            if (equipRifleFlag)
            {
                RPCEventInvoker.SniperSettleRifle();
                equipRifleFlag = false;
            }

            if (sniperButton != null)
            {
                sniperButton.Destroy();
                sniperButton = null;
            }

            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }
        }

        public Sniper()
            : base("Sniper", "sniper", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet,
                 Impostor.impostorEndSet,
                 false, true, true, true, true)
        {
            sniperButton = null;
            killButton = null;

            //通常のキルボタンは使用しない
            HideKillButtonEvenImpostor = true;
        }
    }
}
