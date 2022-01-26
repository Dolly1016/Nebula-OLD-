using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Events.Variation
{
    public class EMI : GlobalEvent
    {
        public EMI(float duration) : base(GlobalEvent.Type.EMI, duration)
        {
        }
    }
}
