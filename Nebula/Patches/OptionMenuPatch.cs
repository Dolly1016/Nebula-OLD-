using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Diagnostics;

namespace Nebula.Patches
{
    public enum NebulaPictureDest
    {
        AmongUsDirectory,
        MyPictures,
        MyDocuments,
        Desktop
    }
    public enum ProcessorAffinity
    {
        DontCare,
        DualCore,
        TripleCore,
        QuadCore
    }


    public class NebulaOption
    {
        static public ConfigEntry<int> configPictureDest;
        static public ConfigEntry<int> configProcessorAffinity;

        static public string GetPicturePath(NebulaPictureDest dest)
        {
            switch (dest)
            {
                case NebulaPictureDest.AmongUsDirectory:
                    return "Screenshots";
                case NebulaPictureDest.MyPictures:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)+ "\\NebulaOnTheShip";
                case NebulaPictureDest.MyDocuments:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\NebulaOnTheShip";
                case NebulaPictureDest.Desktop:
                    return Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\NebulaOnTheShip";

            }
            return "";
        }

        static public string GetPictureDisplayPath(NebulaPictureDest dest)
        {
            switch (dest)
            {
                case NebulaPictureDest.AmongUsDirectory:
                    return "AmongUsDirectory\\Screenshots";
                case NebulaPictureDest.MyPictures:
                    return "MyPictures\\NebulaOnTheShip";
                case NebulaPictureDest.MyDocuments:
                    return "MyDocuments\\NebulaOnTheShip";
                case NebulaPictureDest.Desktop:
                    return "Desktop\\NebulaOnTheShip";

            }
            return "";
        }

        static private string GetCurrentTimeString()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        static public string CreateDirAndGetPictureFilePath(out string displayPath)
        {
            string dir = GetPicturePath((NebulaPictureDest)configPictureDest.Value);
            displayPath = GetPictureDisplayPath((NebulaPictureDest)configPictureDest.Value);
            string currentTime = GetCurrentTimeString();
            displayPath += "\\" + currentTime + ".png";
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            return  dir+"\\"+ currentTime + ".png";
        }

        static public string GetPictureDestMode()
        {
            switch ((NebulaPictureDest)configPictureDest.Value)
            {
                case NebulaPictureDest.AmongUsDirectory:
                    return Language.Language.GetString("config.option.pictureDest.amongUsDirectory");
                case NebulaPictureDest.MyPictures:
                    return Language.Language.GetString("config.option.pictureDest.myPictures");
                case NebulaPictureDest.MyDocuments:
                    return Language.Language.GetString("config.option.pictureDest.myDocuments");
                case NebulaPictureDest.Desktop:
                    return Language.Language.GetString("config.option.pictureDest.desktop");
            }
            return "";
        }

        static public string GetProcessorAffinityMode()
        {
            switch ((ProcessorAffinity)configProcessorAffinity.Value)
            {
                case ProcessorAffinity.DontCare:
                    return Language.Language.GetString("config.option.processorAffinity.dontCare");
                case ProcessorAffinity.DualCore:
                    return Language.Language.GetString("config.option.processorAffinity.dualCore");
                case ProcessorAffinity.TripleCore:
                    return Language.Language.GetString("config.option.processorAffinity.tripleCore");
                case ProcessorAffinity.QuadCore:
                    return Language.Language.GetString("config.option.processorAffinity.quadCore");
            }
            return "";
        }

