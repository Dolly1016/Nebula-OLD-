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
    public class Arsonist : Template.HasHologram, Template.HasWinTrigger
    {
        static public Color Color = new Color(255f/255f, 103f/255f, 1/255f);

        static private CustomButton arsonistButton;

        private Module.CustomOption douseDurationOption;
        private Module.CustomOption douseCoolDownOption;

        public bool WinTrigger { get; set; } = false;

        public override void LoadOptionData()
        {
            douseDurationOption = CreateOption(Color.white, "douseDuration", 3f, 1f, 10f, 1f);
            douseDurationOption.suffix = "second";

            douseCoolDownOption = CreateOption(Color.white, "douseCoolDown", 10f, 0f, 60f, 5f);
            douseCoolDownOption.suffix = "second";
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

        static private List<byte> dousedPlayers=new List<byte>();
        static private bool canIgnite = false;

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            dousedPlayers.Clear();
            canIgnite = false;
            WinTrigger = false;
        }

        public override void CleanUp()
        {
            base.CleanUp();

            dousedPlayers.Clear();
            canIgnite = false;
            WinTrigger = false;
        }

        public override void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index) {
            Vector3 bottomLeft = new Vector3(-HudManager.Instance.UseButton.transform.localPosition.x, HudManager.Instance.UseButton.transform.localPosition.y, HudManager.Instance.UseButton.transform.localPosition.z);

            player.transform.localPosition = bottomLeft + new Vector3(-0.25f, -0.25f, 0) + Vector3.right * index++ * 0.25f;
            player.transform.localScale = Vector3.one * 0.24f;
            player.setSemiTransparent(true);
            player.gameObject.SetActive(true);
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
                        WinTrigger = true;
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
                        dousedPlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);
                        Game.GameData.data.myData.currentTarget = null;
                    }

                    bool cannotIgnite = false;
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player.Data.IsDead) continue;
                        if (dousedPlayers.Contains(player.PlayerId)) continue;
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
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(false,false,dousedPlayers);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

            int visibleCounter = 0;
            Vector3 bottomLeft = HudManager.Instance.UseButton.transform.localPosition;
            bottomLeft.x *= -1;
            bottomLeft += new Vector3(-0.25f, -0.25f, 0);

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (!PlayerIcons.ContainsKey(p.PlayerId)) continue;

                if (p.Data.IsDead || p.Data.Disconnected)
                {
                    PlayerIcons[p.PlayerId].gameObject.SetActive(false);
                }
                else
                {
                    PlayerIcons[p.PlayerId].gameObject.SetActive(true);
                    PlayerIcons[p.PlayerId].transform.localScale = Vector3.one * 0.25f;
                    PlayerIcons[p.PlayerId].transform.localPosition = bottomLeft + Vector3.right * visibleCounter * 0.24f;
                    visibleCounter++;
                }
                bool isDoused = dousedPlayers.Any(x => x == p.PlayerId);
                PlayerIcons[p.PlayerId].setSemiTransparent(!isDoused);

            }
        }

        public Arsonist()
            : base("Arsonist", "arsonist", Color, RoleCategory.Neutral, Side.Arsonist, Side.Arsonist,
                 new HashSet<Side>() { Side.Arsonist }, new HashSet<Side>() { Side.Arsonist },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.ArsonistWin },
                 true, true, true, false, false)
        {
            arsonistButton = null;
        }
    }
}
