using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static class PingPongBuilder
{
    const string Folder = "Assets/_Magolimpiadas/PingPong";
    const string Sprites = Folder + "/Sprites";
    const string RacketsFolder = Folder + "/Raquetas";
    const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
    const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";
    const string LevelName = "Nivel Ping Pong";
    static readonly Vector3 LevelPosition = new Vector3(2000f, 1000f, 0f);

    static readonly (string id, string folder, string displayName, Color color)[] Mages =
    {
        ("nebulosa", "maga_nebulosa", "Maga Nebulosa", new Color(0.72f, 0.38f, 1f)),
        ("rayo", "mago_rayo", "Mago del Rayo", new Color(0.32f, 0.62f, 1f)),
        ("elemental", "mago_elemental", "Mago Elemental", new Color(1f, 0.52f, 0.15f)),
        ("druida", "druida_verde", "Druida Verde", new Color(0.38f, 0.88f, 0.32f)),
    };

    static readonly Dictionary<string, string> RacketOf = new Dictionary<string, string>
    {
        { "nebulosa", "raquetaNebulosa" }, { "rayo", "raquetaRayo" }, { "elemental", "raquetaElemental" }, { "druida", "raquetaDruida" },
    };

    [MenuItem("Magolimpiadas/Ping Pong/Preparar nivel de ping pong")]
    static void Menu()
    {
        bool exists = FindRoot(LevelName) != null;
        bool redo = !exists || EditorUtility.DisplayDialog("Nivel de ping pong",
            "Ya existe el objeto \"" + LevelName + "\" (el coliseo 2D).\n\n¿Lo rehago? Se borra SOLO ese objeto y se crea de nuevo. " +
            "Las raquetas, la mesa, la cortina y el portal no se borran (solo se les ponen sus scripts).", "Rehacer el coliseo", "Dejarlo como está");
        Prepare(redo);
    }

    public static void Prepare(bool redoLevel)
    {
        CreateWhiteTrail();
        ImportSprites();
        var materials = CreateRacketsMaterials();
        PrepareItems3D(materials, out var table);
        PingPongLevel level = null;
        var root = FindRoot(LevelName);
        if (root != null && !redoLevel) level = root.GetComponent<PingPongLevel>();
        else
        {
            if (root != null) Undo.DestroyObjectImmediate(root);
            level = CreateLevel();
        }
        if (table != null && level != null) { Undo.RecordObject(table, "Mesa ping pong"); table.level = level; }
        if (level != null)
        {
            var tut = level.GetComponent<TutorialPingPong>();
            if (tut == null) tut = Undo.AddComponent<TutorialPingPong>(level.gameObject);
            Undo.RecordObject(tut, "Tutorial ping pong");
            tut.scroll = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Magolimpiadas/Arte/UI/FantasyUI/panels/panel_parchment.png");
            tut.titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
            tut.textFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);
            EditorUtility.SetDirty(tut);
        }
        CreateTitles();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Ping pong] Listo. Guarda la escena (Ctrl+S).");
    }

    public static void CreateTitles()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        CreateTitle("Título - Tiro con arco", "Tiro con arco", "Nivel 1", new Vector3(115.3f, 0f, 24.6f), new Vector3(2.4f, 8f, 12f), font);
        CreateTitle("Título - Ping pong", "Ping pong", "Nivel 2", new Vector3(69.7f, 0f, 31.5f), new Vector3(8f, 8f, 7f), font);
    }

    static void CreateTitle(string displayName, string title, string sub, Vector3 pos, Vector3 sizeValue, TMP_FontAsset font)
    {
        if (FindRoot(displayName) != null) return;
        var go = new GameObject(displayName);
        Undo.RegisterCreatedObjectUndo(go, "Título de zona");
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = sizeValue;
        var t = go.AddComponent<ZoneTitle>();
        t.title = title;
        t.subtitle = sub;
        t.font = font;
    }

    static GameObject FindRoot(string displayName) =>
        EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == displayName);

    static void CreateWhiteTrail()
    {
        string origin = Sprites + "/efectos/estelas/estela_simple_mago_rayo.png";
        string destination = Sprites + "/efectos/estelas/estela_simple_blanca.png";
        if (File.Exists(destination) || !File.Exists(origin)) return;
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(origin));
        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            float l = Mathf.Max(px[i].r, px[i].g, px[i].b);
            px[i] = new Color(Mathf.Lerp(l, 1f, 0.6f), Mathf.Lerp(l, 1f, 0.6f), Mathf.Lerp(l, 1f, 0.65f), px[i].a);
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(destination, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
    }

    static Material GrayMaterial()
    {
        const string path = Folder + "/Shaders/M_PingPongGris.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        var shader = Shader.Find("Magolimpiadas/SpriteGris");
        if (shader == null) { Debug.LogWarning("[Ping pong] No encontré el shader Magolimpiadas/SpriteGris."); return null; }
        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ImportSprites()
    {
        foreach (var path in Directory.GetFiles(Sprites, "*.png", SearchOption.AllDirectories).Select(r => r.Replace('\\', '/')))
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            string displayName = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 4096;
            ti.spritePixelsPerUnit = path.Contains("/fondo/") ? 30f : path.Contains("/mesa/") ? 52f : 64f;

            ti.GetSourceTextureWidthAndHeight(out int width, out int heightValue);
            bool sheet = width >= heightValue * 4 && (path.Contains("/personajes/") || path.Contains("/enemigos/") || path.Contains("/estelas/"));
            if (!sheet)
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
                continue;
            }

            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.SaveAndReimport();
            Vector2 pivotPoint = Pivot(displayName, heightValue);
            int cell = width / 6;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();
            var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
            var rects = Enumerable.Range(0, 6).Select(i =>
            {
                string n = displayName + "_" + i;
                return new SpriteRect
                {
                    name = n,
                    rect = new Rect(i * cell, 0, cell, heightValue),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivotPoint,
                    spriteID = ids.TryGetValue(n, out var id) ? id : GUID.Generate(),
                };
            }).ToArray();
            dp.SetSpriteRects(rects);
            var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
            dp.Apply();
            ti.SaveAndReimport();
        }
    }

    static Vector2 Pivot(string displayName, int heightValue)
    {
        if (displayName.StartsWith("estela")) return new Vector2(0.9375f, 0.5f);
        float px = 10f;
        if (displayName.StartsWith("guardian_idle")) px = 18f;
        else if (displayName.StartsWith("guardian_caminar")) px = 26f;
        return new Vector2(0.5f, px / heightValue);
    }

    static Sprite[] Sheet(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int i) ? i : 0).ToArray();
    }

    static SpriteAnimation Anim(string path, float fps, bool repeat = true) =>
        new SpriteAnimation { frames = Sheet(path), fps = fps, repeat = repeat };

    static Sprite One(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static Dictionary<string, Material> CreateRacketsMaterials()
    {
        var res = new Dictionary<string, Material>();
        var sample = GameObject.Find("raquetaNebulosa");
        var original = sample != null ? sample.GetComponent<MeshRenderer>()?.sharedMaterial : null;
        if (original != null && original.name.StartsWith("M_Raqueta_"))
            original = AssetDatabase.LoadAllAssetsAtPath("Assets/_Magolimpiadas/PingPong/Modelos/raqueta.glb").OfType<Material>().FirstOrDefault() ?? original;
        if (original == null) { Debug.LogWarning("[Ping pong] No encontré la raqueta 'raquetaNebulosa' para copiar su material."); return res; }

        string prop = original.HasProperty("baseColorTexture") ? "baseColorTexture" : original.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        var tex = original.GetTexture(prop);
        if (tex == null) { Debug.LogWarning("[Ping pong] El material de la raqueta no tiene textura."); return res; }
        if (!AssetDatabase.IsValidFolder(RacketsFolder)) AssetDatabase.CreateFolder(Folder, "Raquetas");

        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, rt);
        var before = RenderTexture.active;
        RenderTexture.active = rt;
        var baseTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        baseTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        baseTex.Apply();
        RenderTexture.active = before;
        RenderTexture.ReleaseTemporary(rt);
        var px = baseTex.GetPixels();

        foreach (var m in Mages)
        {
            var isNew = new Color[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                bool red = c.r > 0.12f && c.r > c.g * 2.2f && c.r > c.b * 2.2f;
                if (!red) { isNew[i] = c; continue; }
                float lightSource = Mathf.Clamp(c.r / 0.36f, 0.55f, 1.25f);
                var col = m.color * lightSource;
                col.a = c.a;
                isNew[i] = col;
            }
            var t = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            t.SetPixels(isNew);
            t.Apply();
            string texPath = RacketsFolder + "/T_Raqueta_" + m.id + ".png";
            File.WriteAllBytes(texPath, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);

            string matPath = RacketsFolder + "/M_Raqueta_" + m.id + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(original); AssetDatabase.CreateAsset(mat, matPath); }
            else mat.CopyPropertiesFromMaterial(original);
            mat.SetTexture(prop, AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            EditorUtility.SetDirty(mat);
            res[m.id] = mat;
        }
        Object.DestroyImmediate(baseTex);
        AssetDatabase.SaveAssets();
        return res;
    }

    static void PrepareItems3D(Dictionary<string, Material> materials, out PingPongTable table)
    {
        foreach (var par in RacketOf)
        {
            var go = GameObject.Find(par.Value);
            if (go == null) { Debug.LogWarning("[Ping pong] No encontré la raqueta \"" + par.Value + "\"."); continue; }
            var r = go.GetComponent<Racket>() ?? Undo.AddComponent<Racket>(go);
            Undo.RecordObject(r, "Raqueta");
            r.mageId = par.Key;
            r.rotationInHand = new Vector3(0f, 0f, 115f);
            r.positionInHand = new Vector3(0f, 0.04f, 0f);
            r.grip = 0.15f;
            if (materials.TryGetValue(par.Key, out var mat))
            {
                var mr = go.GetComponent<MeshRenderer>();
                Undo.RecordObject(mr, "Raqueta color");
                mr.sharedMaterial = mat;
            }
        }

        table = null;
        var tableObject = GameObject.Find("mesapingpong");
        if (tableObject != null) table = tableObject.GetComponent<PingPongTable>() ?? Undo.AddComponent<PingPongTable>(tableObject);
        else Debug.LogWarning("[Ping pong] No encontré la mesa \"mesapingpong\".");

        var block = GameObject.Find("BloqueoNivel1");
        if (block != null) { if (block.GetComponent<TrialGate>() == null) Undo.AddComponent<TrialGate>(block); }
        else Debug.LogWarning("[Ping pong] No encontré la cortina \"BloqueoNivel1\".");

        var portal = GameObject.Find("PortalNivel1");
        var point = GameObject.Find("nivel2");
        var entrance = portal != null ? portal.GetComponent<WorldEntrance2D>() : null;
        if (entrance != null && point != null)
        {
            Undo.RecordObject(entrance, "Punto al terminar");
            entrance.pointOnFinish = point.transform;
        }
        else Debug.LogWarning("[Ping pong] No encontré PortalNivel1 (con EntradaMundo2D) o el punto \"nivel2\".");
    }

    static PingPongLevel CreateLevel()
    {
        var root = new GameObject(LevelName);
        Undo.RegisterCreatedObjectUndo(root, "Nivel ping pong");
        root.transform.position = LevelPosition;
        var level = root.AddComponent<PingPongLevel>();

        var colosseum = Child("Coliseo", root.transform, Vector3.zero);
        level.content = colosseum.gameObject;

        Sr("Fondo", colosseum, Vector3.zero, One(Sprites + "/fondo/coliseo_fondo.png"), -100);
        Sr("Mesa", colosseum, new Vector3(0f, -2.19f, 0f), One(Sprites + "/mesa/mesa_sin_red.png"), 0);
        Sr("Red", colosseum, new Vector3(0f, -2.19f, 0f), One(Sprites + "/mesa/mesa_red.png"), 250);

        var magesGroup = Child("Magos", colosseum, Vector3.zero);
        foreach (var m in Mages)
        {
            string p = Sprites + "/personajes/" + m.folder + "/" + m.folder + "_paleta_";
            var token = new PingPongLevel.PingPongMage
            {
                id = m.id,
                displayName = m.displayName,
                color = m.color,
                idle = Anim(p + "idle_pies.png", 8f),
                walk = Anim(p + "caminar_pies.png", 10f),
                run = Anim(p + "correr_pies.png", 12f),
                hit = Anim(p + "golpe_lado_pies.png", 12f, false),
                ballSprite = One(Sprites + "/efectos/bolas/bola_" + m.folder + ".png"),
                ballFill = Fill(Sprites + "/efectos/bolas/bola_" + m.folder + ".png"),
                trail = Anim(Sprites + "/efectos/estelas/estela_simple_" + m.folder + ".png", 12f),
                powerTrail = Anim(Sprites + "/efectos/estelas/estela_magica_" + m.folder + ".png", 12f),
            };
            token.body = Sr(m.displayName, magesGroup, Vector3.zero, token.idle.frames.FirstOrDefault(), 100);
            level.mages.Add(token);
        }

        string g = Sprites + "/enemigos/guardian/guardian_";
        level.guardianIdle = Anim(g + "idle_lado_pies.png", 8f);
        level.guardianWalk = Anim(g + "caminar_lado_pies.png", 10f);
        level.guardianHit = Anim(g + "golpe_lado_pies.png", 12f, false);
        level.guardianThrow = Anim(g + "lanzar_esfera_lado_pies.png", 12f, false);
        level.guardian = Sr("Guardián del Olimpo", colosseum, Vector3.zero, level.guardianIdle.frames.FirstOrDefault(), 100);
        level.whiteTrail = Anim(Sprites + "/efectos/estelas/estela_simple_blanca.png", 12f);
        level.grayMaterial = GrayMaterial();

        level.shadow = Sr("Sombra de la bola", colosseum, Vector3.zero, null, 2);
        level.lookDirection = Sr("Mira", colosseum, Vector3.zero, null, 3);
        level.trail = Sr("Estela", colosseum, Vector3.zero, null, 299);
        level.ballSprite = Sr("Bola", colosseum, Vector3.zero, One(Sprites + "/efectos/bolas/bola_maga_nebulosa.png"), 300);

        var cam = Child("Camara Ping Pong", root.transform, new Vector3(0f, 0f, -10f)).gameObject.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 15.55f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.04f, 0.14f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.depth = 10;
        cam.enabled = false;
        level.sceneCamera = cam;

        CreateHud(level, root.transform);
        return level;
    }

    static float Fill(string path)
    {
        var tex = new Texture2D(2, 2);
        if (!File.Exists(path) || !tex.LoadImage(File.ReadAllBytes(path))) { Object.DestroyImmediate(tex); return 1f; }
        int min = tex.width, max = -1;
        var px = tex.GetPixels32();
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                if (px[y * tex.width + x].a > 40) { if (x < min) min = x; if (x > max) max = x; }
        float r = max >= min ? (max - min + 1f) / tex.width : 1f;
        Object.DestroyImmediate(tex);
        return r;
    }

    static void CreateHud(PingPongLevel level, Transform parent)
    {
        var fTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        var fText = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);

        var hud = new GameObject("HUD Ping Pong", typeof(RectTransform));
        hud.transform.SetParent(parent, false);
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var esc = hud.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;
        level.hud = hud;

        var filmStrip = new GameObject("Película inferior", typeof(RectTransform), typeof(Image));
        filmStrip.transform.SetParent(hud.transform, false);
        var ip = filmStrip.GetComponent<Image>();
        ip.raycastTarget = false;
        ip.color = Color.white;
        var filmRect = (RectTransform)filmStrip.transform;
        filmRect.anchorMin = new Vector2(0f, 0f); filmRect.anchorMax = new Vector2(1f, 0f); filmRect.pivot = new Vector2(0.5f, 0f);
        filmRect.anchoredPosition = Vector2.zero;
        filmRect.sizeDelta = new Vector2(0f, 300f);
        level.film = ip;

        var panel = new GameObject("Panel marcador", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(hud.transform, false);
        var img = panel.GetComponent<Image>();
        img.color = new Color(0.04f, 0.02f, 0.12f, 0.75f);
        img.raycastTarget = false;
        var rp = (RectTransform)panel.transform;
        rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0.5f, 0f);
        rp.anchoredPosition = new Vector2(0f, 56f);
        rp.sizeDelta = new Vector2(720f, 76f);
        level.scoreboard = Txt("Marcador", panel.transform, fTitle, 46f, Color.white, "Magos 0 · 0 Guardián", TextAlignmentOptions.Center);
        Stretch(level.scoreboard.rectTransform);

        level.help = Txt("Ayuda", hud.transform, fText, 24f, new Color(1f, 1f, 1f, 0.85f), "", TextAlignmentOptions.Bottom);
        var ra = level.help.rectTransform;
        ra.anchorMin = new Vector2(0f, 0f); ra.anchorMax = new Vector2(1f, 0f); ra.pivot = new Vector2(0.5f, 0f);
        ra.anchoredPosition = new Vector2(0f, 14f);
        ra.sizeDelta = new Vector2(-40f, 36f);

        var background = new GameObject("Fondo mensaje", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(hud.transform, false);
        var fi = background.GetComponent<Image>();
        fi.color = new Color(0.03f, 0.01f, 0.1f, 0.6f);
        fi.raycastTarget = false;
        var rf = (RectTransform)background.transform;
        rf.anchorMin = new Vector2(0f, 0.5f); rf.anchorMax = new Vector2(1f, 0.5f); rf.pivot = new Vector2(0.5f, 0.5f);
        rf.anchoredPosition = new Vector2(0f, 60f);
        rf.sizeDelta = new Vector2(0f, 240f);
        background.SetActive(false);
        level.messageBackground = background;

        level.message = Txt("Mensaje", hud.transform, fTitle, 96f, new Color(1f, 0.9f, 0.55f), "", TextAlignmentOptions.Center);
        var rm = level.message.rectTransform;
        rm.anchorMin = rm.anchorMax = rm.pivot = new Vector2(0.5f, 0.5f);
        rm.anchoredPosition = new Vector2(0f, 60f);
        rm.sizeDelta = new Vector2(1500f, 300f);
        level.message.outlineWidth = 0.25f;
        level.message.outlineColor = new Color32(30, 10, 60, 255);
        var frame = new GameObject("Barra de poder", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(hud.transform, false);
        var im = frame.GetComponent<Image>();
        im.color = new Color(0.04f, 0.02f, 0.12f, 0.8f);
        im.raycastTarget = false;
        var frameRect = (RectTransform)frame.transform;
        frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0.5f, 0f);
        frameRect.anchoredPosition = new Vector2(0f, 140f);
        frameRect.sizeDelta = new Vector2(560f, 34f);
        var fill = new GameObject("Relleno", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(frame.transform, false);
        var fillImage = fill.GetComponent<Image>();
        fillImage.raycastTarget = false;
        var rr = (RectTransform)fill.transform;
        rr.anchorMin = Vector2.zero; rr.anchorMax = new Vector2(0f, 1f); rr.pivot = new Vector2(0f, 0.5f);
        rr.offsetMin = new Vector2(4f, 4f); rr.offsetMax = new Vector2(-4f, -4f);
        level.powerBar = rr;
        level.powerFill = fillImage;
        level.powerText = Txt("Texto poder", frame.transform, fTitle, 26f, Color.white, "Poder", TextAlignmentOptions.Center);
        Stretch(level.powerText.rectTransform);

        var veil = new GameObject("Destello poder", typeof(RectTransform), typeof(Image));
        veil.transform.SetParent(hud.transform, false);
        veil.transform.SetAsFirstSibling();
        var iv = veil.GetComponent<Image>();
        iv.raycastTarget = false;
        iv.color = new Color(1f, 1f, 1f, 0f);
        Stretch((RectTransform)veil.transform);
        veil.SetActive(false);
        level.flashImage = iv;

        hud.SetActive(false);
    }

    static TextMeshProUGUI Txt(string displayName, Transform parent, TMP_FontAsset f, float sizeValue, Color c, string text, TextAlignmentOptions align)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.fontSize = sizeValue;
        t.color = c;
        t.text = text;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Transform Child(string displayName, Transform parent, Vector3 local)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        return go.transform;
    }

    static SpriteRenderer Sr(string displayName, Transform parent, Vector3 local, Sprite s, int order)
    {
        var sr = Child(displayName, parent, local).gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }
}
