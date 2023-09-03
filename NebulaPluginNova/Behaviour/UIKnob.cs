using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Behaviour;

public class UIKnob : PassiveUiElement
{
    static UIKnob() => ClassInjector.RegisterTypeInIl2Cpp<UIKnob>();
    public UIKnob(System.IntPtr ptr) : base(ptr) { }
    public UIKnob() : base(ClassInjector.DerivedConstructorPointer<UIKnob>())
    { ClassInjector.DerivedConstructorBody(this); }

    public (float min, float max) Range = (0.0f, 0.0f);
    public bool IsVert = true;

    public Action? OnHold, OnRelease;
    public Action<float>? OnDragging;
    public SpriteRenderer? Renderer = null;

    private bool isHolding = false;
    public bool IsHolding { get => isHolding; private set {
            if (value == isHolding) return;
            isHolding = value;
            if (value)
                OnHold?.Invoke();
            else
                OnRelease?.Invoke();
        } 
    }

    public override void ReceiveClickDown()
    {
        IsHolding = true;
    }

    public override void ReceiveClickUp()
    {
        IsHolding = false;
    }

    public override void ReceiveClickDrag(Vector2 dragDelta)
    {
        float delta = IsVert ? dragDelta.y : dragDelta.x;

        var localPos = transform.localPosition;
        localPos += new Vector3(IsVert ? 0f : delta, IsVert ? delta : 0f, 0f);
        if (IsVert)
            localPos.y = Mathf.Clamp(localPos.y, Range.min, Range.max);
        else
            localPos.x = Mathf.Clamp(localPos.x, Range.min, Range.max);
        transform.localPosition = localPos;

        OnDragging?.Invoke(IsVert ? localPos.y : localPos.x);
    }

    public override void ReceiveMouseOver()
    {
        if (Renderer) Renderer.color = Color.white.RGBMultiplied(0.5f);
    }

    public override void ReceiveMouseOut()
    {
        if (Renderer) Renderer.color = Color.white;
    }

    public override void OnEnable()
    {
        OnMouseOut = new UnityEngine.Events.UnityEvent();
        OnMouseOver = new UnityEngine.Events.UnityEvent();
    }

    public override bool HandleDown => true;
    public override bool HandleUp => true;
    public override bool HandleDrag => true;
}
