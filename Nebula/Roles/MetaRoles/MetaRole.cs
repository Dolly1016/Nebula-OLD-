using System;
using UnityEngine;
using HarmonyLib;

namespace Nebula.Roles.MetaRoles
{
    [HarmonyPatch(typeof(InGamePlayerList), nameof(InGamePlayerList.PopulateButtons))]
    public static class ButtonUpdatePatch
    {
        public static bool Prefix(InGamePlayerList __instance)
        {
            return false;
        }

        private static PoolableBehavior Get(ObjectPoolBehavior objectPool)
        {
            var obj = objectPool.inactiveChildren;
            PoolableBehavior poolableBehavior;
            lock (obj)
            {
                if (objectPool.inactiveChildren.Count == 0)
                {
                    if (objectPool.activeChildren.Count == 0)
                    {
                        objectPool.InitPool(objectPool.Prefab);
                    }
                    else
                    {
                        objectPool.CreateOneInactive(objectPool.Prefab);
                    }
                }
                poolableBehavior = objectPool.inactiveChildren[objectPool.inactiveChildren.Count - 1];
                objectPool.inactiveChildren.RemoveAt(objectPool.inactiveChildren.Count - 1);
                objectPool.activeChildren.Add(poolableBehavior);
                PoolableBehavior poolableBehavior2 = poolableBehavior;
                int num = objectPool.childIndex;
                objectPool.childIndex = num + 1;
                poolableBehavior2.PoolIndex = num;
                if (objectPool.childIndex > objectPool.poolSize)
                {
                    objectPool.childIndex = 0;
                }
            }
            if (objectPool.DetachOnGet)
            {
                poolableBehavior.transform.SetParent(null, false);
            }
            poolableBehavior.gameObject.SetActive(true);
            poolableBehavior.Reset();
            return poolableBehavior;
        }

        public static void Postfix(InGamePlayerList __instance)
        {
            PlayerControl.LocalPlayer.NetTransform.Halt();

            int layer = LayerMask.NameToLayer("KeyMapper");

            __instance.backgroundSprite.gameObject.layer = layer;
            __instance.backgroundSpriteMask.gameObject.layer = layer;

            __instance.backgroundSprite.sortingOrder = 1;
            __instance.backgroundSprite.gameObject.GetComponent<BoxCollider2D>().enabled = false;

            __instance.buttonPool.ReclaimAll();
            int index = 0;
            foreach (var role in Roles.AllRoles)
            {
                if (role.category == RoleCategory.Complex) continue;

                PlayerIdentifierButton component = Get(__instance.buttonPool).GetComponent<PlayerIdentifierButton>();
                Vector3 localPosition;
                localPosition = new Vector3(-1.16f+ (index % 5) * 0.58f, -__instance.buttonHeight * (float)(index / 5), 0f);
                component.transform.localPosition = localPosition;
                component.transform.localScale = new Vector3(0.21f, 1f, 1f);

                component.MaskArea.gameObject.layer = layer;
                component.NameText.gameObject.layer = layer;

                component.transform.FindChild("PoolablePlayer").gameObject.SetActive(false);
                var obj = component.transform.FindChild("actualButton").gameObject;
                obj.layer = layer;
                obj.transform.GetChild(0).gameObject.layer = layer;
                var renderer= obj.GetComponent<SpriteRenderer>();
                var button = obj.GetComponent<PassiveButton>();
                button.transform.localPosition = new Vector3(0, 0, -1f);
                button.OnClick.RemoveAllListeners();
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(()=> {
                    RPCEventInvoker.ImmediatelyChangeRole(PlayerControl.LocalPlayer, role);
                    __instance.SetActive(false);
                }));
                

                component.NameText.text = Helpers.cs(role.Color, Language.Language.GetString("role." + role.LocalizeName + ".name"));
                component.NameText.transform.localScale = new Vector3(1.5f * 1.3f, 1.3f, 1f);
                //component.NameText.transform.localPosition = new Vector3(-0.3f, 0.06f, -10f);
                component.NameText.transform.localPosition = new Vector3(0, 0, 0f);
                component.NameText.alignment = TMPro.TextAlignmentOptions.Center;
                component.NameText.renderer.sortingOrder = 2;
                
                SpriteRenderer[] componentsInChildren = component.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].material.SetInt("_MaskLayer", index + 2);
                    componentsInChildren[i].sortingOrder = 1;
                }
                component.NameText.maskType = TMPro.MaskingTypes.MaskOff;

