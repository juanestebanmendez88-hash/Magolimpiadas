using UnityEngine;

public class GameMusic : MonoBehaviour
{
    public bool useFixedTrack;
    public GameAudio.Track fixedTrack = GameAudio.Track.Ceremony;
    public float fadeSeconds = 1.5f;

    readonly AudioSource[] fonts = new AudioSource[2];
    readonly float[] volumes = new float[2];
    int active;
    AudioClip actual;
    PlayerMage playerMage;
    PingPongLevel pingPong;
    SwimmingRace race;
    ArtisticSwimmingLevel artisticSwimming;

    void Awake()
    {
        for (int i = 0; i < 2; i++)
        {
            fonts[i] = gameObject.AddComponent<AudioSource>();
            fonts[i].loop = true;
            fonts[i].playOnAwake = false;
            fonts[i].spatialBlend = 0f;
            fonts[i].volume = 0f;
        }
    }

    void Start()
    {
        playerMage = FindFirstObjectByType<PlayerMage>();
        pingPong = FindFirstObjectByType<PingPongLevel>();
        race = FindFirstObjectByType<SwimmingRace>();
        artisticSwimming = FindFirstObjectByType<ArtisticSwimmingLevel>();
    }

    void OnDestroy()
    {
        if (ArcaneGames.AudioManager.Instance != null) ArcaneGames.AudioManager.Instance.ResumeMusic();
    }

    GameAudio.Track? Choose()
    {
        if (useFixedTrack) return fixedTrack;
        if (artisticSwimming != null && artisticSwimming.Playing) return null;
        if (PodiumCeremony.Instance != null && PodiumCeremony.Instance.InProgress) return GameAudio.Track.Ceremony;
        var world = WorldSwitcher.Instance;
        if (world != null && world.InWorld2D) return GameAudio.Track.Temple;
        if (pingPong != null && pingPong.Playing) return GameAudio.Track.PingPong;
        if (race != null && race.InProgress) return GameAudio.Track.Swimming;
        if (playerMage != null && playerMage.InLabyrinth) return GameAudio.Track.Labyrinth;
        return GameAudio.Track.Island;
    }

    void Update()
    {
        var am = ArcaneGames.AudioManager.Instance;
        if (am != null && am.IsMusicPlaying) am.PauseMusic();

        var track = Choose();
        float volume = 0f;
        var clip = track.HasValue ? GameAudio.Music(track.Value, out volume) : null;
        if (clip != actual)
        {
            actual = clip;
            active = 1 - active;
            var f = fonts[active];
            f.Stop();
            f.clip = clip;
            f.volume = 0f;
            if (clip != null) f.Play();
        }
        float target = clip != null ? volume * GameSound.MusicVolume : 0f;
        float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeSeconds);
        for (int i = 0; i < 2; i++)
        {
            float meta = i == active ? target : 0f;
            volumes[i] = Mathf.MoveTowards(volumes[i], meta, step * Mathf.Max(0.05f, Mathf.Max(meta, volumes[i])));
            fonts[i].volume = volumes[i];
            if (i != active && volumes[i] <= 0.001f && fonts[i].isPlaying) fonts[i].Stop();
        }
    }
}
