using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Enemy))]
public class DragonBoss : MonoBehaviour
{
    public float xMin = 1059f, xMax = 1071f;
    public float flightHeight = 980.6f;
    public float speed = 2.2f;

    public float overheadHeight = 4.5f;
    public float upTime = 4f;
    public float downTime = 3.5f;
    public float verticalSpeed = 3f;

    public float interval = 1.4f;
    public float intervalAbove = 0.55f;
    public float icicleDamage = 12f;

    public Rect platform = new Rect(1066f, 979f, 3f, 1f);

    public GameObject sceneBar;
    public RectTransform sceneFill;

    public static DragonBoss Current { get; private set; }
    public Enemy Enemy { get; private set; }

    Rigidbody2D rb;
    int direction = 1;
    float nextIce, phaseChange, yCurrent;
    bool up = true;

    public bool Down => !up && Mathf.Abs(yCurrent - flightHeight) < 0.4f;
    Image fill;
    GameObject bar;
    float shownHealth = 1f;

    void Awake()
    {
        Enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void OnEnable()
    {
        Current = this;
        Enemy.OnEliminate += OnDie;
        CreateBar();
        nextIce = Time.time + 1.5f;
        up = true;
        phaseChange = Time.time + upTime;
        yCurrent = transform.position.y;
        LevelEvent.Particles(LevelEvent.ParticleShape.Snowflakes, transform.position + Vector3.up, 40, 0.15f, new Vector2(1f, 4f), 0.5f, new Vector3(3f, 2f, 0f));
    }

    void OnDisable()
    {
        if (Current == this) Current = null;
        Enemy.OnEliminate -= OnDie;
        if (bar != null) bar.SetActive(false);
    }

    void OnDie(Enemy e)
    {
        if (bar != null) bar.SetActive(false);
        if (Current == this) Current = null;
        Notices.Tip("dragon_vencido", "¡Venciste al Dragón de hielo!");
    }

    public bool Active => Enemy != null && Enemy.Alive && Enemy.CurrentState == Enemy.State.Active;

    void FixedUpdate()
    {
        if (!Active) return;
        Vector2 p = rb.position;
        float x = p.x + direction * speed * Time.fixedDeltaTime;
        if (x > xMax) { x = xMax; direction = -1; }
        if (x < xMin) { x = xMin; direction = 1; }
        if (Time.time >= phaseChange)
        {
            up = !up;
            phaseChange = Time.time + (up ? upTime : downTime);
            if (!up) Notices.Tip("dragon_baja", "¡El dragón bajó! Pégale desde la plataforma");
        }
        float target = up ? flightHeight + overheadHeight : flightHeight;
        yCurrent = Mathf.MoveTowards(yCurrent, target, verticalSpeed * Time.fixedDeltaTime);
        float y = yCurrent + Mathf.Sin(Time.time * 2f) * 0.25f;
        rb.MovePosition(new Vector2(x, y));
        if (Enemy.drawing != null) Enemy.drawing.flipX = direction < 0;
    }

    void Update()
    {
        float health = Mathf.Clamp01(Enemy.Health / Enemy.maxHealth);
        if (fill != null) fill.fillAmount = health;
        BossHealthBar.ApplyFill(sceneFill, ref shownHealth, health);
        if (!Active) return;
        if (Time.time >= nextIce)
        {
            nextIce = Time.time + (up ? intervalAbove : interval);
            Vector2 fromPoint = Enemy.Center + new Vector2(Random.Range(-1.5f, 1.5f), 3.5f);
            FallenIce.Launch(fromPoint, icicleDamage);
        }
    }

    void CreateBar()
    {
        if (sceneBar != null)
        {
            bar = sceneBar;
            shownHealth = Enemy != null && Enemy.maxHealth > 0f ? Mathf.Clamp01(Enemy.Health / Enemy.maxHealth) : 1f;
            bar.SetActive(true);
            return;
        }
        if (bar != null) { bar.SetActive(true); return; }
        bar = new GameObject("Barra del jefe");
        var canvas = bar.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 92;
        var scale = bar.AddComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.matchWidthOrHeight = 0.5f;

        var background = Box(bar.transform, "Fondo", new Color(0.05f, 0.05f, 0.12f, 0.85f), new Vector2(0f, -60f), new Vector2(908f, 40f));
        var fillObject = Box(background.transform, "Vida", new Color(0.55f, 0.85f, 1f), Vector2.zero, new Vector2(900f, 32f));
        fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        var displayName = new GameObject("Nombre", typeof(RectTransform));
        displayName.transform.SetParent(bar.transform, false);
        var t = displayName.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 32; t.fontStyle = FontStyle.Bold; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.text = Enemy.displayName;
        displayName.AddComponent<Outline>().effectColor = Color.black;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -25f);
    }

    static GameObject Box(Transform parent, string displayName, Color color, Vector2 pos, Vector2 sizeValue)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = parent is RectTransform && parent.GetComponent<Canvas>() == null ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return go;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(xMin, flightHeight), new Vector3(xMax, flightHeight));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(platform.center, platform.size);
    }
}
