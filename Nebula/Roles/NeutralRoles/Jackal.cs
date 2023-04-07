using static Il2CppSystem.Globalization.CultureInfo;

namespace Nebula.Roles.NeutralRoles;
public class Jackal : Role
{
    static public Color RoleColor = new Color(0f, 162f / 255f, 211f / 255f);

    static private ModAbilityButton killButton;
    static private ModAbilityButton sidekickButton;

    static public Module.CustomOption CanCreateSidekickOption;
    static public Module.CustomOption NumOfKillingToCreateSidekickOption;
    static public Module.CustomOption KillCoolDownOption;

    private SpriteLoader sidekickButtonSprite = new SpriteLoader("Nebula.Resources.SidekickButton.png", 115f, "ui.button.jackal.sidekick");

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(sidekickButtonSprite,"role.jackal.help.sidekick",0.3f)
    };

    public int jackalDataId { get; private set; }
    public int leftSidekickDataId { get; private set; }
    public int killingDataId { get; private set; }
    public override RelatedRoleData[] RelatedRoleDataInfo
    {
        get => new RelatedRoleData[] {
            new RelatedRoleData(killingDataId, "Jackal Kill", 0, 15),
            new RelatedRoleData(jackalDataId, "Jackal Identifier", 0, 15),
            new RelatedRoleData(leftSidekickDataId, "Can Create Sidekick", 0, 1,new string[]{ "False","True"})};
    }


    public override void LoadOptionData()
    {
        CanCreateSidekickOption = CreateOption(Color.white, "canCreateSidekick", true);
        KillCoolDownOption = CreateOption(Color.white, "killCoolDown", 20f, 10f, 60f, 2.5f);
        KillCoolDownOption.suffix = "second";

        NumOfKillingToCreateSidekickOption = CreateOption(Color.white, "numOfKillingToCreateSidekick", 3, 0, 10, 1).AddPrerequisite(CanCreateSidekickOption);
    }

    public override IEnumerable<Assignable> GetFollowRoles()
    {
        yield return Roles.Sidekick;
    }

    public override void GlobalInitialize(PlayerControl __instance)
    {
        __instance.GetModData().SetRoleData(jackalDataId, __instance.PlayerId);
        __instance.GetModData().SetRoleData(leftSidekickDataId, CanCreateSidekickOption.getBool() ? 1 : 0);
        __instance.GetModData().SetRoleData(killingDataId, 0);
    }


    public override void ButtonInitialize(HudManager __instance)
    {
        int jackalId = Game.GameData.data.AllPlayers[PlayerControl.LocalPlayer.PlayerId].GetRoleData(jackalDataId);
        SpriteRenderer? lockedRenderer = null;

        bool canCreateSidekick(out int left)
        {
            int killing = PlayerControl.LocalPlayer.GetModData().GetRoleData(killingDataId);
            int goal = (int)NumOfKillingToCreateSidekickOption.getFloat();
            left = goal - killing;
            return killing >= goal;
        }
        void setUpSidekickButtonAttribute() {
            sidekickButton.MyAttribute = new InterpersonalAbilityAttribute(0f,0f,(p)=>true,Color.yellow, GameManager.Instance.LogicOptions.GetKillDistance(),
                new SimpleButtonEvent((button) =>
                {
                    int jackalId = PlayerControl.LocalPlayer.GetModData().GetRoleData(jackalDataId);
                    RPCEventInvoker.CreateSidekick(Game.GameData.data.myData.currentTarget.PlayerId, (byte)jackalId);
                    RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftSidekickDataId, -1);

                    Game.GameData.data.myData.currentTarget = null;

                    button.Destroy();
                }, Module.NebulaInputManager.abilityInput.keyCode, true));
            sidekickButton.ShowUsesText(false);
        }

        killButton?.Destroy();
        killButton = new ModAbilityButton(__instance.KillButton.graphic.sprite, Expansion.GridArrangeExpansion.GridArrangeParameter.AlternativeKillButtonContent)
            .SetLabelLocalized("button.label.kill").SetLabelType(ModAbilityButton.LabelType.Impostor);

        killButton.MyAttribute = new KillAbilityAttribute(
            KillCoolDownOption.getFloat(), CustomOptionHolder.InitialKillCoolDownOption.getFloat(),
            (player) =>
            {
                if (player.Object.inVent) return false;
                var modData = player.GetModData();
                if (modData.role == Roles.Sidekick) return modData.GetRoleData(jackalDataId) != jackalId;
                else if (modData.HasExtraRole(Roles.SecondarySidekick)) return modData.GetExtraRoleData(Roles.SecondarySidekick) != (ulong)jackalId;

                return true;
            }, Palette.ImpostorRed, GameManager.Instance.LogicOptions.GetKillDistance(), Game.PlayerData.PlayerStatus.Dead,
            (player) =>
            {
                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, killingDataId, 1);
                if(canCreateSidekick(out int left) && lockedRenderer!=null)
                {
                    GameObject.Destroy(lockedRenderer.gameObject);
                    lockedRenderer = null;
                    setUpSidekickButtonAttribute();
                }
                else
                {
                    sidekickButton.UsesText.text = left.ToString();
                }
            });

        sidekickButton?.Destroy();
        sidekickButton = new ModAbilityButton(sidekickButtonSprite.GetSprite(), Expansion.GridArrangeExpansion.GridArrangeParameter.LeftSideContent)
            .SetLabelLocalized("button.label.sidekick");


        if (canCreateSidekick(out int left))
            setUpSidekickButtonAttribute();
        else
        {
            sidekickButton.UsesText.text = left.ToString();
            lockedRenderer = AbilityButtonDecorator.AddLockedOverlay(sidekickButton);
        }
    }

    public override void CleanUp()
    {
        killButton?.Destroy();
        sidekickButton?.Destroy();
    }

    private void ChangeSidekickToJackal(byte playerId)
    {
        //SidekickをJackalに昇格

        //対象のJackalID
        int jackalId = Game.GameData.data.AllPlayers[playerId].GetRoleData(jackalDataId);

        foreach (Game.PlayerData player in Game.GameData.data.AllPlayers.Values)
        {
            if (Sidekick.SidekickTakeOverOriginalRoleOption.getBool())
            {
                //Jackalに変化できるプレイヤーを抽出

                if (player.role.id != Roles.Sidekick.id) continue;
                if (player.GetRoleData(jackalDataId) != jackalId) continue;
            }
            else
            {
                //プレイヤーを抽出し、追加役職としてのSidekickを除去

                if (!player.HasExtraRole(Roles.SecondarySidekick)) continue;
                if (player.GetExtraRoleData(Roles.SecondarySidekick) != (ulong)jackalId) continue;

                RPCEvents.ImmediatelyUnsetExtraRole(Roles.SecondarySidekick, player.id);
            }

            RPCEvents.ImmediatelyChangeRole(player.id, id);
            RPCEvents.UpdateRoleData(player.id, jackalDataId, jackalId);
            RPCEvents.UpdateRoleData(player.id, leftSidekickDataId, Sidekick.SidekickCanCreateSidekickOption.getBool() ? 1 : 0);
        }
    }

    public override void GlobalFinalizeInGame(PlayerControl __instance)
    {
        base.GlobalFinalizeInGame(__instance);

        ChangeSidekickToJackal(__instance.PlayerId);
    }

    public override void OnDied(byte playerId)
    {
        ChangeSidekickToJackal(playerId);
    }

    public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
    {
        if (PlayerControl.LocalPlayer.GetModData().role == Roles.Sidekick || PlayerControl.LocalPlayer.GetModData().role == Roles.Jackal)
        {
            if (PlayerControl.LocalPlayer.GetModData().GetRoleData(jackalDataId) == Helpers.playerById(playerId).GetModData().GetRoleData(jackalDataId))
            {
                displayColor = RoleColor;
            }
        }
        else if (PlayerControl.LocalPlayer.GetModData().HasExtraRole(Roles.SecondarySidekick))
        {
            if (PlayerControl.LocalPlayer.GetModData().GetExtraRoleData(Roles.SecondarySidekick) == (ulong)Helpers.playerById(playerId).GetModData().GetRoleData(jackalDataId))
            {
                displayColor = RoleColor;
            }
        }
    }

    public Jackal()
        : base("Jackal", "jackal", RoleColor, RoleCategory.Neutral, Side.Jackal, Side.Jackal,
             new HashSet<Side>() { Side.Jackal }, new HashSet<Side>() { Side.Jackal },
             new HashSet<Patches.EndCondition>() { Patches.EndCondition.JackalWin },
             true, VentPermission.CanUseUnlimittedVent, true, true, true)
    {
        killButton = null;
        jackalDataId = Game.GameData.RegisterRoleDataId("jackal.identifier");
        leftSidekickDataId = Game.GameData.RegisterRoleDataId("jackal.leftSidekick");
        killingDataId = Game.GameData.RegisterRoleDataId("jackal.killing");
    }
}