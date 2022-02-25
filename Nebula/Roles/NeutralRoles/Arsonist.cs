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
using Nebula.Game;

namespace Nebula.Roles.NeutralRoles
{
    public class Arsonist : Template.HasAlignedHologram, Template.HasWinTrigger
    {
        static public Color Color = new Color(255f/255f, 103f/255f, 1/255f);

        static private CustomButton arsonistButton;

        private Module.CustomOption douseDurationOption;
        private Module.CustomOption douseCoolDownOption;
        private Module.CustomOption douseRangeOption;

        public bool WinTrigger { get; set; } = false;
        public byte Winner { get; set; } = Byte.MaxValue;

        public override void LoadOptionData()
        {
            douseDurationOption = CreateOption(Color.white, "douseDuration", 3f, 1f, 10f, 1f);
            douseDurationOption.suffix = "second";

            douseCoolDownOption = CreateOption(Color.white, "douseCoolDown", 10f, 0f, 60f, 5f);
            douseCoolDownOption.suffix = "second";

            douseRangeOption = CreateOption(Color.white, "douseRange", 1f, 0.5f, 2f, 0.125f);
            douseRangeOption.suffix = "cross";
        }


        Sprite douseSprite,igniteSprite;
        public Sprite getDouseButtonSprite()
        {
            if (douseSprite) return douseSprite;
            douseSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DouseButton.png", 115f);
            return douseSprite;
        }

        public Sprite getIgniteButtonSprite()
        {
            if (igniteSprite) return igniteSprite;
            igniteSprite = Helpers.loadSpriteFromResources("Nebula.Resources.IgniteButton.png", 115f);
            return igniteSprite;
        }

        static private bool canIgnite = false;

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            canIgnite = false;
            WinTrigger = false;
        }

        public override void CleanUp()
        {
            base.CleanUp();

            canIgnite = false;
            WinTrigger = false;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (arsonistButton != null)
            {
                arsonistButton.Destroy();
            }
            arsonistButton = new CustomButton(
                () => {
                    if (canIgnite)
                    {
                        arsonistButton.isEffectActive = false;
                        arsonistButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                        RPCEventInvoker.WinTrigger(this);
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => {
                    if (arsonistButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                    {
                        arsonistButton.Timer = 0f;
                        arsonistButton.isEffectActive = false;
                    }
                    return PlayerControl.LocalPlayer.CanMove && (Game.GameData.data.myData.currentTarget!=null|| canIgnite); },
                () => {
                    arsonistButton.Timer = arsonistButton.MaxTimer;
                    arsonistButton.isEffectActive = false;
                    arsonistButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                getDouseButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F,
                true,
                douseDurationOption.getFloat(),
                () => {
                    if (Game.GameData.data.myData.currentTarget != null)
                    {
                        activePlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);
                        Game.GameData.data.myData.currentTarget = null;
                    }

                    bool cannotIgnite = false;
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player.Data.IsDead) continue;
                        if (activePlayers.Contains(player.PlayerId)) continue;
                        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                        cannotIgnite = true; break;
                    }

                    if (!cannotIgnite)
                    {
                        //点火可能
                        arsonistButton.Sprite = getIgniteButtonSprite();
                        canIgnite = true;
                        arsonistButton.Timer = 0f;
                    }
                    else
                    {
                        arsonistButton.Timer = arsonistButton.MaxTimer;
                    }
                }
            );
            arsonistButton.MaxTimer = douseCoolDownOption.getFloat();
            arsonistButton.EffectDuration = douseDurationOption.getFloat();
        }

        public override void ButtonActivate()
        {
            base.ButtonActivate();

            arsonistButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            base.ButtonDeactivate();

            arsonistButton.setActive(false);
        }

        public override void MyPlayerControlUpdate()
        {
            base.MyPlayerControlUpdate();

            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1.8f*douseRangeOption.getFloat(), false, false, activePlayers);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Empiric);
            RelatedRoles.Add(Roles.EvilAce);
        }

        public Arsonist()
            : base("Arsonist", "arsonist", Color, RoleCategory.Neutral, Side.Arsonist, Side.Arsonist,
                 new HashSet<Side>() { Side.Arsonist }, new HashSet<Side>() { Side.Arsonist },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.ArsonistWin },
                 true, true, true, false, false)
        {
            arsonistButton = null;

            Patches.EndCondition.ArsonistWin.TriggerRole = this;
        }
    }
}
