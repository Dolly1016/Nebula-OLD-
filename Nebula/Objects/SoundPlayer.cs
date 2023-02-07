namespace Nebula.Objects;

class SoundPlayer
{
    static HashSet<SoundPlayer> players = new HashSet<SoundPlayer>();

    AudioSource audioSource;
    float sec;

    public static void Initialize()
    {
        foreach (var p in players)
        {
            GameObject.Destroy(p.audioSource.gameObject);
        }
        players.Clear();
    }

    private SoundPlayer(Vector2 pos, Module.AudioAsset id, float maxDistance, float minDistance)
    {
        this.audioSource = new GameObject().AddComponent<AudioSource>();

        float v = (SoundManager.SfxVolume + 80) / 80f;
        v = 1f - v;
        v = v * v;
        v = 1f - v;
        this.audioSource.volume = v;

        this.audioSource.transform.position = pos;
        this.audioSource.priority = 0;
        this.audioSource.spatialBlend = 1;
        this.audioSource.clip = Module.AssetLoader.GetAudioClip(id);
        this.audioSource.loop = false;
        this.audioSource.playOnAwake = false;
        this.audioSource.maxDistance = maxDistance;
        this.audioSource.minDistance = minDistance;
        this.audioSource.rolloffMode = UnityEngine.AudioRolloffMode.Linear;
        this.audioSource.PlayOneShot(this.audioSource.clip);
        sec = audioSource.clip.length + 0.1f;
    }

    static public void PlaySound(Vector2 pos, Module.AudioAsset id, float maxDistance, float minDistance)
    {
        if (Constants.ShouldPlaySfx())
        {
            SoundPlayer player = new SoundPlayer(pos, id, maxDistance, minDistance);
            players.Add(player);
        }
    }

    static public AudioSource? PlaySound(Module.AudioAsset id,float volume=0.8f)
    {
        if (Constants.ShouldPlaySfx())
        {
            AudioClip clip = Module.AssetLoader.GetAudioClip(id);
            SoundManager.Instance.StopSound(clip);
            return SoundManager.Instance.PlaySound(clip, false, volume, null);
        }
        return null;
    }

    static public void Update()
    {
        foreach (var p in players)
        {
            p.sec -= Time.deltaTime;
            if (p.sec < 0f)
            {
                GameObject.Destroy(p.audioSource.gameObject);
                p.audioSource = null;
            }
        }
        players.RemoveWhere((p) => p.audioSource == null);
    }
}