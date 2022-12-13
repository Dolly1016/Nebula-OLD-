namespace Nebula.Roles.CrewmateRoles;

public class Necromancer : Template.Draggable
{
    static public Color RoleColor = new Color(110f / 255f, 51f / 255f, 163f / 255f);

    private CustomButton reviveButton;

    private Module.CustomOption reviveCoolDownOption;
    private Module.CustomOption reviveDurationOption;
    public Module.CustomOption maxReviveRoomDistanceOption;
    private Module.CustomOption maxNotificationDistanceOption;

    private SpriteLoader reviveButtonSprite = new SpriteLoader("Nebula.Resources.ReviveButton.png", 115f);
    public override HelpSprite[] helpSprite => new HelpSprite[]
    {
            new HelpSprite(DaDSprite,"role.necromancer.help.dragAndDrop",0.3f),
            new HelpSprite(reviveButtonSprite,"role.necromancer.help.revive",0.3f)
    };

    public Arrow reviveArrow;
    public SystemTypes targetRoom;

    private SpriteRenderer FS_BodiesSensor = null;

    private void UpdateFullScreen()
    {
        if (!PlayerControl.LocalPlayer) return;
        if (PlayerControl.LocalPlayer.GetModData() == null) return;

        if (FS_BodiesSensor == null)
        {
            FS_BodiesSensor = GameObject.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
            FS_BodiesSensor.color = Palette.ClearWhite;
            FS_BodiesSensor.enabled = true;
            FS_BodiesSensor.gameObject.SetActive(true);
        }



        float a = FS_BodiesSensor.color.a;
        var center = PlayerControl.LocalPlayer.transform.position;
        bool flag = false;
        if (!PlayerControl.LocalPlayer.Data.IsDead)
        {
            foreach (var body in Helpers.AllDeadBodies())
            {
                float dis = body.transform.position.Distance(center);
                if (dis > 2f && dis < maxNotificationDistanceOption.getFloat())
                {
                    flag = true;
                    break;
                }
            }
        }

        a += (flag ? 0.8f : -0.8f) * Time.deltaTime;
        FS_BodiesSensor.color = Color.AlphaMultiplied(Mathf.Clamp01(a) * 0.9f);

    }

    public override void LoadOptionData()
    {
        reviveCoolDownOption = CreateOption(Color.white, "reviveCoolDown", 25f, 10f, 60f, 5f);
        reviveCoolDownOption.suffix = "second";

        reviveDurationOption = CreateOption(Color.white, "reviveDuration", 5f, 1f, 10f, 1f);
        reviveDurationOption.suffix = "second";

        maxReviveRoomDistanceOption = CreateOption(Color.white, "maxReviveRoomDistance", 25f, 5f, 40f, 2.5f);
        maxReviveRoomDistanceOption.suffix = "cross";

        maxNotificationDistanceOption = CreateOption(Color.white, "maxNotificationDistance", 20f, 5f, 40f, 2.5f);
        maxNotificationDistanceOption.suffix = "cross";
    }

    public override void OnDropPlayer()
    {
        CleanArrow();
    }