                //UiElement componentInChildren = component.GetComponentInChildren<UiElement>(true);
                //__instance.controllerNavMenu.ControllerSelectable.Add(componentInChildren);

                index++;
            }
        }
    }

    [HarmonyPatch(typeof(InGamePlayerList), nameof(InGamePlayerList.RefreshMenu))]
    public static class MenuRefreshPatch
    {
        public static bool Prefix(InGamePlayerList __instance)
        {
            __instance.controllerNavMenu.ControllerSelectable.Clear();
            __instance.PopulateButtons();
            if(__instance.controllerNavMenu.ControllerSelectable.Count>0)
                __instance.controllerNavMenu.DefaultButtonSelected = __instance.controllerNavMenu.ControllerSelectable[0];
            return false;
        }
        public static void Postfix(InGamePlayerList __instance)
        {
        }
    }

        

    public class MetaRole : ExtraRole
    {
        static public Color Color = new Color(255 / 255f, 255 / 255f, 255 / 255f);

        private TMPro.TextMeshPro log;
        private Vector3 pos;
        InGamePlayerList list;

        public override void Assignment(Patches.AssignMap assignMap)
        {
            if (Game.GameData.data.GameMode != Module.CustomGameMode.FreePlay) return;

            foreach (var player in Game.GameData.data.AllPlayers.Keys)
            {
                assignMap.AssignExtraRole(player, this.id, 0);
            }
        }

        public override void Initialize(PlayerControl __instance)
        {
            log = UnityEngine.Object.Instantiate(HudManager.Instance.TaskText, HudManager.Instance.transform);
            log.maxVisibleLines = 28;
            log.fontSize = log.fontSizeMin = log.fontSizeMax = 2.5f;
            log.outlineWidth += 0.04f;
            log.autoSizeTextContainer = false;
            log.enableWordWrapping = false;
            log.alignment = TMPro.TextAlignmentOptions.TopRight;
            log.rectTransform.pivot = new Vector2(1.0f,0f);
            log.transform.position = Vector3.zero;
            log.transform.localPosition = new Vector3(5.1f, -2.8f, 0);
            log.transform.localScale = Vector3.one;
            log.color = Palette.White;
            log.enabled = true;

            pos = new Vector3(0f,0f);
        }

        public override void MyUpdate()
        {
            if (log)
            {
                log.text = "";

                log.text += "Distance:" + String.Format("{0:f2}", pos.Distance(PlayerControl.LocalPlayer.transform.position));
            }

            if(Input.GetKeyDown(KeyCode.Keypad1)|| Input.GetKeyDown(KeyCode.Alpha1))
            {
                pos = PlayerControl.LocalPlayer.transform.position;
            }
            if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (list == null)
                {
                    list = UnityEngine.Object.Instantiate(InGamePlayerList.instance,Camera.main.transform);

                    list.enabled = true;
                    list.gameObject.active = true;
                    list.transform.localScale = new Vector3(3f, 0.9f);
                }
                list.SetActive(true);
                list.openPosition += new Vector3(2f, 0f);
            }
            if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (!PlayerControl.LocalPlayer.Data.IsDead)
                    Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer, Game.PlayerData.PlayerStatus.Suicide, false, false);
            }
            if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (PlayerControl.LocalPlayer.Data.IsDead)
                    RPCEventInvoker.RevivePlayer(PlayerControl.LocalPlayer);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (list != null) list.SetActive(false);
            }
        }

        public override void CleanUp()
        {
            if (log)
            {
                UnityEngine.Object.Destroy(log);
            }
            if(list!=null)
            {
                UnityEngine.Object.Destroy(list);
            }
        }

        public override void EditDisplayNameForcely(byte playerId, ref string displayName)
        {
            displayName += Helpers.cs(
                    Color, "⌘");
        }

        public override void EditDisplayName(byte playerId, ref string displayName, bool hideFlag)
        {
            EditDisplayNameForcely(playerId,ref displayName);
        }

        public MetaRole() : base("MetaRole", "metaRole", Color, 1)
        {
            IsHideRole = true;
            ValidGamemode = Module.CustomGameMode.FreePlay;

            log = null;

            list = null;
        }
    }
}
