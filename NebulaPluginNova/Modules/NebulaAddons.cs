using Epic.OnlineServices.PlayerDataStorage;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

[NebulaPreLoad]
public class NebulaAddon : IDisposable
{
    public class AddonMeta
    {
        [JsonSerializableField]
        public string Name = "Undefined";
        [JsonSerializableField]
        public string Author = "Unknown";
        [JsonSerializableField]
        public string Description = "";
        [JsonSerializableField]
        public string Version = "";
    }

    static private List<NebulaAddon> allAddons = new();
    static public IEnumerable<NebulaAddon> AllAddons => allAddons;

    static public IEnumerator CoLoad()
    {
        Patches.LoadPatch.LoadingText = "Loading Addons";
        yield return null;

        Directory.CreateDirectory("Addons");

        
        //ローカルなアドオンを更新
        foreach(var dir in Directory.GetDirectories("Addons", "*"))
        {
            string id = Path.GetFileName(dir);
            string filePath = dir + "/" + id + ".zip";
            if (File.Exists(filePath)) File.Move(filePath, "Addons/" + id + ".zip", true);
        }

        
        //組込アドオンの読み込み
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (var file in assembly.GetManifestResourceNames().Where(name => name.StartsWith("Nebula.Resources.Addons.") && name.EndsWith(".zip"))){
            try
            {
                var stream = assembly.GetManifestResourceStream(file);
                if (stream == null) continue;
                var zip = new ZipArchive(stream);

                allAddons.Add(new NebulaAddon(zip, file));
            }
            catch
            {
            }
        }
        
        //外部アドオンの読み込み
        foreach(var file in Directory.GetFiles("Addons"))
        {
            var ext = Path.GetExtension(file);
            if (ext == null) continue;
            if (!ext.Equals(".zip")) continue;

            var zip = ZipFile.OpenRead(file);

            try
            {
                allAddons.Add(new NebulaAddon(zip, file));
            }
            catch
            {
                NebulaPlugin.Log.Print(NebulaLog.LogCategory.Addon, "Failed to load addon \"" + Path.GetFileName(file) + "\".");
            }
        }
    }

    static private string MetaFileName = "addon.meta";
    private NebulaAddon(ZipArchive zip,string path)
    {
        foreach (var entry in zip.Entries)
        {
            if (entry.Name != MetaFileName) continue;

            using var metaFile = entry.Open();

            AddonMeta? meta = (AddonMeta?)JsonStructure.Deserialize(metaFile,typeof(AddonMeta));
            if (meta == null) throw new Exception();

            AddonName = meta.Name;
            Author= meta.Author;
            Description = meta.Description;
            Version = meta.Version;

            InZipPath = entry.FullName.Substring(0, entry.FullName.Length - MetaFileName.Length);
            break;
        }

        using var iconEntry = zip.GetEntry(InZipPath + "icon.png")?.Open();
        if (iconEntry != null)
        {
            var texture = GraphicsHelper.LoadTextureFromStream(iconEntry);
            texture.MarkDontUnload();

            Icon = texture.ToSprite(Mathf.Max(texture.width, texture.height));
            Icon.MarkDontUnload();
        }

        Archive = zip;
    }

    public Stream? OpenStream(string path)
    {
        return Archive.GetEntry(InZipPath + path.Replace('\\', '/'))?.Open();
    }

    public void Dispose()
    {
        Archive.Dispose();
    }

    public string InZipPath { get; private set; } = "";
    public string Author { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Version { get; private set; } = "";
    public string AddonName { get; private set; } = "";
    public Sprite? Icon { get; private set; } = null;
    public ZipArchive Archive { get; private set; }
}
