using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Magolimpiadas/Catálogo de efectos", fileName = "EfectosMagicos")]
public class MagicEffects : ScriptableObject
{
    public enum Effect
    {
        FireImpact, IceImpact, PlantImpact, LightningImpact, NebulaImpact, NeutralImpact,
        OrangeSparks, VioletSparks, GreenSparks, BlueSparks, WhiteSparks, GoldenSparks,
        OrangeBurst, VioletBurst, GreenBurst, BlueBurst, WhiteBurst, GoldenBurst,
        GoldenFlash, WhiteFlash,
        StarHit, SacredHit, LoveHit, ElectricHit, Explosion,
        Smoke, Dust,
        MagicCircle, MagicCircle2, HealingCircle, IceCircle, ArcaneCircle,
        StarAura, LightningAura, LoveAura, HealingAura, PowerAura,
        Teleport, Fire, BluePortal, GreenPortal, RedPortal, YellowPortal,
    }

    [System.Serializable]
    public class Entrance
    {
        public Effect effect;
        public GameObject prefab;
        public float scale = 1f;
    }

    public List<Entrance> effects = new List<Entrance>();

    public enum Effect2D { Burst, Rays, BigBurst, Flash, Star, Whirlpool, Ring }

    public enum Color2D { Orange, Violet, Blue, Green, Brown, Gray, Pink, Red, Indigo }

    [System.Serializable]
    public class Entrance2D
    {
        public Effect2D effect;
        public Sprite[] frameSprites;
        public int columns = 8;
        public float fps = 24f;
    }

    public List<Entrance2D> effects2D = new List<Entrance2D>();

    public static GameObject Launch2D(Effect2D e, Color2D color, Vector3 position, float scale = 1f, int order = 500, int layer = -1,
                                      Transform parent = null, float speed = 1f, float rotation = 0f)
    {
        var c = Catalog;
        if (c == null) return null;
        Entrance2D entrance = null;
        foreach (var x in c.effects2D) if (x != null && x.effect == e && x.frameSprites != null && x.frameSprites.Length > 0) { entrance = x; break; }
        if (entrance == null) return null;
        int columns = Mathf.Max(1, entrance.columns);
        int rows = Mathf.Max(1, entrance.frameSprites.Length / columns);
        int row = Mathf.Clamp((int)color, 0, rows - 1);
        var frames = new Sprite[columns];
        for (int i = 0; i < columns; i++) frames[i] = entrance.frameSprites[Mathf.Min(row * columns + i, entrance.frameSprites.Length - 1)];
        var go = new GameObject("Efecto 2D " + e, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = Vector3.one * scale;
        if (layer >= 0) go.layer = layer;
        go.AddComponent<EffectAnimation2D>().Setup(frames, entrance.fps * speed, Color.white, order);
        return go;
    }

    public static Color2D MageColor(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Color2D.Orange;
            case "nebulosa": return Color2D.Violet;
            case "druida": return Color2D.Green;
            case "rayo": return Color2D.Blue;
        }
        return Color2D.Gray;
    }

