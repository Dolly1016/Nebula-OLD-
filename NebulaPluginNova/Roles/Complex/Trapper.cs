using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Complex;

public class Trapper : ConfigurableStandardRole
{
    static public Trapper MyNiceRole = new(false);
    static public Trapper MyEvilRole = new(true);

    public bool IsEvil { get; private set; }
    public override RoleCategory RoleCategory => IsEvil ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;

    public override string LocalizedName => IsEvil ? "evilTrapper" : "niceTrapper";
    public override Color RoleColor => IsEvil ? Palette.ImpostorRed : new Color(206f / 255f, 219f / 255f, 96f / 255f);
    public override Team Team => IsEvil ? Impostor.Impostor.MyTeam : Crewmate.Crewmate.MyTeam;
    public override IEnumerable<IAssignableBase> RelatedOnConfig() { if (MyNiceRole != this) yield return MyNiceRole; if (MyEvilRole != this) yield return MyEvilRole; }

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => IsEvil ? new EvilInstance(player) : new NiceInstance(player);

    static public NebulaConfiguration NumOfChargesOption;

    private NebulaConfiguration? CommonEditorOption;

    [NebulaPreLoad]
    public class Trap : NebulaSyncStandardObject
    {
        public static string MyGlobalTag = "TrapGlobal";
        public static string MyLocalTag = "TrapLocal";

        static SpriteLoader[] trapSprites = new SpriteLoader[] { };
        public Trap(Vector2 pos,int type, bool isLocal) : base(pos, ZOption.Back, true, trapSprites[type].GetSprite(), isLocal) { }

        public static void Load()
        {
            NebulaSyncObject.RegisterInstantiater(MyGlobalTag, (args) => new Trap(new(args[1], args[2]), (int)args[0], false));
            NebulaSyncObject.RegisterInstantiater(MyLocalTag, (args) => new Trap(new(args[1], args[2]), (int)args[0], true));
        }

        static public Trap GenerateTrap(int type,Vector2 pos)
        {
            return (NebulaSyncObject.LocalInstantiate(MyLocalTag, new float[] { (float)type, pos.x, pos.y }) as Trap)!;
        }
    }

    public Trapper(bool isEvil)
    {
        IsEvil = isEvil;
    }

    protected override void LoadOptions()
    {
        base.LoadOptions();


        NumOfChargesOption ??= new NebulaConfiguration(null, "role.trapper.numOfCharges", null, 1, 15, 3, 3);

        var commonOptions = new NebulaConfiguration[] { NumOfChargesOption };
        foreach (var option in commonOptions) option.Title = new CombinedComponent(new TranslateTextComponent("role.general.common"), new RawTextComponent(" "), new TranslateTextComponent(option.Id));

        CommonEditorOption = new NebulaConfiguration(RoleConfig, () => {
            MetaContext context = new();
            foreach (var option in commonOptions) context.Append(option.GetEditor()!);
            return context;
        });
    }

    public class NiceInstance : Crewmate.Crewmate.Instance
    {
        public override AbstractRole Role => MyNiceRole;
        private int leftCharge = NumOfChargesOption;
        public NiceInstance(PlayerModInfo player) : base(player)
        {
        }

    }

    public class EvilInstance : Crewmate.Crewmate.Instance
    {
        public override AbstractRole Role => MyEvilRole;
        private int leftCharge = NumOfChargesOption;
        public EvilInstance(PlayerModInfo player) : base(player)
        {
        }

    }
}