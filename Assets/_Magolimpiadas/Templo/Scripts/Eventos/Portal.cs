using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Portal : MonoBehaviour
{
    public Portal destination;
    public Vector2 exit = new Vector2(2.6f, 0.05f);
    public SpriteRenderer drawing;
    public Sprite off;
    public Sprite[] activeAnimation;
    public float fps = 10f;
    public bool lit = false;

    public static readonly List<Portal> All = new List<Portal>();
    static readonly Dictionary<Mage, float> cooldownByMage = new Dictionary<Mage, float>();

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (drawing != null) { var a = drawing.GetComponent<AnimatedSprite>(); if (a != null) a.enabled = false; }
        if (!lit && drawing != null && off != null) drawing.sprite = off;
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public Vector2 Exit => (Vector2)transform.position + exit;

    public void Ignite()
    {
        lit = true;
        LevelEvent.Particles(transform.position + Vector3.up * 2f, new Color(0.75f, 0.45f, 1f), 30, 0.3f, new Vector2(1f, 3f), 0.5f, new Vector3(2f, 3f, 0f));
    }

    void Update()
    {
        if (lit && drawing != null && activeAnimation != null && activeAnimation.Length > 0)
            drawing.sprite = activeAnimation[(int)(Time.time * fps) % activeAnimation.Length];
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!lit || destination == null || !destination.lit) return;
        var m = other.GetComponentInParent<Mage>();
        if (m == null || !m.Alive) return;
        if (cooldownByMage.TryGetValue(m, out float t) && Time.time < t) return;
        cooldownByMage[m] = Time.time + 1f;
        LevelEvent.Particles(m.transform.position + Vector3.up, new Color(0.75f, 0.45f, 1f), 15, 0.25f, new Vector2(1f, 3f), 0.2f, Vector3.one);
        m.Place(destination.Exit);
        GameSound.Play(GameAudio.Sound.NebulaPower, 0.5f, 1.1f, null, 0.3f);
        if (WorldSwitcher.Instance != null && WorldSwitcher.Instance.player2D == m)
        {
            var follow = WorldSwitcher.Instance.camera2D != null ? WorldSwitcher.Instance.camera2D.GetComponent<FollowCamera2D>() : null;
            if (follow != null) follow.Center();
        }
    }
}
