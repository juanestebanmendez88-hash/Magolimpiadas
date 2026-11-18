using UnityEngine;

public class FallenIce : MonoBehaviour
{
    float damage, speed = 9f, health = 4f;
    SpriteRenderer drawing;
    static Sprite shape;
    readonly RaycastHit2D[] hits = new RaycastHit2D[6];

    public static void Launch(Vector2 fromPoint, float damage)
    {
        var go = new GameObject("Carámbano");
        go.transform.position = fromPoint;
        var h = go.AddComponent<FallenIce>();
        h.damage = damage;
        h.drawing = go.AddComponent<SpriteRenderer>();
        h.drawing.sprite = Shape();
        h.drawing.color = new Color(0.75f, 0.93f, 1f);
        h.drawing.sortingOrder = 27;
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.12f; trail.startWidth = 0.25f; trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.8f, 0.95f, 1f, 0.7f); trail.endColor = new Color(0.8f, 0.95f, 1f, 0f);
        trail.sortingOrder = 26;
    }

    void Update()
    {
        health -= Time.deltaTime;
        if (health <= 0f) { Destroy(gameObject); return; }
        float step = speed * Time.deltaTime;
        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.Raycast(transform.position, Vector2.down, filter, hits, step + 0.4f);
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c.GetComponentInParent<Enemy>() != null || c.GetComponentInParent<Arrow>() != null) continue;
            var mage = c.GetComponentInParent<Mage>();
            if (mage != null) mage.ReceiveDamage(damage, (Vector2)transform.position + Vector2.up);
            LevelEvent.Particles(LevelEvent.ParticleShape.Snowflakes, hits[i].point, 12, 0.2f, new Vector2(1.5f, 4f), 0.1f, new Vector3(0.4f, 0.1f, 0f));
            Destroy(gameObject);
            return;
        }
        transform.position += Vector3.down * step;
    }

    static Sprite Shape()
    {
        if (shape != null) return shape;
        const int w = 12, h = 28;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float width = y < h * 0.7f ? (y / (h * 0.7f)) * (w / 2f) : ((h - y) / (h * 0.3f)) * (w / 2f);
                float dx = Mathf.Abs(x - w / 2f + 0.5f);
                tex.SetPixel(x, y, dx < width ? (dx < width - 1.5f ? Color.white : new Color(0.3f, 0.5f, 0.75f)) : Color.clear);
            }
        tex.Apply();
        shape = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 24);
        return shape;
    }
}
