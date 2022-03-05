using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Il2CppSystem;
using Hazel;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnhollowerBaseLib;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Twitch;

namespace Nebula.Module
{
    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.UpdateAnnounceText))]
    public static class AnnouncementPatch
    {
        private static bool ShownFlag = false;
        private static ConfigEntry<int> AnnounceVersion = null;
        private static string Announcement = "";

        private static string FormatString(string str)
        {
            foreach(var role in Roles.Roles.AllRoles)
            {
                str = str.Replace("%COLOR:" + role.Name.ToUpper() + "%", Helpers.csTop(role.Color));
                str = str.Replace("%ROLE:" + role.Name.ToUpper() + "%", Helpers.cs(role.Color,Language.Language.GetString("role."+role.LocalizeName+".name")));
            }
            foreach (var role in Roles.Roles.AllExtraRoles)
            {
                str=str.Replace("%COLOR:" + role.Name.ToUpper() + "%", Helpers.csTop(role.Color));
                str = str.Replace("%ROLE:" + role.Name.ToUpper() + "%", Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name")));
            }

            str = str.Replace("%/COLOR%", "</color>");

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

                string lang = Language.Language.GetLanguage(SaveManager.LastLanguage);
                if (jObj[lang]!=null)
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

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class ModUpdaterButton
    {
        private static void Prefix(MainMenuManager __instance)
        {
            //ハット更新
            //CustomHatLoader.LaunchHatFetcher();

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

                string[] tagname = data["tag_name"]?.ToString().Split(',');

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
