using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class EffectsBuilder
{
    const string Folder = "Assets/_Magolimpiadas/Efectos/Resources";
    const string CatalogPath = Folder + "/EfectosMagicos.asset";

    static readonly (MagicEffects.Effect effect, string prefab, float scale)[] Table =
    {
        (MagicEffects.Effect.FireImpact, "Explosion", 0.7f),
        (MagicEffects.Effect.IceImpact, "Snow hit", 1f),
        (MagicEffects.Effect.PlantImpact, "Green hit", 1f),
        (MagicEffects.Effect.LightningImpact, "Electro hit", 1f),
        (MagicEffects.Effect.NebulaImpact, "Love hit", 1f),
        (MagicEffects.Effect.NeutralImpact, "Stones hit", 1f),
        (MagicEffects.Effect.OrangeSparks, "Sparks red", 1f),
        (MagicEffects.Effect.VioletSparks, "Sparks pink", 1f),
        (MagicEffects.Effect.GreenSparks, "Sparks green", 1f),
        (MagicEffects.Effect.BlueSparks, "Sparks blue", 1f),
        (MagicEffects.Effect.WhiteSparks, "Sparks white", 1f),
        (MagicEffects.Effect.GoldenSparks, "Sparks yellow", 1f),
        (MagicEffects.Effect.OrangeBurst, "Sparks explode red", 1f),
        (MagicEffects.Effect.VioletBurst, "Sparks explode pink", 1f),
        (MagicEffects.Effect.GreenBurst, "Sparks explode green", 1f),
        (MagicEffects.Effect.BlueBurst, "Sparks explode blue", 1f),
        (MagicEffects.Effect.WhiteBurst, "Sparks explode white", 1f),
        (MagicEffects.Effect.GoldenBurst, "Sparks explode yellow", 1f),
        (MagicEffects.Effect.GoldenFlash, "Sparks flashing yellow", 1f),
        (MagicEffects.Effect.WhiteFlash, "Sparks flashing white", 1f),
        (MagicEffects.Effect.StarHit, "Star hit", 1f),
        (MagicEffects.Effect.SacredHit, "Holy hit", 1f),
        (MagicEffects.Effect.LoveHit, "Love hit", 1f),
        (MagicEffects.Effect.ElectricHit, "Electro hit", 1f),
        (MagicEffects.Effect.Explosion, "Explosion", 1f),
        (MagicEffects.Effect.Smoke, "Smoke puff", 1f),
        (MagicEffects.Effect.Dust, "Dust puff", 1f),
        (MagicEffects.Effect.MagicCircle, "Magic circle", 1f),
        (MagicEffects.Effect.MagicCircle2, "Magic circle 2", 1f),
        (MagicEffects.Effect.HealingCircle, "Healing circle", 1f),
        (MagicEffects.Effect.IceCircle, "Freeze circle", 1f),
        (MagicEffects.Effect.ArcaneCircle, "FX_MagicCircle_Icearrow03", 1f),
        (MagicEffects.Effect.StarAura, "Star aura", 1f),
        (MagicEffects.Effect.LightningAura, "Lightning aura", 1f),
        (MagicEffects.Effect.LoveAura, "Love aura", 1f),
        (MagicEffects.Effect.HealingAura, "Healing", 1f),
        (MagicEffects.Effect.PowerAura, "Buff", 1f),
        (MagicEffects.Effect.Teleport, "Teleport", 1f),
        (MagicEffects.Effect.Fire, "FireSmall", 1f),
        (MagicEffects.Effect.BluePortal, "Portal blue", 1f),
        (MagicEffects.Effect.GreenPortal, "Portal green", 1f),
        (MagicEffects.Effect.RedPortal, "Portal red", 1f),
        (MagicEffects.Effect.YellowPortal, "Portal yellow", 1f),
    };

    [MenuItem("Magolimpiadas/Efectos/Preparar catálogo")]
    static void Menu() => Debug.Log("[Efectos] " + Prepare());

    public static string Prepare()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }
        var catalog = AssetDatabase.LoadAssetAtPath<MagicEffects>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MagicEffects>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var remaining = new List<string>();
        var newItems = new List<MagicEffects.Entrance>();
        foreach (var (effect, displayName, scale) in Table)
        {
            var prefab = FindPrefab(displayName);
            if (prefab == null) { remaining.Add(displayName); continue; }
            var before = catalog.effects.FirstOrDefault(e => e != null && e.effect == effect);
            newItems.Add(new MagicEffects.Entrance { effect = effect, prefab = prefab, scale = before != null ? before.scale : scale });
        }
        catalog.effects = newItems;
        catalog.effects2D = Import2D();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return newItems.Count + " efectos de partículas y " + catalog.effects2D.Count + " efectos 2D en el catálogo" +
               (remaining.Count > 0 ? "; no encontré: " + string.Join(", ", remaining) : "") + ".";
    }

    const string Sprites2D = "Assets/_Magolimpiadas/Efectos/Sprites2D";

    static readonly (MagicEffects.Effect2D effect, string file, float fps)[] Table2D =
    {
        (MagicEffects.Effect2D.Burst, "Efecto2D_Estallido_766.png", 20f),
        (MagicEffects.Effect2D.Rays, "Efecto2D_Rayos_1339.png", 24f),
        (MagicEffects.Effect2D.BigBurst, "Efecto2D_GranEstallido_1371.png", 20f),
        (MagicEffects.Effect2D.Flash, "Efecto2D_Destello_1637.png", 28f),
        (MagicEffects.Effect2D.Star, "Efecto2D_Estrella_1638.png", 24f),
        (MagicEffects.Effect2D.Whirlpool, "Efecto2D_Remolino_1575.png", 18f),
        (MagicEffects.Effect2D.Ring, "Efecto2D_Anillo_1114.png", 16f),
    };

    static List<MagicEffects.Entrance2D> Import2D()
    {
        var list = new List<MagicEffects.Entrance2D>();
        foreach (var (effect, file, fps) in Table2D)
        {
            string path = Sprites2D + "/" + file;
            var frameSprites = CutGrid(path, 64, out int columns);
            if (frameSprites.Length == 0) continue;
            list.Add(new MagicEffects.Entrance2D { effect = effect, frameSprites = frameSprites, columns = columns, fps = fps });
        }
        return list;
    }

    static Sprite[] CutGrid(string path, int side, out int columns)
    {
        columns = 0;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[Efectos] No encontré " + path); return new Sprite[0]; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = side;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
        ti.GetSourceTextureWidthAndHeight(out int W, out int H);
        columns = W / side;
        int rows = H / side;
        string b = Path.GetFileNameWithoutExtension(path);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        var names = new List<string>();
        var rects = new List<SpriteRect>();
        for (int f = 0; f < rows; f++)
            for (int c = 0; c < columns; c++)
            {
                string displayName = b + "_" + f + "_" + c;
                names.Add(displayName);
                rects.Add(new SpriteRect
                {
                    name = displayName, rect = new Rect(c * side, (rows - 1 - f) * side, side, side), alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f), spriteID = ids.TryGetValue(displayName, out var id) ? id : GUID.Generate(),
                });
            }
        dp.SetSpriteRects(rects.ToArray());
        var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
        var byName = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        return names.Select(n => byName.TryGetValue(n, out var s) ? s : null).Where(s => s != null).ToArray();
    }

    public static GameObject FindPrefab(string displayName)
    {
        var paths = AssetDatabase.FindAssets("t:Prefab " + displayName)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(r => Path.GetFileNameWithoutExtension(r) == displayName)
            .OrderBy(r => r.Contains("Paquetes externos") || r.Contains("Hovl") || r.Contains("portalefectos") || r.Contains("Eric VFX") ? 0 : 1)
            .ToList();
        return paths.Count > 0 ? AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]) : null;
    }
}
