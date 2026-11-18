using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelUI : MonoBehaviour
{
    public float entranceDuration = 1f;
    public float exitDuration = 1f;
    public bool growOnAppear = false;
    [Range(0.5f, 1f)] public float scaleOnAppear = 0.85f;
    public bool hiddenOnBegin = true;

    public bool Visible { get; private set; }

    private CanvasGroup group;
    private Vector3 originalScale;
    private bool request;
    private Coroutine animationValue;

    void Awake()
    {
        Prepare();
        if (hiddenOnBegin && !request)
        {
            group.alpha = 0f;
            Interactable(false);
            gameObject.SetActive(false);
        }
        else
        {
            Visible = gameObject.activeSelf;
        }
    }

    void Prepare()
    {
        if (group != null) return;
        group = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    public void Show() => Change(true, -1f);
    public void Hide() => Change(false, -1f);

    public void ShowFor(float seconds) => Change(true, seconds);

    public IEnumerator ShowAndWait()
    {
        Show();
        yield return new WaitForSecondsRealtime(entranceDuration);
    }

    public IEnumerator HideAndWait()
    {
        Hide();
        yield return new WaitForSecondsRealtime(exitDuration);
    }

    void Change(bool show, float seconds)
    {
        request = true;
        Prepare();
        if (!show && !gameObject.activeSelf) { Visible = false; return; }
        if (show && !gameObject.activeSelf)
        {
            group.alpha = 0f;
            gameObject.SetActive(true);
        }
        if (!gameObject.activeInHierarchy) return;
        if (animationValue != null) StopCoroutine(animationValue);
        animationValue = StartCoroutine(Animate(show, seconds));
    }

    IEnumerator Animate(bool show, float seconds)
    {
        Visible = show;
        Interactable(show);
        float fromPoint = group.alpha, until = show ? 1f : 0f;
        float duration = Mathf.Max(0.01f, show ? entranceDuration : exitDuration);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            group.alpha = Mathf.Lerp(fromPoint, until, s);
            if (show && growOnAppear) transform.localScale = originalScale * Mathf.Lerp(scaleOnAppear, 1f, s);
            yield return null;
        }
        group.alpha = until;
        transform.localScale = originalScale;

        if (show && seconds >= 0f)
        {
            yield return new WaitForSecondsRealtime(seconds);
            animationValue = StartCoroutine(Animate(false, -1f));
            yield break;
        }
        if (!show) gameObject.SetActive(false);
    }

    void Interactable(bool si)
    {
        group.interactable = si;
        group.blocksRaycasts = si;
    }
}
