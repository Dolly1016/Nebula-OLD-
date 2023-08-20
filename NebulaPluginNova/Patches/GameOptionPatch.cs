using HarmonyLib;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Il2CppSystem.Security.Cryptography;
using Nebula.Configuration;
using Nebula.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.RemoteConfigSettingsHelper;

namespace Nebula.Patches;

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
public class GameSettingMenuStartPatch
{
    public static ISpriteLoader nebulaTabSprite = SpriteLoader.FromResource("Nebula.Resources.TabIcon.png",100f);
    public static ISpriteLoader presetTabSprite = SpriteLoader.FromResource("Nebula.Resources.TabIconPreset.png", 100f);
    private static GameObject CreateTab(GameSettingMenu __instance,string tabName,Sprite tabSprite)
    {
        var tabs = __instance.Tabs.transform;

        var roleTab = tabs.FindChild("RoleTab");

        var customTab = UnityEngine.Object.Instantiate(roleTab, roleTab.transform.parent);
        customTab.gameObject.name = tabName;
        customTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = tabSprite;

        return customTab.gameObject;
    }

    private static GameObject CreateSetting(GameSettingMenu __instance, string name)
    {
        return UnityHelper.CreateObject(name, __instance.transform, new Vector3(0, 0, -5f));
    }

    public static void Postfix(GameSettingMenu __instance)
    {
        var tabs = __instance.Tabs.transform;

        //Vanilla役職タブを無効化
        tabs.FindChild("RoleTab").gameObject.SetActive(false);

        var nebulaTab = CreateTab(__instance,"NebulaTab", nebulaTabSprite.GetSprite());
        var presetTab = CreateTab(__instance,"PresetTab", presetTabSprite.GetSprite());

        var nebulaSetting = CreateSetting(__instance,"NebulaSetting");
        var presetSetting = CreateSetting(__instance,"PresetSetting");

        nebulaSetting.AddComponent<NebulaSettingMenu>();
        presetSetting.AddComponent<PresetSettingMenu>();

        nebulaSetting.SetActive(false);
        presetSetting.SetActive(false);


        GameObject[] usingTabs = { tabs.FindChild("GameTab").gameObject, nebulaTab, presetTab };
        GameObject[] settingObj = { __instance.RegularGameSettings, nebulaSetting, presetSetting };
        SpriteRenderer[] highlights = usingTabs.Select(tab => tab.transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>()).ToArray();

        for (int i = 0; i < usingTabs.Length; i++)
        {
            usingTabs[i].SetActive(true);
            usingTabs[i].transform.localPosition = new Vector3((float)i - (float)(usingTabs.Length - 1) / 2f, 0, -5f);

            var button = highlights[i].GetComponent<PassiveButton>();
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            int copiedIndex = i;
            button.OnClick.AddListener(() =>
            {
                SoundManager.Instance.PlaySound(VanillaAsset.SelectClip, false, 0.8f);

                foreach (var setting in settingObj) setting.SetActive(false);
                foreach (var highlight in highlights) highlight.enabled = false;

                settingObj[copiedIndex].SetActive(true);
                highlights[copiedIndex].enabled = true;
            });

            __instance.RolesSettings.gameObject.SetActive(false);
        }
    }
}