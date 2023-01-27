namespace Nebula.Objects;

public class PlayerList
{
    public static PlayerList? Instance;

    public bool IsOpen { get; private set; }
    GameObject listParent;
    Dictionary<byte, Tuple<GameObject, PoolablePlayer>> allPlayers;
    Coroutine? lastCoroutine = null;

    PassiveButton[] changeTargetButtons;

    public PlayerList(PoolablePlayer playerPrefab)
    {
        listParent = new GameObject("PlayerList");
        listParent.transform.SetParent(HudManager.Instance.gameObject.transform);
        listParent.SetActive(true);
        IsOpen = false;

        allPlayers = new Dictionary<byte, Tuple<GameObject, PoolablePlayer>>();

        Sprite sprite = Helpers.loadSpriteFromResources("Nebula.Resources.PlayerMask.png", 100f);

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            GameObject obj = new GameObject(p.name);
            obj.transform.SetParent(listParent.transform);
            obj.layer = LayerExpansion.GetUILayer();
            var mask = obj.AddComponent<SpriteMask>();
            mask.sprite = sprite;



            var poolable = GameObject.Instantiate(playerPrefab, obj.transform);
            poolable.SetPlayerDefaultOutfit(p);
            poolable.cosmetics.SetMaskType(PlayerMaterial.MaskType.SimpleUI);

            poolable.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            poolable.transform.localPosition = new Vector3(0, -0.2f, 0);

            allPlayers.Add(p.PlayerId, new Tuple<GameObject, PoolablePlayer>(obj, poolable));
        }

        changeTargetButtons = new PassiveButton[2];
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = new GameObject("Button");
            obj.transform.SetParent(listParent.transform);
            obj.layer = LayerExpansion.GetUILayer();
            var button = obj.AddComponent<PassiveButton>();
            var renderer = obj.AddComponent<SpriteRenderer>();
            var collider = obj.AddComponent<BoxCollider2D>();
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.OnMouseOut = new UnityEngine.Events.UnityEvent(); ;
            button.OnMouseOver = new UnityEngine.Events.UnityEvent();
            renderer.sprite = Helpers.loadSpriteFromResources($"Nebula.Resources.ArrowButton{(i == 0 ? "Left" : "Right")}.png", 100f);
            collider.size = new Vector2(0.5f, 0.5f);
            int index = i;
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                Patches.EyesightPatch.ObserverMode = true;
                Patches.EyesightPatch.ChangeObserverTarget(index == 1);
            }));
            changeTargetButtons[i] = button;
        }
        SetParentPosition(-3.5f);

        listParent.SetActive(false);

        Instance = this;
    }

    private void SetParentPosition(float y)
    {
        listParent.transform.localPosition = new Vector3(0, y, -10f);
    }
    private float UpdateParentPosition(float goalY)
    {
        float y = listParent.transform.localPosition.y;
        y += (goalY - y) * Time.deltaTime * 4.5f;
        SetParentPosition(y);
        return Mathf.Abs(goalY - y);
    }

    private IEnumerator CoShow()
    {
        if (IsOpen) yield break;

        listParent.SetActive(true);
        IsOpen = true;

        while (true)
        {
            if (UpdateParentPosition(-2.7f) < 0.005f) break;
            yield return null;
        }
        SetParentPosition(-2.7f);
    }

    public void Show()
    {
        if (lastCoroutine != null) HudManager.Instance.StopCoroutine(lastCoroutine);
        lastCoroutine = HudManager.Instance.StartCoroutine(CoShow().WrapToIl2Cpp());
    }

    private IEnumerator CoClose()
    {
        if (!IsOpen) yield break;

        IsOpen = false;

        while (true)
        {
            if (UpdateParentPosition(-3.5f) < 0.005f) break;
            yield return null;
        }

        listParent.SetActive(false);
    }

    public void Close()
    {
        if (lastCoroutine != null) HudManager.Instance.StopCoroutine(lastCoroutine);
        lastCoroutine = HudManager.Instance.StartCoroutine(CoClose().WrapToIl2Cpp());
    }

    public void ListUpPlayers(Predicate<byte> predicate)
    {
        float x = 0f;
        foreach (var entry in allPlayers)
        {
            if (predicate(entry.Key))
            {
                entry.Value.Item1.SetActive(true);
                entry.Value.Item1.transform.localPosition = new Vector3(x, 0, entry.Value.Item1.transform.localPosition.z);
                x += 0.25f;
            }
            else
            {
                entry.Value.Item1.SetActive(false);
                entry.Value.Item1.transform.localPosition = new Vector3(0, 0, 0);
            }
        }

        x -= 0.25f;

        foreach (var tuple in allPlayers.Values)
        {
            if (tuple.Item1.activeSelf)
            {
                tuple.Item1.transform.localPosition -= new Vector3(x * 0.5f, 0, 0);
            }
        }

        for (int i = 0; i < 2; i++)
            changeTargetButtons[i].transform.localPosition = new Vector3((float)(i * 2 - 1) * (x * 0.5f + 0.4f), 0f, -10f);

    }

    public void SelectPlayer(byte id)
    {
        foreach (var entry in allPlayers)
        {
            entry.Value.Item2.setSemiTransparent(entry.Key != id);
            entry.Value.Item1.transform.localPosition = new Vector3(entry.Value.Item1.transform.localPosition.x, 0, (entry.Key == id) ? -10f : 0f);
        }
    }
}