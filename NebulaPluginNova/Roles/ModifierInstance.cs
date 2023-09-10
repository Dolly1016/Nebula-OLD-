using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Roles;

public abstract class ModifierInstance : AssignableInstance
{
    public abstract AbstractModifier Role { get; }

    public ModifierInstance(PlayerModInfo player) : base(player)
    {
    }

    public virtual void DecorateRoleName(ref string text) { }

    public virtual bool InvalidateCrewmateTask => false;
    public virtual string? IntroText => null;
}