        static public void ReflectProcessorAffinity()
        {
            try
            {
                string? affinity = null;
                switch ((ProcessorAffinity)configProcessorAffinity.Value)
                {
                    case ProcessorAffinity.DualCore:
                        affinity = 0b00110.ToString();
                        break;
                    case ProcessorAffinity.TripleCore:
                        affinity = 0b01110.ToString();
                        break;
                    case ProcessorAffinity.QuadCore:
                        affinity = 0b11110.ToString();
                        break;
                }

                if (affinity == null) return;

                var process = System.Diagnostics.Process.GetCurrentProcess();
                string id = process.Id.ToString();
                
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "CPUAffinityEditor.exe";
                processStartInfo.Arguments = id + " " + affinity;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.UseShellExecute = false;
                Process.Start(processStartInfo);
            }
            catch
            {
                NebulaPlugin.Instance.Logger.Print("Error");
            }
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class StartOptionMenuPatch
    {
        
        static public void LoadOption()
        {
            NebulaOption.configPictureDest = NebulaPlugin.Instance.Config.Bind("Config", "PicutureDest", 0);
            NebulaOption.configProcessorAffinity = NebulaPlugin.Instance.Config.Bind("Config", "ProcessorAffinity", 0);
            NebulaOption.ReflectProcessorAffinity();
        }

        static public void UpdateToggleText(this ToggleButtonBehaviour button,bool on,string text)
        {
            button.onState = on;
            Color color = on ? new Color(0f, 1f, 0.16470589f, 1f) : Color.white;
            button.Background.color = color;
            button.Text.text = text + ": " + DestroyableSingleton<TranslationController>.Instance.GetString(button.onState ? StringNames.SettingsOn : StringNames.SettingsOff, new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Object>(0));
            if (button.Rollover)
            {
                button.Rollover.ChangeOutColor(color);
            }
        }

        static public void UpdateButtonText(this ToggleButtonBehaviour button, string text,string state)
        {
            button.onState = false;
            Color color = Color.white;
            button.Background.color = color;
            button.Text.text = text + ": " + state;
            if (button.Rollover)
            {
                button.Rollover.ChangeOutColor(color);
            }
        }

        static ToggleButtonBehaviour debugSnapshot;
        static ToggleButtonBehaviour debugOutputHash;

        static ToggleButtonBehaviour pictureDest;
        static ToggleButtonBehaviour processorAffinity;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            var tabs = new List<TabGroup>(__instance.Tabs.ToArray());

            PassiveButton passiveButton;
            ToggleButtonBehaviour toggleButton;

            //設定項目を追加する

            GameObject nebulaTab = new GameObject("NebulaTab");
            nebulaTab.transform.SetParent(__instance.transform);
            nebulaTab.transform.localScale = new Vector3(1f, 1f, 1f);
            nebulaTab.SetActive(false);

            GameObject applyButtonTemplate = tabs[1].Content.transform.FindChild("ApplyButton").gameObject;
            GameObject toggleButtonTemplate = tabs[0].Content.transform.FindChild("MiscGroup").FindChild("StreamerModeButton").gameObject;

            //Snapshot
            var snapshotButton = GameObject.Instantiate(toggleButtonTemplate, null);
            snapshotButton.transform.SetParent(nebulaTab.transform);
            snapshotButton.transform.localScale = new Vector3(1f, 1f, 1f);
            snapshotButton.transform.localPosition = new Vector3(-1.3f, 1.6f, 0f);
            snapshotButton.name = "SnapshotButton";
            debugSnapshot = snapshotButton.GetComponent<ToggleButtonBehaviour>();
            passiveButton = snapshotButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => debugSnapshot.UpdateToggleText(!debugSnapshot.onState, Language.Language.GetString("config.debug.snapshot"))));

