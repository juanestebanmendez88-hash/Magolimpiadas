using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldEntrance2D : MonoBehaviour
{
    public Transform returnPoint;
    public float returnDistance = 1.5f;
    public Transform pointOnFinish;
    public bool hideScoreboard = true;

    void Start()
    {
        if (pointOnFinish == null || !hideScoreboard) return;
        foreach (var r in pointOnFinish.GetComponents<Renderer>()) r.enabled = false;
        foreach (var c in pointOnFinish.GetComponents<Collider>()) c.enabled = false;
    }

    public Vector3 PositionOnFinish
    {
        get
        {
            if (pointOnFinish == null) return ReturnPosition;
            Vector3 p = pointOnFinish.position;
            float best = float.MaxValue;
            Vector3 ground = p;
            foreach (var g in Physics.RaycastAll(p + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (g.transform == pointOnFinish || g.collider.GetComponentInParent<PlayerMage>() != null || g.collider.GetComponentInParent<BotMage3D>() != null) continue;
                if (g.distance < best) { best = g.distance; ground = g.point; }
            }
            return ground + Vector3.up * 0.05f;
        }
    }

    public Quaternion RotationOnFinish
    {
        get
        {
            if (pointOnFinish == null) return ReturnRotation;
            Vector3 f = pointOnFinish.forward; f.y = 0f;
            return f.sqrMagnitude > 0.001f ? Quaternion.LookRotation(f) : Quaternion.identity;
        }
    }

    public Vector3 ReturnPosition
    {
        get { return returnPoint != null ? returnPoint.position : transform.position - transform.forward * returnDistance; }
    }

    public Quaternion ReturnRotation
    {
        get
        {
            if (returnPoint != null) return returnPoint.rotation;
            Vector3 outside = -transform.forward; outside.y = 0f;
            return outside.sqrMagnitude > 0.001f ? Quaternion.LookRotation(outside) : Quaternion.identity;
        }
    }

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMage>() == null) return;
        if (WorldSwitcher.Instance == null)
        {
            Debug.LogWarning("Falta el objeto 'Cambio de mundo' en la escena.", this);
            return;
        }
        if (!WorldSwitcher.Instance.Busy && ArcaneGames.Network.OnlineTrials.TryRequest(ArcaneGames.Network.OnlineTrial.Temple)) return;
        WorldSwitcher.Instance.Enter2D(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.9f);
        Vector3 p = ReturnPosition;
        Gizmos.DrawWireSphere(p + Vector3.up * 0.1f, 0.3f);
        Gizmos.DrawLine(p + Vector3.up * 0.1f, p + Vector3.up * 0.1f + ReturnRotation * Vector3.forward * 0.8f);
        if (pointOnFinish != null)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
            Gizmos.DrawLine(transform.position, pointOnFinish.position);
            Gizmos.DrawWireSphere(pointOnFinish.position, 0.4f);
        }
    }
}
