using ArcaneGames.Network;
using UnityEngine;

public class IslandTrip : MonoBehaviour
{
    public IslandCinematic cinematic;

    void OnTriggerEnter(Collider other)
    {
        var playerMage = other.GetComponent<PlayerMage>();
        if (playerMage == null || !playerMage.enabled || cinematic == null) return;
        if (IslandArrivalSync.TryRequest(ChosenMage.Chosen)) return;
        cinematic.Begin(playerMage, transform.position);
    }
}
