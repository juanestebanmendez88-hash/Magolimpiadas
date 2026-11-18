using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ElementalCircle : MonoBehaviour
{
    public MagicElement element;
    public SpriteRenderer glow;
    [HideInInspector] public ElementalDoor door;

    public bool Lit { get; private set; }
    public static event System.Action<ElementalCircle> AnyLit;

    public void LightFromNetwork()
    {
        if (Lit) return;
        Lit = true;
        if (glow != null) glow.enabled = true;
        if (door != null) door.LitCircle();
    }

    void Awake()
    {
        if (glow != null) glow.enabled = false;
    }

    public bool ReceiveArrow(Mage shooter)
    {
        if (Lit) return true;
        if (shooter == null || shooter.data.element != element)
        {
            string who = MageName(element);
            DamageNumbers.Message(transform.position + Vector3.up * 0.8f, "Este círculo es " + who, new Color(1f, 0.85f, 0.5f));
            return false;
        }
        Lit = true;
        AnyLit?.Invoke(this);
        if (glow != null) glow.enabled = true;
        LevelEvent.Sparkles(transform.position, shooter.data.color, 20, 0.25f, new Vector2(1f, 3f), 0.3f, Vector3.one * 0.5f);
        if (door != null) door.LitCircle();
        return true;
    }

    public void TurnOff()
    {
        if (glow != null) glow.enabled = false;
        var c = GetComponent<Collider2D>(); if (c != null) c.enabled = false;
    }

    void Update()
    {
        if (Lit && glow != null && glow.enabled)
        {
            float k = 0.75f + Mathf.PingPong(Time.time * 1.5f, 0.25f);
            glow.transform.localScale = Vector3.one * k * 1.4f;
        }
    }

    public static string MageName(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return "del Mago Elemental";
            case MagicElement.Nebula: return "de la Maga Nebulosa";
            case MagicElement.Plant: return "de la Druida Verde";
            case MagicElement.Lightning: return "del Mago del Rayo";
        }
        return "de otro mago";
    }
}
