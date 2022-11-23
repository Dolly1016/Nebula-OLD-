namespace Nebula.Ghost;

public class GhostInfo
{
    static public List<GhostInfo> AllGhostInfo = new List<GhostInfo>();

    public string LocalizeName { get; private set; }
    public string Name { get; private set; }

    public GhostInfo(string Name, string LocalizeName)
    {
        this.Name = Name;
        this.LocalizeName = LocalizeName;
    }

    public static void Load()
    {
        AllGhostInfo.Add(new GhostInfo("", "Test1"));
        AllGhostInfo.Add(new GhostInfo("", "Test2"));
        AllGhostInfo.Add(new GhostInfo("", "Test3"));
        AllGhostInfo.Add(new GhostInfo("", "Test4"));
        AllGhostInfo.Add(new GhostInfo("", "Test5"));
        AllGhostInfo.Add(new GhostInfo("", "Test6"));
    }
}