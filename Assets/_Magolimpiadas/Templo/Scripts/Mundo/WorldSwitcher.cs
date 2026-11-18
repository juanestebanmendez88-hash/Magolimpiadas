using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WorldSwitcher : MonoBehaviour
{
    public static WorldSwitcher Instance { get; private set; }

    public PlayerMage playerMage;
    public Camera camera3D;

    public Camera camera2D;
    public Mage player2D;
    public Transform start2D;
    public GameObject[] items2D;

    public float fadeDuration = 0.5f;
    public float blackPause = 0.2f;
    public Color fadeColor = Color.black;

    public Key exitKey = Key.Escape;
    public string exitNotice = "Esc · volver al mundo principal";

    public UnityEvent onEnter2D;
    public UnityEvent onReturnTo3D;

    public bool InWorld2D { get; private set; }
    public bool Switching { get; private set; }
    public bool InOtherLevel { get; private set; }
    public bool Busy => InWorld2D || InOtherLevel || Switching;

    WorldEntrance2D usedEntrance;
    bool levelFinished;
    System.Action onArrive;
    ThirdPersonCamera tracking3D;
    Image curtain;
    Text notice;
    float inputBlockedUntil;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Hay más de un 'Cambio de mundo' en la escena; se usa solo el primero.", this);
            enabled = false;
            return;
        }
        Instance = this;
        if (playerMage == null) playerMage = FindFirstObjectByType<PlayerMage>();
        if (camera3D == null) camera3D = Camera.main;
        if (camera3D != null) tracking3D = camera3D.GetComponent<ThirdPersonCamera>();
        if (camera2D != null) camera2D.enabled = false;
        Show2D(false);
        CreateCurtain();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        bool online = ArcaneGames.Network.OnlineMatch.IsActive;
        if (notice != null && InWorld2D && !Switching)
            notice.gameObject.SetActive(!online && !TempleTutorial.IsOpen && !string.IsNullOrEmpty(exitNotice));
        if (TempleTutorial.Busy || online) return;
        if (InWorld2D && !Switching && keyboard != null && keyboard[exitKey].wasPressedThisFrame)
            ReturnToMainWorld();
    }

    public void Enter2D(WorldEntrance2D entrance)
    {
        if (Busy || Time.time < inputBlockedUntil) return;
        if (camera2D == null) { Debug.LogWarning("Falta asignar la cámara 2D en 'Cambio de mundo'.", this); return; }
        usedEntrance = entrance;
        StartCoroutine(Change(true));
    }

    public void ReturnToMainWorld()
    {
        if (!InWorld2D || Switching) return;
        levelFinished = false;
        StartCoroutine(Change(false));
    }

    public void FinishLevel() => FinishLevel(null);

    public void FinishLevel(System.Action arrival)
    {
        if (!InWorld2D || Switching) return;
        levelFinished = true;
        onArrive = arrival;
        StartCoroutine(Change(false));
    }

    public bool EnterLevel(System.Action inBlack, System.Action onFinish = null)
    {
        if (Busy || Time.time < inputBlockedUntil) return false;
        StartCoroutine(ChangeLevel(true, inBlack, onFinish));
        return true;
    }

    public void ExitLevel(System.Action inBlack, System.Action onFinish = null)
    {
        if (!InOtherLevel || Switching) return;
        StartCoroutine(ChangeLevel(false, inBlack, onFinish));
    }

    IEnumerator ChangeLevel(bool enter, System.Action inBlack, System.Action onFinish)
    {
        Switching = true;
        if (enter && playerMage != null) playerMage.enabled = false;
        yield return Fade(0f, 1f);
        if (enter)
        {
            if (tracking3D != null) tracking3D.enabled = false;
            if (camera3D != null) camera3D.enabled = false;
            InOtherLevel = true;
            inBlack?.Invoke();
        }
        else
        {
            inBlack?.Invoke();
            InOtherLevel = false;
            if (camera3D != null) camera3D.enabled = true;
            if (playerMage != null) playerMage.enabled = true;
            if (tracking3D != null) { tracking3D.enabled = true; tracking3D.Restart(); }
            inputBlockedUntil = Time.time + 1f;
        }
        yield return new WaitForSecondsRealtime(blackPause);
        yield return Fade(1f, 0f);
        Switching = false;
        onFinish?.Invoke();
    }

    IEnumerator Change(bool on2D)
    {
        Switching = true;
        if (on2D && playerMage != null) playerMage.enabled = false;

        yield return Fade(0f, 1f);

        if (on2D) Ignite2D(); else Ignite3D();
        InWorld2D = on2D;
        notice.gameObject.SetActive(on2D && !string.IsNullOrEmpty(exitNotice));

        yield return new WaitForSecondsRealtime(blackPause);
        yield return Fade(1f, 0f);
        Switching = false;

        if (on2D) onEnter2D?.Invoke(); else onReturnTo3D?.Invoke();
        if (!on2D && onArrive != null) { var a = onArrive; onArrive = null; a(); }
    }

    void Ignite2D()
    {
        if (tracking3D != null) tracking3D.enabled = false;
        if (camera3D != null) camera3D.enabled = false;
        Show2D(true);
        Checkpoint.Restart();
        if (items2D != null)
            foreach (var o in items2D)
            {
                var m = o != null ? o.GetComponent<Mage>() : null;
                if (m == null) continue;
                Transform start = m.startPoint != null ? m.startPoint : (m == player2D ? start2D : null);
                if (start != null) m.Place(start.position);
                m.Heal();
            }
        if (player2D != null && start2D != null && player2D.startPoint == null) player2D.Place(start2D.position);
        camera2D.enabled = true;
        var follow = camera2D.GetComponent<FollowCamera2D>();
        if (follow != null) follow.Center();
        Notices.Tip("escalar", "Dispara flechas a la pared de ladrillo para escalar (Q: arco)");
    }

    void Show2D(bool si)
    {
        if (items2D == null) return;
        foreach (var o in items2D) if (o != null) o.SetActive(si);
    }

    void Ignite3D()
    {
        Show2D(false);
        camera2D.enabled = false;
        if (camera3D != null) camera3D.enabled = true;
        if (playerMage != null)
        {
            playerMage.enabled = true;
            if (usedEntrance != null && levelFinished && usedEntrance.pointOnFinish != null)
            {
                playerMage.transform.rotation = usedEntrance.RotationOnFinish;
                playerMage.Teleport(usedEntrance.PositionOnFinish);
                playerMage.SetRespawnOutside(usedEntrance.PositionOnFinish);
                foreach (var bot in BotMage3D.All)
                    if (bot != null && bot.state == BotMage3D.State.Following) bot.AppearNextToLeader();
            }
            else if (usedEntrance != null)
            {
                playerMage.transform.rotation = usedEntrance.ReturnRotation;
                playerMage.Teleport(usedEntrance.ReturnPosition);
            }
        }
        if (tracking3D != null)
        {
            tracking3D.enabled = true;
            tracking3D.Restart();
        }
        inputBlockedUntil = Time.time + 1f;
        levelFinished = false;
    }

    void CreateCurtain()
    {
        var canvasObject = new GameObject("Cortina cambio de mundo");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scale = canvasObject.AddComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);

        var black = new GameObject("Negro", typeof(RectTransform));
        black.transform.SetParent(canvasObject.transform, false);
        curtain = black.AddComponent<Image>();
        curtain.raycastTarget = false;
        Stretch(curtain.rectTransform);
        curtain.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        curtain.enabled = false;

        var text = new GameObject("Aviso salida", typeof(RectTransform));
        text.transform.SetParent(canvasObject.transform, false);
        notice = text.AddComponent<Text>();
        notice.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notice.fontSize = 28;
        notice.color = new Color(1f, 1f, 1f, 0.85f);
        notice.alignment = TextAnchor.LowerRight;
        notice.raycastTarget = false;
        notice.text = exitNotice;
        var rt = notice.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24f, 20f);
        rt.sizeDelta = new Vector2(900f, 60f);
        text.transform.SetAsFirstSibling();
        text.SetActive(false);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    IEnumerator Fade(float fromPoint, float until)
    {
        curtain.enabled = true;
        float d = Mathf.Max(0.01f, fadeDuration);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / d)
        {
            Alpha(Mathf.Lerp(fromPoint, until, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }
        Alpha(until);
        if (until <= 0f) curtain.enabled = false;
    }

    void Alpha(float a)
    {
        var c = fadeColor; c.a = a;
        curtain.color = c;
    }
}
