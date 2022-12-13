namespace Nebula.Roles.Template;

public class HasAlignedHologram : HasHologram
{
    protected List<byte> activePlayers = new List<byte>();

    public override void Initialize(PlayerControl __instance)
    {
        base.Initialize(__instance);

        activePlayers.Clear();
        UpdatePlayerIcon();
    }

    public override void CleanUp()
    {
        base.CleanUp();

        activePlayers.Clear();
    }
    public override void InitializePlayerIcon(PoolablePlayer player, byte PlayerId, int index)
    {
        player.transform.localPosition = new Vector3(-0.25f, -0.25f, 0) + Vector3.right * index++ * 0.3f;
        player.transform.localScale = Vector3.one * 0.25f;
        player.setSemiTransparent(true);
        player.gameObject.SetActive(true);
    }

    public override void MyPlayerControlUpdate()
    {
        base.MyPlayerControlUpdate();

        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        {
            if (!PlayerIcons.ContainsKey(p.PlayerId)) continue;

            //半透明表示の切り替え
            bool isActive = activePlayers.Any(x => x == p.PlayerId);
            PlayerIcons[p.PlayerId].setSemiTransparent(!isActive);

            if (!PlayerIcons[p.PlayerId].gameObject.active)
            {
                PlayerIcons[p.PlayerId].cosmetics.nameText.text = "";
                PlayerIcons[p.PlayerId].cosmetics.nameText.enabled = false;
            }
        }
    }

    public override void OnMeetingEnd()
    {
        base.OnMeetingEnd();

        UpdatePlayerIcon();
    }

    private void UpdatePlayerIcon()
    {
        int visibleCounter = 0;

        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        {
            if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) { PlayerIcons[p.PlayerId].gameObject.SetActive(false); continue; }
            if (!PlayerIcons.ContainsKey(p.PlayerId)) continue;

            if (p.Data.IsDead || p.Data.Disconnected)
            {
                PlayerIcons[p.PlayerId].gameObject.SetActive(false);
            }
            else
            {
                PlayerIcons[p.PlayerId].gameObject.SetActive(true);
                PlayerIcons[p.PlayerId].transform.localScale = Vector3.one * 0.25f;
                PlayerIcons[p.PlayerId].transform.localPosition = new Vector3(-0.25f, -0.25f, 0) + Vector3.right * visibleCounter * 0.3f;
                visibleCounter++;
            }
        }
    }

    protected HasAlignedHologram(string name, string localizeName, Color color, RoleCategory category,
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
        activePlayers = new List<byte>();
    }
}
