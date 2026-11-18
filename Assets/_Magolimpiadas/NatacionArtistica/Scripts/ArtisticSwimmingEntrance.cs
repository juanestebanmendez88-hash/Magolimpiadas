using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ArtisticSwimmingEntrance : MonoBehaviour
{
    public ArtisticSwimmingLevel level;
    public SwimmingRace race;
    public Transform edge;
    [TextArea(2, 4)]
    public string notice = "¡Última prueba, {mago}! Vuelve a meterte en la piscina: el jurado de búhos espera la natación artística.";
    public float noticeSeconds = 7f;

    bool warned;

    void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

    bool Ready =>
        level != null && !ArtisticSwimmingLevel.Won && !level.Playing &&
        (race == null || (race.Finished && !race.InProgress)) &&
        (PodiumCeremony.Instance == null || !PodiumCeremony.Instance.InProgress);

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerMage>();
        if (player == null || !player.enabled || !Ready) return;
        var world = WorldSwitcher.Instance;
        if (world == null || world.Busy) return;
        if (ArcaneGames.Network.OnlineTrials.TryRequest(ArcaneGames.Network.OnlineTrial.ArtisticSwimming)) return;
        level.Enter();
    }

    void Update()
    {
        if (warned || !Ready || !OwlNotice.Free) return;
        var world = WorldSwitcher.Instance;
        if (world != null && world.Busy) return;
        warned = true;
        StartCoroutine(OwlNotice.Show(this, notice, noticeSeconds));
    }

    public void ReturnToEdge()
    {
        var player = FindFirstObjectByType<PlayerMage>();
        if (player == null || edge == null) return;
        Vector3 front = edge.forward; front.y = 0f;
        if (front.sqrMagnitude > 0.001f) player.transform.rotation = Quaternion.LookRotation(front);
        player.Teleport(edge.position);
        player.SetRespawnOutside(edge.position);
        foreach (var bot in BotMage3D.All)
            if (bot != null && bot.state == BotMage3D.State.Following) bot.AppearNextToLeader();
    }
}
