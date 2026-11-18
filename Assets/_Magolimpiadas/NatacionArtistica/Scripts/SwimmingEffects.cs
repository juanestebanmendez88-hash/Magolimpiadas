using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SwimmingEffects : MonoBehaviour
{
    public string[] laneId = { "elemental", "nebulosa", "druida", "rayo" };
    public TMP_FontAsset font;
    public float effectsScale = 1f;
    public int effectsOrder = 400;
    public int textsOrder = 650;

    class Floating { public TextMeshPro text; public Vector3 fromPoint; public float t, dur; public bool active; }
    readonly List<Floating> floating = new List<Floating>();

    int Layer => gameObject.layer;

    Vector3 Front(Vector3 p) => new Vector3(p.x, p.y, transform.position.z - 1f);

    void Effect(MagicEffects.Effect2D e, MagicEffects.Color2D c, Vector3 pos, float scale, float speed = 1f) =>
        MagicEffects.Launch2D(e, c, Front(pos), effectsScale * scale, effectsOrder, Layer, transform, speed);

    public void Judgment(Vector3 bullseye, Vector3 swimmer, int lane, NoteJudgment judgment, NoteType type)
    {
        if (type == NoteType.Golden && judgment == NoteJudgment.Miss) return;
        string id = lane >= 0 && lane < laneId.Length ? laneId[lane] : "";
        var color = MagicEffects.MageColor(id);
        if (judgment == NoteJudgment.Miss)
        {
            CreateText("Fallo", bullseye + Vector3.up * 0.9f, new Color(0.75f, 0.75f, 0.85f), 4.5f);
            Effect(MagicEffects.Effect2D.Burst, MagicEffects.Color2D.Gray, bullseye, 0.6f, 1.4f);
            GameSound.Play(GameAudio.Sound.FailedNote, 0.4f, 1f, SoundSynthesizer.Miss(), 0.1f);
            return;
        }
        bool perfect = judgment == NoteJudgment.Perfect;
        switch (type)
        {
            case NoteType.White:
                CreateText(perfect ? "¡Sincronía perfecta!" : "¡Sincronía!", bullseye + Vector3.up * 1.2f, new Color(1f, 0.97f, 0.85f), 5.5f);
                Effect(MagicEffects.Effect2D.BigBurst, MagicEffects.Color2D.Gray, swimmer, 1.8f);
                GameSound.Play(SoundSynthesizer.WhiteHit(), 0.8f);
                break;
            case NoteType.Golden:
                CreateText("¡Dorada!", bullseye + Vector3.up * 1f, new Color(1f, 0.82f, 0.3f), 5f);
                Effect(MagicEffects.Effect2D.Star, MagicEffects.Color2D.Orange, swimmer, 1.2f);
                GameSound.Play(SoundSynthesizer.GoldenHit(), 0.75f);
                break;
            default:
                CreateText(perfect ? "¡Perfecto!" : "Bien", bullseye + Vector3.up * 0.9f, perfect ? new Color(1f, 0.9f, 0.45f) : new Color(0.7f, 0.9f, 1f), perfect ? 4.8f : 4f);
                if (perfect) Effect(MagicEffects.Effect2D.Rays, color, bullseye, 1.1f, 1.3f);
                else Effect(MagicEffects.Effect2D.Flash, color, bullseye, 0.8f, 1.6f);
                GameSound.Play(SoundSynthesizer.CorrectHit(lane, perfect), perfect ? 0.7f : 0.45f);
                break;
        }
    }

    public void Trap(Vector3 bullseye, Vector3 swimmer)
    {
        CreateText("¡Trampa!", bullseye + Vector3.up * 0.9f, new Color(1f, 0.4f, 0.4f), 5f);
        Effect(MagicEffects.Effect2D.Burst, MagicEffects.Color2D.Red, bullseye, 0.8f, 1.4f);
        GameSound.Play(GameAudio.Sound.FailedNote, 0.55f, 0.8f, SoundSynthesizer.Trap(), 0.1f);
    }

    public void Star(Vector3 location)
    {
        CreateText("¡Estrella!", location, new Color(1f, 0.85f, 0.35f), 5f);
        Effect(MagicEffects.Effect2D.Flash, MagicEffects.Color2D.Orange, location, 1f, 1.4f);
        GameSound.Play(SoundSynthesizer.Star(), 0.6f);
    }

    public void SplashDown(Vector3 location)
    {
        Effect(MagicEffects.Effect2D.Ring, MagicEffects.Color2D.Blue, location, 1.1f, 1.4f);
        GameSound.Play(GameAudio.Sound.Stroke, 0.7f, Random.Range(0.92f, 1.08f), SoundSynthesizer.SplashDown(), 0.08f);
    }

    public void Wave(Vector3 location)
    {
        CreateText("¡Ola!", location, new Color(0.7f, 0.9f, 1f), 6f);
        GameSound.Play(SoundSynthesizer.Wave(), 0.6f);
    }

    public void Circle(Vector3 location, bool final)
    {
        if (final) Effect(MagicEffects.Effect2D.Whirlpool, MagicEffects.Color2D.Violet, location, 3f);
        else Effect(MagicEffects.Effect2D.Ring, MagicEffects.Color2D.Blue, location, 2.4f);
        GameSound.Play(GameAudio.Sound.Checkpoint, 0.6f, 1f, SoundSynthesizer.Flash());
    }

    public void Firework(Vector3 location, int color)
    {
        var colors = new[] { MagicEffects.Color2D.Orange, MagicEffects.Color2D.Violet, MagicEffects.Color2D.Green, MagicEffects.Color2D.Blue };
        Effect(MagicEffects.Effect2D.BigBurst, colors[Mathf.Abs(color) % colors.Length], location, 1.6f);
        GameSound.Play(SoundSynthesizer.Firework(Mathf.Abs(color) % 3), 0.3f, Random.Range(0.9f, 1.1f));
    }

    public void CreateText(string text, Vector3 location, Color color, float size)
    {
        Floating f = floating.Find(x => !x.active);
        if (f == null)
        {
            var go = new GameObject("Texto flotante");
            go.transform.SetParent(transform, false);
            go.layer = Layer;
            var t = go.AddComponent<TextMeshPro>();
            if (font != null) t.font = font;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(20, 8, 40, 255);
            t.sortingOrder = textsOrder;
            t.rectTransform.sizeDelta = new Vector2(12f, 2f);
            f = new Floating { text = t };
            floating.Add(f);
        }
        f.text.gameObject.SetActive(true);
        f.text.text = text;
        f.text.fontSize = size;
        f.text.color = color;
        f.fromPoint = Front(location);
        f.t = 0f;
        f.dur = 0.7f;
        f.active = true;
        f.text.transform.position = f.fromPoint;
    }

    void Update()
    {
        foreach (var f in floating)
        {
            if (!f.active) continue;
            f.t += Time.deltaTime;
            float k = f.t / f.dur;
            f.text.transform.position = f.fromPoint + Vector3.up * (0.8f * (1f - (1f - k) * (1f - k)));
            float s = k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) / 0.2f);
            f.text.transform.localScale = Vector3.one * Mathf.Max(0f, s);
            var c = f.text.color; c.a = Mathf.Clamp01((1f - k) * 3f); f.text.color = c;
            if (k >= 1f) { f.active = false; f.text.gameObject.SetActive(false); }
        }
    }

    public void ClearTexts()
    {
        foreach (var f in floating) { f.active = false; if (f.text != null) f.text.gameObject.SetActive(false); }
    }
}
