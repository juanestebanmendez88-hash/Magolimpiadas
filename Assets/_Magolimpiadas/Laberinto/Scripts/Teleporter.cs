using System.Collections;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public Teleporter[] destinations;
    public float wait = 0.75f;

    public GameObject teleportEffect;
    public float effectDuration = 0.9f;

    private static float nextUse;

    void OnTriggerEnter(Collider other)
    {
        PlayerMage playerMage = other.GetComponent<PlayerMage>();
        if (playerMage == null || destinations.Length == 0 || Time.time < nextUse) return;

        nextUse = Time.time + wait;

        Teleporter destination = destinations[Random.Range(0, destinations.Length)];
        ShowEffect(playerMage.transform.position);
        playerMage.Teleport(destination.transform.position + Vector3.up * 0.1f);
        ShowEffect(destination.transform.position);
    }

    void ShowEffect(Vector3 place)
    {
        if (teleportEffect == null) return;

        place.y = transform.position.y;
        GameObject effect = Instantiate(teleportEffect, place, Quaternion.identity);
        StartCoroutine(TurnOff(effect));
    }

    IEnumerator TurnOff(GameObject effect)
    {
        yield return new WaitForSeconds(effectDuration);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        Destroy(effect, 1.5f);
    }
}
