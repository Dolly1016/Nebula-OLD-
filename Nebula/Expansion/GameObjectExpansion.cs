using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Expansion;

public static class GameObjectExpansion
{
    static public PassiveButton SetUpButton(this GameObject obj,Action? onClicked)
    {
        var button = obj.AddComponent<PassiveButton>();
        button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        button.OnClick.RemoveAllListeners();
        if (onClicked != null) button.OnClick.AddListener(onClicked);
        return button;
    }
}
