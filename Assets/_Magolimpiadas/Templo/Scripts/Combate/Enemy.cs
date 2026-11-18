using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public enum Movement { Skip, Walk, Hover, Still, Boss }
    public enum State { Asleep, Active, Bullseye, Eliminated }

    public string displayName = "Slime";
    public MagicElement family = MagicElement.None;
    public bool isBoss = false;
    public float maxHealth = 30f;
    public float contactDamage = 10f;

    public Movement movement = Movement.Skip;
    public float speed = 1.6f;
    public float detectionRadius = 7f;
    public bool floatWithoutGround = true;

    public bool becomesTarget = false;
    public Sprite[] bullseyeFrames;
    public Sprite[] targetBreakFrames;
    public float targetBreakFps = 11f;
    public float bullseyeTime = 5f;

    public Sprite[] deathFrames;
    public float deathFps = 10f;
    public float remainsSeconds = 1.2f;

    public SpriteRenderer drawing;
    public bool drawingFacesLeft = false;
    public SpriteRenderer icon;

    public bool onlyWithEncounter = false;

    public State CurrentState { get; private set; } = State.Asleep;
    public float Health { get; private set; }
    public bool IsBullseye => CurrentState == State.Bullseye;
    public bool Alive => CurrentState == State.Active || CurrentState == State.Asleep;
    public Vector2 Center => drawing != null ? (Vector2)drawing.bounds.center : (Vector2)transform.position;

    public event Action<Enemy> OnEliminate;
    public static event Action<Enemy, Mage, float, float> OnReceiveHit;
    public static readonly List<Enemy> All = new List<Enemy>();
    public static event Action<Enemy> AnyEliminated;

    public void EliminateFromNetwork()
    {
        if (CurrentState == State.Eliminated) return;
        breaking = false;
        Eliminate();
    }

    static readonly Dictionary<MageData, int> bullseyes = new Dictionary<MageData, int>();
    public static int TargetsFor(MageData d) => d != null && bullseyes.TryGetValue(d, out int n) ? n : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RestartMatch() => bullseyes.Clear();

    public float RemainingBullseye => CurrentState == State.Bullseye ? bullseyeEnd - Time.time : 0f;
    bool breaking, dying;
    float deathGround, fallSpeed;

    Rigidbody2D rb;
    Collider2D body;
    AnimatedSprite anim;
    Sprite[] normalFrames;
    float fpsNormal;
    Vector3 start;
    float nextJump, nextContact, bullseyeEnd, hitUntil, slowUntil, baseGravity;
    readonly RaycastHit2D[] hits = new RaycastHit2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        if (drawing == null) drawing = GetComponent<SpriteRenderer>();
        anim = GetComponent<AnimatedSprite>();
        if (anim != null) { normalFrames = anim.frames; fpsNormal = anim.fps; }
        rb.freezeRotation = true;
        if (movement == Movement.Still || movement == Movement.Boss) rb.bodyType = RigidbodyType2D.Kinematic;
        baseGravity = movement == Movement.Hover ? 0f : 3f;
        rb.gravityScale = baseGravity;
        start = transform.position;
        Health = maxHealth;
        CreateIcon();
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        foreach (var m in Mage.All) IgnoreWith(m.GetComponent<Collider2D>());
        foreach (var e in All) if (e != this && e.body != null) IgnoreWith(e.body);
    }

    void OnDisable() { All.Remove(this); }

    void IgnoreWith(Collider2D other)
    {
        if (body != null && other != null) Physics2D.IgnoreCollision(body, other, true);
    }

    public void Wake()
    {
        if (CurrentState != State.Asleep) return;
        CurrentState = State.Active;
        if (isBoss || movement == Movement.Boss) GameSound.Play(GameAudio.Sound.MonsterRoar, 0.8f);
        var j = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.player2D : null;
        if (j != null && j.data != null && Weaknesses.Wins(j.data.power.element, family))
            Notices.Tip("fuerte_" + displayName, "¡Tú eres fuerte contra el " + displayName + "!");
    }

    public float ReceivePower(ProjectileData power, Mage attacker, Vector2 point)
    {
        if (!Alive) return 1f;
        if (!onlyWithEncounter) Wake();
        float mult = Weaknesses.Multiplier(power.element, family);
        float damage = power.damage * mult;
        Color color = attacker != null && attacker.data != null ? attacker.data.color : Color.white;
        DamageNumbers.Damage(Center + Vector2.up * 0.8f, damage, mult, color);
        OnReceiveHit?.Invoke(this, attacker, damage, mult);
        if (power.slows && attacker != null && WorldSwitcher.Instance != null && WorldSwitcher.Instance.player2D == attacker)
        {
            if (mult <= 0f) Notices.Tip("hielo_inmune", "El hielo no le hace nada a los enemigos de hielo: usa el fuego (clic izquierdo)");
            else Notices.Tip("hielo", "¡Lo frenaste! El hielo ralentiza 3 segundos, pero el fuego (clic izquierdo) hace más daño");
        }
        if (mult <= 0f) return 0f;
        Health -= damage;
        hitUntil = Time.time + 0.1f;
        if (power.slows) slowUntil = Time.time + 3f;
        if (rb.bodyType == RigidbodyType2D.Dynamic && movement != Movement.Still && movement != Movement.Boss)
            rb.linearVelocity = new Vector2(Mathf.Sign(transform.position.x - point.x) * 3f, rb.linearVelocity.y + 2f);
        if (Health <= 0f) Die();
        return mult;
    }

    public float bossArrowDamage = 3f;

    public bool ReceiveArrow(Mage shooter)
    {
        if ((isBoss || movement == Movement.Boss) && CurrentState == State.Active)
        {
            DamageNumbers.Damage(Center + Vector2.up * 0.8f, bossArrowDamage, 1f, Color.white);
            OnReceiveHit?.Invoke(this, shooter, bossArrowDamage, 1f);
            Health -= bossArrowDamage;
            hitUntil = Time.time + 0.1f;
            if (Health <= 0f) Die();
            return true;
        }
        if (CurrentState != State.Bullseye) return false;
        DamageNumbers.Message(Center + Vector2.up * 0.8f, "¡Eliminado!", shooter != null ? shooter.data.color : Color.white);
        if (shooter != null && shooter.data != null) bullseyes[shooter.data] = TargetsFor(shooter.data) + 1;
        breaking = targetBreakFrames != null && targetBreakFrames.Length > 0;
        Eliminate();
        if (breaking)
        {
            if (anim != null) anim.enabled = false;
            if (drawing != null) drawing.enabled = true;
        }
        return true;
    }

    void Die()
    {
        if (becomesTarget && bullseyeFrames != null && bullseyeFrames.Length > 0) BecomeBullseye();
        else Eliminate();
    }

    void BecomeBullseye()
    {
        Notices.Tip("diana", "¡Cambia al arco (Q) y dale a la diana!");
        CurrentState = State.Bullseye;
        bullseyeEnd = Time.time + bullseyeTime;
        if (anim != null) { anim.frames = bullseyeFrames; anim.fps = 8f; }
        if (icon != null) icon.enabled = false;
        rb.linearVelocity = Vector2.zero;
    }

    void RevertToSlime()
    {
        CurrentState = State.Active;
        Health = maxHealth * 0.5f;
        if (anim != null) { anim.frames = normalFrames; anim.fps = fpsNormal; }
        if (icon != null) icon.enabled = true;
        if (drawing != null) drawing.enabled = true;
        DamageNumbers.Message(Center + Vector2.up * 0.8f, "¡Volvió!", new Color(1f, 0.85f, 0.4f));
    }

    void Eliminate()
    {
        CurrentState = State.Eliminated;
        if (icon != null) icon.enabled = false;
        if (body != null) body.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        OnEliminate?.Invoke(this);
        AnyEliminated?.Invoke(this);
        bullseyeEnd = Time.time;
        dying = !breaking && deathFrames != null && deathFrames.Length > 0 && drawing != null;
        if (dying)
        {
            if (anim != null) anim.enabled = false;
            drawing.enabled = true;
            drawing.color = Color.white;
            drawing.sprite = deathFrames[0];
            deathGround = FindGround();
            fallSpeed = 0f;
        }
    }

    float FindGround()
    {
        float width = drawing != null ? Mathf.Max(0.3f, drawing.bounds.extents.x * 0.8f) : 0.5f;
        float bestY = float.MinValue;
        foreach (float f in new[] { 0f, -0.5f, 0.5f, -1f, 1f })
        {
            Vector2 fromPoint = (Vector2)transform.position + new Vector2(f * width, 0.1f);
            if (InsideSolid(fromPoint)) continue;
            float best = float.MaxValue, y = float.MinValue;
            foreach (var h in Physics2D.RaycastAll(fromPoint, Vector2.down, 60f))
            {
                if (!IsGround(h.collider) || h.distance <= 0.001f) continue;
                if (h.distance < best) { best = h.distance; y = h.point.y; }
            }
            if (y > bestY) bestY = y;
            if (f == 0f && y > float.MinValue) return Mathf.Min(y, transform.position.y);
        }
        return bestY > float.MinValue ? Mathf.Min(bestY, transform.position.y) : transform.position.y;
    }

    bool IsGround(Collider2D c) =>
        c != null && !c.isTrigger && c != body && c.GetComponentInParent<Enemy>() == null && c.GetComponentInParent<Mage>() == null;

    bool InsideSolid(Vector2 p)
    {
        foreach (var c in Physics2D.OverlapPointAll(p)) if (IsGround(c)) return true;
        return false;
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Asleep:
                if (!onlyWithEncounter && NearestMage(out var m, out float d) && d <= detectionRadius) Wake();
                break;
            case State.Active:
                ContactDamage();
                break;
            case State.Bullseye:
                float remains = bullseyeEnd - Time.time;
                if (drawing != null) drawing.enabled = remains > 1f || Mathf.Repeat(remains, 0.2f) > 0.08f;
                if (remains <= 0f) RevertToSlime();
                break;
            case State.Eliminated:
                if (dying)
                {
                    if (transform.position.y > deathGround)
                    {
                        fallSpeed += 30f * Time.deltaTime;
                        var p = transform.position;
                        p.y = Mathf.Max(deathGround, p.y - fallSpeed * Time.deltaTime);
                        transform.position = p;
                        if (rb != null) rb.position = p;
                    }
                    float tm = Time.time - bullseyeEnd;
                    int fm = Mathf.FloorToInt(tm * deathFps);
                    if (fm < deathFrames.Length) { drawing.sprite = deathFrames[fm]; return; }
                    float fadeAmount = (tm - deathFrames.Length / Mathf.Max(1f, deathFps) - remainsSeconds) / 0.6f;
                    if (fadeAmount > 0f) { var c = drawing.color; c.a = Mathf.Clamp01(1f - fadeAmount); drawing.color = c; }
                    if (fadeAmount >= 1f) gameObject.SetActive(false);
                    return;
                }
                if (breaking)
                {
                    int f = Mathf.FloorToInt((Time.time - bullseyeEnd) * targetBreakFps);
                    if (f < targetBreakFrames.Length) { drawing.sprite = targetBreakFrames[f]; return; }
                    if (Time.time - bullseyeEnd > targetBreakFrames.Length / targetBreakFps + 0.6f) gameObject.SetActive(false);
                    return;
                }
                float t = (Time.time - bullseyeEnd) / 0.35f;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 0f, t);
                if (t >= 1f) gameObject.SetActive(false);
                return;
        }

        if (drawing != null)
        {
            Color c = Time.time < hitUntil ? new Color(1f, 0.6f, 0.6f) : Time.time < slowUntil ? new Color(0.6f, 0.85f, 1f) : Color.white;
            drawing.color = c;
        }
    }

    void FixedUpdate()
    {
        if (CurrentState == State.Eliminated || movement == Movement.Boss) return;
        bool withoutGround = floatWithoutGround && movement != Movement.Hover && !HasGround(8f);
        if (CurrentState == State.Bullseye || CurrentState == State.Asleep || movement == Movement.Still)
        {
            rb.gravityScale = withoutGround ? 0f : baseGravity;
            var v0 = rb.linearVelocity; v0.x = Mathf.MoveTowards(v0.x, 0f, 20f * Time.fixedDeltaTime); if (withoutGround) v0.y = 0f;
            rb.linearVelocity = v0;
            return;
        }
        if (!NearestMage(out var mage, out _)) return;

        float slow = Time.time < slowUntil ? 0.4f : 1f;
        Vector2 toward = (Vector2)mage.transform.position - (Vector2)transform.position;
        int dir = toward.x > 0.2f ? 1 : toward.x < -0.2f ? -1 : 0;
        if (drawing != null && dir != 0) drawing.flipX = drawingFacesLeft ? dir > 0 : dir < 0;
        Vector2 v = rb.linearVelocity;

        switch (movement)
        {
            case Movement.Hover:
                Vector2 target = (Vector2)mage.transform.position + Vector2.up * 1.2f;
                v = Vector2.MoveTowards(v, (target - (Vector2)transform.position).normalized * speed * slow, 10f * Time.fixedDeltaTime);
                break;
            case Movement.Walk:
                rb.gravityScale = withoutGround ? 0f : baseGravity;
                v.x = Mathf.MoveTowards(v.x, dir * speed * slow, 20f * Time.fixedDeltaTime);
                if (withoutGround) v.y = 0f;
                break;
            case Movement.Skip:
                rb.gravityScale = withoutGround ? 0f : baseGravity;
                if (withoutGround) { v.x = Mathf.MoveTowards(v.x, dir * speed * slow, 10f * Time.fixedDeltaTime); v.y = 0f; }
                else if (HasGround(0.15f) && Time.time >= nextJump && v.y <= 0.1f)
                {
                    v = new Vector2(dir * speed * 1.6f * slow, 6.5f);
                    nextJump = Time.time + UnityEngine.Random.Range(0.9f, 1.4f) / slow;
                }
                else if (HasGround(0.15f)) v.x = Mathf.MoveTowards(v.x, 0f, 20f * Time.fixedDeltaTime);
                break;
        }
        rb.linearVelocity = v;
    }

    void ContactDamage()
    {
        if (Time.time < nextContact || body == null) return;
        var b = body.bounds;
        foreach (var m in Mage.All)
        {
            if (m == null || !m.Alive) continue;
            var mb = m.GetComponent<Collider2D>().bounds;
            if (!b.Intersects(mb)) continue;
            m.ReceiveDamage(contactDamage, transform.position);
            nextContact = Time.time + 0.5f;
        }
    }

    bool NearestMage(out Mage best, out float dist)
    {
        best = null; dist = float.MaxValue;
        foreach (var m in Mage.All)
        {
            if (m == null || !m.Alive) continue;
            float d = Vector2.Distance(m.transform.position, transform.position);
            if (d < dist) { dist = d; best = m; }
        }
        return best != null;
    }

    bool HasGround(float length)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.1f, Vector2.down, filter, hits, length + 0.1f);
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c == body || c.GetComponentInParent<Mage>() != null || c.GetComponentInParent<Enemy>() != null) continue;
            return true;
        }
        return false;
    }

    void CreateIcon()
    {
        Color c = Weaknesses.WinnerColor(family);
        if (c.a <= 0f) return;
        if (icon == null)
        {
            var go = new GameObject("Icono debilidad");
            go.transform.SetParent(transform, false);
            icon = go.AddComponent<SpriteRenderer>();
        }
        icon.sortingOrder = 26;
        float heightValue = drawing != null && drawing.sprite != null ? drawing.sprite.bounds.max.y : 2f;
        icon.transform.localPosition = new Vector3(0f, heightValue * 0.85f, 0f);
        var icons = ElementsIcons.Load();
        var sprite = icons != null ? icons.For(Weaknesses.Winner(family)) : null;
        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.color = Color.white;
            float parentScale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            icon.transform.localScale = Vector3.one * (0.55f / Mathf.Max(0.01f, sprite.bounds.size.y) / parentScale);
            icon.transform.localPosition = new Vector3(0f, heightValue * 0.95f + 0.4f / parentScale, 0f);
        }
        else
        {
            icon.sprite = Rhombus();
            icon.color = c;
            icon.transform.localScale = Vector3.one * 0.45f;
        }
    }

    static Sprite rhombus;
    static Sprite Rhombus()
    {
        if (rhombus != null) return rhombus;
        const int t = 32;
        var tex = new Texture2D(t, t, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color[t * t];
        for (int y = 0; y < t; y++)
            for (int x = 0; x < t; x++)
            {
                float d = Mathf.Abs(x - t / 2f + 0.5f) + Mathf.Abs(y - t / 2f + 0.5f);
                px[y * t + x] = d < t / 2f - 3 ? Color.white : d < t / 2f ? new Color(0.1f, 0.05f, 0.15f, 1f) : Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        rhombus = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), t);
        return rhombus;
    }
}
