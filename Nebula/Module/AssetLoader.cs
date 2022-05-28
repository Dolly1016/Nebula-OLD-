using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Il2CppType = UnhollowerRuntimeLib.Il2CppType;

namespace Nebula.Module
{
    public static class AssetLoader
    {
        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
        public static AudioClip HadarDive;
        public static AudioClip HadarReappear;
        public static AudioClip HadarFear;
        
        public static AudioClip PlaceTrap2s;
        public static AudioClip PlaceTrap3s;
        public static AudioClip PlaceKillTrap;

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

        }

        public static byte[] ReadFully(this Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
        public static T? LoadAsset<T>(this AssetBundle assetBundle, string name) where T : UnityEngine.Object
        {
            return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
        }
        public static T DontUnload<T>(this T obj) where T : Object
        {
            obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return obj;
        }

        public static AudioClip GetAudioClip(Module.AudioAsset id)
        {
            switch ((int)id)
            {
                case 0: return HadarDive;
                case 1: return HadarReappear;
                case 2: return HadarFear;

                case 3: return PlaceTrap2s;
                case 4: return PlaceTrap3s;
                case 5: return PlaceKillTrap;
            }

            return null;
        }
    }

    public enum AudioAsset
    {
        HadarDive=0,
        HadarReappear,
        HadarFear,
        PlaceTrap2s,
        PlaceTrap3s,
        PlaceKillTrap

    }
}
