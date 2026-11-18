using UnityEngine;
using UnityEngine.UI;

public class DamageNumbers : MonoBehaviour
{
    static Canvas canvas;
    static Font font;

    Vector3 worldPos;
    float time, duration = 0.9f, rise = 1.4f;
    Text text;
    Color colorBase;

    public static void Damage(Vector3 location, float amount, float multiplier, Color mageColor)
    {
        if (multiplier <= 0f) { Create(location, "Inmune", 24, new Color(0.75f, 0.75f, 0.75f), false); return; }
        string n = Mathf.RoundToInt(amount).ToString();
        if (multiplier > 1f) Create(location, n + "\n<size=22>¡Súper efectivo!</size>", 48, mageColor, true);
        else Create(location, n, 24, new Color(0.78f, 0.78f, 0.78f), false);
    }

    public static void MageDamage(Vector3 location, float amount)
    {
        Create(location, "-" + Mathf.RoundToInt(amount), 30, new Color(1f, 0.3f, 0.3f), false);
    }

    public static void Message(Vector3 location, string text, Color color)
    {
        Create(location, text, 26, color, false);
    }

    static void Create(Vector3 location, string content, int sizeValue, Color color, bool glow)
    {
        if (canvas == null)
        {
            var go = new GameObject("Numeros de daño");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scale = go.AddComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1920, 1080);
            scale.matchWidthOrHeight = 0.5f;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        var t = new GameObject("Numero", typeof(RectTransform));
        t.transform.SetParent(canvas.transform, false);
        var txt = t.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = sizeValue;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.supportRichText = true;
        txt.raycastTarget = false;
        txt.text = content;
        txt.color = color;
        var edge = t.AddComponent<Outline>();
        edge.effectColor = new Color(0f, 0f, 0f, 0.85f);
        edge.effectDistance = new Vector2(2f, -2f);
        if (glow)
        {
            var lightSource = t.AddComponent<Shadow>();
            lightSource.effectColor = new Color(1f, 1f, 1f, 0.6f);
            lightSource.effectDistance = new Vector2(0f, 3f);
        }
        var n = t.AddComponent<DamageNumbers>();
        n.worldPos = location + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
        n.text = txt;
        n.colorBase = color;
        if (glow) { n.duration = 1.2f; n.rise = 1.8f; }
        n.Place();
    }

    void Update()
    {
        time += Time.deltaTime;
        float k = time / duration;
        if (k >= 1f) { Destroy(gameObject); return; }
        var c = colorBase; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        text.color = c;
        float scale = time < 0.12f ? Mathf.Lerp(1.4f, 1f, time / 0.12f) : 1f;
        transform.localScale = Vector3.one * scale;
        Place();
    }

    void Place()
    {
        var cam = WorldSwitcher.Instance != null && WorldSwitcher.Instance.camera2D != null && WorldSwitcher.Instance.camera2D.enabled
            ? WorldSwitcher.Instance.camera2D : Camera.main;
        if (cam == null) return;
        Vector3 p = cam.WorldToScreenPoint(worldPos + Vector3.up * (rise * Mathf.Sqrt(Mathf.Clamp01(time / duration))));
        var rt = (RectTransform)transform;
        var canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, p, null, out Vector2 local);
        rt.anchoredPosition = local;
    }
}
