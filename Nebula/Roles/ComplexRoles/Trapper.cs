using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnhollowerBaseLib;
using System.Collections;
using System;
using System.Text;
using UnityEngine;
using System.Reflection;
using Nebula.Objects;

namespace Nebula.Roles.ComplexRoles
{
    public class FTrapper : Template.HasBilateralness
    {
        public Module.CustomOption maxTrapsOption;
        public Module.CustomOption accelTrapSpeedOption;
        public Module.CustomOption decelTrapSpeedOption;
        public Module.CustomOption accelTrapDurationOption;
        public Module.CustomOption decelTrapDurationOption;
        public Module.CustomOption visibleTrapRangeOption;
        public Module.CustomOption invisibleTrapRangeOption;
        public Module.CustomOption commButtonCostOption;
        public Module.CustomOption killButtonCostOption;

        static public Color Color = new Color(206f / 255f, 219f / 255f, 96f / 255f);

        public int remainTrapsId { get; private set; }

        private static Sprite accelButtonSprite;
        public static Sprite getAccelButtonSprite()
        {
            if (accelButtonSprite) return accelButtonSprite;
            accelButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.AccelTrapButton.png", 115f);
            return accelButtonSprite;
        }

        private static Sprite decelButtonSprite;
        public static Sprite getDecelButtonSprite()
        {
            if (decelButtonSprite) return decelButtonSprite;
            decelButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DecelTrapButton.png", 115f);
            return decelButtonSprite;
        }

        private static Sprite killButtonSprite;
        public static Sprite getKillButtonSprite()
        {
            if (killButtonSprite) return killButtonSprite;
            killButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.KillTrapButton.png", 115f);
            return killButtonSprite;
        }

        private static Sprite commButtonSprite;
        public static Sprite getCommButtonSprite()
        {
            if (commButtonSprite) return commButtonSprite;
            commButtonSprite = Helpers.loadSpriteFromResources("Nebula.Resources.CommTrapButton.png", 115f);
            return commButtonSprite;
        }

        public override void LoadOptionData()
        {
            base.LoadOptionData();

            maxTrapsOption = CreateOption(Color.white, "maxTraps", 5f, 1f, 15f, 1f);
            accelTrapSpeedOption = CreateOption(Color.white, "accelSpeed", 1.5f, 1f, 2f, 0.125f);
            accelTrapSpeedOption.suffix = "cross";
            decelTrapSpeedOption = CreateOption(Color.white, "decelSpeed", 0.5f, 0.125f, 1f, 0.125f);
            decelTrapSpeedOption.suffix = "cross";
            accelTrapDurationOption = CreateOption(Color.white, "accelDuration", 5f, 2.5f, 30f, 2.5f);
            accelTrapDurationOption.suffix = "second";
            decelTrapDurationOption = CreateOption(Color.white, "decelDuration", 5f, 2.5f, 30f, 2.5f);
            decelTrapDurationOption.suffix = "second";
            visibleTrapRangeOption = CreateOption(Color.white, "visibleTrapRange", 0.5f, 0.125f, 2f, 0.125f);
            visibleTrapRangeOption.suffix = "cross";
            invisibleTrapRangeOption = CreateOption(Color.white, "invisibleTrapRange", 0.5f, 0.125f, 2f, 0.125f);
            invisibleTrapRangeOption.suffix = "cross";

            commButtonCostOption = CreateOption(Color.white, "commTrapCost", 2f, 1f, 5f, 1f);
            commButtonCostOption.suffix = "cross";
            killButtonCostOption = CreateOption(Color.white, "killTrapCost", 2f, 1f, 5f, 1f);
            killButtonCostOption.suffix = "cross";

            FirstRole = Roles.NiceTrapper;
            SecondaryRole = Roles.EvilTrapper;
        }

