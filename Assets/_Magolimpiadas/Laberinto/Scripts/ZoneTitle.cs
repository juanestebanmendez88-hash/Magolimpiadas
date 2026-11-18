using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class ZoneTitle : MonoBehaviour
{
    public string title = "Tiro con arco";
    public string subtitle = "Nivel 1";
    public TMP_FontAsset font;
    public Color color = new Color(1f, 0.86f, 0.45f);
    public float duration = 2.2f;
    public bool onlyOnce = true;

    bool shown;
    static Canvas canvas;
    static TextMeshProUGUI titleText, subText;
    static Image line, stripe;
    static Coroutine actual;
    static MonoBehaviour owner;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (shown && onlyOnce) return;
        var j = other.GetComponent<PlayerMage>();
        if (j == null || !j.enabled) return;
        shown = true;
        Show();
    }

    public void Show()
    {
        Create();
        if (actual != null && owner != null) owner.StopCoroutine(actual);
        owner = this;
        actual = StartCoroutine(Animate());
    }

    void Create()
    {
        if (canvas != null) return;
        var go = new GameObject("Títulos de zona");
        DontDestroyOnLoad(go);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 140;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;

        var f = new GameObject("Franja", typeof(RectTransform), typeof(Image));
        f.transform.SetParent(go.transform, false);
        stripe = f.GetComponent<Image>();
        stripe.raycastTarget = false;
        stripe.sprite = Gradient();
        var rf = (RectTransform)f.transform;
        rf.anchorMin = new Vector2(0f, 0.5f); rf.anchorMax = new Vector2(1f, 0.5f); rf.pivot = new Vector2(0.5f, 0.5f);
        rf.anchoredPosition = new Vector2(0f, 255f);
        rf.sizeDelta = new Vector2(0f, 300f);

        subText = CreateText("Subtítulo", go.transform, 44f, new Vector2(0f, 330f));
        titleText = CreateText("Título", go.transform, 120f, new Vector2(0f, 240f));
        var l = new GameObject("Línea", typeof(RectTransform), typeof(Image));
        l.transform.SetParent(go.transform, false);
        line = l.GetComponent<Image>();
        line.raycastTarget = false;
        var rl = (RectTransform)l.transform;
        rl.anchorMin = rl.anchorMax = rl.pivot = new Vector2(0.5f, 0.5f);
        rl.anchoredPosition = new Vector2(0f, 170f);
        rl.sizeDelta = new Vector2(700f, 4f);
        go.SetActive(false);
    }

    TextMeshProUGUI CreateText(string displayName, Transform parent, float sizeValue, Vector2 pos)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = sizeValue;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.outlineWidth = 0.22f;
        t.outlineColor = new Color32(25, 10, 50, 255);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1600f, sizeValue * 1.4f);
        return t;
    }

    static Sprite Gradient()
    {
        const int heightValue = 64;
        var tex = new Texture2D(4, heightValue, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < heightValue; y++)
        {
            float t = Mathf.Abs(y / (heightValue - 1f) - 0.5f) * 2f;
            var c = new Color(0.03f, 0.01f, 0.1f, 0.62f * (1f - t * t));
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, heightValue), new Vector2(0.5f, 0.5f), 100f);
    }

    IEnumerator Animate()
    {
        canvas.gameObject.SetActive(true);
        if (font != null) { titleText.font = font; subText.font = font; }
        titleText.text = title;
        subText.text = subtitle;
        subText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        float entrance = 0.6f, exit = 0.8f;
        for (float t = 0f; t < entrance + duration + exit; t += Time.unscaledDeltaTime)
        {
            float a = t < entrance ? t / entrance : t > entrance + duration ? 1f - (t - entrance - duration) / exit : 1f;
            float s = t < entrance ? Mathf.SmoothStep(0f, 1f, t / entrance) : 1f;
            var c = color; c.a = a;
            titleText.color = c;
            titleText.characterSpacing = Mathf.Lerp(40f, 6f, s);
            var cs = Color.white; cs.a = a * 0.85f;
            subText.color = cs;
            var cl = color; cl.a = a * 0.8f;
            line.color = cl;
            line.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0f, 700f, s), 4f);
            stripe.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        canvas.gameObject.SetActive(false);
        actual = null;
    }
}
