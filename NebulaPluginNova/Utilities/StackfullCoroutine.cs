using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public class StackfullCoroutine
{
    private List<IEnumerator> stack = new();

    public StackfullCoroutine(IEnumerator enumerator)
    {
        stack.Add(enumerator);
    }

    public bool MoveNext() {
        if (stack.Count == 0) return false;

        var current = stack[stack.Count - 1];
        if (!current.MoveNext())
            stack.RemoveAt(stack.Count - 1);
        else if (current.Current != null && current.Current is IEnumerator child)
            stack.Add(child);

        return stack.Count > 0;
    }

}
