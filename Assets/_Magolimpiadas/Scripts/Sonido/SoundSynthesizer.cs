using System;
using System.Collections.Generic;
using UnityEngine;

public static class SoundSynthesizer
{
    const int Frequency = 44100;
    static readonly Dictionary<string, AudioClip> done = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Restart() => done.Clear();

    static readonly int[] LaneNote = { 69, 72, 76, 79 };

    public static float Hz(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

    public static AudioClip CorrectHit(int lane, bool perfect)
    {
        int note = LaneNote[Mathf.Clamp(lane, 0, 3)];
        return Make("acierto" + lane + perfect, () =>
        {
            var d = Empty(perfect ? 0.9f : 0.6f);
            Bell(d, 0f, Hz(note), perfect ? 0.9f : 0.6f, perfect ? 1f : 0.45f, 0.55f);
            if (perfect) Bell(d, 0.015f, Hz(note + 12), 0.5f, 0.6f, 0.2f);
            return d;
        });
    }

    public static AudioClip WhiteHit() => Make("blanco", () =>
    {
        var d = Empty(1.4f);
        foreach (int n in LaneNote) Bell(d, 0f, Hz(n), 1.3f, 0.8f, 0.24f);
        Bell(d, 0.06f, Hz(81), 0.9f, 0.9f, 0.18f);
        Glow(d, 0.05f, 0.5f, 0.06f);
        return d;
    });

    public static AudioClip GoldenHit() => Make("dorado", () =>
    {
        var d = Empty(1.1f);
        int[] notes = { 81, 84, 88, 93 };
        for (int i = 0; i < notes.Length; i++) Bell(d, i * 0.06f, Hz(notes[i]), 0.7f, 1f, 0.3f);
        return d;
    });

    public static AudioClip Miss() => Make("fallo", () =>
    {
        var d = Empty(0.3f);
        double phase = 0;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float f = Mathf.Lerp(260f, 95f, Mathf.Clamp01(t / 0.16f));
            phase += 2.0 * Math.PI * f / Frequency;
            d[i] += (float)Math.Sin(phase) * Mathf.Exp(-t * 14f) * 0.6f;
        }
        Noise(d, 0f, 0.08f, 0.12f, 0.15f);
        return d;
    });

