using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BotMage3D : MonoBehaviour
{
    public enum State { Waiting, Cinematic, Following }

    public string displayName = "Mago";
    public string mageId = "";
    public Animator animator;
    public GameObject torch;
    public Transform islandPortal;
    public Collectible bow;
    public Racket racket;

    public PlayerMage leader;
    public Vector3 placeNextToLeader = new Vector3(-1.5f, 0f, -2f);
    public float stopDistance = 1.2f;
    public float runDistance = 6f;
    public float maxDistance = 30f;
    public float bowSightDistance = 15f;
    public float walkSpeed = 2.2f;
    public float runSpeed = 4.5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    public State state = State.Waiting;

    public static readonly List<BotMage3D> All = new List<BotMage3D>();

    CharacterController control;
    float verticalSpeed;
    float check = 1.5f;
    Vector3 lastCheck;

    public CharacterController Control => control != null ? control : (control = GetComponent<CharacterController>());

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        control = GetComponent<CharacterController>();
        lastCheck = transform.position;
        RefreshTorchAnimation();
        animator.SetBool("EnSuelo", true);
    }

    public bool WithTorch => torch != null && torch.activeSelf;

    public void DropTorch()
    {
        if (torch != null) torch.SetActive(false);
        RefreshTorchAnimation();
    }

    void RefreshTorchAnimation()
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
            if (p.name == "ConAntorcha") { animator.SetBool("ConAntorcha", WithTorch); return; }
    }

    public void Teleport(Vector3 destination)
    {
        Control.enabled = false;
        transform.position = destination;
        verticalSpeed = 0f;
        Control.enabled = true;
        Physics.SyncTransforms();
        lastCheck = destination;
    }

    public void ShowBody(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = !visible;
    }

    void Update()
    {
        if (state != State.Following) return;
        if (leader == null || !leader.enabled || !leader.isActiveAndEnabled) { Still(); return; }

        Vector3 meta;
        float stop;
        Transform target = null;
        if (bow != null && !bow.Collected && bow.isActiveAndEnabled && VectorUtils.Flatten(bow.transform.position - transform.position).magnitude < bowSightDistance) target = bow.transform;
        else if (racket != null && !racket.Collected && VectorUtils.Flatten(racket.transform.position - transform.position).magnitude < bowSightDistance) target = racket.transform;
        bool goingForBow = target != null;
        if (goingForBow) { meta = target.position; stop = 0.2f; }
        else { meta = leader.transform.TransformPoint(placeNextToLeader); stop = stopDistance; }

        float toLeader = VectorUtils.Flatten(leader.transform.position - transform.position).magnitude;
        if (toLeader > maxDistance || transform.position.y < leader.transform.position.y - 10f) { AppearNextToLeader(); return; }

        Vector3 remaining = VectorUtils.Flatten(meta - transform.position);
        bool move = remaining.magnitude > stop;
        bool run = move && remaining.magnitude > runDistance;

        if (move) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(remaining), rotationSpeed * Time.deltaTime);
        else if (!goingForBow)
        {
            Vector3 lookDirection = VectorUtils.Flatten(leader.transform.forward);
            if (lookDirection.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 3f * Time.deltaTime);
        }

        if (Control.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        Vector3 step = move ? remaining.normalized * (run ? runSpeed : walkSpeed) : Vector3.zero;
        Control.Move((step + Vector3.up * verticalSpeed) * Time.deltaTime);

        animator.SetBool("EnSuelo", true);
        animator.SetFloat("Velocidad", !move ? 0f : run ? 2f : 1f, 0.1f, Time.deltaTime);

        check -= Time.deltaTime;
        if (check <= 0f)
        {
            bool stuck = move && VectorUtils.Flatten(transform.position - lastCheck).magnitude < 0.3f && toLeader > 6f;
            lastCheck = transform.position;
            check = 1.5f;
            if (stuck) AppearNextToLeader();
        }
    }

    void Still()
    {
        animator.SetFloat("Velocidad", 0f, 0.1f, Time.deltaTime);
        animator.SetBool("EnSuelo", true);
    }

    public void AppearNextToLeader()
    {
        if (leader == null) return;
        Vector3[] attempts =
        {
            leader.transform.TransformPoint(placeNextToLeader),
            leader.transform.TransformPoint(new Vector3(placeNextToLeader.x * 0.5f, 0f, -1.5f)),
            leader.transform.position - leader.transform.forward * 1f,
        };
        foreach (var p in attempts)
        {
            if (Ground(p, leader.transform.position.y, out float y))
            {
                Teleport(new Vector3(p.x, y + 0.05f, p.z));
                transform.rotation = Quaternion.LookRotation(VectorUtils.Flatten(leader.transform.forward).sqrMagnitude > 0.01f ? VectorUtils.Flatten(leader.transform.forward) : Vector3.forward);
                return;
            }
        }
    }

    bool Ground(Vector3 point, float height, out float y)
    {
        y = height;
        float best = float.MaxValue;
        foreach (var g in Physics.RaycastAll(new Vector3(point.x, height + 2f, point.z), Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (g.transform.IsChildOf(transform) || (leader != null && g.transform.IsChildOf(leader.transform))) continue;
            if (g.distance < best) { best = g.distance; y = g.point.y; }
        }
        return best < float.MaxValue;
    }
}
