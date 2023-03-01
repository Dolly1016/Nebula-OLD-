namespace Nebula.Module.Information;

public static class UpperInformationManager
{
    static List<UpperInformation> AllInformations = new List<UpperInformation>();
    static GameObject manager;
    public static void Initialize()
    {
        AllInformations.Clear();
        manager = new GameObject("UpperInformation");
        manager.transform.SetParent(HudManager.Instance.transform);
        manager.transform.localPosition = new Vector3(0, 0, 0);
    }

    public static void Update()
    {
        float y = 2.8f;
        foreach (var i in AllInformations)
        {
            bool active = i.Update();
            i.gameObject.SetActive(active);
            if (active)
            {
                y -= i.height * 0.5f;
                i.gameObject.transform.localPosition = new Vector3(-i.width / 2, y, 0);
                y -= i.height * 0.5f;
            }
        }
    }

    static public void Register(UpperInformation information)
    {
        AllInformations.Add(information);
        information.gameObject.transform.SetParent(manager.transform);
    }

    public static void Remove(Predicate<UpperInformation> predicate)
    {
        AllInformations.RemoveAll((i) =>
        {
            if (predicate(i))
            {
                GameObject.Destroy(i.gameObject);
                return true;
            }
            return false;
        });
    }

    public static void RemoveAll()
    {
        foreach (var i in AllInformations)
        {
            GameObject.Destroy(i.gameObject);
        }
        AllInformations.Clear();
    }

    public static UpperInformation? GetInformation(Predicate<UpperInformation> predicate)
    {
        foreach (var i in AllInformations)
        {
            if (predicate(i)) return i;
        }
        return null;
    }
}

public class UpperInformation
{
    public GameObject gameObject { get; private set; }
    public float height { get; protected set; }
    public float width { get; protected set; }

    public UpperInformation(string name)
    {
        gameObject = new GameObject(name);

        UpperInformationManager.Register(this);

    }

    public virtual bool Update() { return false; }

}