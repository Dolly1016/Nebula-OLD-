using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nebula.Utilities;

public static class UnityHelper
{
    public static GameObject CreateObject(string objName, Transform? parent, Vector3 localPosition,int? layer = null)
    {
        var obj = new GameObject(objName);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = new Vector3(1f, 1f, 1f);
        if (layer.HasValue) obj.layer = layer.Value;
        else if (parent != null) obj.layer = parent.gameObject.layer;
        return obj;
    }

    public static T CreateObject<T>(string objName, Transform? parent, Vector3 localPosition,int? layer = null) where T : Component
    {
        return CreateObject(objName, parent, localPosition, layer).AddComponent<T>();
    }

    public static LineRenderer SetUpLineRenderer(string objName,Transform? parent,Vector3 localPosition,int? layer = null,float width = 0.2f)
    {
        var line = UnityHelper.CreateObject<LineRenderer>(objName, parent, localPosition, layer);
        line.material.shader = Shader.Find("Sprites/Default");
        line.SetColors(Color.clear, Color.clear);
        line.positionCount = 2;
        line.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
        line.useWorldSpace = false;
        line.SetWidth(width, width);
        return line;
    }

    public static T? FindAsset<T>(string name) where T : Il2CppObjectBase
    {
        foreach (var asset in UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<T>()))
        {
            if (asset.name == name) return asset.Cast<T>();
        }
        return null;
    }

    public static T MarkDontUnload<T>(this T obj) where T : UnityEngine.Object
    {
        GameObject.DontDestroyOnLoad(obj);
        obj.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;

        return obj;
    }

    public static Camera? FindCamera(int cameraLayer) => Camera.allCameras.FirstOrDefault(c => (c.cullingMask & (1 << cameraLayer)) != 0);

    public static Vector3 ScreenToWorldPoint(Vector3 screenPos, int cameraLayer)
    {
        return FindCamera(cameraLayer)?.ScreenToWorldPoint(screenPos) ?? Vector3.zero;
    }

    public static Vector3 WorldToScreenPoint(Vector3 worldPos, int cameraLayer)
    {
        return FindCamera(cameraLayer)?.WorldToScreenPoint(worldPos) ?? Vector3.zero;
    }

    public static PassiveButton SetUpButton(this GameObject gameObject, bool withSound = false, SpriteRenderer? buttonRenderer = null, Color? defaultColor = null) {
        var button = gameObject.AddComponent<PassiveButton>();
        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        button.OnMouseOver = new UnityEngine.Events.UnityEvent();

        if (withSound)
        {
            button.OnClick.AddListener(() => SoundManager.Instance.PlaySound(VanillaAsset.SelectClip, false, 0.8f));
            button.OnMouseOver.AddListener(() => SoundManager.Instance.PlaySound(VanillaAsset.HoverClip, false, 0.8f));
        }
        if (buttonRenderer != null)
        {
            button.OnMouseOut.AddListener(() => buttonRenderer!.color = defaultColor ?? Color.white);
            button.OnMouseOver.AddListener(() => buttonRenderer!.color = Color.green);
        }

        if (buttonRenderer != null) buttonRenderer.color = defaultColor ?? Color.white;
        
        return button;
    }

    static public void AddListener(this UnityEngine.UI.Button.ButtonClickedEvent onClick, Action action) => onClick.AddListener((UnityEngine.Events.UnityAction)action);
    static public void AddListener(this UnityEngine.Events.UnityEvent unityEvent, Action action) => unityEvent.AddListener((UnityEngine.Events.UnityAction)action);

    public static void SetModText(this TextTranslatorTMP text,string translationKey)
    {
        text.TargetText = (StringNames)short.MaxValue;
        text.defaultStr = translationKey;
    }
}

