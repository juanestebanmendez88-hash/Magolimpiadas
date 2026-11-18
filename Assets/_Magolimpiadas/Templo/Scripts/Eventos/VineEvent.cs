using System.Collections;
using UnityEngine;

public class VineEvent : LevelEvent
{
    public SpriteRenderer[] sections;
    public Vine climb;
    public float secondsPerSection = 0.6f;

    void Awake()
    {
        foreach (var t in sections) if (t != null) t.enabled = false;
        if (climb != null) climb.enabled = false;
    }

    protected override void OnActivate() { StartCoroutine(Grow()); }

    IEnumerator Grow()
    {
        foreach (var tr in sections)
        {
            if (tr == null) continue;
            var scale = tr.transform.localScale;
            tr.enabled = true;
            Particles(tr.bounds.center, new Color(0.45f, 0.9f, 0.3f), 25, 0.3f, new Vector2(1f, 2.5f), secondsPerSection, new Vector3(1.5f, tr.bounds.size.y, 0f));
            for (float t = 0f; t < 1f; t += Time.deltaTime / secondsPerSection)
            {
                tr.transform.localScale = new Vector3(scale.x, scale.y * Mathf.SmoothStep(0f, 1f, t), scale.z);
                yield return null;
            }
            tr.transform.localScale = scale;
        }
        if (climb != null) climb.enabled = true;
        Notices.Tip("enredadera", "Mantén Espacio o W junto a la enredadera para trepar");
    }
}
