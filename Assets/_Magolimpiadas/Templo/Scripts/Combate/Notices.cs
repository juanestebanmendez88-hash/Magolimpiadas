using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Notices : MonoBehaviour
{
    static Notices instance;
    Canvas canvas;
    Font font;
    Text tip;
    float tipUntil;
    readonly Queue<string> pending = new Queue<string>();
    static readonly HashSet<string> seen = new HashSet<string>();
    readonly Dictionary<object, (Text text, Vector3 location)> labels = new Dictionary<object, (Text, Vector3)>();

    static Notices Instance
    {
        get
        {
            if (instance != null) return instance;
            var go = new GameObject("Avisos del templo");
            instance = go.AddComponent<Notices>();
            instance.Create();
            return instance;
        }
    }

    void Create()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        var scale = gameObject.AddComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.matchWidthOrHeight = 0.5f;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tip = CreateText(40, new Color(1f, 0.92f, 0.55f));
        var rt = tip.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -150f);
        tip.gameObject.SetActive(false);
    }

    Text CreateText(int sizeValue, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = sizeValue; t.fontStyle = FontStyle.Bold; t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var edge = go.AddComponent<Outline>();
        edge.effectColor = new Color(0f, 0f, 0f, 0.9f);
        edge.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    public static void Tip(string key, string text)
    {
        if (seen.Contains(key)) return;
        seen.Add(key);
        Instance.pending.Enqueue(text);
    }

    public static void Label(object owner, Vector3 location, string text, Color color, bool visible)
    {
        if (!visible && instance == null) return;
        var a = Instance;
        if (!a.labels.TryGetValue(owner, out var e))
        {
            if (!visible) return;
            e = (a.CreateText(30, color), location);
        }
        e.text.gameObject.SetActive(visible);
        e.text.text = text;
        e.text.color = color;
        a.labels[owner] = (e.text, location);
    }

    void LateUpdate()
    {
        bool in2D = WorldSwitcher.Instance != null && WorldSwitcher.Instance.InWorld2D;
        var hud = TempleHud.Instance;
        if (hud != null && hud.isActiveAndEnabled)
        {
            tip.gameObject.SetActive(false);
            if (in2D && pending.Count > 0 && !hud.VisibleTip) hud.ShowTip(pending.Dequeue(), 4f);
        }
        else if (!tip.gameObject.activeSelf && pending.Count > 0 && in2D)
        {
            tip.text = pending.Dequeue();
            tip.gameObject.SetActive(true);
            tipUntil = Time.time + 4f;
        }
        if (tip.gameObject.activeSelf)
        {
            float remains = tipUntil - Time.time;
            var c = tip.color; c.a = Mathf.Clamp01(remains / 0.5f); tip.color = c;
            if (remains <= 0f || !in2D) tip.gameObject.SetActive(false);
        }

        var cam = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.camera2D : null;
        foreach (var kv in labels)
        {
            var t = kv.Value.text;
            if (!t.gameObject.activeSelf) continue;
            if (cam == null || !cam.enabled) { t.enabled = false; continue; }
            t.enabled = true;
            Vector3 p = cam.WorldToScreenPoint(kv.Value.location);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, p, null, out Vector2 local);
            t.rectTransform.anchoredPosition = local;
        }
    }
}