    public static AudioClip Trap() => Make("trampa", () =>
    {
        var d = Empty(0.45f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float a = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 110f * t)) + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 116.5f * t));
            d[i] = a * 0.18f * Mathf.Exp(-t * 6f) * Mathf.Min(1f, t / 0.01f);
        }
        return d;
    });

    public static AudioClip SplashDown() => Make("chapuzon", () =>
    {
        var d = Empty(0.9f);
        var random = new System.Random(7);
        float low = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float cut = Mathf.Lerp(0.55f, 0.04f, Mathf.Clamp01(t / 0.6f));
            float r = (float)(random.NextDouble() * 2.0 - 1.0);
            low += (r - low) * cut;
            d[i] = low * Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 5f) * 0.9f;
        }
        for (int b = 0; b < 7; b++)
        {
            float t0 = 0.08f + (float)random.NextDouble() * 0.5f;
            float f0 = 380f + (float)random.NextDouble() * 500f;
            Bubble(d, t0, f0, 0.06f, 0.18f);
        }
        return d;
    });

    public static AudioClip Wave() => Make("ola", () =>
    {
        var d = Empty(1.6f);
        var random = new System.Random(11);
        float low = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float k = t / 1.6f;
            float cut = 0.02f + 0.25f * Mathf.Sin(k * Mathf.PI);
            low += ((float)(random.NextDouble() * 2.0 - 1.0) - low) * cut;
            d[i] = low * Mathf.Sin(k * Mathf.PI) * 0.8f;
        }
        return d;
    });

    public static AudioClip Applause(float seconds = 2.6f) => Make("aplauso" + seconds, () =>
    {
        var d = Empty(seconds);
        var random = new System.Random(3);
        int palms = Mathf.RoundToInt(seconds * 70f);
        for (int p = 0; p < palms; p++)
        {
            float t0 = (float)random.NextDouble() * seconds * 0.92f;
            float wrapping = Mathf.Clamp01(t0 / 0.25f) * Mathf.Clamp01((seconds - t0) / (seconds * 0.6f));
            Palm(d, t0, (0.25f + (float)random.NextDouble() * 0.35f) * wrapping, random);
        }
        return d;
    });

    public static AudioClip Star() => Make("estrella", () =>
    {
        var d = Empty(0.9f);
        int[] notes = { 88, 91, 96 };
        for (int i = 0; i < notes.Length; i++) Bell(d, i * 0.07f, Hz(notes[i]), 0.5f, 0.9f, 0.3f);
        Glow(d, 0.0f, 0.5f, 0.05f);
        return d;
    });

    public static AudioClip Tic(bool strong) => Make("tic" + strong, () =>
    {
        var d = Empty(strong ? 0.7f : 0.25f);
        if (strong) { Bell(d, 0f, Hz(88), 0.6f, 0.9f, 0.35f); Bell(d, 0f, Hz(84), 0.6f, 0.6f, 0.25f); }
        else Bell(d, 0f, Hz(81), 0.2f, 0.5f, 0.4f);
        return d;
    });

    public static AudioClip Hoot() => Make("ulular", () =>
    {
        var d = Empty(0.6f);
        Hoo(d, 0f, 392f, 0.2f);
        Hoo(d, 0.24f, 349f, 0.28f);
        return d;
    });

    public static AudioClip Fanfare() => Make("fanfarria", () =>
    {
        var d = Empty(2.4f);
        float[] times = { 0f, 0.16f, 0.32f, 0.48f };
        int[] notes = { 67, 72, 76, 79 };
        for (int i = 0; i < notes.Length; i++) Metal(d, times[i], Hz(notes[i]), i == 3 ? 1.7f : 0.2f, 0.32f);
        Metal(d, 0.48f, Hz(72), 1.7f, 0.18f);
        Metal(d, 0.48f, Hz(76), 1.7f, 0.18f);
        Timpani(d, 0f, 0.5f); Timpani(d, 0.48f, 0.7f);
        return d;
    });

    public static AudioClip Medal() => Make("medalla", () =>
    {
        var d = Empty(1.6f);
        int[] notes = { 72, 76, 79, 84, 88 };
        for (int i = 0; i < notes.Length; i++) Bell(d, i * 0.05f, Hz(notes[i]), 1.4f, 0.9f, 0.22f);
        Glow(d, 0.1f, 1f, 0.05f);
        return d;
    });

    public static AudioClip Firework(int variant) => Make("cohete" + variant, () =>
    {
        var d = Empty(1.8f);
        var random = new System.Random(20 + variant);
        double phase = 0;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            phase += 2.0 * Math.PI * Mathf.Lerp(70f, 38f, Mathf.Clamp01(t / 0.4f)) / Frequency;
            d[i] += (float)Math.Sin(phase) * Mathf.Exp(-t * 6f) * 0.7f;
        }
        Noise(d, 0f, 0.5f, 0.6f, 0.35f);
        for (int c = 0; c < 40; c++)
        {
            float t0 = 0.15f + (float)random.NextDouble() * 1.4f;
            Noise(d, t0, 0.01f, 0.5f * Mathf.Exp(-t0 * 1.5f), 0.9f);
        }
        return d;
    });

    public static AudioClip Flash() => Make("destello", () =>
    {
        var d = Empty(1.2f);
        Glow(d, 0f, 1.1f, 0.08f);
        Bell(d, 0f, Hz(93), 1f, 0.7f, 0.15f);
        return d;
    });

    public static AudioClip Music(float bpm, int measures, bool festive)
    {
        return Make("musica" + bpm + "_" + measures + festive, () =>
        {
            float spb = 60f / bpm;
            var d = Empty(measures * 4 * spb);
            int[][] chords = festive
                ? new[] { new[] { 60, 64, 67 }, new[] { 59, 62, 67 }, new[] { 57, 60, 64 }, new[] { 57, 60, 65 } }
                : new[] { new[] { 57, 60, 64 }, new[] { 57, 60, 65 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 } };
            int[] lowNotes = festive ? new[] { 36, 43, 45, 41 } : new[] { 45, 41, 48, 43 };
            for (int c = 0; c < measures; c++)
            {
                int k = c % 4;
                float t0 = c * 4 * spb;
                Mattress(d, t0, 4 * spb, chords[k], festive ? 0.07f : 0.05f);
                for (int p = 0; p < 4; p++)
                {
                    float tp = t0 + p * spb;
                    if (festive)
                    {
                        if (p == 0 || p == 2) Timpani(d, tp, p == 0 ? 0.55f : 0.35f);
                        Low(d, tp, Hz(lowNotes[k]), spb * 0.9f, 0.22f);
                        Bell(d, tp, Hz(chords[k][p % 3] + 12), spb * 1.5f, 0.5f, 0.09f);
                        if (c % 2 == 1 && p == 3) Metal(d, tp, Hz(chords[k][2] + 12), spb * 0.9f, 0.08f);
                    }
                    else
                    {
                        if (p == 0 || p == 2 || (p == 3 && c % 2 == 1)) BassDrum(d, tp, 0.7f);
                        if (p == 1 || p == 3) Box(d, tp, 0.35f);
                        Cymbal(d, tp, 0.09f); Cymbal(d, tp + spb * 0.5f, 0.14f);
                        Low(d, tp, Hz(lowNotes[k]), spb * 0.45f, 0.26f);
                        Low(d, tp + spb * 0.5f, Hz(lowNotes[k] + (p == 3 ? 7 : 12)), spb * 0.4f, 0.18f);
                        Bell(d, tp, Hz(chords[k][p % 3] + 12), spb * 0.9f, 0.5f, 0.08f);
                        Bell(d, tp + spb * 0.5f, Hz(chords[k][(p + 1) % 3] + 24), spb * 0.6f, 0.4f, 0.045f);
                    }
                }
            }
            NormalizeSamples(d, 0.8f);
            return d;
        });
    }

    static AudioClip Make(string key, Func<float[]> generate)
    {
        if (done.TryGetValue(key, out var c) && c != null) return c;
        var data = generate();
        NormalizeSamples(data, 0.9f, true);
        c = AudioClip.Create("Sintetizado_" + key, data.Length, 1, Frequency, false);
        c.SetData(data, 0);
        done[key] = c;
        return c;
    }

    static float[] Empty(float seconds) => new float[Mathf.Max(1, Mathf.CeilToInt(seconds * Frequency))];

    static void NormalizeSamples(float[] d, float peak, bool onlyIfPasses = false)
    {
        float max = 0f;
        for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
        if (max <= 0.0001f || (onlyIfPasses && max <= peak)) return;
        float k = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= k;
    }

    static int Sample(float t) => Mathf.RoundToInt(t * Frequency);

    static readonly float[] Partial = { 1f, 2f, 3.01f, 4.17f, 5.43f };

    static void Bell(float[] d, float t0, float f, float dur, float glow, float vol)
    {
        int i0 = Sample(t0), n = Sample(dur);
        int parts = Partial.Length;
        var sen = new double[parts]; var cos = new double[parts]; var rotationS = new double[parts]; var rotationC = new double[parts];
        var amp = new double[parts]; var fall = new double[parts];
        for (int k = 0; k < parts; k++)
        {
            double w = 2 * Math.PI * f * Partial[k] / Frequency;
            if (w >= Math.PI) continue;
            sen[k] = 0; cos[k] = 1; rotationS[k] = Math.Sin(w); rotationC[k] = Math.Cos(w);
            amp[k] = k == 0 ? 1 : glow * new[] { 1.0, 0.45, 0.22, 0.12, 0.07 }[k];
            fall[k] = Math.Exp(-(3.5 + k * 2.5) / Math.Max(0.15, dur) / Frequency);
        }
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            double s = 0;
            for (int k = 0; k < parts; k++)
            {
                if (amp[k] == 0) continue;
                s += amp[k] * sen[k];
                double ns = sen[k] * rotationC[k] + cos[k] * rotationS[k];
                cos[k] = cos[k] * rotationC[k] - sen[k] * rotationS[k];
                sen[k] = ns;
                amp[k] *= fall[k];
            }
            float attack = Mathf.Min(1f, i / (0.003f * Frequency));
            d[i0 + i] += (float)s * attack * vol;
        }
    }

    static void Metal(float[] d, float t0, float f, float dur, float vol)
    {
        int i0 = Sample(t0), n = Sample(dur + 0.15f);
        double phase = 0;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float vib = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Clamp01(t / 0.2f);
            phase += 2.0 * Math.PI * f * vib / Frequency;
            double s = 0;
            for (int k = 1; k <= 6; k++) s += Math.Sin(phase * k) / (k * k * 0.6 + 0.4);
            float env = Mathf.Min(1f, t / 0.04f) * (t < dur ? 1f : Mathf.Exp(-(t - dur) * 25f));
            d[i0 + i] += (float)s * env * vol;
        }
    }

    static void Mattress(float[] d, float t0, float dur, int[] notes, float vol)
    {
        int i0 = Sample(t0), n = Sample(dur);
        foreach (int note in notes)
        {
            double w = 2 * Math.PI * Hz(note) / Frequency;
            double gs = Math.Sin(w), gc = Math.Cos(w);
            double s1 = 0, c1 = 1;
            for (int i = 0; i < n && i0 + i < d.Length; i++)
            {
                float t = i / (float)Frequency;
                float env = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((dur - t) / 0.3f);
                double s2 = 2 * s1 * c1;
                d[i0 + i] += (float)(s1 + 0.25 * s2) * env * vol;
                double ns = s1 * gc + c1 * gs;
                c1 = c1 * gc - s1 * gs;
                s1 = ns;
            }
        }
    }

    static void Low(float[] d, float t0, float f, float dur, float vol)
    {
        int i0 = Sample(t0), n = Sample(dur);
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            double t = i / (double)Frequency;
            double s = 0;
            for (int k = 1; k <= 4; k++) s += Math.Sin(2 * Math.PI * f * k * t) / k;
            float env = Mathf.Min(1f, (float)t / 0.008f) * Mathf.Exp(-(float)t * 3f) * Mathf.Clamp01((dur - (float)t) / 0.02f);
            d[i0 + i] += (float)s * env * vol;
        }
    }

    static void BassDrum(float[] d, float t0, float vol)
    {
        int i0 = Sample(t0), n = Sample(0.35f);
        double phase = 0;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            phase += 2.0 * Math.PI * (45f + 110f * Mathf.Exp(-t * 30f)) / Frequency;
            d[i0 + i] += (float)Math.Sin(phase) * Mathf.Exp(-t * 9f) * vol;
        }
    }

    static void Timpani(float[] d, float t0, float vol)
    {
        int i0 = Sample(t0), n = Sample(0.9f);
        double phase = 0;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            phase += 2.0 * Math.PI * (82f + 30f * Mathf.Exp(-t * 20f)) / Frequency;
            d[i0 + i] += ((float)Math.Sin(phase) + 0.3f * (float)Math.Sin(phase * 1.5)) * Mathf.Exp(-t * 4f) * vol;
        }
        Noise(d, t0, 0.06f, vol * 0.25f, 0.3f);
    }

    static readonly System.Random drumRandom = new System.Random(5);

    static void Box(float[] d, float t0, float vol)
    {
        Noise(d, t0, 0.16f, vol, 0.9f);
        int i0 = Sample(t0), n = Sample(0.12f);
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            d[i0 + i] += Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 30f) * vol * 0.5f;
        }
    }

    static void Cymbal(float[] d, float t0, float vol)
    {
        int i0 = Sample(t0), n = Sample(0.06f);
        float before = 0f;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float r = (float)(drumRandom.NextDouble() * 2.0 - 1.0);
            d[i0 + i] += (r - before) * Mathf.Exp(-t * 70f) * vol;
            before = r;
        }
    }

    static void Noise(float[] d, float t0, float dur, float vol, float high)
    {
        int i0 = Sample(t0), n = Sample(dur);
        float low = 0f;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float r = (float)(drumRandom.NextDouble() * 2.0 - 1.0);
            low += (r - low) * Mathf.Lerp(0.05f, 1f, high);
            d[i0 + i] += low * Mathf.Exp(-t * 4f / Mathf.Max(0.01f, dur)) * vol;
        }
    }

    static void Glow(float[] d, float t0, float dur, float vol)
    {
        var random = new System.Random(13);
        for (int k = 0; k < 14; k++)
        {
            float t = t0 + (float)random.NextDouble() * dur * 0.7f;
            Bell(d, t, 2400f + (float)random.NextDouble() * 2600f, 0.25f, 0.2f, vol * (1f - (t - t0) / dur));
        }
    }

    static void Bubble(float[] d, float t0, float f0, float dur, float vol)
    {
        int i0 = Sample(t0), n = Sample(dur);
        double phase = 0;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            phase += 2.0 * Math.PI * f0 * (1f + t / dur) / Frequency;
            d[i0 + i] += (float)Math.Sin(phase) * Mathf.Sin(Mathf.PI * t / dur) * vol;
        }
    }

    static void Palm(float[] d, float t0, float vol, System.Random random)
    {
        int i0 = Sample(t0), n = Sample(0.03f);
        float before = 0f, low = 0f;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            float r = (float)(random.NextDouble() * 2.0 - 1.0);
            low += (r - low) * 0.5f;
            d[i0 + i] += (low - before) * 2.2f * Mathf.Exp(-t * 120f) * vol;
            before = low;
        }
    }

    static void Hoo(float[] d, float t0, float f, float dur, float vol = 0.45f)
    {
        int i0 = Sample(t0), n = Sample(dur);
        double phase = 0;
        for (int i = 0; i < n && i0 + i < d.Length; i++)
        {
            float t = i / (float)Frequency;
            phase += 2.0 * Math.PI * f * (1f + 0.02f * Mathf.Sin(t * 40f)) / Frequency;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur));
            d[i0 + i] += ((float)Math.Sin(phase) + 0.2f * (float)Math.Sin(phase * 2.0)) * env * env * vol;
        }
    }
}
