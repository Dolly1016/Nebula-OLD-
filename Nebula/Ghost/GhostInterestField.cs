using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Ghost
{
    public class GhostInterest
    {
        Vector2 Position;
        float Duration;
        float Magnitude;
    }
    public class GhostInterestField
    {
        HashSet<GhostInterest> InterestsSet;
    }
}
