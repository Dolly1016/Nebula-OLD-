using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;

namespace Nebula.Roles.ImpostorRoles
{
    public class Jailer : Role
    {
        /* オプション */
        private Module.CustomOption ignoreCommSabotageOption;

        
        public override void LoadOptionData()
        {
            ignoreCommSabotageOption = CreateOption(Color.white, "ignoreCommSabotage", true);
        }
        

        /* ボタン */
        static private CustomButton adminButton;
        public override void ButtonInitialize(HudManager __instance)
        {
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
                KeyCode.F,
                false,
                "button.label.admin"
            );
            adminButton.MaxTimer = 0f;
            adminButton.Timer = 0f;
        }

        public override void ButtonActivate()
        {
            adminButton.setActive(true);
        }

        public override void ButtonDeactivate()
        {
            adminButton.setActive(false);
        }

        public override void Initialize(PlayerControl __instance)
        {

        }

        public override void GlobalInitialize(PlayerControl __instance)
        {

        }

        public override void CleanUp()
        {
            if (adminButton != null)
            {
                adminButton.Destroy();
                adminButton = null;
            }
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