        public FTrapper()
                : base("Trapper", "trapper", Color)
        {
            remainTrapsId = Game.GameData.RegisterRoleDataId("trapper.remainTraps");

            Objects.CustomObject.RegisterUpdater((player) =>
            {
                CustomObject trap = Objects.CustomObject.GetTarget(visibleTrapRangeOption.getFloat() / 2, player, (obj) => { return obj.PassedMeetings > 0; }, CustomObject.Type.AccelTrap, CustomObject.Type.DecelTrap);
                if (trap == null) return;

                if (trap.ObjectType == CustomObject.Type.AccelTrap)
                {
                    RPCEventInvoker.EmitSpeedFactor(player,
                        new Game.SpeedFactor(1, accelTrapDurationOption.getFloat(), accelTrapSpeedOption.getFloat(), false));
                }
                else
                {
                    RPCEventInvoker.EmitSpeedFactor(player,
                        new Game.SpeedFactor(1, decelTrapDurationOption.getFloat(), decelTrapSpeedOption.getFloat(), false));
                }
            });
        }

        public override List<Role> GetImplicateRoles() { return new List<Role>() { Roles.EvilTrapper, Roles.NiceTrapper }; }
    }

    public class Trapper : Role
    {
        private CustomButton trapButton;
        private TMPro.TMP_Text trapButtonString;
        private byte trapKind;
        private static List<byte> detectedPlayers=new List<byte>();

        //インポスターはModで操作するFakeTaskは所持していない
        public Trapper(string name, string localizeName, bool isImpostor)
                : base(name, localizeName,
                     isImpostor ? Palette.ImpostorRed : FTrapper.Color,
                     isImpostor ? RoleCategory.Impostor : RoleCategory.Crewmate,
                     isImpostor ? Side.Impostor : Side.Crewmate, isImpostor ? Side.Impostor : Side.Crewmate,
                     isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                     isImpostor ? ImpostorRoles.Impostor.impostorSideSet : CrewmateRoles.Crewmate.crewmateSideSet,
                     isImpostor ? ImpostorRoles.Impostor.impostorEndSet : CrewmateRoles.Crewmate.crewmateEndSet,
                     false, isImpostor, isImpostor, isImpostor, isImpostor)
        {
            IsHideRole = true;

            trapButton = null;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            Game.GameData.data.myData.getGlobalData().SetRoleData(Roles.F_Trapper.remainTrapsId, (int)Roles.F_Trapper.maxTrapsOption.getFloat());
        }

        public override void MyUpdate()
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return;
            
            //探知されていないプレイヤーを除去する
            detectedPlayers.RemoveAll((id)=> {
                PlayerControl player = Helpers.playerById(id);
                float dis = Roles.F_Trapper.invisibleTrapRangeOption.getFloat()/2;
                foreach (CustomObject obj in CustomObject.Objects.Values)
                {
                    if (obj.ObjectType == CustomObject.Type.CommTrap)
                    {
                        if (obj.GameObject.transform.position.Distance(player.transform.position) < dis) return false;
                    }
                }
                return true;
            });

            HashSet<CustomObject> deleteObjects = new HashSet<CustomObject>();
            foreach(CustomObject obj in CustomObject.Objects.Values)
            {
                if (obj.PassedMeetings == 0) continue;
                if (obj.OwnerId != PlayerControl.LocalPlayer.PlayerId) continue;

                if (obj.ObjectType == CustomObject.Type.KillTrap)
                {
                    if (PlayerControl.LocalPlayer.killTimer > 0f) continue;

                    PlayerControl player = Patches.PlayerControlPatch.GetTarget(obj.GameObject.transform.position, Roles.F_Trapper.invisibleTrapRangeOption.getFloat() / 2, side == Side.Impostor);
                    if (player != null)
                    {
                        Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, player, Game.PlayerData.PlayerStatus.Trapped, false, false);
                        deleteObjects.Add(obj);

                        PlayerControl.LocalPlayer.killTimer = PlayerControl.GameOptions.KillCooldown;
                    }

                }
                else if (obj.ObjectType == CustomObject.Type.CommTrap)
                {
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player.Data.IsDead) continue;
                        if (detectedPlayers.Contains(player.PlayerId)) continue;

