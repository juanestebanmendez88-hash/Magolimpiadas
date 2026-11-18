using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TempleHud : MonoBehaviour
{
    public static TempleHud Instance { get; private set; }

    public Image portrait;
    public TMP_Text displayName;
    public RectTransform healthFill;
    public Image healthImage;
    public TMP_Text healthText;
    public Image bowBackground, powerBackground;
    public Image bowIcon, powerIcon;
    public TMP_Text bowText, powerText;
    public Sprite normalBackground, activeBackground;
    public Sprite[] arrows = new Sprite[4];

    public CanvasGroup actionGroup;
    public TMP_Text actionText;
    public GameObject secondaryPanel;

    public CanvasGroup tipGroup;
    public TMP_Text mainTip;
    public TMP_Text secondaryTip;
    public GameObject tipDivider;

    public RectTransform coinsRows;
    public TMP_FontAsset font;
    public Sprite star;

    public Image nextPortrait;
    public CanvasGroup bottomGroup;

    public RectTransform magePanel;
    public RectTransform tipPanel;
    public RectTransform coinsPanel;
    public RectTransform leftColumn;

    Mage shown;
    float arrangedWidth = -1f;
    float shownHealth = 1f;
    float tipUntil, tipAlpha;
    readonly List<(TMP_Text displayName, TMP_Text count, Image star)> rows = new List<(TMP_Text, TMP_Text, Image)>();
    readonly Dictionary<Sprite, Sprite> portraits = new Dictionary<Sprite, Sprite>();
    CharacterSwitcher change;

    public bool VisibleTip => tipAlpha > 0.01f || Time.time < tipUntil;

    void Awake()
    {
        Instance = this;
        if (tipGroup != null) tipGroup.alpha = 0f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable() => Coin.OnChange += RefreshCoins;
    void OnDisable() => Coin.OnChange -= RefreshCoins;

    public void ShowTip(string text, float seconds)
    {
        string principal = text ?? "", secondary = "";
        int jump = principal.IndexOf('\n');
        if (jump >= 0) { secondary = principal.Substring(jump + 1); principal = principal.Substring(0, jump); }
        else if (principal.EndsWith(")") && principal.LastIndexOf(" (") > 0)
        {
            int i = principal.LastIndexOf(" (");
            secondary = principal.Substring(i + 2, principal.Length - i - 3);
            principal = principal.Substring(0, i);
        }
        if (mainTip != null) mainTip.text = principal;
        if (secondaryTip != null) secondaryTip.text = secondary;
        if (tipDivider != null) tipDivider.SetActive(!string.IsNullOrEmpty(secondary));
        tipUntil = Time.time + seconds;
    }

    void Update()
    {
        Arrange();
        Down();
        var world = WorldSwitcher.Instance;
        var mage = world != null ? world.player2D : null;
        if (mage != shown) ChangeMage(mage);
        if (mage == null || mage.data == null) return;
        Health(mage);
        Mode(mage);
        MageAction(mage);
        Tip();
        if (rows.Count != Mage.All.Count) RefreshCoins();
    }

    void Arrange()
    {
        var canvas = transform as RectTransform;
        if (canvas == null || tipPanel == null) return;
        float width = canvas.rect.width;
        if (width <= 1f || Mathf.Abs(width - arrangedWidth) < 0.5f) return;
        arrangedWidth = width;
        const float margin = 16f, max = 680f, min = 520f, heightValue = 110f;
        float leftEdge = magePanel != null ? magePanel.anchoredPosition.x + magePanel.rect.width + margin : margin;
        float right = coinsPanel != null ? width + coinsPanel.anchoredPosition.x - coinsPanel.rect.width - margin : width - margin;
        float free = right - leftEdge;
        tipPanel.anchorMin = tipPanel.anchorMax = tipPanel.pivot = new Vector2(0f, 1f);
        if (free >= min)
        {
            float w = Mathf.Min(max, free);
            tipPanel.anchoredPosition = new Vector2(leftEdge + (free - w) * 0.5f, magePanel != null ? magePanel.anchoredPosition.y : -14f);
            tipPanel.sizeDelta = new Vector2(w, heightValue);
            return;
        }
        float lowerLeft = leftColumn != null ? leftColumn.anchoredPosition.x + leftColumn.rect.width + margin : margin;
        float y = leftColumn != null ? leftColumn.anchoredPosition.y : -140f;
        if (magePanel != null) y = Mathf.Min(y, magePanel.anchoredPosition.y - magePanel.rect.height - margin);
        float freeBelow = right - lowerLeft;
        if (freeBelow < min) { lowerLeft = margin; freeBelow = width - 2f * margin; y -= 70f; }
        float wa = Mathf.Min(max, freeBelow);
        tipPanel.anchoredPosition = new Vector2(lowerLeft + (freeBelow - wa) * 0.5f, y);
        tipPanel.sizeDelta = new Vector2(wa, heightValue);
    }

    void Down()
    {
        if (bottomGroup == null) return;
        bool speaks = OwlNotice.Owl != null && OwlNotice.Owl.IsOpen;
        bottomGroup.alpha = Mathf.MoveTowards(bottomGroup.alpha, speaks ? 0f : 1f, Time.unscaledDeltaTime * 5f);
    }

    void ChangeMage(Mage mage)
    {
        shown = mage;
        if (mage == null || mage.data == null) return;
        int i = ColorIndex(mage.data.element);
        if (displayName != null) { displayName.text = mage.data.displayName; displayName.color = Color.Lerp(mage.data.color, Color.white, 0.25f); }
        if (portrait != null) portrait.sprite = Portrait(mage);
        if (bowIcon != null && i < arrows.Length) bowIcon.sprite = arrows[i];
        var icons = ElementsIcons.Load();
        if (powerIcon != null && icons != null) powerIcon.sprite = icons.For(mage.data.element);
        if (secondaryPanel != null) secondaryPanel.SetActive(mage.data.hasSecondaryPower);
        if (nextPortrait != null)
        {
            var next = Next(mage);
            nextPortrait.enabled = next != null;
            if (next != null) nextPortrait.sprite = Portrait(next);
        }
        shownHealth = mage.MaxHealth > 0f ? mage.Health / mage.MaxHealth : 1f;
    }

    void Health(Mage mage)
    {
        float meta = mage.MaxHealth > 0f ? Mathf.Clamp01(mage.Health / mage.MaxHealth) : 0f;
        shownHealth = Mathf.MoveTowards(shownHealth, meta, Time.deltaTime * 1.5f);
        if (healthFill != null) healthFill.anchorMax = new Vector2(shownHealth, 1f);
        if (healthImage != null)
            healthImage.color = shownHealth > 0.5f ? new Color(0.35f, 0.9f, 1f) : shownHealth > 0.25f ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.35f, 0.35f);
        if (healthText != null)
            healthText.text = mage.Alive ? Mathf.CeilToInt(mage.Health) + "/" + Mathf.CeilToInt(mage.MaxHealth) : "¡Caíste!";
    }

    void Mode(Mage mage)
    {
        bool bow = mage.Mode == MageMode.Bow;
        Segment(bowBackground, bowIcon, bowText, bow);
        Segment(powerBackground, powerIcon, powerText, !bow);
    }

    void Segment(Image background, Image icon, TMP_Text text, bool active)
    {
        if (background != null) { background.sprite = active ? activeBackground : normalBackground; background.color = active ? Color.white : new Color(1f, 1f, 1f, 0.75f); }
        if (icon != null) icon.color = active ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        if (text != null) text.color = active ? new Color(1f, 0.92f, 0.6f) : new Color(0.75f, 0.72f, 0.85f, 0.8f);
    }

    void MageAction(Mage mage)
    {
        PowerZone near = null;
        float best = float.MaxValue;
        foreach (var z in PowerZone.All)
        {
            if (z == null || !z.Ready) continue;
            float d = Vector2.Distance(mage.transform.position, z.Area.center);
            if (d <= z.messageDistance && d < best) { best = d; near = z; }
        }
        string text;
        float alpha;
        if (near == null) { text = "Poder de zona"; alpha = 0.6f; }
        else if (near.BelongsTo(mage)) { text = Uppercase(near.action); alpha = 1f; }
        else
        {
            string who = near.requiredMage != null ? near.requiredMage.displayName : "otro mago";
            text = "Lo hace " + who;
            alpha = 0.7f;
        }
        if (actionText != null) actionText.text = text;
        if (actionGroup != null) actionGroup.alpha = Mathf.MoveTowards(actionGroup.alpha, alpha, Time.deltaTime * 4f);
    }

    void Tip()
    {
        float meta = Time.time < tipUntil ? 1f : 0f;
        tipAlpha = Mathf.MoveTowards(tipAlpha, meta, Time.deltaTime * 3f);
        if (tipGroup != null)
        {
            tipGroup.alpha = tipAlpha;
            tipGroup.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, tipAlpha);
        }
    }

    void RefreshCoins()
    {
        if (coinsRows == null) return;
        while (rows.Count < Mage.All.Count) rows.Add(NewRow(rows.Count));
        Coin.Summary(out var winner);
        for (int i = 0; i < rows.Count; i++)
        {
            var m = i < Mage.All.Count ? Mage.All[i] : null;
            bool ver = m != null && m.data != null;
            rows[i].displayName.gameObject.SetActive(ver);
            rows[i].count.gameObject.SetActive(ver);
            rows[i].star.gameObject.SetActive(ver && m.data == winner && Coin.For(m.data) > 0);
            if (!ver) continue;
            rows[i].displayName.text = m.data.displayName;
            rows[i].displayName.color = Color.Lerp(m.data.color, Color.white, 0.2f);
            rows[i].count.text = Coin.For(m.data).ToString();
        }
    }

    (TMP_Text, TMP_Text, Image) NewRow(int i)
    {
        float y = -i * 34f;
        var n = CreateText("Nombre " + (i + 1), new Vector2(30f, y), new Vector2(200f, 32f), TextAlignmentOptions.Left);
        var c = CreateText("Cuenta " + (i + 1), new Vector2(232f, y), new Vector2(46f, 32f), TextAlignmentOptions.Right);
        var go = new GameObject("Estrella " + (i + 1), typeof(RectTransform), typeof(Image));
        go.transform.SetParent(coinsRows, false);
        var img = go.GetComponent<Image>();
        img.sprite = star;
        img.color = new Color(1f, 0.85f, 0.3f);
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(6f, y - 7f);
        rt.sizeDelta = new Vector2(18f, 18f);
        return (n, c, img);
    }

    TMP_Text CreateText(string objectName, Vector2 pos, Vector2 sizeValue, TextAlignmentOptions align)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(coinsRows, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = 24f;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(15, 8, 35, 255);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return t;
    }

    Mage Next(Mage actual)
    {
        if (change == null) change = FindFirstObjectByType<CharacterSwitcher>(FindObjectsInactive.Include);
        if (change == null || change.mages.Count < 2) return null;
        int i = Mathf.Max(0, change.mages.IndexOf(actual));
        for (int k = 1; k < change.mages.Count; k++)
        {
            var m = change.mages[(i + k) % change.mages.Count];
            if (m != null && m.Alive) return m;
        }
        return null;
    }

    Sprite Portrait(Mage m)
    {
        var sourceSprite = m.data.idle != null && !m.data.idle.Empty ? m.data.idle.frames[0] : m.body != null ? m.body.sprite : null;
        if (sourceSprite == null) return null;
        if (portraits.TryGetValue(sourceSprite, out var done)) return done;
        var r = sourceSprite.rect;
        float heightValue = r.height * 0.6f;
        float width = Mathf.Min(r.width, heightValue * 1.15f);
        var crop = new Rect(r.center.x - width * 0.5f, r.yMax - heightValue, width, heightValue);
        done = Sprite.Create(sourceSprite.texture, crop, new Vector2(0.5f, 0.5f), sourceSprite.pixelsPerUnit);
        portraits[sourceSprite] = done;
        return done;
    }

    static int ColorIndex(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return 0;
            case MagicElement.Nebula: return 1;
            case MagicElement.Plant: return 2;
            default: return 3;
        }
    }

    static string Uppercase(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
}
