using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TestTeleport : MonoBehaviour
{
    public Transform destination;
    public bool skipCinematic = true;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerMage>();
        if (player == null || !player.enabled || destination == null) return;

        Vector3 front = destination.forward; front.y = 0f;
        if (front.sqrMagnitude > 0.001f) player.transform.rotation = Quaternion.LookRotation(front);
        player.Teleport(destination.position);
        player.SetRespawnOutside(destination.position);
        GameSound.Play(GameAudio.Sound.NebulaPower, 0.6f);

        if (!skipCinematic) return;
        player.DropTorch();
        foreach (var bot in FindObjectsByType<BotMage3D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            bot.DropTorch();
            if (bot.leader == null) bot.leader = player;
            bot.state = BotMage3D.State.Following;
            bot.ShowBody(true);
            bot.AppearNextToLeader();
        }
    }
}
