using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PingPongLevel : MonoBehaviour
{
    [System.Serializable]
    public class PingPongMage
    {
        public string id;
        public string displayName;
        public Color color = Color.white;
        public SpriteRenderer body;
        public SpriteAnimation idle, walk, run, hit;
        public Sprite ballSprite;
        public float ballFill = 1f;
        public SpriteAnimation trail;
        public SpriteAnimation powerTrail;

        [System.NonSerialized] public Vector2 feet;
        [System.NonSerialized] public float house;
        [System.NonSerialized] public SpriteAnimation actual;
        [System.NonSerialized] public float tAnim, hitUntil;
    }

    public GameObject content;
    public Camera sceneCamera;
    public GameObject hud;
    public TMP_Text scoreboard;
    public TMP_Text message;
    public GameObject messageBackground;
    public TMP_Text help;
    public RectTransform powerBar;
    public Image powerFill;
    public TMP_Text powerText;
    public Image flashImage;
    public Image film;
    public Material grayMaterial;
    public SpriteRenderer ballSprite;
    public SpriteRenderer trail;
    public SpriteRenderer shadow;
    public SpriteRenderer lookDirection;

    public List<PingPongMage> mages = new List<PingPongMage>();

    public SpriteRenderer guardian;
    public SpriteAnimation guardianIdle, guardianWalk, guardianHit, guardianThrow;
    public int launchFrame = 3;
    public SpriteAnimation whiteTrail;

    public float tableMinX = -9.6f;
    public float tableMaxX = 9.6f;
    public float tableMinY = -7.4f;
    public float tableMaxY = 4.0f;
    public float magesColumnX = -13.2f;
    public Vector2 mageHand = new Vector2(1.2f, 1.1f);
    public float guardianX = 13.8f;
    public Vector2 guardianHand = new Vector2(-2.0f, 2.6f);
    public float magesScale = 1.15f;
    public float guardianScale = 1.5f;
    public Vector2 visibleSize = new Vector2(55.3f, 31.1f);
    public float ballSize = 0.8f;

    public int pointsToWin = 5;
    [Range(1, 3)] public int maxBalls = 3;
    public float ballsInterval = 5f;
    public float minInterval = 2.5f;
    [Range(0f, 1f)] public float whiteProbability = 0.35f;
    public float initialSpeed = 8f;
    public float increasePerHit = 0.5f;
    public float maxSpeed = 17f;
    public float mageReach = 1.5f;
    public float clickMargin = 0.3f;

    public float botCharge = 0.1f;
    public float captainCharge = 0.18f;
    public float whiteExtraCharge = 0.15f;
    public float powerSpeed = 34f;
    [Range(0.05f, 1f)] public float slowMotionScale = 0.2f;
    public float slowMotionDuration = 0.9f;

    public float captainSpeed = 7f;
    public float runningCaptainSpeed = 11f;
    public float botsSpeed = 12f;
    public float botsReach = 2.4f;
    [Range(0f, 1f)] public float botSlowAccuracy = 1f;
    [Range(0f, 1f)] public float botFastAccuracy = 0.85f;

    public float guardianSpeed = 6f;

    [TextArea(2, 5)]
    public string[] intro =
    {
        "¡Bienvenidos a la Mesa del Coliseo! El Guardián del Olimpo los reta a un partido de ping pong.",
        "El Guardián lanza bolas de colores: cada bola solo la puede devolver el mago de su color. Tú eres {mago}: muévete con W y S y golpea con clic izquierdo las bolas de tu color. ¡Apunta con el ratón!",
        "Las bolas blancas las puede devolver cualquiera. ¡Y puede haber hasta tres bolas a la vez!",
        "Cada bola devuelta carga la barra de poder. Cuando esté llena, tu siguiente golpe será imparable y anotará un punto. Gana el primero que llegue a 5. ¡Buena suerte!"
    };
    public bool introOnlyFirstTime = true;

    public UnityEvent onWin;

    public static bool Won { get; private set; }
    public bool Playing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RestartMatch() => Won = false;
    [System.NonSerialized] public int botsMisses, captainMisses;

    enum Phase { Outside, Intro, Ready, Game, End }
    Phase phase = Phase.Outside;
    int captain;
    int magesPoints, guardianPoints;
    bool introSeen, whiteSeen;
    float tClick = -10f;
    float clickBlockedUntil;
    float charge;
    float nextBall;
    bool slowMotion;
    Coroutine introRoutine, tipRoutine;

    class Ball
    {
        public SpriteRenderer sr, trail, shadow;
        public bool active, toMages, power, miss, attempt;
        public Vector2 fromPoint, until, pos;
        public float t, duration, speed, peak, tTrail;
        public int color;
        public int receptor;
        public int hits;
        public int launch;
        public SpriteAnimation trace;
    }
    readonly List<Ball> balls = new List<Ball>();

    ArcaneGames.Network.PingPongSync online;
    readonly bool[] remoteHuman = new bool[4];
    readonly float[] remotePaddle = new float[4];
    readonly int[] swings = new int[4];
    readonly int[] seenSwings = new int[4];
    int guardianAnimation, guardianAnimationId, seenGuardianAnimationId;
    int outcome, outcomeId, seenOutcomeId;
    string shownMessage = "";

    bool IsReplica => online != null && !online.HasStateAuthority;

    Vector2 guardianFeet;
    SpriteAnimation animGuardian;
    float tAnimGuardian, guardianHitUntil;
    bool launching;

    Sprite whiteBall, circle;
    public Sprite WhiteBall => whiteBall;
    const float WhiteFill = 0.9f;

    void Awake()
    {
        if (content != null) content.SetActive(false);
        if (hud != null) hud.SetActive(false);
        if (sceneCamera != null) sceneCamera.enabled = false;
        whiteBall = CreateBall(Color.white, new Color(0.82f, 0.85f, 0.95f), new Color(0.3f, 0.32f, 0.45f));
        if (flashImage != null) { flashImage.sprite = CreateEdges(); flashImage.type = Image.Type.Simple; }
        if (film != null) film.sprite = CreateGradient();
        circle = CreateHoop();
        if (lookDirection != null) lookDirection.sprite = circle;
        if (shadow != null) { shadow.sprite = whiteBall; shadow.color = new Color(0f, 0f, 0.1f, 0.35f); }
        for (int i = 0; i < 4; i++)
        {
            var b = new Ball();
            if (i == 0) { b.sr = ballSprite; b.trail = trail; b.shadow = shadow; }
            else
            {
                b.sr = Instantiate(ballSprite, ballSprite.transform.parent); b.sr.name = "Bola " + (i + 1);
                b.trail = Instantiate(trail, trail.transform.parent); b.trail.name = "Estela " + (i + 1);
                b.shadow = Instantiate(shadow, shadow.transform.parent); b.shadow.name = "Sombra " + (i + 1);
            }
            Hide(b);
            balls.Add(b);
        }
    }

    public void Enter()
    {
        if (Playing || WorldSwitcher.Instance == null) return;
        if (WorldSwitcher.Instance.EnterLevel(Ignite, OnEnter)) Playing = true;
    }

    public void Exit()
    {
        if (!Playing || WorldSwitcher.Instance == null) return;
        StopAllCoroutines();
        NormalTime();
        WorldSwitcher.Instance.ExitLevel(TurnOff, () => Playing = false);
    }

    void Ignite()
    {
        content.SetActive(true);
        hud.SetActive(true);
        sceneCamera.enabled = true;
        captain = Mathf.Max(0, mages.FindIndex(m => m.id == ChosenMage.Chosen));
        online = ArcaneGames.Network.PingPongSync.IsOnline ? ArcaneGames.Network.PingPongSync.Current : null;
        for (int i = 0; i < remoteHuman.Length; i++)
            remoteHuman[i] = online != null && i < mages.Count && i != captain && ArcaneGames.Network.OnlineMatch.IsRemoteHuman(mages[i].id);
        seenOutcomeId = online != null ? online.Latest.OutcomeId : 0;
        shownMessage = "";
        Place();
        for (int i = 0; i < mages.Count && i < remotePaddle.Length; i++)
        {
            remotePaddle[i] = mages[i].feet.y;
            seenSwings[i] = online != null ? online.Latest.Swings[i] : swings[i];
        }
        seenGuardianAnimationId = online != null ? online.Latest.GuardianAnimationId : guardianAnimationId;
        message.text = "";
        charge = 0f;
        RefreshHud();
    }

    void TurnOff()
    {
        phase = Phase.Outside;
        NormalTime();
        if (OwlNotice.Owl != null && OwlNotice.Owl.IsOpen) OwlNotice.Owl.CloseNow();
        content.SetActive(false);
        hud.SetActive(false);
        sceneCamera.enabled = false;
    }

    void OnEnter()
    {
        if (IsReplica) { phase = Phase.Ready; clickBlockedUntil = Time.time + 0.4f; }
        if (!introSeen || !introOnlyFirstTime) introRoutine = StartCoroutine(Intro());
        else StartCoroutine(AfterIntro());
    }

    void JumpIntro()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = null;
        if (OwlNotice.Owl != null) OwlNotice.Owl.CloseNow();
        var tutorial = GetComponent<TutorialPingPong>();
        if (tutorial != null) tutorial.Hide();
        StartCoroutine(AfterIntro());
    }

    IEnumerator Intro()
    {
        if (!IsReplica) phase = Phase.Intro;
        introSeen = true;
        var tutorial = GetComponent<TutorialPingPong>();
        if (tutorial != null)
        {
            yield return tutorial.Show(this, mages[captain]);
            introRoutine = null;
            yield return AfterIntro();
            yield break;
        }
        var owl = OwlNotice.Owl;
        if (owl != null)
        {
            if (!owl.gameObject.activeInHierarchy) owl.gameObject.SetActive(true);
            foreach (var line in intro)
                if (!string.IsNullOrEmpty(line)) yield return owl.Say(line.Replace("{mago}", mages[captain].displayName), null, false);
            yield return owl.Close();
        }
        introRoutine = null;
        yield return AfterIntro();
    }

    IEnumerator AfterIntro()
    {
        if (online != null)
        {
            online.ReportTutorialRead(captain);
            if (IsReplica)
            {
                if (online.Latest.Phase != (int)Phase.Game) message.text = WaitingTeamMessage;
                clickBlockedUntil = Time.time + 0.4f;
                yield break;
            }
            yield return WaitForTeam();
        }
        yield return Begin();
    }

    IEnumerator WaitForTeam()
    {
        if (TeamReadTutorial()) yield break;
        phase = Phase.Intro;
        message.text = WaitingTeamMessage;
        for (float t = 0f; t < TeamTutorialTimeout && !TeamReadTutorial(); t += Time.unscaledDeltaTime) yield return null;
        message.text = "";
    }

    bool TeamReadTutorial()
    {
        for (int i = 0; i < mages.Count; i++)
            if (IsRemoteHuman(i) && !online.HasReadTutorial(i)) return false;
        return true;
    }

    public int Attempts { get; private set; }

    IEnumerator Begin()
    {
        Attempts++;
        magesPoints = guardianPoints = 0;
        charge = 0f;
        received = new int[mages.Count];
        foreach (var b in balls) Hide(b);
        RefreshHud();
        clickBlockedUntil = Time.time + 0.4f;
        yield return Message("¡Listos!", 1.2f);
        phase = Phase.Game;
        nextBall = Time.time;
    }

    void Place()
    {
        for (int i = 0; i < mages.Count; i++)
        {
            var m = mages[i];
            float y = Mathf.Lerp(MaxFeet - 0.3f, MinFeet + 0.3f, mages.Count > 1 ? i / (float)(mages.Count - 1) : 0.5f);
            m.feet = new Vector2(magesColumnX, y);
            m.house = y;
            Put(m, m.idle);
            m.hitUntil = 0f;
            if (m.body != null) m.body.transform.localScale = Vector3.one * magesScale;
        }
        guardianFeet = new Vector2(guardianX, (tableMinY + tableMaxY) * 0.5f - guardianHand.y);
        if (guardian != null)
        {
            guardian.flipX = false;
            guardian.transform.localScale = new Vector3(-guardianScale, guardianScale, 1f);
        }
        animGuardian = guardianIdle;
        launching = false;
        foreach (var b in balls) Hide(b);
        Draw();
    }

    static float Size(Sprite s) => s != null ? s.rect.width / s.pixelsPerUnit : 1f;

    void LateUpdate()
    {
        if (sceneCamera != null && sceneCamera.enabled && sceneCamera.aspect > 0.01f)
            sceneCamera.orthographicSize = Mathf.Max(visibleSize.y * 0.5f, visibleSize.x * 0.5f / sceneCamera.aspect);
        if (messageBackground != null && message != null) messageBackground.SetActive(phase != Phase.Outside && !string.IsNullOrEmpty(message.text));
    }

    void Update()
    {
        if (phase == Phase.Outside) return;
        var k = Keyboard.current;
        var r = Mouse.current;
        bool skipped = false;
        if (introRoutine != null)
        {
            var owl = OwlNotice.Owl;
            if (owl != null && owl.IsOpen) owl.Notice("Esc · saltar");
            if (k != null && k.escapeKey.wasPressedThisFrame) { JumpIntro(); skipped = true; }
        }
        if (IsReplica) { ReplicaUpdate(k, r); return; }

        if (!skipped && phase != Phase.Intro && online == null && k != null && k.escapeKey.wasPressedThisFrame) { Exit(); return; }
        if (online == null && phase != Phase.Intro && k != null && k.tabKey.wasPressedThisFrame) ChangeCaptain();
        if (phase == Phase.Game && Time.time > clickBlockedUntil &&
            ((r != null && r.leftButton.wasPressedThisFrame) || (k != null && k.spaceKey.wasPressedThisFrame)))
        {
            tClick = Time.unscaledTime;
            Hit(mages[captain]);
        }

        MoveCaptain(k);
        MoveBots();
        MoveGuardian();
        if (phase == Phase.Game)
        {
            NewBalls();
            for (int i = 0; i < balls.Count; i++) if (balls[i].active) Refresh(balls[i]);
        }
        Aim();
        Draw();
        Bar();
    }

    void MoveCaptain(Keyboard k)
    {
        var m = mages[captain];
        float axis = 0f;
        bool run = false;
        if (k != null && (phase == Phase.Game || phase == Phase.Ready))
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) axis += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) axis -= 1f;
            run = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        }
        float v = run ? runningCaptainSpeed : captainSpeed;
        m.feet.y = Mathf.Clamp(m.feet.y + axis * v * Time.deltaTime, MinFeet, MaxFeet);
        Animate(m, axis != 0f ? (run ? m.run : m.walk) : m.idle);
    }

    float MinFeet => tableMinY - mageHand.y - 0.3f;
    float MaxFeet => tableMaxY - mageHand.y + 0.3f;

    void MoveBots()
    {
        for (int i = 0; i < mages.Count; i++)
        {
            if (i == captain) continue;
            if (IsRemoteHuman(i)) { MoveTowardPaddle(mages[i], remotePaddle[i]); continue; }
            var m = mages[i];
            float meta = m.house;
            var b = phase == Phase.Game ? NextFor(i) : null;
            if (b != null) meta = b.until.y - mageHand.y;
            meta = Mathf.Clamp(meta, MinFeet, MaxFeet);
            float before = m.feet.y;
            m.feet.y = Mathf.MoveTowards(m.feet.y, meta, botsSpeed * Time.deltaTime);
            Animate(m, Mathf.Abs(m.feet.y - before) > 0.001f ? m.walk : m.idle);
        }
    }

    Ball NextFor(int mage)
    {
        Ball best = null;
        float remaining = float.MaxValue;
        foreach (var b in balls)
        {
            if (!b.active || !b.toMages || b.miss) continue;
            bool own = b.color == mage || (b.color < 0 && b.receptor == mage);
            if (!own) continue;
            float f = b.duration - b.t;
            if (f < remaining) { remaining = f; best = b; }
        }
        return best;
    }

    void MoveGuardian()
    {
        float meta = (tableMinY + tableMaxY) * 0.5f - guardianHand.y;
        float v = guardianSpeed;
        Ball next = null;
        float remaining = float.MaxValue;
        foreach (var b in balls)
        {
            if (!b.active || b.toMages) continue;
            float f = b.duration - b.t;
            if (f < remaining) { remaining = f; next = b; }
        }
        if (phase == Phase.Game && next != null) meta = next.until.y - guardianHand.y;
        else v *= 0.5f;
        if (launching) v = 0f;
        float min = tableMinY - guardianHand.y - 0.5f, max = tableMaxY - guardianHand.y + 0.5f;
        float before = guardianFeet.y;
        guardianFeet.y = Mathf.Clamp(Mathf.MoveTowards(guardianFeet.y, meta, v * Time.deltaTime), min, max);
        bool moves = Mathf.Abs(guardianFeet.y - before) > 0.001f;
        if (Time.time >= guardianHitUntil && !launching) AnimateGuardian(moves ? guardianWalk : guardianIdle);
    }

    float MagesPaddleX => magesColumnX + mageHand.x;
    float GuardianPaddleX => guardianX + guardianHand.x;
    Vector2 Paddle(PingPongMage m) => m.feet + mageHand;
    Vector2 GuardianPaddle => guardianFeet + guardianHand;

    int ActiveCount() { int n = 0; foreach (var b in balls) if (b.active) n++; return n; }
    bool HasWhite() { foreach (var b in balls) if (b.active && b.color < 0) return true; return false; }

    void NewBalls()
    {
        if (launching || Time.time < nextBall) return;
        int activeCount = ActiveCount();
        if (activeCount >= maxBalls) return;
        float advance = (magesPoints + guardianPoints) / Mathf.Max(1f, pointsToWin * 2f - 1f);
        nextBall = Time.time + Mathf.Lerp(ballsInterval, minInterval, advance);
        if (activeCount > 0 && !HasWhite() && Random.value < whiteProbability) StartCoroutine(LaunchWhite());
        else
        {
            var b = Free();
            if (b == null) return;
            AnimateGuardian(guardianHit, 2f);
            guardianHitUntil = Time.time + 0.4f;
            b.hits = 0;
            LaunchTowardMages(b, GuardianPaddle, BalancedColor(-1), initialSpeed);
        }
    }

    Ball Free() { foreach (var b in balls) if (!b.active) return b; return null; }

    [Range(1f, 2f)] public float captainPreference = 1.4f;
    int[] received = new int[4];

    int BalancedColor(int exclude)
    {
        if (received.Length != mages.Count) received = new int[mages.Count];
        int best = -1;
        float min = float.MaxValue;
        for (int i = 0; i < mages.Count; i++)
        {
            if (i == exclude && mages.Count > 1) continue;
            bool inGame = false;
            foreach (var b in balls) if (b.active && b.toMages && b.color == i) inGame = true;
            float weight = received[i] / (i == captain ? captainPreference : 1f) + Random.value * 0.8f + (inGame ? 4f : 0f);
            if (weight < min) { min = weight; best = i; }
        }
        if (best < 0) best = Random.Range(0, mages.Count);
        received[best]++;
        return best;
    }

    IEnumerator LaunchWhite()
    {
        launching = true;
        AnimateGuardian(guardianThrow);
        float fps = guardianThrow != null ? guardianThrow.fps : 12f;
        for (float t = 0f; t < launchFrame / Mathf.Max(1f, fps); t += Time.deltaTime) yield return null;
        launching = false;
        guardianHitUntil = Time.time + 0.3f;
        if (phase != Phase.Game) yield break;
        var b = Free();
        if (b == null) yield break;
        b.hits = 0;
        LaunchTowardMages(b, GuardianPaddle + new Vector2(-0.6f, -0.3f), -1, initialSpeed * 1.1f);
        if (!whiteSeen) { whiteSeen = true; Tip("¡Bola blanca! Cualquiera puede devolverla"); }
    }

    void LaunchTowardMages(Ball b, Vector2 fromPoint, int color, float speed)
    {
        b.color = color;
        float y = Random.Range(tableMinY + 0.3f, tableMaxY - 0.3f);
        if (color < 0)
        {
            b.receptor = -1;
            float d = float.MaxValue;
            for (int i = 0; i < mages.Count; i++)
            {
                if (i == captain || IsRemoteHuman(i)) continue;
                float gap = Mathf.Abs(Paddle(mages[i]).y - y);
                if (gap < d) { d = gap; b.receptor = i; }
            }
        }
        var spr = color < 0 ? whiteBall : mages[color].ballSprite;
        var trace = color < 0 ? whiteTrail : mages[color].trail;
        Launch(b, fromPoint, new Vector2(MagesPaddleX, y), true, speed, spr, trace, false);
    }

    void Launch(Ball b, Vector2 fromPoint, Vector2 until, bool toMages, float speed, Sprite spr, SpriteAnimation trace, bool power)
    {
        b.active = true;
        b.launch++;
        b.toMages = toMages;
        b.power = power;
        b.fromPoint = fromPoint;
        b.until = new Vector2(until.x, Mathf.Clamp(until.y, tableMinY + 0.2f, tableMaxY - 0.2f));
        b.pos = fromPoint;
        b.speed = speed;
        b.duration = Vector2.Distance(b.fromPoint, b.until) / Mathf.Max(1f, speed);
        b.t = 0f;
        b.miss = b.attempt = false;
        b.peak = power ? 0.3f : (b.color < 0 && toMages) ? 0.8f : Mathf.Lerp(1.8f, 0.9f, Mathf.InverseLerp(initialSpeed, maxSpeed, speed));
        b.sr.sprite = spr;
        b.sr.transform.localScale = Vector3.one * ballSize / Mathf.Max(0.01f, Size(spr) * Fill(spr));
        b.sr.gameObject.SetActive(true);
        b.shadow.gameObject.SetActive(true);
        b.trace = trace;
        b.tTrail = 0f;
        b.trail.gameObject.SetActive(trace != null && !trace.Empty);
    }

    float Fill(Sprite s)
    {
        if (s == whiteBall) return WhiteFill;
        foreach (var m in mages) if (m.ballSprite == s) return Mathf.Max(0.05f, m.ballFill);
        return 1f;
    }

    void Hide(Ball b)
    {
        b.active = false;
        if (b.sr != null) b.sr.gameObject.SetActive(false);
        if (b.trail != null) b.trail.gameObject.SetActive(false);
        if (b.shadow != null) b.shadow.gameObject.SetActive(false);
    }

    void Refresh(Ball b)
    {
        b.t += Time.deltaTime;
        float s = b.t / Mathf.Max(0.01f, b.duration);
        b.pos = b.fromPoint + (b.until - b.fromPoint) * s;

        if (b.toMages)
        {
            var cap = mages[captain];
            bool captainCanHit = b.color < 0 || b.color == captain;
            bool captainArrives = captainCanHit && Mathf.Abs(Paddle(cap).y - b.pos.y) <= mageReach;
            if (!b.miss && captainArrives && b.pos.x <= MagesPaddleX + 1.6f && b.pos.x >= MagesPaddleX - 0.9f &&
                Time.unscaledTime - tClick <= clickMargin)
            {
                tClick = -10f;
                CaptainHits(b);
                return;
            }
            bool botsTurn = b.pos.x <= MagesPaddleX - (captainArrives ? 0.9f : 0.3f);
            if (!b.miss && !b.attempt && botsTurn)
            {
                b.attempt = true;
                int bot = b.color >= 0 ? (b.color != captain && !IsRemoteHuman(b.color) ? b.color : -1) : WhiteBallBot(b);
                if (bot >= 0 && Mathf.Abs(Paddle(mages[bot]).y - b.pos.y) <= botsReach && Random.value < CorrectHit(b.speed))
                {
                    BotHits(b, bot);
                    return;
                }
                b.miss = true;
            }
            if (b.pos.x < MagesPaddleX - 4f)
            {
                if (b.color == captain) captainMisses++; else botsMisses++;
                Hide(b);
                ScorePoint(false);
            }
        }
        else
        {
            if (b.pos.x >= GuardianPaddleX && !b.miss)
            {
                if (!b.power) { GuardianReturns(b); return; }
                b.miss = true;
                AnimateGuardian(guardianHit, 0f);
                guardianHitUntil = Time.time + 0.4f;
            }
            if (b.pos.x > GuardianPaddleX + 4f) { Hide(b); ScorePoint(true); }
        }
    }

    int WhiteBallBot(Ball b)
    {
        int best = b.receptor;
        float d = best >= 0 && best != captain && !IsRemoteHuman(best) ? Mathf.Abs(Paddle(mages[best]).y - b.pos.y) : float.MaxValue;
        for (int i = 0; i < mages.Count; i++)
        {
            if (i == captain || IsRemoteHuman(i)) continue;
            float gap = Mathf.Abs(Paddle(mages[i]).y - b.pos.y);
            if (gap < d) { d = gap; best = i; }
        }
        return best == captain || IsRemoteHuman(best) ? -1 : best;
    }

    float CorrectHit(float v) => Mathf.Lerp(botSlowAccuracy, botFastAccuracy, Mathf.InverseLerp(initialSpeed, maxSpeed, v));

    float Accelerate(Ball b)
    {
        b.hits++;
        return Mathf.Min(initialSpeed + increasePerHit * b.hits, maxSpeed);
    }

    void CaptainHits(Ball b) => HitByMage(b, captain, AimY());

    void HitByMage(Ball b, int mage, float aimY)
    {
        var m = mages[mage];
        if (!(m.actual == m.hit && Time.time < m.hitUntil)) Hit(m);
        bool golden = b.color < 0;
        b.color = mage;
        if (charge >= 1f)
        {
            charge = 0f;
            Launch(b, b.pos, new Vector2(GuardianPaddleX, aimY), false, powerSpeed, m.ballSprite, m.powerTrail, true);
            if (online == null) StartCoroutine(SlowMotion(m, b));
            Tip("¡Golpe de poder!");
            Effect2D(MagicEffects.Effect2D.Rays, MagicEffects.MageColor(m.id), b.pos, 4.5f);
            GameSound.Play(GameAudio.MagePower(m.id), 0.7f);
            GameSound.Play(GameAudio.Sound.PingPong, 0.9f);
            return;
        }
        Effect2D(MagicEffects.Effect2D.Flash, MagicEffects.MageColor(m.id), b.pos, 2.2f, 1.5f);
        GameSound.Play(GameAudio.Sound.PingPong, 0.8f, 1f, SoundSynthesizer.CorrectHit(mage % 4, mage == captain));
        Load(captainCharge + (golden ? whiteExtraCharge : 0f));
        Launch(b, b.pos, new Vector2(GuardianPaddleX, aimY), false, Accelerate(b), m.ballSprite, m.trail, false);
    }

    void BotHits(Ball b, int i)
    {
        var m = mages[i];
        Hit(m);
        Effect2D(MagicEffects.Effect2D.Flash, MagicEffects.MageColor(m.id), b.pos, 1.8f, 1.5f);
        GameSound.Play(GameAudio.Sound.PingPong, 0.55f, 1.05f, SoundSynthesizer.CorrectHit(i % 4, false));
        bool golden = b.color < 0;
        b.color = i;
        Load(botCharge + (golden ? whiteExtraCharge : 0f));
        float y = GuardianPaddle.y + Random.Range(-1.5f, 1.5f);
        Launch(b, b.pos, new Vector2(GuardianPaddleX, y), false, Accelerate(b) * 0.9f, m.ballSprite, m.trail, false);
    }

    void GuardianReturns(Ball b)
    {
        AnimateGuardian(guardianHit, 2f);
        guardianHitUntil = Time.time + (guardianHit != null ? guardianHit.Duration : 0.5f) * 0.8f;
        GameSound.Play(GameAudio.Sound.PingPong, 0.5f, 0.85f);
        int lastHitter = b.color;
        b.toMages = true;
        b.color = -2;
        LaunchTowardMages(b, b.pos, BalancedColor(lastHitter), Accelerate(b));
    }

    void Load(float c)
    {
        bool before = charge >= 1f;
        charge = Mathf.Clamp01(charge + c);
        if (!before && charge >= 1f) Tip("¡Poder listo! Tu próximo golpe es imparable");
    }

    readonly Dictionary<SpriteRenderer, Material> beforeMaterials = new Dictionary<SpriteRenderer, Material>();

    IEnumerator SlowMotion(PingPongMage m, Ball b)
    {
        slowMotion = true;
        Time.timeScale = slowMotionScale;
        Gray(true, m, b);
        float entrance = 0.12f, exit = 0.35f;
        for (float t = 0f; t < slowMotionDuration; t += Time.unscaledDeltaTime)
        {
            float k = t < entrance ? t / entrance : t > slowMotionDuration - exit ? (slowMotionDuration - t) / exit : 1f;
            Shader.SetGlobalFloat("_PingPongGris", k);
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(true);
                var c = m.color; c.a = 0.9f * k * (0.85f + 0.15f * Mathf.Sin(t * 30f));
                flashImage.color = c;
            }
            yield return null;
        }
        NormalTime();
    }

    void Gray(bool si, PingPongMage m = null, Ball b = null)
    {
        if (!si)
        {
            foreach (var par in beforeMaterials) if (par.Key != null) par.Key.sharedMaterial = par.Value;
            beforeMaterials.Clear();
            Shader.SetGlobalFloat("_PingPongGris", 0f);
            return;
        }
        if (grayMaterial == null || content == null) return;
        foreach (var sr in content.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (m != null && sr == m.body) continue;
            if (b != null && (sr == b.sr || sr == b.trail)) continue;
            if (!beforeMaterials.ContainsKey(sr)) beforeMaterials[sr] = sr.sharedMaterial;
            sr.sharedMaterial = grayMaterial;
        }
    }

    void NormalTime()
    {
        if (slowMotion || Time.timeScale != 1f) Time.timeScale = 1f;
        slowMotion = false;
        Gray(false);
        if (flashImage != null) flashImage.gameObject.SetActive(false);
    }

    void ScorePoint(bool forMages)
    {
        if (phase != Phase.Game) return;
        if (forMages) magesPoints++; else guardianPoints++;
        RefreshHud();
        PointEffects(forMages);
        if (magesPoints >= pointsToWin) { StartCoroutine(Win()); return; }
        if (guardianPoints >= pointsToWin) { StartCoroutine(Lose()); return; }
        Tip(forMages ? "¡Punto para los magos!" : "Punto para el Guardián");
        if (ActiveCount() == 0) nextBall = Mathf.Min(nextBall, Time.time + 1f);
    }

    IEnumerator Win()
    {
        phase = Phase.End;
        NormalTime();
        foreach (var b in balls) Hide(b);
        Won = true;
        CelebrateWin();
        RecordOutcome(MagesWon);
        yield return Message(WinMessage, 3f);
        if (online != null) online.MarkFinished();
        var ceremony = PodiumCeremony.Instance;
        if (ceremony != null && ceremony.pingPongMedals != null && ceremony.pingPongMedals.Length > 0)
            yield return ceremony.MedalOnly("Ping pong", ceremony.pingPongMedals, Mathf.Clamp(Attempts - 1, 0, 2));
        GameSound.SilenceCelebrations(0.5f);
        onWin?.Invoke();
        Exit();
    }

    IEnumerator Lose()
    {
        phase = Phase.End;
        NormalTime();
        foreach (var b in balls) Hide(b);
        RecordOutcome(GuardianWon);
        yield return Message("El Guardián ganó esta vez...\n<size=60%>¡Inténtenlo de nuevo!</size>", 3f);
        Place();
        yield return Begin();
    }

    IEnumerator Message(string text, float seconds)
    {
        if (phase != Phase.End) phase = Phase.Ready;
        if (tipRoutine != null) { StopCoroutine(tipRoutine); tipRoutine = null; }
        message.text = text;
        var rt = message.rectTransform;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float s = t < 0.2f ? Mathf.Lerp(0.6f, 1.1f, t / 0.2f) : Mathf.Lerp(1.1f, 1f, (t - 0.2f) / 0.2f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        message.text = "";
    }

    void Tip(string text)
    {
        if (tipRoutine != null) StopCoroutine(tipRoutine);
        tipRoutine = StartCoroutine(TipRoutine(text));
    }

    IEnumerator TipRoutine(string text)
    {
        message.text = "<size=55%>" + text + "</size>";
        message.rectTransform.localScale = Vector3.one;
        for (float t = 0f; t < 1.6f && phase == Phase.Game; t += Time.unscaledDeltaTime) yield return null;
        if (phase == Phase.Game) message.text = "";
        tipRoutine = null;
    }

    float AimY()
    {
        var r = Mouse.current;
        if (r == null || sceneCamera == null) return Random.Range(tableMinY, tableMaxY);
        Vector3 p = sceneCamera.ScreenToWorldPoint(r.position.ReadValue());
        return Mathf.Clamp(transform.InverseTransformPoint(p).y, tableMinY + 0.3f, tableMaxY - 0.3f);
    }

    void Aim()
    {
        if (lookDirection == null) return;
        bool ver = phase == Phase.Game || phase == Phase.Ready;
        lookDirection.gameObject.SetActive(ver);
        if (!ver) return;
        lookDirection.transform.localPosition = new Vector3(GuardianPaddleX - 2.5f, AimY(), 0f);
        var c = mages[captain].color; c.a = 0.55f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f;
        lookDirection.color = c;
    }

    void ChangeCaptain()
    {
        captain = (captain + 1) % mages.Count;
        RefreshHud();
    }

    void RefreshHud()
    {
        scoreboard.text = "Magos  <color=#FFE27A>" + magesPoints + "</color>  ·  <color=#FFE27A>" + guardianPoints + "</color>  Guardián";
        var m = mages[captain];
        string controls = online != null ? "W/S moverse   ·   Clic golpear" : "W/S moverse   ·   Clic golpear   ·   Tab cambiar   ·   Esc salir";
        help.text = "Capitán: <color=#" + ColorUtility.ToHtmlStringRGB(m.color) + ">" + m.displayName + "</color> (tus bolas: las de tu color y las blancas)   ·   " + controls;
    }

    void Bar()
    {
        if (powerBar == null) return;
        powerBar.anchorMax = new Vector2(Mathf.Clamp01(charge), 1f);
        var c = mages[captain].color;
        bool list = charge >= 1f;
        if (powerFill != null)
            powerFill.color = list ? Color.Lerp(c, Color.white, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f)) : c;
        if (powerText != null) powerText.text = list ? "¡Poder listo!" : "Poder";
    }

    void Hit(PingPongMage m)
    {
        int mageIndex = mages.IndexOf(m);
        if (mageIndex >= 0 && mageIndex < swings.Length) swings[mageIndex]++;
        Put(m, m.hit);
        m.tAnim = 1f / Mathf.Max(1f, m.hit != null ? m.hit.fps : 12f);
        m.hitUntil = Time.time + (m.hit != null ? m.hit.Duration : 0.5f) * 0.85f;
    }

    void Animate(PingPongMage m, SpriteAnimation a)
    {
        if (Time.time < m.hitUntil) return;
        if (m.actual != a) Put(m, a);
    }

    static void Put(PingPongMage m, SpriteAnimation a) { m.actual = a; m.tAnim = 0f; }

    void AnimateGuardian(SpriteAnimation a, float fromFrame = 0f)
    {
        if (animGuardian == a && fromFrame == 0f) return;
        guardianAnimation = GuardianAnimationIndex(a);
        guardianAnimationId++;
        animGuardian = a;
        tAnimGuardian = fromFrame / Mathf.Max(1f, a != null ? a.fps : 10f);
    }

    static Sprite Frame(SpriteAnimation a, float t, bool repeat)
    {
        if (a == null || a.Empty) return null;
        int i = (int)(t * a.fps);
        i = repeat ? i % a.frames.Length : Mathf.Min(i, a.frames.Length - 1);
        return a.frames[i];
    }

    static int Order(float y) => 100 - Mathf.RoundToInt(y * 10f);

    void Draw()
    {
        foreach (var m in mages)
        {
            if (m.body == null) continue;
            m.tAnim += Time.deltaTime;
            bool isHit = m.actual == m.hit;
            var s = Frame(m.actual, m.tAnim, !isHit);
            if (s != null) m.body.sprite = s;
            m.body.transform.localPosition = m.feet;
            m.body.sortingOrder = Order(m.feet.y);
        }

        if (guardian != null)
        {
            tAnimGuardian += Time.deltaTime;
            bool one = animGuardian == guardianHit || animGuardian == guardianThrow;
            var s = Frame(animGuardian, tAnimGuardian, !one);
            if (s != null) guardian.sprite = s;
            guardian.transform.localPosition = guardianFeet;
            guardian.sortingOrder = Order(guardianFeet.y);
        }

        foreach (var b in balls) if (b.active) DrawBall(b);
    }

    void DrawBall(Ball b)
    {
        float s = Mathf.Clamp01(b.t / Mathf.Max(0.01f, b.duration));
        const float bounce = 0.72f;
        float h = s < bounce ? b.peak * 4f * (s / bounce) * (1f - s / bounce)
                           : b.peak * 0.5f * 4f * ((s - bounce) / (1f - bounce)) * (1f - (s - bounce) / (1f - bounce));
        if (b.t > b.duration) h = 0f;
        b.sr.transform.localPosition = new Vector3(b.pos.x, b.pos.y + h, 0f);
        b.sr.transform.Rotate(0f, 0f, (b.toMages ? 1f : -1f) * 540f * Time.deltaTime);
        b.shadow.transform.localPosition = new Vector3(b.pos.x, b.pos.y - 0.15f, 0f);
        float t = Mathf.Lerp(1f, 0.55f, h / 2f);
        b.shadow.transform.localScale = new Vector3(1.1f * t, 0.45f * t, 1f) * (ballSize / Mathf.Max(0.01f, Size(b.shadow.sprite) * WhiteFill));

        if (b.trail.gameObject.activeSelf && b.trace != null && !b.trace.Empty)
        {
            b.tTrail += Time.deltaTime;
            b.trail.sprite = Frame(b.trace, b.tTrail, true);
            Vector2 dir = b.until - b.fromPoint;
            b.trail.transform.position = b.sr.transform.position;
            b.trail.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            float length = b.power ? 3.2f : 1.6f;
            b.trail.transform.localScale = Vector3.one * ballSize * length / Mathf.Max(0.01f, Size(b.trail.sprite) * 0.5f);
        }
    }

    void Effect2D(MagicEffects.Effect2D e, MagicEffects.Color2D color, Vector2 local, float scale, float speed = 1f)
    {
        Vector3 p = transform.TransformPoint(new Vector3(local.x, local.y, -1f));
        MagicEffects.Launch2D(e, color, p, scale, 320, gameObject.layer, content != null ? content.transform : null, speed);
    }

    const string WinMessage = "¡Ganaron!\n<size=60%>El Guardián del Olimpo se rinde ante los cuatro magos.</size>";
    const string WaitingTeamMessage = "<size=55%>Esperando a que todos terminen el tutorial...</size>";
    const float TeamTutorialTimeout = 90f;
    const int MagesWon = 1, GuardianWon = 2;

    void CelebrateWin()
    {
        GameSound.Celebrate(GameAudio.Sound.Ovation, 0.8f, 3f, SoundSynthesizer.Fanfare());
        GameSound.Celebrate(GameAudio.Sound.Applause, 0.6f, 3.5f, SoundSynthesizer.Applause(3f), 1f);
        Effect2D(MagicEffects.Effect2D.BigBurst, MagicEffects.Color2D.Orange, new Vector2(magesColumnX + 3f, (tableMinY + tableMaxY) * 0.5f + 2f), 6f);
    }

    void PointEffects(bool forMages)
    {
        var where = new Vector2(forMages ? GuardianPaddleX : MagesPaddleX, (tableMinY + tableMaxY) * 0.5f);
        if (forMages)
        {
            Effect2D(MagicEffects.Effect2D.BigBurst, MagicEffects.Color2D.Orange, where, 4.5f);
            GameSound.Play(GameAudio.Sound.Checkpoint, 0.6f, 1f, SoundSynthesizer.Star());
            return;
        }
        Effect2D(MagicEffects.Effect2D.Burst, MagicEffects.Color2D.Gray, where, 2.2f, 1.4f);
        GameSound.Play(GameAudio.Sound.Hit, 0.5f, 1f, SoundSynthesizer.Miss());
    }

    bool IsRemoteHuman(int mage) => mage >= 0 && mage < remoteHuman.Length && remoteHuman[mage];

    void RecordOutcome(int result)
    {
        outcome = result;
        outcomeId++;
    }

    void MoveTowardPaddle(PingPongMage m, float y)
    {
        float before = m.feet.y;
        m.feet.y = Mathf.MoveTowards(m.feet.y, Mathf.Clamp(y, MinFeet, MaxFeet), runningCaptainSpeed * 1.5f * Time.deltaTime);
        Animate(m, Mathf.Abs(m.feet.y - before) > 0.001f ? m.walk : m.idle);
    }

    public void SetRemotePaddle(int mage, float y)
    {
        if (IsRemoteHuman(mage)) remotePaddle[mage] = y;
    }

    public void TryRemoteHit(int mage, int ball, int launchId, float aimY)
    {
        if (IsReplica || phase != Phase.Game || !IsRemoteHuman(mage) || ball < 0 || ball >= balls.Count) return;
        var b = balls[ball];
        if (!b.active || !b.toMages || b.launch != launchId || !(b.color < 0 || b.color == mage)) return;
        if (b.pos.x < MagesPaddleX - 3f) return;
        HitByMage(b, mage, Mathf.Clamp(aimY, tableMinY + 0.3f, tableMaxY - 0.3f));
    }

    int GuardianAnimationIndex(SpriteAnimation a) => a == guardianWalk ? 1 : a == guardianHit ? 2 : a == guardianThrow ? 3 : 0;

    SpriteAnimation GuardianAnimationFrom(int index) => index == 1 ? guardianWalk : index == 2 ? guardianHit : index == 3 ? guardianThrow : guardianIdle;

    Sprite BallSpriteFor(int color) => color < 0 || color >= mages.Count ? whiteBall : mages[color].ballSprite;

    SpriteAnimation TrailFor(int color, bool power)
    {
        if (color < 0 || color >= mages.Count) return whiteTrail;
        return power ? mages[color].powerTrail : mages[color].trail;
    }

    public void CaptureNetworkState(ArcaneGames.Network.PingPongSnapshot s)
    {
        for (int i = 0; i < s.Balls.Length; i++)
        {
            if (i >= balls.Count) { s.Balls[i] = default; continue; }
            var b = balls[i];
            s.Balls[i] = new ArcaneGames.Network.PingPongBall
            {
                LaunchId = b.launch, Active = b.active, TowardMages = b.toMages, Power = b.power, Color = b.color,
                From = b.fromPoint, To = b.until, Duration = b.duration, Elapsed = b.t, Peak = b.peak,
            };
        }
        for (int i = 0; i < s.PaddleY.Length; i++)
        {
            s.PaddleY[i] = i < mages.Count ? mages[i].feet.y : 0f;
            s.Swings[i] = swings[i];
        }
        s.GuardianY = guardianFeet.y;
        s.GuardianAnimation = guardianAnimation;
        s.GuardianAnimationId = guardianAnimationId;
        s.MagesScore = magesPoints;
        s.GuardianScore = guardianPoints;
        s.Charge = charge;
        s.Phase = (int)phase;
        s.Attempts = Attempts;
        s.Outcome = outcome;
        s.OutcomeId = outcomeId;
        s.Message = message != null ? message.text : "";
        s.HasData = true;
    }

    void ReplicaUpdate(Keyboard k, Mouse r)
    {
        var s = online.Latest;
        if (s.HasData && phase != Phase.End) ApplyReplicaState(s);

        bool reading = introRoutine != null;
        if (!reading && phase == Phase.Game && Time.time > clickBlockedUntil &&
            ((r != null && r.leftButton.wasPressedThisFrame) || (k != null && k.spaceKey.wasPressedThisFrame)))
        {
            tClick = Time.unscaledTime;
            Hit(mages[captain]);
        }
        if (phase == Phase.Game && Time.unscaledTime - tClick <= clickMargin) TrySendHit();

        MoveCaptain(reading ? null : k);
        online.SendPaddle(captain, mages[captain].feet.y);
        for (int i = 0; i < mages.Count && i < s.PaddleY.Length; i++)
        {
            if (i == captain) continue;
            MoveTowardPaddle(mages[i], s.PaddleY[i]);
            if (s.Swings[i] != seenSwings[i]) { seenSwings[i] = s.Swings[i]; Hit(mages[i]); }
        }

        guardianFeet.y = Mathf.MoveTowards(guardianFeet.y, s.GuardianY, guardianSpeed * 3f * Time.deltaTime);
        if (s.GuardianAnimationId != seenGuardianAnimationId)
        {
            seenGuardianAnimationId = s.GuardianAnimationId;
            animGuardian = GuardianAnimationFrom(s.GuardianAnimation);
            tAnimGuardian = animGuardian == guardianHit ? 2f / Mathf.Max(1f, animGuardian != null ? animGuardian.fps : 10f) : 0f;
        }

        if (phase != Phase.End) ApplyReplicaBalls(s);
        Aim();
        Draw();
        Bar();
    }

    void ApplyReplicaState(ArcaneGames.Network.PingPongSnapshot s)
    {
        phase = s.Phase == (int)Phase.Game ? Phase.Game : Phase.Ready;
        charge = s.Charge;
        Attempts = s.Attempts;
        if (s.MagesScore != magesPoints || s.GuardianScore != guardianPoints)
        {
            bool magesScored = s.MagesScore > magesPoints;
            bool guardianScored = s.GuardianScore > guardianPoints;
            magesPoints = s.MagesScore;
            guardianPoints = s.GuardianScore;
            RefreshHud();
            if (magesScored) PointEffects(true);
            else if (guardianScored) PointEffects(false);
        }
        if (s.Message != shownMessage)
        {
            shownMessage = s.Message;
            message.text = s.Message;
            message.rectTransform.localScale = Vector3.one;
        }
        if (s.OutcomeId != seenOutcomeId)
        {
            seenOutcomeId = s.OutcomeId;
            if (s.Outcome == MagesWon) StartCoroutine(ReplicaWin());
            else if (s.Outcome == GuardianWon) foreach (var b in balls) Hide(b);
        }
    }

    void TrySendHit()
    {
        var cap = mages[captain];
        for (int i = 0; i < balls.Count; i++)
        {
            var b = balls[i];
            if (!b.active || !b.toMages || !(b.color < 0 || b.color == captain)) continue;
            if (Mathf.Abs(Paddle(cap).y - b.pos.y) > mageReach + 0.3f) continue;
            if (b.pos.x > MagesPaddleX + 2.2f || b.pos.x < MagesPaddleX - 1.5f) continue;
            online.SendHit(captain, i, b.launch, AimY());
            tClick = -10f;
            return;
        }
    }

    void ApplyReplicaBalls(ArcaneGames.Network.PingPongSnapshot snapshot)
    {
        for (int i = 0; i < balls.Count && i < snapshot.Balls.Length; i++)
        {
            var s = snapshot.Balls[i];
            var b = balls[i];
            if (!s.Active) { if (b.active) Hide(b); continue; }
            if (!b.active || b.launch != s.LaunchId)
            {
                ReplicaLaunchEffects(b, s);
                b.color = s.Color;
                Launch(b, s.From, s.To, s.TowardMages, 1f, BallSpriteFor(s.Color), TrailFor(s.Color, s.Power), s.Power);
                b.launch = s.LaunchId;
                b.duration = Mathf.Max(0.01f, s.Duration);
                b.peak = s.Peak;
                b.t = s.Elapsed;
            }
            else
            {
                b.t += Time.deltaTime;
                if (Mathf.Abs(b.t - s.Elapsed) > 0.25f) b.t = s.Elapsed;
            }
            b.pos = b.fromPoint + (b.until - b.fromPoint) * (b.t / Mathf.Max(0.01f, b.duration));
        }
    }

    void ReplicaLaunchEffects(Ball b, ArcaneGames.Network.PingPongBall s)
    {
        bool wasTowardGuardian = b.active && !b.toMages;
        if (!s.TowardMages && s.Color >= 0 && s.Color < mages.Count)
        {
            var m = mages[s.Color];
            if (s.Power)
            {
                Effect2D(MagicEffects.Effect2D.Rays, MagicEffects.MageColor(m.id), s.From, 4.5f);
                GameSound.Play(GameAudio.MagePower(m.id), 0.7f);
                GameSound.Play(GameAudio.Sound.PingPong, 0.9f);
                return;
            }
            Effect2D(MagicEffects.Effect2D.Flash, MagicEffects.MageColor(m.id), s.From, 2.2f, 1.5f);
            GameSound.Play(GameAudio.Sound.PingPong, 0.7f, 1f, SoundSynthesizer.CorrectHit(s.Color % 4, s.Color == captain));
            return;
        }
        if (s.TowardMages && wasTowardGuardian) GameSound.Play(GameAudio.Sound.PingPong, 0.5f, 0.85f);
    }

    IEnumerator ReplicaWin()
    {
        phase = Phase.End;
        foreach (var b in balls) Hide(b);
        Won = true;
        CelebrateWin();
        yield return Message(WinMessage, 3f);
        var ceremony = PodiumCeremony.Instance;
        if (ceremony != null && ceremony.pingPongMedals != null && ceremony.pingPongMedals.Length > 0)
            yield return ceremony.MedalOnly("Ping pong", ceremony.pingPongMedals, Mathf.Clamp(Attempts - 1, 0, 2));
        GameSound.SilenceCelebrations(0.5f);
        onWin?.Invoke();
        Exit();
    }

    static Sprite CreateBall(Color bright, Color middle, Color edge)
    {
        const int n = 16;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var c = new Vector2(7.5f, 7.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color col = Color.clear;
                if (d <= 7.2f) col = edge;
                if (d <= 6.2f) col = Vector2.Distance(new Vector2(x, y), c + new Vector2(-1.8f, 1.8f)) < 3.6f ? bright : middle;
                if (d <= 6.2f && x == 5 && y == 10) col = Color.white;
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 32f);
    }

    static Sprite CreateEdges()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x - (n - 1) * 0.5f) / (n * 0.5f), dy = Mathf.Abs(y - (n - 1) * 0.5f) / (n * 0.5f);
                float d = Mathf.Max(dx, dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite CreateGradient()
    {
        const int heightValue = 64;
        var tex = new Texture2D(4, heightValue, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < heightValue; y++)
        {
            float t = y / (heightValue - 1f);
            var c = new Color(0f, 0f, 0.02f, Mathf.Lerp(0.82f, 0f, t * t));
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, heightValue), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite CreateHoop()
    {
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var c = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                bool hoop = d <= 15f && d >= 12.5f;
                bool cross = (Mathf.Abs(x - 15.5f) < 1f || Mathf.Abs(y - 15.5f) < 1f) && d < 6f && d > 2.5f;
                tex.SetPixel(x, y, hoop || cross ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 32f);
    }
}
