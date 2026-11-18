using System.Collections.Generic;
using UnityEngine;

public class SafeZone : MonoBehaviour
{
    private static readonly List<SafeZone> zones = new List<SafeZone>();

    private Collider zone;

    void OnEnable()
    {
        zone = GetComponent<Collider>();
        zones.Add(this);
    }

    void OnDisable()
    {
        zones.Remove(this);
    }

    public static bool Contains(Vector3 point)
    {
        foreach (SafeZone safe in zones)
        {
            if (safe.zone.bounds.Contains(point)) return true;
        }
        return false;
    }
}
