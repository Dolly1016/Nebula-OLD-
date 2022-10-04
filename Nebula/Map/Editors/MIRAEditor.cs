using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Editors
{
    class MIRAEditor : MapEditor
    {

        public MIRAEditor() : base(1)
        {

        }

        public override void MapCustomize()
        {
            if (CustomOptionHolder.mapOptions.getBool())
            {
                if (CustomOptionHolder.invalidatePrimaryAdmin.getSelection()==2)
                {
                    var obj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject.transform.FindChild("MapTable").gameObject;
                    //第一のアドミンを無効化
                    GameObject.Destroy(obj.transform.GetChild(0).gameObject);
                }
            }
        }

        public override void ModifySabotage()
        {
            if (CustomOptionHolder.SabotageOption.getBool())
            {
                ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().LifeSuppDuration = CustomOptionHolder.MIRAO2TimeLimitOption.getFloat();
                ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().ReactorDuration = CustomOptionHolder.MIRAReactorTimeLimitOption.getFloat();
            }
        }
    }
}
