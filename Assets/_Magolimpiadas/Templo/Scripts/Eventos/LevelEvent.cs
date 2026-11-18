using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class LevelEvent : MonoBehaviour
{
    public enum ParticleShape { Snowflakes, Leaves, Fire, Lightning, Stars, Sparkles }

    const string ParticleFolder = "Particulas/";
    const float SpriteScale = 4f;
    const int SheetTiles = 2;
    const int SparkleTiles = 4;
    const string SpriteShader = "Sprites/Default";

    static readonly Dictionary<ParticleShape, Material> shapeMaterials = new Dictionary<ParticleShape, Material>();
    static readonly Color[] sparkleRows =
    {
        new Color(0.72f, 0.38f, 1f),
        new Color(0.3f, 0.75f, 1f),
        new Color(1f, 0.5f, 0.15f),
        new Color(0.55f, 0.9f, 0.2f),
    };

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
        var ps = CreateSystem(location, color, amount, size, speed, duration, area);
        ps.Play();
        return ps;
    }

    public static ParticleSystem Particles(ParticleShape shape, Vector3 location, int amount, float size, Vector2 speed, float duration, Vector3 area)
        => WithSprite(shape, location, amount, size, speed, duration, area, 0, Color.white);

    public static ParticleSystem Sparkles(Vector3 location, Color color, int amount, float size, Vector2 speed, float duration, Vector3 area)
    {
        int row = 0;
        float best = float.MaxValue;
        for (int i = 0; i < sparkleRows.Length; i++)
        {
            Color c = sparkleRows[i];
            float distance = (c.r - color.r) * (c.r - color.r) + (c.g - color.g) * (c.g - color.g) + (c.b - color.b) * (c.b - color.b);
            if (distance < best) { best = distance; row = i; }
        }
        return WithSprite(ParticleShape.Sparkles, location, amount, size, speed, duration, area, row, color);
    }

    static string ResourceName(ParticleShape shape)
    {
        switch (shape)
        {
            case ParticleShape.Snowflakes: return "copos";
            case ParticleShape.Leaves: return "hojas";
            case ParticleShape.Fire: return "fuego";
            case ParticleShape.Lightning: return "rayo";
            case ParticleShape.Stars: return "estrellas";
            default: return "chispitas";
        }
    }

    static Color FallbackColor(ParticleShape shape, Color sparkleColor)
    {
        switch (shape)
        {
            case ParticleShape.Snowflakes: return new Color(0.75f, 0.92f, 1f);
            case ParticleShape.Leaves: return new Color(0.45f, 0.9f, 0.3f);
            case ParticleShape.Fire: return new Color(1f, 0.55f, 0.15f);
            case ParticleShape.Stars: return new Color(1f, 0.85f, 0.3f);
            case ParticleShape.Sparkles: return sparkleColor;
            default: return new Color(0.6f, 0.85f, 1f);
        }
    }

    static float SizeFactor(ParticleShape shape)
    {
        switch (shape)
        {
            case ParticleShape.Lightning: return 1.6f;
            case ParticleShape.Sparkles: return 2.2f;
            case ParticleShape.Stars: return 1.7f;
            default: return 1f;
        }
    }

    static Material MaterialFor(ParticleShape shape)
    {
        if (shapeMaterials.TryGetValue(shape, out var material) && material != null) return material;
        var texture = Resources.Load<Texture2D>(ParticleFolder + ResourceName(shape));
        if (texture == null) return null;
        material = new Material(Shader.Find(SpriteShader)) { mainTexture = texture };
        shapeMaterials[shape] = material;
        return material;
    }

    static ParticleSystem WithSprite(ParticleShape shape, Vector3 location, int amount, float size, Vector2 speed, float duration, Vector3 area, int row, Color sparkleColor)
    {
        var material = MaterialFor(shape);
        if (material == null) return Particles(location, FallbackColor(shape, sparkleColor), amount, size, speed, duration, area);

        if (shape == ParticleShape.Sparkles) amount = Mathf.Max(2, amount / 3);
        var ps = CreateSystem(location, Color.white, amount, size * SpriteScale * SizeFactor(shape), speed, duration, area);
        var main = ps.main;
        ps.GetComponent<ParticleSystemRenderer>().material = material;

        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = SheetTiles;
        sheet.numTilesY = SheetTiles;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.cycleCount = 1;
        var playThrough = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 0.999f));
        if (shape == ParticleShape.Sparkles)
        {
            sheet.numTilesX = SparkleTiles;
            sheet.numTilesY = SparkleTiles;
            sheet.animation = ParticleSystemAnimationType.SingleRow;
            sheet.rowMode = ParticleSystemAnimationRowMode.Custom;
            sheet.rowIndex = row;
            sheet.frameOverTime = playThrough;
        }
        else if (shape == ParticleShape.Lightning) sheet.frameOverTime = playThrough;
        else sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);

        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;
        var rotation = ps.rotationOverLifetime;
        var sizeOverLifetime = ps.sizeOverLifetime;
        switch (shape)
        {
            case ParticleShape.Snowflakes:
                main.gravityModifier = 0.08f;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
                break;
            case ParticleShape.Leaves:
                main.gravityModifier = 0.15f;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                break;
            case ParticleShape.Fire:
                main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                main.gravityModifier = -0.25f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
                break;
            case ParticleShape.Stars:
                main.startRotation = 0f;
                main.gravityModifier = -0.05f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x * 0.5f, speed.y * 0.5f);
                sizeOverLifetime.enabled = true;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.2f, 1f), new Keyframe(0.7f, 0.85f), new Keyframe(1f, 0.2f)));
                break;
            case ParticleShape.Sparkles:
                main.startRotation = 0f;
                main.gravityModifier = 0f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x * 0.25f, speed.y * 0.25f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
                break;
            case ParticleShape.Lightning:
                main.gravityModifier = 0f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x * 0.3f, speed.y * 0.3f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                break;
        }
        ps.Play();
        return ps;
    }

    static ParticleSystem CreateSystem(Vector3 location, Color color, int amount, float size, Vector2 speed, float duration, Vector3 area)
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
        r.material = new Material(Shader.Find(SpriteShader));
        r.sortingOrder = 30;
        Destroy(go, duration + 2f);
        return ps;
    }
}
