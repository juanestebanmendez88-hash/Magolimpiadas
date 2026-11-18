using System.Collections;
using UnityEngine;

public class BrambleEvent : LevelEvent
{
    public SpriteRenderer[] brambles;
    public Sprite[] turnsOn;
    public Sprite[] turnsOff;
    public float fps = 8f;
    public Collider2D block;
    public Enemy[] appear;

    void Awake()
    {
        foreach (var e in appear) if (e != null) e.gameObject.SetActive(false);
        foreach (var z in brambles) { var a = z != null ? z.GetComponent<AnimatedSprite>() : null; if (a != null) a.enabled = false; }
    }

    protected override void OnActivate() { StartCoroutine(Burn()); }

    IEnumerator Burn()
    {
        foreach (var z in brambles)
            if (z != null) Particles(ParticleShape.Fire, z.bounds.center, 40, 0.3f, new Vector2(1f, 4f), 2f, z.bounds.size);
        yield return Play(turnsOn);
        if (block != null) block.enabled = false;
        yield return new WaitForSeconds(0.5f);
        yield return Play(turnsOff);
        foreach (var e in appear)
        {
            if (e == null) continue;
            e.gameObject.SetActive(true);
            e.Wake();
        }
        if (appear.Length > 0) Notices.Tip("zarza_enemigos", "¡Cuidado! Salieron enemigos de entre las cenizas");
    }

    IEnumerator Play(Sprite[] frameSprites)
    {
        if (frameSprites == null) yield break;
        foreach (var s in frameSprites)
        {
            foreach (var z in brambles) if (z != null) z.sprite = s;
            yield return new WaitForSeconds(1f / fps);
        }
    }
}
