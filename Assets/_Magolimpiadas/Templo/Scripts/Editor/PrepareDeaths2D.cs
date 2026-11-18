using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class PrepareMages2D
{
    static readonly Dictionary<string, string> Deaths = new Dictionary<string, string>
    {
        { "dragon_de_hielo", "dragon_hielo_muere" },
        { "espiritu_de_planta", "espiritu_planta_muere" },
        { "pocion_carmesi", "pocion_carmesi_muere" },
        { "golem_de_lava", "golem_lava_muere" },
        { "espectro_carmesi", "espectro_carmesi_muere" },
    };

    [MenuItem("Magolimpiadas/Templo 2D/Animaciones de muerte de los jefes")]
    static void DeathsMenu()
    {
        int n = ApplyDeaths();
        var map = GameObject.Find(MapName);
        if (map != null) EditorSceneManager.MarkSceneDirty(map.scene);
        Debug.Log("[Templo] Animaciones de muerte puestas en " + n + " enemigos. Guarda la escena (Ctrl+S).");
    }

    public static int ApplyDeaths()
    {
        var map = GameObject.Find(MapName);
        var characters = map != null ? map.transform.Find("Personajes") : null;
        if (characters == null) { Debug.LogWarning("[Templo] No encontré \"Personajes\" dentro del mapa."); return 0; }

        var sheets = new Dictionary<string, Sprite[]>();
        Sprite[] bullseye = null, breaks = null;
        int n = 0;
        foreach (var e in characters.GetComponentsInChildren<Enemy>(true))
        {
            var anim = e.GetComponent<AnimatedSprite>();
            if (anim == null || anim.frames == null || anim.frames.Length == 0) continue;
            string sheet = System.Text.RegularExpressions.Regex.Replace(anim.frames[0].name, @"_\d+$", "");

            if (sheet == "cristal_del_rayo")
            {
                if (!sheets.TryGetValue("cristal", out var crystalFrames))
                    sheets["cristal"] = crystalFrames = CutAtGaps(Root + "/personajes/enemigos/muertes/cristal_rayo_muere.png", anim.frames[0]);
                Undo.RecordObject(e, "Animaciones de muerte");
                e.deathFrames = crystalFrames;
                e.deathFps = 10f;
                n++;
            }
            else if (Deaths.TryGetValue(sheet, out var death))
            {
                if (!sheets.TryGetValue(death, out var frames))
                    sheets[death] = frames = Cut(Root + "/personajes/enemigos/muertes/" + death + ".png", 6, CharactersPpu, -1f);
                Undo.RecordObject(e, "Animaciones de muerte");
                e.deathFrames = frames;
                e.deathFps = 10f;
                n++;
            }
            else if (sheet == "golem_de_roca")
            {
                if (bullseye == null)
                {
                    bullseye = Cut(Root + "/personajes/enemigos/diana_de_madera.png", 6, CharactersPpu, -1f);
                    breaks = Cut(Root + "/personajes/enemigos/diana_de_madera_se_rompe.png", 6, CharactersPpu, -1f);
                }
                Undo.RecordObject(e, "Golem de roca diana");
                e.becomesTarget = true;
                e.bullseyeFrames = bullseye;
                e.targetBreakFrames = breaks;
                n++;
            }
        }
        return n;
    }

    static Sprite[] CutAtGaps(string path, Sprite reference)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("No encontré " + path); return new Sprite[0]; }
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        int W = tex.width, H = tex.height;
        var px = tex.GetPixels32();
        Object.DestroyImmediate(tex);

        var col = new bool[W];
        int ground = H;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                if (px[y * W + x].a > 30) { col[x] = true; if (y < ground) ground = y; }
        var sections = new List<RectInt>();
        int ini = -1;
        for (int x = 0; x <= W; x++)
        {
            bool c = x < W && col[x];
            if (c && ini < 0) ini = x;
            if (!c && ini >= 0) { sections.Add(new RectInt(ini, 0, x - ini, H)); ini = -1; }
        }
        if (sections.Count == 0) return new Sprite[0];

        int a = H, b = -1;
        for (int x = sections[0].xMin; x < sections[0].xMax; x++)
            for (int y = 0; y < H; y++)
                if (px[y * W + x].a > 30) { a = Mathf.Min(a, y); b = Mathf.Max(b, y); }
        float deathHeight = Mathf.Max(1, b - a + 1);
        float idleHeight = DrawnHeight(reference);
        float ppu = reference.pixelsPerUnit * deathHeight / Mathf.Max(1f, idleHeight);

        int width = Mathf.Min(W, sections.Max(t => t.width) + 8);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 4096;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var ids = dp.GetSpriteRects().GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < sections.Count; i++)
        {
            float center = sections[i].xMin + sections[i].width * 0.5f;
            float x0 = Mathf.Clamp(Mathf.Round(center - width * 0.5f), 0, W - width);
            string displayName = baseName + "_" + i;
            rects.Add(new SpriteRect
            {
                name = displayName,
                rect = new Rect(x0, 0, width, H),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((center - x0) / width, Mathf.Max(0, ground - 2) / (float)H),
                spriteID = ids.TryGetValue(displayName, out var id) ? id : GUID.Generate(),
            });
        }
        dp.SetSpriteRects(rects.ToArray());
        var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
    }

    static float DrawnHeight(Sprite s)
    {
        string path = AssetDatabase.GetAssetPath(s);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        var px = tex.GetPixels32();
        int W = tex.width;
        var r = s.rect;
        int a = int.MaxValue, b = -1;
        for (int x = (int)r.xMin; x < (int)r.xMax; x++)
            for (int y = (int)r.yMin; y < (int)r.yMax; y++)
                if (px[y * W + x].a > 30) { a = Mathf.Min(a, y); b = Mathf.Max(b, y); }
        Object.DestroyImmediate(tex);
        return b >= a ? b - a + 1 : r.height;
    }
}
