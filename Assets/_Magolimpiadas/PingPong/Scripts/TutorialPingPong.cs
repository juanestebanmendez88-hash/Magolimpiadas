using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPingPong : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        public string title;
        [TextArea(2, 4)] public string[] lines;
    }

    public Sprite scroll;
    public TMP_FontAsset titleFont;
    public TMP_FontAsset textFont;
    public Color ink = new Color(0.3f, 0.17f, 0.09f);

    public List<Page> pages = new List<Page>
    {
        new Page { title = "Bolas de colores", lines = new[] {
            "¡Bienvenidos a la Mesa del Coliseo! El Guardián del Olimpo los reta a un partido de ping pong.",
            "El Guardián lanza bolas de colores: cada bola solo la puede devolver el mago de su color.",
            "Las bolas blancas las puede devolver cualquiera. ¡Y puede haber hasta tres bolas a la vez!" } },
        new Page { title = "Cómo golpear", lines = new[] {
            "Tú eres {mago}: muévete con W y S, y golpea con clic izquierdo cuando llegue tu bola.",
            "¡La bola sale hacia donde apuntas con el ratón! Tus compañeros devuelven solos las suyas." } },
        new Page { title = "La barra de poder", lines = new[] {
            "Cada bola devuelta carga la barra de poder. Cuando esté llena, tu siguiente golpe será imparable y anotará un punto.",
            "Gana el primero que llegue a 5. Si ganan al primer intento, ¡medalla de oro! ¡Buena suerte!" } },
    };

    Canvas canvas;
    CanvasGroup group;
    TMP_Text title, number;
    RectTransform[] contents;
    PingPongLevel level;
    PingPongLevel.PingPongMage captain;
    int page = -1;
    float t;

    class Anim { public Image img; public Sprite[] frames; public float fps; public bool repeat; public float t0; public float scale; }
    readonly List<Anim> anims = new List<Anim>();
    readonly List<(RectTransform r, float phase)> bouncingBalls = new List<(RectTransform, float)>();
    Image captainImage, demoBall, demoTrail, fillBar, flashImage, captainPower, powerBall, powerTrail;
    TMP_Text barText;

    public bool IsOpen => group != null && group.gameObject.activeSelf;

    public IEnumerator Show(PingPongLevel n, PingPongLevel.PingPongMage cap)
    {
        level = n; captain = cap;
        Create();
        group.gameObject.SetActive(true);
        for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / 0.3f) { group.alpha = a; yield return null; }
        group.alpha = 1f;
        var owl = OwlNotice.Owl;
        if (owl != null && !owl.gameObject.activeInHierarchy) owl.gameObject.SetActive(true);
        for (int i = 0; i < pages.Count; i++)
        {
            yield return ChangePage(i);
            if (owl == null) { yield return new WaitForSecondsRealtime(3f); continue; }
            foreach (var l in pages[i].lines)
            {
                owl.Notice("Esc · saltar");
                if (!string.IsNullOrEmpty(l)) yield return owl.Say(l.Replace("{mago}", captain != null ? captain.displayName : "mago"), null, false);
            }
        }
        if (owl != null) yield return owl.Close();
        for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime / 0.3f) { group.alpha = a; yield return null; }
        Hide();
    }

    public void Hide()
    {
        if (group != null) { group.alpha = 0f; group.gameObject.SetActive(false); }
        if (contents != null) foreach (var c in contents) if (c != null) c.gameObject.SetActive(false);
        page = -1;
    }

    IEnumerator ChangePage(int i)
    {
        if (page >= 0)
        {
            var old = contents[page].GetComponent<CanvasGroup>();
            for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime / 0.2f) { old.alpha = a; yield return null; }
            contents[page].gameObject.SetActive(false);
        }
        page = i;
        t = 0f;
        title.text = pages[i].title;
        number.text = (i + 1) + " / " + pages.Count;
        Build(i);
        var g = contents[i].GetComponent<CanvasGroup>();
        contents[i].gameObject.SetActive(true);
        for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / 0.3f)
        {
            float s = Mathf.SmoothStep(0f, 1f, a);
            g.alpha = s;
            contents[i].anchoredPosition = new Vector2((1f - s) * 40f, 0f);
            yield return null;
        }
        g.alpha = 1f;
        contents[i].anchoredPosition = Vector2.zero;
    }

    void Create()
    {
        if (canvas != null) return;
        var go = new GameObject("Tutorial Ping Pong", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;

        var contentRoot = Child("Contenido", go.transform);
        Stretch(contentRoot);
        group = contentRoot.gameObject.AddComponent<CanvasGroup>();
        var background = Img("Fondo", contentRoot, null, new Color(0f, 0f, 0.02f, 0.6f));
        Stretch(background.rectTransform);

        var scrollImage = Img("Pergamino", contentRoot, scroll, Color.white);
        scrollImage.type = Image.Type.Sliced;
        var rp = scrollImage.rectTransform;
        rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0.5f, 1f);
        rp.anchoredPosition = new Vector2(0f, -22f);
        rp.sizeDelta = new Vector2(1500f, 660f);

        title = Txt("Titulo", rp, titleFont, 60f, ink, new Vector2(0f, 290f), new Vector2(1100f, 80f));
        number = Txt("Pagina", rp, textFont, 22f, new Color(ink.r, ink.g, ink.b, 0.7f), new Vector2(640f, 280f), new Vector2(150f, 30f));
        number.alignment = TextAlignmentOptions.Right;

        contents = new RectTransform[pages.Count];
        for (int i = 0; i < pages.Count; i++)
        {
            var c = Child("Pagina " + (i + 1), rp);
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
            c.sizeDelta = new Vector2(1500f, 660f);
            c.gameObject.AddComponent<CanvasGroup>();
            c.gameObject.SetActive(false);
            contents[i] = c;
        }
        contentRoot.gameObject.SetActive(false);
    }

    void Build(int i)
    {
        var c = contents[i];
        for (int k = c.childCount - 1; k >= 0; k--) Destroy(c.GetChild(k).gameObject);
        anims.Clear(); bouncingBalls.Clear();
        captainImage = demoBall = demoTrail = fillBar = flashImage = captainPower = powerBall = powerTrail = null;
        if (level == null) return;
        var mages = level.mages;

        if (i == 0)
        {
            for (int m = 0; m < mages.Count; m++)
            {
                float x = -480f + m * 280f;
                Character(c, mages[m].idle, new Vector2(x, -230f), 0.85f, 8f, m * 0.13f);
                var b = Ball(c, mages[m].ballSprite, mages[m].ballFill, new Vector2(x, 70f));
                bouncingBalls.Add((b.rectTransform, m * 0.7f));
                var n = Txt("Nombre", c, textFont, 26f, Dark(mages[m].color), new Vector2(x, -265f), new Vector2(270f, 36f));
                n.text = mages[m].displayName;
            }
            var white = Ball(c, level.WhiteBall, 0.9f, new Vector2(620f, 70f));
            bouncingBalls.Add((white.rectTransform, 2.2f));
            var tb = Txt("Blanca", c, textFont, 26f, ink, new Vector2(620f, -10f), new Vector2(220f, 70f));
            tb.text = "Blanca:\ncualquiera";
        }
        else if (i == 1)
        {
            captainImage = Character(c, captain.idle, new Vector2(-420f, -230f), 1.05f, 8f, 0f);
            demoTrail = Img("Estela", c, null, Color.white);
            demoBall = Ball(c, captain.ballSprite, captain.ballFill, new Vector2(500f, 0f));
            var legend = Txt("Leyenda", c, textFont, 34f, ink, new Vector2(0f, -275f), new Vector2(1200f, 50f));
            legend.text = "W / S: moverse   ·   Clic izquierdo: golpear   ·   El ratón apunta";
        }
        else
        {
            flashImage = Img("Destello", c, null, new Color(captain.color.r, captain.color.g, captain.color.b, 0f));
            flashImage.rectTransform.sizeDelta = new Vector2(1420f, 560f);
            captainPower = Character(c, captain.idle, new Vector2(-460f, -170f), 1.05f, 8f, 0f);
            powerTrail = Img("Estela", c, null, Color.white);
            powerBall = Ball(c, captain.ballSprite, captain.ballFill, new Vector2(-340f, -40f));
            var frame = Img("Barra", c, null, new Color(0.15f, 0.08f, 0.05f, 0.85f));
            frame.rectTransform.anchoredPosition = new Vector2(0f, -250f);
            frame.rectTransform.sizeDelta = new Vector2(560f, 40f);
            fillBar = Img("Relleno", frame.rectTransform, null, captain.color);
            var rr = fillBar.rectTransform;
            rr.anchorMin = Vector2.zero; rr.anchorMax = new Vector2(0f, 1f); rr.pivot = new Vector2(0f, 0.5f);
            rr.offsetMin = new Vector2(4f, 4f); rr.offsetMax = new Vector2(-4f, -4f);
            barText = Txt("Texto", frame.rectTransform, titleFont, 28f, Color.white, Vector2.zero, new Vector2(560f, 40f));
        }
    }

    void Update()
    {
        if (!IsOpen || page < 0 || level == null) return;
        float dt = Time.unscaledDeltaTime;
        t += dt;
        foreach (var a in anims)
        {
            if (a.img == null || a.frames == null || a.frames.Length == 0) continue;
            float tt = Time.unscaledTime - a.t0;
            int f = (int)(tt * a.fps);
            f = a.repeat ? f % a.frames.Length : Mathf.Min(f, a.frames.Length - 1);
            Put(a.img, a.frames[f], a.scale);
        }
        foreach (var b in bouncingBalls)
            if (b.r != null) b.r.anchoredPosition = new Vector2(b.r.anchoredPosition.x, 60f + Mathf.Abs(Mathf.Sin(t * 3f + b.phase)) * 70f);

        if (page == 1 && captainImage != null) HitDemo();
        if (page == 2 && captainPower != null) PowerDemo();
    }

    void HitDemo()
    {
        const float cycle = 2.4f;
        float c = t % cycle;
        Vector2 racket = new Vector2(-330f, -110f), far = new Vector2(560f, 10f);
        var anim = anims.Find(a => a.img == captainImage);
        if (c < 1.1f)
        {
            float k = c / 1.1f;
            demoBall.rectTransform.anchoredPosition = Vector2.Lerp(far, racket, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 90f;
            Trail(demoTrail, null, demoBall.rectTransform.anchoredPosition, 180f);
            if (anim != null && anim.frames != captain.idle.frames) { anim.frames = captain.idle.frames; anim.fps = 8f; anim.repeat = true; }
        }
        else
        {
            if (anim != null && anim.frames != captain.hit.frames) { anim.frames = captain.hit.frames; anim.fps = 14f; anim.repeat = false; anim.t0 = Time.unscaledTime; }
            float k = Mathf.Clamp01((c - 1.1f) / 0.9f);
            Vector2 meta = new Vector2(600f, 120f);
            demoBall.rectTransform.anchoredPosition = Vector2.Lerp(racket, meta, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 60f;
            Trail(demoTrail, k < 1f ? captain.trail : null, demoBall.rectTransform.anchoredPosition, 0f);
        }
    }

    void PowerDemo()
    {
        const float cycle = 4f;
        float c = t % cycle;
        var anim = anims.Find(a => a.img == captainPower);
        float full = Mathf.Clamp01(c / 2f);
        fillBar.rectTransform.anchorMax = new Vector2(full, 1f);
        bool list = full >= 1f;
        fillBar.color = list ? Color.Lerp(captain.color, Color.white, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 10f)) : captain.color;
        barText.text = list ? "¡Poder listo!" : "Poder";
        Vector2 start = new Vector2(-340f, -40f), end = new Vector2(650f, 20f);
        if (c < 2.6f)
        {
            powerBall.rectTransform.anchoredPosition = start + Vector2.up * Mathf.Abs(Mathf.Sin(c * 4f)) * 30f;
            Trail(powerTrail, null, start, 0f);
            flashImage.color = new Color(captain.color.r, captain.color.g, captain.color.b, 0f);
            if (anim != null && anim.frames != captain.idle.frames) { anim.frames = captain.idle.frames; anim.fps = 8f; anim.repeat = true; }
        }
        else
        {
            if (anim != null && anim.frames != captain.hit.frames) { anim.frames = captain.hit.frames; anim.fps = 14f; anim.repeat = false; anim.t0 = Time.unscaledTime; }
            float k = Mathf.Clamp01((c - 2.6f) / 0.5f);
            powerBall.rectTransform.anchoredPosition = Vector2.Lerp(start, end, k);
            Trail(powerTrail, k < 1f ? captain.powerTrail : null, powerBall.rectTransform.anchoredPosition, 0f, 2.2f);
            float flash = Mathf.Clamp01(1f - (c - 2.6f) / 1.2f) * 0.35f;
            flashImage.color = new Color(captain.color.r, captain.color.g, captain.color.b, flash);
        }
    }

    void Trail(Image img, SpriteAnimation trace, Vector2 pos, float angle, float length = 1f)
    {
        if (img == null) return;
        bool ver = trace != null && !trace.Empty;
        img.enabled = ver;
        if (!ver) return;
        var s = trace.frames[(int)(Time.unscaledTime * trace.fps) % trace.frames.Length];
        img.sprite = s;
        var rt = img.rectTransform;
        rt.pivot = s.pivot / s.rect.size;
        rt.sizeDelta = s.rect.size * 1.3f * new Vector2(length, 1f);
        rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    Image Character(RectTransform parent, SpriteAnimation a, Vector2 foot, float scale, float fps, float offset)
    {
        var img = Img("Mago", parent, null, Color.white);
        img.rectTransform.anchoredPosition = foot;
        anims.Add(new Anim { img = img, frames = a != null ? a.frames : null, fps = fps, repeat = true, t0 = Time.unscaledTime - offset, scale = scale });
        if (a != null && !a.Empty) Put(img, a.frames[0], scale);
        return img;
    }

    Image Ball(RectTransform parent, Sprite s, float fill, Vector2 pos)
    {
        var img = Img("Bola", parent, s, Color.white);
        img.preserveAspect = true;
        float sizeValue = 70f / Mathf.Max(0.1f, fill);
        img.rectTransform.sizeDelta = new Vector2(sizeValue, sizeValue);
        img.rectTransform.anchoredPosition = pos;
        return img;
    }

    static void Put(Image img, Sprite s, float scale)
    {
        if (s == null) return;
        if (img.sprite != s) img.sprite = s;
        var rt = img.rectTransform;
        rt.pivot = s.pivot / s.rect.size;
        rt.sizeDelta = s.rect.size * scale;
    }

    static Color Dark(Color c) => Color.Lerp(c, Color.black, 0.35f);

    static RectTransform Child(string displayName, Transform parent)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image Img(string displayName, Transform parent, Sprite s, Color c)
    {
        var go = new GameObject(displayName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        return img;
    }

    static TMP_Text Txt(string displayName, Transform parent, TMP_FontAsset f, float sizeValue, Color c, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.fontSize = sizeValue;
        t.color = c;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }
}
