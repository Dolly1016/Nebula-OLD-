using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Events
{
    class Events
    {
        static public void Load()
        {
            GlobalEvent.Register(GlobalEvent.Type.Camouflage,duration => { return new Variation.Camouflage(duration); });
            GlobalEvent.Register(GlobalEvent.Type.EMI, duration => { return new Variation.EMI(duration); });
        }
        
    }
}
