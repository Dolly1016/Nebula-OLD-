namespace Nebula.Utilities;

public class ObjectPool<ObjectType> where ObjectType : Component
{
    ObjectType prefab;
    Transform parent;
    System.Action<ObjectType>? initializer;

    List<ObjectType> activeObjects;
    List<ObjectType> inactiveObjects;

    public ObjectPool(ObjectType prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
        activeObjects = new List<ObjectType>();
        inactiveObjects = new List<ObjectType>();
        initializer = null;
    }

    public void SetInitializer(System.Action<ObjectType> initializer)
    {
        this.initializer = initializer;
    }

    public ObjectType Get()
    {
        ObjectType obj;
        if (inactiveObjects.Count > 0)
        {
            obj = inactiveObjects[inactiveObjects.Count - 1];
            inactiveObjects.RemoveAt(inactiveObjects.Count - 1);
        }
        else
        {
            obj = GameObject.Instantiate(prefab, parent);
            if (initializer != null) initializer(obj);
        }

        obj.gameObject.SetActive(true);
        activeObjects.Add(obj);
        return obj;
    }

    public void Reclaim()
    {
        foreach (var obj in activeObjects)
        {
            obj.gameObject.SetActive(false);
        }
        inactiveObjects.AddRange(activeObjects);
        activeObjects.Clear();
    }

    public void Destroy()
    {
        foreach (var obj in activeObjects) GameObject.Destroy(obj);
        foreach (var obj in inactiveObjects) GameObject.Destroy(obj);

        activeObjects.Clear();
        inactiveObjects.Clear();
    }
}
