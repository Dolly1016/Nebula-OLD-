using Il2CppSystem.Text.Json;
using Nebula.Configuration;
using Nebula.Modules.ScriptComponents;
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

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private NebulaConfiguration NumOfExemptedTasksOption = null!;
    private NebulaConfiguration NumOfExtraTasksOption = null!;
    private new VentConfiguration VentConfiguration = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        VentConfiguration = new(RoleConfig, (0, 16, 3), null, (2.5f, 30f, 10f));
        NumOfExemptedTasksOption = new(RoleConfig, "numOfExemptedTasks", null, 1, 8, 3, 3);
        NumOfExtraTasksOption = new(RoleConfig, "numOfExtraTasks", null, 1, 8, 3, 3);
    }

    public class Instance : Crewmate.Instance
    {
        private ModAbilityButton? taskButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.AgentButton.png", 115f);
        public override AbstractRole Role => MyRole;
        public override bool CanUseVent => leftVent > 0;
        private int leftVent = MyRole.VentConfiguration.Uses;
        private Timer ventDuration = new Timer(MyRole.VentConfiguration.Duration);
        private TMPro.TextMeshPro UsesText = null!;

        public override Timer? VentDuration => ventDuration;
        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void OnEnterVent(Vent vent)
        {
            ventDuration.Start();

            leftVent--;
            UsesText.text = leftVent.ToString();
            if (leftVent <= 0) UsesText.transform.parent.gameObject.SetActive(false);
        }

        public override void OnSetTaskLocal(ref List<GameData.TaskInfo> tasks)
        {
            int extempts = MyRole.NumOfExemptedTasksOption;
            for (int i = 0; i < extempts; i++) tasks.RemoveAt(System.Random.Shared.Next(tasks.Count));
        }

        public override void OnActivated()
        {
            if (AmOwner)
            {
                taskButton = Bind(new ModAbilityButton()).KeyBind(NebulaInput.GetKeyCode(KeyAssignmentType.Ability));
                taskButton.SetSprite(buttonSprite.GetSprite());
                taskButton.Availability = (button) => MyPlayer.MyControl.CanMove && MyPlayer.Tasks.IsCompletedCurrentTasks;
                taskButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                taskButton.OnClick = (button) => {
                    MyPlayer.Tasks.GainExtraTasksAndRecompute(MyRole.NumOfExtraTasksOption, 0, 0, false);
                };
                taskButton.SetLabelType(ModAbilityButton.LabelType.Standard);
                taskButton.SetLabel("agent");


                Bind(new GameObjectBinding(HudManager.Instance.ImpostorVentButton.ShowUsesIcon(3, out UsesText)));
                UsesText.text = leftVent.ToString();
            }
        }
    }
}

