namespace Nebula.Map.Editors;

class MIRAEditor : MapEditor
{

    public MIRAEditor() : base(1)
    {

    }

    public override void MapCustomize()
    {
        if (CustomOptionHolder.mapOptions.getBool())
        {
            if (CustomOptionHolder.invalidatePrimaryAdmin.getSelection() == 2)
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

    public override void MinimapOptimizeForJailer(Transform romeNames, MapCountOverlay countOverlay, InfectedOverlay infectedOverlay)
    {
        for (int i = 0; i < infectedOverlay.transform.childCount; i++)
            infectedOverlay.transform.GetChild(i).transform.localScale *= 0.8f;


        romeNames.GetChild(2).localPosition += new Vector3(0f, 0.15f, 0f);
        romeNames.GetChild(7).localPosition += new Vector3(0f, 0.15f, 0f);

        infectedOverlay.transform.GetChild(0).localPosition += new Vector3(0f, 0.24f, 0f);
        infectedOverlay.transform.GetChild(1).localPosition += new Vector3(0.45f, 0.5f, 0f);
        infectedOverlay.transform.GetChild(2).localPosition += new Vector3(-0.1f, 0.43f, 0f);
        infectedOverlay.transform.GetChild(3).localPosition += new Vector3(0.6f, 0.25f, 0f);

        countOverlay.transform.GetChild(3).localPosition += new Vector3(0f, -0.2f, 0f);
        countOverlay.transform.GetChild(5).localPosition += new Vector3(0f, -0.6f, 0f);
        countOverlay.transform.GetChild(9).localPosition += new Vector3(0f, -0.45f, 0f);
        countOverlay.transform.GetChild(10).localPosition += new Vector3(0.05f, -0.3f, 0f);

        foreach (var c in countOverlay.CountAreas) c.YOffset *= -1f;

    }
}