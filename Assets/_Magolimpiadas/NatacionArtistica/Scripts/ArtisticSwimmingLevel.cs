using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ArtisticSwimmingLevel : MonoBehaviour
{
    public SwimmingSettings settings;
    public SwimmingRoutine routine;

    public GameObject content;
    public Camera sceneCamera;
    public NoteLanes lanes;
    public SwimmersFormation formation;
    public OwlsJury jury;
    public SwimmingEffects effects;
    public SwimmingHud hud;
    public SwimmingMusic music;
    public Vector2 visibleSize = new Vector2(30f, 16.875f);

    public string[] laneId = { "elemental", "nebulosa", "druida", "rayo" };
    public string[] laneName = { "Mago Elemental", "Maga Nebulosa", "Druida Verde", "Mago del Rayo" };

    public string testName = "Natación artística";
    public Sprite[] medals;
    public string finalScene = "99_CeremoniaFinal";

    [TextArea(2, 5)]
    public string[] intro =
    {
        "¡Bienvenidos a la Piscina de la Luna! La última prueba de las Magolimpiadas es la natación artística: los cuatro nadan juntos una rutina de cuatro figuras.",
        "Cada mago tiene un carril de notas de su color. Tú eres {mago}: pulsa Espacio (o clic) justo cuando una nota de tu color cruce la línea dorada.",
        "Las notas blancas son de los cuatro: solo cuentan si todos aciertan a la vez. Las doradas dan puntos extra. Y las rojas con una X son trampas: ¡no las toques!",
        "Siete búhos jueces califican cada figura. Si una figura sale mal, se repite. Con la nota final ganan oro, plata o bronce. ¡Al agua!",
    };
    public bool introOnlyFirstTime = true;

    public bool playSolo;

    public static bool Won { get; private set; }
    public bool Playing { get; private set; }

    enum Phase { Outside, Intro, Figure, Pause, End }
    Phase phase = Phase.Outside;
    bool introSeen;
    int playerLane;
    int currentFigure;
    float currentBeat;
    float beatBlockedUntil;
    float[] figureNotes = new float[4];
    readonly NotesJudge judge = new NotesJudge();
    readonly SwimmingScoreboard scoreboard = new SwimmingScoreboard();
    readonly RoutineClock clock = new RoutineClock();
    Coroutine match, introRoutine;
    bool warnedWave;
    int lastWholeBeat = int.MinValue;
    int figurePlay = -1;

    static ArcaneGames.Network.ArtisticSwimmingSync Online =>
        ArcaneGames.Network.ArtisticSwimmingSync.IsOnline ? ArcaneGames.Network.ArtisticSwimmingSync.Current : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RestartMatch() => Won = false;

    void Awake()
    {
        if (content != null) content.SetActive(false);
        if (hud != null) hud.Show(false);
        if (sceneCamera != null) sceneCamera.enabled = false;
        judge.OnJudge += OnJudge;
        judge.OnPress += (c, withNote) => { if (lanes != null) lanes.Flash(c, withNote); };
        judge.OnTouchTrap += OnTouchTrap;
        judge.LocalLaneJudged += (note, judgment, trap) => { if (Online != null) Online.SendLocalJudgment(figurePlay, note, playerLane, judgment, trap); };
        scoreboard.OnWinStar += () => { if (effects != null && lanes != null) effects.Star(BullseyePos(playerLane) + Vector3.up * 2.4f); };
        if (formation != null) formation.OnFallIntoWater += p => { if (effects != null) effects.SplashDown(p); };
    }

    public void Enter()
    {
        if (Playing || WorldSwitcher.Instance == null) return;
        if (WorldSwitcher.Instance.EnterLevel(Ignite, OnEnter)) Playing = true;
    }

    public void Exit()
    {
        if (!Playing || WorldSwitcher.Instance == null) return;
        if (match != null) StopCoroutine(match);
        match = null;
        WorldSwitcher.Instance.ExitLevel(TurnOff, () => Playing = false);
    }

    [ContextMenu("Probar: entrar al nivel")]
    void TestEnter() => Enter();

    void Ignite()
    {
        content.SetActive(true);
        hud.Show(true);
        sceneCamera.enabled = true;
        playerLane = Mathf.Max(0, System.Array.IndexOf(laneId, ChosenMage.Chosen));
        judge.PlayerLane = playSolo ? -1 : playerLane;
        for (int c = 0; c < judge.RemoteLanes.Length; c++)
            judge.RemoteLanes[c] = Online != null && c < laneId.Length && ArcaneGames.Network.OnlineMatch.IsRemoteHuman(laneId[c]);
        figurePlay = -1;
        if (lanes != null) { lanes.MarkPlayer(playerLane, laneName[playerLane]); lanes.Clear(); }
        if (jury != null) jury.Hide();
        formation.OnPlatform();
        scoreboard.RestartAll();
        hud.Scoreboard(0, 0, 0);
        hud.Figure(0, Total, routine != null && routine.figures.Count > 0 ? routine.figures[0].displayName : "", 0);
        hud.Progress(0f);
        hud.Note("");
        Help();
        if (music != null) music.Prepare(routine);
    }

    void TurnOff()
    {
        phase = Phase.Outside;
        GameSound.SilenceCelebrations(0.3f);
        if (OwlNotice.Owl != null && OwlNotice.Owl.IsOpen) OwlNotice.Owl.CloseNow();
        if (music != null) music.Drop();
        if (effects != null) effects.ClearTexts();
        hud.RemoveMessage();
        content.SetActive(false);
        hud.Show(false);
        sceneCamera.enabled = false;
        var entrance = FindFirstObjectByType<ArtisticSwimmingEntrance>();
        if (entrance != null) entrance.ReturnToEdge();
    }

    void OnEnter()
    {
        match = StartCoroutine(Match(Online == null && (!introSeen || !introOnlyFirstTime)));
    }

    int Total => routine != null ? routine.figures.Count : 0;

    IEnumerator Match(bool withIntro)
    {
        if (routine == null || settings == null || routine.figures.Count == 0)
        {
            Debug.LogWarning("[Natación artística] Falta la rutina o los ajustes (Magolimpiadas > Natación artística > Preparar nivel).", this);
            yield break;
        }
        if (withIntro)
        {
            phase = Phase.Intro;
            introSeen = true;
            introRoutine = StartCoroutine(Intro());
            while (introRoutine != null) yield return null;
        }
        phase = Phase.Pause;
        hud.Message("¡Al agua!", 1.2f);
        yield return formation.DiveIn(routine.figures[0].formation, 0.35f);

        figureNotes = new float[Total];
        while (true)
        {
            scoreboard.RestartAll();
            for (currentFigure = 0; currentFigure < Total; currentFigure++)
            {
                float note = 0f;
                for (int attempt = 0; attempt <= settings.maxRetries; attempt++)
                {
                    figurePlay++;
                    if (Online != null) yield return Online.WaitForFigureStart(figurePlay);
                    yield return Figure(currentFigure, attempt);
                    float precision = scoreboard.Precision;
                    if (Online != null) yield return Online.WaitForFigureResult(figurePlay, precision, p => precision = p);
                    yield return Jury(precision, r => note = r);
                    bool passes = precision >= settings.minAccuracy;
                    if (passes || attempt == settings.maxRetries)
                    {
                        yield return hud.Message(passes ? "¡Figura superada!" : "Seguimos con esta nota...", 1.6f);
                        break;
                    }
                    scoreboard.UndoFigure();
                    yield return hud.Message("¡Hay que repetir la figura!\n<size=55%>Precisión " + Mathf.RoundToInt(precision * 100f) + " %  ·  mínimo " + Mathf.RoundToInt(settings.minAccuracy * 100f) + " %</size>", 2.4f);
                    jury.Hide();
                    hud.Note("");
                }
                figureNotes[currentFigure] = note;
                jury.Hide();
                if (currentFigure < Total - 1) yield return Transition(currentFigure + 1);
            }

            float average = 0f;
            for (int i = 0; i < Total; i++) average += figureNotes[i];
            average /= Mathf.Max(1, Total);
            int medal = settings.Medal(average);
            if (medal >= 0)
            {
                yield return Victory(average, medal);
                yield break;
            }
            yield return hud.Message("Nota final " + average.ToString("0.0") + "\n<size=55%>No alcanza para el bronce (" + settings.bronzeNote.ToString("0.0") + "). ¡Otra vez desde la primera figura!</size>", 3.2f);
            formation.GoTo(routine.figures[0].formation, settings.transitionSeconds);
            yield return new WaitForSeconds(settings.transitionSeconds);
        }
    }

    IEnumerator Intro()
    {
        var owl = OwlNotice.Owl;
        if (owl != null)
        {
            if (!owl.gameObject.activeInHierarchy) owl.gameObject.SetActive(true);
            foreach (var line in intro)
                if (!string.IsNullOrEmpty(line)) yield return owl.Say(line.Replace("{mago}", laneName[playerLane]), null, false);
            yield return owl.Close();
        }
        introRoutine = null;
    }

    IEnumerator Figure(int index, int attempt)
    {
        GameSound.SilenceCelebrations(0.4f);
        var fig = routine.figures[index];
        hud.Figure(index, Total, fig.displayName, attempt);
        hud.Note("");
        float spb = routine.SecondsPerBeat;
        int count = Mathf.Max(1, settings.countdownBeats);
        judge.Prepare(fig, settings, spb, playSolo ? -1 : playerLane, 1000 * index + attempt + 7);
        scoreboard.BeginFigure(judge.Possible);
        hud.Scoreboard(scoreboard.Points, scoreboard.Combo, scoreboard.Stars);
        formation.Rotate(fig.formation == Formation.Flower);
        formation.Flower(fig.formation == Formation.Flower ? 0.25f : 0f, 0f);

        double countStart = AudioSettings.dspTime + 0.25;
        double start = countStart + count * spb;
        clock.Setup(start, spb, settings.audioLatency);
        if (music != null) music.Touch(routine, fig, countStart, count);
        beatBlockedUntil = Time.time + 0.3f;
        warnedWave = false;
        lastWholeBeat = int.MinValue;
        phase = Phase.Figure;

        while (true)
        {
            currentBeat = clock.Beat;
            Countdown(currentBeat, count);
            Waves(fig, currentBeat);
            judge.Refresh(currentBeat);
            lanes.Draw(judge.Notes, currentBeat, fig.fallBeats);
            hud.Progress((index + Mathf.Clamp01(currentBeat / Mathf.Max(1, fig.beats))) / Mathf.Max(1, Total));
            if (fig.formation == Formation.Flower) formation.Flower(Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(currentBeat / fig.beats)), scoreboard.Precision);
            if (judge.Finished(currentBeat) && currentBeat > fig.beats + 0.5f) break;
            yield return null;
        }
        phase = Phase.Pause;
        lanes.Clear();
        if (music != null) music.Stop(1.2f);
        formation.Rotate(false);
        if (fig.formation == Formation.Flower && scoreboard.Precision >= settings.minAccuracy) yield return Bloom();
    }

    void Countdown(float beat, int count)
    {
        int whole = Mathf.FloorToInt(beat);
        if (whole == lastWholeBeat || whole > 0 || whole < -count) return;
        lastWholeBeat = whole;
        if (whole == 0) { hud.Message("¡Ya!", 0.5f); GameSound.Play(SoundSynthesizer.Tic(true), 0.7f); return; }
        if (-whole <= 3) hud.Message((-whole).ToString(), 0.45f);
        GameSound.Play(SoundSynthesizer.Tic(false), 0.6f);
    }

    void Waves(RoutineFigure fig, float beat)
    {
        foreach (float o in fig.waves)
        {
            if (beat < o - 1f || beat >= o) continue;
            if (warnedWave) return;
            warnedWave = true;
            formation.Wave(settings.waveBeats * routine.SecondsPerBeat + 0.6f);
            if (effects != null) effects.Wave(formation.WorldCenter() + Vector3.up * 3.2f);
            return;
        }
        if (!judge.InWave(beat)) warnedWave = false;
    }

    IEnumerator Bloom()
    {
        if (effects != null) effects.Circle(formation.WorldCenter(), true);
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            formation.Flower(1f, Mathf.Clamp01(t / 0.6f));
            sceneCamera.orthographicSize = BaseOrtho * Mathf.Lerp(1f, 0.86f, Mathf.Sin(Mathf.Clamp01(t / 1.4f) * Mathf.PI));
            yield return null;
        }
        sceneCamera.orthographicSize = BaseOrtho;
    }

    IEnumerator Jury(float precision, System.Action<float> result)
    {
        hud.Note("Precisión  " + Mathf.RoundToInt(precision * 100f) + " %");
        if (effects != null && precision >= settings.minAccuracy) effects.Circle(formation.WorldCenter(), false);
        yield return new WaitForSeconds(0.4f);
        yield return jury.Score(precision, settings.owlsVariation, (i, n) =>
            GameSound.Play(SoundSynthesizer.Hoot(), 0.35f, Mathf.Lerp(0.9f, 1.15f, i / 6f)));
        float note = jury.LastAverage;
        hud.Note("Precisión  " + Mathf.RoundToInt(precision * 100f) + " %   ·   Nota  " + note.ToString("0.0"));
        GameSound.Celebrate(GameAudio.Sound.Applause, Mathf.Lerp(0.25f, 0.7f, note / 10f), 2.5f, SoundSynthesizer.Applause(2f), 0.9f);
        result(note);
        yield return new WaitForSeconds(1.4f);
    }

    IEnumerator Transition(int next)
    {
        GameSound.SilenceCelebrations(0.8f);
        var fig = routine.figures[next];
        hud.Figure(next, Total, fig.displayName, 0);
        hud.Message("Siguiente: " + fig.displayName, 1.6f);
        formation.GoTo(fig.formation, settings.transitionSeconds);
        yield return new WaitForSeconds(settings.transitionSeconds + 0.3f);
    }

    IEnumerator Victory(float average, int medal)
    {
        phase = Phase.End;
        Won = true;
        hud.Progress(1f);
        formation.Celebrate(true);
        string[] names = { "¡Oro!", "¡Plata!", "¡Bronce!" };
        hud.Message("¡Rutina completa!\n<size=55%>Nota final " + average.ToString("0.0") + "  ·  " + names[medal] + "</size>", 3.5f);
        GameSound.Celebrate(GameAudio.Sound.Ovation, 0.8f, 3f, SoundSynthesizer.Fanfare());
        GameSound.Celebrate(GameAudio.Sound.Applause, 0.6f, 3.5f, SoundSynthesizer.Applause(3.5f), 1f);
        for (int i = 0; i < 4; i++)
        {
            if (effects != null) effects.Firework(transform.position + new Vector3(Random.Range(-9f, 5f), Random.Range(4.2f, 6.4f), 0f), i);
            yield return new WaitForSeconds(0.8f);
        }
        yield return new WaitForSeconds(0.6f);
        formation.Celebrate(false);
        var ceremony = PodiumCeremony.Instance;
        if (ceremony != null && medals != null && medals.Length > 0)
            yield return ceremony.MedalOnly(testName, medals, medal);
        else
        {
            GameResults.RegisterTeam(testName, medal);
            if (medals != null && medal < medals.Length) PlayerMedals.Give(testName, medal, medals[medal]);
        }
        yield return Final();
    }

    IEnumerator Final()
    {
        if (music != null) music.Stop(0.8f);
        GameSound.SilenceCelebrations(0.6f);
        var ceremony = PodiumCeremony.Instance;
        if (ceremony != null) yield return ceremony.Black(0f, 1f, 0.8f);
        Time.timeScale = 1f;
        if (Online != null)
        {
            Online.FinishForEveryone(finalScene);
            yield break;
        }
        if (Application.CanStreamedLevelBeLoaded(finalScene)) SceneManager.LoadScene(finalScene);
        else
        {
            Debug.LogWarning("[Natación artística] La escena \"" + finalScene + "\" no está en Build Settings.", this);
            if (ceremony != null) yield return ceremony.Black(1f, 0f, 0.5f);
            Exit();
        }
    }

    void OnJudge(NotesJudge.Note note, int lane, NoteJudgment judgment)
    {
        scoreboard.Register(note.data.type, lane, judgment, settings);
        hud.Scoreboard(scoreboard.Points, scoreboard.Combo, scoreboard.Stars);
        int c = lane >= 0 ? lane : playerLane;
        var bullseye = note.IsForEveryone ? (BullseyePos(1) + BullseyePos(2)) * 0.5f : BullseyePos(c);
        if (effects != null) effects.Judgment(bullseye, note.IsForEveryone ? formation.WorldCenter() : formation.WorldPosition(c), lane, judgment, note.data.type);
        if (judgment == NoteJudgment.Miss)
        {
            if (note.data.type != NoteType.Golden) formation.Miss(note.IsForEveryone ? -1 : c);
        }
        else formation.CorrectHit(note.IsForEveryone ? -1 : c, judgment, note.data.type);
    }

    void OnTouchTrap(NotesJudge.Note note, int lane)
    {
        scoreboard.Trap(lane, settings);
        hud.Scoreboard(scoreboard.Points, scoreboard.Combo, scoreboard.Stars);
        formation.Miss(lane);
        if (effects != null) effects.Trap(BullseyePos(lane), formation.WorldPosition(lane));
    }

    Vector3 BullseyePos(int lane)
    {
        if (lanes == null || lane < 0 || lane >= lanes.lanes.Length || lanes.lanes[lane] == null) return transform.position;
        return lanes.lanes[lane].position;
    }

    float BaseOrtho => sceneCamera != null && sceneCamera.aspect > 0.01f ? Mathf.Max(visibleSize.y * 0.5f, visibleSize.x * 0.5f / sceneCamera.aspect) : visibleSize.y * 0.5f;

    void LateUpdate()
    {
        if (sceneCamera != null && sceneCamera.enabled && phase != Phase.Pause) sceneCamera.orthographicSize = BaseOrtho;
    }

    void Update()
    {
        if (phase == Phase.Outside) return;
        var k = Keyboard.current;
        var r = Mouse.current;
        if (phase == Phase.Intro)
        {
            var owl = OwlNotice.Owl;
            if (owl != null && owl.IsOpen) owl.Notice("Esc · saltar");
            if (k != null && k.escapeKey.wasPressedThisFrame) JumpIntro();
            return;
        }
        if (Online == null && k != null && k.escapeKey.wasPressedThisFrame && phase != Phase.End) { Exit(); return; }
        if (Online == null && k != null && k.tabKey.wasPressedThisFrame && phase != Phase.End) ChangeMage();
        if (phase != Phase.Figure || Time.time < beatBlockedUntil) return;
        bool touches = (k != null && (k.spaceKey.wasPressedThisFrame || k.jKey.wasPressedThisFrame)) ||
                    (r != null && r.leftButton.wasPressedThisFrame) ||
                    (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (touches) judge.PlayerPress(clock.Beat);
    }

    void JumpIntro()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = null;
        if (OwlNotice.Owl != null) OwlNotice.Owl.CloseNow();
        phase = Phase.Pause;
    }

    void ChangeMage()
    {
        playerLane = (playerLane + 1) % laneId.Length;
        judge.PlayerLane = playSolo ? -1 : playerLane;
        if (lanes != null) lanes.MarkPlayer(playerLane, laneName[playerLane]);
        Help();
    }

    public void ApplyRemoteJudgment(int play, int noteIndex, int lane, NoteJudgment judgment, bool trap)
    {
        if (phase != Phase.Figure || play != figurePlay || lane == playerLane) return;
        judge.ApplyRemoteJudgment(noteIndex, lane, judgment, trap);
    }

    void Help()
    {
        var col = lanes != null && playerLane < lanes.colors.Length ? lanes.colors[playerLane] : Color.white;
        if (Online != null)
        {
            hud.Help("Tu carril: <color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + laneName[playerLane] + "</color>   ·   Espacio / clic: tocar");
            return;
        }
        hud.Help("Tu carril: <color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + laneName[playerLane] + "</color>   ·   Espacio / clic: tocar   ·   Tab: cambiar de mago   ·   Esc: salir");
    }
}
