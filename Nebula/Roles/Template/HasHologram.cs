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
using Nebula.Game;

namespace Nebula.Roles.Template
{
    public class HasHologram : Role
    {
        protected Dictionary<byte, PoolablePlayer> PlayerIcons;

        public override void Initialize(PlayerControl __instance)
        {
            int playerCounter = 0;
            if (PlayerControl.LocalPlayer != null && HudManager.Instance != null)
            {
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    GameData.PlayerInfo data = p.Data;
                    PoolablePlayer player = UnityEngine.Object.Instantiate<PoolablePlayer>(Patches.IntroCutsceneOnDestroyPatch.PlayerPrefab, HudManager.Instance.transform);
                    player.SetSkin(data.DefaultOutfit.SkinId, data.DefaultOutfit.ColorId);
                    player.cosmetics.SetColor(data.DefaultOutfit.ColorId);
                    player.cosmetics.SetBodyColor(data.DefaultOutfit.ColorId);
                    //PlayerControl.SetPlayerMaterialColors(data.DefaultOutfit.ColorId, player.Body);
                    //DestroyableSingleton<HatManager>.Instance.SetSkin(player.Skin.layer, data.DefaultOutfit.SkinId);
                    player.cosmetics.SetHat(data.DefaultOutfit.HatId, data.DefaultOutfit.ColorId);
                    PlayerControl.SetPetImage(p.cosmetics.currentPet.Data, data.DefaultOutfit.ColorId, player.cosmetics.currentPet.rend);
                    player.cosmetics.nameText.text ="";
                    player.SetFlipX(true);
                    PlayerIcons[p.PlayerId] = player;

                    InitializePlayerIcon(player, p.PlayerId,playerCounter);
                }
            }
        }

        public virtual PoolablePlayer GetPlayerIcon(byte playerId)
        {
            return PlayerIcons[playerId];
        }

        public virtual void InitializePlayerIcon(PoolablePlayer player,byte PlayerId,int index){
            Vector3 bottomLeft = new Vector3(-HudManager.Instance.UseButton.transform.localPosition.x, HudManager.Instance.UseButton.transform.localPosition.y, HudManager.Instance.UseButton.transform.localPosition.z);

            player.transform.localPosition = bottomLeft + new Vector3(-0.35f, -0.25f, 0);
            player.transform.localScale = Vector3.one * 0.35f;

            player.gameObject.SetActive(false);
        }

        public override void CleanUp()
        {
            foreach(var icon in PlayerIcons.Values)
                UnityEngine.GameObject.Destroy(icon);
            
            PlayerIcons.Clear();
        }

        protected HasHologram(string name, string localizeName, Color color, RoleCategory category,
            Side side, Side introMainDisplaySide, HashSet<Side> introDisplaySides, HashSet<Side> introInfluenceSides,
            HashSet<Patches.EndCondition> winReasons,
            bool hasFakeTask, VentPermission canUseVents, bool canMoveInVents,
            bool ignoreBlackout, bool useImpostorLightRadius) :
            base(name, localizeName, color, category,
                side, introMainDisplaySide, introDisplaySides, introInfluenceSides,
                winReasons,
                hasFakeTask, canUseVents, canMoveInVents,
                ignoreBlackout, useImpostorLightRadius)
        {
            PlayerIcons = new Dictionary<byte, PoolablePlayer>();
        }
    }
}
