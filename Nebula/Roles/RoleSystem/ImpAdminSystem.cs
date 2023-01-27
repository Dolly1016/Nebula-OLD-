using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nebula.Roles.RoleSystem
{
    public static class ImpAdminSystem
    {
        static MapCountOverlay? jailerCountOverlay = null;

        static public bool IsJailerCountOverlay(MapCountOverlay overlay) => overlay == jailerCountOverlay;

        static private CustomButton adminButton = null;

        static public void ButtonInitialize(HudManager __instance,bool canMoveWithLookingMap,bool ignoreComm,bool canIdentifyImpostors)
        {
            jailerCountOverlay = null;

            if (adminButton != null)
            {
                adminButton.Destroy();
            }
            if (!canMoveWithLookingMap)
            {
                adminButton = new CustomButton(
                    () =>
                    {
                        RoleSystem.HackSystem.showAdminMap(ignoreComm, canIdentifyImpostors ? AdminPatch.AdminMode.ImpostorsAndDeadBodies : AdminPatch.AdminMode.Default);
                    },
                    () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                    () => { return PlayerControl.LocalPlayer.CanMove; },
                    () => { },
                    __instance.UseButton.fastUseSettings[ImageNames.AdminMapButton].Image,
                    Expansion.GridArrangeExpansion.GridArrangeParameter.None,
                    __instance,
                    Module.NebulaInputManager.abilityInput.keyCode,
                    "button.label.admin"
                );
                adminButton.MaxTimer = 0f;
                adminButton.Timer = 0f;
            }
            else
            {
                adminButton = null;
            }
        }

        static public void CleanUp()
        {
            if (adminButton != null)
            {
                adminButton.Destroy();
                adminButton = null;
            }

            if (jailerCountOverlay)
            {
                GameObject.Destroy(jailerCountOverlay.gameObject);
            }
            jailerCountOverlay = null;

            if (MapBehaviour.Instance) GameObject.Destroy(MapBehaviour.Instance.gameObject);
        }

        public static void OnShowMapTaskOverlay(MapTaskOverlay mapTaskOverlay, Action<Vector2, bool> iconGenerator,bool canMoveWithLookingMap, bool ignoreComm, bool canIdentifyImpostors)
        {
            if (!canMoveWithLookingMap) return;

            if (jailerCountOverlay == null)
            {
                jailerCountOverlay = GameObject.Instantiate(ShipStatus.Instance.MapPrefab.countOverlay);
                jailerCountOverlay.transform.SetParent(MapBehaviour.Instance.transform);
                jailerCountOverlay.transform.localPosition = MapBehaviour.Instance.countOverlay.transform.localPosition;
                jailerCountOverlay.transform.localScale = MapBehaviour.Instance.countOverlay.transform.localScale;
                jailerCountOverlay.gameObject.name = "JailerCountOverlay";

                Transform roomNames;
                if (GameOptionsManager.Instance.CurrentGameOptions.MapId == 0)
                    roomNames = MapBehaviour.Instance.transform.FindChild("RoomNames (1)");
                else
                    roomNames = MapBehaviour.Instance.transform.FindChild("RoomNames");
                Map.MapEditor.MapEditors[GameOptionsManager.Instance.CurrentGameOptions.MapId].MinimapOptimizeForJailer(roomNames, jailerCountOverlay, MapBehaviour.Instance.infectedOverlay);
            }

            jailerCountOverlay.gameObject.SetActive(true);

            Patches.AdminPatch.divMask = ~1;
            Patches.AdminPatch.adminMode = canIdentifyImpostors ? AdminPatch.AdminMode.ImpostorsAndDeadBodies : AdminPatch.AdminMode.Default;
            Patches.AdminPatch.isAffectedByCommAdmin = !ignoreComm;
            Patches.AdminPatch.isStandardAdmin = false;
            Patches.AdminPatch.shouldChangeColor = false;

            mapTaskOverlay.Hide();
        }

        public static void OnMapClose(MapBehaviour mapBehaviour)
        {
            if (jailerCountOverlay != null) jailerCountOverlay.gameObject.SetActive(false);
        }

    }
}
