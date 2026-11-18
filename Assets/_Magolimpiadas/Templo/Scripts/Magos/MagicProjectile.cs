using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public ProjectileData data;
    public Mage launcher;
    Vector2 direction;
    Vector2? fromPoint;
    float tour;
    SpriteRenderer drawing;
    bool colliding;
    float collisionTime;

    static Sprite glow;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    public static MagicProjectile Launch(ProjectileData data, Vector2 origin, Vector2 direction, Mage launcher)
    {
        var go = new GameObject("Poder " + data.element);
        go.transform.position = origin;
        var p = go.AddComponent<MagicProjectile>();
        p.data = data;
        p.launcher = launcher;
        p.direction = direction.normalized;

        p.fromPoint = launcher != null ? new Vector2(launcher.transform.position.x, origin.y) : origin;
        p.drawing = go.AddComponent<SpriteRenderer>();
        p.drawing.sprite = Glow();
        p.drawing.color = data.color;
        p.drawing.sortingOrder = 25;
        go.transform.localScale = Vector3.one * data.size;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = data.size * 0.7f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        var c = data.color;
        trail.startColor = new Color(c.r, c.g, c.b, 0.8f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        trail.sortingOrder = 24;
        if (launcher != null && WorldSwitcher.Instance != null && launcher == WorldSwitcher.Instance.player2D)
            GameSound.Play(GameAudio.PowerOf(data.element), 0.5f, Random.Range(0.95f, 1.05f), null, 0.08f);
        return p;
    }

    void Update()
    {
        if (colliding)
        {
            collisionTime += Time.deltaTime;
            float t = collisionTime / 0.15f;
            transform.localScale = Vector3.one * data.size * Mathf.Lerp(1.6f, 0f, t);
            if (t >= 1f) Destroy(gameObject);
            return;
        }

        float step = data.speed * Time.deltaTime;
        var filter = new ContactFilter2D { useTriggers = false };
        Vector2 start = fromPoint ?? (Vector2)transform.position;
        float length = Vector2.Distance(start, transform.position) + step + data.size * 0.4f;
        fromPoint = null;
        int n = Physics2D.Raycast(start, direction, filter, hits, length);
        for (int i = 0; i < n; i++)
        {
            var col = hits[i].collider;
            if (launcher != null && col.attachedRigidbody != null && col.attachedRigidbody.gameObject == launcher.gameObject) continue;
            if (col.GetComponentInParent<Mage>() != null) continue;
            if (col.GetComponentInParent<Arrow>() != null) continue;
            transform.position = hits[i].point;
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (enemy.IsBullseye) DamageNumbers.Message(enemy.Center + Vector2.up * 0.8f, "¡Usa el arco!", new Color(1f, 0.9f, 0.5f));
                else
                {
                    enemy.ReceivePower(data, launcher, hits[i].point);
                    GameSound.Play(GameAudio.Sound.Hit, 0.45f, Random.Range(0.9f, 1.1f), null, 0.06f);
                }
            }
            Collide(true);
            return;
        }
        transform.position += (Vector3)(direction * step);
        tour += step;
        if (tour >= data.reach) Collide(false);
    }

    void Collide(bool hit)
    {
        if (colliding) return;
        colliding = true;
        var p = transform.position + Vector3.back * 0.5f;
        float scale = 1.1f * Mathf.Max(0.7f, data.size / 0.7f);
        var color = MagicEffects.ColorOf(data.element);
        if (hit) MagicEffects.Launch2D(MagicEffects.Effect2D.Burst, color, p, scale, 30, gameObject.layer, null, 1.4f);
        else MagicEffects.Launch2D(MagicEffects.Effect2D.Flash, color, p, scale * 0.6f, 30, gameObject.layer, null, 1.8f);
    }

    static Sprite Glow()
    {
        if (glow != null) return glow;
        const int t = 64;
        var tex = new Texture2D(t, t, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[t * t];
        for (int y = 0; y < t; y++)
            for (int x = 0; x < t; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(t / 2f, t / 2f)) / (t / 2f);
                float a = Mathf.Clamp01(1f - d);
                float center = Mathf.Clamp01(1f - d * 2.2f);
                px[y * t + x] =Color.Lerp(new Color(1f, 1f, 1f, a * a), new Color(1f, 1f, 1f, 1f), center);
            }
        tex.SetPixels(px);
        tex.Apply();
        glow = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), t);
        return glow;
    }
}
