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

    static public IEnumerator WaitAsCoroutine(this Task task)
    {
        while (!task.IsCompleted) yield return null;
        yield break;
    }

    static public IEnumerator WaitAsCoroutine<T>(this Task<T> task)
    {
        while (!task.IsCompleted) yield return null;
        yield break;
    }
}
