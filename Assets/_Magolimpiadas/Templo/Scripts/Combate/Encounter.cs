using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Encounter : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>();
    public List<GameObject> barriers = new List<GameObject>();
    public string messageOnFinish = "¡Paso libre!";
    public Enemy boss;
    public string bossMessage = "¡Cuidado! ¡El Dragón de hielo!";
    bool bossOutside;

    public bool Started { get; private set; }
    public bool Finished { get; private set; }
    public static event System.Action<Encounter> AnyStarted;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void Start()
    {
        if (enemies.Count == 0)
        {
            var zone = GetComponent<BoxCollider2D>().bounds;
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                var p = e.transform.position; p.z = zone.center.z;
                if (zone.Contains(p)) enemies.Add(e);
            }
        }
        foreach (var e in enemies) if (e != null) e.onlyWithEncounter = true;
        foreach (var b in barriers) if (b != null) b.SetActive(false);
    }

    void PutEnemiesInside()
    {
        Interior(out float min, out float max);
        foreach (var e in enemies)
        {
            if (e == null || !e.isActiveAndEnabled || e.CurrentState == Enemy.State.Eliminated) continue;
            float x = e.transform.position.x;
            if (x > min + 0.8f && x < max - 0.8f) continue;
            var rb = e.GetComponent<Rigidbody2D>();
            Vector2 p = new Vector2(Mathf.Clamp(x, min + 1.5f, max - 1.5f), e.transform.position.y);
            if (rb != null) { rb.position = p; rb.linearVelocity = Vector2.zero; }
            e.transform.position = p;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { Check(other); }
    void OnTriggerStay2D(Collider2D other) { Check(other); }

    void Check(Collider2D other)
    {
        if (Started || Finished) return;
        var m = other.GetComponentInParent<Mage>();
        if (m == null || WorldSwitcher.Instance == null || m != WorldSwitcher.Instance.player2D) return;
        Begin();
    }

    void Interior(out float min, out float max)
    {
        var zone = GetComponent<BoxCollider2D>().bounds;
        min = zone.min.x; max = zone.max.x;
        float bMin = float.MaxValue, bMax = float.MinValue;
        foreach (var b in barriers)
            if (b != null) { bMin = Mathf.Min(bMin, b.transform.position.x); bMax = Mathf.Max(bMax, b.transform.position.x); }
        if (bMin < bMax) { min = bMin; max = bMax; }
    }

    public void Begin()
    {
        if (Started) return;
        Started = true;
        AnyStarted?.Invoke(this);
        Interior(out float min, out float max);
        var zone = GetComponent<BoxCollider2D>().bounds;
        Vector2 j = WorldSwitcher.Instance.player2D.transform.position;
        float x = Mathf.Clamp(j.x, min + 1.8f, max - 1.8f);
        foreach (var m in Mage.All)
        {
            if (m == null) continue;
            float px = m.transform.position.x;
            if (px < min + 1.2f || px > max - 1.2f) m.Place(new Vector2(x, zone.min.y + 0.05f));
        }
        foreach (var b in barriers) if (b != null) b.SetActive(true);
        PutEnemiesInside();
        foreach (var e in enemies) if (e != null) e.Wake();
    }

    void Pause()
    {
        Started = false;
        foreach (var b in barriers) if (b != null) b.SetActive(false);
    }

    void Update()
    {
        if (Started && !Finished && WorldSwitcher.Instance != null && WorldSwitcher.Instance.player2D != null)
        {
            var j = WorldSwitcher.Instance.player2D;
            Interior(out float min, out float max);
            if (!ArcaneGames.Network.OnlineMatch.IsActive && j.Alive && (j.transform.position.x < min || j.transform.position.x > max)) { Pause(); return; }
        }
        if (!Started || Finished) return;
        PutEnemiesInside();
        foreach (var e in enemies)
            if (e != null && e.CurrentState != Enemy.State.Eliminated) return;
        if (boss != null)
        {
            if (!bossOutside)
            {
                bossOutside = true;
                boss.gameObject.SetActive(true);
                boss.Wake();
                Notices.Tip("jefe_" + boss.displayName, bossMessage);
                return;
            }
            if (boss.CurrentState != Enemy.State.Eliminated) return;
        }
        Finish();
    }

    void Finish()
    {
        Finished = true;
        foreach (var b in barriers) if (b != null) b.SetActive(false);
        var j = WorldSwitcher.Instance != null ? WorldSwitcher.Instance.player2D : null;
        if (j != null && !string.IsNullOrEmpty(messageOnFinish))
            DamageNumbers.Message((Vector2)j.transform.position + Vector2.up * 3f, messageOnFinish, new Color(0.6f, 1f, 0.6f));
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
