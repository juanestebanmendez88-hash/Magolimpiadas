using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float embeddedTime = 5f;
    public float noticeTime = 1.2f;
    public float penetration = 0.35f;
    public float platformThickness = 0.2f;

    public Mage launcher;
    public MagicElement element;
    public bool Stuck => state == State.Stuck;

    enum State { Flying, Stuck, Bouncing }
    State state;
    Vector2 direction, bounceSpeed;
    Vector2 previousTip;
    float speed, reach, tour, time;
    float length = 2f;
    SpriteRenderer drawing;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    public static Arrow Shoot(Mage mage, Vector2 origin, Vector2 direction)
    {
        var d = mage.data;
        var go = new GameObject("Flecha " + d.displayName);
        var f = go.AddComponent<Arrow>();
        f.launcher = mage;
        f.element = d.element;
        f.direction = direction.normalized;
        f.speed = d.arrowSpeed;
        f.reach = d.arrowReach;
        f.drawing = go.AddComponent<SpriteRenderer>();
        f.drawing.sprite = d.arrow;
        f.drawing.sortingOrder = 22;
        if (d.arrow != null) f.length = d.arrow.bounds.size.x;
        f.Orient(f.direction);
        go.transform.position = origin;
        f.previousTip = new Vector2(mage.transform.position.x, origin.y);
        return f;
    }

    void Orient(Vector2 dir)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        drawing.flipY = dir.x < 0f;
    }

    void Update()
    {
        switch (state)
        {
            case State.Flying: Fly(); break;
            case State.Stuck: Wait(); break;
            case State.Bouncing: Bounce(); break;
        }
    }

    void Fly()
    {
        float step = speed * Time.deltaTime;
        Vector2 newPos = (Vector2)transform.position + direction * step;
        Vector2 newTip = newPos + direction * (length * 0.5f);
        Vector2 section = newTip - previousTip;

        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.Raycast(previousTip, section.normalized, filter, hits, section.magnitude);
        RaycastHit2D? first = null;
        for (int i = 0; i < n; i++)
        {
            var col = hits[i].collider;
            if (col.GetComponentInParent<Mage>() != null) continue;
            if (col.GetComponentInParent<Arrow>() != null) continue;
            if (first == null || hits[i].distance < first.Value.distance) first = hits[i];
        }
        if (first != null)
        {
            var g = first.Value;
            var circle = g.collider.GetComponent<ElementalCircle>();
            if (circle != null)
            {
                if (circle.ReceiveArrow(launcher)) { Destroy(gameObject); return; }
                BeginBounce(g.point, g.normal);
                return;
            }
            var enemy = g.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (enemy.ReceiveArrow(launcher)) { Destroy(gameObject); return; }
                BeginBounce(g.point, g.normal);
                return;
            }
            if (IsClimbable(g.collider, g.point, g.normal)) Stick(g.point, g.normal);
            else BeginBounce(g.point, g.normal);
            return;
        }

        transform.position = newPos;
        previousTip = newTip;
        tour += step;
        if (tour >= reach)
        {
            state = State.Bouncing;
            time = 0f;
            bounceSpeed = direction * speed * 0.3f;
        }
    }

    static bool IsClimbable(Collider2D col, Vector2 point, Vector2 normal)
    {
        if (Mathf.Abs(normal.x) < 0.7f) return false;
        var mark = col.GetComponentInParent<ClimbableSurface>();
        if (mark != null) return mark.climbable;
        var walls = col.GetComponent<ClimbableWalls>();
        return walls != null && walls.IsClimbable(point, normal);
    }

    void Stick(Vector2 point, Vector2 normal)
    {
        state = State.Stuck;
        time = 0f;
        float towardWall = normal.x < 0f ? 1f : -1f;

        transform.rotation = Quaternion.identity;
        drawing.flipY = false;
        drawing.flipX = towardWall < 0f;
        transform.position = new Vector3(point.x - towardWall * (length * 0.5f - penetration), point.y, 0f);

        var box = gameObject.AddComponent<BoxCollider2D>();
        float outside = length - penetration;
        box.size = new Vector2(outside, platformThickness);
        box.offset = new Vector2(-towardWall * penetration * 0.5f, 0f);
        box.usedByEffector = true;
        var effect = gameObject.AddComponent<PlatformEffector2D>();
        effect.useOneWay = true;
        effect.surfaceArc = 170f;
    }

    void Wait()
    {
        time += Time.deltaTime;
        float remains = embeddedTime - time;
        if (remains <= noticeTime) drawing.enabled = Mathf.Repeat(remains, 0.2f) > 0.08f;
        if (remains <= 0f) Destroy(gameObject);
    }

    void BeginBounce(Vector2 point, Vector2 normal)
    {
        state = State.Bouncing;
        time = 0f;
        transform.position = point - direction * (length * 0.5f);
        bounceSpeed = Vector2.Reflect(direction, normal) * speed * 0.25f + Vector2.up * 2f;
    }

    void Bounce()
    {
        time += Time.deltaTime;
        bounceSpeed += Vector2.down * 25f * Time.deltaTime;
        transform.position += (Vector3)(bounceSpeed * Time.deltaTime);
        transform.Rotate(0f, 0f, (bounceSpeed.x >= 0f ? -720f : 720f) * Time.deltaTime);
        var c = drawing.color; c.a = 1f - time / 0.4f; drawing.color = c;
        if (time >= 0.4f) Destroy(gameObject);
    }
}
