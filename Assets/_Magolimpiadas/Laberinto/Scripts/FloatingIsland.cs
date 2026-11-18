using System.Collections.Generic;
using UnityEngine;

public class FloatingIsland : MonoBehaviour
{
    public float height = 0.15f;
    public float period = 4f;
    [Range(0f, 1f)] public float offset;

    public List<Transform> companions = new List<Transform>();

    private Vector3 start;
    private float previousHeight;
    private PlayerMage playerMage;
    private CharacterController control;

    void Start()
    {
        start = transform.position;
        playerMage = FindAnyObjectByType<PlayerMage>();
        if (playerMage != null) control = playerMage.GetComponent<CharacterController>();
    }

    void Update()
    {
        float height = Mathf.Sin((Time.time / period + offset) * 2f * Mathf.PI) * this.height;
        Vector3 delta = Vector3.up * (height - previousHeight);
        previousHeight = height;

        transform.position = start + Vector3.up * height;
        foreach (var companion in companions)
            if (companion != null) companion.position += delta;

        if (playerMage != null && playerMage.enabled && control.enabled && IsAbove()) playerMage.Push(delta);
    }

    bool IsAbove()
    {
        Vector3 feet = playerMage.transform.position + Vector3.up * 0.3f;
        if (!Physics.Raycast(feet, Vector3.down, out RaycastHit hit, 0.7f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (hit.transform.IsChildOf(transform)) return true;
        foreach (var companion in companions)
            if (companion != null && hit.transform.IsChildOf(companion)) return true;
        return false;
    }
}
