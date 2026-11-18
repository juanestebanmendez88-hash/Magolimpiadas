using System.Collections;
using UnityEngine;

public class LightChainEvent : LevelEvent
{
    public SpriteRenderer crystal, mirror, receptor;
    public Sprite[] crystalStates, mirrorStates, receptorStates;
    public Elevator elevator;
    public Color lightningColor = new Color(0.55f, 0.85f, 1f);
    public float secondsPerSection = 0.4f;

    LineRenderer lightning;
    Vector3[] points;

    void Awake()
    {
        State(crystal, crystalStates, 0);
        State(mirror, mirrorStates, 0);
        State(receptor, receptorStates, 0);
    }

    static void State(SpriteRenderer sr, Sprite[] states, int i)
    {
        if (sr == null || states == null || states.Length <= i) return;
        var a = sr.GetComponent<AnimatedSprite>(); if (a != null) a.enabled = false;
        sr.sprite = states[i];
    }

    protected override void OnActivate() { StartCoroutine(Ignite()); }

    IEnumerator Ignite()
    {
        points = new[] { crystal.bounds.center, mirror.bounds.center, receptor.bounds.center };
        var go = new GameObject("Rayo de luz");
        go.transform.SetParent(transform, false);
        lightning = go.AddComponent<LineRenderer>();
        lightning.material = new Material(Shader.Find("Sprites/Default"));
        lightning.startColor = lightning.endColor = lightningColor;
        lightning.widthMultiplier = 0.25f;
        lightning.sortingOrder = 24;
        lightning.numCapVertices = 4;
        lightning.positionCount = 2;
        lightning.SetPosition(0, points[0]); lightning.SetPosition(1, points[0]);
        State(crystal, crystalStates, 1);

        for (int section = 0; section < 2; section++)
        {
            Vector3 a = points[section], b = points[section + 1];
            lightning.positionCount = section + 2;
            for (float t = 0f; t < 1f; t += Time.deltaTime / secondsPerSection)
            {
                lightning.SetPosition(section + 1, Vector3.Lerp(a, b, t));
                yield return null;
            }
            lightning.SetPosition(section + 1, b);
            State(section == 0 ? mirror : receptor, section == 0 ? mirrorStates : receptorStates, 1);
            Particles(b, lightningColor, 15, 0.2f, new Vector2(1f, 3f), 0.2f, Vector3.one * 0.5f);
        }
        if (elevator != null) elevator.Ignite();
        Notices.Tip("elevador", "¡El elevador se encendió! Súbete a la isla flotante");
    }

    void Update()
    {
        if (lightning != null) lightning.widthMultiplier = 0.2f + Mathf.PingPong(Time.time * 0.6f, 0.12f);
    }
}
