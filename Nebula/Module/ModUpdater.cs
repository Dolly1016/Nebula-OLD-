using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Il2CppSystem;
using Hazel;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Twitch;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Nebula.Module
{
    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.UpdateAnnounceText))]
    public static class AnnouncementPatch
    {
        private static bool ShownFlag = false;
        private static ConfigEntry<int> AnnounceVersion = null;
        private static string Announcement = "";

        private static string FormatRoleString(Match match,string str,string key,string defaultString)
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

            str = str.Replace(match.Value,defaultString);
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

            regex = new Regex("%ROLE:[A-Z]+\\([a-zA-Z0-9 ]+\\)%");
            foreach (Match match in regex.Matches(str))
            {
                var split = match.Value.Split(':','(', ')');
                str = FormatRoleString(match, str, split[1], split[2]);
            }

            str = str.Replace("%/COLOR%", "</color>");

            regex = new Regex("%OPTION\\([a-zA-Z\\.0-9]+\\)\\,\\([a-zA-Z\\.0-9 ]+\\)%");
            foreach(Match match in regex.Matches(str))
            {
                var split=match.Value.Split('(',')');

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
                JToken? version = jObj.get_Item("Version");
                if (version == null) return false;
                int Version = int.Parse(version.ToString());

                //既にみたことがあれば出さない
                if (AnnounceVersion.Value == Version)
                {
                    ShownFlag = true;
                }
                //更新する
                AnnounceVersion.Value = Version;

                string lang = Language.Language.GetLanguage(SaveManager.LastLanguage);
                if (jObj.get_Item(lang)!=null)
                    Announcement = jObj.get_Item(lang).ToString();
                else if (jObj.get_Item("English") != null)
                    Announcement = jObj.get_Item("English").ToString();
                else if (jObj.get_Item("Japanese") != null)
                    Announcement = jObj.get_Item("Japanese").ToString();
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

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class ModUpdaterButton
    {
        private static void Prefix(MainMenuManager __instance)
        {
            //More Cosmic 更新
            CosmicLoader.LaunchCosmicFetcher();

            ModUpdater.LaunchUpdater();
            if (!ModUpdater.hasUpdate  && !ModUpdater.hasUnprocessableUpdate) return;

            var template = GameObject.Find("ExitGameButton");
            if (template == null) return;

            var button = UnityEngine.Object.Instantiate(template, null);
            button.transform.localPosition = new Vector3(button.transform.localPosition.x, button.transform.localPosition.y + 0.6f, button.transform.localPosition.z);

            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new Button.ButtonClickedEvent();
            if(ModUpdater.hasUpdate)
                passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)onClick);

            var text = button.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            __instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) => {
                text.SetText(Language.Language.GetString(ModUpdater.hasUnprocessableUpdate ? "title.button.existNewerNebula":"title.button.updateNebula")); ;
            })));

            TwitchManager man = DestroyableSingleton<TwitchManager>.Instance;
            ModUpdater.InfoPopup = UnityEngine.Object.Instantiate<GenericPopup>(man.TwitchPopup);
            ModUpdater.InfoPopup.TextAreaTMP.fontSize *= 0.7f;
            ModUpdater.InfoPopup.TextAreaTMP.enableAutoSizing = false;

            void onClick()
            {
                ModUpdater.ExecuteUpdate();
                button.SetActive(false);
            }
        }
    }

    public class ModUpdater
    {
        public static bool running = false;
        public static bool hasUpdate = false;
        public static bool hasUnprocessableUpdate = false;
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

        public static void ExecuteUpdate()
        {
            string info = Language.Language.GetString("update.pleaseWait");
            ModUpdater.InfoPopup.Show(info); // Show originally
            if (updateTask == null)
            {
                if (updateURI != null)
                {
                    updateTask = downloadUpdate();
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

                string[] tagname = data.get_Item("tag_name")?.ToString().Split(',');

                if (tagname == null)
                {
                    return false; // Something went wrong
                }

                if (tagname.Length != 2)
                {
                    return false;
                }

                // check version

                int modDiff = NebulaPlugin.PluginVersionForFetch.CompareTo(tagname[0]);
                int amoDiff = NebulaPlugin.AmongUsVersion.CompareTo(tagname[1]);
                if ((modDiff != 0) && (amoDiff == 0))
                { // Update required
                    hasUpdate = true;
                    JToken assets = data.get_Item("assets");
                    if (!assets.HasValues)
                        return false;

                    for (JToken current = assets.First; current != null; current = current.Next)
                    {
                        string browser_download_url = current.get_Item("browser_download_url")?.ToString();
                        if (browser_download_url != null && current.get_Item("content_type") != null)
                        {
                            if (current.get_Item("content_type").ToString().Equals("application/x-msdownload") &&
                                browser_download_url.EndsWith(".dll"))
                            {
                                updateURI = browser_download_url;
                                return true;
                            }
                        }
                    }
                }
                else if ((modDiff != 0) && (amoDiff != 0))
                {
                    hasUnprocessableUpdate = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex);

            }
           
            return false;
        }

        public static async Task<bool> downloadUpdate()
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
}
