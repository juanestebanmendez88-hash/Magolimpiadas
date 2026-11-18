using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SwimmingHud : MonoBehaviour
{
    public CanvasScaler climber;
    public RectTransform frame;

    public TMP_Text figure;
    public TMP_Text subtitle;
    public RectTransform progress;
    public Image progressFill;
    public Image[] marks = new Image[4];
    public TMP_Text points;
    public TMP_Text combo;
    public TMP_Text stars;
    public TMP_Text message;
    public GameObject messageBackground;
    public TMP_Text help;
    public TMP_Text note;

    Coroutine messageRoutine;
    float popCombo, starsPop;
    int lastCombo, lastStars;

    public void Show(bool si) => gameObject.SetActive(si);

    public void Figure(int index, int total, string displayName, int attempt)
    {
        if (figure != null) figure.text = (index < total - 1 ? "Figura " + (index + 1) : "Figura final") + " · " + displayName;
        if (subtitle != null) subtitle.text = attempt > 0 ? "Repetición " + attempt : "Natación artística · La Piscina de la Luna";
        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] == null) continue;
            marks[i].color = i < index ? new Color(1f, 0.84f, 0.35f) : i == index ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
    }

    public void Progress(float total)
    {
        if (progress != null) progress.anchorMax = new Vector2(Mathf.Clamp01(total), 1f);
    }

    public void Scoreboard(int pts, int cmb, int est)
    {
        if (points != null) points.text = "Puntos  <color=#FFE27A>" + pts.ToString("N0") + "</color>";
        if (cmb != lastCombo && cmb > lastCombo) popCombo = 1f;
        if (est > lastStars) starsPop = 1f;
        lastCombo = cmb; lastStars = est;
        if (combo != null) combo.text = cmb >= 2 ? "Combo  x" + cmb : "";
        if (stars != null) stars.text = "x " + est;
    }

    public void Note(string text)
    {
        if (note != null) note.text = text;
    }

    public void Help(string text)
    {
        if (help != null) help.text = text;
    }

    public Coroutine Message(string text, float seconds)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
        return messageRoutine;
    }

    public void RemoveMessage()
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = null;
        if (message != null) message.text = "";
        if (messageBackground != null) messageBackground.SetActive(false);
    }

    IEnumerator MessageRoutine(string text, float seconds)
    {
        if (message == null) yield break;
        message.text = text;
        if (messageBackground != null) messageBackground.SetActive(true);
        var rt = message.rectTransform;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float s = t < 0.18f ? Mathf.Lerp(0.6f, 1.12f, t / 0.18f) : Mathf.Lerp(1.12f, 1f, (t - 0.18f) / 0.2f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        message.text = "";
        if (messageBackground != null) messageBackground.SetActive(false);
        messageRoutine = null;
    }

    Canvas canvas;

    void Update()
    {
        if (climber != null)
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            var px = canvas != null ? canvas.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            float look = px.height > 0f ? px.width / px.height : 16f / 9f;
            climber.matchWidthOrHeight = look >= 16f / 9f ? 1f : 0f;
        }
        if (help != null)
        {
            bool speaks = OwlNotice.Owl != null && OwlNotice.Owl.IsOpen;
            help.alpha = Mathf.MoveTowards(help.alpha, speaks ? 0f : 0.9f, Time.unscaledDeltaTime * 4f);
        }
        if (combo != null) combo.rectTransform.localScale = Vector3.one * (1f + popCombo * 0.25f);
        if (stars != null) stars.rectTransform.localScale = Vector3.one * (1f + starsPop * 0.4f);
        popCombo = Mathf.MoveTowards(popCombo, 0f, Time.unscaledDeltaTime * 5f);
        starsPop = Mathf.MoveTowards(starsPop, 0f, Time.unscaledDeltaTime * 3f);
    }
}
