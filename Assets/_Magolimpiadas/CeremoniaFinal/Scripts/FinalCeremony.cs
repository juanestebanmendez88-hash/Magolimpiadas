using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FinalCeremony : MonoBehaviour
{
    [System.Serializable]
    public class Mage
    {
        public string id;
        public string displayName;
        public Color color = Color.white;
        public Animator animator;
        [System.NonSerialized] public SpriteRenderer medal;
        [System.NonSerialized] public Transform chest;
        [System.NonSerialized] public GameObject aura;
    }

    [System.Serializable]
    public class TestMedals
    {
        public string test;
        public string shortName;
        public Sprite[] sprites = new Sprite[3];
    }

    public List<Mage> mages = new List<Mage>();
    public List<TestMedals> medals = new List<TestMedals>();

    public Transform[] places = new Transform[4];
    [FormerlySerializedAs("caldero")]
    public Transform center;
    public float viewRadius = 34f;
    public float viewHeight = 14f;
    public Camera sceneCamera;
    public RuntimeAnimatorController ceremoniesController;
    public string[] fixedHeadMages = { "elemental" };

    public CanvasGroup titleGroup;
    public TMP_Text title;
    public TMP_Text subtitle;
    public CanvasGroup medalTableGroup;
    public RectTransform medalTableRows;
    public CanvasGroup finalGroup;
    public TMP_Text finalText;
    public TMP_Text finalDetail;
    public Button startButton;
    public Button exitButton;
    public Image black;
    public TMP_Text continueNotice;
    public TMP_FontAsset titleFont;
    public TMP_FontAsset textFont;
    public Sprite gap;

    public string startScene = "00_Inicio";
    public float medalSize = 0.32f;
    public int fireworks = 6;

    List<string> order = new List<string>();
    readonly List<GameObject> effects = new List<GameObject>();
    bool finished;
    float rotation;

    static readonly MagicEffects.Effect[] Rockets =
    {
        MagicEffects.Effect.GoldenBurst, MagicEffects.Effect.VioletBurst, MagicEffects.Effect.BlueBurst,
        MagicEffects.Effect.GreenBurst, MagicEffects.Effect.OrangeBurst, MagicEffects.Effect.WhiteBurst,
    };

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameSound.SilenceCelebrations(0.3f);
        if (ArcaneGames.AudioManager.Instance != null) ArcaneGames.AudioManager.Instance.ResumeMusic();
        if (startButton != null) startButton.onClick.AddListener(ReturnToStart);
        if (exitButton != null) exitButton.onClick.AddListener(Exit);
        Show(titleGroup, 0f); Show(medalTableGroup, 0f); Show(finalGroup, 0f);
        if (titleGroup != null) scale = titleGroup.GetComponentInParent<CanvasScaler>();
        StartCoroutine(Ceremony());
    }

    CanvasScaler scale;

    void Update()
    {
        if (scale == null) return;
        var canvas = scale.GetComponent<Canvas>();
        var px = canvas != null ? canvas.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
        float look = px.height > 0f ? px.width / px.height : 16f / 9f;
        scale.matchWidthOrHeight = look >= 16f / 9f ? 1f : 0f;
    }

    IEnumerator Ceremony()
    {
        yield return Prepare();

        StartCoroutine(Fade(black, 1f, 0f, 1.6f));
        if (title != null) title.text = "Ceremonia de clausura";
        if (subtitle != null) subtitle.text = "Magolimpiadas  ·  Isla de la Ceremonia";
        StartCoroutine(FadeGroup(titleGroup, 0f, 1f, 0.8f));
        yield return Take(t => Orbit(t * 0.3f, viewRadius, viewHeight), 6f);
        StartCoroutine(FadeGroup(titleGroup, 1f, 0f, 0.6f));

        yield return MedalTable();

        yield return Victory();
    }

    IEnumerator Prepare()
    {
        if (GameResults.Tests.Count == 0)
            foreach (var m in medals) GameResults.RegisterTeam(m.test, 0);
        string player = ChosenMage.Chosen;
        var ids = mages.Select(m => m.id).OrderBy(id => id == player ? 0 : 1).ToList();
        order = GameResults.Ranking(ids);

        for (int i = 0; i < order.Count && i < places.Length; i++)
        {
            var m = Token(order[i]);
            if (m == null || m.animator == null || places[i] == null) continue;
            m.animator.applyRootMotion = false;
            m.animator.transform.SetPositionAndRotation(places[i].position, places[i].rotation);
            if (m.animator.isHuman)
            {
                m.chest = m.animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (m.chest == null) m.chest = m.animator.GetBoneTransform(HumanBodyBones.Chest);
            }
            m.medal = new GameObject("Medalla " + m.displayName, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            m.medal.gameObject.SetActive(false);
        }
        yield return null;
        foreach (var m in mages)
        {
            if (m.animator == null || ceremoniesController == null) continue;
            if (System.Array.IndexOf(fixedHeadMages, m.id) >= 0) FixedHead.Put(m.animator);
            m.animator.runtimeAnimatorController = ceremoniesController;
            m.animator.Play("Celebrar", 0, Random.value * 0.4f);
        }
    }

    IEnumerator MedalTable()
    {
        BuildTable();
        StartCoroutine(FadeGroup(medalTableGroup, 0f, 1f, 0.6f));
        var rows = medalTableRows != null ? medalTableRows.Cast<Transform>().Skip(1).ToList() : new List<Transform>();
        foreach (var f in rows) f.localScale = Vector3.zero;
        bool jump = false;
        for (int i = order.Count - 1; i >= 0; i--)
        {
            var m = Token(order[i]);
            if (i < rows.Count) StartCoroutine(Appear(rows[i], 0f));
            if (m != null) GiveMedal(m, i);
            if (!jump) yield return Take(t => PodiumShot(t + (order.Count - 1 - i) * 1.4f), 1.4f, () => jump = true);
        }
        if (!jump) yield return Take(t => PodiumShot(t + order.Count * 1.4f), 4f);
        StartCoroutine(FadeGroup(medalTableGroup, 1f, 0f, 0.6f));
    }

    void GiveMedal(Mage m, int place)
    {
        var best = BestMedal(m.id);
        if (m.medal != null && best != null)
        {
            m.medal.sprite = best;
            m.medal.gameObject.SetActive(true);
        }
        MagicEffects.Launch(MagicEffects.SparksOf(m.id), Chest(m) + Vector3.up * 0.3f, 0.5f);
        if (place == 0)
        {
            m.aura = MagicEffects.Launch(MagicEffects.Effect.StarAura, m.animator.transform.position, 0.45f, 0, -1, null, m.animator.transform, -1f);
            MagicEffects.Tint(m.aura, MagicEffects.MageThemeColor(m.id));
            if (m.aura != null) effects.Add(m.aura);
            GameSound.Celebrate(GameAudio.Sound.Medal, 0.7f, 4f, SoundSynthesizer.Medal(), 0.6f);
        }
        else GameSound.Play(SoundSynthesizer.Star(), 0.3f, 1f + (3 - place) * 0.05f);
    }

    IEnumerator Victory()
    {
        int[] total = new int[3];
        foreach (var p in GameResults.Tests)
        {
            int m = p.IsTeam ? p.teamMedal : MedalOf(p, ChosenMage.Chosen);
            if (m >= 0) total[m]++;
        }
        var champion = Token(order.FirstOrDefault());
        if (finalText != null) finalText.text = "¡Ganaron las Magolimpiadas!";
        if (finalDetail != null)
            finalDetail.text = "Tus medallas:  <color=#FFD45A>" + total[0] + " de oro</color>  ·  <color=#D8DCEC>" + total[1] + " de plata</color>  ·  <color=#E39A5B>" + total[2] + " de bronce</color>\n" +
                                (champion != null ? "Primero del medallero: <color=#" + ColorUtility.ToHtmlStringRGB(champion.color) + ">" + champion.displayName + "</color>\n" : "") +
                                "<size=80%>Los cuatro magos celebran juntos en la Isla de la Ceremonia. ¡Gracias por jugar!</size>";
        StartCoroutine(FadeGroup(finalGroup, 0f, 1f, 0.8f));
        GameSound.Celebrate(GameAudio.Sound.Ovation, 0.6f, 3f, SoundSynthesizer.Fanfare());
        float next = Time.time + 1.2f;
        int n = 0;
        while (!finished)
        {
            rotation += Time.deltaTime;
            Orbit(rotation * 0.1f + 1.2f, viewRadius * 0.85f, viewHeight * 0.7f);
            if (n < fireworks && Time.time >= next)
            {
                next = Time.time + Random.Range(2.4f, 3.6f);
                Vector3 c = Center;
                Vector3 p = c + new Vector3(Random.Range(-12f, 12f), Random.Range(14f, 19f), Random.Range(-12f, 12f));
                MagicEffects.Launch(Rockets[n % Rockets.Length], p, Random.Range(1.2f, 1.6f));
                GameSound.Play(SoundSynthesizer.Firework(n % 3), 0.2f, Random.Range(0.85f, 1.15f));
                n++;
            }
            yield return null;
        }
    }

    void BuildTable()
    {
        if (medalTableRows == null) return;
        for (int i = medalTableRows.childCount - 1; i >= 0; i--) Destroy(medalTableRows.GetChild(i).gameObject);
        Row(-1, null);
        for (int i = 0; i < order.Count; i++) Row(i, Token(order[i]));
    }

    void Row(int place, Mage m)
    {
        var row = new GameObject(m != null ? "Fila " + m.displayName : "Encabezado", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(medalTableRows, false);
        var background = row.GetComponent<Image>();
        background.color = m == null ? new Color(0f, 0f, 0f, 0f) : m.id == ChosenMage.Chosen ? new Color(0.35f, 0.22f, 0.05f, 0.85f) : new Color(0.1f, 0.06f, 0.22f, 0.85f);
        background.raycastTarget = false;
        var rt = (RectTransform)row.transform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        float heightValue = m == null ? 44f : 66f;
        rt.sizeDelta = new Vector2(0f, heightValue);
        rt.anchoredPosition = new Vector2(0f, m == null ? 0f : -48f - place * 72f);

        if (m == null)
        {
            CreateText(row.transform, "Puesto", 26f, new Color(0.8f, 0.75f, 1f), 30f, 120f, TextAlignmentOptions.Left);
            CreateText(row.transform, "Mago", 26f, new Color(0.8f, 0.75f, 1f), 150f, 390f, TextAlignmentOptions.Left);
            for (int p = 0; p < medals.Count; p++) CreateText(row.transform, medals[p].shortName, 24f, new Color(0.8f, 0.75f, 1f), 540f + p * 150f, 150f, TextAlignmentOptions.Center);
            CreateText(row.transform, "Oro · Plata · Bronce", 22f, new Color(0.8f, 0.75f, 1f), 540f + medals.Count * 150f + 10f, 230f, TextAlignmentOptions.Center);
            return;
        }
        string[] colors = { "#FFD45A", "#D8DCEC", "#E39A5B", "#9C94C4" };
        CreateText(row.transform, "<color=" + colors[Mathf.Min(place, 3)] + ">" + (place + 1) + ".º</color>", 40f, Color.white, 30f, 120f, TextAlignmentOptions.Left);
        CreateText(row.transform, "<color=#" + ColorUtility.ToHtmlStringRGB(m.color) + ">" + m.displayName + "</color>" + (m.id == ChosenMage.Chosen ? " <size=70%><color=#FFD45A>(tú)</color></size>" : ""), 36f, Color.white, 150f, 390f, TextAlignmentOptions.Left);
        for (int p = 0; p < medals.Count; p++)
        {
            var test = GameResults.Tests.FirstOrDefault(x => x.displayName == medals[p].test);
            int medalIndex = test != null ? MedalOf(test, m.id) : -1;
            var cell = new GameObject("Medalla " + medals[p].shortName, typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(row.transform, false);
            var img = cell.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (medalIndex >= 0 && medalIndex < medals[p].sprites.Length && medals[p].sprites[medalIndex] != null) img.sprite = medals[p].sprites[medalIndex];
            else { img.sprite = gap; img.color = new Color(1f, 1f, 1f, 0.25f); }
            var rc = (RectTransform)cell.transform;
            rc.anchorMin = rc.anchorMax = new Vector2(0f, 0.5f); rc.pivot = new Vector2(0.5f, 0.5f);
            rc.anchoredPosition = new Vector2(540f + p * 150f + 75f, 0f);
            rc.sizeDelta = new Vector2(58f, 60f);
        }
        var count = GameResults.Medals(m.id);
        CreateText(row.transform, "<color=#FFD45A>" + count[0] + "</color> · <color=#D8DCEC>" + count[1] + "</color> · <color=#E39A5B>" + count[2] + "</color>", 34f, Color.white, 540f + medals.Count * 150f + 10f, 230f, TextAlignmentOptions.Center);
    }

    void CreateText(Transform parent, string text, float sizeValue, Color c, float x, float width, TextAlignmentOptions align)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (textFont != null) t.font = textFont;
        t.text = text;
        t.fontSize = sizeValue;
        t.enableAutoSizing = true;
        t.fontSizeMin = sizeValue * 0.6f;
        t.fontSizeMax = sizeValue;
        t.color = c;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(width, 0f);
    }

    IEnumerator Take(System.Action<float> take, float seconds, System.Action onJump = null)
    {
        if (continueNotice != null) continueNotice.gameObject.SetActive(true);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            take(t);
            if (t > 0.5f && Beat()) { onJump?.Invoke(); break; }
            yield return null;
        }
        if (continueNotice != null) continueNotice.gameObject.SetActive(false);
    }

    Vector3 Center => center != null ? center.position : Vector3.zero;

    List<Transform> Places => places.Where(p => p != null).ToList();

    Vector3 PodiumCenter
    {
        get
        {
            var ps = Places;
            if (ps.Count == 0) return Center;
            Vector3 s = Vector3.zero; foreach (var p in ps) s += p.position;
            return s / ps.Count;
        }
    }

    Vector3 PodiumFront => places.Length > 0 && places[0] != null ? VectorUtils.FlatDirection(places[0].forward) : Vector3.forward;

    void Orbit(float angle, float radius, float heightValue)
    {
        if (sceneCamera == null) return;
        Vector3 c = Center;
        Vector3 fromPoint = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f) * PodiumFront;
        sceneCamera.transform.position = c + fromPoint * radius + Vector3.up * heightValue;
        sceneCamera.transform.LookAt(c + Vector3.up * 2f);
    }

    void PodiumShot(float t)
    {
        if (sceneCamera == null) return;
        PodiumFraming(out Vector3 pos, out Quaternion camRotation);
        Vector3 side = Vector3.Cross(Vector3.up, PodiumFront);
        pos += side * Mathf.Sin(t * 0.4f) * 0.6f - PodiumFront * Mathf.Min(t, 8f) * 0.06f;
        float k = 1f - Mathf.Exp(-3f * Time.deltaTime);
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, pos, k);
        sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, camRotation, k);
    }

    const float TopEdge = 0.9f;
    const float MageHeight = 2.1f;

    float TableEdge()
    {
        if (medalTableGroup == null || !medalTableGroup.gameObject.activeInHierarchy) return 0.5f;
        var canvas = medalTableGroup.GetComponentInParent<Canvas>();
        var camUI = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var corners = new Vector3[4];
        ((RectTransform)medalTableGroup.transform).GetWorldCorners(corners);
        float up = RectTransformUtility.WorldToScreenPoint(camUI, corners[1]).y;
        float heightValue = canvas != null ? canvas.pixelRect.height : Screen.height;
        return Mathf.Clamp(up / Mathf.Max(1f, heightValue) + 0.03f, 0.3f, 0.7f);
    }

    void PodiumFraming(out Vector3 pos, out Quaternion camRotation)
    {
        var ps = Places;
        Vector3 c = PodiumCenter;
        Vector3 front = PodiumFront;
        Vector3 side = Vector3.Cross(Vector3.up, front);
        float ground = ps.Count > 0 ? ps.Min(p => p.position.y) : c.y;
        float ceiling = (ps.Count > 0 ? ps.Max(p => p.position.y) : c.y) + MageHeight;
        float width = ps.Count > 1 ? ps.Max(p => Vector3.Dot(p.position - c, side)) - ps.Min(p => Vector3.Dot(p.position - c, side)) : 4f;
        float tanA = Mathf.Tan(sceneCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanA * Mathf.Max(0.5f, sceneCamera.aspect);
        float camHeight = ground + 2.6f;
        float down = Mathf.Atan((TableEdge() - 0.5f) * 2f * tanA);
        float d = 6f, tilt = 0f;
        for (; d < 45f; d += 0.25f)
        {
            float feetAngle = Mathf.Atan2(ground - camHeight, d);
            tilt = feetAngle - down;
            float headAngle = Mathf.Atan2(ceiling - camHeight, d);
            float headFraction = 0.5f + Mathf.Tan(headAngle - tilt) / (2f * tanA);
            bool fitsWidth = width * 0.5f + 1.6f <= d * tanH * 0.92f;
            if (headFraction <= TopEdge && fitsWidth) break;
        }
        pos = new Vector3(c.x, camHeight, c.z) + front * d;
        Vector3 lookDirection = -front * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt);
        camRotation = Quaternion.LookRotation(lookDirection);
    }

    void LateUpdate()
    {
        if (sceneCamera == null) return;
        foreach (var m in mages)
        {
            if (m.medal == null || !m.medal.gameObject.activeSelf || m.animator == null) continue;
            Vector3 front = m.animator.transform.forward;
            m.medal.transform.position = Chest(m) + front * 0.22f;
            m.medal.transform.rotation = Quaternion.LookRotation(m.medal.transform.position - sceneCamera.transform.position);
            float heightValue = m.medal.sprite != null ? m.medal.sprite.bounds.size.y : 1f;
            m.medal.transform.localScale = Vector3.one * medalSize / Mathf.Max(0.01f, heightValue) * (1f + 0.04f * Mathf.Sin(Time.time * 3f));
        }
    }

    Vector3 Chest(Mage m)
    {
        if (m.chest != null) return m.chest.position;
        return m.animator != null ? m.animator.transform.position + Vector3.up * 1.1f : Vector3.zero;
    }

    Mage Token(string id) => mages.FirstOrDefault(m => m.id == id);

    static int MedalOf(GameResults.Test p, string id) => GameResults.MedalOf(p, id);

    Sprite BestMedal(string id)
    {
        Sprite best = null;
        int bestValue = 3;
        foreach (var mp in medals)
        {
            var p = GameResults.Tests.FirstOrDefault(x => x.displayName == mp.test);
            if (p == null) continue;
            int m = MedalOf(p, id);
            if (m >= 0 && m < bestValue && m < mp.sprites.Length && mp.sprites[m] != null) { bestValue = m; best = mp.sprites[m]; }
        }
        return best;
    }

    static bool Beat()
    {
        var k = Keyboard.current; var r = Mouse.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (r != null && r.leftButton.wasPressedThisFrame);
    }

    static void Show(CanvasGroup g, float a)
    {
        if (g == null) return;
        g.alpha = a;
        g.gameObject.SetActive(a > 0f);
        g.interactable = g.blocksRaycasts = a > 0.5f;
    }

    IEnumerator FadeGroup(CanvasGroup g, float fromPoint, float until, float dur)
    {
        if (g == null) yield break;
        g.gameObject.SetActive(true);
        for (float t = 0f; t < dur; t += Time.deltaTime) { g.alpha = Mathf.Lerp(fromPoint, until, t / dur); yield return null; }
        Show(g, until);
    }

    IEnumerator Fade(Image img, float fromPoint, float until, float dur)
    {
        if (img == null) yield break;
        img.gameObject.SetActive(true);
        for (float t = 0f; t < dur; t += Time.deltaTime) { var c = img.color; c.a = Mathf.Lerp(fromPoint, until, t / dur); img.color = c; yield return null; }
        var f = img.color; f.a = until; img.color = f;
        if (until <= 0f) img.gameObject.SetActive(false);
    }

    IEnumerator Appear(Transform t, float wait)
    {
        for (float x = 0f; x < wait; x += Time.deltaTime) yield return null;
        for (float x = 0f; x < 0.4f; x += Time.deltaTime)
        {
            if (t == null) yield break;
            float k = x / 0.4f;
            float s = 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f);
            t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    void ReturnToStart()
    {
        if (finished) return;
        finished = true;
        ButtonSound();
        NewMatch.Restart();
        StartCoroutine(GoTo(startScene));
    }

    void Exit()
    {
        if (finished) return;
        finished = true;
        ButtonSound();
        GameSound.SilenceCelebrations(0.2f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void ButtonSound()
    {
        if (ArcaneGames.AudioManager.Instance != null) ArcaneGames.AudioManager.Instance.PlayButtonClick();
    }

    IEnumerator GoTo(string scene)
    {
        GameSound.SilenceCelebrations(0.6f);
        yield return Fade(black, 0f, 1f, 0.8f);
        foreach (var e in effects) if (e != null) Destroy(e);
        if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
        else Debug.LogWarning("[Ceremonia final] La escena \"" + scene + "\" no está en Build Settings.");
    }
}
