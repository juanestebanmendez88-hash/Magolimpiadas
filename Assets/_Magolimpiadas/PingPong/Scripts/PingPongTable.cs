using UnityEngine;

public class PingPongTable : MonoBehaviour
{
    public PingPongLevel level;
    public float touchDistance = 0.8f;
    [TextArea(2, 4)]
    public string noRacketMessage = "Para jugar contra el Guardián del Olimpo necesitas tu raqueta, {mago}. ¡Búscala cerca de aquí!";
    public float noticeSeconds = 5f;

    Collider zone;
    PlayerMage player;
    CharacterController control;
    bool waitUntilAway;
    float nextNotice;

    void Start()
    {
        zone = GetComponent<Collider>();
        if (zone == null) zone = GetComponentInChildren<Collider>();
        player = FindFirstObjectByType<PlayerMage>();
        if (player != null) control = player.GetComponent<CharacterController>();
        if (level == null) level = FindFirstObjectByType<PingPongLevel>();
    }

    void Update()
    {
        if (zone == null || player == null || level == null || PingPongLevel.Won) return;
        var world = WorldSwitcher.Instance;
        if (world == null || world.Busy || !player.enabled) { waitUntilAway = true; return; }

        Vector3 chest = player.transform.position + Vector3.up;
        float d = (zone.ClosestPoint(chest) - chest).magnitude - (control != null ? control.radius : 0.3f);
        if (waitUntilAway) { if (d > touchDistance + 1.5f) waitUntilAway = false; return; }
        if (d > touchDistance) return;

        if (Racket.PlayerHasOwn)
        {
            waitUntilAway = true;
            if (!ArcaneGames.Network.OnlineTrials.TryRequest(ArcaneGames.Network.OnlineTrial.PingPong)) level.Enter();
        }
        else if (Time.time > nextNotice && OwlNotice.Free)
        {
            nextNotice = Time.time + noticeSeconds + 6f;
            StartCoroutine(OwlNotice.Show(this, noRacketMessage, noticeSeconds,
                () => Racket.PlayerHasOwn || (zone.ClosestPoint(player.transform.position + Vector3.up) - (player.transform.position + Vector3.up)).magnitude > touchDistance + 5f));
        }
    }
}
