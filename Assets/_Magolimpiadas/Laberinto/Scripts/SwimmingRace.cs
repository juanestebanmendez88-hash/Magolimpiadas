using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class SwimmingRace : MonoBehaviour
{
    public Transform exit;
    public float length = 38f;
    public float laneWidth = 2.8f;
    public string[] lanes = { "nebulosa", "rayo", "elemental", "druida" };
    public Transform arrival;

    public float swimmingHeight = -0.45f;
    public float floatingHeight = -0.85f;

    public float impulse = 0.5f;
    public float drag = 1f;
    public float maxSpeed = 6f;
    public Vector2 botRhythm = new Vector2(4.6f, 6.4f);

    public float cameraHeight = 14f;
    public float cameraBehind = 8f;
    public float lookAhead = 7f;

    public TMP_FontAsset titleFont;
    public TMP_FontAsset textFont;

    public bool Finished { get; private set; }
    public bool InProgress { get; private set; }
    public static bool AnyRunning { get; private set; }

    class Swimmer
    {
        public string id; public Transform root; public Animator anim; public MonoBehaviour control; public CharacterController cc;
        public RuntimeAnimatorController before; public bool rootMotionBefore; public float speedBefore;
        public int lane; public float dist, vel, rhythm, credit, time = -1f; public bool player, remote; public string state;
        public Image bar; public TMP_Text label;
    }
    readonly List<Swimmer> swimmers = new List<Swimmer>();

    Canvas canvas;
    TMP_Text bigText, helpText;

    void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (InProgress || Finished) return;
        var j = other.GetComponent<PlayerMage>();
        if (j == null || !j.enabled) return;
        if (ArcaneGames.Network.OnlineTrials.TryRequest(ArcaneGames.Network.OnlineTrial.SwimRace)) return;
        StartCoroutine(Race());
    }

    void OnDisable() { if (InProgress) AnyRunning = false; }

    public void Begin() { if (!InProgress) StartCoroutine(Race()); }

    IEnumerator Race()
    {
        InProgress = true;
        AnyRunning = true;
        var network = ArcaneGames.Network.SwimRaceSync.IsOnline ? ArcaneGames.Network.SwimRaceSync.Current : null;
        bool isAuthority = network == null || network.HasStateAuthority;
        if (network != null) network.BeginRace();
        var ceremony = PodiumCeremony.Instance;
        if (ceremony != null) yield return ceremony.Black(0f, 1f, 0.4f);

        foreach (var r in Racket.All.ToArray())
            if (r != null && r.Collected) foreach (var rend in r.GetComponentsInChildren<Renderer>()) rend.enabled = false;

        swimmers.Clear();
        foreach (var m in PodiumCeremony.Mages3D())
        {
            var n = new Swimmer { id = m.id, root = m.root, anim = m.anim, control = m.control, player = m.control is PlayerMage };
            n.remote = network != null && !n.player && ArcaneGames.Network.OnlineMatch.IsRemoteHuman(n.id);
            n.cc = m.root.GetComponent<CharacterController>();
            n.lane = Mathf.Max(0, System.Array.IndexOf(lanes, n.id));
            n.rhythm = Random.Range(botRhythm.x, botRhythm.y);
            if (n.control != null) n.control.enabled = false;
            PodiumCeremony.WithoutTorch(n.root);
            if (n.cc != null) n.cc.enabled = false;
            if (n.anim != null)
            {
                n.before = n.anim.runtimeAnimatorController; n.rootMotionBefore = n.anim.applyRootMotion; n.speedBefore = n.anim.speed;
                if (ceremony != null) ceremony.UseCeremonies(n.id, n.anim);
                n.anim.applyRootMotion = false;
            }
            swimmers.Add(n);
            Put(n, "Flotar");
        }
        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<ThirdPersonCamera>() : null;
        if (follow != null) follow.enabled = false;
        CreateInterface();
        PlaceCamera(cam, 0f, true);
        if (ceremony != null) yield return ceremony.Black(1f, 0f, 0.4f);

        helpText.text = "¡Pulsa <color=#FFE27A>Espacio</color> lo más rápido que puedas para nadar!";
        foreach (var c in new[] { "3", "2", "1", "¡Ya!" })
        {
            bigText.text = c;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                bigText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, t / 0.25f);
                PlaceCamera(cam, 0f, false);
                yield return null;
            }
        }
        bigText.text = "";

        float clock = 0f;
        int arrivedCount = 0;
        float playerEnd = -1f;
        float lastHumanArrival = -1f;
        while (true)
        {
            float dt = Time.deltaTime;
            clock += dt;
            var k = Keyboard.current;
            foreach (var n in swimmers)
            {
                if (n.time >= 0f) { Put(n, "Flotar"); continue; }
                int beats = 0;
                if (n.player)
                {
                    if (k != null && k.spaceKey.wasPressedThisFrame)
                    {
                        beats = 1;
                        GameSound.Play(GameAudio.Sound.Stroke, 0.55f, Random.Range(0.9f, 1.1f), null, 0.06f);
                        if (!isAuthority) network.SendStroke(n.lane);
                    }
                }
                else if (n.remote)
                {
                    if (isAuthority) beats = network.TakeStrokes(n.lane);
                }
                else if (isAuthority)
                {
                    n.credit += n.rhythm * Random.Range(0.85f, 1.15f) * dt;
                    while (n.credit >= 1f) { n.credit -= 1f; beats++; }
                }
                bool arrived;
                if (isAuthority)
                {
                    n.vel = Mathf.Min(maxSpeed, n.vel + beats * impulse);
                    if ((n.player || n.remote) && arrivedCount >= swimmers.Count - 1) n.vel = Mathf.Max(n.vel, 1.6f);
                    n.vel = Mathf.Max(0f, n.vel - n.vel * drag * dt);
                    n.dist = Mathf.Min(length, n.dist + n.vel * dt);
                    arrived = n.dist >= length;
                }
                else
                {
                    float previous = n.dist;
                    float target = network.DistanceOf(n.lane);
                    n.dist = Mathf.MoveTowards(n.dist, target, Mathf.Max(maxSpeed * 1.5f, Mathf.Abs(target - n.dist) * 8f) * dt);
                    n.vel = dt > 0f ? Mathf.Max(0f, n.dist - previous) / dt : 0f;
                    arrived = network.FinishTimeOf(n.lane) > 0f;
                }
                Put(n, n.vel > 0.35f ? "Nadar" : "Flotar");
                if (n.anim != null) n.anim.speed = n.state == "Nadar" ? Mathf.Lerp(0.6f, 1.6f, n.vel / 4f) : 1f;
                if (arrived)
                {
                    n.time = isAuthority ? clock : network.FinishTimeOf(n.lane);
                    if (!isAuthority) n.dist = length;
                    arrivedCount++;
                    n.label.text = Name(n.id) + "  <color=#FFE27A>" + arrivedCount + ".º</color>";
                    if (n.player || n.remote) lastHumanArrival = clock;
                    if (n.player)
                    {
                        playerEnd = clock;
                        GameSound.Celebrate(GameAudio.Sound.Ovation, arrivedCount == 1 ? 0.8f : 0.5f, 3f);
                    }
                }
                if (isAuthority && network != null) network.Publish(n.lane, n.dist, n.time);
                n.bar.fillAmount = n.dist / length;
            }
            PlaceCamera(cam, swimmers.Where(n => n.player).Select(n => n.dist).DefaultIfEmpty(0f).First(), false);
            if (network == null)
            {
                if (arrivedCount >= swimmers.Count) break;
                if (playerEnd >= 0f && clock - playerEnd > 6f) break;
            }
            else if (isAuthority)
            {
                bool humansSwimming = swimmers.Any(n => (n.player || n.remote) && n.time < 0f);
                if (arrivedCount >= swimmers.Count) break;
                if (!humansSwimming && lastHumanArrival >= 0f && clock - lastHumanArrival > 6f) break;
            }
            else if (network.IsFinished) break;
            yield return null;
        }
        if (network != null)
        {
            if (isAuthority) network.MarkFinished();
            else
                foreach (var n in swimmers)
                {
                    n.time = network.FinishTimeOf(n.lane);
                    n.dist = n.time > 0f ? length : network.DistanceOf(n.lane);
                }
        }
        foreach (var n in swimmers) if (n.anim != null) n.anim.speed = 1f;

        var order = swimmers.OrderBy(n => n.time < 0f ? 1 : 0).ThenBy(n => n.time).ThenByDescending(n => n.dist).ToList();
        int place = order.FindIndex(n => n.player);
        bigText.text = place == 0 ? "¡Ganaste!" : "¡Llegaste " + (place + 1) + ".º!";
        helpText.text = "";

        GameSound.SilenceCelebrations(1f);
        foreach (var n in swimmers) Put(n, "SalirDelAgua", true);
        for (float t = 0f; t < 2.2f; t += Time.deltaTime) { PlaceCamera(cam, length, false); yield return null; }
        if (ceremony != null) yield return ceremony.Black(0f, 1f, 0.4f);
        canvas.gameObject.SetActive(false);
        Vector3 outside = arrival != null ? arrival.position : exit.position + exit.forward * (length + 3f);
        Vector3 right = Vector3.Cross(Vector3.up, exit.forward);
        foreach (var n in swimmers)
        {
            if (ceremony != null) PodiumCeremony.ReturnToGame(n.anim, n.root);
            else if (n.anim != null) { n.anim.runtimeAnimatorController = n.before; n.anim.applyRootMotion = n.rootMotionBefore; n.anim.speed = n.speedBefore; }
            n.root.position = Ground(outside + right * ((n.lane - 1.5f) * 1.4f));
            n.root.rotation = Quaternion.LookRotation(exit.forward);
            if (n.cc != null) n.cc.enabled = true;
        }
        Physics.SyncTransforms();
        Finished = true;

        if (ceremony != null)
        {
            var rows = order.Select(n => new PodiumCeremony.Row { id = n.id, values = new[] { n.time >= 0f ? n.time.ToString("0.0") + " s" : "—" } }).ToList();
            ceremony.StartCoroutine(ceremony.Black(1f, 0f, 0.4f));
            yield return ceremony.Screens("Natación", ceremony.swimmingMedals, new[] { "Tiempo" }, rows, false);
            yield return ceremony.Ceremony3D(ceremony.level3Podium, order.Select(n => n.id).ToList());
        }
        else
        {
            foreach (var n in swimmers) if (n.control != null) n.control.enabled = true;
            if (follow != null) { follow.enabled = true; follow.Restart(); }
        }
        InProgress = false;
        AnyRunning = false;
    }

    void Put(Swimmer n, string state, bool force = false)
    {
        Vector3 right = Vector3.Cross(Vector3.up, exit.forward);
        float heightValue = state == "Nadar" ? swimmingHeight : floatingHeight;
        n.root.position = exit.position + right * ((n.lane - 1.5f) * laneWidth) + exit.forward * n.dist + Vector3.up * heightValue;
        n.root.rotation = Quaternion.LookRotation(exit.forward);
        if (n.anim == null || (!force && n.state == state)) return;
        n.state = state;
        n.anim.CrossFadeInFixedTime(state, 0.25f);
    }

    void PlaceCamera(Camera cam, float playerDist, bool jump)
    {
        if (cam == null || exit == null) return;
        float d = Mathf.Clamp(playerDist, 0f, length - lookAhead);
        Vector3 lookDirection = exit.position + exit.forward * (d + lookAhead);
        Vector3 pos = exit.position + exit.forward * (d - cameraBehind) + Vector3.up * cameraHeight;
        if (jump) cam.transform.position = pos;
        else cam.transform.position = Vector3.Lerp(cam.transform.position, pos, 1f - Mathf.Exp(-4f * Time.deltaTime));
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(lookDirection - cam.transform.position), jump ? 1f : 1f - Mathf.Exp(-5f * Time.deltaTime));
    }

    static Vector3 Ground(Vector3 p)
    {
        float best = float.MaxValue; Vector3 r = p;
        foreach (var h in Physics.RaycastAll(p + Vector3.up * 4f, Vector3.down, 15f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (h.distance < best) { best = h.distance; r = h.point; }
        }
        return r + Vector3.up * 0.05f;
    }

    string Name(string id)
    {
        var f = PodiumCeremony.Instance != null ? PodiumCeremony.Instance.Token(id) : null;
        string displayName = f != null ? f.displayName : id;
        string color = f != null ? ColorUtility.ToHtmlStringRGB(f.color) : "FFFFFF";
        return "<color=#" + color + ">" + displayName + "</color>";
    }

    void CreateInterface()
    {
        if (canvas == null)
        {
            var go = new GameObject("Interfaz natación", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 130;
            var esc = go.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1920, 1080);
            esc.matchWidthOrHeight = 0.5f;
            bigText = CreateText(go.transform, titleFont, 150f, new Vector2(0.5f, 0.6f), new Vector2(0f, 0f), new Color(1f, 0.86f, 0.4f));
            helpText = CreateText(go.transform, textFont, 44f, new Vector2(0.5f, 0f), new Vector2(0f, 90f), Color.white);
        }
        canvas.gameObject.SetActive(true);
        var old = canvas.transform.Find("Progreso");
        if (old != null) Destroy(old.gameObject);
        var panel = new GameObject("Progreso", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var background = panel.GetComponent<Image>();
        background.color = new Color(0.04f, 0.02f, 0.12f, 0.7f);
        var rp = (RectTransform)panel.transform;
        rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0f, 1f);
        rp.anchoredPosition = new Vector2(30f, -30f);
        rp.sizeDelta = new Vector2(620f, 40f + swimmers.Count * 62f);
        int i = 0;
        foreach (var n in swimmers.OrderBy(x => x.lane))
        {
            float y = -30f - i * 62f;
            n.label = CreateText(panel.transform, textFont, 32f, new Vector2(0f, 1f), new Vector2(20f, y), Color.white);
            n.label.alignment = TextAlignmentOptions.Left;
            n.label.rectTransform.pivot = new Vector2(0f, 0.5f);
            n.label.rectTransform.sizeDelta = new Vector2(320f, 50f);
            n.label.enableAutoSizing = true;
            n.label.fontSizeMin = 20f;
            n.label.fontSizeMax = 32f;
            n.label.text = Name(n.id) + (n.player ? "  <color=#FFE27A>(tú)</color>" : "");
            Bar(panel.transform, new Vector2(350f, y), new Color(1f, 1f, 1f, 0.15f));
            n.bar = Bar(panel.transform, new Vector2(350f, y), PodiumCeremony.Instance != null && PodiumCeremony.Instance.Token(n.id) != null ? PodiumCeremony.Instance.Token(n.id).color : Color.cyan);
            n.bar.type = Image.Type.Filled;
            n.bar.fillMethod = Image.FillMethod.Horizontal;
            n.bar.fillAmount = 0f;
            i++;
        }
    }

    static Sprite white;
    static Image Bar(Transform parent, Vector2 pos, Color c)
    {
        if (white == null)
        {
            var t = new Texture2D(4, 4); var px = new Color[16]; for (int k = 0; k < 16; k++) px[k] = Color.white; t.SetPixels(px); t.Apply();
            white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
        var go = new GameObject("Barra", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = white;
        img.color = c;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(250f, 22f);
        return img;
    }

    static TMP_Text CreateText(Transform parent, TMP_FontAsset f, float sizeValue, Vector2 anchor, Vector2 pos, Color c)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.fontSize = sizeValue;
        t.color = c;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(15, 8, 35, 255);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1600f, sizeValue * 1.4f);
        return t;
    }
}
