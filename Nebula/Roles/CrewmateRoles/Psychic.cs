namespace Nebula.Roles.Crewmate;

public class Psychic : Role
{
    static public Color RoleColor = new Color(96f / 255f, 206f / 255f, 137f / 255f);

    ModAbilityButton searchButton;

    private Module.CustomOption searchCoolDownOption;
    private Module.CustomOption searchDurationOption;

    private Sprite mapIconSprite = null;

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SearchButton.png", 115f, "ui.button.psychic.search");

    public Sprite getMapIconSprite()
    {
        if (mapIconSprite) return mapIconSprite;
        mapIconSprite = Helpers.loadSpriteFromResources("Nebula.Resources.PsychicMapIcon.png", 100f);
        return mapIconSprite;
    }

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.psychic.help.search",0.3f)
    };

    public override void Initialize(PlayerControl __instance)
    {
        foreach (Objects.Ghost g in Ghosts)
        {
            g.Remove();
        }
        Ghosts.Clear();
    }

    EffectActivatedAttribute activatedAttribute;
    
    public override void ButtonInitialize(HudManager __instance)
    {
        searchButton?.Destroy();

        searchButton = new ModAbilityButton(buttonSprite.GetSprite()).SetLabelLocalized("button.label.search");

        activatedAttribute = searchButton.SetUpEffectAttribute(Module.NebulaInputManager.abilityInput.keyCode,
            CustomOptionHolder.InitialAbilityCoolDownOption.getFloat(),
            searchCoolDownOption.getFloat(), searchDurationOption.getFloat(),null,
            ()=>AbstractArrow.RemoveArrow("PsychicArrow")).Item2;
    }

    public override void OnDeadBodyGenerated(DeadBody deadBody)
    {
        if (searchButton.MyAttribute != activatedAttribute) return;

        new FollowerArrow("PsychicArrow",true,deadBody.gameObject,Color.blue, arrowSprite.GetSprite());
    }

    public override void CleanUp()
    {
        searchButton?.Destroy();
    }

    static public HashSet<Objects.Ghost> Ghosts = new HashSet<Objects.Ghost>();

    public override void OnAnyoneMurdered(byte murderId, byte targetId)
    {
        if (targetId == PlayerControl.LocalPlayer.PlayerId) return;

        Objects.Ghost ghost = new Objects.Ghost(Helpers.playerById(targetId).transform.position);
        Ghosts.Add(ghost);
    }

    public override void OnMeetingEnd()
    {
        foreach (Objects.Ghost g in Ghosts)
        {
            g.Remove();
        }
        Ghosts.Clear();
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Vulture);
        RelatedRoles.Add(Roles.Reaper);
    }


    private float deathMessageInterval;

    private string[] PsychicMessage = new string[] { "elapsedTime", "killerColor", "killerRole", "myRole" };

    SpriteLoader arrowSprite = new SpriteLoader("role.psychic.arrow");

    public override void MyPlayerControlUpdate()
    {
        deathMessageInterval -= Time.deltaTime;
        if (deathMessageInterval > 0) return;
        deathMessageInterval = 7f;

        foreach (Game.DeadPlayerData deadPlayerData in Game.GameData.data.deadPlayers.Values)
        {
            if (!deadPlayerData.existDeadBody) continue;

            float distance = deadPlayerData.deathLocation.Distance(PlayerControl.LocalPlayer.transform.position);
            if (distance > 14) continue;

            string m_time = "", m_color = "", m_role = "", i_role = "";

            m_time = ((int)(deadPlayerData.Elapsed / 5f) * 5).ToString();
            i_role = Language.Language.GetString("role." + deadPlayerData.Data.role.LocalizeName + ".name");

            if (deadPlayerData.MurderId != Byte.MaxValue)
            {
                m_color = Module.DynamicColors.IsLightColor(Palette.PlayerColors[deadPlayerData.MurderId]) ?
                    Language.Language.GetString("role.psychic.color.light") : Language.Language.GetString("role.psychic.color.dark");
                m_role = Language.Language.GetString("role." + Helpers.GetModData(deadPlayerData.MurderId).role.LocalizeName + ".name");

            }

            string transratedMessage = Language.Language.GetString("role.psychic.message." + PsychicMessage[NebulaPlugin.rnd.Next(PsychicMessage.Length)]);
            transratedMessage = transratedMessage.Replace("%TIME%", m_time).Replace("%COLOR%", m_color).Replace("%ROLE%", m_role).Replace("%MYROLE%", i_role);

            CustomMessage message = CustomMessage.Create(deadPlayerData.deathLocation, true, transratedMessage, (5 - distance), 1f, 1f, 1f, new Color32(255, 255, 255, 150));
            message.textSwapGain = (int)(distance * 3);
            message.textSwapDuration = 0.05f + (14 - distance) * 0.06f;
            message.textSizeVelocity = new Vector3(0.1f, 0.1f);
            message.velocity = new Vector3(0, 0.1f, 0);
        }
    }

    public override void LoadOptionData()
    {
        searchCoolDownOption = CreateOption(Color.white, "searchCoolDown", 20f, 5f, 60f, 5f);
        searchCoolDownOption.suffix = "second";

        searchDurationOption = CreateOption(Color.white, "searchDuration", 5f, 2.5f, 20f, 1.25f);
        searchDurationOption.suffix = "second";
    }

    public Psychic()
        : base("Psychic", "psychic", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
        deathMessageInterval = 5f;
        searchButton = null;
    }
}