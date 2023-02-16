using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.HnSImpostorRoles;

public class Hadar : Impostor
{
    public override bool ShowInHelpWindow => false;
    public Hadar()
            : base("Hadar", "hadar")
    {
        HideKillButtonEvenImpostor = true;
    }
}