using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 3.2f;
    public float minDistance = 1.2f;
    public float maxDistance = 10f;
    public float zoomStep = 0.8f;
    public float zoomSmoothness = 12f;

    public float gazeHeight = 1.6f;
    public float tilt = 14f;
    public float minTilt = -10f;
    public float maxTilt = 55f;
    public float sensitivity = 0.15f;
    public float arrowsArrangement = 2.5f;
    [Range(0f, 1f)] public float followJump = 0.25f;

    public float eyesHeight = 1.55f;
    public float firstMinTilt = -70f;
    public float firstMaxTilt = 70f;

    public float maxHeight = 2.6f;
    public float collisionRadius = 0.25f;
    public float nearDistance = 1.7f;
    public LayerMask collisionLayers = ~0;

    public bool FirstPerson { get; private set; }

    private float rotation;
    private float currentTilt;
    private float targetDistance;
    private float currentDistance;
    private Vector3 lastPosition;
    private float baseHeight;
    private float takeoffHeight;
    private bool inAir;
    private bool adjustingLanding;
    private PlayerMage playerMage;
    private Renderer[] body;

    void Start()
    {
        playerMage = target.GetComponent<PlayerMage>();
        body = target.GetComponentsInChildren<Renderer>(true);
        targetDistance = currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        Restart();
    }

    public void Restart()
    {
        rotation = target.eulerAngles.y;
        currentTilt = FirstPerson ? 0f : tilt;
        lastPosition = target.position;
        baseHeight = target.position.y;
        inAir = false;
    }

    void OnEnable()
    {
        if (FirstPerson) ShowBody(false);
    }

    void OnDisable()
    {
        ShowBody(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        var mouse = Mouse.current;
        Zoom(mouse);

        bool spinning = FirstPerson || (mouse != null && mouse.rightButton.isPressed);
        Cursor.lockState = spinning ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !spinning;

        Vector3 advance = target.position - lastPosition;
        advance.y = 0f;
        lastPosition = target.position;

        if (spinning && mouse != null)
        {
            Vector2 movement = mouse.delta.ReadValue();
            rotation += movement.x * sensitivity;
            currentTilt -= movement.y * sensitivity;
        }
        else if (playerMage != null && playerMage.enabled && playerMage.MovingWithArrows && advance.sqrMagnitude > 0.0000001f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(rotation, target.eulerAngles.y)) < 110f)
                rotation = Mathf.LerpAngle(rotation, target.eulerAngles.y, arrowsArrangement * Time.deltaTime);
        }
        currentTilt = FirstPerson
            ? Mathf.Clamp(currentTilt, firstMinTilt, firstMaxTilt)
            : Mathf.Clamp(currentTilt, minTilt, maxTilt);

        Quaternion orientation = Quaternion.Euler(currentTilt, rotation, 0f);

        if (FirstPerson)
        {
            target.rotation = Quaternion.Euler(0f, rotation, 0f);
            transform.SetPositionAndRotation(target.position + Vector3.up * eyesHeight, orientation);
            return;
        }

        Vector3 gaze = target.position;
        gaze.y = TrackedHeight() + gazeHeight;
        Vector3 towardBack = orientation * Vector3.back;
        float freeDistance = currentDistance;

        if (Physics.SphereCast(gaze, collisionRadius, towardBack, out RaycastHit hit, currentDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            freeDistance = Mathf.Max(0.3f, hit.distance);
        }

        Vector3 position = gaze + towardBack * freeDistance;

        bool withCeiling = playerMage == null || playerMage.InLabyrinth;
        float ceiling = withCeiling ? maxHeight : gaze.y + maxDistance;
        position.y = Mathf.Min(position.y, ceiling);

        float proximity = 1f - Mathf.Clamp01((freeDistance - 0.3f) / (nearDistance - 0.3f));
        position.y = Mathf.Lerp(position.y, withCeiling ? maxHeight : gaze.y + 1.2f, proximity);

        transform.position = position;
        transform.LookAt(gaze);
    }

    float TrackedHeight()
    {
        float y = target.position.y;
        bool onFloor = playerMage == null || !playerMage.enabled || playerMage.TouchingGround;

        if (!onFloor)
        {
            if (!inAir) { inAir = true; takeoffHeight = baseHeight; }
            baseHeight = y > takeoffHeight ? takeoffHeight + (y - takeoffHeight) * followJump : y;
        }
        else
        {
            if (inAir) { inAir = false; adjustingLanding = true; }
            float difference = y - baseHeight;
            if (adjustingLanding && Mathf.Abs(difference) > 0.01f && Mathf.Abs(difference) < 3f)
            {
                baseHeight += difference * (1f - Mathf.Exp(-10f * Time.deltaTime));
            }
            else
            {
                baseHeight = y;
                adjustingLanding = false;
            }
        }
        return baseHeight;
    }

    void Zoom(Mouse mouse)
    {
        float wheel = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            float steps = Mathf.Abs(wheel) >= 30f ? wheel / 120f : Mathf.Sign(wheel);

            if (FirstPerson)
            {
                if (steps < 0f) ChangeFirstPerson(false);
            }
            else
            {
                float newValue = targetDistance - steps * zoomStep;
                if (newValue < minDistance - 0.01f && targetDistance <= minDistance + 0.01f)
                    ChangeFirstPerson(true);
                else
                    targetDistance = Mathf.Clamp(newValue, minDistance, maxDistance);
            }
        }
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, 1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime));
    }

    void ChangeFirstPerson(bool activate)
    {
        if (FirstPerson == activate) return;
        FirstPerson = activate;
        ShowBody(!activate);
        if (activate)
        {
            currentTilt = 0f;
        }
        else
        {
            currentTilt = tilt;
            targetDistance = currentDistance = minDistance;
        }
    }

    void ShowBody(bool visible)
    {
        if (body == null) return;
        foreach (var r in body)
            if (r != null) r.forceRenderingOff = !visible;
    }
}
