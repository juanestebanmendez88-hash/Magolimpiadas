using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class CeremoniesBuilder
{
    const string Folder = "Assets/_Magolimpiadas/Podio";
    const string Controller = "Assets/_Magolimpiadas/Magos3D/Animaciones/Ceremonias.controller";
    const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
    const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";

    static readonly (string id, string file, string displayName, Color color)[] Mages =
    {
        ("nebulosa", "maga_nebulosa", "Maga Nebulosa", new Color(0.72f, 0.38f, 1f)),
        ("rayo", "mago_rayo", "Mago del Rayo", new Color(0.32f, 0.62f, 1f)),
        ("elemental", "mago_elemental", "Mago Elemental", new Color(1f, 0.52f, 0.15f)),
        ("druida", "druida_verde", "Druida Verde", new Color(0.38f, 0.88f, 0.32f)),
    };

    [MenuItem("Magolimpiadas/Ceremonias y natación/Preparar")]
    static void Menu()
    {
        Debug.Log("[Ceremonias] " + Prepare() + " Guarda la escena (Ctrl+S).");
    }

    public static string Prepare()
    {
        var report = new List<string>();
        var fTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        var fText = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);

        var ceremony = Root("Ceremonia del podio");
        if (ceremony == null)
        {
            ceremony = new GameObject("Ceremonia del podio");
            Undo.RegisterCreatedObjectUndo(ceremony, "Ceremonia");
            report.Add("creé \"Ceremonia del podio\"");
        }
        var c = Ensure<PodiumCeremony>(ceremony);
        Undo.RecordObject(c, "Ceremonia");
        c.mages.Clear();
        foreach (var m in Mages)
            c.mages.Add(new PodiumCeremony.CeremonyMage
            {
                id = m.id, displayName = m.displayName, color = m.color,
                celebration = Cut(Folder + "/personajes/" + m.file + "_celebracion.png", 6),
                defeat = Cut(Folder + "/personajes/" + m.file + "_derrota.png", 6),
            });
        c.podium = Unique(Folder + "/escenario/podio.png");
        c.bowMedals = Cut(Folder + "/medallas/medallas_arco.png", 3, 0.5f);
        c.swimmingMedals = Cut(Folder + "/medallas/medallas_natacion.png", 3, 0.5f);
        c.pingPongMedals = Cut(Folder + "/medallas/medallas_pingpong.png", 3, 0.5f);
        c.syncedMedals = Cut(Folder + "/medallas/medallas_sincronizado.png", 3, 0.5f);
        c.titleFont = fTitle;
        c.textFont = fText;
        c.slot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Magolimpiadas/Arte/UI/FantasyUI/slots/slot_common.png");
        c.ceremoniesController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
        var podium1 = Find("PodioNivel1");
        var podium3 = Find("PodioNivel3");
        c.level1Podium = podium1;
        c.level3Podium = podium3;
        EditorUtility.SetDirty(c);
        report.Add("ceremonia lista (podios: " + (podium1 != null) + "/" + (podium3 != null) + ", controlador: " + (c.ceremoniesController != null) + ")");

        if (podium1 != null)
        {
            var point = Root("Punto inicio nivel 2 (podio)");
            if (point == null)
            {
                point = new GameObject("Punto inicio nivel 2 (podio)");
                Undo.RegisterCreatedObjectUndo(point, "Punto");
                point.transform.position = Front(podium1, 3.4f);
                point.transform.rotation = Quaternion.LookRotation(VectorUtils.FlatDirection(podium1.forward));
                report.Add("creé el punto delante de PodioNivel1");
            }
            var portal = GameObject.Find("PortalNivel1");
            var entrance = portal != null ? portal.GetComponent<WorldEntrance2D>() : null;
            if (entrance != null)
            {
                Undo.RecordObject(entrance, "Punto al terminar");
                entrance.pointOnFinish = point.transform;
                report.Add("PortalNivel1: al terminar el templo se aparece en el podio del nivel 1");
            }
        }

        var pool = GameObject.Find("Piscina");
        var water = pool != null ? pool.transform.Find("Piscina_Agua") : null;
        var strings = pool != null ? pool.transform.Find("Piscina_Cuerdas") : null;
        if (pool != null && water != null)
        {
            if (pool.GetComponent<Collider>() == null && pool.GetComponent<MeshFilter>() != null)
            {
                Undo.AddComponent<MeshCollider>(pool);
                report.Add("le puse MeshCollider a la piscina (no tenía y no se podía pisar)");
            }
            Physics.SyncTransforms();
            var ba = water.GetComponent<Renderer>().bounds;
            float sup = strings != null ? strings.GetComponent<Renderer>().bounds.center.y : ba.max.y - 0.5f;
            float stringsWidth = strings != null ? strings.GetComponent<Renderer>().bounds.size.x : ba.size.x * 0.52f;

            var swimRoot = Root("Natación (nivel 3)");
            if (swimRoot == null)
            {
                swimRoot = new GameObject("Natación (nivel 3)");
                Undo.RegisterCreatedObjectUndo(swimRoot, "Natación");
                report.Add("creé \"Natación (nivel 3)\"");
            }
            swimRoot.transform.position = new Vector3(ba.center.x, sup - 0.9f, ba.center.z);
            var box = Ensure<BoxCollider>(swimRoot);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(ba.size.x - 0.6f, 2.4f, ba.size.z - 0.6f);
            var race = Ensure<SwimmingRace>(swimRoot);
            Undo.RecordObject(race, "Natación");
            var exit = Child(swimRoot.transform, "Salida");
            exit.position = new Vector3(ba.center.x, sup, ba.min.z + 1.3f);
            exit.rotation = Quaternion.LookRotation(Vector3.forward);
            race.exit = exit;
            race.length = ba.size.z - 2.6f;
            race.laneWidth = stringsWidth / 2f;
            var arrival = Child(swimRoot.transform, "Llegada (al salir del agua)");
            arrival.position = Ground(new Vector3(ba.center.x, sup + 3f, ba.max.z + 2.2f));
            race.arrival = arrival;
            race.titleFont = fTitle;
            race.textFont = fText;
            EditorUtility.SetDirty(race);
            report.Add("carrera: " + race.length.ToString("0.0") + " m, carriles de " + race.laneWidth.ToString("0.00") + " m, agua a y=" + sup.ToString("0.00"));

            if (Root("Título - Natación") == null)
            {
                var t = new GameObject("Título - Natación");
                Undo.RegisterCreatedObjectUndo(t, "Título");
                t.transform.position = new Vector3(ba.center.x, sup + 2f, ba.min.z - 2.5f);
                var tb = t.AddComponent<BoxCollider>(); tb.isTrigger = true; tb.size = new Vector3(14f, 6f, 3f);
                var tz = t.AddComponent<ZoneTitle>();
                tz.title = "Natación"; tz.subtitle = "Nivel 3"; tz.font = fTitle;
                report.Add("creé el título \"Natación\"");
            }
            CreatePoolTeleport(new Vector3(ba.center.x, sup, ba.min.z - 3f), report);
        }
        else report.Add("no encontré la Piscina (con Piscina_Agua)");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return string.Join("; ", report) + ".";
    }

    static void CreatePoolTeleport(Vector3 approxDestination, List<string> report)
    {
        const string displayName = "Teleport de prueba (a la piscina)";
        if (Root(displayName) != null) return;
        var bridge = Find("Camino_10_Puente");
        if (bridge == null) return;
        var bp = bridge.GetComponentInChildren<Renderer>().bounds;
        Vector3 place = Ground(new Vector3(bp.center.x, bp.max.y + 2f, bp.max.z - 1.2f));
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(cube, "Teleport");
        cube.name = displayName;
        cube.transform.position = place + Vector3.up * 1f;
        cube.transform.localScale = new Vector3(2.2f, 2f, 0.4f);
        cube.GetComponent<Collider>().isTrigger = true;
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Magolimpiadas/PingPong/M_TeleportPrueba.mat");
        if (mat != null) cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var destination = new GameObject("Destino (borde de la piscina)").transform;
        destination.SetParent(cube.transform, true);
        destination.position = Ground(approxDestination + Vector3.up * 3f);
        destination.rotation = Quaternion.LookRotation(Vector3.forward);
        var tp = cube.AddComponent<TestTeleport>();
        tp.destination = destination;
        tp.skipCinematic = true;
        report.Add("creé el teleport de prueba al final del puente de la arena");
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    static GameObject Root(string n) => EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == n);

    static Transform Find(string n)
    {
        foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in r.GetComponentsInChildren<Transform>(true))
                if (t.name == n) return t;
        return null;
    }

    static Transform Child(Transform parent, string displayName)
    {
        var t = parent.Find(displayName);
        if (t != null) return t;
        var go = new GameObject(displayName);
        Undo.RegisterCreatedObjectUndo(go, displayName);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Vector3 Front(Transform podium, float meters)
    {
        var mf = podium.GetComponent<MeshFilter>();
        var b = mf != null ? mf.sharedMesh.bounds : new Bounds();
        Vector3 center = podium.TransformPoint(b.center);
        Vector3 p = center + VectorUtils.FlatDirection(podium.forward) * (b.extents.z * podium.lossyScale.z + meters);
        return Ground(p + Vector3.up * 3f);
    }

    static Vector3 Ground(Vector3 fromPoint)
    {
        if (Physics.Raycast(fromPoint, Vector3.down, out var h, 20f, ~0, QueryTriggerInteraction.Ignore)) return h.point + Vector3.up * 0.05f;
        return fromPoint;
    }

    static Sprite Unique(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 64f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite[] Cut(string path, int n, float pivotY = -1f)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("No encontré " + path); return new Sprite[0]; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 64f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 4096;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
        ti.GetSourceTextureWidthAndHeight(out int W, out int H);
        int cw = W / n;
        var pivotPoint = new Vector2(0.5f, pivotY >= 0f ? pivotY : 10f / H);
        string b = Path.GetFileNameWithoutExtension(path);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        var rects = Enumerable.Range(0, n).Select(i =>
        {
            string displayName = b + "_" + i;
            return new SpriteRect
            {
                name = displayName, rect = new Rect(i * cw, 0, cw, H), alignment = SpriteAlignment.Custom, pivot = pivotPoint,
                spriteID = ids.TryGetValue(displayName, out var id) ? id : GUID.Generate(),
            };
        }).ToArray();
        dp.SetSpriteRects(rects);
        var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
    }
}
