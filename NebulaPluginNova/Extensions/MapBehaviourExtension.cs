using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Extensions;

public static class MapBehaviourExtension
{
    public static bool CanIdentifyImpostors = false;
    public static bool CanIdentifyDeadBodies = false;
    public static bool AffectedByCommSab = true;
    public static Color? MapColor = null;
    public static void InitializeModOption(this MapCountOverlay overlay)
    {
        CanIdentifyImpostors = false;
        AffectedByCommSab = true;
        MapColor = null;
    }

    public static void SetModOption(this MapCountOverlay overlay, bool? canIdentifyImpostors = null, bool? canIdentifyDeadBodies = null, bool? affectedByCommSab = null, Color? mapColor = null)
    {
        if (canIdentifyImpostors.HasValue) CanIdentifyImpostors = canIdentifyImpostors.Value;
        if (canIdentifyDeadBodies.HasValue) CanIdentifyDeadBodies = canIdentifyDeadBodies.Value;
        if (affectedByCommSab.HasValue) AffectedByCommSab = affectedByCommSab.Value;
        if (mapColor.HasValue)
        {
            MapColor = mapColor.Value;
            overlay.BackgroundColor.SetColor(MapBehaviourExtension.MapColor ?? Color.green);
        }
    }

    public static void UpdateCount(this CounterArea counterArea, int cnt, int impostors, int deadBodies)
    {
        while (counterArea.myIcons.Count < cnt)
        {
            PoolableBehavior item = counterArea.pool.Get<PoolableBehavior>();
            counterArea.myIcons.Add(item);
        }
        while (counterArea.myIcons.Count > cnt)
        {
            PoolableBehavior poolableBehavior = counterArea.myIcons[counterArea.myIcons.Count - 1];
            counterArea.myIcons.RemoveAt(counterArea.myIcons.Count - 1);
            poolableBehavior.OwnerPool.Reclaim(poolableBehavior);
        }

        for (int i = 0; i < counterArea.myIcons.Count; i++)
        {
            int num = i % counterArea.MaxColumns;
            int num2 = i / counterArea.MaxColumns;
            float num3 = (float)(Mathf.Min(cnt - num2 * counterArea.MaxColumns, counterArea.MaxColumns) - 1) * counterArea.XOffset / -2f;
            counterArea.myIcons[i].transform.position = counterArea.transform.position + new Vector3(num3 + (float)num * counterArea.XOffset, (float)num2 * counterArea.YOffset, -1f);

            if (impostors > 0)
            {
                impostors--;
                PlayerMaterial.SetColors(Palette.ImpostorRed, counterArea.myIcons[i].GetComponent<SpriteRenderer>());
            }
            else if (deadBodies > 0)
            {
                deadBodies--;
                PlayerMaterial.SetColors(Palette.DisabledGrey, counterArea.myIcons[i].GetComponent<SpriteRenderer>());
            }
            else
            {
                PlayerMaterial.SetColors(new Color(224f / 255f, 255f / 255f, 0f / 255f), counterArea.myIcons[i].GetComponent<SpriteRenderer>());
            }
        }
    }
}
