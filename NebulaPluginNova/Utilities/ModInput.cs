using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public class NebulaInput
{
    private static bool SomeUiIsActive => ControllerManager.Instance && ControllerManager.Instance.IsUiControllerActive;

    public static bool GetKeyDown(KeyCode keyCode)
    {
        if (SomeUiIsActive) return false;
        return Input.GetKeyDown(keyCode);
    }

    public static bool GetKeyUp(KeyCode keyCode)
    {
        if (SomeUiIsActive) return true;
        return Input.GetKeyUp(keyCode);
    }

    public static bool GetKey(KeyCode keyCode)
    {
        if (SomeUiIsActive) return false;
        return Input.GetKey(keyCode);
    }

    
}
