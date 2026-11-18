using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    public string phase = "Fase 1";
    public float separation = 0.9f;

    public static Checkpoint Current { get; private set; }

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        var m = other.GetComponentInParent<Mage>();
        if (m == null || WorldSwitcher.Instance == null || m != WorldSwitcher.Instance.player2D) return;
        if (Current == this) return;
        Current = this;
        DamageNumbers.Message((Vector2)m.transform.position + Vector2.up * 3f, "Punto de control", new Color(1f, 0.9f, 0.5f));
        GameSound.Play(GameAudio.Sound.Checkpoint, 0.7f);
    }

    public static void Restart() { Current = null; }

    public static Vector2 RespawnPlace(Mage m)
    {
        if (Current == null) return m.startPoint != null ? (Vector2)m.startPoint.position : (Vector2)m.transform.position;
        int i = Mathf.Max(0, Mage.All.IndexOf(m));
        return (Vector2)Current.transform.position + new Vector2(i * Current.separation, 0.05f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.3f, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2.5f);
    }
}
