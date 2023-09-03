using Nebula.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Patches;

[HarmonyPatch(typeof(ShipStatus),nameof(ShipStatus.Awake))]
public class ShipStatusPatch
{
    static public void Postfix(ShipStatus __instance)
    {
        if (GeneralConfigurations.SilentVentOption.GetBool()!.Value)
        {
            //ベントを見えなくする
            foreach (var vent in ShipStatus.Instance.AllVents)
            {
                GameObject shadowObj = new GameObject("ShadowVent");
                shadowObj.transform.SetParent(vent.transform);
                shadowObj.transform.localPosition = new Vector3(0f, 0f, 0f);
                shadowObj.transform.localScale = new Vector3(1f, 1f, 1f);
                shadowObj.AddComponent<SpriteRenderer>().sprite = vent.GetComponent<SpriteRenderer>().sprite;
                shadowObj.layer = LayerExpansion.GetShadowLayer();

                vent.gameObject.layer = LayerExpansion.GetDefaultLayer();
            }
        }
    }
}
