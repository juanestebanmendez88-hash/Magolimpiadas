using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwimmersFormation : MonoBehaviour
{
    [System.Serializable]
    public class Swimmer
    {
        public string id;
        public string displayName;
        public Color color = Color.white;
        public SpriteRenderer body;
        public SpriteRenderer ripple;
        public SpriteAnimation swimForward, swimSide, pose, fromFront, celebrate;
        public Transform spawn;

        [System.NonSerialized] public Vector2 pos, fromPoint, until;
        [System.NonSerialized] public float tMov = 1f, durMov = 1f;
        [System.NonSerialized] public bool inWater, diving, celebrating;
        [System.NonSerialized] public SpriteAnimation actual;
        [System.NonSerialized] public float tAnim;
        [System.NonSerialized] public float jumpT = -1f, jumpDuration, highJump, rotation;
        [System.NonSerialized] public float stunned, bounce, nextJump;
    }

    public List<Swimmer> swimmers = new List<Swimmer>();

    public Transform[] line = new Transform[4];
    public Transform[] diamond = new Transform[4];
    public Transform[] square = new Transform[4];
    public Transform[] flower = new Transform[4];
    public Transform flowerCenter;
    public SpriteRenderer[] petals = new SpriteRenderer[4];
    public SpriteRenderer waveBand;

    public float referencePpu = 100f;
    public float scale = 1f;
    [Range(0.5f, 1f)] public float backgroundScale = 0.84f;
    public Vector2 waterHeight = new Vector2(-1.8f, 1.8f);
    public float platformScale = 0.62f;
    public int baseOrder = 100;
    public float flowerRotation = 14f;

    Formation formation;
    bool spinning;
    float flowerAngle, visibleFlower, glowFlower, tWave = -1f, waveDuration = 2.4f;
    readonly List<(SpriteRenderer sr, Vector2 v, float t)> drops = new List<(SpriteRenderer, Vector2, float)>();
    readonly List<SpriteRenderer> freeDrops = new List<SpriteRenderer>();

    public System.Action<Vector3> OnFallIntoWater;

    void Awake()
    {
        foreach (var n in swimmers)
            if (n.ripple != null) n.ripple.sprite = PixelShapes.Ripple();
        foreach (var p in petals) if (p != null) { p.sprite = PixelShapes.Petal(); p.color = Color.clear; }
        if (waveBand != null) { waveBand.sprite = PixelShapes.WaveBand(); waveBand.gameObject.SetActive(false); }
    }

    Transform[] Points(Formation f) => f == Formation.Line ? line : f == Formation.Diamond ? diamond : f == Formation.Square ? square : flower;

    Vector2 Local(Transform t) => t != null ? (Vector2)transform.InverseTransformPoint(t.position) : Vector2.zero;

    Vector2 Slot(Formation f, int i)
    {
        var points = Points(f);
        Vector2 p = points != null && i < points.Length ? Local(points[i]) : Vector2.zero;
        if (f != Formation.Flower || flowerCenter == null || !spinning) return p;
        Vector2 c = Local(flowerCenter);
        Vector2 r = p - c;
        float radiusX = Mathf.Max(0.5f, Mathf.Abs(Local(flower[2]).x - c.x));
        float radiusY = Mathf.Max(0.3f, Mathf.Abs(Local(flower[0]).y - c.y));
        float a = Mathf.Atan2(r.y / radiusY, r.x / radiusX) + flowerAngle * Mathf.Deg2Rad;
        return c + new Vector2(Mathf.Cos(a) * radiusX, Mathf.Sin(a) * radiusY);
    }

    public void OnPlatform()
    {
        spinning = false; flowerAngle = 0f; visibleFlower = 0f; glowFlower = 0f;
        foreach (var n in swimmers)
        {
            n.pos = n.spawn != null ? Local(n.spawn) : Vector2.zero;
            n.inWater = false; n.diving = false; n.celebrating = false;
            n.jumpT = -1f; n.tMov = 1f; n.stunned = 0f; n.rotation = 0f;
            Put(n, n.fromFront);
        }
        formation = Formation.Line;
    }

    public IEnumerator DiveIn(Formation destination, float separation)
    {
        formation = destination;
        for (int i = 0; i < swimmers.Count; i++)
        {
            StartCoroutine(Dive(swimmers[i], Slot(destination, i)));
            yield return new WaitForSeconds(separation);
        }
        while (swimmers.Exists(n => n.diving)) yield return null;
    }

    IEnumerator Dive(Swimmer n, Vector2 destination)
    {
        n.diving = true;
        Vector2 start = n.pos;
        Put(n, n.pose != null && !n.pose.Empty ? n.pose : n.fromFront);
        const float dur = 0.95f;
        float heightValue = Mathf.Max(start.y, destination.y) + 2.2f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            float x = Mathf.Lerp(start.x, destination.x, k);
            float y = k < 0.4f ? Mathf.Lerp(start.y, heightValue, 1f - (1f - k / 0.4f) * (1f - k / 0.4f))
                               : Mathf.Lerp(heightValue, destination.y, ((k - 0.4f) / 0.6f) * ((k - 0.4f) / 0.6f));
            n.pos = new Vector2(x, y);
            n.rotation = -360f * Mathf.SmoothStep(0f, 1f, k);
            yield return null;
        }
        n.pos = destination;
        n.rotation = 0f;
        n.inWater = true;
        n.diving = false;
        n.until = destination; n.fromPoint = destination; n.tMov = 1f;
        Put(n, n.swimForward);
        Splash(destination, n.color, 14);
        OnFallIntoWater?.Invoke(transform.TransformPoint(destination));
    }

    public void GoTo(Formation f, float seconds)
    {
        formation = f;
        spinning = false;
        flowerAngle = 0f;
        for (int i = 0; i < swimmers.Count; i++)
        {
            var n = swimmers[i];
            n.fromPoint = n.pos;
            n.until = Slot(f, i);
            n.tMov = 0f;
            n.durMov = Mathf.Max(0.1f, seconds);
        }
    }

    public void Rotate(bool si) { spinning = si && formation == Formation.Flower; }

    public bool Moving => swimmers.Exists(n => n.tMov < 1f || n.diving);

    public Vector3 WorldPosition(int lane)
    {
        if (lane < 0 || lane >= swimmers.Count) return transform.position;
        var n = swimmers[lane];
        return transform.TransformPoint(n.pos + Vector2.up * 0.9f * Scale(n));
    }

    public Vector3 WorldCenter()
    {
        Vector2 sum = Vector2.zero;
        foreach (var n in swimmers) sum += n.pos;
        return transform.TransformPoint(swimmers.Count > 0 ? sum / swimmers.Count : Vector2.zero);
    }

    public void CorrectHit(int lane, NoteJudgment judgment, NoteType type)
    {
        if (lane < 0) { for (int c = 0; c < swimmers.Count; c++) Skip(swimmers[c], 1.05f, 0.75f); return; }
        if (lane >= swimmers.Count) return;
        var n = swimmers[lane];
        if (type == NoteType.Golden) { Skip(n, 1.35f, 0.85f); return; }
        n.bounce = judgment == NoteJudgment.Perfect ? 1f : 0.6f;
        if (judgment == NoteJudgment.Perfect && n.jumpT < 0f) Skip(n, 0.3f, 0.32f, false);
    }

    public void Miss(int lane)
    {
        if (lane < 0) { foreach (var n in swimmers) n.stunned = 1f; return; }
        if (lane < swimmers.Count) swimmers[lane].stunned = 1f;
    }

    void Skip(Swimmer n, float heightValue, float dur, bool withPose = true)
    {
        if (!n.inWater) return;
        n.jumpT = 0f; n.highJump = heightValue; n.jumpDuration = dur;
        if (withPose) Put(n, n.celebrating && n.celebrate != null && !n.celebrate.Empty ? n.celebrate : n.pose);
        if (heightValue > 0.6f) Splash(n.pos, n.color, 8);
    }

    public void Celebrate(bool si)
    {
        for (int i = 0; i < swimmers.Count; i++)
        {
            swimmers[i].celebrating = si;
            swimmers[i].nextJump = Time.time + i * 0.25f;
        }
    }

    public void Wave(float seconds)
    {
        tWave = 0f;
        waveDuration = Mathf.Max(0.5f, seconds);
        if (waveBand != null) waveBand.gameObject.SetActive(true);
    }

    public void Flower(float visible, float glow)
    {
        visibleFlower = Mathf.Clamp01(visible);
        glowFlower = Mathf.Clamp01(glow);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (spinning) flowerAngle += flowerRotation * dt;
        bool hasWave = tWave >= 0f;
        if (hasWave)
        {
            tWave += dt;
            if (tWave > waveDuration) { tWave = -1f; if (waveBand != null) waveBand.gameObject.SetActive(false); }
        }
        for (int i = 0; i < swimmers.Count; i++) Refresh(swimmers[i], i, dt, hasWave);
        DrawWave();
        DrawFlower();
        MoveDrops(dt);
    }

    void Refresh(Swimmer n, int i, float dt, bool hasWave)
    {
        if (n.body == null) return;
        if (n.inWater && n.tMov < 1f)
        {
            n.tMov = Mathf.Min(1f, n.tMov + dt / n.durMov);
            n.pos = Vector2.Lerp(n.fromPoint, n.until, Mathf.SmoothStep(0f, 1f, n.tMov));
        }
        else if (n.inWater && spinning && !n.diving) n.pos = Vector2.Lerp(n.pos, Slot(formation, i), 1f - Mathf.Exp(-6f * dt));

        if (n.celebrating && n.inWater && n.jumpT < 0f && Time.time >= n.nextJump)
        {
            Skip(n, 1.2f, 0.8f);
            n.nextJump = Time.time + 1.1f + i * 0.1f;
        }

        float heightValue = 0f;
        if (n.jumpT >= 0f)
        {
            n.jumpT += dt;
            float k = Mathf.Clamp01(n.jumpT / n.jumpDuration);
            heightValue = 4f * n.highJump * k * (1f - k);
            if (k >= 1f)
            {
                n.jumpT = -1f;
                if (n.highJump > 0.6f) Splash(n.pos, n.color, 10);
            }
        }

        float bob = n.inWater ? Mathf.Sin(Time.time * 2.4f + i * 1.7f) * 0.05f * (hasWave ? 3.5f : 1f) : 0f;
        float drift = hasWave && n.inWater ? Mathf.Sin(Time.time * 3f + i) * 0.25f * Mathf.Sin(Mathf.PI * tWave / waveDuration) : 0f;
        float tremor = n.stunned > 0f ? Mathf.Sin(Time.time * 55f) * 0.07f * n.stunned : 0f;
        float sunken = n.stunned > 0f ? -0.15f * n.stunned : 0f;
        n.stunned = Mathf.MoveTowards(n.stunned, 0f, dt * 1.8f);
        n.bounce = Mathf.MoveTowards(n.bounce, 0f, dt * 4f);

        Vector2 p = n.pos + new Vector2(tremor + drift, bob + heightValue + sunken);
        n.body.transform.localPosition = new Vector3(p.x, p.y, 0f);
        n.body.transform.localRotation = Quaternion.Euler(0f, 0f, n.rotation);

        bool moving = n.inWater && n.tMov < 1f && (n.until - n.fromPoint).sqrMagnitude > 0.04f;
        bool lateral = moving && Mathf.Abs(n.until.x - n.fromPoint.x) > Mathf.Abs(n.until.y - n.fromPoint.y) * 0.8f;
        SpriteAnimation wants;
        if (!n.inWater) wants = n.diving ? n.actual : n.fromFront;
        else if (n.jumpT >= 0f && n.highJump > 0.6f) wants = n.actual;
        else if (lateral && n.swimSide != null && !n.swimSide.Empty) wants = n.swimSide;
        else wants = n.swimForward;
        if (wants != n.actual) Put(n, wants);
        n.tAnim += dt;
        var s = Frame(n.actual, n.tAnim);
        if (s != null) n.body.sprite = s;
        n.body.flipX = n.actual == n.swimSide && n.until.x < n.fromPoint.x;

        float e = Scale(n) * (1f + n.bounce * 0.18f);
        float ppu = n.body.sprite != null ? n.body.sprite.pixelsPerUnit : referencePpu;
        n.body.transform.localScale = Vector3.one * e * ppu / referencePpu;
        n.body.sortingOrder = baseOrder - Mathf.RoundToInt(n.pos.y * 10f) + (heightValue > 0.3f || !n.inWater ? 40 : 0);
        n.body.color = Color.Lerp(Color.white, new Color(0.55f, 0.6f, 0.75f), n.stunned * 0.8f);

        if (n.ripple != null)
        {
            bool ver = n.inWater && !n.diving;
            n.ripple.enabled = ver;
            if (ver)
            {
                float beat = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + i);
                n.ripple.transform.localPosition = new Vector3(n.pos.x + drift, n.pos.y + bob * 0.3f + 0.05f, 0f);
                n.ripple.transform.localScale = new Vector3(2.1f + beat * 0.3f + heightValue * 0.8f, 0.55f + beat * 0.08f, 1f) * Scale(n);
                var c = Color.Lerp(Color.white, n.color, 0.35f); c.a = 0.35f + n.bounce * 0.3f;
                n.ripple.color = c;
                n.ripple.sortingOrder = n.body.sortingOrder - 1;
            }
        }
    }

    float Scale(Swimmer n)
    {
        if (!n.inWater && !n.diving) return scale * platformScale;
        float k = Mathf.InverseLerp(waterHeight.x, waterHeight.y, n.pos.y);
        return scale * Mathf.Lerp(1f, backgroundScale, k);
    }

    void DrawWave()
    {
        if (waveBand == null || tWave < 0f) return;
        float k = tWave / waveDuration;
        var p = waveBand.transform.localPosition;
        waveBand.transform.localPosition = new Vector3(Mathf.Lerp(-9f, 9f, k), p.y, p.z);
        var c = new Color(0.85f, 0.95f, 1f, Mathf.Sin(Mathf.PI * k) * 0.8f);
        waveBand.color = c;
    }

    void DrawFlower()
    {
        if (petals == null || flowerCenter == null) return;
        Vector2 c = Local(flowerCenter);
        for (int i = 0; i < petals.Length && i < swimmers.Count; i++)
        {
            var p = petals[i];
            if (p == null) continue;
            Vector2 toward = swimmers[i].pos - c;
            float ang = Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg - 90f;
            p.transform.localPosition = new Vector3(c.x, c.y, 0f);
            p.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            float length = Mathf.Max(0.5f, toward.magnitude) / 2f;
            p.transform.localScale = new Vector3(1.6f + glowFlower * 0.6f, length * (0.6f + 0.4f * visibleFlower), 1f);
            var col = swimmers[i].color;
            col.a = visibleFlower * (0.35f + 0.35f * glowFlower + 0.08f * Mathf.Sin(Time.time * 4f + i));
            p.color = col;
        }
    }

    public void Splash(Vector2 location, Color tint, int amount)
    {
        for (int k = 0; k < amount; k++)
        {
            SpriteRenderer sr;
            if (freeDrops.Count > 0) { sr = freeDrops[freeDrops.Count - 1]; freeDrops.RemoveAt(freeDrops.Count - 1); sr.gameObject.SetActive(true); }
            else
            {
                var go = new GameObject("Gota", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = PixelShapes.Disc(8);
            }
            sr.transform.localPosition = new Vector3(location.x + Random.Range(-0.4f, 0.4f), location.y + 0.1f, 0f);
            sr.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
            sr.color = Color.Lerp(new Color(0.75f, 0.9f, 1f), tint, Random.Range(0f, 0.3f));
            sr.sortingOrder = baseOrder + 80;
            var v = new Vector2(Random.Range(-2.2f, 2.2f), Random.Range(3f, 6f));
            drops.Add((sr, v, 0f));
        }
    }

    void MoveDrops(float dt)
    {
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            var g = drops[i];
            g.t += dt;
            g.v += Vector2.down * 14f * dt;
            g.sr.transform.localPosition += (Vector3)(g.v * dt);
            var c = g.sr.color; c.a = Mathf.Clamp01(1f - g.t / 0.7f); g.sr.color = c;
            if (g.t >= 0.7f) { g.sr.gameObject.SetActive(false); freeDrops.Add(g.sr); drops.RemoveAt(i); }
            else drops[i] = g;
        }
    }

    static void Put(Swimmer n, SpriteAnimation a)
    {
        if (a == null || a.Empty) return;
        n.actual = a;
        n.tAnim = 0f;
    }

    static Sprite Frame(SpriteAnimation a, float t)
    {
        if (a == null || a.Empty) return null;
        int i = (int)(t * a.fps);
        i = a.repeat ? i % a.frames.Length : Mathf.Min(i, a.frames.Length - 1);
        return a.frames[i];
    }
}
