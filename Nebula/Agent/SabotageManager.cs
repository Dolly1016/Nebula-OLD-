namespace Nebula.Agent;

static public class SabotageManager
{
    static public bool ExistAnySabotages()
    {
        return Helpers.SabotageIsActive();
    }

    static public void BeginSabotage(SystemTypes room)
    {
        switch (room)
        {
            case SystemTypes.Reactor:
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 3);
                break;
            case SystemTypes.Comms:
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 14);
                break;
            case SystemTypes.LifeSupp:
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 8);
                break;
            case SystemTypes.Electrical:
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 7);
                break;
            case SystemTypes.GapRoom:
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 21);
                break;
        }
    }

    static public void BeginReactorSabotage()
    {
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 3);
    }

    static public void BeginCommsSabotage()
    {
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 14);
    }

    static public void BeginOxygenSabotage()
    {
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 8);
    }

    static public void BeginLightsSabotage()
    {
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 7);
    }

    static public void BeginSeismicSabotage()
    {
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Sabotage, 21);
    }

    static public void BeginDoorSabotage(SystemTypes room)
    {
        NebulaPlugin.Instance.Logger.Print("Close " + room + "'s Door");
        ShipStatus.Instance.RpcCloseDoorsOfType(room);
    }
}
