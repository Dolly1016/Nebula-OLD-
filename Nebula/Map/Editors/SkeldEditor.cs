using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Editors
{
    class SkeldEditor : MapEditor
    {
        public SkeldEditor() : base(0)
        {
        }

        public override void AddVents()
        {
            CreateVent(SystemTypes.Cafeteria,"CafeUpperVent", new UnityEngine.Vector2(-2.1f, 3.8f));
            CreateVent(SystemTypes.Storage,"StorageVent", new UnityEngine.Vector2(0.45f, -3.6f));
        }

        public override void MapCustomize()
        {
            if (CustomOptionHolder.mapOptions.getBool())
            {
                if (CustomOptionHolder.invalidatePrimaryAdmin.getSelection() == 2)
                {
                    var obj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject.transform.parent.GetChild(0).GetChild(3).gameObject;
                    //第一のアドミンを無効化
                    GameObject.Destroy(obj.transform.GetChild(1).GetComponent<CircleCollider2D>());
                }
            }
        }

        public override void ModifySabotage()
        {
            if (CustomOptionHolder.SabotageOption.getBool())
            {
                ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().LifeSuppDuration = CustomOptionHolder.SkeldO2TimeLimitOption.getFloat();
                ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().ReactorDuration = CustomOptionHolder.SkeldReactorTimeLimitOption.getFloat();
            }

           // RPCEventInvoker.ObjectInstantiate(Objects.CustomObject.Type.Diamond, new Vector3(3, 0, 0));
        }
    }
}
