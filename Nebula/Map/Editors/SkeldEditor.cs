namespace Nebula.Map.Editors;

class SkeldEditor : MapEditor
{
    public SkeldEditor() : base(0)
    {
    }

    public override void AddVents()
    {
        CreateVent(SystemTypes.Cafeteria, "CafeUpperVent", new UnityEngine.Vector2(-2.1f, 3.8f));
        CreateVent(SystemTypes.Storage, "StorageVent", new UnityEngine.Vector2(0.45f, -3.6f));
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

    public override void MinimapOptimizeForJailer(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        for (int i = 0; i < infectedOverlay.transform.childCount; i++)
            infectedOverlay.transform.GetChild(i).transform.localScale *= 0.8f;


        romeNames.GetChild(13).localPosition += new Vector3(0f, 0.1f, 0f);

        infectedOverlay.transform.GetChild(4).localPosition += new Vector3(0.07f, 0.2f, 0f);

        countOverlay.transform.GetChild(0).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(1).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(2).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(3).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(4).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(9).localPosition += new Vector3(0f, -0.55f, 0f);
        countOverlay.transform.GetChild(10).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(12).localPosition += new Vector3(0f, -0.75f, 0f);
        countOverlay.transform.GetChild(13).localPosition += new Vector3(0f, -0.42f, 0f);

        foreach (var c in countOverlay.CountAreas) c.YOffset *= -1f;

    }

}