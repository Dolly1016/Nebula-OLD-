using Hazel;

namespace Nebula.Roles.CrewmateRoles
{
    public class Navvy : Role
    {
        static public Color RoleColor = new Color(71f / 255f, 93f / 255f, 206f / 255f);

        private CustomButton repairButton;
        private CustomButton sealButton;

        private Vent ventTarget = null;
        //直したことがあるかどうか
        private bool hasBeenRepaired = false;

        public int remainingScrewsDataId { get; private set; }
        public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(remainingScrewsDataId, "Sealing Tapes", 0, 20) }; }


        private Module.CustomOption maxScrewsOption;
        private Module.CustomOption sealCoolDownOption;
        private Module.CustomOption ventCoolDownOption;
        private Module.CustomOption ventDurationOption;

        private SpriteLoader sealButtonSprite=new SpriteLoader("Nebula.Resources.CloseVentButton.png",115f);
        private SpriteLoader repairButtonSprite = new SpriteLoader("Nebula.Resources.RepairButton.png", 115f);

        public override HelpSprite[] helpSprite => new HelpSprite[]
        {
            new HelpSprite(sealButtonSprite,"role.navvy.help.seal",0.3f),
            new HelpSprite(repairButtonSprite,"role.navvy.help.repair",0.3f)
        };

        private Sprite ventSealedSprite=null;
        public Sprite getVentSealedSprite()
        {
            if (ventSealedSprite) return ventSealedSprite;
            ventSealedSprite = Helpers.loadSpriteFromResources("Nebula.Resources.VentSealed.png", 100f);
            return ventSealedSprite;
        }

        private Sprite caveSealedSprite=null;
        public Sprite getCaveSealedSprite()
        {
            if (caveSealedSprite) return caveSealedSprite;
            caveSealedSprite = Helpers.loadSpriteFromResources("Nebula.Resources.CaveSealed.png", 100f);
            return caveSealedSprite;
        }

        public override void MyPlayerControlUpdate()
        {
            if (!ShipStatus.Instance) return;

            Vent target = null;
            Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
            float closestDistance = float.MaxValue;
            for (int i = 0; i < ShipStatus.Instance.AllVents.Length; i++)
            {
                Vent vent = ShipStatus.Instance.AllVents[i];
                if (vent.GetVentData().PreSealed || vent.GetVentData().Sealed) continue;
                float distance = Vector2.Distance(vent.transform.position, truePosition);
                if (distance <= vent.UsableDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    target = vent;
                }
            }
            ventTarget = target;
        }


        public override void GlobalInitialize(PlayerControl __instance)
        {
            int value = (int)maxScrewsOption.getFloat();
            __instance.GetModData().SetRoleData(remainingScrewsDataId, value);
        }

        public override void Initialize(PlayerControl __instance)
        {
            //最初からは使用できない
            canMoveInVents = false;
            VentPermission = VentPermission.CanNotUse;
            hasBeenRepaired = false;


            VentCoolDownMaxTimer = ventCoolDownOption.getFloat();
            VentDurationMaxTimer = ventDurationOption.getFloat();
        }

        public override void OnMeetingEnd()
        {
            base.OnMeetingEnd();

            //設置後はベント使用可能
            if (Game.GameData.data.myData.getGlobalData().GetRoleData(remainingScrewsDataId) == 0)
            {
                canMoveInVents = true;
                VentPermission = VentPermission.CanUseLimittedVent;
            }
        }

        public void SetSealedVentSprite(Vent vent,float alpha)
        {
            vent.EnterVentAnim = vent.ExitVentAnim = null;
            if (PlayerControl.GameOptions.MapId == 2)
            {
                //Polus
                vent.myRend.sprite = getCaveSealedSprite();
            }
            else
            {
                PowerTools.SpriteAnim animator = vent.GetComponent<PowerTools.SpriteAnim>();
                animator?.Stop();
                vent.myRend.sprite = getVentSealedSprite();
            }
            vent.myRend.color = new Color(1f, 1f, 1f, alpha);
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (sealButton != null)
            {
                sealButton.Destroy();
            }
            sealButton = new CustomButton(
                () => {
                    if (ventTarget != null)
                    { // Seal vent
                        MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SealVent, Hazel.SendOption.Reliable);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(ventTarget.Id);
                        writer.EndMessage();
                        RPCEvents.SealVent(PlayerControl.LocalPlayer.PlayerId,ventTarget.Id);

                        SetSealedVentSprite(ventTarget, 0.5f);
                        ventTarget.GetVentData().PreSealed = true;

                        ventTarget = null;

                        sealButton.Timer = sealButton.MaxTimer;
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && Game.GameData.data.myData.getGlobalData().GetRoleData(remainingScrewsDataId) > 0; },
                () => {
                    int total = (int)maxScrewsOption.getFloat();
                    int remain = Game.GameData.data.myData.getGlobalData().GetRoleData(remainingScrewsDataId);
                    sealButton.UpperText.text = $"{remain}/{total}";

                    return (ventTarget != null) && remain > 0 && PlayerControl.LocalPlayer.CanMove;
                    
                },
                () => { sealButton.Timer = sealButton.MaxTimer; },
                sealButtonSprite.GetSprite(),
                new Vector3(-1.8f, 0f, 0),
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                false,
                "button.label.seal"
            ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
            sealButton.MaxTimer = sealCoolDownOption.getFloat();


            if (repairButton != null)
            {
                repairButton.Destroy();
            }
            repairButton = new CustomButton(
                () => {
                    Helpers.RepairSabotage();
                    hasBeenRepaired = true;
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead && VentPermission!=VentPermission.CanNotUse && !hasBeenRepaired; },
                () => {
                    return Helpers.SabotageIsActive() && PlayerControl.LocalPlayer.CanMove;

                },
                () => { repairButton.Timer = 0; },
                repairButtonSprite.GetSprite(),
                new Vector3(0f, 1f, 0),
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                false,
                "button.label.repair"
            );
            repairButton.MaxTimer = repairButton.Timer = 0;
        }
        public override void CleanUp()
        {
            if (sealButton != null)
            {
                sealButton.Destroy();
                sealButton = null;
            }
            
            if (repairButton != null)
            {
                repairButton.Destroy();
                repairButton = null;
            }
        }

        public override void LoadOptionData()
        {
            maxScrewsOption = CreateOption(Color.white, "maxScrews", 5f, 0f, 7f, 1f);
            sealCoolDownOption = CreateOption(Color.white, "sealCoolDown", 5f, 0f, 40f, 2.5f);
            sealCoolDownOption.suffix = "second";
            ventCoolDownOption = CreateOption(Color.white, "ventCoolDown", 20f, 5f, 60f, 2.5f);
            ventCoolDownOption.suffix = "second";
            ventDurationOption = CreateOption(Color.white, "ventDuration", 10f, 5f, 60f, 2.5f);
            ventDurationOption.suffix = "second";
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Opportunist);
            RelatedRoles.Add(Roles.Vulture);
            RelatedRoles.Add(Roles.Reaper);
        }

        public Navvy()
            : base("Navvy", "navvy", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
                 Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
                 false, VentPermission.CanNotUse, false, false, false)
        {
            sealButton = null;

            remainingScrewsDataId=Game.GameData.RegisterRoleDataId("navvy.remainScrew");

            VentColor = Palette.CrewmateBlue;
        }
    }
}
