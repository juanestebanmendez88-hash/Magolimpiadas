using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Elevator : MonoBehaviour
{
    public float riseHeight = 9f;
    public float speed = 2.2f;
    public float wait = 1.5f;
    public bool lit = false;

    Rigidbody2D rb;
    Vector2 down;
    bool rising = true;
    float waitUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        down = rb.position;
    }

    public void Ignite() { lit = true; waitUntil = Time.time + 0.5f; }

    void FixedUpdate()
    {
        if (!lit || Time.time < waitUntil) return;
        Vector2 destination = rising ? down + Vector2.up * riseHeight : down;
        Vector2 p = Vector2.MoveTowards(rb.position, destination, speed * Time.fixedDeltaTime);
        rb.MovePosition(p);
        if ((p - destination).sqrMagnitude < 0.0001f) { rising = !rising; waitUntil = Time.time + wait; }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * riseHeight);
    }
}
