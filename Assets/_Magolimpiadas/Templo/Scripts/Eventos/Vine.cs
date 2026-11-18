using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Vine : MonoBehaviour
{
    public float climbSpeed = 4.5f;

    public static readonly List<Vine> All = new List<Vine>();
    BoxCollider2D zone;

    void Awake() { zone = GetComponent<BoxCollider2D>(); zone.isTrigger = true; }
    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public Bounds Area => zone.bounds;
    public float Cap => zone.bounds.max.y;

    public bool Touches(Mage m)
    {
        var b = zone.bounds;
        Vector2 p = m.transform.position;
        return p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y - 0.1f && p.y <= b.max.y;
    }

    public static Vine ForMage(Mage m)
    {
        foreach (var e in All) if (e.isActiveAndEnabled && e.Touches(m)) return e;
        return null;
    }

    public static Vine Nearest(Vector2 p, float distance)
    {
        Vine best = null; float d = distance;
        foreach (var e in All)
        {
            if (!e.isActiveAndEnabled) continue;
            float dx = Mathf.Abs(e.Area.center.x - p.x);
            if (dx < d && p.y >= e.Area.min.y - 1f && p.y <= e.Area.max.y) { d = dx; best = e; }
        }
        return best;
    }
}
