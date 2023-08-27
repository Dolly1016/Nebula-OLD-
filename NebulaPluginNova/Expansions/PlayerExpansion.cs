using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Modules;
using UnityEngine.Networking.PlayerConnection;

namespace Nebula.Expansions;

public static class PlayerExpansion
{

    public static void StopAllAnimations(this CosmeticsLayer layer)
    {
        if (layer.skin.animator) layer.skin.animator.Stop();
        if (layer.currentPet.animator)layer.currentPet.animator.Stop();
    }
}
