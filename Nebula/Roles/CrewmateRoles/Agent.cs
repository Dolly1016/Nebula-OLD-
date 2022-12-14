namespace Nebula.Roles.CrewmateRoles;

public class Agent : Template.ExemptTasks
{
    static public Color RoleColor = new Color(166f / 255f, 183f / 255f, 144f / 255f);

    private CustomButton agentButton;

    private TMPro.TextMeshPro ventButtonUsesString;
    private GameObject ventButtonUsesObject;
    public int remainingVentsDataId { get; private set; }
    public override RelatedRoleData[] RelatedRoleDataInfo { get => new RelatedRoleData[] { new RelatedRoleData(remainingVentsDataId, "Use of Vent", 0, 20) }; }


    private Module.CustomOption maxVentsOption;
    private Module.CustomOption actOverOption;
    private Module.CustomOption madmateKillCoolDownOption;

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        actOverOption = CreateOption(Color.white, "actOverTasks", 1f, 1f, 10f, 1f);

        maxVentsOption = CreateOption(Color.white, "maxVents", 3f, 0f, 20f, 1f);

        madmateKillCoolDownOption = CreateOption(Color.white, "killCoolDownBonus", 5f, 2.5f, 20f, 2.5f).
            AddCustomPrerequisite(() => { return CanHaveExtraAssignable(Roles.SecondaryMadmate) && Roles.SecondaryMadmate.IsSpawnable(); });
        madmateKillCoolDownOption.suffix = "second";
    }

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.AgentButton.png", 115f);

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.agent.help.agent",0.3f)
    };

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetRoleData(remainingVentsDataId, (int)maxVentsOption.getFloat());
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (agentButton != null)
        {
            agentButton.Destroy();
        }
        agentButton = new CustomButton(
            () =>
            {
                Game.TaskData task = PlayerControl.LocalPlayer.GetModData().Tasks;

                RPCEventInvoker.RefreshTasks(PlayerControl.LocalPlayer.PlayerId, (int)actOverOption.getFloat(), 0, 0.2f);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                Game.TaskData? task = PlayerControl.LocalPlayer.GetModData().Tasks;
                if (task == null) return false;

                return task.AllTasks == task.Completed && PlayerControl.LocalPlayer.CanMove && task.Quota > 0;

            },
            () => { agentButton.Timer = 0; },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.agent"
        );
        agentButton.MaxTimer = agentButton.Timer = 0;

        var ventButton = FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton;
        ventButtonUsesObject = ventButton.ShowUsesIcon(0,out ventButtonUsesString);
        ventButtonUsesString.text = maxVentsOption.getFloat().ToString();
        ventButton.gameObject.GetComponent<SpriteRenderer>().sprite = RoleManager.Instance.AllRoles.First(r => r.Role == RoleTypes.Engineer).Ability.Image;
        ventButton.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().outlineColor = Palette.CrewmateBlue;

    }

    public override void MyUpdate()
    {
        var data = PlayerControl.LocalPlayer.GetModData();
        if (data == null) return;

        VentPermission = (!PlayerControl.LocalPlayer.inVent && data.GetRoleData(remainingVentsDataId) <= 0) ? VentPermission.CanNotUse : VentPermission.CanUseUnlimittedVent;
    }

    public override void OnEnterVent(Vent vent)
    {
        PlayerControl.LocalPlayer.GetModData().AddRoleData(remainingVentsDataId, -1);
        int remain = PlayerControl.LocalPlayer.GetModData().GetRoleData(remainingVentsDataId);
        if (ventButtonUsesObject)
            ventButtonUsesString.text = remain.ToString();
    }


    public override void CleanUp()
    {
        if (agentButton != null)
        {
            agentButton.Destroy();
            agentButton = null;
        }
        if (ventButtonUsesObject)
        {
            UnityEngine.Object.Destroy(ventButtonUsesObject);
        }
    }
    public override void FinalizeInGame(PlayerControl __instance) 
    { 
        if (HudManager.InstanceExists)
        {
            var ventButton = FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton;
            ventButton.gameObject.GetComponent<SpriteRenderer>().sprite = CustomButton.OriginalVentButtonSprite;
            ventButton.transform.GetChild(1).GetComponent<TMPro.TextMeshPro>().outlineColor = Palette.ImpostorRed;
        }
    }

    public override void OnTaskComplete()
    {
        if (!PlayerControl.LocalPlayer.IsMadmate()) return;

        //Madmate設定 インポスターのキルクールを進める
        RPCEventInvoker.EditCoolDown(CoolDownType.ImpostorsKill, madmateKillCoolDownOption.getFloat());
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.EvilGuesser);
        RelatedRoles.Add(Roles.NiceGuesser);
        RelatedRoles.Add(Roles.EvilAce);
    }

    public override bool HasExecutableFakeTask(byte playerId)
    {
        return Game.GameData.data.GetPlayerData(playerId).HasExtraRole(Roles.SecondaryMadmate);
    }

    public Agent()
        : base("Agent", "agent", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanUseUnlimittedVent, true, false, false)
    {
        agentButton = null;

        remainingVentsDataId = Game.GameData.RegisterRoleDataId("agent.remainVents");

        VentColor = Palette.CrewmateBlue;
    }
}