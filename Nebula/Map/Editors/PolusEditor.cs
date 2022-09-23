using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Editors
{
    class PolusEditor : MapEditor
    {
        public PolusEditor() : base(2)
        {

        }

        public override void MapCustomize()
        {
            if (CustomOptionHolder.mapOptions.getBool())
            {
                if (CustomOptionHolder.invalidatePrimaryAdmin.getSelection() == 2)
                {
                    var obj = ShipStatus.Instance.FastRooms[SystemTypes.Admin].gameObject.transform.FindChild("mapTable").gameObject;
                    //第一のアドミンを無効化
                    GameObject.Destroy(obj.transform.GetChild(0).GetComponent<BoxCollider2D>());
                    GameObject.Destroy(obj.transform.GetChild(1).GetComponent<BoxCollider2D>());
                    GameObject.Destroy(obj.transform.GetChild(2).gameObject);
                }

                if (CustomOptionHolder.useClassicAdmin.getBool())
                {
                    /*
                    PolygonCollider2D collider;
                    List<Vector2> pointsList;

                    //Electrical
                    collider =ShipStatus.Instance.FastRooms[SystemTypes.Electrical].gameObject.GetComponent<PolygonCollider2D>();
                    pointsList = new List<Vector2>(collider.points);
                    pointsList.RemoveAt(4);
                    collider.points = pointsList.ToArray();

                    //Laboratory
                    collider = ShipStatus.Instance.FastRooms[SystemTypes.Laboratory].gameObject.GetComponent<PolygonCollider2D>();
                    pointsList = new List<Vector2>(collider.points);
                    pointsList.RemoveAt(7);
                    collider.points = pointsList.ToArray();
                    */

                    //BoilerRoom
                    ShipStatus.Instance.MapPrefab.countOverlay.transform.FindChild("BoilerRoom").localPosition = new Vector3(100, 0, 0);
                    ShipStatus.Instance.MapPrefab.transform.FindChild("RoomNames").transform.FindChild("BoilerRoom").gameObject.SetActive(false);
                }
            }
        }

        public override void ModifySabotage()
        {
            if (CustomOptionHolder.SabotageOption.getBool())
            {
                ShipStatus.Instance.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>().ReactorDuration = CustomOptionHolder.SeismicStabilizersTimeLimitOption.getFloat();
            }
        }
    }
}
