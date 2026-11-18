using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Racket : MonoBehaviour
{
    public string mageId = "nebulosa";

    public bool useExactPose = true;
    public Vector3 exactPosition = new Vector3(0.15208f, 0.19395f, 0.20056f);
    public Vector3 exactRotation = new Vector3(25f, -90f, 0f);
    public float exactScale = 0.13581f;

    public float lengthInHand = 0.6f;
    public Vector3 rotationInHand = new Vector3(0f, 0f, 115f);
    public Vector3 positionInHand = new Vector3(0f, 0.04f, 0f);
    [Range(0f, 1f)] public float grip = 0.15f;
    public float flightDuration = 0.6f;
    public float collectRadius = 0.7f;

    public bool leaveDecoration = true;

    public UnityEvent onCollect;

    public BotMage3D Owner { get; private set; }
    public bool Collected { get; private set; }

    public static readonly List<Racket> All = new List<Racket>();

    public static bool PlayerHasOwn => All.Any(r => r != null && r.Owner == null && r.Collected);

    public static event Action<Racket> CollectedLocally;

    public static Racket OfWizard(string wizardId) => All.FirstOrDefault(r => r != null && r.mageId == wizardId);

    SphereCollider zone;
    Vector3 lengthAxis, faceAxis;
    float meshLength;
    Vector3 handleTip;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        Owner = FindObjectsByType<BotMage3D>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.mageId == mageId);
        if (Owner != null) Owner.racket = this;

        Measure();
        zone = gameObject.AddComponent<SphereCollider>();
        zone.isTrigger = true;
        float scale = Mathf.Max(0.0001f, transform.lossyScale.x);
        zone.radius = collectRadius / scale;
        zone.center = handleTip + lengthAxis * (meshLength * 0.5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Collected) return;
        if (Owner != null)
        {
            var bot = other.GetComponent<BotMage3D>();
            if (bot == Owner) StartCoroutine(Collect(bot.animator, true));
            return;
        }
        var player = other.GetComponent<PlayerMage>();
        if (player != null && player.enabled) StartCoroutine(Collect(player.animator, true));
    }

    public void CollectRemotely(Animator animator)
    {
        if (Collected || !isActiveAndEnabled) return;
        StartCoroutine(Collect(animator, false));
    }

    IEnumerator Collect(Animator animator, bool collectedHere)
    {
        Collected = true;
        if (collectedHere) CollectedLocally?.Invoke(this);
        if (zone != null) zone.enabled = false;
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null) hand = animator.transform;

        if (leaveDecoration)
        {
            var parent = transform.parent;
            var children = new List<Transform>();
            foreach (Transform h in transform) children.Add(h);
            foreach (var h in children)
            {
                h.SetParent(parent, true);
                foreach (var ps in h.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                foreach (var lightSource in h.GetComponentsInChildren<Light>()) lightSource.enabled = false;
            }
        }

        transform.SetParent(null, true);
        Vector3 initialScale = transform.localScale;
        float lengthNow = meshLength * transform.lossyScale.x;
        Vector3 finalScale = useExactPose ? Vector3.one * exactScale
                                             : initialScale * (lengthInHand / Mathf.Max(0.01f, lengthNow));
        Vector3 fromPoint = transform.position;
        Quaternion rotationFrom = transform.rotation;

        for (float t = 0f; t < 1f; t += Time.deltaTime / flightDuration)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(initialScale, finalScale, s);
            Pose(hand, out Vector3 meta, out Quaternion rotation);
            transform.rotation = Quaternion.Slerp(rotationFrom, rotation, s);
            transform.position = Vector3.Lerp(fromPoint, meta, s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 1.2f;
            yield return null;
        }
        transform.localScale = finalScale;
        Pose(hand, out Vector3 final, out Quaternion finalRotation);
        transform.SetPositionAndRotation(final, finalRotation);
        transform.SetParent(hand, true);
        if (collectedHere) onCollect?.Invoke();
    }

    [ContextMenu("Volver a colocar en la mano")]
    public void Reposition()
    {
        if (!Collected || transform.parent == null) return;
        Pose(transform.parent, out Vector3 p, out Quaternion g);
        transform.SetPositionAndRotation(p, g);
        if (useExactPose) transform.localScale = Vector3.one * exactScale / Mathf.Max(0.0001f, transform.parent.lossyScale.x);
    }

    void Pose(Transform hand, out Vector3 position, out Quaternion rotation)
    {
        if (useExactPose)
        {
            rotation = hand.rotation * Quaternion.Euler(exactRotation);
            position = hand.position + hand.rotation * exactPosition;
            return;
        }
        Quaternion handWorldRotation = hand.rotation * Quaternion.Euler(rotationInHand);
        rotation = handWorldRotation * Quaternion.Inverse(Quaternion.LookRotation(faceAxis, lengthAxis));
        Vector3 meshGrip = handleTip + lengthAxis * (meshLength * grip);
        Vector3 palm = hand.position + hand.rotation * positionInHand;
        position = palm - rotation * Vector3.Scale(meshGrip, transform.lossyScale);
    }

    void Measure()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) { lengthAxis = Vector3.up; faceAxis = Vector3.forward; meshLength = 1f; return; }
        var b = mf.sharedMesh.bounds;
        Vector3 t = b.size;
        lengthAxis = t.x >= t.y && t.x >= t.z ? Vector3.right : t.y >= t.z ? Vector3.up : Vector3.forward;
        faceAxis = t.x <= t.y && t.x <= t.z ? Vector3.right : t.y <= t.z ? Vector3.up : Vector3.forward;
        if (faceAxis == lengthAxis) faceAxis = lengthAxis == Vector3.forward ? Vector3.right : Vector3.forward;
        meshLength = Vector3.Dot(t, lengthAxis);

        var v = mf.sharedMesh.vertices;
        float min = Vector3.Dot(b.min, lengthAxis), max = Vector3.Dot(b.max, lengthAxis);
        float minWidth = 0f, maxWidth = 0f;
        Vector3 side = Vector3.one - lengthAxis - faceAxis;
        foreach (var p in v)
        {
            float d = Vector3.Dot(p, lengthAxis);
            float width = Mathf.Abs(Vector3.Dot(p - b.center, side));
            if (d < min + meshLength * 0.15f) minWidth = Mathf.Max(minWidth, width);
            if (d > max - meshLength * 0.15f) maxWidth = Mathf.Max(maxWidth, width);
        }
        if (maxWidth < minWidth) lengthAxis = -lengthAxis;
        handleTip = b.center - lengthAxis * (meshLength * 0.5f);
    }
}
