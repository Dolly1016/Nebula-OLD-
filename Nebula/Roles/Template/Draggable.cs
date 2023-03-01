using Hazel;

namespace Nebula.Roles.Template;

public class Draggable : Role
{
    /* ボタン */
    private CustomButton dragButton;
    public override void ButtonInitialize(HudManager __instance)
    {
        if (dragButton != null)
        {
            dragButton.Destroy();
        }
        dragButton = new CustomButton(
            () =>
            {
                Vector2 pos = new Vector2();
                byte target;
                if (Game.GameData.data.myData.getGlobalData().dragPlayerId != Byte.MaxValue)
                {
                    target = Byte.MaxValue;

                    DeadBody? body = Helpers.AllDeadBodies().FirstOrDefault((d) => d.ParentId == Game.GameData.data.myData.getGlobalData().dragPlayerId);
                    if (body != null)
                    {
                        Vector3 playerPos = PlayerControl.LocalPlayer.GetTruePosition();
                        Vector3 deadBodyPos = body.TruePosition;
                        Vector3 diff = (deadBodyPos - playerPos);
                        float d = diff.magnitude;
                        if (PhysicsHelpers.AnythingBetween(playerPos, deadBodyPos, Constants.ShipAndAllObjectsMask, false))
                        {
                            foreach (var ray in PhysicsHelpers.castHits)
                            {
                                float temp = ((Vector3)ray.point - playerPos).magnitude;
                                if (d > temp) d = temp;
                            }

                            d -= 0.15f;
                            if (d < 0f) d = 0f;

                            pos = playerPos + diff.normalized * d;
                        }
                        else
                        {
                            pos = body.transform.position;
                        }
                    }
                    OnDropPlayer();
                }
                else
                {
                    target = (byte)deadBodyId;
                    OnDragPlayer(target);
                }

                MessageWriter dragAndDropWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DragAndDropPlayer, Hazel.SendOption.Reliable, -1);
                dragAndDropWriter.Write(PlayerControl.LocalPlayer.PlayerId);
                dragAndDropWriter.Write(target);
                dragAndDropWriter.Write(pos.x);
                dragAndDropWriter.Write(pos.y);
                AmongUsClient.Instance.FinishRpcImmediately(dragAndDropWriter);
                RPCEvents.DragAndDropPlayer(PlayerControl.LocalPlayer.PlayerId, target, pos);
            },
            () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
            () => { return PlayerControl.LocalPlayer.CanMove && (Game.GameData.data.myData.getGlobalData().dragPlayerId != Byte.MaxValue || deadBodyId != Byte.MaxValue); },
            () => { },
            DaDSprite.GetSprite(),
            Expansion.GridArrangeExpansion.GridArrangeParameter.None,
            __instance,
            Module.NebulaInputManager.abilityInput.keyCode,
            "button.label.drag"
        );

        dragButton.MaxTimer = 0;
        dragButton.Timer = 0;
    }

    [RoleLocalMethod]
    public virtual void OnDragPlayer(byte playerId)
    {
    }

    [RoleLocalMethod]
    public virtual void OnDropPlayer()
    {
    }

    /* 画像 */

    protected SpriteLoader DaDSprite;

    /* 各種変数 */
    public byte deadBodyId;


    public override void MyPlayerControlUpdate()
    {
        if (dragButton == null) return;
        if (Game.GameData.data.myData.getGlobalData() == null) return;

        if (Game.GameData.data.myData.getGlobalData().dragPlayerId == byte.MaxValue)
        {
            dragButton.SetLabel("button.label.drag");
            DeadBody body = Patches.PlayerControlPatch.SetMyDeadTarget();

            if (body != null)
            {
                deadBodyId = body.ParentId;
                Patches.PlayerControlPatch.SetDeadBodyOutline(body, Color.yellow);
            }
            else
            {
                deadBodyId = byte.MaxValue;
            }
        }
        else
        {
            dragButton.SetLabel("button.label.drop");
        }
    }

    public override void CleanUp()
    {
        if (dragButton != null)
        {
            dragButton.Destroy();
            dragButton = null;
        }
    }

    //インポスターはModで操作するFakeTaskは所持していない
    protected Draggable(string name, string localizeName, Color color, RoleCategory category,
        Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
        HashSet<Patches.EndCondition> winReasons,
        bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
        bool ignoreBlackout, bool useImpostorLightRadius) :
        base(name, localizeName, color, category,
            side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
            winReasons,
            hasFakeTask, canUseVents, canMoveInVents,
            ignoreBlackout, useImpostorLightRadius)
    {
        dragButton = null;

        DaDSprite = new SpriteLoader("Nebula.Resources.DragAndDropButton.png", 115f, "ui.button." + localizeName + ".dragAndDrop");
    }
}
