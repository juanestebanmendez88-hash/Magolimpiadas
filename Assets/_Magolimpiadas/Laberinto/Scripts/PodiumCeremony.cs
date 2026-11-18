using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PodiumCeremony : MonoBehaviour
{
    public static PodiumCeremony Instance { get; private set; }

    [Serializable]
    public class CeremonyMage
    {
        public string id;
        public string displayName;
        public Color color = Color.white;
        public Sprite[] celebration;
        public Sprite[] defeat;
    }

    public class Row
    {
        public string id;
        public string[] values;
    }

    public List<CeremonyMage> mages = new List<CeremonyMage>();
    public Sprite podium;
    public Vector2[] podiumPlaces = { new Vector2(418, 10), new Vector2(150, 64), new Vector2(686, 105) };
    public Sprite[] bowMedals;
    public Sprite[] swimmingMedals;
    public Sprite[] pingPongMedals;
    public Sprite[] syncedMedals;
    public TMP_FontAsset titleFont;
    public TMP_FontAsset textFont;

    public float tableSeconds = 6f;
    public float medalSeconds = 4f;
    public float podiumSeconds = 5f;
    public float seconds3D = 7f;

    public RuntimeAnimatorController ceremoniesController;
    public string[] fixedHeadMages = { "elemental" };

    static readonly Dictionary<Animator, (RuntimeAnimatorController ctrl, bool root)> original =
        new Dictionary<Animator, (RuntimeAnimatorController, bool)>();

    public static void ReturnToGame(Animator anim, Transform mage)
    {
        if (anim == null) return;
        FixedHead.Remove(anim);
        if (original.TryGetValue(anim, out var o))
        {
            anim.runtimeAnimatorController = o.ctrl;
            anim.applyRootMotion = o.root;
            original.Remove(anim);
        }
        anim.speed = 1f;
        WithoutTorch(mage);
    }

    public RuntimeAnimatorController ControllerFor(string id) => ceremoniesController;

    public void UseCeremonies(string id, Animator anim)
    {
        if (anim == null || ceremoniesController == null) return;
        if (anim.runtimeAnimatorController != ceremoniesController && !original.ContainsKey(anim))
            original[anim] = (anim.runtimeAnimatorController, anim.applyRootMotion);
        if (fixedHeadMages != null && System.Array.IndexOf(fixedHeadMages, id) >= 0) FixedHead.Put(anim);
        anim.applyRootMotion = false;
        anim.runtimeAnimatorController = ceremoniesController;
    }
    public Transform level1Podium;
    public Transform level3Podium;

    public bool InProgress { get; private set; }

    Canvas canvas;
    Image background, black;
    RectTransform layer;
    class AnimUI { public Image img; public Sprite[] frames; public float fps; public bool repeat; public float t; public float scale; }
    readonly List<AnimUI> anims = new List<AnimUI>();
    Sprite rays, star;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public IEnumerator TempleEnd()
    {
        InProgress = true;
        var rows = new List<(string id, int bullseyes, int coins)>();
        foreach (var m in Mage.All)
        {
            if (m == null || m.data == null) continue;
            rows.Add((Id(m.data.element), Enemy.TargetsFor(m.data), Coin.For(m.data)));
        }
        var order = rows.OrderByDescending(f => f.bullseyes + f.coins).ThenByDescending(f => f.bullseyes).ToList();
        var table = order.Select(f => new Row { id = f.id, values = new[] { f.bullseyes.ToString(), f.coins.ToString(), (f.bullseyes + f.coins).ToString() } }).ToList();
        yield return Screens("Tiro con arco", bowMedals, new[] { "Dianas", "Monedas", "Total" }, table, true);

        bool ready = WorldSwitcher.Instance == null;
        if (!ready) WorldSwitcher.Instance.FinishLevel(() => ready = true);
        for (float t = 0f; !ready && t < 6f; t += Time.unscaledDeltaTime) yield return null;
        StowBows();
        yield return Ceremony3D(level1Podium, order.Select(f => f.id).ToList());
        InProgress = false;
    }

    public static void StowBows()
    {
        foreach (var bow in FindObjectsByType<Collectible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (bow == null) continue;
            if (bow.Collected) { if (bow.item != null) bow.item.gameObject.SetActive(false); }
            else bow.gameObject.SetActive(false);
        }
    }

    public IEnumerator Screens(string sport, Sprite[] medals, string[] columns, List<Row> rows, bool podium2D)
    {
        Create();
        canvas.gameObject.SetActive(true);
        yield return Fade(background, 0f, 0.93f, 0.4f);
        string player = ChosenMage.Chosen;
        int place = Mathf.Max(0, rows.FindIndex(f => f.id == player));
        GameResults.RegisterPlaces(sport, rows.Select(f => f.id));
        if (columns != null && columns.Length > 0) yield return TableStep(sport, columns, rows);
        Give(sport, medals, place);
        yield return MedalStep(sport, medals, place);
        if (podium2D) yield return PodiumStep(sport, rows.Select(f => f.id).ToList(), player);
        Clear();
        yield return Fade(background, background.color.a, 0f, 0.4f);
        canvas.gameObject.SetActive(false);
    }

    public IEnumerator MedalOnly(string sport, Sprite[] medals, int place)
    {
        Create();
        canvas.gameObject.SetActive(true);
        yield return Fade(background, 0f, 0.93f, 0.4f);
        GameResults.RegisterTeam(sport, place);
        Give(sport, medals, place);
        yield return MedalStep(sport, medals, place);
        Clear();
        yield return Fade(background, background.color.a, 0f, 0.4f);
        canvas.gameObject.SetActive(false);
    }

    static void Give(string sport, Sprite[] medals, int place)
    {
        if (medals != null && place >= 0 && place < 3 && place < medals.Length) PlayerMedals.Give(sport, place, medals[place]);
    }

    IEnumerator TableStep(string sport, string[] columns, List<Row> rows)
    {
        Clear();
        CreateText(sport, textFont, 42f, new Color(0.78f, 0.72f, 1f), new Vector2(0f, 430f));
        var title = CreateText("Resultados", titleFont, 96f, new Color(1f, 0.84f, 0.35f), new Vector2(0f, 335f));
        var panel = Box(new Vector2(0f, -40f), new Vector2(1250f, 120f + rows.Count * 105f), new Color(0.1f, 0.06f, 0.22f, 0.92f));
        string header = "<pos=4%>Puesto<pos=20%>Mago";
        for (int c = 0; c < columns.Length; c++) header += "<pos=" + Column(c, columns.Length) + "%>" + columns[c];
        var tc = TextIn(panel, header, textFont, 36f, new Color(0.8f, 0.75f, 1f), new Vector2(0f, panel.sizeDelta.y * 0.5f - 60f), TextAlignmentOptions.Left);
        tc.rectTransform.sizeDelta = new Vector2(1180f, 60f);
        for (int i = 0; i < rows.Count; i++)
        {
            var m = Token(rows[i].id);
            string medal = i == 0 ? "<color=#FFD45A>" : i == 1 ? "<color=#D8DCEC>" : i == 2 ? "<color=#E39A5B>" : "<color=#9C94C4>";
            string line = "<pos=4%>" + medal + (i + 1) + ".º</color><pos=20%><color=#" + ColorUtility.ToHtmlStringRGB(m != null ? m.color : Color.white) + ">" + (m != null ? m.displayName : rows[i].id) + "</color>";
            if (rows[i].id == ChosenMage.Chosen) line += " <size=70%><color=#FFD45A>(tú)</color></size>";
            for (int c = 0; c < rows[i].values.Length; c++) line += "<pos=" + Column(c, rows[i].values.Length) + "%>" + rows[i].values[c];
            var t = TextIn(panel, line, textFont, 44f, Color.white, new Vector2(0f, panel.sizeDelta.y * 0.5f - 140f - i * 105f), TextAlignmentOptions.Left);
            t.rectTransform.sizeDelta = new Vector2(1180f, 80f);
            StartCoroutine(Appear(t.rectTransform, 0.15f + i * 0.12f));
        }
        StartCoroutine(Appear(title.rectTransform, 0f));
        yield return Wait(tableSeconds);
    }

    static int Column(int c, int total) => total <= 1 ? 72 : 64 + c * (30 / Mathf.Max(1, total - 1));

    IEnumerator MedalStep(string sport, Sprite[] medals, int place)
    {
        Clear();
        bool has = medals != null && place < 3 && place < medals.Length && medals[place] != null;
        Color tone = place == 0 ? new Color(1f, 0.82f, 0.3f) : place == 1 ? new Color(0.85f, 0.88f, 1f) : place == 2 ? new Color(0.95f, 0.6f, 0.35f) : new Color(0.7f, 0.65f, 0.9f);
        CreateText(sport, textFont, 42f, new Color(0.78f, 0.72f, 1f), new Vector2(0f, 430f));
        string[] names = { "¡Medalla de oro!", "¡Medalla de plata!", "¡Medalla de bronce!" };
        var title = CreateText(has ? names[place] : "¡Casi! Quedaste en " + (place + 1) + ".º lugar", titleFont, 104f, tone, new Vector2(0f, 300f));
        if (has) GameSound.Celebrate(GameAudio.Sound.Medal, 0.85f, 4f, SoundSynthesizer.Medal(), 0.6f);
        else GameSound.Play(SoundSynthesizer.Miss(), 0.5f);

        var r = CreateImage(Rays(), new Vector2(0f, -70f), new Vector2(1100f, 1100f));
        var cr = tone; cr.a = 0.22f; r.color = cr;
        var stars = new List<RectTransform>();
        for (int i = 0; i < 9; i++)
        {
            float ang = i * 40f * Mathf.Deg2Rad;
            var e = CreateImage(Star(), new Vector2(Mathf.Cos(ang) * UnityEngine.Random.Range(230f, 330f), -70f + Mathf.Sin(ang) * UnityEngine.Random.Range(230f, 330f)), Vector2.one * UnityEngine.Random.Range(26f, 48f));
            e.color = new Color(1f, 0.95f, 0.7f, 0.9f);
            stars.Add(e.rectTransform);
        }

        RectTransform center;
        if (has)
        {
            var s = medals[place];
            float heightValue = 470f;
            var img = CreateImage(s, new Vector2(0f, -70f), new Vector2(s.rect.width / s.rect.height * heightValue, heightValue));
            center = img.rectTransform;
        }
        else
        {
            var m = Token(ChosenMage.Chosen);
            center = Character(m != null ? m.defeat : null, new Vector2(0f, -280f), 1.4f, 8f, false).rectTransform;
        }

        StartCoroutine(Appear(title.rectTransform, 0.35f));
        float dur = 0.9f;
        for (float t = 0f; t < medalSeconds; t += Time.unscaledDeltaTime)
        {
            r.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -t * 25f);
            float s = t < dur ? EaseOutBack(t / dur) : 1f + Mathf.Sin(t * 3f) * 0.015f;
            float rotation = t < dur ? Mathf.Abs(Mathf.Cos(t / dur * Mathf.PI * 3f)) * 0.8f + 0.2f : 1f;
            if (has) center.localScale = new Vector3(s * rotation, s, 1f);
            for (int i = 0; i < stars.Count; i++)
                stars[i].localScale = Vector3.one * Mathf.Max(0f, Mathf.Sin(t * 4f + i * 1.3f)) * (t > 0.5f ? 1f : 0f);
            if (t > 0.6f && Beat()) break;
            yield return null;
        }
    }

    IEnumerator PodiumStep(string sport, List<string> order, string player)
    {
        Clear();
        CreateText(sport, textFont, 42f, new Color(0.78f, 0.72f, 1f), new Vector2(0f, 430f));
        var title = CreateText("Podio final", titleFont, 104f, new Color(1f, 0.84f, 0.35f), new Vector2(0f, 335f));
        StartCoroutine(Appear(title.rectTransform, 0f));
        if (podium == null) { yield return Wait(podiumSeconds); yield break; }

        const float esc = 1.15f;
        Vector2 sizeValue = podium.rect.size * esc;
        Vector2 pos = new Vector2(-60f, -480f + sizeValue.y * 0.5f);
        CreateImage(podium, pos, sizeValue);
        Vector2 upperLeft = pos + new Vector2(-sizeValue.x * 0.5f, sizeValue.y * 0.5f);
        for (int i = 0; i < order.Count; i++)
        {
            var m = Token(order[i]);
            if (m == null) continue;
            Vector2 foot;
            Image img;
            if (i < 3 && i < podiumPlaces.Length)
            {
                foot = upperLeft + new Vector2(podiumPlaces[i].x * esc, -podiumPlaces[i].y * esc);
                img = Character(m.celebration, foot, esc, 10f, true, i * 0.17f);
            }
            else
            {
                foot = new Vector2(pos.x + sizeValue.x * 0.5f + 190f, pos.y - sizeValue.y * 0.5f + 8f);
                img = Character(m.defeat, foot, esc, 8f, false);
            }
            StartCoroutine(Appear(img.rectTransform, 0.2f + i * 0.15f));
            if (order[i] == player)
            {
                var box = Box(foot + new Vector2(0f, i < 3 ? 345f : 290f), new Vector2(110f, 64f), new Color(0.25f, 0.16f, 0.05f, 0.95f));
                var t = TextIn(box, "Tú", titleFont, 44f, new Color(1f, 0.85f, 0.4f), Vector2.zero, TextAlignmentOptions.Center);
                t.rectTransform.sizeDelta = box.sizeDelta;
                StartCoroutine(Hover(box));
            }
        }
        yield return Wait(podiumSeconds);
    }

    class Actor { public string id; public Transform root; public Animator anim; public RuntimeAnimatorController before; public bool rootMotionBefore; public MonoBehaviour control; public PlayerMage player; public BotMage3D bot; }

    public IEnumerator Ceremony3D(Transform podium3D, List<string> order)
    {
        if (podium3D == null) yield break;
        InProgress = true;
        var actors = Actors();
        foreach (var a in actors) { if (a.control != null) a.control.enabled = false; WithoutTorch(a.root); }
        var mf = podium3D.GetComponent<MeshFilter>();
        Bounds b = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.up, new Vector3(8f, 2f, 3f));
        Vector3 front = VectorUtils.FlatDirection(podium3D.forward);
        float[] u = { 0f, 0.62f, -0.62f };
        for (int i = 0; i < order.Count; i++)
        {
            var a = actors.FirstOrDefault(x => x.id == order[i]);
            if (a == null) continue;
            Vector3 p = i < 3
                ? Ground(podium3D.TransformPoint(new Vector3(b.center.x + u[i] * b.extents.x, b.max.y + 0.5f, b.center.z)))
                : Ground(podium3D.TransformPoint(new Vector3(b.center.x - 1.45f * b.extents.x, b.max.y + 0.5f, b.center.z + b.extents.z * 0.8f)));
            Place(a, p, front);
            Animate(a, i < 3 ? "Celebrar" : "Triste");
        }

        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<ThirdPersonCamera>() : null;
        if (follow != null) follow.enabled = false;
        Vector3 center = podium3D.TransformPoint(b.center) + Vector3.up * 1.4f;

        var effects = new List<GameObject>();
        var winner = order.Count > 0 ? actors.FirstOrDefault(x => x.id == order[0]) : null;
        if (winner != null)
        {
            var aura = MagicEffects.Launch(MagicEffects.Effect.StarAura, winner.root.position, 0.45f, 0, -1, null, winner.root, -1f);
            MagicEffects.Tint(aura, MagicEffects.MageThemeColor(winner.id));
            effects.Add(aura);
        }
        GameSound.Celebrate(GameAudio.Sound.Applause, 0.6f, 4f, SoundSynthesizer.Applause(3.5f), 1.2f);
        float nextRocket = 0.8f;
        int rocket = 0;
        var bursts = new[] { MagicEffects.Effect.GoldenBurst, MagicEffects.Effect.VioletBurst, MagicEffects.Effect.BlueBurst, MagicEffects.Effect.GreenBurst };

        for (float t = 0f; t < seconds3D; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds3D;
            if (cam != null)
            {
                cam.transform.position = center + front * Mathf.Lerp(9f, 6.5f, k) + Vector3.up * Mathf.Lerp(2.6f, 1.6f, k) + Vector3.Cross(Vector3.up, front) * Mathf.Sin(k * Mathf.PI) * 1.5f;
                cam.transform.LookAt(center + Vector3.up * 0.4f);
            }
            if (t >= nextRocket && rocket < 4)
            {
                nextRocket += UnityEngine.Random.Range(1.2f, 1.7f);
                Vector3 side = Vector3.Cross(Vector3.up, front);
                Vector3 p = center + Vector3.up * UnityEngine.Random.Range(4f, 6.5f) + side * UnityEngine.Random.Range(-5f, 5f) - front * UnityEngine.Random.Range(2f, 4f);
                MagicEffects.Launch(bursts[rocket % bursts.Length], p, 1.2f);
                GameSound.Play(SoundSynthesizer.Firework(rocket % 3), 0.25f, UnityEngine.Random.Range(0.9f, 1.1f));
                rocket++;
            }
            if (t > 1f && Beat()) break;
            yield return null;
        }
        foreach (var e in effects) MagicEffects.TurnOff(e, 1.5f);
        GameSound.SilenceCelebrations(0.6f);

        foreach (var a in actors) Restore(a);
        var j = actors.FirstOrDefault(x => x.player != null);
        if (j != null)
        {
            Vector3 ahead = Ground(podium3D.TransformPoint(b.center) + front * (b.extents.z * podium3D.lossyScale.z + 2.5f) + Vector3.up * 3f);
            j.player.transform.rotation = Quaternion.LookRotation(front);
            j.player.Teleport(ahead);
            j.player.SetRespawnOutside(ahead);
        }
        foreach (var a in actors) if (a.control != null) a.control.enabled = true;
        foreach (var a in actors) if (a.bot != null && a.bot.state == BotMage3D.State.Following) a.bot.AppearNextToLeader();
        if (follow != null) { follow.enabled = true; follow.Restart(); }
        InProgress = false;
    }

    List<Actor> Actors()
    {
        var list = new List<Actor>();
        var j = FindFirstObjectByType<PlayerMage>();
        if (j != null) list.Add(new Actor { id = ChosenMage.Chosen, root = j.transform, anim = j.animator, control = j, player = j });
        foreach (var b in FindObjectsByType<BotMage3D>(FindObjectsSortMode.None))
            list.Add(new Actor { id = b.mageId, root = b.transform, anim = b.animator, control = b, bot = b });
        foreach (var a in list) if (a.anim != null) { a.before = a.anim.runtimeAnimatorController; a.rootMotionBefore = a.anim.applyRootMotion; }
        return list;
    }

    public static List<(string id, Transform root, Animator anim, MonoBehaviour control)> Mages3D()
    {
        var res = new List<(string, Transform, Animator, MonoBehaviour)>();
        var j = FindFirstObjectByType<PlayerMage>();
        if (j != null) res.Add((ChosenMage.Chosen, j.transform, j.animator, j));
        foreach (var b in FindObjectsByType<BotMage3D>(FindObjectsSortMode.None)) res.Add((b.mageId, b.transform, b.animator, b));
        return res;
    }

    void Place(Actor a, Vector3 p, Vector3 front)
    {
        if (front.sqrMagnitude > 0.01f) a.root.rotation = Quaternion.LookRotation(front);
        if (a.player != null) a.player.Teleport(p);
        else if (a.bot != null) a.bot.Teleport(p);
        else a.root.position = p;
    }

    void Animate(Actor a, string state)
    {
        if (a.anim == null || ceremoniesController == null) return;
        if (a.anim.runtimeAnimatorController != ceremoniesController) UseCeremonies(a.id, a.anim);
        a.anim.Play(state, 0, UnityEngine.Random.value * 0.3f);
    }

    void Restore(Actor a)
    {
        if (a.anim == null) return;
        ReturnToGame(a.anim, a.root);
    }

    public static void WithoutTorch(Transform mage)
    {
        if (mage == null) return;
        var j = mage.GetComponent<PlayerMage>();
        if (j != null) j.DropTorch();
        var b = mage.GetComponent<BotMage3D>();
        if (b != null) b.DropTorch();
    }

    static Vector3 Ground(Vector3 fromPoint)
    {
        float best = float.MaxValue; Vector3 p = fromPoint;
        foreach (var h in Physics.RaycastAll(fromPoint + Vector3.up * 0.5f, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (h.distance < best) { best = h.distance; p = h.point; }
        }
        return p + Vector3.up * 0.03f;
    }

    public static string Id(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return "elemental";
            case MagicElement.Nebula: return "nebulosa";
            case MagicElement.Plant: return "druida";
            case MagicElement.Lightning: return "rayo";
        }
        return "";
    }

    public CeremonyMage Token(string id) => mages.FirstOrDefault(m => m.id == id);

    static bool Beat()
    {
        var k = Keyboard.current; var m = Mouse.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame)) ||
               (m != null && m.leftButton.wasPressedThisFrame);
    }

    IEnumerator Wait(float s)
    {
        for (float t = 0f; t < s; t += Time.unscaledDeltaTime)
        {
            if (t > 0.6f && Beat()) yield break;
            yield return null;
        }
    }

    public IEnumerator Black(float fromPoint, float until, float dur)
    {
        Create();
        canvas.gameObject.SetActive(true);
        yield return Fade(black, fromPoint, until, dur);
        if (until <= 0f && background.color.a <= 0.01f && layer.childCount == 0) canvas.gameObject.SetActive(false);
    }

    IEnumerator Fade(Image img, float fromPoint, float until, float dur)
    {
        img.gameObject.SetActive(true);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            var c = img.color; c.a = Mathf.Lerp(fromPoint, until, t / dur); img.color = c;
            yield return null;
        }
        var f = img.color; f.a = until; img.color = f;
        if (until <= 0f) img.gameObject.SetActive(false);
    }

    IEnumerator Appear(RectTransform r, float wait)
    {
        r.localScale = Vector3.zero;
        for (float t = 0f; t < wait; t += Time.unscaledDeltaTime) yield return null;
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            if (r == null) yield break;
            float s = EaseOutBack(t / 0.45f);
            r.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (r != null) r.localScale = Vector3.one;
    }

    IEnumerator Hover(RectTransform r)
    {
        Vector2 p0 = r.anchoredPosition;
        for (float t = 0f; r != null; t += Time.unscaledDeltaTime)
        {
            r.anchoredPosition = p0 + Vector2.up * Mathf.Sin(t * 4f) * 8f;
            yield return null;
        }
    }

    static float EaseOutBack(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    public Sprite slot;
    public string[] testsWithGap = { "Tiro con arco", "Ping pong", "Natación" };
    public string[] gapNames = { "Arco", "Ping pong", "Natación" };
    public float gapSize = 104f;

    Canvas medalsCanvas;
    readonly Dictionary<string, Image> gaps = new Dictionary<string, Image>();
    string lastTest;
    float popUntil;

    void OnEnable() { PlayerMedals.OnWin += OnWinMedal; }
    void OnDisable() { PlayerMedals.OnWin -= OnWinMedal; }
    void OnWinMedal(PlayerMedals.Medal m) { lastTest = m.sport; popUntil = Time.unscaledTime + 0.8f; }

    IslandCinematic cinematic;
    bool CinematicInProgress()
    {
        if (cinematic == null) cinematic = FindFirstObjectByType<IslandCinematic>();
        return cinematic != null && cinematic.InProgress;
    }

    void MedalsPanel()
    {
        var world = WorldSwitcher.Instance;
        bool in3D = (world == null || (!world.InWorld2D && !world.InOtherLevel && !world.Switching)) &&
                    !InProgress && !LabyrinthIntro.InProgress && !CinematicInProgress();
        if (medalsCanvas == null)
        {
            if (!in3D) return;
            var go = new GameObject("Medallas ganadas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            medalsCanvas = go.AddComponent<Canvas>();
            medalsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            medalsCanvas.sortingOrder = 90;
            var esc = go.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1920, 1080);
            esc.matchWidthOrHeight = 0.5f;
            for (int i = 0; i < testsWithGap.Length; i++)
            {
                int k = testsWithGap.Length - 1 - i;
                var h = new GameObject("Hueco " + testsWithGap[i], typeof(RectTransform), typeof(Image));
                h.transform.SetParent(go.transform, false);
                var frame = h.GetComponent<Image>();
                frame.sprite = slot;
                frame.raycastTarget = false;
                if (slot == null) frame.color = new Color(0.08f, 0.05f, 0.18f, 0.7f);
                var rh = (RectTransform)h.transform;
                rh.anchorMin = rh.anchorMax = rh.pivot = new Vector2(1f, 1f);
                rh.anchoredPosition = new Vector2(-24f - k * (gapSize + 12f), -20f);
                rh.sizeDelta = Vector2.one * gapSize;
                var m = new GameObject("Medalla", typeof(RectTransform), typeof(Image));
                m.transform.SetParent(h.transform, false);
                var img = m.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                var rm = (RectTransform)m.transform;
                rm.anchorMin = Vector2.zero; rm.anchorMax = Vector2.one;
                rm.offsetMin = Vector2.one * gapSize * 0.12f; rm.offsetMax = -Vector2.one * gapSize * 0.12f;
                gaps[testsWithGap[i]] = img;
                var t = new GameObject("Nombre", typeof(RectTransform));
                t.transform.SetParent(h.transform, false);
                var txt = t.AddComponent<TextMeshProUGUI>();
                if (textFont != null) txt.font = textFont;
                txt.text = gapNames != null && i < gapNames.Length ? gapNames[i] : testsWithGap[i];
                txt.fontSize = 18f;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = new Color(1f, 1f, 1f, 0.85f);
                txt.outlineWidth = 0.25f;
                txt.outlineColor = new Color32(15, 8, 35, 255);
                txt.raycastTarget = false;
                txt.textWrappingMode = TextWrappingModes.NoWrap;
                var rt = txt.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -2f);
                rt.sizeDelta = new Vector2(gapSize + 20f, 24f);
            }
        }
        medalsCanvas.gameObject.SetActive(in3D);
        if (!in3D) return;
        foreach (var par in gaps)
        {
            var m = PlayerMedals.All.FirstOrDefault(x => x.sport == par.Key);
            par.Value.enabled = m != null;
            if (m != null && par.Value.sprite != m.sprite) par.Value.sprite = m.sprite;
            float k = par.Key == lastTest ? Mathf.Clamp01((popUntil - Time.unscaledTime) / 0.8f) : 0f;
            par.Value.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.4f);
        }
    }

    CanvasScaler screensScale;

    void Update()
    {
        MedalsPanel();
        if (screensScale != null && canvas != null && canvas.gameObject.activeInHierarchy)
        {
            var px = canvas.pixelRect;
            float look = px.height > 0f ? px.width / px.height : 16f / 9f;
            screensScale.matchWidthOrHeight = look >= 16f / 9f ? 1f : 0f;
        }
        for (int i = anims.Count - 1; i >= 0; i--)
        {
            var a = anims[i];
            if (a.img == null) { anims.RemoveAt(i); continue; }
            if (a.frames == null || a.frames.Length == 0) continue;
            a.t += Time.unscaledDeltaTime;
            int f = (int)(a.t * a.fps);
            f = a.repeat ? f % a.frames.Length : Mathf.Min(f, a.frames.Length - 1);
            Put(a.img, a.frames[f], a.scale);
        }
    }

    void Create()
    {
        if (canvas != null) return;
        var go = new GameObject("Interfaz ceremonia", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 170;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;
        screensScale = esc;

        background = Fullscreen("Fondo", go.transform, new Color(0.05f, 0.02f, 0.13f, 0f));
        var c = new GameObject("Capa", typeof(RectTransform));
        c.transform.SetParent(go.transform, false);
        layer = (RectTransform)c.transform;
        black = Fullscreen("Negro", go.transform, new Color(0f, 0f, 0f, 0f));
        black.gameObject.SetActive(false);
        go.SetActive(false);
    }

    static Image Fullscreen(string displayName, Transform parent, Color c)
    {
        var go = new GameObject(displayName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    void Clear()
    {
        anims.Clear();
        for (int i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);
    }

    TextMeshProUGUI CreateText(string text, TMP_FontAsset f, float sizeValue, Color c, Vector2 pos) =>
        TextIn(layer, text, f, sizeValue, c, pos, TextAlignmentOptions.Center);

    TextMeshProUGUI TextIn(RectTransform parent, string text, TMP_FontAsset f, float sizeValue, Color c, Vector2 pos, TextAlignmentOptions align)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.fontSize = sizeValue;
        t.color = c;
        t.text = text;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.outlineWidth = 0.18f;
        t.outlineColor = new Color32(20, 8, 40, 255);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1700f, sizeValue * 1.4f);
        return t;
    }

    RectTransform Box(Vector2 pos, Vector2 sizeValue, Color c)
    {
        var go = new GameObject("Caja", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(layer, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return rt;
    }

    Image CreateImage(Sprite s, Vector2 pos, Vector2 sizeValue)
    {
        var go = new GameObject("Imagen", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(layer, false);
        var img = go.GetComponent<Image>();
        img.sprite = s;
        img.raycastTarget = false;
        img.preserveAspect = true;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return img;
    }

    Image Character(Sprite[] frames, Vector2 foot, float scale, float fps, bool repeat, float offset = 0f)
    {
        var go = new GameObject("Personaje", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(layer, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = foot;
        if (frames != null && frames.Length > 0) Put(img, frames[0], scale);
        anims.Add(new AnimUI { img = img, frames = frames, fps = fps, repeat = repeat, t = offset, scale = scale });
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

    Sprite Rays()
    {
        if (rays != null) return rays;
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var c = new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float r = d.magnitude / (n * 0.5f);
                float ang = Mathf.Atan2(d.y, d.x);
                float lightning = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang * 8f)), 6f);
                float a = Mathf.Clamp01((lightning * 0.85f + 0.15f) * (1f - r)) * (r < 1f ? 1f : 0f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return rays = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite Star()
    {
        if (star != null) return star;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var c = new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 d = (new Vector2(x, y) - c) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - (Mathf.Abs(d.x) * 6f + Mathf.Abs(d.y))) + Mathf.Clamp01(1f - (Mathf.Abs(d.y) * 6f + Mathf.Abs(d.x)));
                a = Mathf.Clamp01(a + Mathf.Clamp01(1f - d.magnitude * 3f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return star = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
