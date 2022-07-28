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
    public class Empiric : Template.HasAlignedHologram , Template.HasWinTrigger
    {
        static public Color RoleColor = new Color(183f / 255f, 233f / 255f, 0f / 255f);

        static private CustomButton infectButton;
        private TMPro.TMP_Text infectButtonString;

        private Module.CustomOption maxInfectMyselfOption;
        private Module.CustomOption infectRangeOption;
        private Module.CustomOption infectDurationOption;
        private Module.CustomOption canInfectMyKillerOption;
        private Module.CustomOption coastingPhaseOption;
        private Module.CustomOption canUseVentsOption;
        private Module.CustomOption ventCoolDownOption;
        private Module.CustomOption ventDurationOption;

        private int leftInfect;
        private Dictionary<byte, float> infectProgress;
        private float coasting;

        public bool WinTrigger { get; set; } = false;
        public byte Winner { get; set; } = Byte.MaxValue;

        public override void LoadOptionData()
        {
            maxInfectMyselfOption = CreateOption(Color.white, "maxInfectMyself", 1f, 1f, 5f, 1f);

            infectRangeOption = CreateOption(Color.white, "infectRange", 1f, 0.25f, 3f, 0.25f);
            infectRangeOption.suffix = "cross";

            infectDurationOption = CreateOption(Color.white, "infectDuration", 20f, 5f, 60f, 1f);
            infectDurationOption.suffix = "second";

            canInfectMyKillerOption = CreateOption(Color.white, "canInfectMyKiller", true);

            coastingPhaseOption = CreateOption(Color.white, "coastingPhase", 10f, 0f, 30f, 1f);
            coastingPhaseOption.suffix = "second";

            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
            ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f).AddPrerequisite(canUseVentsOption);
            ventCoolDownOption.suffix = "second";
            ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f).AddPrerequisite(canUseVentsOption);
            ventDurationOption.suffix = "second";
        }

        Sprite infectSprite;
        public Sprite getInfectButtonSprite()
        {
            if (infectSprite) return infectSprite;
            infectSprite = Helpers.loadSpriteFromResources("Nebula.Resources.InfectButton.png", 115f);
            return infectSprite;
        }

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);

            CanMoveInVents = canUseVentsOption.getBool();
            VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseLimittedVent : VentPermission.CanNotUse;
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            infectProgress.Clear();
            leftInfect = (int)maxInfectMyselfOption.getFloat();
            WinTrigger = false;

            VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
            VentDurationMaxTimer = ventDurationOption.getFloat();
        }

        public override void CleanUp()
        {
            base.CleanUp();

            leftInfect = 0;
            WinTrigger = false;

            if (infectButton != null)
            {
                infectButton.Destroy();
                infectButton = null;
            }
            if (infectButtonString != null)
            {
                UnityEngine.Object.Destroy(infectButtonString.gameObject);
                infectButtonString = null;
            }
        }

        public override void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index)
        {
            base.InitializePlayerIcon(player, PlayerId, index);

            player.cosmetics.nameText.transform.localScale *= 2f;
            player.cosmetics.nameText.transform.position += new Vector3(0,0.25f);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (infectButton != null)
            {
                infectButton.Destroy();
            }
            infectButton = new CustomButton(
                () =>
                {
                    if (!activePlayers.Contains(Game.GameData.data.myData.currentTarget.PlayerId))
                    {
                        activePlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);
                        leftInfect--;
                        Game.GameData.data.myData.currentTarget = null;
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && leftInfect>0; },
                () => {
                    infectButtonString.text = $"{leftInfect}/{(int)maxInfectMyselfOption.getFloat()}";
                    return Game.GameData.data.myData.currentTarget!=null && PlayerControl.LocalPlayer.CanMove; },
                () => { },
                getInfectButtonSprite(),
                new Vector3(-1.8f, -0.06f, 0),
                __instance,
                KeyCode.F,
                false,
                "button.label.infect"
            );

            infectButtonString = GameObject.Instantiate(infectButton.actionButton.cooldownTimerText, infectButton.actionButton.cooldownTimerText.transform.parent);
            infectButtonString.text = "";
            infectButtonString.enableWordWrapping = false;
            infectButtonString.transform.localScale = Vector3.one * 0.5f;
            infectButtonString.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);
        }

        public override void ButtonActivate()
        {
            base.ButtonActivate();

            infectButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            base.ButtonDeactivate();

            infectButton.setActive(false);
        }

        public override void OnMeetingStart()
        {
            base.OnMeetingStart();
            
            //停滞期
            coasting = coastingPhaseOption.getFloat();
        }

        public override void OnMurdered(byte murderId)
        {
            base.OnMurdered(murderId);

            if(canInfectMyKillerOption.getBool())
                activePlayers.Add(murderId);
        }

        public override void MyPlayerControlUpdate()
        {
            base.MyPlayerControlUpdate();

            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1f,false, false, activePlayers);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

            //感染停滞期を進める
            if (MeetingHud.Instance == null)
            {
                coasting -= Time.deltaTime;
            }

            //感染しない間はなにもしない
            if (coasting > 0f || MeetingHud.Instance!=null)
            {
                return;
            }

            bool allPlayerInfected = true;

            float infectDistance = 1f*infectRangeOption.getFloat();
            float infectProgressPerTime = Time.deltaTime / infectDurationOption.getFloat();
            bool infectProceedFlag = false;
            PlayerControl infected;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.IsDead) continue;
                if (activePlayers.Contains(player.PlayerId)) continue;
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (!player.gameObject.active) continue;

                allPlayerInfected = false;

                infectProceedFlag = false;

                foreach (byte playerId in activePlayers)
                {
                    infected = Helpers.playerById(playerId);
                    if (infected.Data.IsDead) continue;
                    if(infected.transform.position.Distance(player.transform.position)< infectDistance)
                    {
                        infectProceedFlag = true;
                        break;
                    }
                }

                if (infectProceedFlag)
                {
                    if (!infectProgress.ContainsKey(player.PlayerId))
                    {
                        infectProgress.Add(player.PlayerId, 0);
                    }
                    infectProgress[player.PlayerId] += infectProgressPerTime;

                    if (infectProgress[player.PlayerId] > 1)
                    {
                        activePlayers.Add(player.PlayerId);
                    }
                }
            }

            if (allPlayerInfected)RPCEventInvoker.WinTrigger(this);

            foreach (KeyValuePair<byte,PoolablePlayer> player in PlayerIcons)
            {
                if (!player.Value.gameObject.active)
                {
                    player.Value.cosmetics.nameText.text = "";
                    continue;
                }

                if (activePlayers.Contains(player.Key))
                {
                    player.Value.cosmetics.nameText.text = "";
                }
                else
                {
                    if (infectProgress.ContainsKey(player.Key))
                    {
                        player.Value.cosmetics.nameText.text = String.Format("{0:f1}%", infectProgress[player.Key]*100f);
                    }
                    else
                    {
                        player.Value.cosmetics.nameText.text = "0.0%";
                    }
                    player.Value.cosmetics.nameText.color = Color.white;
                }
            }
        }

        public Empiric()
            : base("Empiric", "empiric", RoleColor, RoleCategory.Neutral, Side.Empiric, Side.Empiric,
                 new HashSet<Side>() { Side.Empiric }, new HashSet<Side>() { Side.Empiric },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.EmpiricWin },
                 true, VentPermission.CanUseLimittedVent, true, false, false)
        {
            infectButton = null;
            infectProgress = new Dictionary<byte, float>();
            coasting = 0f;

            Patches.EndCondition.EmpiricWin.TriggerRole = this;
        }
    }
}
