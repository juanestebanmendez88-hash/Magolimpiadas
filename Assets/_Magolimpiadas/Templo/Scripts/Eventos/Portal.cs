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
        m.Place(destination.Exit);
        GameSound.Play(GameAudio.Sound.NebulaPower, 0.5f, 1.1f, null, 0.3f);
        if (WorldSwitcher.Instance != null && WorldSwitcher.Instance.player2D == m)
        {
            var follow = WorldSwitcher.Instance.camera2D != null ? WorldSwitcher.Instance.camera2D.GetComponent<FollowCamera2D>() : null;
            if (follow != null) follow.Center();
        }
    }
}
