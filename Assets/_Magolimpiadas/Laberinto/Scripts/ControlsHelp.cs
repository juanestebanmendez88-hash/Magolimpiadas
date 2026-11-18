using System.Collections;
using UnityEngine;

public class ControlsHelp : MonoBehaviour
{
    const float AppearDelay = 0.6f;
    const float FadeInSeconds = 0.35f;
    const float FadeOutSeconds = 0.6f;
    const float MovementThreshold = 0.01f;

    public float minSeconds = 6f;
    public float movingSeconds = 5f;
    public float maxSeconds = 20f;
    public float noIntroDelay = 3f;
    public CanvasGroup bar;

    static bool shownThisMatch;

    PlayerMage player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RestartMatch() => shownThisMatch = false;

    void Awake()
    {
        if (bar == null) return;
        bar.alpha = 0f;
        bar.blocksRaycasts = false;
        bar.interactable = false;
        bar.gameObject.SetActive(true);
    }

    IEnumerator Start()
    {
        if (bar == null) yield break;
        if (shownThisMatch) { bar.gameObject.SetActive(false); yield break; }
        player = FindFirstObjectByType<PlayerMage>();
        if (player == null) yield break;

        float waited = 0f;
        bool hadIntro = false;
        while (true)
        {
            if (LabyrinthIntro.InProgress) hadIntro = true;
            else if ((hadIntro || waited > noIntroDelay) && CanShow()) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(AppearDelay);

        shownThisMatch = true;
        float visible = 0f, moving = 0f, alpha = 0f;
        Vector3 previous = player.transform.position;
        while (visible < maxSeconds && !(visible >= minSeconds && moving >= movingSeconds))
        {
            bool show = CanShow();
            alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, Time.unscaledDeltaTime / FadeInSeconds);
            bar.alpha = alpha;
            if (show)
            {
                visible += Time.unscaledDeltaTime;
                Vector3 current = player.transform.position;
                if (new Vector2(current.x - previous.x, current.z - previous.z).magnitude > MovementThreshold) moving += Time.unscaledDeltaTime;
                previous = current;
            }
            yield return null;
        }
        for (; alpha > 0f; alpha -= Time.unscaledDeltaTime / FadeOutSeconds)
        {
            bar.alpha = alpha;
            yield return null;
        }
        bar.alpha = 0f;
        bar.gameObject.SetActive(false);
    }

    bool CanShow()
    {
        if (player == null || !player.enabled || LabyrinthIntro.InProgress) return false;
        var world = WorldSwitcher.Instance;
        if (world != null && world.Busy) return false;
        var owl = OwlNotice.Owl;
        if (owl != null && owl.IsOpen) return false;
        var ceremony = PodiumCeremony.Instance;
        return ceremony == null || !ceremony.InProgress;
    }
}
