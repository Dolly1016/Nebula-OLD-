using BepInEx.Configuration;
using UnityEngine.UI;
using System.Reflection;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Twitch;
using System.Text.RegularExpressions;
using Nebula.Patches;
using AmongUs.Data;
using Assets.InnerNet;
using AmongUs.Data.Player;
using Iced.Intel;

namespace Nebula.Module;


public class ModNews
{
    public int Number;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement();
        result.Number = Number;
        result.Title = Title;
        result.SubTitle = SubTitle;
        result.ShortTitle = ShortTitle;
        result.Text = Text;
        result.Language = (uint)DataManager.Settings.Language.CurrentLanguage;
        result.Date = Date;
        result.Id = "ModNews";

        return result;
    }
}

[HarmonyPatch]
public class ModNewsHistory
{
    public static List<ModNews> AllModNews = new List<ModNews>();

    private static Regex RoleRegex = new Regex("%ROLE:[A-Z]+\\([^)]+\\)%");
    private static Regex OptionRegex = new Regex("%LANG\\([a-zA-Z\\.0-9]+\\)\\,\\([^)]+\\)%");

    private static void FormatRoleString(Match match, ref string str, string key, string defaultString)
    {
        foreach (var role in Roles.Roles.AllRoles)
        {
            if (role.Name.ToUpper() == key)
            {
                str = str.Replace(match.Value, Helpers.csWithoutAlpha(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
            }
        }
        foreach (var role in Roles.Roles.AllExtraRoles)
        {
            if (role.Name.ToUpper() == key)
            {
                str = str.Replace(match.Value, Helpers.csWithoutAlpha(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
            }
        }
        str = str.Replace(match.Value, defaultString);
    }


    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Init)), HarmonyPostfix]
    public static void Initialize(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        IEnumerator GetEnumerator()
        {
            while (AnnouncementPopUp.UpdateState == AnnouncementPopUp.AnnounceState.Fetching) yield return null;
            if (AnnouncementPopUp.UpdateState > AnnouncementPopUp.AnnounceState.Fetching) yield break;
            AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.Fetching;
            AllModNews.Clear();

            var lang = Language.Language.GetLanguage((uint)DataManager.Settings.Language.CurrentLanguage);

            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            var task = http.GetAsync(new System.Uri($"https://raw.githubusercontent.com/Dolly1016/Nebula/master/Announcement_{lang}.json"), HttpCompletionOption.ResponseContentRead);
            while (!task.IsCompleted) yield return null;
            var response = task.Result;


            if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
            {
                AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
                yield break;
            }

            string json = response.Content.ReadAsStringAsync().Result;
            JToken jObj = JObject.Parse(json)["News"];
            for (JToken current = jObj.First; current != null; current = current.Next)
            {
                if (!current.HasValues) continue;

                try
                {
                    var news = new ModNews();
                    news.Number = int.Parse(current["Number"].ToString());
                    news.Title = current["Title"].ToString();
                    news.SubTitle = current["Subtitle"].ToString();
                    news.ShortTitle = current["Short"].ToString();
                    news.Text = current["Body"].ToString();
                    news.Date = current["Date"].ToString();

                    foreach (Match match in RoleRegex.Matches(news.Text))
                    {
                        var split = match.Value.Split(':', '(', ')');
                        FormatRoleString(match, ref news.Text, split[1], split[2]);
                    }

                    foreach (Match match in OptionRegex.Matches(news.Text))
                    {
                        var split = match.Value.Split('(', ')');

                        news.Text = news.Text.Replace(match.Value,
                            Language.Language.CheckValidKey(split[1]) ?
                            Language.Language.GetString(split[1]) : split[3]);
                    }


                    AllModNews.Add(news);
                }
                catch
                {
                    NebulaPlugin.Instance.Logger.Print("ModNews", "Failed to load news.");
                }
            }

            AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
        }

        __result = Effects.Sequence(GetEnumerator().WrapToIl2Cpp(), __result);
    }

    public static int Compare(DateTime t1, DateTime t2)
    {
        long ticks1 = t1.Ticks;
        long ticks2 = t2.Ticks;
        if (ticks1 > ticks2) return 1;
        if (ticks1 < ticks2) return -1;
        return 0;
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] Il2CppReferenceArray<Announcement> aRange)
    {
        DateTime ParseDateTime(Announcement a)
        {
            try
            {
                return DateTime.Parse(a.Date);
            }
            catch
            {
                return new DateTime(2000, 1, 1);
            }
        }

        List<Announcement> list = new();
        foreach (var a in aRange) list.Add(a);
        foreach (var m in AllModNews) list.Add(m.ToAnnouncement());
        list.Sort((a1, a2) => { return DateTime.Compare(ParseDateTime(a2), ParseDateTime(a1)); });

        __instance.allAnnouncements = new Il2CppSystem.Collections.Generic.List<Announcement>();
        foreach (var a in list) __instance.allAnnouncements.Add(a);


        __instance.HandleChange();
        __instance.OnAddAnnouncement?.Invoke();

        return false;
    }

