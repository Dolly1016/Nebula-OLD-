namespace Nebula.Roles.ImpostorRoles;

public class EvilAce : Role
{
    private Module.CustomOption killCoolDownMultiplierOption;
    private Module.CustomOption canKnowDeadNonImpostorsRolesOption;
    private Module.CustomOption canKnowRolesOnlyMyMurdersOption;

    public override void LoadOptionData()
    {
        killCoolDownMultiplierOption = CreateOption(Color.white, "killCoolDown", 0.5f, 0.125f, 1f, 0.125f);
        killCoolDownMultiplierOption.suffix = "cross";
        killCoolDownMultiplierOption.IntimateValueDecorator = (text, option) => {
            float t = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * option.getFloat();
            return string.Format(text + Helpers.cs(new Color(0.8f, 0.8f, 0.8f), " ({0:0.##}" + Language.Language.GetString("option.suffix.second") + ")"), t);
        };

        canKnowDeadNonImpostorsRolesOption = CreateOption(Color.white, "canKnowDeadNonImpostorsRoles", false);
        canKnowRolesOnlyMyMurdersOption = CreateOption(Color.white, "canKnowRolesOnlyMyMurders", true).AddPrerequisite(canKnowDeadNonImpostorsRolesOption);
    }

    public override void OnAnyoneDied(byte playerId)
    {
        try
        {
            PlayerControl p = Helpers.playerById(playerId);
            var data = p.GetModData();
            //赤文字は何もしない
            if (data.role.category == RoleCategory.Impostor || data.role.GetActualRole(data) == Roles.Spy) return;
            if ((!canKnowRolesOnlyMyMurdersOption.getBool()) || Game.GameData.data.deadPlayers[p.PlayerId].MurderId == PlayerControl.LocalPlayer.PlayerId)
                data.RoleInfo = Helpers.cs(data.role.GetActualRole(data).Color, Language.Language.GetString("role." + data.role.GetActualRole(data).LocalizeName + ".name"));
        }
        catch { }
    }


    public override void onRevived(byte playerId)
    {
        //情報を消去する
        try
        {
            PlayerControl p = Helpers.playerById(playerId);
            var data = p.GetModData();
            data.RoleInfo = "";
        }
        catch { }
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Jackal);
    }

    public EvilAce()
            : base("EvilAce", "evilAce", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet, Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {

    }

    public override void SetKillCoolDown(ref float multiplier, ref float addition)
    {
        int impostorSide = 0;
        foreach (Game.PlayerData data in Game.GameData.data.AllPlayers.Values)
        {
            if (!data.IsAlive)
            {
                continue;
            }
            if (data.role.side == Side.Impostor)
            {
                impostorSide++;
            }
        }
        if (impostorSide == 1)
        {
            multiplier = killCoolDownMultiplierOption.getFloat();
        }
    }
}