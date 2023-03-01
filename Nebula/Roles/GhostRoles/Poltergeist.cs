namespace Nebula.Roles.GhostRoles;

public class Poltergeist : GhostRole
{
    public class PoltergeistEvent : Events.LocalEvent
    {
        DeadBody? deadBody = null;
        Vector2 vector;
        float mag;

        public PoltergeistEvent(byte deadBodyId, Vector2 vector) : base(1f)
        {
            foreach (var body in Helpers.AllDeadBodies())
            {
                if (body.ParentId == deadBodyId)
                {
                    deadBody = body;
                    break;
                }
            }
            this.vector = vector.normalized;
            this.mag = vector.magnitude;
        }

        public override void LocalUpdate()
        {
            if (deadBody == null || !deadBody) return;

            if (PhysicsHelpers.AnyNonTriggersBetween(deadBody.transform.position, vector, mag * duration * Time.deltaTime, Constants.ShipAndAllObjectsMask)) return;

            deadBody.transform.position += (Vector3)(vector * (mag * duration * Time.deltaTime));
            var pos = deadBody.transform.position;
            deadBody.transform.position = new Vector3(pos.x, pos.y, pos.y / 1000f);
        }
    }

    static public Color RoleColor = new Color(210f / 255f, 220f / 255f, 234f / 255f);

    private Module.CustomOption poltergeistCoolDownOption;
    private Module.CustomOption hasCrewmateTaskOption;
    public override void LoadOptionData()
    {
        poltergeistCoolDownOption = CreateOption(Color.white, "poltergeistCoolDown", 30f, 10f, 100f, 5f);
        poltergeistCoolDownOption.suffix = "second";

        hasCrewmateTaskOption = CreateOption(Color.white, "hasCrewmateTask", false);
    }

    public override bool IsAssignableTo(Game.PlayerData player)
    {
        return player.role.side == Side.Crewmate;
    }

    CustomButton poltergeistButton;
    SpriteLoader buttonSprite = new SpriteLoader("Nebula.Resources.PoltergeistButton.png", 115f, "ui.button.poltergeist.poltergeist");

    public override HelpSprite[] helpSprite => new HelpSprite[]
     {
            new HelpSprite(buttonSprite,"role.poltergeist.help.poltergeist",0.3f)
     };

    public override void ButtonInitialize(HudManager __instance)
    {
        if (poltergeistButton != null)
        {
            poltergeistButton.Destroy();
        }
        poltergeistButton = new CustomButton(
            () =>
            {
                RPCEventInvoker.Poltergeist(deadBody.ParentId, bodyVector);
                deadBody = null;
                poltergeistButton.Timer = poltergeistButton.MaxTimer;
            },
            () => { return PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                return deadBody != null;
            },
            () => { poltergeistButton.Timer = poltergeistButton.MaxTimer; },
            buttonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.poltergeist"
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        poltergeistButton.MaxTimer = poltergeistCoolDownOption.getFloat();
    }

    public override void CleanUp()
    {
        if (poltergeistButton != null)
        {
            poltergeistButton.Destroy();
            poltergeistButton = null;
        }
    }

    private DeadBody? deadBody;
    private Vector2 bodyVector;

    public override void MyPlayerControlUpdate()
    {
        if (poltergeistButton == null) return;

        if (Game.GameData.data.myData.getGlobalData() == null) return;

        DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget(3f);

        if (body != null)
        {
            deadBody = body;
            bodyVector = (Vector2)PlayerControl.LocalPlayer.transform.position - (Vector2)deadBody.transform.position;
            bodyVector *= 1.85f;

            Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
        }
        else
        {
            deadBody = null;
        }
    }

    public override bool HasCrewmateTask(byte playerId) => hasCrewmateTaskOption.getBool();

    public Poltergeist() : base("Poltergeist", "poltergeist", RoleColor)
    {

    }
}
