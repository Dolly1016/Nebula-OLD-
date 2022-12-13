namespace Nebula.Roles.NeutralRoles;

public class ChainShifter : Role
{
    /* 陣営色 */
    static public Color RoleColor = new Color(115f / 255f, 115f / 255f, 115f / 255f);

    /* オプション */
    private Module.CustomOption isGuessableOption;
    private Module.CustomOption secondaryGuesserShiftOption;

    private PlayerControl? shiftPlayer;

    public override void LoadOptionData()
    {
        isGuessableOption = CreateOption(Color.white, "isGuessable", false);
        secondaryGuesserShiftOption = CreateOption(Color.white, "guesserMode", new string[] { "role.chainShifter.guesserMode.dontShift", "role.chainShifter.guesserMode.erase", "role.chainShifter.guesserMode.shift" });
    }

    public override bool IsGuessableRole { get => isGuessableOption.getBool(); protected set => base.IsGuessableRole = value; }

    public override void MyPlayerControlUpdate()
    {
        if (shiftPlayer == null)
        {
            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget();
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }
    }

    /* ボタン */
    static private CustomButton shiftButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        if (shiftButton != null)
        {
            shiftButton.Destroy();
        }
        shiftButton = new CustomButton(
            () =>
            {
                if (Game.GameData.data.myData.currentTarget != null)
                {
                    shiftPlayer = Game.GameData.data.myData.currentTarget;
                    shiftButton.UpperText.text = shiftPlayer.name;
                }
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return shiftPlayer == null && Game.GameData.data.myData.currentTarget != null && PlayerControl.LocalPlayer.CanMove; },
            () => { shiftButton.Timer = shiftButton.MaxTimer; },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.chainShift"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        shiftButton.MaxTimer = 10.0f;
    }

    public override void OnMeetingStart()
    {
        if (shiftPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
        {
            switch (secondaryGuesserShiftOption.getSelection())
            {
                case 1:
                    RPCEventInvoker.UnsetExtraRole(PlayerControl.LocalPlayer, Roles.SecondaryGuesser, true);
                    RPCEventInvoker.UnsetExtraRole(shiftPlayer, Roles.SecondaryGuesser, true);
                    break;
                case 2:
                    RPCEventInvoker.SwapExtraRole(PlayerControl.LocalPlayer, shiftPlayer, Roles.SecondaryGuesser, true);
                    break;
            }
            RPCEventInvoker.SwapExtraRole(PlayerControl.LocalPlayer, shiftPlayer, Roles.SecondaryMadmate, true);
            RPCEventInvoker.SwapRole(PlayerControl.LocalPlayer, shiftPlayer);

            shiftPlayer = null;
        }
    }


    public byte deadBodyId;

    /* 画像 */
    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.ChainShiftButton.png", 115f);

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.chainShifter.help.shift",0.3f)
    };

    public override void Initialize(PlayerControl __instance)
    {
        shiftPlayer = null;
    }

    public override void FinalizeInGame(PlayerControl __instance)
    {
        RPCEventInvoker.ExemptAllTask(__instance.PlayerId);
    }

    public override void CleanUp()
    {
        if (shiftButton != null)
        {
            shiftButton.Destroy();
            shiftButton = null;
        }
    }

    public ChainShifter()
        : base("ChainShifter", "chainShifter", RoleColor, RoleCategory.Neutral, Side.ChainShifter, Side.ChainShifter,
             new HashSet<Side>() { Side.ChainShifter }, new HashSet<Side>() { Side.ChainShifter },
             new HashSet<Patches.EndCondition>() { },
             true, VentPermission.CanNotUse, false, false, false)
    {
    }
}