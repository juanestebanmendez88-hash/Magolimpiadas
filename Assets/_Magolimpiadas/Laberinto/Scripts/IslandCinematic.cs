using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArcaneGames.Network;
using UnityEngine;

public class IslandCinematic : MonoBehaviour
{
    public ThirdPersonCamera gameCamera;
    public Camera cinematicCamera;

    public Transform arrivalPortal;
    public Transform cauldron;
    public GameObject cauldronFire;
    public Transform islandCenter;
    public GameObject teleportEffect;

    public BotMage3D[] otherMages;
    public float pauseBetweenMages = 0.4f;

    public PanelUI titlePanel;
    public PanelUI fadePanel;

    public float portalExitDistance = 3.5f;
    public float exitCameraDistance = 6f;
    public float riseDuration = 2f;

    public float walkSpeed = 2.2f;
    public float cauldronMargin = 0.6f;
    public float separationAround = 55f;
    public float overheadViewHeight = 13f;

    public string igniteTrigger = "Encender";
    public float igniteDuration = 3f;
    [Range(0f, 1f)] public float igniteMoment = 0.5f;

    public float orbitRadius = 55f;
    public float orbitHeight = 22f;
    public float orbitDuration = 12f;
    public float returnDuration = 1.5f;
    public bool onlyFirstTime = true;

    public static bool AnyInProgress { get; private set; }

    public bool InProgress { get; private set; }
    public string Phase { get; private set; } = "espera";

    class Actor
    {
        public Transform t;
        public CharacterController control;
        public Animator animator;
        public BotMage3D bot;
        public Transform portal;
        public Vector3 exit, normal;
        public bool walking;
    }

    private bool alreadySeen;
    private PlayerMage playerMage;
    private Actor playerMageActor;
    private readonly List<Actor> actors = new List<Actor>();
    private Actor focus;
    private Camera mainCamera;
    private Vector3 cameraSpeed;

    private Vector3 fixedPosePosition;
    private float overheadBlend;
    private bool overheadStarted;
    private Vector3 overheadPosition;
    private Vector3 overheadLook;
    private Vector3 gazeSpeed;
    private Vector3 head, headSpeed;
    private bool cut;

    void Start()
    {
        if (cauldronFire != null) cauldronFire.SetActive(false);
        if (cinematicCamera != null) cinematicCamera.enabled = false;
    }

    void OnDestroy()
    {
        if (InProgress) AnyInProgress = false;
    }

