using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Map;

public abstract class MapData
{
    abstract protected Vector2[] MapArea { get; }

    public bool CheckMapArea(Vector2 position)
    {
        int num = Physics2D.OverlapCircleNonAlloc(position, 0.23f, PhysicsHelpers.colliderHits, Constants.ShipAndAllObjectsMask);
        if (num > 0) for (int i = 0; i < num; i++) if (!PhysicsHelpers.colliderHits[i].isTrigger) return false;


        Vector2 vector;
        float magnitude;

        foreach (Vector2 p in MapArea)
        {
            vector = p - position;
            magnitude = vector.magnitude;
            if (magnitude > 12.0f) continue;

            if (!PhysicsHelpers.AnyNonTriggersBetween(position, vector.normalized, magnitude, Constants.ShipAndAllObjectsMask)) return true;
        }

        return false;
    }

    static private MapData[] AllMapData = new MapData[] { new SkeldData(), new MiraData(), new PolusData(), null!, new AirshipData() };
    static public MapData GetCurrentMapData() => AllMapData[AmongUsUtil.CurrentMapId];
}
