using Il2CppSystem.Reflection.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Utilities;

public static class Helpers
{
    public static PlayerControl? GetPlayer(byte? id)
    {
        if (!id.HasValue) return null;
        return PlayerControl.AllPlayerControls.Find((Il2CppSystem.Predicate<PlayerControl>)((p) => p.PlayerId == id!));
    }

    public static DeadBody? GetDeadBody(byte id)
    {
        return AllDeadBodies().FirstOrDefault((p) => p.ParentId == id);
    }

    public static int ComputeConstantHash(this string str)
    {
        const int MulPrime = 127;
        const int SurPrime = 104729;

        int val = 0;
        int mul = 1;
        foreach (char c in str)
        {
            mul *= MulPrime;
            mul %= SurPrime;
            val += (int)c * mul;
            val %= SurPrime;
        }
        return val;
    }

    public static DeadBody[] AllDeadBodies()
    {
        //Componentで探すよりタグで探す方が相当はやい
        var bodies = GameObject.FindGameObjectsWithTag("DeadBody");
        DeadBody[] deadBodies = new DeadBody[bodies.Count];
        for (int i = 0; i < bodies.Count; i++) deadBodies[i] = bodies[i].GetComponent<DeadBody>();
        return deadBodies;
    }

    public static int[] GetRandomArray(int length)
    {
        var array = new int[length];
        for (int i = 0; i < length; i++) array[i] = i;
        return array.OrderBy(i => Guid.NewGuid()).ToArray();
    }
    
}