    static SpriteLoader ModLabel = new SpriteLoader("Nebula.Resources.NebulaNewsIcon.png", 100f);

    [HarmonyPatch(typeof(AnnouncementPanel), nameof(AnnouncementPanel.SetUp)), HarmonyPostfix]
    public static void SetUpPanel(AnnouncementPanel __instance, [HarmonyArgument(0)] Announcement announcement)
    {
        if (announcement.Number < 100000) return;
        var obj = new GameObject("ModLabel");
        obj.layer = LayerExpansion.GetUILayer();
        obj.transform.SetParent(__instance.transform);
        obj.transform.localPosition=new Vector3(-0.8f,0.13f,0.5f);
        obj.transform.localScale = new Vector3(0.9f,0.9f,0.9f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = ModLabel.GetSprite();
        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
}

[HarmonyPatch(typeof(SelectableHyperLinkHelper),nameof(SelectableHyperLinkHelper.DecomposeHyperlinkText))]
public static class HyperLinkHelperPatch
{
    static bool Prefix([HarmonyArgument(0)]string textToDecompose, [HarmonyArgument(1)] Il2CppSystem.Collections.Generic.List<ITextPart> textConstituents, [HarmonyArgument(2)] Il2CppSystem.Collections.Generic.List<ITextPart> selectableHyperLinks)
    {
        if (string.IsNullOrEmpty(textToDecompose))
        {
            return false;
        }
        selectableHyperLinks.Clear();
        textConstituents.Clear();
        while (textToDecompose.Length > 0)
        {
            string text;
            string text2;
            SelectableHyperLinkHelper.GetFirstUrl(textToDecompose, out text, out text2);
            if (!string.IsNullOrEmpty(text))
            {
                ITextPart item = (new TextPart(text)).CastFast<ITextPart>();
                textConstituents.Add(item);
            }
            int padding = 1;
            if (!string.IsNullOrEmpty(text2))
            {
                
                SelectableHyperLink component = UnityEngine.Object.Instantiate<GameObject>(Resources.Load("SelectableHyperlink").CastFast<GameObject>()).GetComponent<SelectableHyperLink>();
                try
                {
                    component.LinkText = text2;
                    var textPart = component.CastFast<ITextPart>();
                    selectableHyperLinks.Add(textPart);
                    textConstituents.Add(textPart);
                }
                catch
                {
                    ITextPart item = (new TextPart(text2)).CastFast<ITextPart>();
                    textConstituents.Add(item);
                    UnityEngine.Object.Destroy(component.gameObject);
                    padding = 0;
                }
            }
            int startIndex = Math.Min(textToDecompose.Length, text.Length + text2.Length + padding);
            textToDecompose = textToDecompose.Substring(startIndex);
        }

        return false;
    }
}


/*
    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.UpdateAnnounceText))]
    public static class AnnouncementPatch
    {
        private static bool ShownFlag = false;
        private static ConfigEntry<int> AnnounceVersion = null;
        private static string Announcement = "";

        private static string FormatRoleString(Match match, string str, string key, string defaultString)
        {
            foreach (var role in Roles.Roles.AllRoles)
            {
                if (role.Name.ToUpper() == key)
                {
                    str = str.Replace(match.Value, Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
                    return str;
                }
            }
            foreach (var role in Roles.Roles.AllExtraRoles)
            {
                if (role.Name.ToUpper() == key)
                {
                    str = str.Replace(match.Value, Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
                    return str;
                }
            }

            str = str.Replace(match.Value, defaultString);
            return str;
        }

        private static string FormatString(string str)
        {
            Regex regex;

            //旧式の変換
            foreach (var role in Roles.Roles.AllRoles)
            {
                str = str.Replace("%ROLE:" + role.Name.ToUpper() + "%", Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
            }
            foreach (var role in Roles.Roles.AllExtraRoles)
            {
                str = str.Replace("%ROLE:" + role.Name.ToUpper() + "%", Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
            }

            regex = new Regex("%ROLE:[A-Z]+\\([^)]+\\)%");
            foreach (Match match in regex.Matches(str))
            {
                var split = match.Value.Split(':', '(', ')');
                str = FormatRoleString(match, str, split[1], split[2]);
            }

            str = str.Replace("%/COLOR%", "</color>");

            regex = new Regex("%OPTION\\([a-zA-Z\\.0-9]+\\)\\,\\([^)]+\\)%");
            foreach (Match match in regex.Matches(str))
            {
                var split = match.Value.Split('(', ')');

                str = str.Replace(match.Value,
                    Language.Language.CheckValidKey(split[1]) ?
                    Language.Language.GetString(split[1]) : split[3]);
            }

            return str;
        }

        public static bool LoadAnnouncement()
        {
            if (AnnounceVersion == null)
            {
                AnnounceVersion = NebulaPlugin.Instance.Config.Bind("Announce", "Version", (int)0);
            }

            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            var response = http.GetAsync(new System.Uri($"https://raw.githubusercontent.com/Dolly1016/Nebula/master/announcement.json"), HttpCompletionOption.ResponseContentRead).Result;


            try
            {
                if (response.StatusCode != HttpStatusCode.OK) return false;
                if (response.Content == null) return false;
                string json = response.Content.ReadAsStringAsync().Result;
                JObject jObj = JObject.Parse(json);
                JToken? version = jObj["Version"];
                if (version == null) return false;
                int Version = int.Parse(version.ToString());

                //既にみたことがあれば出さない
                if (AnnounceVersion.Value == Version)
                {
                    ShownFlag = true;
                }
                //更新する
                AnnounceVersion.Value = Version;

                string lang = Language.Language.GetLanguage((uint)AmongUs.Data.DataManager.Settings.Language.CurrentLanguage);
                if (jObj[lang] != null)
                    Announcement = jObj[lang].ToString();
                else if (jObj["English"] != null)
                    Announcement = jObj["English"].ToString();
                else if (jObj["Japanese"] != null)
                    Announcement = jObj["Japanese"].ToString();
                else
                {
                    Announcement = "-Invalid Announcement-";
                    return false;
                }
                Announcement = FormatString(Announcement);
            }
            catch (System.Exception ex)
            {
            }
            return !ShownFlag;
        }

        public static bool Prefix(AnnouncementPopUp __instance)
        {
            if (!ShownFlag)
            {
                if (LoadAnnouncement())
                {
                    AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.Success;
                    ShownFlag = true;
                }
            }
            else { LoadAnnouncement(); }

            __instance.AnnounceTextMeshPro.text = Announcement;

            return false;
        }
    }
    */

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public class ModUpdaterButton
{
    private static GameObject GenerateButton(GameObject template, Color color, string text, System.Action? action, bool mirror, ref int buttons)
    {
        buttons++;

        var button = UnityEngine.Object.Instantiate(template, null);
        button.transform.localPosition = new Vector3(mirror ? -button.transform.localPosition.x : button.transform.localPosition.x, button.transform.localPosition.y + ((float)buttons * 0.6f), button.transform.localPosition.z);

        var renderer = button.gameObject.GetComponent<SpriteRenderer>();
        renderer.color = color;

        var child = button.transform.GetChild(0);
        child.GetComponent<TextTranslatorTMP>().enabled = false;
        var tmpText = child.GetComponent<TMPro.TMP_Text>();
        tmpText.SetText(Language.Language.GetString(text));
        tmpText.color = color;

        PassiveButton passiveButton = button.GetComponent<PassiveButton>();
        passiveButton.OnClick = new Button.ButtonClickedEvent();
        if (action != null) passiveButton.OnClick.AddListener(action);
        passiveButton.OnMouseOut.AddListener((UnityEngine.Events.UnityAction)(() => renderer.color = tmpText.color));

        AspectPosition aspectPosition = button.GetComponent<AspectPosition>();
        if (mirror) aspectPosition.Alignment = AspectPosition.EdgeAlignments.RightBottom;
        aspectPosition.DistanceFromEdge = new Vector3(0.6f, 0.35f + 0.6f * buttons, -5f);


        return button;
    }

    private static void Prefix(MainMenuManager __instance)
    {
        //More Cosmic 更新
        CosmicLoader.LaunchCosmicFetcher();

        ModUpdater.LaunchUpdater();

        var template = GameObject.Find("ExitGameButton");
        if (template == null) return;

        int buttons = 0;

        List<GameObject> updateButtons = new List<GameObject>();
        if (ModUpdater.hasUpdate || ModUpdater.hasUnprocessableUpdate || NebulaPlugin.IsSnapshot)
        {
            string message;
            if (ModUpdater.hasUnprocessableUpdate) message = "title.button.existNewerNebula";
            else if (ModUpdater.hasUpdate) message = "title.button.updateNebula";
            else message = "title.button.getStableNebula";
            updateButtons.Add(GenerateButton(template, Color.white, message, !ModUpdater.hasUnprocessableUpdate ? (System.Action)onClickUpdateButton : null, false, ref buttons));

            void onClickUpdateButton()
            {
                ModUpdater.ExecuteUpdate(ModUpdater.updateURI);
                foreach (var b in updateButtons) b.SetActive(false);
            }
        }

        //最新版(あるいは後続のスナップショット)を所持している場合のみスナップショットを利用可能
        if (NebulaOption.configSnapshot.Value && ModUpdater.hasNewestSnapshot)
        {
            updateButtons.Add(GenerateButton(template, new Color(0.3f, 0.6f, 0.75f), "title.button.updateNebulaSnapshot", onClickUpdateSnapshotButton, false, ref buttons));

            void onClickUpdateSnapshotButton()
            {
                ModUpdater.ExecuteUpdate("https://github.com/Dolly1016/Nebula/releases/download/snapshot/Nebula.dll");
                foreach (var b in updateButtons) b.SetActive(false);
            }
        }

        int mirrorButtons = -1;

        GenerateButton(template, new Color(29f / 255f, 161f / 255f, 242f / 255f), "title.button.twitter", () => Application.OpenURL("https://twitter.com/NebulaOnTheShip"), true, ref mirrorButtons);
        GenerateButton(template, new Color(86f / 255f, 97f / 255f, 234f / 255f), "title.button.discord", () => Application.OpenURL("https://discord.gg/kHNZD4pq9E"), true, ref mirrorButtons);


        TwitchManager man = DestroyableSingleton<TwitchManager>.Instance;
        ModUpdater.InfoPopup = UnityEngine.Object.Instantiate<GenericPopup>(man.TwitchPopup);
        ModUpdater.InfoPopup.TextAreaTMP.fontSize *= 0.7f;
        ModUpdater.InfoPopup.TextAreaTMP.enableAutoSizing = false;

    }
}

public class ModUpdater
{
    public static bool running = false;
    public static bool hasUpdate = false;
    public static bool hasUnprocessableUpdate = false;
    public static bool hasNewestSnapshot = false;
    public static string updateURI = null;
    private static Task updateTask = null;
    public static GenericPopup InfoPopup;

    public static void LaunchUpdater()
    {
        if (running) return;
        running = true;
        checkForUpdate().GetAwaiter().GetResult();
        clearOldVersions();
    }

    public static void ExecuteUpdate(string updateURI)
    {
        string info = Language.Language.GetString("update.pleaseWait");
        ModUpdater.InfoPopup.Show(info); // Show originally
        if (updateTask == null)
        {
            if (updateURI != null)
            {
                updateTask = downloadUpdate(updateURI);
            }
            else
            {
                info = Language.Language.GetString("update.manually");
            }
        }
        else
        {
            info = Language.Language.GetString("update.inProgress");
        }
        ModUpdater.InfoPopup.StartCoroutine(Effects.Lerp(0.01f, new System.Action<float>((p) => { ModUpdater.setPopupText(info); })));
    }

    public static void clearOldVersions()
    {
        try
        {
            DirectoryInfo d = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath) + @"\BepInEx\plugins");
            string[] files = d.GetFiles("*.old").Select(x => x.FullName).ToArray(); // Getting old versions
            foreach (string f in files)
                File.Delete(f);
        }
        catch (System.Exception e)
        {
            System.Console.WriteLine("Exception occured when clearing old versions:\n" + e);
        }
    }

