using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using Nebula.Utilities;

namespace Nebula.Roles.ImpostorRoles
{
    public class Jailer : Role
    {
        /* オプション */
        private Module.CustomOption ignoreCommSabotageOption;

        MapCountOverlay? jailerCountOverlay = null;

        public bool IsJailerCountOverlay(MapCountOverlay overlay) => overlay == jailerCountOverlay;

        public override void LoadOptionData()
        {
            ignoreCommSabotageOption = CreateOption(Color.white, "ignoreCommSabotage", true);
        }
        

        /* ボタン */
        static private CustomButton adminButton;
        public override void ButtonInitialize(HudManager __instance)
        {
            jailerCountOverlay = null;

            if (adminButton != null)
            {
                adminButton.Destroy();
            }
            adminButton = new CustomButton(
                () =>
                {
                    RoleSystem.HackSystem.showAdminMap(ignoreCommSabotageOption.getBool());
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => { return PlayerControl.LocalPlayer.CanMove; },
                () => {  },
                __instance.UseButton.fastUseSettings[ImageNames.AdminMapButton].Image,
                new Vector3(-1.8f, 0f, 0),
                __instance,
                Module.NebulaInputManager.abilityInput.keyCode,
                false,
                "button.label.admin"
            );
            adminButton.MaxTimer = 0f;
            adminButton.Timer = 0f;
        }
        public override void CleanUp()
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

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Disturber);
            RelatedRoles.Add(Roles.Doctor);
            RelatedRoles.Add(Roles.NiceTrapper);
            RelatedRoles.Add(Roles.EvilTrapper);
            RelatedRoles.Add(Roles.Arsonist);
            RelatedRoles.Add(Roles.Opportunist);
        }

        public override void OnShowMapTaskOverlay(MapTaskOverlay mapTaskOverlay, Action<Vector2, bool> iconGenerator)
        {
            if (jailerCountOverlay == null)
            {
                jailerCountOverlay=GameObject.Instantiate(MapBehaviour.Instance.countOverlay);
                jailerCountOverlay.transform.SetParent(MapBehaviour.Instance.transform);
                jailerCountOverlay.transform.localPosition = MapBehaviour.Instance.countOverlay.transform.localPosition;
                jailerCountOverlay.transform.localScale = MapBehaviour.Instance.countOverlay.transform.localScale;
                jailerCountOverlay.gameObject.name = "JailerCountOverlay";

                Map.MapEditor.MapEditors[PlayerControl.GameOptions.MapId].MinimapOptimizeForJailer(MapBehaviour.Instance.transform.FindChild("RoomNames") , jailerCountOverlay, MapBehaviour.Instance.infectedOverlay);
            }

            jailerCountOverlay.gameObject.SetActive(true);
        }

        /// <summary>
        /// マップを閉じるときに呼び出されます。
        /// </summary>
        [RoleLocalMethod]
        public override void OnMapClose(MapBehaviour mapBehaviour) {
            if (jailerCountOverlay != null) jailerCountOverlay.gameObject.SetActive(false);
        }

        public Jailer()
            : base("Jailer", "jailer", Palette.ImpostorRed, RoleCategory.Impostor, Side.Impostor, Side.Impostor,
                 Impostor.impostorSideSet, Impostor.impostorSideSet,
                 Impostor.impostorEndSet,
                 true, VentPermission.CanUseUnlimittedVent, true, true, true)
        {
            adminButton = null;
        }
    }
}
