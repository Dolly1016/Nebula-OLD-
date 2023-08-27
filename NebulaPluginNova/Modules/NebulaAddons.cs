using Epic.OnlineServices.PlayerDataStorage;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Modules;

[NebulaPreLoad]
public class NebulaAddon
{
    public class AddonMeta
    {
        [JsonSerializableField]
        public string Name;
        [JsonSerializableField]
        public string Author;
        [JsonSerializableField]
        public string Description;
    }
    static public List<NebulaAddon> AllAddons = new();

    static public void Load()
    {
        Directory.CreateDirectory("Addons");

        foreach(var file in Directory.GetFiles("Addons"))
        {
            var ext = Path.GetExtension(file);
            if (ext == null) continue;
            if (!ext.Equals(".zip")) continue;

            using var zip = ZipFile.OpenRead(file);

            try
            {
                AllAddons.Add(new NebulaAddon(zip, file));
            }
            catch
            {
                Debug.Log("[Addons] Failed to load addon \"" + Path.GetFileName(file) + "\".");
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

            AddonName=meta.Name.Replace('_',' ');
            Author= meta.Author;
            Description= meta.Description.Replace('_', ' ');

            InZipPath = entry.FullName.Substring(0, entry.FullName.Length - MetaFileName.Length);
            break;
        }

        using var iconEntry = zip.GetEntry(InZipPath + "icon.png")?.Open();
        if (iconEntry != null)
        {
            var texture = GraphicsHelper.LoadTextureFromStream(iconEntry);
            texture.MarkDontUnload();
            Icon = texture.ToSprite(100f);
            Icon.MarkDontUnload();
        }
        
    }

    public string InZipPath { get; private set; } = "";

    public string Author { get; private set; }
    public string Description { get; private set; }
    public string AddonName { get; private set; }
    public Sprite? Icon { get; private set; } = null;
}
