namespace Nebula.Roles.Template;

public class HasHologram : Role
{
    protected Dictionary<byte, PoolablePlayer> PlayerIcons;
    protected Transform PlayerIconsTop;

    public bool GetIconState(byte playerId) { return PlayerIcons[playerId].gameObject.activeSelf; }

    public override void Initialize(PlayerControl __instance)
    {
        PlayerIconsTop = new GameObject("PlayerIcons").transform;
        PlayerIconsTop.SetParent(HudManager.Instance.UseButton.transform.parent);
        PlayerIconsTop.localScale = new Vector3(1f, 1f, 1f);
        PlayerIconsTop.localPosition = new Vector3(0f, 0f, -10f);
        Expansion.GridArrangeExpansion.AddGridArrangeContent(PlayerIconsTop.gameObject,
            Expansion.GridArrangeExpansion.GridArrangeParameter.LeftSideContent | Expansion.GridArrangeExpansion.GridArrangeParameter.OccupyingLineContent | Expansion.GridArrangeExpansion.GridArrangeParameter.AlwaysVisible);

        int playerCounter = 0;
        if (HudManager.InstanceExists && PlayerControl.LocalPlayer != null)
        {
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                GameData.PlayerInfo data = p.Data;
                PoolablePlayer player = UnityEngine.Object.Instantiate<PoolablePlayer>(Patches.IntroCutsceneOnDestroyPatch.PlayerPrefab, HudManager.Instance.transform);

                player.transform.SetParent(PlayerIconsTop);
                player.cosmetics.ResetCosmetics();
                player.cosmetics.SetColor(data.DefaultOutfit.ColorId);
                player.cosmetics.SetBodyColor(data.DefaultOutfit.ColorId);
                if (data.DefaultOutfit.SkinId != null) player.cosmetics.SetSkin(data.DefaultOutfit.SkinId, data.DefaultOutfit.ColorId);
                if (data.DefaultOutfit.HatId != null) player.cosmetics.SetHat(data.DefaultOutfit.HatId, data.DefaultOutfit.ColorId);
                if (data.DefaultOutfit.VisorId != null) player.cosmetics.SetVisor(data.DefaultOutfit.VisorId, data.DefaultOutfit.ColorId);
                player.cosmetics.SetPetIdle(data.DefaultOutfit.PetId, data.DefaultOutfit.ColorId);
                player.cosmetics.nameText.text = "";
                player.SetFlipX(true);
                PlayerIcons[p.PlayerId] = player;

                InitializePlayerIcon(player, p.PlayerId, playerCounter);
            }
        }
    }

    public virtual PoolablePlayer GetPlayerIcon(byte playerId)
    {
        return PlayerIcons[playerId];
    }

    public virtual void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index)
    {
        player.transform.localPosition = new Vector3(-0.35f, -0.25f, 0);
        player.transform.localScale = Vector3.one * 0.35f;

        player.gameObject.SetActive(false);
    }

    public override void CleanUp()
    {
        foreach (var icon in PlayerIcons.Values)
        {
            try
            {
                UnityEngine.GameObject.Destroy(icon.gameObject);
            }
            catch { }
        }

        PlayerIcons.Clear();

        UnityEngine.GameObject.Destroy(PlayerIconsTop.gameObject);
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
