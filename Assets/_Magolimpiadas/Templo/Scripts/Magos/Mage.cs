using System;
using UnityEngine;

public enum MageMode { Power, Bow }

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Mage : MonoBehaviour
{
    public MageData data;
    public SpriteRenderer body;
    public MageControl control;
    public bool startFacingRight = true;
    public Transform startPoint;
    public float minHeight = -10000f;

    public float Health { get; private set; }
    public float MaxHealth => data != null ? data.maxHealth : 100f;
    public bool Alive => !isDead;
    public event Action<Mage> OnChangeHealth;
    public bool IsNetworkReplica { get; set; }
    public MageOrders LastOrders => orders;
    bool isDead;
    float invulnerableUntil, respawnIn;

    public static readonly System.Collections.Generic.List<Mage> All = new System.Collections.Generic.List<Mage>();

    public MageMode Mode { get; private set; } = MageMode.Power;
    public bool FacingRight { get; private set; } = true;
    public bool Grounded { get; private set; }
    public bool Climbing { get; private set; }
    public bool Attacking => action != MageAction.None;
    public Vector2 Speed => rb.linearVelocity;
    public Vector2 ShotPoint => (Vector2)transform.position + new Vector2(FacingRight ? data.shotPoint.x : -data.shotPoint.x, data.shotPoint.y);

    public event Action<Mage> OnChangeMode;
    public event Action<Mage> OnUseZonePower;
    public event Action<Mage> OnAttack;

    enum MageAction { None, Attack, Shot, Secondary, Zone, SecondaryZone }

    Rigidbody2D rb;
    CapsuleCollider2D capsule;
    MageOrders orders;
    float lastGroundedTime = -10f, lastJumpRequest = -10f, nextAttack;
    bool jumping;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    ContactFilter2D groundFilter;

    MageAction action;
    float actionStart;
    bool launchedAction;
    Vector2 aimAction;

    SpriteAnimation currentAnim;
    float animStart;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        FacingRight = startFacingRight;
        Health = MaxHealth;
    }

    void OnEnable()
    {
        action = MageAction.None;
        currentAnim = null;
        jumping = false;
        foreach (var other in All)
            if (other != null && other != this) Physics2D.IgnoreCollision(capsule, other.capsule, true);
        foreach (var e in Enemy.All)
        {
            var c = e != null ? e.GetComponent<Collider2D>() : null;
            if (c != null) Physics2D.IgnoreCollision(capsule, c, true);
        }
        if (!All.Contains(this)) All.Add(this);
    }

    public void ApplyNetworkHealth(float health, bool dead)
    {
        if (dead && !isDead) { Die(); return; }
        if (!dead && isDead) { respawnIn = Mathf.Min(respawnIn, Time.time); return; }
        if (isDead || Mathf.Abs(Health - health) < 0.01f) return;
        Health = Mathf.Clamp(health, 0f, MaxHealth);
        OnChangeHealth?.Invoke(this);
    }

    public void ReceiveDamage(float amount, Vector2 fromPoint)
    {
        if (IsNetworkReplica) return;
        if (isDead || Time.time < invulnerableUntil || amount <= 0f) return;
        Health = Mathf.Max(0f, Health - amount);
        invulnerableUntil = Time.time + 1f;
        DamageNumbers.MageDamage((Vector2)transform.position + Vector2.up * 2.4f, amount);
        rb.linearVelocity = new Vector2(Mathf.Sign(transform.position.x - fromPoint.x) * 6f, 6f);
        OnChangeHealth?.Invoke(this);
        if (Health <= 0f) Die();
    }

    public void Heal()
    {
        Health = MaxHealth;
        OnChangeHealth?.Invoke(this);
    }

    void Die()
    {
        isDead = true;
        action = MageAction.None;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        respawnIn = Time.time + 1.2f;
        OnChangeHealth?.Invoke(this);
    }

    void Respawn()
    {
        isDead = false;
        rb.simulated = true;
        Vector2 place = Checkpoint.RespawnPlace(this);
        Place(place);
        Health = MaxHealth;
        invulnerableUntil = Time.time + 1.5f;
        if (body != null) { body.enabled = true; body.color = Color.white; }
        OnChangeHealth?.Invoke(this);
        if (WorldSwitcher.Instance != null && WorldSwitcher.Instance.player2D == this && WorldSwitcher.Instance.camera2D != null)
        {
            var follow = WorldSwitcher.Instance.camera2D.GetComponent<FollowCamera2D>();
            if (follow != null) follow.Center();
        }
    }

    bool CheckDeath()
    {
        if (!isDead && !IsNetworkReplica && transform.position.y < minHeight) Die();
        if (isDead)
        {
            if (body != null) body.color = new Color(1f, 1f, 1f, Mathf.Clamp01((respawnIn - Time.time) / 1.2f));
            if (Time.time >= respawnIn) Respawn();
            return true;
        }
        if (body != null) body.enabled = Time.time >= invulnerableUntil || Mathf.Repeat(Time.time, 0.16f) > 0.06f;
        return false;
    }

    void OnDisable() { All.Remove(this); }

    public void Place(Vector2 position)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        action = MageAction.None;
    }

    public void ChangeMode()
    {
        Mode = Mode == MageMode.Power ? MageMode.Bow : MageMode.Power;
        OnChangeMode?.Invoke(this);
    }

    MageControl Control()
    {
        if (control != null && control.isActiveAndEnabled) return control;
        foreach (var c in GetComponents<MageControl>()) if (c.enabled) return c;
        return null;
    }

    void Update()
    {
        if (data == null) return;
        if (CheckDeath()) { orders = new MageOrders(); return; }
        var c = Control();
        orders = c != null ? c.ReadOrders(this) : new MageOrders();

        if (orders.jump) lastJumpRequest = Time.time;
        if (orders.changeMode && action == MageAction.None) ChangeMode();
        if (orders.useZonePower)
        {
            OnUseZonePower?.Invoke(this);
            var zone = PowerZone.Use(this);
            if (zone != null && action == MageAction.None)
            {
                action = zone.useSecondaryPower && data.hasSecondaryPower ? MageAction.SecondaryZone : MageAction.Zone;
                actionStart = Time.time;
                launchedAction = true;
                if (Mode != MageMode.Power) ChangeMode();
            }
        }

        if (action == MageAction.None && Time.time >= nextAttack)
        {
            if (orders.attackInput) Begin(Mode == MageMode.Power ? MageAction.Attack : MageAction.Shot);
            else if (orders.secondaryPower && data.hasSecondaryPower && Mode == MageMode.Power) Begin(MageAction.Secondary);
        }
        if (action != MageAction.None) FollowAction();

        if (action == MageAction.None && Mathf.Abs(orders.move) > 0.01f) FacingRight = orders.move > 0f;
        Animate();
    }

    void Begin(MageAction a)
    {
        action = a;
        actionStart = Time.time;
        launchedAction = false;
        aimAction = orders.aim;
        FacingRight = aimAction.x >= transform.position.x;
        OnAttack?.Invoke(this);
    }

    void FollowAction()
    {
        SpriteAnimation anim = ActionAnim();
        int launchFrame = action == MageAction.Attack || action == MageAction.Zone ? data.attackLaunchFrame
                        : action == MageAction.Shot ? data.arrowLaunchFrame
                        : data.secondaryLaunchFrame;
        float fps = anim.Empty ? 12f : anim.fps;
        int frame = Mathf.FloorToInt((Time.time - actionStart) * fps);
        if (!anim.Empty) launchFrame = Mathf.Clamp(launchFrame, 0, anim.frames.Length - 1);

        if (!launchedAction && frame >= launchFrame)
        {
            launchedAction = true;
            Launch();
        }
        if (Time.time - actionStart >= Mathf.Max(anim.Duration, (launchFrame + 1) / fps))
        {
            action = MageAction.None;
            nextAttack = Time.time + data.attackWait;
        }
    }

    void Launch()
    {
        Vector2 origin = ShotPoint;
        switch (action)
        {
            case MageAction.Attack:
                MagicProjectile.Launch(data.power, origin, FacingRight ? Vector2.right : Vector2.left, this);
                break;
            case MageAction.Secondary:
                MagicProjectile.Launch(data.secondaryPower, origin, FacingRight ? Vector2.right : Vector2.left, this);
                break;
            case MageAction.Shot:
                Vector2 dir = aimAction - origin;
                if (dir.sqrMagnitude < 0.01f) dir = FacingRight ? Vector2.right : Vector2.left;
                Arrow.Shoot(this, origin, dir.normalized);
                break;
        }
    }

    SpriteAnimation ActionAnim()
    {
        switch (action)
        {
            case MageAction.Attack:
            case MageAction.Zone: return data.attack;
            case MageAction.Shot: return data.bowShot;
            case MageAction.Secondary:
            case MageAction.SecondaryZone: return data.secondaryAttack != null && !data.secondaryAttack.Empty ? data.secondaryAttack : data.attack;
        }
        return null;
    }

    void FixedUpdate()
    {
        if (data == null || isDead) return;
        CheckGround();

        Vector2 v = rb.linearVelocity;
        float target = Mathf.Clamp(orders.move, -1f, 1f) * (orders.run ? data.runSpeed : data.walkSpeed);
        float accel = Grounded ? data.acceleration : data.airAcceleration;
        v.x = Mathf.MoveTowards(v.x, target, accel * Time.fixedDeltaTime);

        if (Time.time - lastJumpRequest <= data.jumpMemory && Time.time - lastGroundedTime <= data.jumpMargin)
        {
            v.y = Mathf.Sqrt(2f * data.gravity * data.jumpHeight);
            lastJumpRequest = lastGroundedTime = -10f;
            jumping = true;
            Grounded = false;
            if (WorldSwitcher.Instance != null && WorldSwitcher.Instance.player2D == this)
                GameSound.Play(GameAudio.Sound.Jump, 0.45f, UnityEngine.Random.Range(0.95f, 1.05f));
        }
        var vine = Vine.ForMage(this);
        if (vine != null && (orders.holdJump || !Grounded))
        {
            Climbing = true;
            jumping = false;
            v.y = orders.holdJump ? vine.climbSpeed : Mathf.Max(v.y, -1.2f);
            rb.gravityScale = orders.holdJump ? 0f : 0.2f;
            rb.linearVelocity = v;
            return;
        }
        Climbing = false;

        if (jumping && !orders.holdJump && v.y > 0f) { v.y *= 0.5f; jumping = false; }
        if (v.y <= 0f) jumping = false;

        rb.gravityScale = data.gravity / Mathf.Abs(Physics2D.gravity.y) * (v.y < 0f ? data.fallMultiplier : 1f);
        v.y = Mathf.Max(v.y, -data.maxFallSpeed);
        rb.linearVelocity = v;
    }

    void CheckGround()
    {
        Grounded = false;
        if (jumping || (rb.linearVelocity.y > 0.1f && Climbing)) return;
        int n = capsule.Cast(Vector2.down, groundFilter, hits, 0.06f);
        for (int i = 0; i < n; i++)
            if (hits[i].normal.y > 0.6f) { Grounded = true; lastGroundedTime = Time.time; return; }
    }

    void Animate()
    {
        if (body == null) return;
        bool bow = Mode == MageMode.Bow;
        SpriteAnimation anim;
        if (action != MageAction.None) anim = ActionAnim();
        else if (Climbing && data.climb != null && !data.climb.Empty) anim = data.climb;
        else if (!Grounded) anim = bow ? data.airBow : data.air;
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.2f && Mathf.Abs(orders.move) > 0.01f)
            anim = orders.run ? (bow ? data.runBow : data.run) : (bow ? data.walkBow : data.walk);
        else anim = bow ? data.bowIdle : data.idle;

        if (anim == null || anim.Empty) anim = data.idle;
        if (anim == null || anim.Empty) return;

        if (anim != currentAnim)
        {
            currentAnim = anim;
            animStart = action != MageAction.None ? actionStart : Time.time;
        }
        int f = Mathf.FloorToInt((Time.time - animStart) * anim.fps);
        f = anim.repeat ? f % anim.frames.Length : Mathf.Min(f, anim.frames.Length - 1);
        if (anim == data.climb && rb.linearVelocity.y < 0.5f) f = 0;
        body.sprite = anim.frames[f];
        body.flipX = !FacingRight;
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.magenta;
        Vector3 p = transform.position + new Vector3(data.shotPoint.x, data.shotPoint.y);
        Gizmos.DrawWireSphere(p, 0.15f);
    }
}
