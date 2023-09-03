using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules.ScriptComponents;

public abstract class INebulaBindableComponent
{
    public INebulaBindableComponent()
    {
    }

    public virtual void Release() { }

}

public abstract class INebulaScriptComponent : INebulaBindableComponent
{
    public INebulaScriptComponent()
    {
        NebulaGameManager.Instance?.RegisterComponent(this);
    }

    public abstract void Update();
    public virtual void OnMeetingStart() { }
    public virtual void OnReleased() { }
    public virtual void OnGameReenabled() { }
    public virtual void OnGameStart() { }
    public override void Release()
    {
        MarkedRelease = true;
    }
    public bool MarkedRelease { get; private set; } = false;
    public virtual bool UpdateWithMyPlayer { get => false; }

}

public class GameObjectBinding : INebulaScriptComponent
{
    public GameObject? MyObject { get; private init; }

    public GameObjectBinding(GameObject binding) : base()
    {
        MyObject = binding;
    }

    public override void Update() { }
    public override void OnReleased() {
        if (MyObject) GameObject.Destroy(MyObject);
    }
}

public class ScriptHolder : INebulaBindableComponent
{

    private List<INebulaBindableComponent> myComponent { get; init; } = new();
    protected T Bind<T>(T component) where T : INebulaBindableComponent
    {
        BindComponent(component);
        return component;
    }

    protected void BindComponent(INebulaBindableComponent component) => myComponent.Add(component);

    protected void ReleaseComponents()
    {
        foreach (INebulaBindableComponent component in myComponent) component.Release();
        myComponent.Clear();
    }

    public override void Release()
    {
        ReleaseComponents();
    }

}