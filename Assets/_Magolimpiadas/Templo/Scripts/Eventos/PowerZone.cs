using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PowerZone : MonoBehaviour
{
    public MageData requiredMage;
    public string action = "congela el lago";
    public bool useSecondaryPower = false;
    public LevelEvent levelEvent;

    public Enemy requiredEnemy;
    public Encounter requiredEncounter;

    public float messageDistance = 9f;

    public bool Used { get; private set; }
    public static readonly List<PowerZone> All = new List<PowerZone>();
    public static event System.Action<PowerZone> AnyUsed;

    BoxCollider2D zone;

    void Awake() { zone = GetComponent<BoxCollider2D>(); zone.isTrigger = true; }
    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); Notices.Label(this, Vector3.zero, "", Color.white, false); }

    public bool Ready
    {
        get
        {
            if (Used) return false;
            if (requiredEnemy != null && requiredEnemy.CurrentState != Enemy.State.Eliminated) return false;
            if (requiredEncounter != null && !requiredEncounter.Finished) return false;
            return true;
        }
    }

    public Bounds Area => zone != null ? zone.bounds : GetComponent<BoxCollider2D>().bounds;
    public bool BelongsTo(Mage m) => m != null && m.data == requiredMage;

    public bool Contains(Mage m)
    {
        var b = Area; var p = m.transform.position + Vector3.up; p.z = b.center.z;
        return b.Contains(p);
    }

    public static PowerZone Use(Mage m)
    {
        foreach (var z in All)
        {
            if (!z.Contains(m) || !z.Ready) continue;
            if (!z.BelongsTo(m))
            {
                string who = z.requiredMage != null ? z.requiredMage.displayName : "otro mago";
                DamageNumbers.Message((Vector2)m.transform.position + Vector2.up * 3f, "Esto lo hace " + who, new Color(1f, 0.85f, 0.5f));
                return null;
            }
            z.Activate();
            return z;
        }
        return null;
    }

    public void Activate()
    {
        if (Used) return;
        Used = true;
        AnyUsed?.Invoke(this);
        Notices.Label(this, Vector3.zero, "", Color.white, false);
        if (levelEvent != null) levelEvent.Activate();
    }

    void Update()
    {
        var j = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.player2D : null;
        bool ver = j != null && Ready && WorldSwitcher.Instance.InWorld2D &&
                   Vector2.Distance(j.transform.position, Area.center) <= messageDistance;
        string who = requiredMage != null ? requiredMage.displayName.Split(' ')[requiredMage.displayName.Split(' ').Length - 1] : "?";
        string text = BelongsTo(j) ? "E: " + action : who + ": " + action;
        Color color = requiredMage != null ? requiredMage.color : Color.white;
        Notices.Label(this, new Vector3(Area.center.x, Area.max.y + 0.5f, 0f), text, color, ver);
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Gizmos.color = requiredMage != null ? new Color(requiredMage.color.r, requiredMage.color.g, requiredMage.color.b, 0.3f) : new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
