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
                    player.Skin.SetColor(data.DefaultOutfit.ColorId);
                    PlayerControl.SetPlayerMaterialColors(data.DefaultOutfit.ColorId, player.CurrentBodySprite.BodySprite);
                    //PlayerControl.SetPlayerMaterialColors(data.DefaultOutfit.ColorId, player.Body);
                    //DestroyableSingleton<HatManager>.Instance.SetSkin(player.Skin.layer, data.DefaultOutfit.SkinId);
                    player.HatSlot.SetHat(data.DefaultOutfit.HatId, data.DefaultOutfit.ColorId);
                    PlayerControl.SetPetImage(data.DefaultOutfit.PetId, data.DefaultOutfit.ColorId, player.PetSlot);
                    player.NameText.text ="";
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

        public virtual void InitializePlayerIcon(PoolablePlayer player,byte PlayerId,int index){}

        public override void CleanUp()
        {
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
