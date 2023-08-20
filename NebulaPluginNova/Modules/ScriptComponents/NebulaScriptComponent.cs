using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules.ScriptComponents;

public abstract class INebulaScriptComponent
{
    public INebulaScriptComponent()
    {
        NebulaGameManager.Instance?.RegisterComponent(this);
    }

    public abstract void Update();
    public virtual void OnReleased() { }
    public virtual bool UpdateWithMyPlayer { get => false; }

}
