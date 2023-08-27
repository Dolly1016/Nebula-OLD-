using Il2CppSystem.Text.Json;
using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Crewmate;

public class Agent : ConfigurableStandardRole
{
    static public Agent MyRole = new Agent();

    public override RoleCategory RoleCategory => RoleCategory.CrewmateRole;

    public override string LocalizedName => "agent";
    public override Color RoleColor => new Color(166f / 255f, 183f / 255f, 144f / 255f);
    public override Team Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(PlayerControl player, int[]? arguments) => new Instance(player);

    private NebulaConfiguration NumOfExemptedTasksOption;
    private NebulaConfiguration NumOfExtraTasksOption;
    protected override void LoadOptions()
    {
        base.LoadOptions();

        NumOfExemptedTasksOption = new(RoleConfig, "numOfExemptedTasks", null, 1, 8, 3, 3);
        NumOfExtraTasksOption = new(RoleConfig, "numOfExtraTasks", null, 1, 8, 3, 3);
    }

    public class Instance : Crewmate.Instance
    {
        private ModAbilityButton? taskButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.AgentButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public Instance(PlayerControl player) : base(player)
        {
        }

        public override void OnSetTaskLocal(ref List<GameData.TaskInfo> tasks)
        {
            int extempts = MyRole.NumOfExemptedTasksOption.GetMappedInt().Value;
            for (int i = 0; i < extempts; i++) tasks.RemoveAt(System.Random.Shared.Next(tasks.Count));
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                taskButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
                taskButton.SetSprite(buttonSprite.GetSprite());
                taskButton.Availability = (button) => player.CanMove && (player.GetModInfo()?.Tasks.IsCompletedCurrentTasks ?? false);
                taskButton.Visibility = (button) => !player.Data.IsDead;
                taskButton.OnClick = (button) => {
                    player.GetModInfo().Tasks.GainExtraTasksAndRecompute(MyRole.NumOfExtraTasksOption.GetMappedInt().Value, 0, 0, false);
                };
                taskButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                taskButton.SetLabel("agent");
            }
        }
    }
}

