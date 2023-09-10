using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public static class NebulaPhysicsHelpers
{
    public static bool AnyNonTriggersBetween(Vector2 source, Vector2 dirNorm, float mag, int layerMask, out float distance)
    {
        int num = Physics2D.RaycastNonAlloc(source, dirNorm, PhysicsHelpers.castHits, mag, layerMask);
        bool result = false;
        distance = mag;
        for (int i = 0; i < num; i++)
        {
            if (!PhysicsHelpers.castHits[i].collider.isTrigger)
            {
                result = true;

                float d = source.Distance(PhysicsHelpers.castHits[i].point);
                if (d < distance) distance = d;
            }
        }
        return result;
    }
}
