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

        public override void OptimizeMap() {
            var obj = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].gameObject;
            //インポスターについてのみ影を無効化
            obj.transform.FindChild("Shadow").FindChild("LedgeShadow").GetComponent<OneWayShadows>().IgnoreImpostor = true;

            SpriteRenderer renderer;

            GameObject fance = new GameObject("ModFance");
            fance.layer = LayerMask.NameToLayer("Ship");
            fance.transform.SetParent(obj.transform);
            fance.transform.localPosition = new Vector3(4.2f, 0.15f, 0.5f);
            fance.transform.localScale = new Vector3(1f,1f,1f);
            fance.SetActive(true);
            var Collider = fance.AddComponent<EdgeCollider2D>();
            Collider.points = new Vector2[] { new Vector2(1.5f, -0.2f), new Vector2(-1.5f, -0.2f), new Vector2(-1.5f, 1.5f) };
            Collider.enabled = true;
            renderer = fance.AddComponent<SpriteRenderer>();
            renderer.sprite = Helpers.loadSpriteFromResources("Nebula.Resources.AirshipFance.png", 100f);

            GameObject pole = new GameObject("DownloadPole");
            pole.layer = LayerMask.NameToLayer("Ship");
            pole.transform.SetParent(obj.transform);
            pole.transform.localPosition = new Vector3(4.1f, 0.75f, 0.8f);
            pole.transform.localScale = new Vector3(1f, 1f, 1f);
            renderer = pole.AddComponent<SpriteRenderer>();
            renderer.sprite = Helpers.loadSpriteFromResources("Nebula.Resources.AirshipDownloadG.png", 100f);

            var panel = obj.transform.FindChild("panel_data");
            panel.localPosition = new Vector3(4.1f, 0.72f, 0.1f);
            panel.gameObject.GetComponent<Console>().usableDistance = 0.9f;

        }
    }
}
