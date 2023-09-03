using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Seer : ConfigurableStandardRole
{
    static public Seer MyRole = new Seer();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "seer";
    public override Color RoleColor => new Color(73f / 255f, 166f / 255f, 104f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration GhostDurationOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        GhostDurationOption = new(RoleConfig, "ghostDuration", null, 15f, 300f, 15f, 90f, 90f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    [NebulaPreLoad]
    public class Ghost : INebulaScriptComponent
    {
        SpriteRenderer renderer;
        static XOnlyDividedSpriteLoader ghostSprite = XOnlyDividedSpriteLoader.FromResource("Nebula.Resources.Ghost.png", 100f, 9);
        private float time;
        private float indexTime;
        private int index;
        public Ghost(Vector2 pos, float time) : base()
        {
            renderer = UnityHelper.CreateObject<SpriteRenderer>("Ghost", null, (Vector3)pos + new Vector3(0, 0, -10f), LayerExpansion.GetDrawShadowsLayer());
            this.time = time;

            renderer.sprite = ghostSprite.GetSprite(0);
        }

        public override void Update()
        {
            if (time > 0f && AmongUsUtil.InMeeting) time -= Time.deltaTime;
            indexTime -= Time.deltaTime;

            if (indexTime < 0f)
            {
                index = time > 0f ? (index + 1) % 3 : index + 1;
                indexTime = 0.3f;

                if (index < 9) renderer.sprite = ghostSprite.GetSprite(index);
                else Release();
            }
        }

        public override void OnReleased()
        {
            if(renderer)GameObject.Destroy(renderer.gameObject);
        }
    }

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnPlayerDeadLocal(PlayerControl dead)
        {
            new Ghost(dead.transform.position, MyRole.GhostDurationOption.GetFloat()!.Value);
        }
    }
}


