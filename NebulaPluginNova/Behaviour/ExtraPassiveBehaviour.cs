using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Behaviour;

public class ExtraPassiveBehaviour : MonoBehaviour
{
    private PassiveUiElement myElement = null!;

    public void Start()
    {
        myElement = gameObject.GetComponent<PassiveUiElement>();
    }

    public void Update()
    {
        if(AmongUsUtil.CurrentUiElement == myElement)
        {
            OnPiled?.Invoke();

            if (Input.GetKeyUp(KeyCode.Mouse1)) OnRightClicked?.Invoke();
        }
    }

    public Action? OnPiled;
    public Action? OnRightClicked;
}
