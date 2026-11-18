using UnityEngine;

public static class GameSound
{
    const int Fonts = 12;
    static AudioSource[] fonts;
    static int next;
    static CelebrationSounds celebrations;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Restart() { fonts = null; celebrations = null; next = 0; lastTime.Clear(); }

    public static void Celebrate(GameAudio.Sound s, float volume, float seconds, AudioClip fallback = null, float fade = 0.8f, float tone = 1f)
    {
        if (lastTime.TryGetValue(s, out float t) && Time.unscaledTime - t < 0.15f) return;
        lastTime[s] = Time.unscaledTime;
        var clip = GameAudio.Clip(s, out float v);
        if (clip == null) { clip = fallback; v = 1f; }
        if (clip == null) return;
        Ensure();
        celebrations.Play(clip, Mathf.Clamp01(volume * v) * EffectsVolume, seconds, fade, tone);
    }

    public static void SilenceCelebrations(float fade = 0.5f)
    {
        if (celebrations != null) celebrations.Silence(fade);
    }

    public static bool HasCelebration => celebrations != null && celebrations.AnyPlaying;

    public static float EffectsVolume => ArcaneGames.AudioManager.Instance != null ? ArcaneGames.AudioManager.Instance.SfxVolume : 1f;
    public static float MusicVolume => ArcaneGames.AudioManager.Instance != null ? ArcaneGames.AudioManager.Instance.MusicVolume : 0.8f;

    public static void Play(AudioClip clip, float volume = 1f, float tone = 1f)
    {
        if (clip == null) return;
        var f = Next();
        f.pitch = tone;
        f.PlayOneShot(clip, Mathf.Clamp01(volume) * EffectsVolume);
    }

    static readonly System.Collections.Generic.Dictionary<GameAudio.Sound, float> lastTime = new System.Collections.Generic.Dictionary<GameAudio.Sound, float>();

    public static void Play(GameAudio.Sound s, float volume = 1f, float tone = 1f, AudioClip fallback = null, float separation = 0.05f)
    {
        if (lastTime.TryGetValue(s, out float t) && Time.unscaledTime - t < separation) return;
        lastTime[s] = Time.unscaledTime;
        var clip = GameAudio.Clip(s, out float v);
        if (clip != null) Play(clip, volume * v, tone);
        else Play(fallback, volume, tone);
    }

    public static void PlayAt(GameAudio.Sound s, Vector3 location, float volume = 1f)
    {
        var clip = GameAudio.Clip(s, out float v);
        if (clip != null) AudioSource.PlayClipAtPoint(clip, location, Mathf.Clamp01(volume * v) * EffectsVolume);
    }

    public static void PlayAt(AudioClip clip, double dsp, float volume = 1f)
    {
        if (clip == null) return;
        var f = Next();
        f.Stop();
        f.clip = clip;
        f.pitch = 1f;
        f.volume = Mathf.Clamp01(volume) * EffectsVolume;
        f.PlayScheduled(dsp);
    }

    static AudioSource Next()
    {
        Ensure();
        var f = fonts[next];
        next = (next + 1) % Fonts;
        return f;
    }

    static void Ensure()
    {
        if (fonts != null && fonts[0] != null && celebrations != null) return;
        var go = new GameObject("Sonidos del juego");
        Object.DontDestroyOnLoad(go);
        fonts = new AudioSource[Fonts];
        for (int i = 0; i < Fonts; i++)
        {
            fonts[i] = go.AddComponent<AudioSource>();
            fonts[i].playOnAwake = false;
            fonts[i].spatialBlend = 0f;
        }
        celebrations = go.AddComponent<CelebrationSounds>();
    }
}
