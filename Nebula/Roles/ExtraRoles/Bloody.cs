namespace Nebula.Roles.ExtraRoles;

public class Bloody : Template.StandardExtraRole
{
    public class BloodyEvent : Events.LocalEvent
    {
        int num;
        float timer;

        public BloodyEvent(float duration) : base(duration)
        {
            num = 0;
        }

        private void GenerateFootprint()
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return;

            if (PlayerControl.LocalPlayer.MyPhysics.Velocity.magnitude > 0)
            {
                //歩いているように血の足跡

                var vec = PlayerControl.LocalPlayer.MyPhysics.Velocity.normalized * 0.08f;

                if (num % 2 == 0) vec *= -1f;

                RPCEventInvoker.ObjectInstantiate(Objects.CustomObject.Type.Footprint, PlayerControl.LocalPlayer.transform.position + new Vector3(-vec.y, vec.x - 0.22f));
            }
            else
            {
                //動いてない場合、中央に血の足跡
                RPCEventInvoker.ObjectInstantiate(Objects.CustomObject.Type.Footprint, PlayerControl.LocalPlayer.transform.position + new Vector3(0, -0.22f));
            }
            num++;
        }

        public override void LocalUpdate()
        {
            timer += Time.deltaTime;
            if (timer > 0.2f)
            {
                GenerateFootprint();
                timer = 0f;
            }
        }
    }

    static public Color RoleColor = new Color(180f / 255f, 0f / 255f, 0f / 255f);

    private Module.CustomOption bloodyDurationOption;

    public override void GlobalInitialize(PlayerControl __instance)
    {
        base.GlobalInitialize(__instance);
    }

    public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
    {
        bool showFlag = false;
        if (playerId == PlayerControl.LocalPlayer.PlayerId || Game.GameData.data.myData.CanSeeEveryoneInfo) showFlag = true;

        if (showFlag) EditDisplayNameForcely(playerId, ref displayName);
    }


    public override void EditDisplayNameForcely(byte playerId, ref string displayName)
    {
        displayName += Helpers.cs(
                RoleColor, "†");
    }

    public override void LoadOptionData()
    {
        base.LoadOptionData();

        bloodyDurationOption = CreateOption(Color.white, "bloodyDuration", 4f, 1f, 10f, 1f);
        bloodyDurationOption.suffix = "second";
    }

    public override void OnDied(byte playerId)
    {
        if (MeetingHud.Instance) return;
        if (Game.GameData.data.deadPlayers[playerId].MurderId != PlayerControl.LocalPlayer.PlayerId) return;

        //自殺の場合は何もしない(Busker対策)
        if (playerId == PlayerControl.LocalPlayer.PlayerId) return;

        Events.LocalEvent.Activate(new BloodyEvent(bloodyDurationOption.getFloat()));
        Helpers.PlayQuickFlash(Palette.ImpostorRed);
    }

    public override void EditSpawnableRoleShower(ref string suffix, Role role)
    {
        if (IsSpawnable() && role.CanHaveExtraAssignable(this)) suffix += Helpers.cs(Color, "†");
    }

    public override Module.CustomOption? RegisterAssignableOption(Role role)
    {
        Module.CustomOption option = role.CreateOption(new Color(0.8f, 0.95f, 1f), "option.canBeBloody", role.CanHaveExtraAssignable(this), true).HiddenOnDisplay(true).SetIdentifier("role." + role.LocalizeName + ".canBeBloody");
        option.AddPrerequisite(CustomOptionHolder.advanceRoleOptions);
        option.AddCustomPrerequisite(() => { return Roles.Bloody.IsSpawnable(); });
        return option;
    }

    public Bloody() : base("Bloody", "bloody", RoleColor, 0)
    {
    }
}