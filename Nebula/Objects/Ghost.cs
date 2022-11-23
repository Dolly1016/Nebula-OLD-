namespace Nebula.Objects;

public class Ghost
{
    static public Sprite[] Sprites = new Sprite[3], FadeSprites = new Sprite[6];

    public byte Id { get; }

    public static List<Ghost> Ghosts = new List<Ghost>();
    public GameObject GameObject { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public int PassedMeetings { get; set; }
    public bool IsInFadePhase { get; private set; }
    private int Anim { get; set; }
    private float AnimTimer { get; set; }
    public float Timer { get; private set; }
    public bool IsRemoved { get; private set; }

    private static bool LoadedFlag = false;

    static public void Load()
    {
        Sprites[0] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.0.png", 150f);
        Sprites[1] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.1.png", 150f);
        Sprites[2] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.2.png", 150f);

        FadeSprites[0] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.Fade0.png", 150f);
        FadeSprites[1] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.Fade1.png", 150f);
        FadeSprites[2] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.Fade2.png", 150f);
        FadeSprites[3] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.Fade3.png", 150f);
        FadeSprites[4] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.Fade4.png", 150f);
        FadeSprites[5] = Helpers.loadSpriteFromResources("Nebula.Resources.Ghost.Fade5.png", 150f);
    }

    public Ghost(Vector3 pos)
    {
        GameObject = new GameObject("Ghost");

        GameObject.transform.position = new Vector3(pos.x, pos.y, pos.y / 1000f - 1f);
        Renderer = GameObject.AddComponent<SpriteRenderer>();
        Renderer.sprite = Sprites[0];

        PassedMeetings = 0;

        AnimTimer = 0f;
        Timer = 0f;

        IsRemoved = false;

        Ghosts.Add(this);
    }

    public static void Initialize()
    {
        foreach (Ghost ghost in Ghosts)
        {
            if (!ghost.IsRemoved) UnityEngine.Object.Destroy(ghost.GameObject);
        }
        Ghosts.Clear();

        Load();
        LoadedFlag = true;
    }

    public static void Update()
    {
        foreach (Ghost ghost in Ghosts)
        {
            ghost.AnimTimer -= Time.deltaTime;

            if (MeetingHud.Instance == null)
            {
                ghost.Timer += Time.deltaTime;
            }

            if (ghost.AnimTimer < 0f)
            {
                ghost.AnimTimer = 0.1f;

                ghost.Anim++;

                if (ghost.IsInFadePhase)
                {
                    if (ghost.Anim >= 6)
                    {
                        ghost.IsRemoved = true;
                        UnityEngine.Object.Destroy(ghost.GameObject);
                    }
                    else
                        ghost.Renderer.sprite = FadeSprites[ghost.Anim];
                }
                else
                {
                    ghost.Anim = ghost.Anim % 3;
                    ghost.Renderer.sprite = Sprites[ghost.Anim];
                }
            }
        }

        Ghosts.RemoveAll((g) => { return g.IsRemoved; });
    }

    public void Fade()
    {
        if (IsInFadePhase) return;
        IsInFadePhase = true;
        Anim = 0;
        AnimTimer = 0.1f;
        Renderer.sprite = FadeSprites[0];
    }

    public void Remove()
    {
        UnityEngine.Object.Destroy(GameObject);
        IsRemoved = true;
        Ghosts.Remove(this);
    }
}