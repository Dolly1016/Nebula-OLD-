using Il2CppSystem.Xml.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements.UIR;

namespace Nebula.Modules;

public class ModUpdater
{
    class ReleaseContent
    {
        [JsonSerializableField]
        public string? name = null!;
    }

    public class ReleasedInfo
    {
        public enum ReleaseCategory
        {
            Major,
            Snapshot,
            PreRelease,
            Custom,
            Unknown
        }
        
        public static Color[] CategoryColors = {
            new Color(176f / 255f, 204f / 255f, 251f / 255f),
            new Color(247f / 255f, 255f / 255f, 29f / 255f),
            new Color(217f / 255f, 179f / 255f, 237f / 255f),
            new Color(180f / 255f, 159f / 255f, 107f / 255f),
            new Color(141f / 255f, 141f / 255f, 141f / 255f)
        };

        public static string[] CategoryTranslationKeys = {
            "version.category.major",
            "version.category.snapshot",
            "version.category.preRelease",
            "version.category.custom",
            "version.category.unknown"
        };
        
        public ReleaseCategory Category;
        public int BuildNum;
        public int Epoch;
        public string? Version;
        public string RawTag => rawTag;
        private string rawTag;

        public ReleasedInfo(string tag)
        {
            rawTag = tag;

            string[] strings = tag.Split(",");
            if (strings.Length != 4) return;

            switch (strings[0])
            {
                case "v":
                    Category = ReleaseCategory.Major;
                    break;
                case "s":
                    Category = ReleaseCategory.Snapshot;
                    break;
                case "p":
                    Category = ReleaseCategory.PreRelease;
                    break;
                case "c":
                    Category = ReleaseCategory.Custom;
                    break;
                default:
                    Category= ReleaseCategory.Unknown;
                    break;
            }

            Version = strings[1];
            if (int.TryParse(strings[2], out int epoch))
                Epoch = epoch;
            else
                Epoch = -1;

            if (int.TryParse(strings[3], out int build))
                BuildNum = build;
            else
                BuildNum = -1;
        }

        private async Task UpdateAsync()
        {
            string url = $"https://github.com/Dolly1016/Nebula/releases/download/{rawTag}/Nebula.dll";
            var response = await NebulaPlugin.HttpClient.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK) return;
            var dllStream = await response.Content.ReadAsStreamAsync();

            try
            {
                string path = System.Uri.UnescapeDataString(new System.UriBuilder(Assembly.GetExecutingAssembly().Location!).Path);
                File.Move(path, path + ".old", true);
                using var fileStream = File.Create(path);
                dllStream.CopyTo(fileStream);
                fileStream.Flush();
            }catch(Exception ex)
            {
                NebulaPlugin.Log.Print(null, ex.GetType().Name + ex.Message + ex.StackTrace);
            }
        }

        public IEnumerator CoUpdateAndShowDialog()
        {
            var preWindow = MetaScreen.GenerateWindow(new Vector2(3f, 1.2f), null, new Vector3(0, 0, 0), true, true, true);
            preWindow.SetContext(new MetaContext.Text(new(TextAttribute.NormalAttr) { Size = new(3f, 1.2f) }) { TranslationKey = "ui.update.waitFinishing" });
            yield return UpdateAsync().WaitAsCoroutine();
            preWindow.CloseScreen();

            var postWindow = MetaScreen.GenerateWindow(new Vector2(3f, 1.2f), null, new Vector3(0, 0, 0), true, true, true);
            postWindow.SetContext(new MetaContext.Text(new(TextAttribute.NormalAttr) { Size = new(3f, 1.2f) }) { TranslationKey = "ui.update.finishUpdate" });
        }
    }

    static string GetTagsUrl(int page) => "https://api.github.com/repos/Dolly1016/Nebula/tags?per_page=100&page=" + (page);
    private static async Task FetchAsync()
    {
        List<ReleasedInfo> releases = new();

        int page = 1;
        while (true)
        {
            var response = await NebulaPlugin.HttpClient.GetAsync(GetTagsUrl(page));
            if (response.StatusCode != HttpStatusCode.OK) break;
            string json = await response.Content.ReadAsStringAsync();

            var tags = JsonStructure.Deserialize<List<ReleaseContent>>(json);
            if (tags != null) foreach (var tag in tags) if (tag.name != null) releases.Add(new(tag.name));
            NebulaPlugin.Log.Print(null, releases.Count.ToString());
            if (tags == null || tags.Count < 100 || tags.Count == 0) break;
            page++;
        }

        releases.Sort((v1, v2) => v1.Epoch != v2.Epoch ? v2.Epoch - v1.Epoch : v2.BuildNum - v1.BuildNum);

        cache = releases;
    }

    static private List<ReleasedInfo>? cache = null;
    public static IEnumerator CoFetchVersionTags(Action<List<ReleasedInfo>> postAction)
    {
        yield return FetchAsync().WaitAsCoroutine();
        postAction.Invoke(cache!);
        yield break;
    }
}
