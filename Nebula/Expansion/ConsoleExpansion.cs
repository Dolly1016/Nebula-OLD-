using Il2CppSystem.Net;
using Il2CppSystem.Security.AccessControl;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Expansion
{
    public static class ConsoleExpansion
    {

        private static Material? highlightMaterial = null;

        private static Material GetHighlightMaterial()
        {
            if (highlightMaterial != null) return new Material(highlightMaterial);
            foreach (var mat in UnityEngine.Resources.FindObjectsOfTypeAll(Material.Il2CppType))
            {
                if (mat.name == "HighlightMat")
                {
                    highlightMaterial = mat.TryCast<Material>();
                    break;
                }
            }
            return new Material(highlightMaterial);
        }

        public static Console GenerateConsole(Vector3 pos,string name,Sprite sprite)
        {
            var obj = new GameObject(name);
            obj.transform.position = pos;
            obj.AddComponent<SpriteRenderer>().sprite=sprite;
            return Consolize(obj);
        }

        public static Console Consolize(GameObject obj,SpriteRenderer? renderer = null)
        {
            obj.layer = LayerMask.NameToLayer("ShortObjects");
            Console console = obj.GetComponent<Console>();
            PassiveButton button = obj.GetComponent<PassiveButton>();
            Collider2D collider = obj.GetComponent<Collider2D>();
            if (!console)
            {
                console = obj.AddComponent<Console>();
                console.checkWalls = true;
                console.usableDistance = 0.7f;
                console.TaskTypes = new TaskTypes[0];
                console.ValidTasks = new UnhollowerBaseLib.Il2CppReferenceArray<TaskSet>(0);
                var list = ShipStatus.Instance.AllConsoles.ToList();
                list.Add(console);
                ShipStatus.Instance.AllConsoles = new UnhollowerBaseLib.Il2CppReferenceArray<Console>(list.ToArray());
            }
            if (console.Image == null)
            {
                if (renderer != null)
                {
                    console.Image = renderer;
                }
                else
                {
                    console.Image = obj.GetComponent<SpriteRenderer>();
                    console.Image.material = GetHighlightMaterial();
                }
            }
            if (!button)
            {
                button = obj.AddComponent<PassiveButton>();
                button.OnMouseOut = new UnityEngine.Events.UnityEvent();
                button.OnMouseOver = new UnityEngine.Events.UnityEvent();
                button._CachedZ_k__BackingField = 0.1f;
                button.CachedZ = 0.1f;
            }

            if (!collider)
            {
                var cCollider = obj.AddComponent<CircleCollider2D>();
                cCollider.radius = 0.4f;
                cCollider.isTrigger = true;
            }

            return console;
        }

        public static Console AddValidTask(this Console console,TaskTypes taskType)
        {
            var list = console.TaskTypes.ToList();
            list.Add(taskType);
            console.TaskTypes = new UnhollowerBaseLib.Il2CppStructArray<TaskTypes>(list.ToArray());
            return console;
        }

        public static Console ConsolizePlayer(this PlayerControl player,string objectName)
        {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(player.transform);
            obj.transform.localPosition = new Vector3(0,0);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();

            Console c = Consolize(obj, player.cosmetics.currentBodySprite.BodySprite);
            return c;
        }
    }
}
