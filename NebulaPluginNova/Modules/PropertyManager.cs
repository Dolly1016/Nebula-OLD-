using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

public interface INebulaProperty
{
    public string GetString();
}

public class NebulaFunctionProperty : INebulaProperty
{
    Func<string> myFunc;
    public NebulaFunctionProperty(string id, Func<string> func)
    {
        myFunc = func;
        PropertyManager.Register(id, this);
    }

    public string GetString() { 
        return myFunc.Invoke();
    }
}

static public class PropertyManager
{
    static Dictionary<string, INebulaProperty> allProperties = new();
    static public void Register(string id, INebulaProperty property)
    {
        allProperties[id] = property;
    }

    static public INebulaProperty? GetProperty(string id) => allProperties.TryGetValue(id, out var property) ? property : null;
}
