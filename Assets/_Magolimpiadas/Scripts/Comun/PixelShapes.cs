using System.Collections.Generic;
using UnityEngine;

public static class PixelShapes
{
    static readonly Dictionary<string, Sprite> done = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Restart() => done.Clear();

    public static Sprite Pill(int width, int heightValue, bool cross = false, bool star = false)
    {
        return Make("pastilla" + width + "x" + heightValue + cross + star, width, heightValue, FilterMode.Point, 16f, (x, y) =>
        {
            int r = Mathf.Min(3, heightValue / 2);
            if (OutsideCorner(x, y, width, heightValue, r)) return Color.clear;
            bool edge = OutsideCorner(x, y, width, heightValue, r, 1) || x == 0 || y == 0 || x == width - 1 || y == heightValue - 1;
            if (edge) return new Color(0.35f, 0.35f, 0.4f, 1f);
            if (cross)
            {
                int cx = x - width / 2, cy = y - heightValue / 2;
                if (Mathf.Abs(Mathf.Abs(cx) - Mathf.Abs(cy)) <= 0 && Mathf.Abs(cx) <= heightValue / 2 - 2) return new Color(0.12f, 0.05f, 0.08f, 1f);
            }
            if (star)
            {
                int cx = x - width / 2, cy = y - heightValue / 2;
                if ((cx == 0 && Mathf.Abs(cy) <= 2) || (cy == 0 && Mathf.Abs(cx) <= 2)) return Color.white;
            }
            if (y >= heightValue - 3) return Color.white;
            if (y <= 2) return new Color(0.72f, 0.72f, 0.78f, 1f);
            return new Color(0.9f, 0.9f, 0.93f, 1f);
        });
    }

    public static Sprite Panel()
    {
        const int n = 32;
        if (done.TryGetValue("panel", out var s) && s != null) return s;
        var tex = Texture(n, n, FilterMode.Bilinear);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - n / 2f) - (n / 2f - 8f));
                float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - n / 2f) - (n / 2f - 8f));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(8f - d));
            }
        tex.SetPixels(px); tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        done["panel"] = s;
        return s;
    }

    public static Sprite Glow() => Make("brillo", 64, 64, FilterMode.Bilinear, 64f, (x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
        float a = Mathf.Clamp01(1f - d);
        return new Color(1f, 1f, 1f, a * a);
    });

    public static Sprite Hoop(int sizeValue = 32, int thickness = 3) => Make("aro" + sizeValue + "_" + thickness, sizeValue, sizeValue, FilterMode.Point, sizeValue, (x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(sizeValue / 2f, sizeValue / 2f));
        return d <= sizeValue / 2f && d >= sizeValue / 2f - thickness ? Color.white : Color.clear;
    });

    public static Sprite Disc(int sizeValue = 32) => Make("disco" + sizeValue, sizeValue, sizeValue, FilterMode.Point, sizeValue, (x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(sizeValue / 2f, sizeValue / 2f));
        return d <= sizeValue / 2f ? Color.white : Color.clear;
    });

    public static Sprite Ripple() => Make("onda", 64, 64, FilterMode.Bilinear, 64f, (x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
        float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f);
        return new Color(1f, 1f, 1f, a);
    });

    public static Sprite Beam()
    {
        if (done.TryGetValue("haz", out var s) && s != null) return s;
        const int w = 16, h = 64;
        var tex = Texture(w, h, FilterMode.Bilinear);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float side = 1f - Mathf.Abs(x + 0.5f - w / 2f) / (w / 2f);
                px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(side * 1.6f) * (1f - y / (float)h));
            }
        tex.SetPixels(px); tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        done["haz"] = s;
        return s;
    }

    public static Sprite Petal()
    {
        if (done.TryGetValue("petalo", out var s) && s != null) return s;
        const int w = 32, h = 64;
        var tex = Texture(w, h, FilterMode.Bilinear);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 2.5f) * 0.9f);
            }
        tex.SetPixels(px); tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 32f);
        done["petalo"] = s;
        return s;
    }

    public static Sprite Flash() => Make("destello", 32, 32, FilterMode.Bilinear, 32f, (x, y) =>
    {
        Vector2 d = (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(16f, 16f)) / 16f;
        float a = Mathf.Clamp01(1f - (Mathf.Abs(d.x) * 6f + Mathf.Abs(d.y))) + Mathf.Clamp01(1f - (Mathf.Abs(d.y) * 6f + Mathf.Abs(d.x)));
        a = Mathf.Clamp01(a + Mathf.Clamp01(1f - d.magnitude * 3f));
        return new Color(1f, 1f, 1f, a);
    });

    public static Sprite WaveBand() => Make("ola", 128, 16, FilterMode.Bilinear, 16f, (x, y) =>
    {
        float center = 8f + Mathf.Sin(x / 128f * Mathf.PI * 6f) * 3f;
        float a = Mathf.Clamp01(1f - Mathf.Abs(y + 0.5f - center) / 2.5f);
        return new Color(1f, 1f, 1f, a * 0.9f);
    });

    static Texture2D Texture(int w, int h, FilterMode filter) =>
        new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };

    static Sprite Make(string key, int w, int h, FilterMode filter, float ppu, System.Func<int, int, Color> pixel)
    {
        if (done.TryGetValue(key, out var s) && s != null) return s;
        var tex = Texture(w, h, filter);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = pixel(x, y);
        tex.SetPixels(px);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        s.name = key;
        done[key] = s;
        return s;
    }

    static bool OutsideCorner(int x, int y, int w, int h, int r, int inside = 0)
    {
        int rx = r + inside, x0 = inside, y0 = inside, x1 = w - 1 - inside, y1 = h - 1 - inside;
        if (x < x0 || y < y0 || x > x1 || y > y1) return true;
        int cx = x < x0 + rx ? x0 + rx : x > x1 - rx ? x1 - rx : x;
        int cy = y < y0 + rx ? y0 + rx : y > y1 - rx ? y1 - rx : y;
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy > rx * rx + 0.5f;
    }
}
