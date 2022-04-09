using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Map.Editors
{
    public class AirshipEditor : MapEditor
    {
        public AirshipEditor():base(4)
        {
        }

        private Sprite MedicalWiring;
        private Sprite GetMedicalSprite()
        {
            if (MedicalWiring) return MedicalWiring;
            MedicalWiring = Helpers.loadSpriteFromResources("Nebula.Resources.AirshipWiringM.png", 100f);
            return MedicalWiring;
        }

        public override void AddWirings()
        {
            ActivateWiring("task_wiresHallway2", 2);
            ActivateWiring("task_electricalside2", 3).Room=SystemTypes.Armory;
            ActivateWiring("task_wireShower", 4);
            ActivateWiring("taks_wiresLounge", 5);
            CreateConsole(SystemTypes.Medical, "task_wireMedical", GetMedicalSprite(), new Vector2(-0.84f, 5.63f));
            ActivateWiring("task_wireMedical", 6).Room = SystemTypes.Medical;
            ActivateWiring("panel_wireHallwayL", 7);
            ActivateWiring("task_wiresStorage", 8);
            ActivateWiring("task_electricalSide", 9).Room = SystemTypes.VaultRoom;
            ActivateWiring("task_wiresMeeting", 10);
        }

        public override void FixTasks()
        {
            //宿舎下ダウンロード
            EditConsole(SystemTypes.Engine, "panel_data", (c) => {
                c.checkWalls = true;
                c.usableDistance = 0.9f;
            });

            //写真現像タスク
            EditConsole(SystemTypes.MainHall, "task_developphotos", (c) =>
            {
                c.checkWalls = true;
            });

            //シャワータスク
            EditConsole(SystemTypes.Showers, "task_shower", (c) =>
            {
                c.checkWalls = true;
            });

            //ラウンジゴミ箱タスク
            EditConsole(SystemTypes.Lounge, "task_garbage5", (c) =>
            {
                c.checkWalls = true;
            });
        }
    }
}
