namespace Nebula.Roles.CrewmateRoles;

public class Oracle : Role
{
    static public Color RoleColor = new Color(214f / 255f, 156f / 255f, 45f / 255f);

    private CustomButton oracleButton;

    private Module.CustomOption OracleCooldownOption;
    private Module.CustomOption OracleCooldownAdditionOption;
    private Module.CustomOption OracleDurationOption;
    private Module.CustomOption CandidatesOption;
    private Module.CustomOption DieWhenDiviningOpportunistOption;
    private Module.CustomOption DieWhenDiviningOracleOption;

    private Dictionary<byte, List<Role>> divineResult = new Dictionary<byte, List<Role>>();

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.OracleButton.png", 115f, "ui.button.oracle.oracle");
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.oracle.help.oracle", 0.3f)
    };

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1.5f);
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
    }

    public override void Initialize(PlayerControl __instance)
    {
        divineResult.Clear();

        Game.GameData.data.EstimationAI.DivPresume(__instance, this, 1f);

        if (DieWhenDiviningOpportunistOption.getBool())
            Game.GameData.data.EstimationAI.Eliminate(Roles.Opportunist);

        if (DieWhenDiviningOracleOption.getBool())
            Game.GameData.data.EstimationAI.Eliminate(Roles.Oracle);

    }

    public override void ButtonInitialize(HudManager __instance)
    {
        if (oracleButton != null)
        {
            oracleButton.Destroy();
        }
        oracleButton = new CustomButton(
            () =>
            {
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (oracleButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                {
                    oracleButton.Timer = 0f;
                    oracleButton.isEffectActive = false;
                }
                return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove;
            },
            () => { oracleButton.Timer = oracleButton.MaxTimer; },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            true,
            OracleDurationOption.getFloat(),
            () =>
            {
                PlayerControl target = Game.GameData.data.myData.currentTarget;

                if (CannotDivineRole(target.GetModData().role))
                {
                    Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer, Game.PlayerData.PlayerStatus.Punished, false, false);
                    return;
                }

                    //まだ占っていなければ占う
                    if (!divineResult.ContainsKey(target.PlayerId))
                {
                    divineResult[target.PlayerId] = Divine(target);
                }

                string message = Language.Language.GetString("role.oracle.message");
                string roles = "";
                int index = 0;
                float rate = 1f;
                foreach (var role in divineResult[target.PlayerId])
                {
                    if (!roles.Equals(""))
                    {
                        roles += ", ";
                    }

                    if (index % 4 == 3) { roles += "\n"; rate *= 1.8f; }

                    roles += Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name"));

                    index++;
                }
                target.GetModData().RoleInfo = roles.Replace("\n", "");
                message = message.Replace("%ROLES%", roles);
                message = message.Replace("%PLAYER%", target.name);
                CustomMessage customMessage = CustomMessage.Create(target.transform.position, true, message, 5f, 0.5f, 2f, rate, Color.white);
                customMessage.velocity = new Vector3(0f, 0.1f);

                oracleButton.MaxTimer += OracleCooldownAdditionOption.getFloat();
                oracleButton.Timer = oracleButton.MaxTimer;
                Game.GameData.data.myData.currentTarget = null;
            },
            "button.label.oracle"
        ).SetTimer(CustomOptionHolder.InitialModestAbilityCoolDownOption.getFloat());
        oracleButton.MaxTimer = OracleCooldownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (oracleButton != null)
        {
            oracleButton.Destroy();
            oracleButton = null;
        }
    }

    public override void PreloadOptionData()
    {
        extraAssignableOptions.Add(Roles.Confused, null);
        defaultUnassignable.Add(Roles.SecondaryGuesser);
    }

    public override void LoadOptionData()
    {
        OracleCooldownOption = CreateOption(Color.white, "divineCoolDown", 30f, 10f, 60f, 2.5f);
        OracleCooldownOption.suffix = "second";

        OracleCooldownAdditionOption = CreateOption(Color.white, "divineCoolDownAddition", 5f, 0f, 30f, 2.5f);
        OracleCooldownAdditionOption.suffix = "second";

        OracleDurationOption = CreateOption(Color.white, "divineDuration", 1f, 0.5f, 5f, 0.5f);
        OracleDurationOption.suffix = "second";

        CandidatesOption = CreateOption(Color.white, "countOfCandidates", 4f, 1f, 8f, 1f);

        DieWhenDiviningOpportunistOption = CreateOption(Color.white, "dieWhenDiviningOpportunist", false);
        DieWhenDiviningOracleOption = CreateOption(Color.white, "dieWhenDiviningOracle", true);
    }

    public override void OnAnyoneRoleChanged(byte playerId)
    {
        divineResult.Remove(playerId);
    }

    private List<Role> Divine(PlayerControl target)
    {
        List<Role> result = new List<Role>();
        Role role = null;

        var data = target.GetModData();
        result.Add(data.role.GetActualRole(data));

        float relatedRoleChance = 0.8f *
            (float)(Game.GameData.data.myData.getGlobalData().Tasks?.Completed ?? 0f) /
            (float)(Game.GameData.data.myData.getGlobalData().Tasks?.Quota ?? 1f);

        for (int i = 1; i < (int)CandidatesOption.getFloat(); i++)
        {
            if (result.Count < 3)
            {
                List<RoleCategory> leftCategory = new List<RoleCategory>();
                if (!result.Any<Role>(role => role.oracleCategory == RoleCategory.Crewmate)) leftCategory.Add(RoleCategory.Crewmate);
                if (!result.Any<Role>(role => role.oracleCategory == RoleCategory.Impostor)) leftCategory.Add(RoleCategory.Impostor);
                if (!result.Any<Role>(role => role.oracleCategory == RoleCategory.Neutral)) leftCategory.Add(RoleCategory.Neutral);

                role = DivineRole(target, result, leftCategory[NebulaPlugin.rnd.Next(leftCategory.Count)], relatedRoleChance);
            }
            else
            {
                role = DivineRole(target, result, null, relatedRoleChance);
            }

            if (role != null) result.Add(role);
        }

        //ランダムに並び替えたものを返す
        return result.OrderBy(a => Guid.NewGuid()).ToList();
    }

    private Role DivineRole(PlayerControl target, List<Role> excludeRoles, RoleCategory? category, float choiceRelatedRoleChance)
    {
        if (NebulaPlugin.rnd.NextDouble() < choiceRelatedRoleChance)
        {
            Role role = DivineRelatedRole(target, excludeRoles, category);
            if (role != null) return role;
        }

        return DivineRandomRole(excludeRoles, category);
    }

    private Role DivineRandomRole(List<Role> excludeRoles, RoleCategory? category)
    {
        var list = Game.GameData.data.EstimationAI.GetOracleRoleList(category);

        for (int i = 0; i < list.Count; i++)
        {
            if (excludeRoles.Contains(list[i]))
            {
                list.RemoveAt(i);
                i--;
            }
        }

        if (list.Count == 0) return null;
        return list[NebulaPlugin.rnd.Next(list.Count)];
    }

    private Role? DivineRelatedRole(PlayerControl target, List<Role> excludeRoles, RoleCategory? category)
    {
        var candidate = new List<Role>();
        var data = target.GetModData();
        foreach (var role in data.role.GetActualRole(data).RelatedRoles)
        {
            if (category != null && role.oracleCategory != category) continue;
            if (excludeRoles.Contains(role)) continue;
            if (Game.GameData.data.EstimationAI.GetRoleProbability(role) < 0f) continue;

            candidate.Add(role);
        }

        if (candidate.Count == 0) return null;
        return candidate[NebulaPlugin.rnd.Next(candidate.Count)];
    }

    private bool CannotDivineRole(Role role)
    {
        if (DieWhenDiviningOpportunistOption.getBool() && role == Roles.Opportunist)
            return true;

        if (DieWhenDiviningOracleOption.getBool() && role == Roles.Oracle)
            return true;

        return false;
    }

    public Oracle()
        : base("Oracle", "oracle", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             Crewmate.crewmateSideSet, Crewmate.crewmateSideSet, Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
        oracleButton = null;
    }
}