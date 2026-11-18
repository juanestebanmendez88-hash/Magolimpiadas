using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class FinalCeremonyBuilder
{
    const string Folder = "Assets/_Magolimpiadas/CeremoniaFinal";
    const string Prefabs = Folder + "/Prefabs";
    const string Materials = Folder + "/Materiales";
    const string IslandModel = Folder + "/Modelos/Isla_Ceremonia.fbx";
    const string GameScene = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
    const string FinalScene = "Assets/_Magolimpiadas/Escenas/99_CeremoniaFinal.unity";
    const string Sky = "Assets/_Magolimpiadas/Cielo/Epic_GloriousPink/Materials/Epic_GloriousPink_EquiRect.mat";
    const string Controller = "Assets/_Magolimpiadas/Magos3D/Animaciones/Ceremonias.controller";
    const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
    const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";
    const string Medals = "Assets/_Magolimpiadas/Podio/medallas/";
    const string ButtonPath = "Assets/_Magolimpiadas/Arte/UI/FantasyUI/buttons/button_normal.png";
    const string Gap = "Assets/_Magolimpiadas/Arte/UI/FantasyUI/slots/slot_locked.png";
    const string Panel = "Assets/_Magolimpiadas/Arte/UI/FantasyUI/panels/tooltip.png";
    const string StandName = "Ceremonia_Estrado";
    const string GroundName = "Isla_Ceremonia";

    static readonly string[] Old = { Prefabs + "/Isla_Ceremonia.prefab", Prefabs + "/Fuego_Caldero.prefab", Prefabs + "/Podio_Ceremonia.prefab" };

    static readonly (string id, string displayName, Color color)[] Mages =
    {
        ("elemental", "Mago Elemental", new Color(1f, 0.52f, 0.15f)),
        ("nebulosa", "Maga Nebulosa", new Color(0.72f, 0.38f, 1f)),
        ("druida", "Druida Verde", new Color(0.38f, 0.88f, 0.32f)),
        ("rayo", "Mago del Rayo", new Color(0.32f, 0.62f, 1f)),
    };

    static readonly (string test, string shortName, string file)[] Tests =
    {
        ("Tiro con arco", "Arco", "medallas_arco"),
        ("Ping pong", "Ping pong", "medallas_pingpong"),
        ("Natación", "Natación", "medallas_natacion"),
        ("Natación artística", "Artística", "medallas_sincronizado"),
    };

    class Copy
    {
        public readonly Dictionary<string, GameObject> mages = new Dictionary<string, GameObject>();
        public Color ambience;
        public float lightIntensity; public Color lightColor; public Quaternion lightRotation;
        public VolumeProfile profile;
    }

    [MenuItem("Magolimpiadas/Ceremonia final/Crear escena")]
    static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Debug.Log("[Ceremonia final] " + Create());
    }

    public static string Create()
    {
        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory(Materials);
        AssetDatabase.Refresh();
        var island = PrepareModel(out string modelReport);
        if (island == null) return "falta el modelo " + IslandModel;
        var copy = CopyFromGame();
        if (copy == null) return "no pude abrir " + GameScene;
        string report = BuildScene(copy, island);
        foreach (var v in Old) if (AssetDatabase.LoadMainAssetAtPath(v) != null) AssetDatabase.DeleteAsset(v);
        SortProject.PutScenesInBuild();
        return report + "; " + modelReport;
    }

    static GameObject PrepareModel(out string report)
    {
        report = "";
        var mi = AssetImporter.GetAtPath(IslandModel) as ModelImporter;
        if (mi == null) return null;
        bool change = false;
        if (mi.importAnimation) { mi.importAnimation = false; change = true; }
        if (mi.animationType != ModelImporterAnimationType.None) { mi.animationType = ModelImporterAnimationType.None; change = true; }
        if (mi.importCameras || mi.importLights) { mi.importCameras = false; mi.importLights = false; change = true; }
        if (!mi.useFileScale || !Mathf.Approximately(mi.globalScale, 1f)) { mi.useFileScale = true; mi.globalScale = 1f; change = true; }
        if (mi.addCollider) { mi.addCollider = false; change = true; }
        if (change) mi.SaveAndReimport();

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        int withTexture = 0, total = 0;
        var names = AssetDatabase.LoadAllAssetsAtPath(IslandModel).OfType<Material>().Select(m => m.name)
            .Concat(mi.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)).Select(k => k.name))
            .Distinct().ToList();
        foreach (var displayName in names)
        {
            string path = Materials + "/" + displayName + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
            if (Adjust(mat, displayName)) withTexture++;
            EditorUtility.SetDirty(mat);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), displayName), mat);
            total++;
        }
        mi.SaveAndReimport();
        AssetDatabase.SaveAssets();
        report = "isla: " + total + " materiales (" + withTexture + " con textura" + (withTexture == 0 ? "; faltan las texturas T_*.png del FBX" : "") + ")";
        return AssetDatabase.LoadAssetAtPath<GameObject>(IslandModel);
    }

    static readonly Dictionary<string, (Color color, float smooth)> Colors = new Dictionary<string, (Color, float)>
    {
        { "Hoja_Clara", (new Color(0.238f, 0.552f, 0.056f), 0.25f) },
        { "Hoja_Verde", (new Color(0.05f, 0.283f, 0.041f), 0.25f) },
        { "Flor_Crema", (new Color(0.939f, 0.839f, 0.597f), 0.3f) },
        { "Flor_Amarilla", (new Color(0.93f, 0.584f, 0.058f), 0.3f) },
        { "Flor_Rosa", (new Color(0.888f, 0.147f, 0.397f), 0.3f) },
        { "Piedra_Beige", (new Color(0.791f, 0.624f, 0.445f), 0.2f) },
        { "Panel_Morado", (new Color(0.068f, 0.025f, 0.223f), 0.35f) },
        { "Oro", (new Color(0.888f, 0.451f, 0.042f), 0.6f) },
        { "Tela_Azul", (new Color(0.026f, 0.045f, 0.301f), 0.15f) },
        { "Piedra_Morada", (new Color(0.198f, 0.141f, 0.392f), 0.2f) },
        { "Piedra_Estatua", (new Color(0.61f, 0.485f, 0.361f), 0.15f) },
        { "Piedra_Estatua_Oscura", (new Color(0.209f, 0.15f, 0.107f), 0.1f) },
        { "FX_Agua", (new Color(0.275f, 0.716f, 1f), 0.8f) },
        { "Tronco", (new Color(0.156f, 0.068f, 0.027f), 0.1f) },
        { "Roca_Isla", (new Color(0.105f, 0.078f, 0.209f), 0.1f) },
        { "FX_Llama", (new Color(1f, 0.445f, 0.042f), 0.3f) },
        { "Pino", (new Color(0.025f, 0.112f, 0.048f), 0.15f) },
        { "Pino_Claro", (new Color(0.047f, 0.195f, 0.072f), 0.15f) },
        { "Cesped", (new Color(0.048f, 0.144f, 0.037f), 0.1f) },
        { "Suelo_Lavanda", (new Color(0.381f, 0.332f, 0.651f), 0.25f) },
    };

    static bool Adjust(Material mat, string displayName)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (mat.shader != lit) mat.shader = lit;
        (Color color, float smooth) data = Colors.TryGetValue(displayName, out var d) ? d : (new Color(0.6f, 0.6f, 0.6f), 0.2f);
        var texture = Texture("T_" + displayName) ?? (displayName == "FX_Agua" ? Texture("T_Agua") : null);
        var normal = Texture("T_" + displayName + "_N") ?? (displayName == "Piedra_Estatua_Oscura" ? Texture("T_Piedra_Estatua_N") : null);
        mat.SetColor("_BaseColor", texture != null ? Color.white : data.color.gamma);
        mat.SetTexture("_BaseMap", texture);
        mat.SetFloat("_Smoothness", data.smooth);
        mat.SetFloat("_Metallic", displayName == "Oro" ? 0.6f : 0f);
        if (normal != null)
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        else { mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP"); }

        bool shines = displayName == "FX_Llama" || displayName == "FX_Agua";
        if (shines)
        {
            Color emission = displayName == "FX_Llama" ? new Color(1f, 0.323f, 0.014f) : new Color(0.212f, 0.631f, 1f);
            float force = displayName == "FX_Llama" ? 1.8f : 0.6f;
            mat.SetColor("_EmissionColor", emission.gamma * Mathf.Pow(force, 1f / 2.2f));
            if (displayName == "FX_Agua" && texture != null) mat.SetTexture("_EmissionMap", texture);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        return texture != null;
    }

    static Texture2D Texture(string displayName)
    {
        foreach (var folder in new[] { Folder + "/Modelos/Isla_Ceremonia.fbm", Folder + "/Modelos/Texturas", Folder + "/Modelos" })
            foreach (var ext in new[] { ".png", ".jpg", ".tga" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + displayName + ext);
                if (t != null) return t;
            }
        return null;
    }

    static Copy CopyFromGame()
    {
        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        if (!scene.IsValid()) return null;
        var c = new Copy();

        var playerMage = Object.FindFirstObjectByType<PlayerMage>(FindObjectsInactive.Include);
        if (playerMage != null && playerMage.animator != null) c.mages[ChosenMage.PlayerMageId] = CaptureMage(playerMage.animator.gameObject, playerMage.torch, ChosenMage.PlayerMageId);
        foreach (var bot in Object.FindObjectsByType<BotMage3D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (bot.animator != null && !c.mages.ContainsKey(bot.mageId)) c.mages[bot.mageId] = CaptureMage(bot.animator.gameObject, bot.torch, bot.mageId);

        c.ambience = RenderSettings.ambientLight;
        var lightSource = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (lightSource != null) { c.lightIntensity = lightSource.intensity; c.lightColor = lightSource.color; c.lightRotation = lightSource.transform.rotation; }
        else { c.lightIntensity = 1.4f; c.lightColor = new Color(1f, 0.93f, 0.83f); c.lightRotation = Quaternion.Euler(50f, 330f, 0f); }
        var volume = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(v => v.isGlobal);
        c.profile = volume != null ? volume.sharedProfile : null;
        return c;
    }

    static GameObject CaptureMage(GameObject model, GameObject torch, string id)
    {
        var copy = Object.Instantiate(model);
        copy.name = "Mago_" + id;
        copy.transform.localScale = model.transform.lossyScale;
        if (torch != null && torch.transform.IsChildOf(model.transform))
        {
            var t = copy.transform.Find(AnimationUtility.CalculateTransformPath(torch.transform, model.transform));
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        foreach (var r in copy.GetComponentsInChildren<Racket>(true)) Object.DestroyImmediate(r.gameObject);
        foreach (var r in copy.GetComponentsInChildren<Collectible>(true)) Object.DestroyImmediate(r.gameObject);
        foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
        var prefab = PrefabUtility.SaveAsPrefabAsset(copy, Prefabs + "/Mago_" + id + ".prefab");
        Object.DestroyImmediate(copy);
        return prefab;
    }

    static string BuildScene(Copy c, GameObject islandModel)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var fTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        var fText = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);

        var stage = new GameObject("— Escenario —").transform;
        var island = (GameObject)PrefabUtility.InstantiatePrefab(islandModel, stage);
        island.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var ground = island.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == GroundName);
        var stand = Child(island.transform, StandName);
        island.name = "Isla de la Ceremonia";
        Bounds islandBounds = ground != null ? ground.bounds : Limits(island);
        var center = new GameObject("Centro de la plaza").transform;
        center.SetParent(stage, false);
        center.position = new Vector3(islandBounds.center.x, HeightAt(ground, islandBounds.center, islandBounds.max.y), islandBounds.center.z);
        float radius = Mathf.Max(islandBounds.extents.x, islandBounds.extents.z);

        var places = Steps(stand, center.position, out string standInfo);
        for (int i = 0; i < places.Length; i++) places[i].SetParent(stage, true);

        var system = new GameObject("— Sistema —").transform;
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(system, false);
        var cam = camGo.GetComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 2500f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        Vector3 front = places[0] != null ? places[0].forward : Vector3.forward;
        camGo.transform.position = center.position + front * (radius + 14f) + Vector3.up * 14f;
        camGo.transform.LookAt(center.position + Vector3.up * 2f);

        var lightSource = new GameObject("Luz Direccional", typeof(Light)).GetComponent<Light>();
        lightSource.transform.SetParent(system, false);
        lightSource.type = LightType.Directional;
        lightSource.intensity = c.lightIntensity;
        lightSource.color = c.lightColor;
        lightSource.shadows = LightShadows.Soft;
        lightSource.transform.rotation = c.lightRotation;
        RenderSettings.sun = lightSource;

        if (c.profile != null)
        {
            var vol = new GameObject("Volumen Global", typeof(Volume)).GetComponent<Volume>();
            vol.transform.SetParent(system, false);
            vol.isGlobal = true;
            vol.sharedProfile = c.profile;
        }
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(system, false);
        ArtisticSwimmingBuilder.EnsureAudio();
        AudioBuilder.EnsureMusic(true, GameAudio.Track.Ceremony);

        var sky = AssetDatabase.LoadAssetAtPath<Material>(Sky);
        if (sky != null) RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = c.ambience.maxColorComponent > 0.05f ? c.ambience : new Color(0.55f, 0.5f, 0.7f);
        RenderSettings.fog = false;

        var magesGroup = new GameObject("— Magos —").transform;
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
        var ceremony = new GameObject("Ceremonia final").AddComponent<FinalCeremony>();
        ceremony.transform.SetParent(system, false);
        for (int i = 0; i < Mages.Length; i++)
        {
            var m = Mages[i];
            if (!c.mages.TryGetValue(m.id, out var prefab) || prefab == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, magesGroup);
            go.name = m.displayName;
            if (places[i] != null) go.transform.SetPositionAndRotation(places[i].position, places[i].rotation);
            ceremony.mages.Add(new FinalCeremony.Mage { id = m.id, displayName = m.displayName, color = m.color, animator = go.GetComponent<Animator>() });
        }
        ceremony.places = places;
        ceremony.center = center;
        ceremony.viewRadius = radius + 14f;
        ceremony.viewHeight = 13f;
        ceremony.sceneCamera = cam;
        ceremony.ceremoniesController = controller;
        ceremony.titleFont = fTitle;
        ceremony.textFont = fText;
        ceremony.gap = AssetDatabase.LoadAssetAtPath<Sprite>(Gap);
        foreach (var p in Tests)
        {
            var s = AssetDatabase.LoadAllAssetsAtPath(Medals + p.file + ".png").OfType<Sprite>().OrderBy(x => x.name).ToArray();
            ceremony.medals.Add(new FinalCeremony.TestMedals { test = p.test, shortName = p.shortName, sprites = s.Length >= 3 ? s.Take(3).ToArray() : s });
        }

        CreateInterface(ceremony, fTitle, fText);
        EditorSceneManager.SaveScene(scene, FinalScene);
        return "escena " + FinalScene + " creada (magos: " + ceremony.mages.Count + "/4, " + standInfo + ")";
    }

    static Transform[] Steps(Transform stand, Vector3 square, out string info)
    {
        var places = new Transform[4];
        var heights = new List<(float center, float heightValue, float width)>();
        Vector3 front = Vector3.forward, length = Vector3.right;
        Bounds b = new Bounds(square, Vector3.one);
        if (stand != null)
        {
            b = Limits(stand.gameObject);
            length = b.size.x >= b.size.z ? Vector3.right : Vector3.forward;
            Vector3 shortName = length == Vector3.right ? Vector3.forward : Vector3.right;
            front = Vector3.Dot(square - b.center, shortName) >= 0f ? shortName : -shortName;

            var temporary = new List<MeshCollider>();
            foreach (var mf in stand.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temporary.Add(mc); }
            Physics.SyncTransforms();
            float fromPoint = Vector3.Dot(b.min, length), until = Vector3.Dot(b.max, length);
            float step = 0.05f;
            float? start = null; float sectionHeight = 0f, last = 0f;
            for (float s = fromPoint + step; s <= until - step + 1e-4f; s += step)
            {
                Vector3 p = b.center + length * (s - Vector3.Dot(b.center, length)) + Vector3.up * (b.extents.y + 2f);
                bool has = Physics.Raycast(p, Vector3.down, out var h, b.size.y + 4f, ~0, QueryTriggerInteraction.Ignore);
                float y = has ? h.point.y : float.NegativeInfinity;
                if (start.HasValue && has && Mathf.Abs(y - sectionHeight) < 0.03f) { last = s; continue; }
                if (start.HasValue && last - start.Value >= 1f) heights.Add(((start.Value + last) * 0.5f, sectionHeight, last - start.Value));
                start = has ? s : (float?)null; sectionHeight = y; last = s;
            }
            if (start.HasValue && last - start.Value >= 1f) heights.Add(((start.Value + last) * 0.5f, sectionHeight, last - start.Value));
            foreach (var mc in temporary) Object.DestroyImmediate(mc);
        }

        var order = heights.OrderByDescending(a => a.heightValue).Take(4).ToList();
        for (int i = 0; i < 4; i++)
        {
            Vector3 p;
            if (i < order.Count)
                p = b.center + length * (order[i].center - Vector3.Dot(b.center, length)) + front * 0.15f;
            else
                p = b.center + front * (b.extents.magnitude + 1.5f) + length * ((i - 1.5f) * 2.2f);
            p.y = i < order.Count ? order[i].heightValue + 0.02f : square.y + 0.02f;
            var t = new GameObject("Podium_" + (i + 1)).transform;
            t.SetPositionAndRotation(p, Quaternion.LookRotation(front));
            places[i] = t;
        }
        info = stand == null ? "sin estrado: magos en fila" :
               "estrado con " + order.Count + " escalones (" + string.Join(", ", order.Select(a => a.heightValue.ToString("0.00") + " m")) + ")";
        return places;
    }

    static void CreateInterface(FinalCeremony c, TMP_FontAsset fTitle, TMP_FontAsset fText)
    {
        var go = new GameObject("Interfaz ceremonia", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var esc = go.GetComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;
        var root = go.transform;

        var title = Group("Título", root, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 340f));
        title.anchorMin = new Vector2(0f, 1f); title.anchorMax = new Vector2(1f, 1f);
        var shadow = title.gameObject.AddComponent<Image>();
        shadow.sprite = Gradient();
        shadow.color = new Color(1f, 1f, 1f, 0.8f);
        shadow.raycastTarget = false;
        c.titleGroup = title.GetComponent<CanvasGroup>();
        c.title = Txt("Texto título", title, fTitle, 110f, new Color(1f, 0.84f, 0.38f), "Ceremonia de clausura", new Vector2(0f, -70f), new Vector2(1600f, 140f), new Vector2(0.5f, 1f));
        c.title.outlineWidth = 0.22f; c.title.outlineColor = new Color32(40, 12, 60, 255);
        c.title.enableAutoSizing = true; c.title.fontSizeMin = 60f; c.title.fontSizeMax = 110f;
        c.subtitle = Txt("Subtítulo", title, fText, 40f, new Color(1f, 0.95f, 0.85f), "Magolimpiadas", new Vector2(0f, -205f), new Vector2(1600f, 60f), new Vector2(0.5f, 1f));
        c.subtitle.outlineWidth = 0.3f; c.subtitle.outlineColor = new Color32(20, 8, 40, 255);

        var panel = AssetDatabase.LoadAssetAtPath<Sprite>(Panel);
        var medalTable = Group("Medallero", root, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(1500f, 430f));
        var background = medalTable.gameObject.AddComponent<Image>();
        Background(background, panel, 0.95f);
        c.medalTableGroup = medalTable.GetComponent<CanvasGroup>();
        var tm = Txt("Título medallero", medalTable, fTitle, 52f, new Color(1f, 0.84f, 0.38f), "Medallero final", new Vector2(0f, -8f), new Vector2(1400f, 64f), new Vector2(0.5f, 1f));
        tm.outlineWidth = 0.2f; tm.outlineColor = new Color32(30, 10, 50, 255);
        var rows = new GameObject("Filas", typeof(RectTransform)).GetComponent<RectTransform>();
        rows.SetParent(medalTable, false);
        rows.anchorMin = new Vector2(0f, 0f); rows.anchorMax = new Vector2(1f, 1f);
        rows.offsetMin = new Vector2(30f, 16f); rows.offsetMax = new Vector2(-30f, -78f);
        c.medalTableRows = rows;

        var final = Group("Victoria", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1300f, 600f));
        var finalBackground = final.gameObject.AddComponent<Image>();
        Background(finalBackground, panel, 0.97f);
        c.finalGroup = final.GetComponent<CanvasGroup>();
        c.finalText = Txt("Texto victoria", final, fTitle, 92f, new Color(1f, 0.84f, 0.38f), "¡Ganaron las Magolimpiadas!", new Vector2(0f, -40f), new Vector2(1250f, 120f), new Vector2(0.5f, 1f));
        c.finalText.outlineWidth = 0.22f; c.finalText.outlineColor = new Color32(40, 12, 60, 255);
        c.finalText.enableAutoSizing = true; c.finalText.fontSizeMin = 56f; c.finalText.fontSizeMax = 92f;
        c.finalDetail = Txt("Detalle", final, fText, 34f, Color.white, "", new Vector2(0f, -175f), new Vector2(1200f, 220f), new Vector2(0.5f, 1f));
        c.finalDetail.textWrappingMode = TextWrappingModes.Normal;
        c.finalDetail.lineSpacing = 12f;
        c.finalDetail.enableAutoSizing = true; c.finalDetail.fontSizeMin = 22f; c.finalDetail.fontSizeMax = 34f;
        c.startButton = CreateButton("Volver al inicio", final, new Vector2(-215f, 54f), fText);
        c.exitButton = CreateButton("Salir del juego", final, new Vector2(215f, 54f), fText);

        c.continueNotice = Txt("Aviso continuar", root, fText, 24f, new Color(1f, 1f, 1f, 0.95f), "Clic o Espacio · continuar", new Vector2(-30f, 16f), new Vector2(420f, 36f), new Vector2(1f, 0f), TextAlignmentOptions.Right);
        c.continueNotice.outlineWidth = 0.32f; c.continueNotice.outlineColor = new Color32(20, 8, 40, 255);
        c.continueNotice.gameObject.SetActive(false);
        var black = new GameObject("Negro", typeof(RectTransform), typeof(Image));
        black.transform.SetParent(root, false);
        var img = black.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var rn = (RectTransform)black.transform;
        rn.anchorMin = Vector2.zero; rn.anchorMax = Vector2.one; rn.offsetMin = rn.offsetMax = Vector2.zero;
        c.black = img;
    }

    static RectTransform Group(string displayName, Transform parent, Vector2 anchor, Vector2 pos, Vector2 sizeValue)
    {
        var go = new GameObject(displayName, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return rt;
    }

    static Button CreateButton(string text, Transform parent, Vector2 pos, TMP_FontAsset f)
    {
        var go = new GameObject("Botón " + text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
        img.type = img.sprite != null && img.sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        if (img.sprite == null) img.color = new Color(0.35f, 0.2f, 0.55f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360f, 112f);
        go.AddComponent<ArcaneGames.UI.UIButtonFx>();
        var t = Txt("Texto", go.transform, f, 30f, new Color(1f, 0.95f, 0.85f), text, new Vector2(0f, 4f), new Vector2(240f, 70f), new Vector2(0.5f, 0.5f));
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(30, 10, 50, 255);
        t.enableAutoSizing = true; t.fontSizeMin = 18f; t.fontSizeMax = 30f;
        return go.GetComponent<Button>();
    }

    static void Background(Image img, Sprite panel, float alpha)
    {
        img.sprite = panel;
        img.type = panel != null && panel.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        img.color = panel != null ? new Color(1f, 1f, 1f, alpha) : new Color(0.05f, 0.02f, 0.13f, alpha * 0.86f);
        img.raycastTarget = false;
    }

    static Sprite Gradient()
    {
        const string path = Folder + "/Arte/Degradado_Titulo.png";
        var done = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (done != null) return done;
        Directory.CreateDirectory(Folder + "/Arte");
        var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            float a = Mathf.SmoothStep(0f, 1f, y / 63f) * 0.75f;
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(0.06f, 0.02f, 0.14f, a));
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static TextMeshProUGUI Txt(string displayName, Transform parent, TMP_FontAsset f, float sizeValue, Color c, string text, Vector2 pos, Vector2 box, Vector2 anchor,
                               TextAlignmentOptions align = TextAlignmentOptions.Center)
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
        t.textWrappingMode = TextWrappingModes.NoWrap;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }

    static Transform Child(Transform root, string displayName) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == displayName);

    static float HeightAt(MeshRenderer ground, Vector3 point, float fallback)
    {
        if (ground == null) return fallback;
        var mf = ground.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return fallback;
        var mc = ground.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;
        Physics.SyncTransforms();
        float y = Physics.Raycast(new Vector3(point.x, ground.bounds.max.y + 5f, point.z), Vector3.down, out var h, ground.bounds.size.y + 10f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : fallback;
        Object.DestroyImmediate(mc);
        return y;
    }

    static Bounds Limits(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 4f);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
