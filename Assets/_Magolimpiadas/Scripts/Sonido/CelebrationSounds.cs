using System.Collections.Generic;
using UnityEngine;

public class CelebrationSounds : MonoBehaviour
{
    class Playing
    {
        public AudioSource font;
        public float volume;
        public float end;
        public float fade;
    }

    readonly List<Playing> playing = new List<Playing>();
    readonly List<AudioSource> free = new List<AudioSource>();

    public bool AnyPlaying => playing.Count > 0;

    public void Play(AudioClip clip, float volume, float seconds, float fade, float tone)
    {
        if (clip == null) return;
        AudioSource f;
        if (free.Count > 0) { f = free[free.Count - 1]; free.RemoveAt(free.Count - 1); }
        else
        {
            f = gameObject.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.spatialBlend = 0f;
            f.loop = false;
        }
        f.clip = clip;
        f.pitch = tone;
        f.volume = volume;
        f.Play();
        float lasts = Mathf.Min(seconds, clip.length / Mathf.Max(0.1f, tone));
        playing.Add(new Playing { font = f, volume = volume, end = Time.unscaledTime + lasts, fade = Mathf.Clamp(fade, 0.05f, lasts) });
    }

    public void Silence(float fade)
    {
        fade = Mathf.Max(0.05f, fade);
        float end = Time.unscaledTime + fade;
        foreach (var s in playing)
        {
            if (s.end <= end) continue;
            s.end = end;
            s.fade = fade;
        }
    }

    void Update()
    {
        for (int i = playing.Count - 1; i >= 0; i--)
        {
            var s = playing[i];
            float remaining = s.end - Time.unscaledTime;
            if (s.font == null || remaining <= 0f || (!s.font.isPlaying && !AudioListener.pause))
            {
                if (s.font != null) { s.font.Stop(); s.font.clip = null; free.Add(s.font); }
                playing.RemoveAt(i);
                continue;
            }
            s.font.volume = s.volume * Mathf.Clamp01(remaining / s.fade);
        }
    }
}
