using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Impostor;

[NebulaRPCHolder]
public class Camouflager : ConfigurableStandardRole
{
    static public Camouflager MyRole = new Camouflager();
    public override RoleCategory RoleCategory => RoleCategory.ImpostorRole;

    public override string LocalizedName => "camouflager";
    public override Color RoleColor => Palette.ImpostorRed;
    public override Team Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration CamoCoolDownOption;
    private NebulaConfiguration CamoDurationOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        CamoCoolDownOption = new NebulaConfiguration(RoleConfig, "camoCoolDown", null, 5f, 60f, 5f, 20f, 20f) { Decorator = NebulaConfiguration.SecDecorator };
        CamoDurationOption = new NebulaConfiguration(RoleConfig, "camoDuration", null, 5f, 60f, 5f, 15f, 15f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Impostor.Instance
    {
        private ModAbilityButton? camouflageButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.CamoButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                camouflageButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                camouflageButton.SetSprite(buttonSprite.GetSprite());
                camouflageButton.Availability = (button) =>MyPlayer.MyControl.CanMove;
                camouflageButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                camouflageButton.OnClick = (button) => {
                    button.ActivateEffect();
                };
                camouflageButton.OnEffectStart = (button) =>
                {
                    RpcCamouflage.Invoke(new(MyPlayer.PlayerId,true));
                };
                camouflageButton.OnEffectEnd = (button) =>
                {
                    RpcCamouflage.Invoke(new(MyPlayer.PlayerId,false));
                };
                camouflageButton.CoolDownTimer = Bind(new Timer(MyRole.CamoCoolDownOption.GetFloat()!.Value).SetAsAbilityCoolDown().Start());
                camouflageButton.EffectTimer = Bind(new Timer(MyRole.CamoDurationOption.GetFloat()!.Value));
                camouflageButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                camouflageButton.SetLabel("camo");
            }
        }
    }

    private static GameData.PlayerOutfit CamouflagerOutfit = new() { PlayerName = "", ColorId = 16, HatId = "hat_NoHat", SkinId = "skin_None", VisorId = "visor_EmptyVisor", PetId= "pet_EmptyPet" };

    public static RemoteProcess<(byte camouflagerId, bool on)> RpcCamouflage = new(
        "Camouflage",
        (message, _) =>
        {
            PlayerModInfo.OutfitCandidate outfit = new("Camo" + message.camouflagerId, 100, true, CamouflagerOutfit);
            foreach(var p in NebulaGameManager.Instance.AllPlayerInfo())
            {
                if (message.on)
                    p.AddOutfit(outfit);
                else
                    p.RemoveOutfit(outfit.Tag);
            }
        }
        );
}