                        if (player.transform.position.Distance(obj.GameObject.transform.position) < Roles.F_Trapper.invisibleTrapRangeOption.getFloat() / 2)
                        {
                            Arrow arrow = new Arrow(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
                            arrow.arrow.SetActive(true);
                            arrow.Update(obj.GameObject.transform.position);
                            detectedPlayers.Add(player.PlayerId);

                            byte id = player.PlayerId;
                            Vector3 pos = obj.GameObject.transform.position;
                            HudManager.Instance.StartCoroutine(Effects.Lerp(5f, new Action<float>((p) =>
                            {
                                arrow.Update(pos);
                                if (p > 0.8f)
                                {
                                    arrow.image.color = new Color(arrow.image.color.r, arrow.image.color.g, arrow.image.color.b, (1f - p) * 5f);
                                }
                                if (p == 1f)
                                {
                                    //矢印を消す
                                    UnityEngine.Object.Destroy(arrow.arrow);
                                }
                            })));
                        }
                    }
                }
            }
            foreach(CustomObject obj in deleteObjects)
            {
                obj.Destroy();
            }


            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                trapKind++;
                if (trapKind > 2) trapKind = 0;
            }
            else { return; }

            switch (trapKind)
            {
                case 0:
                    trapButton.Sprite = FTrapper.getAccelButtonSprite();
                    break;
                case 1:
                    trapButton.Sprite = FTrapper.getDecelButtonSprite();
                    break;
                case 2:
                    if (side == Side.Impostor)
                    {
                        trapButton.Sprite = FTrapper.getKillButtonSprite();
                    }
                    else
                    {
                        trapButton.Sprite = FTrapper.getCommButtonSprite();
                    }
                    break;
            }
        }
        public override void ButtonInitialize(HudManager __instance)
        {
            if (trapButton != null)
            {
                trapButton.Destroy();
            }
            trapButton = new CustomButton(
                () => {
                    switch (trapKind)
                    {
                        case 0:
                            RPCEventInvoker.ObjectInstantiate(CustomObject.Type.AccelTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset);
                            RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -1);
                            break;
                        case 1:
                            RPCEventInvoker.ObjectInstantiate(CustomObject.Type.DecelTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset);
                            RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -1);
                            break;
                        case 2:
                            if (side == Side.Impostor)
                            {
                                RPCEventInvoker.ObjectInstantiate(CustomObject.Type.KillTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset);
                                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -(int)Roles.F_Trapper.killButtonCostOption.getFloat());
                            }
                            else
                            {
                                RPCEventInvoker.ObjectInstantiate(CustomObject.Type.CommTrap, PlayerControl.LocalPlayer.transform.position + (Vector3)PlayerControl.LocalPlayer.Collider.offset);
                                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, Roles.F_Trapper.remainTrapsId, -(int)Roles.F_Trapper.commButtonCostOption.getFloat());
                            }
                            break;
                    }
                    

                    trapButton.Timer = trapButton.MaxTimer;                   
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetRoleData(Roles.F_Trapper.remainTrapsId) > 0; },
                () => {
                    int total = (int)Roles.F_Trapper.maxTrapsOption.getFloat();
                    int remain = Game.GameData.data.myData.getGlobalData().GetRoleData(Roles.F_Trapper.remainTrapsId);
                    trapButtonString.text = $"{remain}/{total}";

                    return remain > 0 && PlayerControl.LocalPlayer.CanMove;

                },
                () => { trapButton.Timer = trapButton.MaxTimer; },
                FTrapper.getAccelButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F
            );
            trapButton.MaxTimer = 20;

            trapButtonString = GameObject.Instantiate(trapButton.actionButton.cooldownTimerText, trapButton.actionButton.cooldownTimerText.transform.parent);
            trapButtonString.text = "";
            trapButtonString.enableWordWrapping = false;
            trapButtonString.transform.localScale = Vector3.one * 0.5f;
            trapButtonString.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);

            trapKind = 0;

            //直近にすれ違った人をリセットする
            detectedPlayers.Clear();
        }

        public override void ButtonActivate()
        {
            trapButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            trapButton.setActive(false);
        }

        public override void CleanUp()
        {
            if (trapButton != null)
            {
                trapButton.Destroy();
                trapButton = null;
            }

            if (trapButtonString != null)
            {
                trapButtonString.DestroySubMeshObjects();
                trapButtonString = null;
            }
        }
    }
}
