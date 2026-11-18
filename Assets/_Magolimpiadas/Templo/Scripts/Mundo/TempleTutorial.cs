using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TempleTutorial : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        [TextArea(2, 4)] public string text;
        public RectTransform highlight;
    }

    [System.Serializable]
    public class Page
    {
        public string title;
        public CanvasGroup content;
        public List<Line> lines = new List<Line>();
    }

    public static bool IsOpen { get; private set; }
    public static bool Busy => IsOpen || Time.frameCount == closingFrame;
    static int closingFrame = -1;

    public OwlGuide guide;
    public CanvasGroup group;
    public RectTransform scroll;
    public TMP_Text title;
    public TMP_Text pageNumber;
    public AttackDemo demo;
    public TMP_Text yourMageText;
    public UnityEngine.UI.Image yourMageIcon;
    public TMP_Text slimeValues;
    public TMP_Text bossValues;

    public Sprite[] fireSlime;
    public Sprite[] nebulaSlime;
    public Sprite[] plantSlime;
    public Sprite[] lightningSlime;
    public Sprite[] bullseye;
    public Sprite[] targetBreak;

    public bool onlyFirstTime = true;
    public bool owlBelow = true;
    public Key skipKey = Key.Escape;

    public List<Page> pages = new List<Page>();

    [TextArea(2, 4)]
    public string[] secondaryPowerTexts =
    {
        "Además, {mago} maneja el hielo: con el clic derecho lo lanzas y frena a los enemigos durante 3 segundos.",
        "Pero el hielo hace menos daño que el fuego, y a los enemigos de hielo no les hace nada. Para pelear, usa el fuego; el hielo sirve para frenarlos y para congelar el lago (E)."
    };
    public int secondaryPowerPage = 2;

    bool seen;
    float timeScale = 1f;
    MageData mage;

    void Start()
    {
        if (group != null) { group.alpha = 0f; group.gameObject.SetActive(false); }
        if (WorldSwitcher.Instance != null) WorldSwitcher.Instance.onEnter2D.AddListener(OnEnterTemple);
    }

    void OnDestroy()
    {
        if (WorldSwitcher.Instance != null) WorldSwitcher.Instance.onEnter2D.RemoveListener(OnEnterTemple);
        if (IsOpen) { IsOpen = false; Time.timeScale = timeScale; }
    }

    void OnEnterTemple()
    {
        if (onlyFirstTime && seen) return;
        StartCoroutine(Show());
    }

    public IEnumerator Show()
    {
        if (IsOpen || guide == null || pages.Count == 0) yield break;
        seen = true;
        IsOpen = true;
        timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        if (!ArcaneGames.GameSession.IsOnline)
            Time.timeScale = 0f;

        var player = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.player2D : null;
        mage = player != null ? player.data : null;
        Fill();

        group.gameObject.SetActive(true);
        foreach (var p in pages) if (p.content != null) { p.content.alpha = 0f; p.content.gameObject.SetActive(false); }
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f) { group.alpha = t; yield return null; }
        group.alpha = 1f;
        guide.Notice("Esc · saltar");

        for (int i = 0; i < pages.Count; i++)
        {
            yield return ChangePage(i);
            foreach (var l in pages[i].lines)
                yield return guide.Say(CreateText(l.text), Point(l.highlight), !owlBelow);
            if (i == secondaryPowerPage - 1 && mage != null && mage.hasSecondaryPower && secondaryPowerTexts != null)
                foreach (var text in secondaryPowerTexts)
                    if (!string.IsNullOrEmpty(text)) yield return guide.Say(CreateText(text), null, !owlBelow);
        }

        yield return guide.Close();
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.3f) { group.alpha = t; yield return null; }
        Finish();
    }

    IEnumerator ChangePage(int i)
    {
        foreach (var p in pages)
            if (p != pages[i] && p.content != null && p.content.gameObject.activeSelf)
            {
                for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.2f) { p.content.alpha = t; yield return null; }
                p.content.gameObject.SetActive(false);
            }
        var newValue = pages[i];
        title.text = newValue.title;
        if (pageNumber != null) pageNumber.text = (i + 1) + " / " + pages.Count;
        if (newValue.content == null) yield break;
        newValue.content.gameObject.SetActive(true);
        if (demo != null && demo.transform.IsChildOf(newValue.content.transform)) demo.Restart();
        var r = (RectTransform)newValue.content.transform;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            newValue.content.alpha = s;
            r.anchoredPosition = new Vector2((1f - s) * 40f, 0f);
            yield return null;
        }
        newValue.content.alpha = 1f;
        r.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (!IsOpen) return;
        var k = Keyboard.current;
        if (k != null && k[skipKey].wasPressedThisFrame) Skip();
    }

    public void Skip()
    {
        if (!IsOpen) return;
        StopAllCoroutines();
        guide.CloseNow();
        Finish();
    }

    void Finish()
    {
        group.alpha = 0f;
        group.gameObject.SetActive(false);
        Time.timeScale = timeScale;
        IsOpen = false;
        closingFrame = Time.frameCount;
    }

    void Fill()
    {
        var e = mage != null ? mage.element : MagicElement.Nebula;
        var defeated = Defeated(e);
        if (yourMageText != null)
            yourMageText.text = (mage != null ? mage.displayName : "Tu mago") + "\n<size=80%>Tu elemento: <b>" + Name(e) + "</b>\nVences a: <b>" + DefeatedName(e) + "</b></size>";
        var icons = ElementsIcons.Load();
        if (yourMageIcon != null && icons != null) yourMageIcon.sprite = icons.For(e);

        float power = mage != null ? mage.power.damage : 10f;
        float arrow = 3f;
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (enemy.isBoss) { arrow = enemy.bossArrowDamage; break; }
        if (slimeValues != null)
            slimeValues.text = "Flecha: <b>no les hace daño</b>\nPoder: <b>" + power + "</b>\nSi tu elemento lo vence: <b>" + power * 2f + "</b>\nMismo elemento: <b>0</b>";
        if (bossValues != null)
            bossValues.text = "Flecha: <b>" + arrow + "</b> (poco)\nPoder: <b>" + power + "</b>\nSi tu elemento lo vence: <b>" + power * 2f + "</b>";

        if (demo != null && mage != null)
        {
            Sprite[] slime = defeated == MagicElement.Fire ? fireSlime : defeated == MagicElement.Nebula ? nebulaSlime :
                             defeated == MagicElement.Plant ? plantSlime : lightningSlime;
            demo.Configure(mage, slime, bullseye, targetBreak);
        }
    }

    string CreateText(string t)
    {
        if (string.IsNullOrEmpty(t)) return "";
        var e = mage != null ? mage.element : MagicElement.Nebula;
        return t.Replace("{mago}", mage != null ? mage.displayName : "mago")
                .Replace("{elemento}", Name(e))
                .Replace("{vence}", DefeatedName(e));
    }

    static Vector3? Point(RectTransform r)
    {
        if (r == null || !r.gameObject.activeInHierarchy) return null;
        Vector2 p = RectTransformUtility.WorldToScreenPoint(null, r.position);
        return new Vector3(p.x, p.y, 1f);
    }

    static MagicElement Defeated(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return MagicElement.Plant;
            case MagicElement.Plant: return MagicElement.Nebula;
            case MagicElement.Nebula: return MagicElement.Lightning;
            case MagicElement.Lightning: return MagicElement.Fire;
        }
        return MagicElement.None;
    }

    static string DefeatedName(MagicElement e) => e == MagicElement.Fire ? "Planta y Hielo" : Name(Defeated(e));

    public static string Name(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return "Fuego";
            case MagicElement.Nebula: return "Nebuloso";
            case MagicElement.Plant: return "Planta";
            case MagicElement.Lightning: return "Rayo";
            case MagicElement.Ice: return "Hielo";
        }
        return "Ninguno";
    }
}
