namespace Nebula.Map.Editors;
class PolusEditor : MapEditor
{
    public PolusEditor() : base(2)
    {

    }

    public override void AddVents()
    {
        CreateVent(SystemTypes.Specimens, "SpecimenVent", new UnityEngine.Vector2(-1f, -1.35f));
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

    public override void MinimapOptimizeForJailer(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        for (int i = 0; i < infectedOverlay.transform.childCount; i++)
            infectedOverlay.transform.GetChild(i).transform.localScale *= 0.8f;


        romeNames.GetChild(1).localPosition += new Vector3(0f, 0.35f, 0f);
        romeNames.GetChild(3).localPosition += new Vector3(0f, 0.35f, 0f);
        romeNames.GetChild(7).localPosition += new Vector3(0f, 0.35f, 0f);

        infectedOverlay.transform.GetChild(0).localPosition += new Vector3(0f, 0.4f, 0f);
        //infectedOverlay.transform.GetChild(0).GetChild(1).localPosition += new Vector3(-0.05f, 0f, 0f);
        infectedOverlay.transform.GetChild(1).localPosition += new Vector3(-1f, 0.4f, 0f);
        infectedOverlay.transform.GetChild(3).localPosition += new Vector3(0.6f, 0.3f, 0f);
        infectedOverlay.transform.GetChild(4).localPosition += new Vector3(-0.5f, 0.3f, 0f);
        infectedOverlay.transform.GetChild(5).localPosition += new Vector3(0f, 0.28f, 0f);
        infectedOverlay.transform.GetChild(6).localPosition += new Vector3(0f, 0.4f, 0f);

        countOverlay.transform.GetChild(0).localPosition += new Vector3(0f, 0.1f, 0f);
        countOverlay.transform.GetChild(1).localPosition += new Vector3(0.55f, -0.9f, 0f);
        countOverlay.transform.GetChild(2).localPosition += new Vector3(0f, -0.1f, 0f);
        countOverlay.transform.GetChild(3).localPosition += new Vector3(0f, -0.2f, 0f);
        countOverlay.transform.GetChild(4).localPosition += new Vector3(0.0f, 0f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0.0f, -0.15f, 0f);
        countOverlay.transform.GetChild(6).localPosition += new Vector3(0.0f, -0.15f, 0f);
        countOverlay.transform.GetChild(7).localPosition += new Vector3(0.0f, 0f, 0f);
        countOverlay.transform.GetChild(8).localPosition += new Vector3(0.0f, -0.15f, 0f);
        countOverlay.transform.GetChild(9).localPosition += new Vector3(0f, 0.1f, 0f);
        countOverlay.transform.GetChild(10).localPosition += new Vector3(0f, -0.1f, 0f);

        foreach (var c in countOverlay.CountAreas) c.YOffset *= -1f;

    }

}