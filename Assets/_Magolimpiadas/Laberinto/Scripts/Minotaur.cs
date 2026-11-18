using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Minotaur : MonoBehaviour
{
    private const string ChasingParameter = "Persiguiendo";
    private const string CatchParameter = "Atrapar";
    private const float PreyRadius = 0.35f;
    private const float WaypointReachedDistance = 0.5f;
    private const float MinimumAnimationSpeed = 0.2f;
    private const float UnreachableGiveUpSeconds = 2f;

    [FormerlySerializedAs("agente")] [SerializeField] private NavMeshAgent agent;
    [FormerlySerializedAs("animador")] [SerializeField] private Animator animator;
    [FormerlySerializedAs("maga")] [SerializeField] private PlayerMage player;
    [FormerlySerializedAs("puntosPatrulla")] [SerializeField] private Transform[] patrolPoints;
    [FormerlySerializedAs("distanciaVision")] [SerializeField] private float visionDistance = 12f;
    [FormerlySerializedAs("radioVision")] [SerializeField] private float visionRadius = 0.45f;
    [FormerlySerializedAs("alturaVision")] [SerializeField] private float visionHeight = 1f;
    [FormerlySerializedAs("capasVision")] [SerializeField] private LayerMask visionLayers = ~0;
    [FormerlySerializedAs("velocidadPersecucion")] [SerializeField] private float chaseSpeed = 5.5f;
    [FormerlySerializedAs("distanciaAtrapar")] [SerializeField] private float catchDistance = 1.5f;
    [FormerlySerializedAs("tiempoRespawn")] [SerializeField] private float respawnSeconds = 3f;
    [FormerlySerializedAs("tiempoGolpe")] [SerializeField] private float hitPauseSeconds = 1.5f;

    private readonly List<IMinotaurPrey> prey = new List<IMinotaurPrey>();
    private IMinotaurPrey target;
    private LocalPlayerPrey localPrey;
    private float patrolSpeed;
    private float hitPauseEnd;
    private float unreachableSince = -1f;

    public event Action ChaseStarted;
    public event Action PreyCaught;

    public bool IsChasing => target != null;
    public Animator Animator => animator;
    public NavMeshAgent Agent => agent;

    public void Configure(NavMeshAgent navigationAgent, Animator bodyAnimator, Transform[] waypoints, PlayerMage localPlayer)
    {
        agent = navigationAgent;
        animator = bodyAnimator;
        patrolPoints = waypoints;
        player = localPlayer;
    }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        gameObject.AddComponent<MinotaurProximitySound>();
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMage>();
        localPrey = player != null ? new LocalPlayerPrey(player) : null;
        patrolSpeed = agent.speed;
        GoToRandomWaypoint();
    }

    private void Update()
    {
        if (IsPausedAfterHit())
            return;

        float referenceSpeed = IsChasing ? chaseSpeed : patrolSpeed;
        animator.speed = Mathf.Max(MinimumAnimationSpeed, agent.velocity.magnitude / referenceSpeed);

        if (IsChasing)
        {
            Chase();
            return;
        }

        LookForPrey();
        if (!agent.pathPending && agent.remainingDistance < WaypointReachedDistance)
            GoToRandomWaypoint();
    }

    public void LoseTrack()
    {
        if (!IsChasing)
            return;

        target = null;
        unreachableSince = -1f;
        agent.speed = patrolSpeed;
        animator.SetBool(ChasingParameter, false);
        GoToRandomWaypoint();
    }

    public void ReturnTo(Vector3 position)
    {
        if (!enabled || agent == null || !agent.enabled)
            return;

        agent.Warp(position);
        agent.isStopped = false;
        hitPauseEnd = 0f;
        target = null;
        agent.speed = patrolSpeed;
        animator.SetBool(ChasingParameter, false);
        GoToRandomWaypoint();
    }

    private bool IsPausedAfterHit()
    {
        if (hitPauseEnd <= 0f)
            return false;
        if (Time.time < hitPauseEnd)
            return true;

        hitPauseEnd = 0f;
        agent.isStopped = false;
        GoToRandomWaypoint();
        return false;
    }

    private void Chase()
    {
        if (!target.CanBeHunted)
        {
            LoseTrack();
            return;
        }

        if (Vector3.Distance(transform.position, target.Position) <= catchDistance)
        {
            CatchTarget();
            return;
        }

        if (HasGivenUpOnUnreachableTarget())
        {
            LoseTrack();
            return;
        }

        agent.SetDestination(target.Position);
    }

    private bool HasGivenUpOnUnreachableTarget()
    {
        if (agent.pathPending || agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            unreachableSince = -1f;
            return false;
        }

        if (unreachableSince < 0f)
            unreachableSince = Time.time;
        return Time.time - unreachableSince >= UnreachableGiveUpSeconds;
    }

    private void LookForPrey()
    {
        prey.Clear();
        if (localPrey != null)
            prey.Add(localPrey);
        MinotaurPreyRegistry.CopyRemoteTo(prey);

        foreach (IMinotaurPrey candidate in prey)
        {
            if (!candidate.CanBeHunted || !CanSee(candidate))
                continue;

            StartChase(candidate);
            return;
        }
    }

    private bool CanSee(IMinotaurPrey candidate)
    {
        Vector3 eye = transform.position + Vector3.up * visionHeight;
        Vector3 toPrey = candidate.Position + Vector3.up * visionHeight - eye;
        float ahead = Vector3.Dot(toPrey, transform.forward);
        if (ahead <= 0f || ahead > visionDistance)
            return false;

        float sideways = (toPrey - transform.forward * ahead).magnitude;
        return sideways <= visionRadius + PreyRadius && !IsViewBlocked(eye, toPrey, candidate.Body);
    }

    private bool IsViewBlocked(Vector3 eye, Vector3 toPrey, Transform preyBody)
    {
        foreach (RaycastHit hit in Physics.RaycastAll(eye, toPrey.normalized, toPrey.magnitude, visionLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(preyBody) && !hit.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private void StartChase(IMinotaurPrey candidate)
    {
        target = candidate;
        agent.speed = chaseSpeed;
        animator.SetBool(ChasingParameter, true);
        GameSound.PlayAt(GameAudio.Sound.MinotaurRoar, transform.position);
        ChaseStarted?.Invoke();
    }

    private void CatchTarget()
    {
        IMinotaurPrey caught = target;
        target = null;
        agent.speed = patrolSpeed;
        agent.isStopped = true;
        hitPauseEnd = Time.time + hitPauseSeconds;

        animator.speed = 1f;
        animator.SetBool(ChasingParameter, false);
        animator.SetTrigger(CatchParameter);
        caught.Catch(respawnSeconds);
        PreyCaught?.Invoke();
    }

    private void GoToRandomWaypoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.SetDestination(patrolPoints[UnityEngine.Random.Range(0, patrolPoints.Length)].position);
    }
}
