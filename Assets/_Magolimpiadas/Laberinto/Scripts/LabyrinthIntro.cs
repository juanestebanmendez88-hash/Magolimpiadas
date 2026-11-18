using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LabyrinthIntro : MonoBehaviour
{
    public enum Highlight { Nothing, Target, PlayerFlame }

    [System.Serializable]
    public class Step
    {
        [TextArea(2, 4)] public string text;
        public Transform target;
        public bool isPlayer;
        public float distance = 6f;
        public float height = 3f;
        public float gazeHeight = 1f;
        public bool fromFront;
        public float rotation;
        public Highlight highlight = Highlight.Target;
        public Transform cameraPoint;
    }

    public OwlGuide guide;
    public Camera sceneCamera;
    public bool onBegin = true;
    public Key skipKey = Key.Escape;
    public float smoothing = 0.6f;
    public float viewDistance = 85f;
    public bool avoidWalls = true;
    public List<Step> steps = new List<Step>();

    public static bool InProgress { get; private set; }

    PlayerMage player;
    ThirdPersonCamera gameCamera;
    Camera mainCamera;
    Step actual;
    Vector3 speed;
    float farClipBefore;
    Vector3 center;
    Minotaur minotaur;
    Vector3 minotaurPlace;
    float extraHeight, extraRotation;

    IEnumerator Start()
    {
        yield return null;
        if (onBegin) yield return Play();
    }

    public IEnumerator Play()
    {
        player = FindFirstObjectByType<PlayerMage>();
        mainCamera = Camera.main;
        gameCamera = mainCamera != null ? mainCamera.GetComponent<ThirdPersonCamera>() : null;
        if (player == null || sceneCamera == null || guide == null || mainCamera == null || steps.Count == 0) yield break;

        InProgress = true;
        center = player.labyrinthZone.center;
        player.enabled = false;
        minotaur = FindFirstObjectByType<Minotaur>();
        if (minotaur != null) minotaurPlace = minotaur.transform.position;
        player.animator.SetFloat("Velocidad", 0f);

        sceneCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
        farClipBefore = sceneCamera.farClipPlane;
        sceneCamera.farClipPlane = viewDistance;
        if (gameCamera != null) gameCamera.enabled = false;
        mainCamera.enabled = false;
        sceneCamera.enabled = true;
        guide.Notice("Esc · saltar");

        foreach (var p in steps)
        {
            extraRotation = 0f;
            extraHeight = avoidWalls && p.cameraPoint == null ? FreeHeight(p) : 0f;
            actual = p;
            speed = Vector3.zero;
            for (float t = 0f; t < smoothing * 2.5f; t += Time.unscaledDeltaTime)
            {
                Pose(p, out Vector3 meta, out _);
                if ((sceneCamera.transform.position - meta).magnitude < 0.2f) break;
                yield return null;
            }
            yield return guide.Say(CreateText(p.text), PointedPoint(p));
        }

        actual = null;
        yield return guide.Close();
        yield return ReturnToGame();
        Finish();
    }

    void Update()
    {
        if (!InProgress) return;
        var k = Keyboard.current;
        if (k != null && k[skipKey].wasPressedThisFrame) Skip();
    }

    void LateUpdate()
    {
        if (!InProgress || actual == null) return;
        Pose(actual, out Vector3 meta, out Vector3 look);
        var c = sceneCamera.transform;
        c.position = Vector3.SmoothDamp(c.position, meta, ref speed, smoothing, Mathf.Infinity, Time.unscaledDeltaTime);
        var rotation = Quaternion.LookRotation(look - c.position);
        c.rotation = Quaternion.Slerp(c.rotation, rotation, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
    }

    public void Skip()
    {
        if (!InProgress) return;
        StopAllCoroutines();
        guide.CloseNow();
        actual = null;
        Finish();
    }

    Transform Target(Step p) => p.isPlayer || p.target == null ? player.transform : p.target;

    void Pose(Step p, out Vector3 position, out Vector3 look)
    {
        if (p.cameraPoint != null)
        {
            position = p.cameraPoint.position;
            look = position + p.cameraPoint.forward * 10f;
            return;
        }
        var t = Target(p);
        Vector3 b = t.position;
        look = b + Vector3.up * p.gazeHeight;
        Vector3 dir;
        if (p.fromFront) dir = VectorUtils.Flatten(t.forward);
        else
        {
            dir = VectorUtils.Flatten(center - b);
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
        }
        dir = Quaternion.Euler(0f, p.rotation + (p == actual ? extraRotation : 0f), 0f) * dir.normalized;
        position = b + dir * p.distance + Vector3.up * (p.height + (p == actual ? extraHeight : 0f));
    }

    public void ShotFor(int index, out Vector3 position, out Vector3 look)
    {
        if (player == null) player = FindFirstObjectByType<PlayerMage>();
        center = player != null ? player.labyrinthZone.center : Vector3.zero;
        var p = steps[index];
        var previous = actual;
        extraRotation = 0f;
        extraHeight = avoidWalls && p.cameraPoint == null ? FreeHeight(p) : 0f;
        actual = p;
        Pose(p, out position, out look);
        actual = previous;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying || steps == null) return;
        for (int i = 0; i < steps.Count; i++)
        {
            if (!steps[i].isPlayer && steps[i].target == null) continue;
            ShotFor(i, out Vector3 pos, out Vector3 look);
            Gizmos.color = Color.HSVToRGB(i / (float)Mathf.Max(1, steps.Count), 0.7f, 1f);
            Gizmos.DrawWireSphere(pos, 0.35f);
            Gizmos.DrawLine(pos, look);
            if (steps[i].cameraPoint != null && sceneCamera != null)
            {
                Gizmos.matrix = Matrix4x4.TRS(pos, steps[i].cameraPoint.rotation, Vector3.one);
                Gizmos.DrawFrustum(Vector3.zero, sceneCamera.fieldOfView, 3f, 0.2f, 16f / 9f);
                Gizmos.matrix = Matrix4x4.identity;
            }
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, "Paso " + (i + 1));
        }
    }
