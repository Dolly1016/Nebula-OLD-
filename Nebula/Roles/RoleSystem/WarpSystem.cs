using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.RoleSystem
{
    static public class WarpSystem
    {
        static private PlayerControl? SearchPlayer(float distance, float angle)
        {
            Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
            float lightAngle = PlayerControl.LocalPlayer.lightSource.GetFlashlightAngle();

            PlayerControl? result = null;
            float resultNum = 0f;

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (p.Data.IsDead) continue;

                Vector3 dis = (p.transform.position - myPos);
                float mag = dis.magnitude;
                float ang = Mathf.Abs(Mathf.Atan2(dis.y, dis.x) - lightAngle);

                while (ang > Mathf.PI * 2f) ang -= Mathf.PI * 2f;

                if (mag < distance && ang < angle && (result == null || ang + Mathf.Abs(mag - distance) < resultNum))
                {
                    result = p;
                    resultNum = ang + Mathf.Abs(mag - distance);
                }
            }
            return result;
        }

        static public IEnumerator CoOrient(LightSource light, float preDuration, float duration, Action<float> inSerchFunc, Action finalFunc)
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

            Game.GameData.data.myData.Vision.Register(new Game.VisionFactor(duration, 1.15f));

            while (t < duration)
            {
                float p = t / duration;
                float invp = 1f - p;
                Patches.LightPatch.PlayerRadius = dis * ((1f - invp * invp) * 1.2f - 0.3f);
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 1f + p * 12f;
                light.flashlightSize = p * p * 0.15f;

                t += Time.deltaTime;

                inSerchFunc(p);

                yield return null;
            }

            finalFunc();

            if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 5f;
            light.SetFlashlightEnabled(false);
            Patches.LightPatch.ClairvoyanceFlag = false;

            t = 0f;
            while (t < 0.15f)
            {
                float p = t / 0.15f;
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 2f + (1f - (1 - p) * (1 - p) * (1 - p)) * 3f;

                t += Time.deltaTime;

                yield return null;
            }
        }

        static public IEnumerator CoOrient(LightSource light, float preDuration, float duration, Action<PlayerControl?> nearbyPlayerFunc, Action<PlayerControl?> finalPlayerFunc)
        => CoOrient(light, preDuration, duration, 
            (p) =>nearbyPlayerFunc(SearchPlayer(3f + p * 2.5f, 0.1f + p * 0.2f)),
            ()=> finalPlayerFunc(SearchPlayer(4f, 0.3f)));
    }
}