    static void ShowBody(Actor a, bool visible)
    {
        foreach (var r in a.t.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = !visible;
    }

    public void Begin(PlayerMage player, Vector3 origin)
    {
        if (InProgress) return;
        PrepareLocalActor(player);
        StartCoroutine(Sequence(origin, false));
    }

    public bool TryStartShared()
    {
        var player = FindFirstObjectByType<PlayerMage>();
        var world = WorldSwitcher.Instance;
        if (InProgress || player == null || !player.Available || LabyrinthIntro.InProgress || (world != null && world.Busy)) return false;
        PrepareLocalActor(player);
        StartCoroutine(Sequence(null, true));
        return true;
    }

    void PrepareLocalActor(PlayerMage player)
    {
        playerMage = player;
        mainCamera = gameCamera.GetComponent<Camera>();
        playerMageActor = new Actor { t = playerMage.transform, control = playerMage.GetComponent<CharacterController>(), animator = playerMage.animator, portal = arrivalPortal };
    }

    IEnumerator Sequence(Vector3? origin, bool shared)
    {
        InProgress = true;
        AnyInProgress = true;
        playerMage.enabled = false;
        var animator = playerMageActor.animator;

        Phase = "entrando";
        animator.SetBool("EnSuelo", true);
        if (playerMage.minotaur != null) playerMage.minotaur.LoseTrack();
        if (origin.HasValue) yield return WalkIntoPortal(origin.Value);
        else animator.SetFloat("Velocidad", 0f);
        if (fadePanel != null) yield return fadePanel.ShowAndWait();
        yield return new WaitForSeconds(0.3f);

        Phase = "viaje";
        PrepareExit(playerMageActor);
        playerMage.Teleport(playerMageActor.exit);
        playerMage.transform.rotation = Quaternion.LookRotation(playerMageActor.normal);
        playerMage.SetRespawnOutside(playerMageActor.exit + playerMageActor.normal * 2.5f);
        ShowEffect(playerMageActor.exit);
        ShowBody(playerMageActor, true);

        actors.Clear();
        actors.Add(playerMageActor);
        var bots = new List<Actor>();
        if (otherMages != null)
            foreach (var b in otherMages)
            {
                if (b == null || b.islandPortal == null || (!shared && b.state != BotMage3D.State.Waiting)) continue;
                var a = new Actor { t = b.transform, control = b.Control, animator = b.animator, bot = b, portal = b.islandPortal };
                b.state = BotMage3D.State.Cinematic;
                PrepareExit(a);
                ShowBody(a, false);
                b.Teleport(a.exit);
                b.transform.rotation = Quaternion.LookRotation(a.normal);
                a.animator.SetBool("EnSuelo", true);
                a.animator.SetFloat("Velocidad", 0f);
                bots.Add(a);
                actors.Add(a);
            }

        if (!shared && alreadySeen && onlyFirstTime)
        {
            playerMage.Teleport(playerMageActor.exit + playerMageActor.normal * 2.5f);
            foreach (var a in bots) { ShowBody(a, true); BeginToFollow(a.bot); a.bot.AppearNextToLeader(); }
            if (gameCamera.FirstPerson) ShowBody(playerMageActor, false);
            gameCamera.Restart();
            if (fadePanel != null) fadePanel.Hide();
            Finish();
            yield break;
        }
        if (fadePanel != null) fadePanel.Hide();

        gameCamera.enabled = false;
        mainCamera.enabled = false;
        cinematicCamera.enabled = true;
        LookAtPortal(playerMageActor);
        animator.SetBool("EnSuelo", true);
        yield return Wait(0.5f);

        Phase = "salida";
        yield return Walk(playerMageActor, playerMageActor.exit + playerMageActor.normal * portalExitDistance, bots.Count > 0, true);

        foreach (var a in bots)
        {
            Phase = "salida " + a.bot.displayName;
            LookAtPortal(a);
            yield return Wait(pauseBetweenMages);
            ShowEffect(a.exit);
            ShowBody(a, true);
            yield return Wait(0.3f);
            yield return Walk(a, a.exit + a.normal * portalExitDistance, true, true);
        }

        Phase = "caminata";
        focus = actors[actors.Count - 1];
        fixedPosePosition = cinematicCamera.transform.position;
        StartCoroutine(RaiseCamera());
        var destinations = DestinationsAround();
        foreach (var a in bots) StartCoroutine(WalkAlone(a, destinations[a]));
        yield return Walk(playerMageActor, destinations[playerMageActor], true, true);
        while (bots.Any(a => a.walking)) { RefreshCamera(); yield return null; }

        var fromPoint = actors.ToDictionary(a => a, a => a.t.rotation);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            foreach (var a in actors)
                a.t.rotation = Quaternion.Slerp(fromPoint[a], Quaternion.LookRotation(VectorUtils.Flatten(cauldron.position - a.t.position)), t);
            RefreshCamera();
            yield return null;
        }

        Phase = "encender";
        foreach (var a in actors) a.animator.SetTrigger(igniteTrigger);
        yield return Wait(igniteDuration * igniteMoment);
        foreach (var a in bots) StartCoroutine(Spark(a));
        yield return Spark(playerMageActor);
        IgniteCauldron();
        yield return Wait(igniteDuration * (1f - igniteMoment) + 0.5f);

        Phase = "recorrido";
        if (titlePanel != null) titlePanel.ShowFor(Mathf.Max(1f, orbitDuration * 0.45f));
        yield return Tour();

        Phase = "vuelta";
        gameCamera.Restart();
        gameCamera.enabled = true;
        yield return null;
        Vector3 fromPos = cinematicCamera.transform.position;
        Quaternion fromRotation = cinematicCamera.transform.rotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / returnDuration)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            var meta = gameCamera.transform;
            cinematicCamera.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, meta.position, s), Quaternion.Slerp(fromRotation, meta.rotation, s));
            yield return null;
        }

        foreach (var a in bots)
            if (!OnlineMatch.IsPuppet(a.bot.mageId)) BeginToFollow(a.bot);
        alreadySeen = true;
        Finish();
    }

    IEnumerator WalkIntoPortal(Vector3 origin)
    {
        Vector3 towardPortal = VectorUtils.Flatten(origin - playerMage.transform.position).normalized;
        if (towardPortal.sqrMagnitude < 0.01f) towardPortal = VectorUtils.Flatten(playerMage.transform.forward).normalized;
        Vector3 inside = new Vector3(origin.x, playerMage.transform.position.y, origin.z) + towardPortal * 0.6f;
        yield return Walk(playerMageActor, inside, false, false, true);
        ShowBody(playerMageActor, false);
        ShowEffect(origin);
        playerMageActor.animator.SetFloat("Velocidad", 0f);
        yield return new WaitForSeconds(0.5f);
    }

    void BeginToFollow(BotMage3D b)
    {
        b.leader = playerMage;
        b.state = BotMage3D.State.Following;
    }

    void Finish()
    {
        mainCamera.enabled = true;
        gameCamera.enabled = true;
        if (cinematicCamera != null) cinematicCamera.enabled = false;
        playerMageActor.animator.SetFloat("Velocidad", 0f);
        playerMage.enabled = true;
        InProgress = false;
        AnyInProgress = false;
        Phase = "fin";
    }

    IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime) { RefreshCamera(); yield return null; }
    }

    void PrepareExit(Actor a)
    {
        a.normal = PortalFront(a.portal);
        Vector3 exit = a.portal.position + a.normal * 0.3f;
        exit.y = GroundHeight(exit, a.portal.position.y) + 0.05f;
        a.exit = exit;
    }

    void LookAtPortal(Actor a)
    {
        focus = a;
        cut = true;
        fixedPosePosition = a.exit + a.normal * exitCameraDistance + Vector3.up * 2f;
        overheadBlend = 0f;
        overheadStarted = false;
        RefreshCamera();
    }

    Vector3 PortalFront(Transform portal)
    {
        Vector3 towardCauldron = VectorUtils.Flatten(cauldron.position - portal.position).normalized;
        var disc = portal.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null);
        if (disc == null) return towardCauldron;

        var t = disc.sharedMesh.bounds.size;
        var local = t.x <= t.y && t.x <= t.z ? Vector3.right : t.y <= t.z ? Vector3.up : Vector3.forward;
        Vector3 normal = VectorUtils.Flatten(disc.transform.TransformDirection(local)).normalized;
        if (normal.sqrMagnitude < 0.01f) return towardCauldron;
        return Vector3.Dot(normal, towardCauldron) >= 0f ? normal : -normal;
    }

    Dictionary<Actor, Vector3> DestinationsAround()
    {
        float radius = CauldronRadius() + cauldronMargin;
        var angles = actors.Select(a => { var d = VectorUtils.Flatten(a.t.position - cauldron.position); return Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg; }).ToArray();
        float min = Mathf.Min(separationAround, 360f / Mathf.Max(1, actors.Count) - 1f);
        for (int iteration = 0; iteration < 30; iteration++)
            for (int i = 0; i < angles.Length; i++)
                for (int j = i + 1; j < angles.Length; j++)
                {
                    float d = Mathf.DeltaAngle(angles[i], angles[j]);
                    if (Mathf.Abs(d) >= min) continue;
                    float push = (min - Mathf.Abs(d)) * 0.5f * (d >= 0f ? 1f : -1f);
                    angles[i] -= push;
                    angles[j] += push;
                }
        var destinations = new Dictionary<Actor, Vector3>();
        for (int i = 0; i < actors.Count; i++)
        {
            float r = angles[i] * Mathf.Deg2Rad;
            destinations[actors[i]] = cauldron.position + new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r)) * radius;
        }
        return destinations;
    }

    IEnumerator WalkAlone(Actor a, Vector3 destination)
    {
        a.walking = true;
        yield return Walk(a, destination, true, false);
        a.walking = false;
    }

    IEnumerator Walk(Actor a, Vector3 destination, bool stop, bool useCinematicCamera, bool passThrough = false)
    {
        float fall = 0f;
        float check = 1f;
        Vector3 lastCheck = a.t.position;
        bool direct = passThrough;

        while (true)
        {
            Vector3 remaining = VectorUtils.Flatten(destination - a.t.position);
            if (remaining.magnitude < 0.15f) break;

            Vector3 step = remaining.normalized * Mathf.Min(walkSpeed * Time.deltaTime, remaining.magnitude);
            a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(remaining), 10f * Time.deltaTime);
            a.animator.SetFloat("Velocidad", 1f, 0.1f, Time.deltaTime);

            if (!direct)
            {
                if (a.control.isGrounded) fall = -2f;
                fall += -20f * Time.deltaTime;
                a.control.Move(step + Vector3.up * fall * Time.deltaTime);

                check -= Time.deltaTime;
                if (check <= 0f)
                {
                    if (VectorUtils.Flatten(a.t.position - lastCheck).magnitude < 0.3f) direct = true;
                    lastCheck = a.t.position;
                    check = 1f;
                }
            }
            else
            {
                a.control.enabled = false;
                Vector3 p = a.t.position + step;
                p.y = GroundHeight(p, a.t.position.y);
                a.t.position = p;
            }

            if (useCinematicCamera) RefreshCamera();
            yield return null;
        }

        a.control.enabled = true;
        if (!stop) yield break;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            a.animator.SetFloat("Velocidad", 0f, 0.1f, Time.deltaTime);
            if (useCinematicCamera) RefreshCamera();
            yield return null;
        }
        a.animator.SetFloat("Velocidad", 0f);
    }

    IEnumerator RaiseCamera()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / riseDuration)
        {
            overheadBlend = t;
            yield return null;
        }
        overheadBlend = 1f;
    }

    Vector3 OverheadPose(out Vector3 gaze)
    {
        bool group = actors.Count > 1;
        Vector3 m = playerMage.transform.position;
        gaze = Vector3.Lerp(m, cauldron.position, group ? 0.85f : 0.4f);
        float height = overheadViewHeight * (group ? 1.3f : 1f);
        Vector3 back = VectorUtils.Flatten(m - cauldron.position).normalized;
        return gaze + back * (height * 0.45f) + Vector3.up * height;
    }

    void RefreshCamera()
    {
        if (focus == null) focus = playerMageActor;
        Vector3 meta = OverheadPose(out Vector3 targetLook);
        if (!overheadStarted)
        {
            overheadPosition = meta;
            overheadLook = targetLook;
            cameraSpeed = gazeSpeed = Vector3.zero;
            overheadStarted = true;
        }
        overheadPosition = Vector3.SmoothDamp(overheadPosition, meta, ref cameraSpeed, 0.6f);
        overheadLook = Vector3.SmoothDamp(overheadLook, targetLook, ref gazeSpeed, 0.6f);

        Vector3 goalHead = focus.t.position + Vector3.up * 1.2f;
        if (cut) { head = goalHead; headSpeed = Vector3.zero; cut = false; }
        else head = Vector3.SmoothDamp(head, goalHead, ref headSpeed, 0.25f);

        float s = Mathf.SmoothStep(0f, 1f, overheadBlend);
        Vector3 position = Vector3.Lerp(fixedPosePosition, overheadPosition, s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 4f;
        Vector3 target = Vector3.Lerp(head, overheadLook, s);
        Vector3 direction = (target - position).normalized;

        Vector3 towardCauldron = VectorUtils.Flatten(cauldron.position - position).normalized;
        Vector3 screenUp = Vector3.Slerp(Vector3.up, towardCauldron, Mathf.Clamp01(-direction.y));
        cinematicCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, screenUp));
    }

    IEnumerator Spark(Actor a)
    {
        var torchFire = a.t.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "Fuego" && t.parent != null && t.parent.name == "Antorcha");
        Vector3 fromPoint = torchFire != null ? torchFire.position : a.t.position + Vector3.up * 1.8f;
        Vector3 until = cauldronFire != null ? cauldronFire.transform.position : cauldron.position;

        GameObject spark = null;
        if (torchFire != null)
        {
            spark = Instantiate(torchFire.gameObject, fromPoint, Quaternion.identity);
            spark.transform.localScale = Vector3.one * 1.6f;
            foreach (var r in spark.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false;
        }
        if (a.bot != null) a.bot.DropTorch(); else playerMage.DropTorch();
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
        {
            Vector3 p = Vector3.Lerp(fromPoint, until, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.5f;
            if (spark != null) spark.transform.position = p;
            if (a == playerMageActor) RefreshCamera();
            yield return null;
        }
        if (spark != null) Destroy(spark);
    }

    void IgniteCauldron()
    {
        if (cauldronFire == null) return;
        cauldronFire.SetActive(true);
        GameSound.Play(GameAudio.Sound.Torch, 0.9f);
        StartCoroutine(RaiseLight());
    }

    IEnumerator RaiseLight()
    {
        var flickers = cauldronFire.GetComponentsInChildren<LightFlicker>();
        var goals = flickers.Select(p => p.intensity).ToArray();
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
        {
            for (int i = 0; i < flickers.Length; i++) flickers[i].intensity = goals[i] * t;
            yield return null;
        }
        for (int i = 0; i < flickers.Length; i++) flickers[i].intensity = goals[i];
    }

    IEnumerator Tour()
    {
        Vector3 center = islandCenter != null ? islandCenter.position : cauldron.position;
        var cam = cinematicCamera.transform;
        Vector3 start = VectorUtils.Flatten(cam.position - center);
        float startAngle = Mathf.Atan2(start.z, start.x);

        Vector3 fromPos = cam.position;
        Quaternion fromRotation = cam.rotation;
        Vector3 Point(float angle) => center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius + Vector3.up * orbitHeight;
        Quaternion Gaze(Vector3 p) => Quaternion.LookRotation(center + Vector3.up * 2f - p);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 2.5f)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            Vector3 p = Vector3.Lerp(fromPos, Point(startAngle), s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 6f;
            cam.SetPositionAndRotation(p, Quaternion.Slerp(fromRotation, Gaze(p), s));
            yield return null;
        }

        for (float t = 0f; t < 1f; t += Time.deltaTime / orbitDuration)
        {
            float angle = startAngle + Mathf.SmoothStep(0f, 1f, t) * 2f * Mathf.PI;
            Vector3 p = Point(angle);
            cam.SetPositionAndRotation(p, Gaze(p));
            yield return null;
        }
    }

    float CauldronRadius()
    {
        var rs = cauldron.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 1f;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return Mathf.Max(b.extents.x, b.extents.z);
    }

    float GroundHeight(Vector3 point, float fallback)
    {
        var hits = Physics.RaycastAll(point + Vector3.up * 1.5f, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore)
            .Where(g => g.collider.GetComponentInParent<PlayerMage>() == null && g.collider.GetComponentInParent<BotMage3D>() == null)
            .OrderBy(g => g.distance);
        foreach (var g in hits) return g.point.y;
        return fallback;
    }

    void ShowEffect(Vector3 place)
    {
        if (teleportEffect == null) return;
        var effect = Instantiate(teleportEffect, place, Quaternion.identity);
        StartCoroutine(TurnOffEffect(effect));
    }

    IEnumerator TurnOffEffect(GameObject effect)
    {
        yield return new WaitForSeconds(0.9f);
        foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(effect, 1.5f);
    }
}
