using System.Reflection;
using Il2CppType = UnhollowerRuntimeLib.Il2CppType;

namespace Nebula.Module;

public static class AssetLoader
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
    public static AudioClip HadarDive;
    public static AudioClip HadarReappear;
    public static AudioClip HadarFear;

    public static AudioClip PlaceTrap2s;
    public static AudioClip PlaceTrap3s;
    public static AudioClip PlaceKillTrap;

    public static AudioClip SniperShot;
    public static AudioClip SniperEquip;
    public static AudioClip RaiderThrow;

    static public void Load()
    {
        var resourceStream = assembly.GetManifestResourceStream("Nebula.Resources.Assets.nebula_asset");
        var assetBundleBundle = AssetBundle.LoadFromMemory(resourceStream.ReadFully());

        HadarDive = assetBundleBundle.LoadAsset<AudioClip>("HadarDive.wav").DontUnload();
        HadarReappear = assetBundleBundle.LoadAsset<AudioClip>("HadarReappear.wav").DontUnload();
        HadarReappear = assetBundleBundle.LoadAsset<AudioClip>("HadarFear.wav").DontUnload();

        PlaceTrap2s = assetBundleBundle.LoadAsset<AudioClip>("PlaceTrap2s.wav").DontUnload();
        PlaceTrap3s = assetBundleBundle.LoadAsset<AudioClip>("PlaceTrap3s.wav").DontUnload();
        PlaceKillTrap = assetBundleBundle.LoadAsset<AudioClip>("PlaceKillTrap.wav").DontUnload();

        SniperShot = assetBundleBundle.LoadAsset<AudioClip>("SniperShot.wav").DontUnload();
        SniperEquip = assetBundleBundle.LoadAsset<AudioClip>("SniperEquip.wav").DontUnload();
        RaiderThrow = assetBundleBundle.LoadAsset<AudioClip>("RaiderThrow.wav").DontUnload();

    }

    public static byte[] ReadFully(this Stream input)
    {
        using (var ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }
    public static T LoadAsset<T>(this AssetBundle assetBundle, string name) where T : UnityEngine.Object
    {
        return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
    }
    public static T DontUnload<T>(this T obj) where T : UnityEngine.Object
    {
        obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;

        return obj;
    }

    public static AudioClip GetAudioClip(Module.AudioAsset id)
    {
        switch (id)
        {
            case AudioAsset.HadarDive:
                return HadarDive;
            case AudioAsset.HadarReappear:
                return HadarReappear;
            case AudioAsset.HadarFear:
                return HadarFear;

            case AudioAsset.PlaceTrap2s:
                return PlaceTrap2s;
            case AudioAsset.PlaceTrap3s:
                return PlaceTrap3s;
            case AudioAsset.PlaceKillTrap:
                return PlaceKillTrap;

            case AudioAsset.SniperShot:
                return SniperShot;
            case AudioAsset.SniperEquip:
                return SniperEquip;
            case AudioAsset.RaiderThrow:
                return RaiderThrow;
        }

        return null;
    }
}

public enum AudioAsset
{
    HadarDive = 0,
    HadarReappear,
    HadarFear,
    PlaceTrap2s,
    PlaceTrap3s,
    PlaceKillTrap,
    SniperShot,
    SniperEquip,
    RaiderThrow

}