#endif

    static readonly float[] TestRotations = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f };
    static readonly float[] FrontRotations = { 0f, 30f, -30f, 60f, -60f };

    float FreeHeight(Step p)
    {
        var previous = actual;
        actual = p;
        var t = Target(p);
        float bestHeight = 12f, bestRotation = 0f;
        for (float extra = 0f; extra <= 12f; extra += 0.5f)
        {
            bool found = false;
            foreach (float g in p.fromFront ? FrontRotations : TestRotations)
            {
                extraHeight = extra;
                extraRotation = g;
                Pose(p, out Vector3 pos, out Vector3 look);
                if (!Covered(look, pos, t)) { bestHeight = extra; bestRotation = g; found = true; break; }
            }
            if (found) break;
        }
        actual = previous;
        extraRotation = bestRotation;
        return bestHeight;
    }

    static bool Covered(Vector3 fromPoint, Vector3 until, Transform target)
    {
        foreach (var g in Physics.RaycastAll(fromPoint, until - fromPoint, Vector3.Distance(fromPoint, until), ~0, QueryTriggerInteraction.Ignore))
        {
            var tr = g.collider.transform;
            if (tr.IsChildOf(target) || g.collider.GetComponentInParent<PlayerMage>() != null ||
                g.collider.GetComponentInParent<Minotaur>() != null || g.collider.GetComponentInParent<BotMage3D>() != null) continue;
            return true;
        }
        return false;
    }

    Vector3? PointedPoint(Step p)
    {
        Vector3 point;
        switch (p.highlight)
        {
            case Highlight.Target:
                point = Target(p).position + Vector3.up * p.gazeHeight;
                break;
            case Highlight.PlayerFlame:
                var flame = player.torch != null ? player.torch.transform.Find("Fuego") : null;
                point = flame != null ? flame.position : player.torch != null ? player.torch.transform.position : player.transform.position + Vector3.up * 1.5f;
                break;
            default:
                return null;
        }
        return sceneCamera.WorldToScreenPoint(point);
    }

    string CreateText(string t)
    {
        return string.IsNullOrEmpty(t) ? "" : t.Replace("{mago}", PlayerName());
    }

    static string PlayerName()
    {
        switch (ChosenMage.Chosen)
        {
            case "elemental": return "Mago Elemental";
            case "druida": return "Druida Verde";
            case "rayo": return "Mago del Rayo";
            default: return "Maga Nebulosa";
        }
    }

    IEnumerator ReturnToGame()
    {
        if (gameCamera == null) yield break;
        gameCamera.Restart();
        gameCamera.enabled = true;
        yield return null;
        Vector3 fromPos = sceneCamera.transform.position;
        Quaternion fromRotation = sceneCamera.transform.rotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.2f)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            var meta = mainCamera.transform;
            sceneCamera.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, meta.position, s), Quaternion.Slerp(fromRotation, meta.rotation, s));
            yield return null;
        }
    }

    void Finish()
    {
        sceneCamera.enabled = false;
        sceneCamera.farClipPlane = farClipBefore;
        mainCamera.enabled = true;
        if (gameCamera != null) { gameCamera.enabled = true; gameCamera.Restart(); }
        if (minotaur != null)
            minotaur.ReturnTo(minotaurPlace);
        StartCoroutine(ReturnControl());
    }

    IEnumerator ReturnControl()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        var world = WorldSwitcher.Instance;
        if ((world == null || !world.Busy) && !SwimmingRace.AnyRunning) player.enabled = true;
        InProgress = false;
    }
}
