using Nebula.Patches;
using Nebula.Game;

namespace Nebula.Roles.NeutralRoles;

/*
public class SantaClaus : Role
{
    public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
    {
        var data = PlayerControl.LocalPlayer.GetModData();
        if (data.role == Roles.BlackSanta || data.HasExtraRole(Roles.TeamSanta))
        {
            displayColor = RoleColor;
        }
    }


    static public Color RoleColor = new Color(255f / 255f, 102f / 255f, 102f / 255f);

    private Module.CustomOption isGuessableOption;
    public Module.CustomOption killCoolDownOption;
    private Module.CustomOption tasksPerPresentOption;
    private Module.CustomOption maxPresentsOption;
    private Module.CustomOption occupyDoubleRoleCountOption;

    public int leftPresentDataId;
    public int leftTaskSetDataId;

    public override Role[] AssignedRoles => new Role[] { this, Roles.BlackSanta };

    public override void LoadOptionData()
    {
        TopOption.name = "role.teamSanta.name";

        isGuessableOption = CreateOption(Color.white, "santaClausIsGuessable", false);
        tasksPerPresentOption = CreateOption(Color.white, "tasksPerPresent", 3f, 1f, 6f, 1f);
        maxPresentsOption = CreateOption(Color.white, "maxPresents", 2f, 0f, 5f, 1f);
        killCoolDownOption = CreateOption(Color.white, "killCoolDown", 25f, 10f, 60f, 2.5f);
        killCoolDownOption.suffix = "second";
        occupyDoubleRoleCountOption = CreateOption(Color.white, "occupyDoubleRoleCount", false);
    }

    public override int AssignmentCost => occupyDoubleRoleCountOption.getBool() ? 2 : 1;
    public override bool IsGuessableRole { get => isGuessableOption.getBool(); protected set => base.IsGuessableRole = value; }

    public override void OnSetTasks(ref List<GameData.TaskInfo> initialTasks, ref List<GameData.TaskInfo>? actualTasks)
    {
        actualTasks = Helpers.GetRandomTaskList((int)tasksPerPresentOption.getFloat(), 0.4);
    }

    public override void OnTaskComplete()
    {
        var task = Game.GameData.data.myData.getGlobalData().Tasks;
        if (task.Completed == task.AllTasks)
        {
            RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftPresentDataId, 1);
            if (Game.GameData.data.myData.getGlobalData().GetRoleData(leftTaskSetDataId) > 0)
            {
                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftTaskSetDataId, -1);
                RPCEventInvoker.ChangeTasks(Helpers.GetRandomTaskList((int)tasksPerPresentOption.getFloat(), 0.4), true);
            }
        }
    }

    public override void Initialize(PlayerControl __instance)
    {
        RPCEventInvoker.UpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftPresentDataId, 0);
        RPCEventInvoker.UpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftTaskSetDataId, (int)maxPresentsOption.getFloat() - 1);
    }

    private CustomButton? santaButton = null;
    public override void ButtonInitialize(HudManager __instance)
    {

        if (santaButton != null)
        {
            santaButton.Destroy();
        }
        santaButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.AddExtraRole(Game.GameData.data.myData.currentTarget, Roles.TeamSanta, 0);
                RPCEventInvoker.AddAndUpdateRoleData(PlayerControl.LocalPlayer.PlayerId, leftPresentDataId, -1);
                Game.GameData.data.myData.currentTarget = null;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                int leftPresents = Game.GameData.data.myData.getGlobalData().GetRoleData(leftPresentDataId);
                santaButton.UpperText.text = "Left: " + leftPresents;
                return Game.GameData.data.myData.currentTarget != null && leftPresents > 0 && PlayerControl.LocalPlayer.CanMove;
            },
            () => { santaButton.Timer = santaButton.MaxTimer; },
            buttonSprite.GetSprite(),
            new Vector3(-1.8f, 0, 0),
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            false,
            "button.label.present"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        santaButton.MaxTimer = 10.0f;
    }

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget((p) =>
        {
            var data = p.GetModData();
            if (data.role.side == Side.SantaClaus) return false;
            if (data.HasExtraRole(Roles.TeamSanta)) return false;
            return true;
        });
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
    }

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.SantaButton.png", 115f);

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.santaClaus.help.santa",0.3f)
    };

    public override void CleanUp()
    {
        if (santaButton != null)
        {
            santaButton.Destroy();
            santaButton = null;
        }
    }

    public override int GetCustomRoleCount() => 1;
    public override bool HasExecutableFakeTask(byte playerId) => true;

    public SantaClaus()
        : base("SantaClaus", "santaClaus", RoleColor, RoleCategory.Neutral, Side.SantaClaus, Side.SantaClaus,
             new HashSet<Side>() { Side.SantaClaus }, new HashSet<Side>() { Side.SantaClaus },
             new HashSet<Patches.EndCondition>() { EndCondition.SantaWin },
             true, VentPermission.CanNotUse, false, false, false)
    {
        FixedRoleCount = true;
        leftPresentDataId = Game.GameData.RegisterRoleDataId("santaClaus.leftPresents");
        leftTaskSetDataId = Game.GameData.RegisterRoleDataId("santaClaus.leftTaskSets");
    }
}

public class BlackSanta : Role
{
    static public Color RoleColor = new Color(80f / 255f, 93f / 255f, 100f / 255f);

    public override bool IsGuessableRole => Roles.SantaClaus.IsGuessableRole;

    public override void EditDisplayNameColor(byte playerId, ref Color displayColor)
    {
        if (PlayerControl.LocalPlayer.GetModData().role == Roles.SantaClaus)
        {
            displayColor = RoleColor;
        }
    }

    private DeadBody? targetBody = null;
    private byte capturedBodyId = byte.MaxValue;

    private CustomButton? santaButton = null;
    private CustomButton? killButton = null;
    public override void ButtonInitialize(HudManager __instance)
    {
        capturedBodyId = byte.MaxValue;

        if (santaButton != null)
        {
            santaButton.Destroy();
        }
        santaButton = new CustomButton(
            () =>
            {
                if (capturedBodyId != byte.MaxValue)
                {
                    RPCEventInvoker.InstantiateDeadBody(capturedBodyId, PlayerControl.LocalPlayer.transform.position);

                }

                if (targetBody == null)
                {
                    capturedBodyId = byte.MaxValue;
                }
                else
                {
                    capturedBodyId = targetBody.ParentId;
                    RPCEventInvoker.CleanDeadBody(capturedBodyId);
                    if (killButton.Timer < 5f) killButton.Timer = 5f;
                }

                santaButton.Timer = santaButton.MaxTimer;
                targetBody = null;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (capturedBodyId == byte.MaxValue)
                {
                    santaButton.SetLabel("button.label.capture");
                    santaButton.Sprite = buttonSprite.GetSprite();
                }
                else if (targetBody == null)
                {
                    santaButton.SetLabel("button.label.release");
                    santaButton.Sprite = buttonDeadBodySprite.GetSprite();
                }
                else
                {
                    santaButton.SetLabel("button.label.recapture");
                    santaButton.Sprite = buttonDeadBodySprite.GetSprite();
                }
                return (targetBody != null || capturedBodyId != byte.MaxValue) && PlayerControl.LocalPlayer.CanMove;
            },
            () =>
            {
                santaButton.Timer = santaButton.MaxTimer;
                capturedBodyId = byte.MaxValue;
                santaButton.SetLabel("button.label.capture");
            },
            buttonSprite.GetSprite(),
            new Vector3(-1.8f, 0f, 0),
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            false,
            "button.label.capture"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        santaButton.MaxTimer = 10.0f;

        if (killButton != null)
        {
            killButton.Destroy();
        }
        killButton = new CustomButton(
            () =>
            {
                if (santaButton.Timer < 0f && capturedBodyId != byte.MaxValue)
                {
                    RPCEventInvoker.InstantiateDeadBody(capturedBodyId, PlayerControl.LocalPlayer.transform.position);

                    capturedBodyId = Game.GameData.data.myData.currentTarget.PlayerId;
                    Helpers.checkMurderAttemptAndAction(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, () =>
                    {
                        RPCEventInvoker.CloseUpKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, PlayerData.PlayerStatus.Slapped);
                    },
                    () =>
                    {
                        Game.GameData.data.myData.currentTarget.ShowFailedMurder();
                    }, false);
                }
                else
                {
                    Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, Game.GameData.data.myData.currentTarget, PlayerData.PlayerStatus.Slapped);
                }
                santaButton.Timer = santaButton.MaxTimer;
                killButton.Timer = killButton.MaxTimer;
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return Game.GameData.data.myData.currentTarget && PlayerControl.LocalPlayer.CanMove; },
            () => { killButton.Timer = killButton.MaxTimer; },
            __instance.KillButton.graphic.sprite,
            new Vector3(0f, 1f, 0),
            __instance,
            Module.NebulaInputManager.modKillInput.keyCode
        ).SetTimer(CustomOptionHolder.InitialKillCoolDownOption.getFloat());
        killButton.MaxTimer = Roles.SantaClaus.killCoolDownOption.getFloat();
    }

    public override void MyPlayerControlUpdate()
    {
        Game.MyPlayerData data = Game.GameData.data.myData;
        data.currentTarget = Patches.PlayerControlPatch.SetMyTarget((p) =>
        {
            var data = p.GetModData();
            if (data.role.side == Side.SantaClaus) return false;
            if (data.HasExtraRole(Roles.TeamSanta)) return false;
            return true;
        });
        Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);

        targetBody = Patches.PlayerControlPatch.SetMyDeadTarget();
        Patches.PlayerControlPatch.SetDeadBodyOutline(targetBody, Color.yellow);
    }

    private SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.BlackSantaButton.png", 115f);
    private SpriteLoader buttonDeadBodySprite = new SpriteLoader("Nebula.Resources.BlackSantaBodyButton.png", 115f);

    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(buttonSprite,"role.blackSanta.help.capture",0.3f),
            new HelpSprite(buttonDeadBodySprite,"role.blackSanta.help.release",0.3f)
    };

    public override void CleanUp()
    {
        if (santaButton != null)
        {
            santaButton.Destroy();
            santaButton = null;
        }

        if (killButton != null)
        {
            killButton.Destroy();
            killButton = null;
        }
    }

    public BlackSanta()
       : base("BlackSanta", "blackSanta", RoleColor, RoleCategory.Neutral, Side.SantaClaus, Side.SantaClaus,
             new HashSet<Side>() { Side.SantaClaus }, new HashSet<Side>() { Side.SantaClaus },
             new HashSet<Patches.EndCondition>() { EndCondition.SantaWin },
             true, VentPermission.CanUseUnlimittedVent, true, false, true)
    {
        IsHideRole = true;
    }
}
*/
