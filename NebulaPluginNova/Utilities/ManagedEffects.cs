using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public static class ManagedEffects
{
    static public IEnumerator ToCoroutine(this Action action)
    {
        action.Invoke();
        yield break;
    }

    static public IEnumerator Action(Action action)
    {
        action.Invoke();
        yield break;
    }
}
