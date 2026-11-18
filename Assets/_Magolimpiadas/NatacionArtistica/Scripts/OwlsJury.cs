using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OwlsJury : MonoBehaviour
{
    [System.Serializable]
    public class Owl
    {
        public SpriteRenderer body;
        public TMP_Text note;
        public Sprite[] frameSprites;
        [System.NonSerialized] public Vector3 position, scale;
        [System.NonSerialized] public float jump;
    }

    public List<Owl> owls = new List<Owl>();
    public TMP_Text average;

    static readonly float[] Signs = { 0f, 2f, 4f, 6f, 7f, 8f, 10f };

    public float LastAverage { get; private set; }

    void Awake()
    {
        foreach (var b in owls)
        {
            if (b.body == null) continue;
            b.position = b.body.transform.localPosition;
            b.scale = b.body.transform.localScale;
        }
        Hide();
    }

    public static float OwlNote(float precision, float variation) =>
        Mathf.Clamp(Mathf.Round((precision * 10f + variation) * 2f) / 2f, 0f, 10f);

    public void Hide()
    {
        foreach (var b in owls)
        {
            if (b.body != null && b.frameSprites != null && b.frameSprites.Length > 0) b.body.sprite = b.frameSprites[0];
            if (b.note != null) b.note.text = "";
        }
        if (average != null) average.text = "";
    }

    public IEnumerator Score(float precision, float[] variations, System.Action<int, float> onShow)
    {
        float sum = 0f;
        for (int i = 0; i < owls.Count; i++)
        {
            float v = variations != null && i < variations.Length ? variations[i] : 0f;
            float note = OwlNote(precision, v);
            sum += note;
            var b = owls[i];
            if (b.body != null && b.frameSprites != null && b.frameSprites.Length >= 8) b.body.sprite = b.frameSprites[1 + Sign(note)];
            if (b.note != null)
            {
                b.note.text = note.ToString(note % 1f == 0f ? "0" : "0.0");
                b.note.color = NoteColor(note);
            }
            b.jump = 1f;
            onShow?.Invoke(i, note);
            yield return new WaitForSeconds(0.22f);
        }
        LastAverage = owls.Count > 0 ? sum / owls.Count : 0f;
        if (average != null)
        {
            average.text = "Nota del jurado  <color=#" + ColorUtility.ToHtmlStringRGB(NoteColor(LastAverage)) + ">" + LastAverage.ToString("0.0") + "</color>";
        }
    }

    static int Sign(float note)
    {
        int best = 0;
        for (int i = 1; i < Signs.Length; i++)
            if (Mathf.Abs(Signs[i] - note) < Mathf.Abs(Signs[best] - note)) best = i;
        return best;
    }

    public static Color NoteColor(float note) =>
        note >= 9f ? new Color(1f, 0.85f, 0.3f) : note >= 7.5f ? new Color(0.88f, 0.92f, 1f) : note >= 6f ? new Color(1f, 0.65f, 0.35f) : new Color(1f, 0.45f, 0.45f);

    void Update()
    {
        for (int i = 0; i < owls.Count; i++)
        {
            var b = owls[i];
            if (b.body == null) continue;
            float breathes = Mathf.Sin(Time.time * 1.6f + i * 0.9f) * 0.02f;
            float hop = Mathf.Sin(b.jump * Mathf.PI) * 0.35f;
            b.body.transform.localPosition = b.position + new Vector3(0f, hop, 0f);
            b.body.transform.localScale = Vector3.Scale(b.scale, new Vector3(1f, 1f + breathes, 1f));
            b.jump = Mathf.MoveTowards(b.jump, 0f, Time.deltaTime * 3f);
        }
    }
}
