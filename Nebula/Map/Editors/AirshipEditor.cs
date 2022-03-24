using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Map.Editors
{
    public class AirshipEditor : MapEditor
    {
        public AirshipEditor():base(4)
        {
        }

        public override void AddWirings()
        {
            ActivateWiring("task_wiresHallway2", 2);
            ActivateWiring("task_electricalside2", 3).Room=SystemTypes.Armory;
            ActivateWiring("task_wireShower", 4);
            ActivateWiring("taks_wiresLounge", 5);
            ActivateWiring("panel_wireHallwayL", 6);
            ActivateWiring("task_wiresStorage", 7);
            ActivateWiring("task_electricalSide", 8).Room = SystemTypes.VaultRoom;
            ActivateWiring("task_wiresMeeting", 9);

        }
    }
}
