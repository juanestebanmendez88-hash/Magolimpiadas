using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OwlGuide : MonoBehaviour
{
    public RectTransform canvas;
    public RectTransform panel;
    public CanvasGroup panelGroup;
    public TMP_Text title;
    public TMP_Text text;
    public RectTransform proceed;
    public TMP_Text notice;
    public RectTransform owl;
    public Image owlImage;
    public Vector2 housePosition = new Vector2(-575f, -500f);

    public Sprite[] speak;
    public float speakFps = 8f;
    public Sprite[] pointLeft;
    public Sprite[] pointRight;
    public float pointFps = 10f;
    public Sprite[] flight;
    public float flightFps = 12f;

    [Range(5f, 200f)] public float lettersPerSecond = 40f;
    [Range(0f, 1f)] public float scorePause = 0.15f;
    public AudioClip letterSound;
    public AudioSource soundSource;

    public float openDuration = 0.35f;
    public float closeDuration = 0.2f;

    public float flightArcHeight = 180f;
    public float targetSeparation = 250f;
    public float screenMargin = 200f;

    public bool IsOpen { get; private set; }
    public bool Writing { get; private set; }

    Vector2 House => canvas != null ? new Vector2(housePosition.x, housePosition.y + 540f - canvas.rect.height * 0.5f) : housePosition;

    enum Anim { Still, Speak, Highlight, Fly }
    Anim anim;
    bool pointsRight;
    float tAnim;
    Vector2 perch;
    bool perched;

    void Awake()
    {
        if (panelGroup != null) panelGroup.alpha = 0f;
        if (panel != null) panel.localScale = Vector3.one * 0.6f;
        if (proceed != null) proceed.gameObject.SetActive(false);
        if (notice != null) notice.gameObject.SetActive(false);
        if (owl != null) owl.gameObject.SetActive(false);
    }

    public IEnumerator Say(string message, Vector3? screenPoint = null, bool fly = true)
    {
        if (!IsOpen) yield return Open();

        Vector2 destination = House;
        bool points = false;
        if (screenPoint.HasValue && screenPoint.Value.z > 0f)
        {
            if (fly) points = PointingSpot(screenPoint.Value, out destination, out pointsRight);
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPoint.Value, null, out Vector2 target);
                pointsRight = target.x >= House.x;
                points = true;
            }
        }
        if ((destination - owl.anchoredPosition).magnitude > 25f) yield return Fly(destination);
        Animate(points ? Anim.Highlight : Anim.Speak);

        if (proceed != null) proceed.gameObject.SetActive(false);
        text.text = message;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;
        Writing = true;
        float credit = 0f;
        int visible = 0;
        yield return null;
        while (visible < total)
        {
            if (Beat()) { visible = total; break; }
            credit += Time.unscaledDeltaTime * lettersPerSecond;
            while (credit >= 1f && visible < total)
            {
                char c = text.textInfo.characterInfo[visible].character;
                visible++;
                credit -= 1f;
                if (".,!?:;…".IndexOf(c) >= 0) credit -= scorePause * lettersPerSecond;
                else if (letterSound != null && soundSource != null && !char.IsWhiteSpace(c)) soundSource.PlayOneShot(letterSound, 0.5f);
            }
            text.maxVisibleCharacters = visible;
            yield return null;
        }
        text.maxVisibleCharacters = total;
        Writing = false;

        if (proceed != null) proceed.gameObject.SetActive(true);
        yield return null;
        while (!Beat()) yield return null;
        if (proceed != null) proceed.gameObject.SetActive(false);
    }

    public void Notice(string message)
    {
        if (notice == null) return;
        notice.text = message;
        notice.gameObject.SetActive(!string.IsNullOrEmpty(message) && IsOpen);
    }

    public IEnumerator Close()
    {
        if (!IsOpen) yield break;
        IsOpen = false;
        if (proceed != null) proceed.gameObject.SetActive(false);
        if (notice != null) notice.gameObject.SetActive(false);
        StartCoroutine(Fly(new Vector2(-canvas.rect.width * 0.5f - 300f, owl.anchoredPosition.y + 250f), true));
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / closeDuration)
        {
            float s = 1f - t;
            panelGroup.alpha = s;
            panel.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, s);
            yield return null;
        }
        panelGroup.alpha = 0f;
        yield return new WaitForSecondsRealtime(0.4f);
        owl.gameObject.SetActive(false);
    }

    public void CloseNow()
    {
        StopAllCoroutines();
        IsOpen = false;
        Writing = false;
        panelGroup.alpha = 0f;
        if (proceed != null) proceed.gameObject.SetActive(false);
        if (notice != null) notice.gameObject.SetActive(false);
        owl.gameObject.SetActive(false);
    }

    IEnumerator Open()
    {
        IsOpen = true;
        text.text = "";
        if (notice != null) notice.gameObject.SetActive(!string.IsNullOrEmpty(notice.text));

        owl.gameObject.SetActive(true);
        owl.anchoredPosition = new Vector2(-canvas.rect.width * 0.5f - 300f, House.y + 300f);
        StartCoroutine(Fly(House));

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / openDuration)
        {
            panelGroup.alpha = Mathf.Clamp01(t * 2f);
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Bounce(t));
            yield return null;
        }
        panelGroup.alpha = 1f;
        panel.localScale = Vector3.one;
        while (!perched) yield return null;
    }

    IEnumerator Fly(Vector2 destination, bool exit = false)
    {
        perched = false;
        Animate(Anim.Fly);
        Vector2 fromPoint = owl.anchoredPosition;
        float x = Mathf.Sign(destination.x - fromPoint.x);
        owl.localScale = new Vector3(x < 0f ? -1f : 1f, 1f, 1f);
        float duration = Mathf.Max(0.5f, flight != null && flight.Length > 0 ? flight.Length / flightFps : 0.6f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            owl.anchoredPosition = Vector2.Lerp(fromPoint, destination, s) + Vector2.up * Mathf.Sin(s * Mathf.PI) * flightArcHeight;
            yield return null;
        }
        owl.anchoredPosition = destination;
        owl.localScale = Vector3.one;
        perch = destination;
        perched = !exit;
        Animate(Anim.Still);
    }

    bool PointingSpot(Vector3 screenPoint, out Vector2 place, out bool towardRight)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPoint, null, out Vector2 target);
        towardRight = target.x < 0f;
        float owlHeight = owl.rect.height;
        place = new Vector2(target.x + (towardRight ? -targetSeparation : targetSeparation),
                            target.y - owlHeight * 0.45f);

        float halfWidth = canvas.rect.width * 0.5f, halfHeight = canvas.rect.height * 0.5f;
        float panelCeiling = canvas.InverseTransformPoint(UpperCorner(panel)).y;
        place.x = Mathf.Clamp(place.x, -halfWidth + screenMargin, halfWidth - screenMargin);
        place.y = Mathf.Clamp(place.y, panelCeiling + 10f, halfHeight - owlHeight - 20f);
        towardRight = target.x >= place.x;
        return true;
    }

    static Vector3 UpperCorner(RectTransform r)
    {
        var corners = new Vector3[4];
        r.GetWorldCorners(corners);
        return corners[1];
    }

    void Animate(Anim a)
    {
        anim = a;
        tAnim = 0f;
    }

    void Update()
    {
        if (owl == null || !owl.gameObject.activeSelf) return;
        tAnim += Time.unscaledDeltaTime;

        Sprite frame = null;
        switch (anim)
        {
            case Anim.Fly:
                frame = Frame(flight, Mathf.Min((int)(tAnim * flightFps), Length(flight) - 1));
                break;
            case Anim.Speak:
                frame = Frame(speak, Writing ? (int)(tAnim * speakFps) % Mathf.Max(1, Length(speak)) : 0);
                break;
            case Anim.Highlight:
                var sheet = pointsRight ? pointRight : pointLeft;
                int n = Length(sheet);
                int i = (int)(tAnim * pointFps);
                if (i >= n) i = Writing && n > 2 ? 2 + (i - n) % (n - 2) : n - 1;
                frame = Frame(sheet, i);
                break;
            default:
                frame = Frame(speak, 0);
                break;
        }
        if (frame != null && owlImage.sprite != frame) owlImage.sprite = frame;

        if (perched) owl.anchoredPosition = perch + Vector2.up * Mathf.Sin(Time.unscaledTime * 2.2f) * 3f;

        if (proceed != null && proceed.gameObject.activeSelf)
        {
            var arrow = proceed.childCount > 1 ? proceed.GetChild(1) as RectTransform : null;
            if (arrow != null) arrow.localPosition = new Vector3(baseArrow + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 8f, arrow.localPosition.y, 0f);
        }
    }

    float baseArrow;

    void Start()
    {
        var arrow = proceed != null && proceed.childCount > 1 ? proceed.GetChild(1) as RectTransform : null;
        if (arrow != null) baseArrow = arrow.localPosition.x;
    }

    static int Length(Sprite[] s) => s != null ? s.Length : 0;
    static Sprite Frame(Sprite[] s, int i) => s != null && s.Length > 0 ? s[Mathf.Clamp(i, 0, s.Length - 1)] : null;

    static float Bounce(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    bool request;

    public void Proceed() { request = true; }

    bool Beat()
    {
        if (request) { request = false; return true; }
        var k = Keyboard.current;
        var m = Mouse.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame ||
                              k.numpadEnterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame)) ||
               (m != null && m.leftButton.wasPressedThisFrame);
    }
}
