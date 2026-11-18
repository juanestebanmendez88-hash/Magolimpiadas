using System.Collections;
using UnityEngine;

public class LakeEvent : LevelEvent
{
    public SpriteRenderer[] icePieces;
    public Collider2D iceGround;
    public float secondsPerPiece = 0.15f;

    void Awake()
    {
        foreach (var p in icePieces) if (p != null) p.enabled = false;
        if (iceGround != null) iceGround.enabled = false;
    }

    protected override void OnActivate() { StartCoroutine(Freeze()); }

    IEnumerator Freeze()
    {
        if (iceGround != null) iceGround.enabled = true;
        foreach (var p in icePieces)
        {
            if (p == null) continue;
            p.enabled = true;
            Particles(ParticleShape.Snowflakes, p.bounds.center, 18, 0.25f, new Vector2(1f, 3f), 0.3f, new Vector3(p.bounds.size.x, 0.5f, 0f));
            for (float t = 0f; t < 1f; t += Time.deltaTime / secondsPerPiece)
            {
                p.color = new Color(1f, 1f, 1f, t);
                yield return null;
            }
            p.color = Color.white;
        }
        Notices.Tip("lago", "¡El lago se congeló! Ahora se puede caminar encima");
    }
}
