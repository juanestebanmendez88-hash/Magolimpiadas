using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

public class ImportMap : EditorWindow
{
    [System.Serializable] class Piece { public string layer, key; public int x, y, w, h, frames, fps; public bool animated; }
    [System.Serializable] class Item { public string key; public int x, y, w, h, n, fps, frame; public bool animated; }
    [System.Serializable] class Entity { public string key, displayName, type, file; public int x, y, vw, ch, baseY, n, fps; }
    [System.Serializable] class Map
    {
        public string format, displayName, background; public int unit, x0, y0, width, heightValue, heightBackground; public float charactersScale;
        public Piece[] pieces; public Item[] items; public Entity[] entities;
    }

    const int MaxTexture = 4096;
    const float CameraSize = 8f;

    TextAsset json;
    string rootFolder = "Assets/_Magolimpiadas/Templo";
    bool collisions = true;
    Vector3 mapPosition = new Vector3(1000, 1000, 0);

    [MenuItem("Tools/Templo/Importar mapa")]
    static void Open() { GetWindow<ImportMap>("Importar mapa"); }

    void OnEnable()
    {
        if (json == null) json = AssetDatabase.LoadAssetAtPath<TextAsset>(rootFolder + "/Mapas/mapa_magolimpiadas.json");
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        json = (TextAsset)EditorGUILayout.ObjectField("Mapa (.json)", json, typeof(TextAsset), false);
        rootFolder = EditorGUILayout.TextField("Carpeta del zip", rootFolder);
        collisions = EditorGUILayout.Toggle("Colisiones en el terreno", collisions);
        mapPosition = EditorGUILayout.Vector3Field("Posición del mapa", mapPosition);
        EditorGUILayout.HelpBox("'Carpeta del zip' es donde copiaste el contenido del zip (debe tener las carpetas assets/ y personajes/).\n" +
                                "'Posición del mapa' es la esquina superior izquierda del mapa en la escena; ponla lejos de lo 3D.", MessageType.Info);
        GUI.enabled = json != null;
        if (GUILayout.Button("Construir mapa", GUILayout.Height(32))) Build();
        GUI.enabled = true;
    }

    int U;
    string gen;
    readonly Dictionary<string, Dictionary<string, Sprite>> cacheSprites = new Dictionary<string, Dictionary<string, Sprite>>();
    readonly Dictionary<string, TileBase> cacheTiles = new Dictionary<string, TileBase>();

