using System;
using System.Collections;
using UnityEngine;

public static class OwlNotice
{
    static OwlGuide owl;
    static bool busy;

    public static OwlGuide Owl
    {
        get
        {
            if (owl == null) owl = UnityEngine.Object.FindFirstObjectByType<OwlGuide>(FindObjectsInactive.Include);
            return owl;
        }
    }

    public static string PlayerName()
    {
        switch (ChosenMage.Chosen)
        {
            case "elemental": return "Mago Elemental";
            case "druida": return "Druida Verde";
            case "rayo": return "Mago del Rayo";
            default: return "Maga Nebulosa";
        }
    }

    public static bool Free => Owl != null && !busy && !Owl.IsOpen;

    public static IEnumerator Show(MonoBehaviour owner, string text, float seconds, Func<bool> cut = null)
    {
        if (!Free) yield break;
        busy = true;
        var b = Owl;
        if (!b.gameObject.activeInHierarchy) b.gameObject.SetActive(true);
        string message = (text ?? "").Replace("{mago}", PlayerName());
        bool finished = false;
        var say = owner.StartCoroutine(Wrap(b.Say(message, null, false), () => finished = true));
        float t = 0f;
        while (t < seconds && !finished && (cut == null || !cut()))
        {
            b.Notice("");
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (say != null) owner.StopCoroutine(say);
        yield return b.Close();
        busy = false;
    }

    static IEnumerator Wrap(IEnumerator routine, Action onFinish)
    {
        yield return routine;
        onFinish();
    }
}
