using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public static class MathHelpers
{
    static public float Distance(this Vector3 myVec, Vector3 vector) {
        var vec = myVec - vector;
        vec.z = 0;
        return vec.magnitude;
    }

    static public float Distance(this Vector2 myVec, Vector2 vector) => (myVec - vector).magnitude;
}
