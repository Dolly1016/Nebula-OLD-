using Il2CppInterop.Runtime.Injection;
using Nebula.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules.CustomMap;

public class ModShipStatus : ShipStatus
{
    static ModShipStatus() => ClassInjector.RegisterTypeInIl2Cpp<ModShipStatus>();
    public ModShipStatus(System.IntPtr ptr) : base(ptr) { }
    public ModShipStatus() : base(ClassInjector.DerivedConstructorPointer<ModShipStatus>())
    { ClassInjector.DerivedConstructorBody(this); }
}
