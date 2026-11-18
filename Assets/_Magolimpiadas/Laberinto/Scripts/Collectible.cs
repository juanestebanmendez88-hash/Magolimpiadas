using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Collectible : MonoBehaviour
{
    public Transform item;
    public GameObject[] turnOffToCollect;

    public float lengthOnBack = 1.5f;
    public float tilt = 35f;
    public float separation = 0.3f;
    public float height = -0.1f;
    public float flightDuration = 0.6f;

    public BotMage3D owner;

    public UnityEvent onCollect;
    public bool Collected { get; private set; }

    public static event Action<Collectible> CollectedLocally;

    public string WizardId => owner != null ? owner.mageId : ChosenMage.Chosen;

    private Vector3 lengthAxis, thinAxis;
    private float currentLength;

    void Start()
    {
        MeasureAxes();
    }

    void OnTriggerEnter(Collider other)
    {
        if (Collected || item == null) return;
        if (owner != null)
        {
            var bot = other.GetComponent<BotMage3D>();
            if (bot == owner) StartCoroutine(Collect(bot.animator, bot.transform, true));
            return;
        }
        var playerMage = other.GetComponent<PlayerMage>();
        if (playerMage != null && playerMage.enabled) StartCoroutine(Collect(playerMage.animator, playerMage.transform, true));
    }

    public static Collectible OfWizard(string wizardId)
    {
        foreach (var bow in FindObjectsByType<Collectible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (bow.WizardId == wizardId) return bow;
        return null;
    }

    public void CollectRemotely(Animator animator, Transform body)
    {
        if (Collected || item == null || !isActiveAndEnabled) return;
        StartCoroutine(Collect(animator, body, false));
    }

    IEnumerator Collect(Animator animator, Transform body, bool collectedHere)
    {
        Collected = true;
        if (collectedHere) CollectedLocally?.Invoke(this);
        foreach (var effect in turnOffToCollect)
        {
            if (effect == null) continue;
            foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            foreach (var lightSource in effect.GetComponentsInChildren<Light>()) lightSource.enabled = false;
        }

        Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = body;

        item.SetParent(null, true);
        Vector3 initialScale = item.localScale;
        Vector3 finalScale = initialScale * (lengthOnBack / Mathf.Max(0.01f, currentLength));
        Vector3 fromPos = item.position;
        Quaternion fromRotation = item.rotation;

        for (float t = 0f; t < 1f; t += Time.deltaTime / flightDuration)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            item.localScale = Vector3.Lerp(initialScale, finalScale, s);
            BackPose(body, chest, out Vector3 meta, out Quaternion rotation);
            item.rotation = Quaternion.Slerp(fromRotation, rotation, s);
            item.position = Vector3.Lerp(fromPos, meta, s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 1.5f;
            yield return null;
        }

        item.localScale = finalScale;
        BackPose(body, chest, out Vector3 final, out Quaternion finalRotation);
        item.SetPositionAndRotation(final, finalRotation);
        item.SetParent(chest, true);
        if (collectedHere) onCollect?.Invoke();
    }

    void BackPose(Transform body, Transform chest, out Vector3 position, out Quaternion rotation)
    {
        Vector3 forward = body.forward, up = body.up, right = body.right;
        float a = tilt * Mathf.Deg2Rad;
        Vector3 length = (up * Mathf.Cos(a) + right * Mathf.Sin(a)).normalized;
        rotation = Quaternion.LookRotation(-forward, length) * Quaternion.Inverse(Quaternion.LookRotation(thinAxis, lengthAxis));

        Vector3 localCenter = LocalCenter();
        Vector3 meta = chest.position - forward * separation + up * height;
        position = meta - rotation * Vector3.Scale(localCenter, item.lossyScale);
    }

    void MeasureAxes()
    {
        if (item == null) return;
        var b = LocalLimits(out _);
        Vector3 t = b.size;
        lengthAxis = t.x >= t.y && t.x >= t.z ? Vector3.right : t.y >= t.z ? Vector3.up : Vector3.forward;
        thinAxis = t.x <= t.y && t.x <= t.z ? Vector3.right : t.y <= t.z ? Vector3.up : Vector3.forward;
        if (thinAxis == lengthAxis) thinAxis = lengthAxis == Vector3.forward ? Vector3.right : Vector3.forward;
        currentLength = Mathf.Max(0.01f, Vector3.Dot(Vector3.Scale(t, item.lossyScale), lengthAxis));
    }

    Vector3 LocalCenter()
    {
        return LocalLimits(out _).center;
    }

    Bounds LocalLimits(out bool has)
    {
        has = false;
        var b = new Bounds();
        foreach (var mf in item.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var world = mf.transform.TransformPoint(corner);
                var local = item.InverseTransformPoint(world);
                if (!has) { b = new Bounds(local, Vector3.zero); has = true; }
                else b.Encapsulate(local);
            }
        }
        return b;
    }
}
