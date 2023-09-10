using Nebula.Configuration;

namespace Nebula.Roles.Neutral;

public class Jester : ConfigurableStandardRole
{
    static public Jester MyRole = new Jester();
    static public Team MyTeam = new("teams.jester", MyRole.RoleColor, TeamRevealType.OnlyMe);

    public override RoleCategory RoleCategory => RoleCategory.NeutralRole;

    public override string LocalizedName => "jester";
    public override Color RoleColor => new Color(253f / 255f, 84f / 255f, 167f / 255f);
    public override Team Team => MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration CanDragDeadBodyOption;
    private VentConfiguration VentConfiguration;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        VentConfiguration = new(RoleConfig, null, (5f, 60f, 15f), (2.5f, 30f, 10f));
        CanDragDeadBodyOption = new NebulaConfiguration(RoleConfig, "canDragDeadBody", null, true, true);
    }

    static public NebulaEndCriteria JesterCriteria = new(CustomGameMode.Standard)
    {
        OnExiled = (PlayerControl? p)=>
        {
            if(p== null) return null;

            if (p.GetModInfo().Role.Role != MyRole) return null;
            return new(NebulaGameEnd.JesterWin, 1 << p.PlayerId);
        }
    };

    public class Instance : RoleInstance
    {
        public override AbstractRole Role => MyRole;
        private Scripts.Draggable? draggable = null;
        private Timer ventCoolDown = new Timer(MyRole.VentConfiguration.CoolDown).SetAsAbilityCoolDown().Start();
        private Timer ventDuration = new(MyRole.VentConfiguration.Duration);
        public override Timer? VentCoolDown => ventCoolDown;
        public override Timer? VentDuration => ventDuration;

        public Instance(PlayerModInfo player) : base(player)
        {
            if (MyRole.CanDragDeadBodyOption) draggable = Bind(new Scripts.Draggable());
        }

        public override bool CheckWins(CustomEndCondition endCondition) => false;


        public override void OnActivated()
        {
            NebulaGameManager.Instance?.CriteriaManager.AddCriteria(JesterCriteria);

            draggable?.OnActivated(this);
            
        }

        public override void OnDead()
        {
            draggable?.OnDead(this);
        }

        protected override void OnInactivated()
        {
            draggable?.OnInactivated(this);
        }
    }
}

