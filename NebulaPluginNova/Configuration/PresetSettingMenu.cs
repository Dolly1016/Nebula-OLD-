using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Configuration;

public class PresetSettingMenu : MonoBehaviour
{
    static PresetSettingMenu()
    {
        ClassInjector.RegisterTypeInIl2Cpp<PresetSettingMenu>();
    }
}
