using Il2CppSystem.Net;
using Il2CppSystem.Security.AccessControl;
using JetBrains.Annotations;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Expansion;

public static class ConsoleExpansion
{

    private static Material? highlightMaterial = null;

    public static Material GetHighlightMaterial()
    {
        if (highlightMaterial != null) return new Material(highlightMaterial);
        foreach (var mat in UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppType.Of<Material>()))
        {
            if (mat.name == "HighlightMat")
            {
                highlightMaterial = mat.TryCast<Material>();
                break;
            }
        }
        return new Material(highlightMaterial);
    }

    public static Console GenerateConsole<C>(Vector3 pos,string name,Sprite sprite) where C : Console
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        obj.AddComponent<SpriteRenderer>().sprite=sprite;
        return Consolize<C>(obj);
    }

    public static Console Consolize<C>(GameObject obj,SpriteRenderer? renderer = null) where C : Console
    {
        obj.layer = LayerMask.NameToLayer("ShortObjects");
        Console console = obj.GetComponent<Console>();
        PassiveButton button = obj.GetComponent<PassiveButton>();
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (!console)
        {
            console = obj.AddComponent<C>();
            console.checkWalls = true;
            console.usableDistance = 0.7f;
            console.TaskTypes = new TaskTypes[0];
            console.ValidTasks = new Il2CppReferenceArray<TaskSet>(0);
            var list = ShipStatus.Instance.AllConsoles.ToList();
            list.Add(console);
            ShipStatus.Instance.AllConsoles = new Il2CppReferenceArray<Console>(list.ToArray());
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
        console.TaskTypes = new Il2CppStructArray<TaskTypes>(list.ToArray());
        return console;
    }

    public static Console ConsolizePlayer<C>(this PlayerControl player,string objectName,Sprite? sprite=null) where C :Console
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(player.transform);
        obj.transform.localPosition = new Vector3(0,0);

        SpriteRenderer? renderer = null;
        if (sprite != null)
        {
            obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
        }

        Console c = Consolize<C>(obj, sprite != null ? renderer : player.cosmetics.currentBodySprite.BodySprite);
        return c;
    }
}
