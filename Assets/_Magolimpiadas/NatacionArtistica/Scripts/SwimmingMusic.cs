using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SwimmingMusic : MonoBehaviour
{
    [Range(0f, 1f)] public float volume = 0.75f;
    public int testMeasures = 8;

    AudioSource font;
    AudioClip test;
    Coroutine fade;

    AudioSource Font
    {
        get
        {
            if (font == null)
            {
                font = GetComponent<AudioSource>();
                font.playOnAwake = false;
                font.spatialBlend = 0f;
            }
            return font;
        }
    }

    public void Prepare(SwimmingRoutine routine)
    {
        if (routine != null && routine.song == null && test == null)
            test = SoundSynthesizer.Music(routine.bpm, Mathf.Max(4, testMeasures), false);
        if (ArcaneGames.AudioManager.Instance != null) ArcaneGames.AudioManager.Instance.PauseMusic();
    }

    public void Touch(SwimmingRoutine routine, RoutineFigure figure, double startDsp, float beforeBeats)
    {
        if (fade != null) { StopCoroutine(fade); fade = null; }
        var f = Font;
        f.Stop();
        f.volume = volume * GameSound.MusicVolume;
        if (routine != null && routine.song != null)
        {
            f.clip = routine.song;
            f.loop = false;
            float fromPoint = figure.songStart - beforeBeats * routine.SecondsPerBeat;
            if (fromPoint >= 0f) { f.time = Mathf.Min(fromPoint, f.clip.length - 0.05f); f.PlayScheduled(startDsp); }
            else { f.time = 0f; f.PlayScheduled(startDsp - fromPoint); }
            return;
        }
        if (test == null) Prepare(routine);
        f.clip = test;
        f.loop = true;
        f.time = 0f;
        f.PlayScheduled(startDsp);
    }

    public void Stop(float seconds)
    {
        if (fade != null) StopCoroutine(fade);
        if (!isActiveAndEnabled) { Font.Stop(); return; }
        fade = StartCoroutine(Fade(seconds));
    }

    public void Drop()
    {
        if (fade != null) { StopCoroutine(fade); fade = null; }
        Font.Stop();
        if (ArcaneGames.AudioManager.Instance != null) ArcaneGames.AudioManager.Instance.ResumeMusic();
    }

    IEnumerator Fade(float seconds)
    {
        var f = Font;
        float v0 = f.volume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            f.volume = Mathf.Lerp(v0, 0f, t / seconds);
            yield return null;
        }
        f.Stop();
        fade = null;
    }
}