            //OutputHash
            var outputHashButton = GameObject.Instantiate(toggleButtonTemplate, null);
            outputHashButton.transform.SetParent(nebulaTab.transform);
            outputHashButton.transform.localScale = new Vector3(1f, 1f, 1f);
            outputHashButton.transform.localPosition = new Vector3(1.3f, 1.6f, 0f);
            outputHashButton.name = "OutputHashButton";
            debugOutputHash = outputHashButton.GetComponent<ToggleButtonBehaviour>();
            passiveButton = outputHashButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => debugOutputHash.UpdateToggleText(!debugOutputHash.onState, Language.Language.GetString("config.debug.outputHash"))));

            //適用ボタン
            var applyButton = GameObject.Instantiate(applyButtonTemplate, null);
            applyButton.transform.SetParent(nebulaTab.transform);
            applyButton.transform.localScale = new Vector3(1f, 1f, 1f);
            applyButton.transform.localPosition = new Vector3(0f, 1f, 0f);
            applyButton.name = "ApplyButton";
            passiveButton = applyButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                NebulaPlugin.DebugMode.SetToken("Snapshot", debugSnapshot.onState);
                NebulaPlugin.DebugMode.SetToken("OutputHash", debugOutputHash.onState);
                NebulaPlugin.DebugMode.OutputToken();
            }
            ));

            //PictureDest
            var pictureDestButton = GameObject.Instantiate(toggleButtonTemplate, null);
            pictureDestButton.transform.SetParent(nebulaTab.transform);
            pictureDestButton.transform.localScale = new Vector3(1f, 1f, 1f);
            pictureDestButton.transform.localPosition = new Vector3(-1.3f, 0f, 0f);
            pictureDestButton.name = "PictureDest";
            pictureDest = pictureDestButton.GetComponent<ToggleButtonBehaviour>();
            passiveButton = pictureDestButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                NebulaOption.configPictureDest.Value++;
                NebulaOption.configPictureDest.Value %= 4;
                pictureDest.UpdateButtonText(Language.Language.GetString("config.option.pictureDest"),NebulaOption.GetPictureDestMode());
            }));

            //ProcessorAffinity
            var processorAffinityButton = GameObject.Instantiate(toggleButtonTemplate, null);
            processorAffinityButton.transform.SetParent(nebulaTab.transform);
            processorAffinityButton.transform.localScale = new Vector3(1f, 1f, 1f);
            processorAffinityButton.transform.localPosition = new Vector3(1.3f, 0f, 0f);
            processorAffinityButton.name = "ProcessorAffinity";
            processorAffinity = processorAffinityButton.GetComponent<ToggleButtonBehaviour>();
            passiveButton = processorAffinityButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                NebulaOption.configProcessorAffinity.Value++;
                NebulaOption.configProcessorAffinity.Value %= 4;
                processorAffinity.UpdateButtonText(Language.Language.GetString("config.option.processorAffinity"), NebulaOption.GetProcessorAffinityMode());
                NebulaOption.ReflectProcessorAffinity();
            }));

            //タブを追加する

            tabs[tabs.Count - 1] = (GameObject.Instantiate(tabs[1], null));
            var nebulaButton = tabs[tabs.Count - 1];
            nebulaButton.gameObject.name = "NebulaButton";
            nebulaButton.transform.SetParent(tabs[0].transform.parent);
            nebulaButton.transform.localScale = new Vector3(1f, 1f, 1f);
            nebulaButton.Content = nebulaTab;
            var textObj = nebulaButton.transform.FindChild("Text_TMP").gameObject;
            textObj.GetComponent<TextTranslatorTMP>().enabled = false;
            textObj.GetComponent<TMPro.TMP_Text>().text = "NoS";

            passiveButton = nebulaButton.gameObject.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.OpenTabGroup(tabs.Count - 1);

                debugSnapshot.UpdateToggleText(NebulaPlugin.DebugMode.HasToken("Snapshot"),Language.Language.GetString("config.debug.snapshot"));
                debugOutputHash.UpdateToggleText(NebulaPlugin.DebugMode.HasToken("OutputHash"), Language.Language.GetString("config.debug.outputHash"));
                pictureDest.UpdateButtonText(Language.Language.GetString("config.option.pictureDest"), NebulaOption.GetPictureDestMode());
                processorAffinity.UpdateButtonText(Language.Language.GetString("config.option.processorAffinity"), NebulaOption.GetProcessorAffinityMode());

                passiveButton.OnMouseOver.Invoke();
            }
            ));

            float y = tabs[0].transform.localPosition.y, z = tabs[0].transform.localPosition.z;
            if (tabs.Count == 3)
                for (int i = 0; i < 3; i++) tabs[i].transform.localPosition = new Vector3(1.7f * (float)(i - 1), y, z);
            else if (tabs.Count == 4)
                for (int i = 0; i < 4; i++) tabs[i].transform.localPosition = new Vector3(1.62f * ((float)i - 1.5f), y, z);

            __instance.Tabs = new UnhollowerBaseLib.Il2CppReferenceArray<TabGroup>(tabs.ToArray());


        }
    }
}
