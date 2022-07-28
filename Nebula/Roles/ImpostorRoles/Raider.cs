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
using System.Collections;
using BepInEx.IL2CPP.Utils.Collections;

namespace Nebula.Roles.ImpostorRoles
{
    public class Raider : Role
    {
        private bool equipAxeFlag;

        /* オプション */
        private Module.CustomOption throwCoolDownOption;
        private Module.CustomOption axeSizeOption;
        public Module.CustomOption axeSpeedOption;
        private Module.CustomOption canKillImpostorsOption;

        private CustomMessage? message;

        public override void LoadOptionData()
        {
            throwCoolDownOption = CreateOption(Color.white, "throwCoolDown", 20f, 5f, 60f, 2.5f);
            throwCoolDownOption.suffix = "second";
            axeSizeOption = CreateOption(Color.white, "axeSize", 1f, 0.5f, 4f, 0.25f);
            axeSizeOption.suffix = "cross";
            axeSpeedOption = CreateOption(Color.white, "axeSpeed", 1f, 0.5f, 2.5f, 0.25f);
            axeSpeedOption.suffix = "cross";
            canKillImpostorsOption = CreateOption(Color.white, "canKillImpostors", false);
        }


        /* ボタン */
        static private CustomButton axeButton;
        static private CustomButton killButton;

        public CustomObject? lastAxe=null;
        public CustomObject? thrownAxe = null;

        public override void ButtonInitialize(HudManager __instance)
        {
            if (axeButton != null)
            {
                axeButton.Destroy();
            }
            axeButton = new CustomButton(
                () =>
                {
                    if (equipAxeFlag)
                    {
                        RPCEventInvoker.RaiderSettleAxe();
                    }
                    else
                    {
                        lastAxe=RPCEventInvoker.ObjectInstantiate(Objects.ObjectTypes.RaidAxe.Axe, PlayerControl.LocalPlayer.transform.position);
                    }
                    equipAxeFlag = !equipAxeFlag;

                    axeButton.SetLabel(equipAxeFlag ? "button.label.unequip" : "button.label.equip");
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && killButton.Timer <= 0f; },
                () => {
                    axeButton.Timer = axeButton.MaxTimer;
                    axeButton.SetLabel("button.label.equip");
                },
                getAxeButtonSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.equip"
            );
            axeButton.Timer = axeButton.MaxTimer = 0f;

            if (killButton != null)
            {
                killButton.Destroy();
            }
            killButton = new CustomButton(
                () =>
                {
                    RPCEventInvoker.RaiderThrow(lastAxe.GameObject.transform.position, lastAxe.GameObject.transform.eulerAngles.z);
                    thrownAxe = lastAxe;
                    lastAxe = null;

                    killButton.Timer = killButton.MaxTimer;   
                    equipAxeFlag = false;
                    axeButton.SetLabel("button.label.equip");
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove && equipAxeFlag; },
                () => { killButton.Timer = killButton.MaxTimer; },
                __instance.KillButton.graphic.sprite,
                new Vector3(0f, 1f, 0),
                __instance,
                KeyCode.Q,
                false,
                 "button.label.throw"
            ).SetTimer(10f);
            killButton.MaxTimer = throwCoolDownOption.getFloat();
            killButton.FireOnClicked = true;
            killButton.SetButtonCoolDownOption(true);

        }

        public override void ButtonActivate()
        {
            axeButton.setActive(true);
            killButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            axeButton.setActive(false);
            killButton.setActive(false);
        }

        public override void EditCoolDown(CoolDownType type, float count)
        {
            killButton.Timer -= count;
            killButton.actionButton.ShowButtonText("+" + count + "s");
        }

        public byte deadBodyId;

        public override void OnMeetingStart()
        {
            RPCEventInvoker.RaiderSettleAxe();
            equipAxeFlag = false;
        }

        /* 画像 */
        private Sprite axeButtonSprite = null;
        public Sprite getAxeButtonSprite()
        {
            if (axeButtonSprite) return axeButtonSprite;
            axeButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.AxeButton.png", 115f);
            return axeButtonSprite;
        }

        private IEnumerator GetMessageUpdater()
        {
            while (true)
            {
                bool flag = false;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p == PlayerControl.LocalPlayer) continue;
                    if (p.GetModData().isInvisiblePlayer)
                    {
                        if (p.transform.position.Distance(PlayerControl.LocalPlayer.transform.position) < 5.0f)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag) yield return null;
                else break;
            }
        }

        public override void MyPlayerControlUpdate()
        {
            if (thrownAxe != null)
            {
                if (thrownAxe.Data[0] == (int)Objects.ObjectTypes.RaidAxe.AxeState.Thrown)
                {
                    Vector3 pos = thrownAxe.GameObject.transform.position;
                    float d = 0.4f * axeSizeOption.getFloat();
                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p.Data.IsDead) continue;
                        if (p == PlayerControl.LocalPlayer) continue;
                        if (!canKillImpostorsOption.getBool() && p.Data.Role.Role == RoleTypes.Impostor) continue;

                        if (pos.Distance(p.transform.position) < d) RPCEventInvoker.UncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId, p.PlayerId, Game.PlayerData.PlayerStatus.Beaten.Id, false);
                    }
                }
            }

            if (message == null || !message.isActive)
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p == PlayerControl.LocalPlayer) continue;
                    if (p.GetModData().isInvisiblePlayer)
                    {
                        if (p.transform.position.Distance(PlayerControl.LocalPlayer.transform.position) < 5.0f)
                        {
                            message = new CustomMessage(new Vector3(0, -1.5f, 0), false, Language.Language.GetString("role.sniper.nearMessage"), GetMessageUpdater(), 1.0f, Palette.ImpostorRed);
                            break;
                        }
                    }
                }
            }

            if (equipAxeFlag)
            {
                RPCEventInvoker.UpdatePlayerControl();
            }
        }

        public override void OnDied()
        {
            if (equipAxeFlag)
            {
                RPCEventInvoker.RaiderSettleAxe();
                equipAxeFlag = false;
            }
        }

        public override void Initialize(PlayerControl __instance)
        {
            equipAxeFlag = false;

            message = null;

            thrownAxe = null;
            lastAxe = null;
        }


        public override void CleanUp()
        {
            if (equipAxeFlag)
            {
                RPCEventInvoker.RaiderSettleAxe();
                equipAxeFlag = false;
            }

            if (axeButton != null)
            {
                axeButton.Destroy();
                axeButton = null;
            }

            if (killButton != null)
            {
                killButton.Destroy();
                killButton = null;
            }

            thrownAxe = null;
            lastAxe = null;

        }

        public Raider()
            : base("Raider", "raider", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet,
                 Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            axeButton = null;
            killButton = null;

            //通常のキルボタンは使用しない
            HideKillButtonEvenImpostor = true;
        }
    }
}
