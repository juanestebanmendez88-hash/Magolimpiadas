using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinCounter : MonoBehaviour
{
    public int letterSize = 28;
    readonly List<Text> lines = new List<Text>();
    Text title;

    void OnEnable() { Coin.OnChange += Refresh; Refresh(); }
    void OnDisable() { Coin.OnChange -= Refresh; }

    Text NewText(string displayName, int sizeValue, float y)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = sizeValue; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.UpperRight;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        var o = go.AddComponent<Outline>(); o.effectColor = new Color(0f, 0f, 0f, 0.85f); o.effectDistance = new Vector2(2f, -2f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-30f, y);
        rt.sizeDelta = new Vector2(600f, sizeValue * 1.4f);
        return t;
    }

    void Update()
    {
        if (lines.Count != Mage.All.Count) Refresh();
    }

    void Refresh()
    {
        if (title == null) { title = NewText("Monedas", letterSize, -24f); title.color = new Color(1f, 0.85f, 0.3f); title.text = "Monedas"; }
        while (lines.Count < Mage.All.Count) lines.Add(NewText("Monedas mago", letterSize - 4, -24f - (lines.Count + 1) * (letterSize + 4)));
        Coin.Summary(out var winner);
        for (int i = 0; i < lines.Count; i++)
        {
            var m = i < Mage.All.Count ? Mage.All[i] : null;
            lines[i].gameObject.SetActive(m != null && m.data != null);
            if (m == null || m.data == null) continue;
            lines[i].text = (m.data == winner ? "* " : "") + m.data.displayName + "   " + Coin.For(m.data);
            lines[i].color = m.data.color;
        }
    }
}
