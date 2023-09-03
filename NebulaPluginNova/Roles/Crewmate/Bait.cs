using Nebula.Configuration;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Bait : ConfigurableStandardRole
{
    static public Bait MyRole = new Bait();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "bait";
    public override Color RoleColor => new Color(0f / 255f, 247f / 255f, 255f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration ShowKillFlashOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        ShowKillFlashOption = new(RoleConfig, "showKillFlash", null, false, false);
    }

    public class Instance : Crewmate.Instance
    {
        public override AbstractRole Role => MyRole;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        private IEnumerator CoReport(PlayerControl murderer)
        {
            if(Bait.MyRole.ShowKillFlashOption.GetBool()!.Value) AmongUsUtil.PlayQuickFlash(Role.RoleColor);

            float t = 0.1f + 0.25f * (float)System.Random.Shared.NextDouble();
            yield return new WaitForSeconds(t);
            murderer.CmdReportDeadBody(MyPlayer.MyControl.Data);
        }
        public override void OnMurdered(PlayerControl murderer)
        {
            if (murderer.PlayerId == MyPlayer.PlayerId) return;

            if (PlayerControl.LocalPlayer.PlayerId == murderer.PlayerId) NebulaManager.Instance.StartCoroutine(CoReport(murderer).WrapToIl2Cpp());
        }
    }
}

