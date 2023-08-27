using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles.Scripts;

public class Draggable : ScriptHolder
{
    static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.DragAndDropButton.png", 115f);

    public Action<DeadBody>? OnHoldingDeadBody { get; set; } = null;

    public void OnActivated(RoleInstance role)
    {
        if (role.player.AmOwner)
        {
            var deadBodyTracker = Bind(ObjectTrackers.ForDeadBody(1.2f, role.player, (d) => d.GetHolder() == null));

            var dragButton = Bind(new ModAbilityButton()).KeyBind(KeyCode.F);
            dragButton.SetSprite(buttonSprite.GetSprite());
            dragButton.Availability = (button) =>
            {
                return (deadBodyTracker.CurrentTarget != null || role.player.GetModInfo().HoldingDeadBody.HasValue) && role.player.CanMove;
            };
            dragButton.Visibility = (button) => !role.player.Data.IsDead;
            dragButton.OnClick = (button) =>
            {
                if (!role.player.GetModInfo().HoldingDeadBody.HasValue)
                {
                    role.player.GetModInfo().HoldDeadBody(deadBodyTracker.CurrentTarget);
                    OnHoldingDeadBody?.Invoke(deadBodyTracker.CurrentTarget);
                }
                else
                    role.player.GetModInfo().ReleaseDeadBody();
            };
            dragButton.OnUpdate = (button) => dragButton.SetLabel(role.player.GetModInfo().HoldingDeadBody.HasValue ? "release" : "drag");
            dragButton.SetLabelType(ModAbilityButton.LabelType.Standard);
            dragButton.SetLabel("drag");
        }
    }

    public void OnDead(RoleInstance role)
    {
        role.player.GetModInfo()?.ReleaseDeadBody();
    }

    public void OnInactivated(RoleInstance role)
    {
        role.player.GetModInfo()?.ReleaseDeadBody();
    }
}
