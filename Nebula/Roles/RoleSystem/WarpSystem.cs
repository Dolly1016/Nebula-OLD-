using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.RoleSystem
{
    static public class WarpSystem
    {
        static private PlayerControl? SearchPlayer(float distance,float angle)
        {
            Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
            float lightAngle = PlayerControl.LocalPlayer.lightSource.GetFlashlightAngle();

            PlayerControl? result = null;
            float resultNum = 0f;

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                Vector3 dis = (p.transform.position - myPos);
                float mag = dis.magnitude;
                float ang = Mathf.Abs(Mathf.Atan2(dis.y, dis.x) / Mathf.PI * 180 - lightAngle);

                while (ang > 360) ang -= 360f;

                if (mag < distance && ang < angle && (result == null || distance + ang / 200f < resultNum))
                {
                    result = p;
                    resultNum = distance + ang / 200f;
                }
            }
            return result;
        }

        static public IEnumerator CoOrient(LightSource light,float preDuration,float duration,Action<PlayerControl?> nearbyPlayerFunc,Action<PlayerControl?> finalPlayerFunc)
        {
            float t;

            t = 0f;
            while (t < preDuration)
            {
                float p = t / preDuration;
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 5f - p * p * 5f;
                
                t += Time.deltaTime;

                yield return null;
            }

            light.SetFlashlightEnabled(true);
            Patches.LightPatch.ClairvoyanceFlag = true;

            if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 5f;


            t = 0f;
            float dis = light.viewDistance;
            while (t < duration)
            {
                float p = t / duration;
                float invp = 1f - p;
                Patches.LightPatch.PlayerRadius = dis * (invp* invp * 0.6f - 0.3f);
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 1f+p * 12f;
                light.flashlightSize = p * p * 0.1f;

                t += Time.deltaTime;

                nearbyPlayerFunc(SearchPlayer(1f + p * 3f, 20 + p * 20f));

                yield return null;
            }

            finalPlayerFunc(SearchPlayer(4f, 40f));

            if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 5f;
            light.SetFlashlightEnabled(false);
            Patches.LightPatch.ClairvoyanceFlag = false;
        }
    }
}
