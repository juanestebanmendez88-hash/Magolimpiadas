using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class ColorPortal : MonoBehaviour
{
    [ColorUsage(false)] public Color color = Color.white;

    [Range(0f, 4f)] public float intensity = 1f;

    public bool keepShades = false;

    public bool includeLights = true;

    public List<Renderer> exclude = new List<Renderer>();

    [SerializeField, HideInInspector] private Color colorOriginal = Color.white;
    [SerializeField, HideInInspector] private bool read;
    [SerializeField, HideInInspector] private List<OriginalParticles> particles = new List<OriginalParticles>();
    [SerializeField, HideInInspector] private List<OriginalLine> lines = new List<OriginalLine>();
    [SerializeField, HideInInspector] private List<OriginalLight> lights = new List<OriginalLight>();

    [Serializable]
    private class OriginalParticles
    {
        public ParticleSystem system;
        public ParticleSystem.MinMaxGradient start, health, speed, healthTrail, trailLength;
    }

    [Serializable]
    private class OriginalLine
    {
        public Renderer render;
        public Gradient gradient;
    }

    [Serializable]
    private class OriginalLight
    {
        public Light lightSource;
        public Color color;
    }

    private static MaterialPropertyBlock block;
    private static readonly List<Material> materials = new List<Material>();

    public Color ColorOriginal => colorOriginal;

    public void ChangeColor(Color isNew)
    {
        color = isNew;
        Apply();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnDisable()
    {
        Restore();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyDeferred;
        UnityEditor.EditorApplication.delayCall += ApplyDeferred;
    }

    void ApplyDeferred()
    {
        if (this != null && isActiveAndEnabled && gameObject.scene.IsValid()) Apply();
    }
#endif

    [ContextMenu("Volver al color original")]
    public void RestoreOriginalColor()
    {
        color = colorOriginal;
        intensity = 1f;
        Apply();
    }

    [ContextMenu("Volver a leer los colores originales")]
    public void ReadAgain()
    {
        Restore();
        particles.Clear();
        lines.Clear();
        lights.Clear();
        read = false;
        Read();
        Apply();
    }

    public void Apply()
    {
        Read();
        if (IsOriginal())
        {
            Restore();
            return;
        }

        foreach (var p in particles)
        {
            if (p.system == null || Excluded(p.system.GetComponent<ParticleSystemRenderer>())) continue;
            PutColors(p.system, Transform(p.start), Transform(p.health), Transform(p.speed),
                         Transform(p.healthTrail), Transform(p.trailLength));
        }

        foreach (var l in lines)
        {
            if (l.render == null || Excluded(l.render)) continue;
            PutGradient(l.render, Transform(l.gradient));
        }

        foreach (var l in lights)
        {
            if (l.lightSource == null) continue;
            l.lightSource.color = includeLights ? Transform(l.color) : l.color;
        }

        if (block == null) block = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.GetSharedMaterials(materials);
            for (int i = 0; i < materials.Count; i++)
            {
                var m = materials[i];
                if (m == null || Excluded(r))
                {
                    r.SetPropertyBlock(null, i);
                    continue;
                }
                block.Clear();
                foreach (int id in EffectColors(m)) block.SetColor(id, Transform(m.GetColor(id)));
                r.SetPropertyBlock(block.isEmpty ? null : block, i);
            }
        }
    }

    public void Restore()
    {
        foreach (var p in particles)
            if (p.system != null) PutColors(p.system, p.start, p.health, p.speed, p.healthTrail, p.trailLength);
        foreach (var l in lines)
            if (l.render != null) PutGradient(l.render, l.gradient);
        foreach (var l in lights)
            if (l.lightSource != null) l.lightSource.color = l.color;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.GetSharedMaterials(materials);
            for (int i = 0; i < materials.Count; i++) r.SetPropertyBlock(null, i);
        }
    }

    private void Read()
    {
        particles.RemoveAll(p => p == null || p.system == null);
        lines.RemoveAll(l => l == null || l.render == null);
        lights.RemoveAll(l => l == null || l.lightSource == null);

        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particles.Exists(p => p.system == ps)) continue;
            var trails = ps.trails;
            particles.Add(new OriginalParticles
            {
                system = ps,
                start = Copy(ps.main.startColor),
                health = Copy(ps.colorOverLifetime.color),
                speed = Copy(ps.colorBySpeed.color),
                healthTrail = Copy(trails.colorOverLifetime),
                trailLength = Copy(trails.colorOverTrail),
            });
        }
        foreach (var r in GetComponentsInChildren<TrailRenderer>(true))
            if (!lines.Exists(l => l.render == r)) lines.Add(new OriginalLine { render = r, gradient = Copy(r.colorGradient) });
        foreach (var r in GetComponentsInChildren<LineRenderer>(true))
            if (!lines.Exists(l => l.render == r)) lines.Add(new OriginalLine { render = r, gradient = Copy(r.colorGradient) });
        foreach (var lightSource in GetComponentsInChildren<Light>(true))
            if (!lights.Exists(l => l.lightSource == lightSource)) lights.Add(new OriginalLight { lightSource = lightSource, color = lightSource.color });

        if (!read)
        {
            colorOriginal = CalculateOriginalTone();
            color = colorOriginal;
            intensity = 1f;
            read = true;
        }
    }

    private Color CalculateOriginalTone()
    {
        float x = 0f, y = 0f;
        void Add(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float weight = s * Mathf.Min(v, 1f) * Mathf.Max(c.a, 0.1f);
            x += Mathf.Cos(h * 2f * Mathf.PI) * weight;
            y += Mathf.Sin(h * 2f * Mathf.PI) * weight;
        }
        void AddGradient(Gradient g)
        {
            if (g == null) return;
            foreach (var k in g.colorKeys) Add(k.color);
        }
        void AddMinMax(ParticleSystem.MinMaxGradient g)
        {
            Add(g.colorMin); Add(g.colorMax); Add(g.color);
            AddGradient(g.gradientMin); AddGradient(g.gradientMax); AddGradient(g.gradient);
        }

        foreach (var p in particles)
        {
            AddMinMax(p.start);
            if (p.system != null && p.system.colorOverLifetime.enabled) AddMinMax(p.health);
        }
        foreach (var l in lines) AddGradient(l.gradient);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (Excluded(r)) continue;
            r.GetSharedMaterials(materials);
            foreach (var m in materials)
            {
                if (m == null) continue;
                bool twoColors = !m.HasFloat("_DualColorsState") || m.GetFloat("_DualColorsState") > 0.5f;
                foreach (int id in EffectColors(m))
                {
                    if (!twoColors && (id == IdColorA || id == IdColorB)) continue;
                    Add(m.GetColor(id));
                }
            }
        }

        if (x * x + y * y < 1e-6f) return Color.white;
        float tone = Mathf.Repeat(Mathf.Atan2(y, x) / (2f * Mathf.PI), 1f);
        return Color.HSVToRGB(tone, 1f, 1f);
    }

    private static readonly int IdColorA = Shader.PropertyToID("_ColorA");
    private static readonly int IdColorB = Shader.PropertyToID("_ColorB");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private static readonly int IdBaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly List<int> ids = new List<int>();

    private static List<int> EffectColors(Material m)
    {
        ids.Clear();
        var shader = m.shader;
        bool surface = shader.name.Contains("Lit") && !shader.name.Contains("Unlit")
                          && m.HasTexture(IdBaseMap) && m.GetTexture(IdBaseMap) != null;
        if (surface)
        {
            if (m.IsKeywordEnabled("_EMISSION") && m.HasColor(EmissionId)) ids.Add(EmissionId);
            return ids;
        }
        int amount = shader.GetPropertyCount();
        for (int k = 0; k < amount; k++)
        {
            if (shader.GetPropertyType(k) != ShaderPropertyType.Color) continue;
            int id = shader.GetPropertyNameId(k);
            if (m.HasColor(id)) ids.Add(id);
        }
        return ids;
    }

    private bool IsOriginal()
    {
        return Mathf.Abs(color.r - colorOriginal.r) < 0.002f && Mathf.Abs(color.g - colorOriginal.g) < 0.002f
            && Mathf.Abs(color.b - colorOriginal.b) < 0.002f && Mathf.Abs(intensity - 1f) < 0.001f;
    }

    private bool Excluded(Renderer r) => r != null && exclude != null && exclude.Contains(r);

    private Color Transform(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        Color.RGBToHSV(color, out float hChosen, out float sChosen, out float vChosen);
        if (keepShades)
        {
            Color.RGBToHSV(colorOriginal, out float hOriginal, out _, out _);
            h = Mathf.Repeat(h + hChosen - hOriginal, 1f);
            s *= sChosen;
        }
        else
        {
            h = hChosen;
            s = sChosen;
        }
        var isNew = Color.HSVToRGB(h, s, v * vChosen * intensity, true);
        isNew.a = c.a;
        return isNew;
    }

    private Gradient Transform(Gradient g) => Map(g, Transform);

    private ParticleSystem.MinMaxGradient Transform(ParticleSystem.MinMaxGradient g) => Map(g, Transform);

    private static Gradient Copy(Gradient g) => Map(g, c => c);

    private static ParticleSystem.MinMaxGradient Copy(ParticleSystem.MinMaxGradient g) => Map(g, c => c);

    private static Gradient Map(Gradient g, Func<Color, Color> f)
    {
        if (g == null) return null;
        var keys = g.colorKeys;
        for (int i = 0; i < keys.Length; i++) keys[i].color = f(keys[i].color);
        var isNew = new Gradient { mode = g.mode, colorSpace = g.colorSpace };
        isNew.SetKeys(keys, g.alphaKeys);
        return isNew;
    }

    private static ParticleSystem.MinMaxGradient Map(ParticleSystem.MinMaxGradient g, Func<Color, Color> f)
    {
        switch (g.mode)
        {
            case ParticleSystemGradientMode.TwoColors:
                return new ParticleSystem.MinMaxGradient(f(g.colorMin), f(g.colorMax));
            case ParticleSystemGradientMode.Gradient:
                return new ParticleSystem.MinMaxGradient(Map(g.gradient, f));
            case ParticleSystemGradientMode.TwoGradients:
                return new ParticleSystem.MinMaxGradient(Map(g.gradientMin, f), Map(g.gradientMax, f));
            case ParticleSystemGradientMode.RandomColor:
                var random = new ParticleSystem.MinMaxGradient(Map(g.gradient, f));
                random.mode = ParticleSystemGradientMode.RandomColor;
                return random;
            default:
                return new ParticleSystem.MinMaxGradient(f(g.color));
        }
    }

    private static void PutColors(ParticleSystem ps, ParticleSystem.MinMaxGradient start, ParticleSystem.MinMaxGradient health,
                                     ParticleSystem.MinMaxGradient speed, ParticleSystem.MinMaxGradient healthTrail,
                                     ParticleSystem.MinMaxGradient trailLength)
    {
        var principal = ps.main;
        principal.startColor = start;
        var healthColor = ps.colorOverLifetime;
        healthColor.color = health;
        var speedColor = ps.colorBySpeed;
        speedColor.color = speed;
        var trails = ps.trails;
        trails.colorOverLifetime = healthTrail;
        trails.colorOverTrail = trailLength;
    }

    private static void PutGradient(Renderer r, Gradient g)
    {
        if (g == null) return;
        if (r is TrailRenderer trail) trail.colorGradient = g;
        else if (r is LineRenderer line) line.colorGradient = g;
    }
}