    public static async Task<bool> checkForUpdate()
    {
        try
        {
            //安定版の確認

            HttpClient http = new HttpClient();
            //http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            http.DefaultRequestHeaders.Add("User-Agent", "Nebula Updater");

            var response = await http.GetAsync(new System.Uri("https://api.github.com/repos/Dolly1016/Nebula/releases/latest"), HttpCompletionOption.ResponseContentRead);

            if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
            {
                System.Console.WriteLine("Server returned no data: " + response.StatusCode.ToString());
                return false;
            }
            string json = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(json);

            string[] tagname = data["tag_name"]?.ToString().Split(',');

            if (tagname == null)
            {
                return false;
            }

            if (tagname.Length != 2)
            {
                return false;
            }

            // check version

            int modDiff = NebulaPlugin.PluginVersionForFetch.CompareTo(tagname[0]);
            int amoDiff = NebulaPlugin.AmongUsVersion.CompareTo(tagname[1]);
            if (amoDiff == 0)
            { // Update required
                hasUpdate = (modDiff != 0);
                JToken assets = data["assets"];
                if (!assets.HasValues)
                    return false;

                for (JToken current = assets.First; current != null; current = current.Next)
                {
                    string browser_download_url = current["browser_download_url"]?.ToString();
                    if (browser_download_url != null && current["content_type"] != null)
                    {
                        if (current["content_type"].ToString().Equals("application/x-msdownload") &&
                            browser_download_url.EndsWith(".dll"))
                        {
                            updateURI = browser_download_url;
                            break;
                        }
                    }
                }
            }
            else if ((modDiff != 0) && (amoDiff != 0))
            {
                hasUnprocessableUpdate = true;
            }

            //スナップショットの確認
            http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "Nebula Updater");

            response = await http.GetAsync(new System.Uri("https://api.github.com/repos/Dolly1016/Nebula/releases/tags/snapshot"), HttpCompletionOption.ResponseContentRead);

            if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
            {
                System.Console.WriteLine("Server returned no data: " + response.StatusCode.ToString());
                return false;
            }
            json = await response.Content.ReadAsStringAsync();
            data = JObject.Parse(json);

            string[] body = data["body"]?.ToString().Split(",");

            if (body == null || body.Length != 2)
            {
                return false; // Something went wrong
            }

            // check version
            modDiff = NebulaPlugin.PluginVersionForFetch.CompareTo(body[0]);
            int snapDiff = NebulaPlugin.PluginVisualVersion.CompareTo(body[1]);

            if ((modDiff == 0) && (snapDiff != 0))
            { // Update required
                hasNewestSnapshot = true;
            }

        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex);

        }

        return true;
    }

    public static async Task<bool> downloadUpdate(string updateURI)
    {
        try
        {
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "Nebula Updater");
            var response = await http.GetAsync(new System.Uri(updateURI), HttpCompletionOption.ResponseContentRead);
            if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
            {
                System.Console.WriteLine("Server returned no data: " + response.StatusCode.ToString());
                return false;
            }
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            System.UriBuilder uri = new System.UriBuilder(codeBase);
            string fullname = System.Uri.UnescapeDataString(uri.Path);
            if (File.Exists(fullname + ".old")) // Clear old file in case it wasnt;
                File.Delete(fullname + ".old");

            File.Move(fullname, fullname + ".old"); // rename current executable to old

            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                using (var fileStream = File.Create(fullname))
                { // probably want to have proper name here
                    responseStream.CopyTo(fileStream);
                }
            }
            showPopup(Language.Language.GetString("update.restart"));
            return true;
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex);
        }
        showPopup(Language.Language.GetString("update.failed"));
        return false;
    }
    private static void showPopup(string message)
    {
        setPopupText(message);
        InfoPopup.gameObject.SetActive(true);
    }

    public static void setPopupText(string message)
    {
        if (InfoPopup == null)
            return;
        if (InfoPopup.TextAreaTMP != null)
        {
            InfoPopup.TextAreaTMP.text = message;
        }
    }
}