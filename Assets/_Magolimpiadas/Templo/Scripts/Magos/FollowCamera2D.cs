using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowCamera2D : MonoBehaviour
{
    public Transform target;
    public float gazeHeight = 1.5f;
    public float lead = 2f;
    public float smoothness = 6f;
    public Vector2 minLimit = new Vector2(1000f, 969f);
    public Vector2 maxLimit = new Vector2(1202f, 1000f);

    Camera cam;
    float currentLead;

    void Awake() { cam = GetComponent<Camera>(); }

    public void Center()
    {
        if (target == null) return;
        currentLead = 0f;
        transform.position = Limit(Desired());
    }

    void LateUpdate()
    {
        if (target == null || cam == null || !cam.enabled) return;
        var mage = target.GetComponent<Mage>();
        float wants = mage != null ? (mage.FacingRight ? lead : -lead) : 0f;
        currentLead = Mathf.Lerp(currentLead, wants, 2f * Time.deltaTime);
        Vector3 p = Vector3.Lerp(transform.position, Desired(), 1f - Mathf.Exp(-smoothness * Time.deltaTime));
        transform.position = Limit(p);
    }

    Vector3 Desired()
    {
        Vector3 o = target.position;
        return new Vector3(o.x + currentLead, o.y + gazeHeight, transform.position.z);
    }

    Vector3 Limit(Vector3 p)
    {
        if (cam == null) cam = GetComponent<Camera>();
        float heightValue = cam.orthographicSize, width = heightValue * cam.aspect;
        p.x = maxLimit.x - minLimit.x > width * 2f ? Mathf.Clamp(p.x, minLimit.x + width, maxLimit.x - width) : (minLimit.x + maxLimit.x) / 2f;
        p.y = maxLimit.y - minLimit.y > heightValue * 2f ? Mathf.Clamp(p.y, minLimit.y + heightValue, maxLimit.y - heightValue) : (minLimit.y + maxLimit.y) / 2f;
        return p;
    }
}
