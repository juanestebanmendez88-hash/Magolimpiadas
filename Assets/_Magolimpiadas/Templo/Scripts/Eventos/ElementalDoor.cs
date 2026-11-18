using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalDoor : LevelEvent
{
    public SpriteRenderer door;
    public Sprite[] doorStates;
    public Sprite[] lowering;
    public float loweringFps = 10f;
    public Collider2D block;
    public ElementalCircle[] circles;
    public Encounter beforeEncounter;

    public SpriteRenderer plate;
    public Sprite[] plateStates;
    public Rune[] runes;
    public float waitOnFinish = 3f;

    public GameObject loweringSmoke;
    public float smokeScale = 0.35f;
    public int smokeClouds = 3;

    const float SmokeFadeSeconds = 2.5f;
    const float SmokeEdgeInset = 0.5f;
    const float SmokeHeight = 0.6f;

    [System.Serializable]
    public class Rune
    {
        public MagicElement element;
        public Vector2 point;
        public SpriteRenderer glow;
        [System.NonSerialized] public bool litUp;
    }

    public static ElementalDoor Current { get; private set; }
    public bool IsOpen { get; private set; }
    public bool Completed { get; private set; }
    public static event System.Action<ElementalDoor> AnyCompleted;
    public static bool TempleCompleted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RestartMatch() => TempleCompleted = false;

    public void CompleteFromNetwork()
    {
        if (Completed) return;
        foreach (var r in runes) r.litUp = true;
        StartCoroutine(Finish());
    }

    void OnEnable() { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }

    void Start()
    {
        if (beforeEncounter != null) return;
        float best = float.MaxValue;
        foreach (var e in FindObjectsByType<Encounter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            float dx = transform.position.x - e.transform.position.x;
            if (dx >= -2f && dx < best) { best = dx; beforeEncounter = e; }
        }
    }

    public bool FreeStep => beforeEncounter == null || beforeEncounter.Finished;

    void Awake()
    {
        if (door != null && doorStates != null && doorStates.Length > 1) door.sprite = doorStates[1];
        if (plate != null && plateStates != null && plateStates.Length > 1) plate.sprite = plateStates[1];
        foreach (var r in runes) if (r.glow != null) r.glow.enabled = false;
        foreach (var c in circles) if (c != null) c.door = this;
    }

    public ElementalCircle CircleOf(MagicElement e)
    {
        foreach (var c in circles) if (c != null && c.element == e) return c;
        return null;
    }

    public Rune RuneOf(MagicElement e)
    {
        foreach (var r in runes) if (r.element == e) return r;
        return null;
    }

    public void LitCircle()
    {
        foreach (var c in circles) if (c != null && !c.Lit) return;
        Activate();
    }

    protected override void OnActivate() { StartCoroutine(Sink()); }

    IEnumerator Sink()
    {
        if (door != null && doorStates != null && doorStates.Length > 0) door.sprite = doorStates[0];
        Notices.Tip("puerta_abierta", "¡Los 4 círculos! La puerta se abre");
        yield return new WaitForSeconds(0.8f);
        foreach (var c in circles) if (c != null) c.TurnOff();
        if (door != null)
        {
            var a = door.GetComponent<AnimatedSprite>(); if (a != null) a.enabled = false;
            var smoke = SpawnSmoke(door);
            foreach (var s in lowering) { door.sprite = s; yield return new WaitForSeconds(1f / loweringFps); }
            foreach (var cloud in smoke)
            {
                if (cloud == null) continue;
                foreach (var ps in cloud.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(cloud, SmokeFadeSeconds);
            }
        }
        if (block != null) block.enabled = false;
        IsOpen = true;
        var j = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.player2D : null;
        Notices.Tip("runas", j != null ? "Párate sobre la runa " + ColorName(j.data.element) + " de la placa: es la tuya" : "Cada mago se para sobre la runa de su elemento");
    }

    List<GameObject> SpawnSmoke(SpriteRenderer target)
    {
        var clouds = new List<GameObject>();
        if (loweringSmoke == null) return clouds;
        var bounds = target.bounds;
        int count = Mathf.Max(1, smokeClouds);
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? bounds.center.x : Mathf.Lerp(bounds.min.x + SmokeEdgeInset, bounds.max.x - SmokeEdgeInset, i / (float)(count - 1));
            var cloud = Instantiate(loweringSmoke, new Vector3(x, bounds.min.y + SmokeHeight, 0f), Quaternion.identity);
            cloud.transform.localScale = Vector3.one * smokeScale;
            foreach (var r in cloud.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                r.sortingLayerID = target.sortingLayerID;
                r.sortingOrder = target.sortingOrder + 1;
            }
            clouds.Add(cloud);
        }
        return clouds;
    }

    static string ColorName(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return "naranja";
            case MagicElement.Nebula: return "morado";
            case MagicElement.Plant: return "verde";
            case MagicElement.Lightning: return "azul";
        }
        return "";
    }

    void Update()
    {
        var j = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.player2D : null;
        if (j != null && !IsOpen && FreeStep && Vector2.Distance(j.transform.position, transform.position) < 9f)
            Notices.Tip("puerta_jugador", "Cambia al arco (Q) y dispara al círculo " + ColorName(j.data.element) + ": es el tuyo");
        if (!IsOpen || Completed) return;
        foreach (var m in Mage.All)
        {
            if (m == null || !m.Alive || !m.Grounded) continue;
            var r = RuneOf(m.data.element);
            if (r == null || r.litUp) continue;
            if (Mathf.Abs(m.transform.position.x - r.point.x) < 0.7f && Mathf.Abs(m.transform.position.y - r.point.y) < 1.5f)
            {
                r.litUp = true;
                if (r.glow != null) r.glow.enabled = true;
                Sparkles(new Vector3(r.point.x, r.point.y + 1f, 0f), m.data.color, 20, 0.25f, new Vector2(1f, 3f), 0.3f, new Vector3(1f, 0.3f, 0f));
            }
        }
        foreach (var r in runes) if (!r.litUp) return;
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        Completed = true;
        TempleCompleted = true;
        AnyCompleted?.Invoke(this);
        if (plate != null && plateStates != null && plateStates.Length > 0) plate.sprite = plateStates[0];
        foreach (var r in runes) if (r.glow != null) r.glow.enabled = false;
        Vector3 plateCenter = plate != null ? plate.bounds.center : transform.position;
        Particles(ParticleShape.Stars, plateCenter, 60, 0.3f, new Vector2(2f, 6f), 1f, new Vector3(10f, 1f, 0f));
        foreach (var m in Mage.All)
            if (m != null && m.data != null) Sparkles(plateCenter, m.data.color, 30, 0.3f, new Vector2(2f, 6f), 1f, new Vector3(10f, 1f, 0f));
        if (PodiumCeremony.Instance != null)
        {
            Notices.Tip("nivel_completado", "¡Nivel completado!");
            yield return new WaitForSeconds(1.5f);
            PodiumCeremony.Instance.StartCoroutine(PodiumCeremony.Instance.TempleEnd());
            yield break;
        }
        string coins = Coin.Summary(out var winner);
        Notices.Tip("nivel_completado", "¡NIVEL COMPLETADO!\n<size=28>Monedas: " + coins +
                       (winner != null ? "\n¡" + winner.displayName + " recogió más monedas!" : "") + "</size>");
        yield return new WaitForSeconds(waitOnFinish);
        if (WorldSwitcher.Instance != null) WorldSwitcher.Instance.FinishLevel();
    }

    static Sprite spriteGlow;
    public static Sprite Glow()
    {
        if (spriteGlow != null) return spriteGlow;
        const int t = 48;
        var tex = new Texture2D(t, t, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < t; y++)
            for (int x = 0; x < t; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(t / 2f, t / 2f)) / (t / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * 0.9f));
            }
        tex.Apply();
        spriteGlow = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), t);
        return spriteGlow;
    }
}
