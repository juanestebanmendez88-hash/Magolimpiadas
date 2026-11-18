using UnityEngine;
using UnityEngine.Events;

public abstract class LevelEvent : MonoBehaviour
{
    public UnityEvent onActivate;

    public bool Activated { get; private set; }
    public static event System.Action<LevelEvent> AnyActivated;

    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        AnyActivated?.Invoke(this);
        onActivate?.Invoke();
        OnActivate();
    }

    protected abstract void OnActivate();

    public static ParticleSystem Particles(Vector3 location, Color color, int amount, float size, Vector2 speed, float duration, Vector3 area)
    {
        var go = new GameObject("Particulas");
        go.transform.position = location;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = amount / Mathf.Max(0.1f, duration);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = area;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Sprites/Default"));
        r.sortingOrder = 30;
        ps.Play();
        Destroy(go, duration + 2f);
        return ps;
    }
}