    void Build()
    {
        var m = JsonUtility.FromJson<Map>(json.text);
        if (m == null || m.format != "templo-mapa-1") { EditorUtility.DisplayDialog("Importar mapa", "Este archivo no es un mapa del editor.", "Ok"); return; }

        string rootName = "Mapa - " + m.displayName;
        var old = GameObject.Find(rootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Importar mapa", "Ya hay un \"" + rootName + "\" en la escena. ¿Lo reemplazo por uno nuevo?", "Reemplazar", "Cancelar")) return;
            Undo.DestroyObjectImmediate(old);
        }

        U = m.unit > 0 ? m.unit : 64;
        gen = rootFolder.TrimEnd('/') + "/_Generado";
        Folder(gen + "/Tiles");
        cacheSprites.Clear(); cacheTiles.Clear();

        var root = new GameObject(rootName);
        root.transform.position = mapPosition;
        root.AddComponent<Grid>().cellSize = Vector3.one;
        var layers = new Dictionary<string, Tilemap>
        {
            { "agua", CreateTilemap(root, "Agua", -10, false) },
            { "terreno", CreateTilemap(root, "Terreno", 0, collisions) },
            { "deco", CreateTilemap(root, "Decoracion", 5, false) },
        };
        Transform start = null;
        try
        {
            CreateBackground(root, m);
            int i = 0, total = (m.pieces?.Length ?? 0) + (m.items?.Length ?? 0) + (m.entities?.Length ?? 0);
            foreach (var p in m.pieces ?? new Piece[0])
            {
                EditorUtility.DisplayProgressBar("Importar mapa", p.key, (float)i++ / total);
                if (!layers.TryGetValue(p.layer, out var tm)) continue;
                for (int c = 0; c < p.w; c++)
                    for (int r = 0; r < p.h; r++)
                    {
                        var tile = TileOf(p, c, r);
                        if (tile != null) tm.SetTile(new Vector3Int(p.x + c, -(p.y + r) - 1, 0), tile);
                    }
            }
            var gObj = new GameObject("Objetos"); gObj.transform.SetParent(root.transform, false);
            foreach (var o in m.items ?? new Item[0])
            {
                EditorUtility.DisplayProgressBar("Importar mapa", o.key, (float)i++ / total);
                string path = rootFolder + "/assets/objetos/" + o.key + ".png";
                var frames = Strip(path, o.w * U, o.h * U, o.n, U, new Vector2(0.5f, 0f));
                if (frames == null) continue;
                var go = new GameObject(o.key);
                go.transform.SetParent(gObj.transform, false);
                go.transform.localPosition = new Vector3(o.x + o.w / 2f, -(o.y + o.h), 0);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 10;
                sr.sprite = frames[Mathf.Clamp(o.frame, 0, frames.Length - 1)];
                if (o.animated && frames.Length > 1) { var a = go.AddComponent<AnimatedSprite>(); a.frames = frames; a.fps = o.fps > 0 ? o.fps : 8; }
            }
            var gEnt = new GameObject("Personajes"); gEnt.transform.SetParent(root.transform, false);
            float ppu = U / (m.charactersScale > 0 ? m.charactersScale : 0.5f);
            foreach (var e in m.entities ?? new Entity[0])
            {
                EditorUtility.DisplayProgressBar("Importar mapa", e.displayName, (float)i++ / total);
                string path = rootFolder + "/" + e.file;
                var frames = Strip(path, e.vw, e.ch, e.n, ppu, new Vector2(0.5f, (e.ch - e.baseY) / (float)e.ch));
                if (frames == null) continue;
                var go = new GameObject(e.type == "jugador" ? "Inicio jugador - " + e.displayName : e.displayName);
                go.transform.SetParent(gEnt.transform, false);
                go.transform.localPosition = new Vector3(e.x + 0.5f, -(e.y + 1), 0);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 20; sr.sprite = frames[0];
                var a = go.AddComponent<AnimatedSprite>(); a.frames = frames; a.fps = e.fps > 0 ? e.fps : 8;
                if (start == null && e.type == "jugador") start = go.transform;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        if (collisions) PrepareComposite(layers["terreno"]);
        var cam = CreateCamera(root, m, start);

        AssetDatabase.SaveAssets();
        Undo.RegisterCreatedObjectUndo(root, "Importar mapa");
        Selection.activeGameObject = root;
        Debug.Log("Mapa importado: " + m.displayName + " (" + m.width + " x " + m.heightValue + " tiles) en " + mapPosition);
        Capture(cam, m);
    }

    void CreateBackground(GameObject root, Map m)
    {
        if (string.IsNullOrEmpty(m.background) || m.heightBackground <= 0) return;
        string path = rootFolder + "/" + m.background;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("No encontré el fondo " + path); return; }
        PngSize(path, out int fw, out int fh);
        float ppu = fh / (float)m.heightBackground;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.textureType = TextureImporterType.Sprite;
        st.spriteMode = (int)SpriteImportMode.Single;
        st.spritePixelsPerUnit = ppu;
        st.spriteAlignment = (int)SpriteAlignment.TopLeft;
        st.filterMode = FilterMode.Point;
        st.mipmapEnabled = false;
        ti.SetTextureSettings(st);
        ti.maxTextureSize = MaxTexture;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) return;
        var g = new GameObject("Fondo"); g.transform.SetParent(root.transform, false);
        float wu = fw / ppu;
        for (float x = Mathf.Floor(m.x0 / wu) * wu; x < m.x0 + m.width; x += wu)
        {
            var go = new GameObject("fondo");
            go.transform.SetParent(g.transform, false);
            go.transform.localPosition = new Vector3(x, 0, 0);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = -100;
        }
    }

    Tilemap CreateTilemap(GameObject root, string displayName, int order, bool collider)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(root.transform, false);
        var tm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = order;
        if (collider) go.AddComponent<TilemapCollider2D>();
        return tm;
    }

    static void PrepareComposite(Tilemap terrain)
    {
        var go = terrain.gameObject;
        var tc = go.GetComponent<TilemapCollider2D>();
        if (tc == null) return;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var cc = go.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        cc.GenerateGeometry();
    }

    Camera CreateCamera(GameObject root, Map m, Transform start)
    {
        var go = new GameObject("Camara Mapa 2D");
        go.transform.SetParent(root.transform, false);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.06f, 0.16f);
        cam.nearClipPlane = 0.3f; cam.farClipPlane = 50f;
        cam.depth = 10;
        float halfWidth = CameraSize * 16f / 9f;
        Vector3 p = start != null ? start.localPosition : new Vector3(halfWidth, -m.heightValue / 2f, 0);
        p.x = Mathf.Clamp(p.x, halfWidth, Mathf.Max(halfWidth, m.width - halfWidth));
        p.y = Mathf.Clamp(p.y + 2f, -m.heightValue + CameraSize, -CameraSize);
        go.transform.localPosition = new Vector3(p.x, p.y, -10f);
        cam.enabled = false;
        return cam;
    }

    static void Capture(Camera cam, Map m)
    {
        if (cam == null) return;
        var t = cam.transform; var pos = t.localPosition; float size = cam.orthographicSize; bool wasEnabled = cam.enabled;
        var rt = new RenderTexture(m.width * 16, m.heightValue * 16, 24);
        try
        {
            t.localPosition = new Vector3(m.width / 2f, -m.heightValue / 2f, -10f);
            cam.orthographicSize = m.heightValue / 2f;
            cam.enabled = true;
            var req = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
            else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            string file = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "captura_mapa_2d.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            DestroyImmediate(tex);
            Debug.Log("Captura del mapa guardada en " + file);
        }
        catch (System.Exception ex) { Debug.LogWarning("No pude guardar la captura del mapa (no afecta al mapa): " + ex.Message); }
        finally
        {
            cam.targetTexture = null;
            t.localPosition = pos; cam.orthographicSize = size; cam.enabled = wasEnabled;
            rt.Release(); DestroyImmediate(rt);
        }
    }

    TileBase TileOf(Piece p, int c, int r)
    {
        string key = p.key + "_" + c + "_" + r;
        if (cacheTiles.TryGetValue(key, out var t)) return t;
        string assetPath = gen + "/Tiles/" + key + ".asset";
        TileBase tile = null;
        if (p.animated)
        {
            var sprites = new List<Sprite>();
            for (int f = 1; f <= Mathf.Max(1, p.frames); f++)
            {
                var s = Cell(rootFolder + "/assets/tiles/" + p.key + "_" + f + ".png", c, r);
                if (s != null) sprites.Add(s);
            }
            if (sprites.Count > 0)
            {
                var at = AssetDatabase.LoadAssetAtPath<AnimatedTileMap>(assetPath);
                if (at == null) { at = CreateInstance<AnimatedTileMap>(); AssetDatabase.CreateAsset(at, assetPath); }
                at.sprites = sprites.ToArray(); at.fps = p.fps > 0 ? p.fps : 4;
                EditorUtility.SetDirty(at); tile = at;
            }
        }
        else
        {
            var s = Cell(rootFolder + "/assets/tiles/" + p.key + ".png", c, r);
            if (s != null && !Empty(s))
            {
                var st = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (st == null) { st = CreateInstance<Tile>(); AssetDatabase.CreateAsset(st, assetPath); }
                st.sprite = s;
                st.colliderType = p.layer == "terreno" ? Tile.ColliderType.Sprite : Tile.ColliderType.None;
                EditorUtility.SetDirty(st); tile = st;
            }
        }
        cacheTiles[key] = tile;
        return tile;
    }

    Sprite Cell(string path, int c, int r)
    {
        var d = Slice(path, U, U, U, new Vector2(0.5f, 0.5f), true);
        if (d == null) return null;
        d.TryGetValue(Path.GetFileNameWithoutExtension(path) + "_" + c + "_" + r, out var s);
        return s;
    }

    Sprite[] Strip(string path, int cw, int ch, int n, float ppu, Vector2 pivot)
    {
        var d = Slice(path, cw, ch, ppu, pivot, false);
        if (d == null) return null;
        string b = Path.GetFileNameWithoutExtension(path);
        var list = new List<Sprite>();
        for (int i = 0; i < n; i++) if (d.TryGetValue(b + "_" + i, out var s)) list.Add(s);
        return list.Count > 0 ? list.ToArray() : null;
    }

    Dictionary<string, Sprite> Slice(string path, int cw, int ch, float ppu, Vector2 pivot, bool grid)
    {
        if (cacheSprites.TryGetValue(path, out var cached)) return cached;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("No encontré " + path); cacheSprites[path] = null; return null; }
        PngSize(path, out int W, out int H);
        string b = Path.GetFileNameWithoutExtension(path);

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = MaxTexture;
        ti.mipmapEnabled = false;
        ti.isReadable = true;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        var rects = new List<SpriteRect>();
        int cols = W / cw, rows = grid ? H / ch : 1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                string displayName = grid ? b + "_" + c + "_" + r : b + "_" + c;
                rects.Add(new SpriteRect
                {
                    name = displayName,
                    rect = new Rect(c * cw, H - (r + 1) * ch, cw, ch),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = ids.TryGetValue(displayName, out var id) ? id : GUID.Generate(),
                });
            }
        dp.SetSpriteRects(rects.ToArray());
        var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();

        var d = new Dictionary<string, Sprite>();
        foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) d[s.name] = s;
        if (d.Count == 0) Debug.LogWarning("No se pudieron cortar sprites en " + path);
        cacheSprites[path] = d;
        return d;
    }

    static bool Empty(Sprite s)
    {
        var r = s.rect;
        var px = s.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        for (int i = 0; i < px.Length; i++) if (px[i].a > 0.02f) return false;
        return true;
    }

    static void PngSize(string path, out int w, out int h)
    {
        var b = new byte[24];
        using (var f = File.OpenRead(path)) f.Read(b, 0, 24);
        w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