    public override void OnDragPlayer(byte playerId)
    {
        string room = Language.Language.GetString("role.necromancer.room." + GetReviveRooomType(playerId).ToString());
        string message = Language.Language.GetString("role.necromancer.phantomMessage").Replace("%ROOM%", room);

        Action<float> createMessageAction = null;
        createMessageAction = new Action<float>((p) =>
         {
             if (p == 0f)
             {
                 CustomMessage.Create(new Vector3(0, 1f, 0), false, message, 0.5f, 0.4f, 0.7f, 1.0f, RoleColor);
             }
             if (p == 1f && Game.GameData.data.myData.getGlobalData().dragPlayerId == playerId)
             {
                 FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1.6f, createMessageAction));
             }
         });

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1.6f, createMessageAction));

        SpawnArrow(GetReviveRooomType(playerId));
    }

    private SystemTypes GetReviveRooomType(byte playerId)
    {

        return Game.GameData.data.deadPlayers[playerId].RespawnRoom;
    }

    private bool DeadBodyIsInReviveRoom(DeadBody deadBody)
    {
        try
        {
            SystemTypes roomType = Game.GameData.data.deadPlayers[deadBody.ParentId].RespawnRoom;
            PlainShipRoom room = ShipStatus.Instance.FastRooms[roomType];
            return room.roomArea.OverlapPoint(deadBody.myCollider.transform.position);
        }
        catch (KeyNotFoundException excep) { return false; }
    }

    private bool DraggingPlayerIsInReviveRoom()
    {
        byte id = PlayerControl.LocalPlayer.GetModData().dragPlayerId;
        if (id == byte.MaxValue) return false;
        foreach (DeadBody body in Helpers.AllDeadBodies())
        {
            if (body.ParentId == id)
            {
                return DeadBodyIsInReviveRoom(body);
            }
        }
        return false;
    }

    public override void ButtonInitialize(HudManager __instance)
    {
        base.ButtonInitialize(__instance);

        if (reviveButton != null)
        {
            reviveButton.Destroy();
        }
        reviveButton = new CustomButton(
            () => { },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () =>
            {
                if (reviveButton.isEffectActive && !DraggingPlayerIsInReviveRoom())
                {
                    reviveButton.Timer = 0f;
                    reviveButton.isEffectActive = false;
                }
                return PlayerControl.LocalPlayer.CanMove && DraggingPlayerIsInReviveRoom();
            },
            () =>
            {
                reviveButton.Timer = reviveButton.MaxTimer;
                reviveButton.isEffectActive = false;
            },
            reviveButtonSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None, 
            __instance,
            Module.NebulaInputManager.secondaryAbilityInput.keyCode,
            true,
            reviveDurationOption.getFloat(),
            () =>
            {
                if (!DraggingPlayerIsInReviveRoom()) return;

                CleanArrow();
                RPCEventInvoker.RevivePlayer(Helpers.playerById(PlayerControl.LocalPlayer.GetModData().dragPlayerId));
                if (PlayerControl.LocalPlayer.IsMadmate())
                {
                    RPCEventInvoker.ImmediatelyChangeRole(Helpers.playerById(PlayerControl.LocalPlayer.GetModData().dragPlayerId), Roles.Madmate);
                }
            },
            "button.label.revive",
            ImageNames.VitalsButton
        ).SetTimer(CustomOptionHolder.InitialAbilityCoolDownOption.getFloat());
        reviveButton.MaxTimer = reviveCoolDownOption.getFloat();
    }

    public override void OnMeetingEnd()
    {
        CleanArrow();
    }

    public override void CleanUp()
    {
        base.CleanUp();

        if (reviveButton != null)
        {
            reviveButton.Destroy();
            reviveButton = null;
        }

        if (FS_BodiesSensor != null)
        {
            GameObject.Destroy(FS_BodiesSensor.gameObject);
            FS_BodiesSensor = null;
        }

        CleanArrow();
    }

    private void CleanArrow()
    {
        if (reviveArrow != null)
        {
            UnityEngine.Object.Destroy(reviveArrow.arrow);
            reviveArrow = null;
        }
    }

    private void SpawnArrow(SystemTypes roomType)
    {
        CleanArrow();

        reviveArrow = new Arrow(Color.cyan);
        reviveArrow.arrow.SetActive(true);
        reviveArrow.Update(ShipStatus.Instance.FastRooms[roomType].roomArea.transform.position);
        targetRoom = roomType;
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        if (reviveArrow != null)
        {
            reviveArrow.Update(ShipStatus.Instance.FastRooms[targetRoom].roomArea.transform.position);
        }

        UpdateFullScreen();
    }

    public override void OnRoleRelationSetting()
    {
        RelatedRoles.Add(Roles.Jester);
        RelatedRoles.Add(Roles.Reaper);
    }

    public Necromancer()
        : base("Necromancer", "necromancer", RoleColor, RoleCategory.Crewmate, Side.Crewmate, Side.Crewmate,
             CrewmateRoles.Crewmate.crewmateSideSet, CrewmateRoles.Crewmate.crewmateSideSet,
             CrewmateRoles.Crewmate.crewmateEndSet,
             false, VentPermission.CanNotUse, false, false, false)
    {
        reviveArrow = null;
    }
}