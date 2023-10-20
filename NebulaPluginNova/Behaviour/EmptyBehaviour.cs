using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Behaviour;

public class EmptyBehaviour : MonoBehaviour
{
    static EmptyBehaviour() => ClassInjector.RegisterTypeInIl2Cpp<EmptyBehaviour>();
}