    public static Color2D ColorOf(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return Color2D.Orange;
            case MagicElement.Nebula: return Color2D.Violet;
            case MagicElement.Plant: return Color2D.Green;
            case MagicElement.Lightning: return Color2D.Blue;
            case MagicElement.Ice: return Color2D.Gray;
        }
        return Color2D.Gray;
    }

    static MagicEffects catalog;
    static bool searched;

    public static MagicEffects Catalog
    {
        get
        {
            if (!searched) { catalog = Resources.Load<MagicEffects>("EfectosMagicos"); searched = true; }
            return catalog;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Restart() { catalog = null; searched = false; }

    Entrance Find(Effect e)
    {
        foreach (var x in effects) if (x != null && x.effect == e && x.prefab != null) return x;
        return null;
    }

    public static bool Has(Effect e) => Catalog != null && Catalog.Find(e) != null;

    public static GameObject Launch(Effect e, Vector3 position, float scale = 1f, int order = 500, int layer = -1,
                                    Quaternion? orientation = null, Transform parent = null, float health = 0f)
    {
        var c = Catalog;
        var entrance = c != null ? c.Find(e) : null;
        if (entrance == null) return null;
        return Launch(entrance.prefab, position, scale * entrance.scale, order, layer, orientation, parent, health);
    }

    public static GameObject Launch(GameObject prefab, Vector3 position, float scale = 1f, int order = 500, int layer = -1,
                                    Quaternion? orientation = null, Transform parent = null, float health = 0f)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, position, orientation ?? prefab.transform.rotation, parent);
        go.transform.localScale = prefab.transform.localScale * scale;
        float lasts = 0f;
        bool hasLoop = false;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            if (main.loop) hasLoop = true;
            else lasts = Mathf.Max(lasts, main.duration + main.startLifetime.constantMax);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r != null) r.sortingOrder = order + r.sortingOrder;
        }
        foreach (var tr in go.GetComponentsInChildren<TrailRenderer>(true)) tr.sortingOrder = order + tr.sortingOrder;
        if (layer >= 0) foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        if (health == 0f) health = hasLoop && lasts <= 0f ? 3f : Mathf.Max(1f, lasts + 0.5f);
        if (health > 0f) Destroy(go, health);
        return go;
    }

    public static Color MageThemeColor(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Weaknesses.ColorElemental;
            case "nebulosa": return Weaknesses.NebulaColor;
            case "druida": return Weaknesses.DruidColor;
            case "rayo": return Weaknesses.LightningColor;
        }
        return Color.white;
    }

    public static void Tint(GameObject effect, Color color)
    {
        if (effect == null) return;
        foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            var startColor = main.startColor;
            switch (startColor.mode)
            {
                case ParticleSystemGradientMode.Color:
                    startColor.color = TintColor(startColor.color, color);
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    startColor.colorMin = TintColor(startColor.colorMin, color);
                    startColor.colorMax = TintColor(startColor.colorMax, color);
                    break;
                case ParticleSystemGradientMode.Gradient:
                case ParticleSystemGradientMode.RandomColor:
                    startColor.gradient = TintGradient(startColor.gradient, color);
                    break;
                case ParticleSystemGradientMode.TwoGradients:
                    startColor.gradientMin = TintGradient(startColor.gradientMin, color);
                    startColor.gradientMax = TintGradient(startColor.gradientMax, color);
                    break;
            }
            main.startColor = startColor;
            var overLifetime = ps.colorOverLifetime;
            if (!overLifetime.enabled) continue;
            var lifetimeColor = overLifetime.color;
            if (lifetimeColor.mode == ParticleSystemGradientMode.Gradient) lifetimeColor.gradient = TintGradient(lifetimeColor.gradient, color);
            else if (lifetimeColor.mode == ParticleSystemGradientMode.Color) lifetimeColor.color = TintColor(lifetimeColor.color, color);
            overLifetime.color = lifetimeColor;
        }
    }

    static Color TintColor(Color original, Color mage)
    {
        const float minOriginalBrightness = 0.55f, minMageBrightness = 0.8f;
        Color.RGBToHSV(mage, out float hue, out float saturation, out float brightness);
        Color.RGBToHSV(original, out _, out _, out float originalBrightness);
        var tinted = Color.HSVToRGB(hue, saturation, Mathf.Max(originalBrightness, minOriginalBrightness) * Mathf.Max(brightness, minMageBrightness), true);
        tinted.a = original.a;
        return tinted;
    }

    static Gradient TintGradient(Gradient gradient, Color mage)
    {
        if (gradient == null) return null;
        var keys = gradient.colorKeys;
        for (int i = 0; i < keys.Length; i++) keys[i].color = TintColor(keys[i].color, mage);
        var tinted = new Gradient { mode = gradient.mode };
        tinted.SetKeys(keys, gradient.alphaKeys);
        return tinted;
    }

    public static void TurnOff(GameObject effect, float destroyAfter = 2f)
    {
        if (effect == null) return;
        foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(effect, destroyAfter);
    }

    public static Effect ImpactOf(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return Effect.FireImpact;
            case MagicElement.Ice: return Effect.IceImpact;
            case MagicElement.Plant: return Effect.PlantImpact;
            case MagicElement.Lightning: return Effect.LightningImpact;
            case MagicElement.Nebula: return Effect.NebulaImpact;
        }
        return Effect.NeutralImpact;
    }

    public static Effect BurstOf(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Effect.OrangeBurst;
            case "nebulosa": return Effect.VioletBurst;
            case "druida": return Effect.GreenBurst;
            case "rayo": return Effect.BlueBurst;
        }
        return Effect.WhiteBurst;
    }

    public static Effect SparksOf(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Effect.OrangeSparks;
            case "nebulosa": return Effect.VioletSparks;
            case "druida": return Effect.GreenSparks;
            case "rayo": return Effect.BlueSparks;
        }
        return Effect.WhiteSparks;
    }

    public static Effect AuraOf(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Effect.PowerAura;
            case "nebulosa": return Effect.LoveAura;
            case "druida": return Effect.HealingAura;
            case "rayo": return Effect.LightningAura;
        }
        return Effect.StarAura;
    }

    public static Effect MageImpact(string mageId)
    {
        switch (mageId)
        {
            case "elemental": return Effect.FireImpact;
            case "nebulosa": return Effect.NebulaImpact;
            case "druida": return Effect.PlantImpact;
            case "rayo": return Effect.LightningImpact;
        }
        return Effect.NeutralImpact;
    }
}
