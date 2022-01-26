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
        static public Color Color = new Color(183f / 255f, 233f / 255f, 0f / 255f);

        static private CustomButton infectButton;
        private TMPro.TMP_Text infectButtonString;

        private Module.CustomOption maxInfectMyselfOption;
        private Module.CustomOption infectRangeOption;
        private Module.CustomOption infectDurationOption;
        private Module.CustomOption canInfectMyKillerOption;
        private Module.CustomOption coastingPhaseOption;

        private int leftInfect;
        private Dictionary<byte, float> infectProgress;
        private float coasting;

        public bool WinTrigger { get; set; } = false;

        public override void LoadOptionData()
        {
            maxInfectMyselfOption = CreateOption(Color.white, "maxInfectMyself", 1f, 1f, 5f, 1f);

            infectRangeOption = CreateOption(Color.white, "infectRange", 1f, 0.25f, 3f, 0.25f);
            infectRangeOption.suffix = "cross";

            infectDurationOption = CreateOption(Color.white, "infectDuration", 20f, 5f, 120f, 5f);
            infectDurationOption.suffix = "second";

            coastingPhaseOption = CreateOption(Color.white, "coastingPhase", 10f, 0f, 30f, 5f);
            coastingPhaseOption.suffix = "second";
        }


        Sprite infectSprite;
        public Sprite getInfectButtonSprite()
        {
            if (infectSprite) return infectSprite;
            infectSprite = Helpers.loadSpriteFromResources("Nebula.Resources.InfectButton.png", 115f);
            return infectSprite;
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            leftInfect = (int)maxInfectMyselfOption.getFloat();
            WinTrigger = false;
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
                infectButtonString.DestroySubMeshObjects();
                infectButtonString = null;
            }
        }

        public override void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index)
        {
            base.InitializePlayerIcon(player, PlayerId, index);

            player.NameText.transform.localScale *= 2f;
            player.NameText.transform.position += new Vector3(0,0.25f);
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
                KeyCode.F
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

        public override void MyPlayerControlUpdate()
        {
            base.MyPlayerControlUpdate();

            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(false, false, activePlayers);
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
                    player.Value.NameText.text = "";
                    continue;
                }

                if (activePlayers.Contains(player.Key))
                {
                    player.Value.NameText.text = "";
                }
                else
                {
                    if (infectProgress.ContainsKey(player.Key))
                    {
                        player.Value.NameText.text = String.Format("{0:f1}%", infectProgress[player.Key]*100f);
                    }
                    else
                    {
                        player.Value.NameText.text = "0.0%";
                    }
                    player.Value.NameText.color = Color.white;
                }
            }
        }

        public Empiric()
            : base("Empiric", "empiric", Color, RoleCategory.Neutral, Side.Empiric, Side.Empiric,
                 new HashSet<Side>() { Side.Empiric }, new HashSet<Side>() { Side.Empiric },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.EmpiricWin },
                 true, true, true, false, false)
        {
            infectButton = null;
            infectProgress = new Dictionary<byte, float>();
            coasting = 0f;
        }
    }
}
