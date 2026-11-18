using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public float intensity = 8f;
    [Range(0f, 1f)] public float variation = 0.2f;
    public float speed = 6f;

    private Light lightSource;
    private float seed;

    void Awake()
    {
        lightSource = GetComponent<Light>();
        seed = Random.value * 100f;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        lightSource.intensity = intensity * (1f + noise * variation);
    }
}
