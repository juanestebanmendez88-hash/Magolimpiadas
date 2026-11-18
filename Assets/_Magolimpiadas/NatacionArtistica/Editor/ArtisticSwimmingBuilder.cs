using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static class ArtisticSwimmingBuilder
{
    const string Folder = "Assets/_Magolimpiadas/NatacionArtistica";
    const string Art = Folder + "/Arte";
    const string Generated = Art + "/Generado";
    const string Data = Folder + "/Datos";
    const string Background = Art + "/Piscina_de_la_Luna.png";
    const string Temple = "Assets/_Magolimpiadas/Templo/personajes";
    const string Podium = "Assets/_Magolimpiadas/Podio";
    const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
    const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";
    const string StarIcon = "Assets/_Magolimpiadas/Arte/UI/Iconos/Star.png";
    const string Song = "Assets/_Magolimpiadas/Audio/Musica/Mus_NatacionArtistica_151BPM.mp3";
    const float SongBpm = 151f;
    const float SongOffset = 0.296f;
    const string LevelName = "Nivel Natación Artística";
    const string EntranceName = "Natación artística (nivel 4)";
    const string GapTest = "Natación artística";
    static readonly Vector3 LevelPosition = new Vector3(3000f, 1000f, 0f);
    const float MagesPpu = 100f;

    static readonly (string id, string displayName, string folder, string prefix, string pose, int poseFrames, string podium, Color color)[] Mages =
    {
        ("elemental", "Mago Elemental", "mago_elemental", "elemental", "elemental_idle_con_poder_de_frente", 6, "mago_elemental", new Color(1f, 0.52f, 0.15f)),
        ("nebulosa", "Maga Nebulosa", "maga_nebulosa", "nebulosa", "nebulosa_ataque_de_frente", 3, "maga_nebulosa", new Color(0.72f, 0.38f, 1f)),
        ("druida", "Druida Verde", "druida_verde", "druida", "druida_idle_de_frente_con_poder", 6, "druida_verde", new Color(0.38f, 0.88f, 0.32f)),
        ("rayo", "Mago del Rayo", "mago_del_rayo", "rayo", "rayo_idle_de_frente_con_poder", 6, "mago_rayo", new Color(0.32f, 0.62f, 1f)),
    };

    static readonly string[] Letters = { "E", "N", "D", "R" };
    static readonly string[] Owls = { "Buho_Cafe_Oscuro", "Buho_Beige_Claro", "Buho_Marron_Miel", "Buho_Juez", "Buho_Marron_Miel", "Buho_Beige_Claro", "Buho_Cafe_Oscuro" };

    static readonly Vector2[] Line = { new Vector2(-4.8f, -0.2f), new Vector2(-1.6f, -0.2f), new Vector2(1.6f, -0.2f), new Vector2(4.8f, -0.2f) };
    static readonly Vector2[] Diamond = { new Vector2(0f, 1.25f), new Vector2(-3.6f, -0.2f), new Vector2(3.6f, -0.2f), new Vector2(0f, -1.65f) };
    static readonly Vector2[] Square = { new Vector2(-2.4f, 1.05f), new Vector2(2.4f, 1.05f), new Vector2(-2.4f, -1.45f), new Vector2(2.4f, -1.45f) };
    static readonly Vector2[] Flower = { new Vector2(0f, 1.15f), new Vector2(-3f, -0.25f), new Vector2(3f, -0.25f), new Vector2(0f, -1.65f) };
    static readonly Vector2 FlowerCenter = new Vector2(0f, -0.25f);
    static readonly Vector2[] DivingBoard = { new Vector2(-10.25f, 2f), new Vector2(-9.15f, 2f), new Vector2(-10.15f, 0.8f), new Vector2(-9.05f, 0.8f) };
    static readonly float[] LaneX = { 8.875f, 10.495f, 12.115f, 13.735f };
    const float LineY = -4.7f, FallHeight = 10.4f, LaneWidth = 1.45f;

    [MenuItem("Magolimpiadas/Natación artística/Preparar nivel")]
    static void Menu()
    {
        bool exists = Root(LevelName) != null;
        bool redo = !exists || EditorUtility.DisplayDialog("Natación artística",
            "Ya existe \"" + LevelName + "\".\n\n¿Lo rehago? Se borra SOLO ese objeto y se crea de nuevo.", "Rehacer el nivel", "Dejarlo como está");
        Debug.Log("[Natación artística] " + Prepare(redo) + " Guarda la escena (Ctrl+S).");
    }

    public static string Prepare(bool redoLevel)
    {
        var report = new List<string>();
        Directory.CreateDirectory(Generated);
        Directory.CreateDirectory(Data);
        AssetDatabase.Refresh();

        var background = Unique(Background, 64f, FilterMode.Point);
        var judgeSprites = new Dictionary<string, Sprite[]>();
        foreach (var b in Owls.Distinct()) judgeSprites[b] = Grid(Art + "/Jurado/" + b + ".png", 4, 2, 64f);
        var medals = Cut(Podium + "/medallas/medallas_sincronizado.png", 3, 64f, 0.5f);
        var shapes = SaveShapes();
        report.Add("sprites listos (fondo: " + (background != null) + ", búhos: " + judgeSprites.Values.Count(v => v.Length == 8) + "/4, medallas: " + medals.Length + ")");

        var settings = Ensure<SwimmingSettings>(Data + "/AjustesNatacion.asset");
        if (Mathf.Approximately(settings.botAccuracy, 0.95f) && Mathf.Approximately(settings.botsPerfect, 0.7f))
        {
            settings.botAccuracy = 0.97f; settings.botsPerfect = 0.8f; settings.botWhiteAccuracy = 0.985f;
            EditorUtility.SetDirty(settings);
        }
        var routine = Ensure<SwimmingRoutine>(Data + "/RutinaNatacion.asset");
        var song = AssetDatabase.LoadAssetAtPath<AudioClip>(Song);
        if (song != null && routine.song != song)
        {
            routine.song = song;
            routine.bpm = SongBpm;
            routine.songOffset = SongOffset;
            RoutineGenerator.Generate(routine);
            report.Add("rutina hecha para la canción (" + SongBpm + " BPM)");
        }
        else if (routine.figures.Count == 0) RoutineGenerator.Generate(routine);
        report.Add(RoutineGenerator.Validate(routine));

        var root = Root(LevelName);
        ArtisticSwimmingLevel level;
        if (root != null && !redoLevel) level = root.GetComponent<ArtisticSwimmingLevel>();
        else
        {
            if (root != null) Undo.DestroyObjectImmediate(root);
            level = CreateLevel(background, judgeSprites, shapes, settings, routine, medals);
            report.Add("creé \"" + LevelName + "\" en " + LevelPosition);
        }

        if (level != null) report.Add(CreateEntrance(level));

        report.Add(PrepareCeremony(medals));
        report.Add(EnsureAudio());

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return string.Join("; ", report) + ".";
    }

    static ArtisticSwimmingLevel CreateLevel(Sprite background, Dictionary<string, Sprite[]> judges, Shapes shapes, SwimmingSettings settings, SwimmingRoutine routine, Sprite[] medals)
    {
        var fTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        var fText = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);

        var root = new GameObject(LevelName);
        Undo.RegisterCreatedObjectUndo(root, "Natación artística");
        root.transform.position = LevelPosition;
        var level = root.AddComponent<ArtisticSwimmingLevel>();
        level.settings = settings;
        level.routine = routine;
        level.medals = medals;
        level.laneId = Mages.Select(m => m.id).ToArray();
        level.laneName = Mages.Select(m => m.displayName).ToArray();
        var audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        level.music = root.AddComponent<SwimmingMusic>();
        level.music.volume = 0.85f;

        var mural = Child("Mural_P5", root.transform, Vector3.zero);
        level.content = mural.gameObject;
        Sr("Fondo", mural, Vector3.zero, background, -100);

        var spawns = new Transform[4];
        for (int i = 0; i < 4; i++) spawns[i] = Child("MuralSpawn_" + (i + 1), mural, DivingBoard[i]);
        Child("DivingBoard", mural, new Vector3(-9.7f, 2f, 0f));
        var formations = Child("Formaciones", mural, Vector3.zero);
        Transform[] Points(string displayName, Vector2[] pos) => Enumerable.Range(0, 4).Select(i => Child(displayName + "_" + (i + 1), formations, pos[i])).ToArray();

        var swimmers = Child("Nadadores", mural, Vector3.zero);
        var formation = swimmers.gameObject.AddComponent<SwimmersFormation>();
        formation.line = Points("Formation_Line", Line);
        formation.diamond = Points("Formation_Diamond", Diamond);
        formation.square = Points("Formation_Square", Square);
        formation.flower = Points("Formation_Flower", Flower);
        formation.flowerCenter = Child("Flower_Center", formations, FlowerCenter);
        formation.referencePpu = MagesPpu;
        formation.waterHeight = new Vector2(-1.9f, 2.4f);
        for (int i = 0; i < 4; i++)
        {
            var m = Mages[i];
            string folder = Temple + "/" + m.folder + "/" + m.prefix + "_";
            var n = new SwimmersFormation.Swimmer
            {
                id = m.id, displayName = m.displayName, color = m.color, spawn = spawns[i],
                swimForward = Anim(folder + "nadar_de_frente.png", 6, 9f, true),
                swimSide = Anim(folder + "nadar_de_lado.png", 6, 10f, true),
                pose = Anim(Temple + "/" + m.folder + "/" + m.pose + ".png", m.poseFrames, 10f, true),
                fromFront = Anim(folder + "idle_hacia_abajo.png", 6, 6f, true),
                celebrate = new SpriteAnimation { frames = Celebration(m.podium), fps = 10f, repeat = true },
            };
            n.body = Sr(m.displayName, swimmers, (Vector3)DivingBoard[i], n.fromFront.frames.FirstOrDefault(), 100);
            n.ripple = Sr("Onda " + m.displayName, swimmers, (Vector3)Line[i], shapes.ripple, 95);
            n.ripple.color = new Color(1f, 1f, 1f, 0f);
            formation.swimmers.Add(n);
        }
        for (int i = 0; i < 4; i++)
        {
            var p = Sr("Pétalo " + (i + 1), swimmers, (Vector3)FlowerCenter, shapes.petal, 90);
            p.color = Color.clear;
            formation.petals[i] = p;
        }
        formation.waveBand = Sr("Ola", swimmers, new Vector3(-9f, 0.2f, 0f), shapes.wave, 92);
        formation.waveBand.transform.localScale = new Vector3(1.7f, 1.4f, 1f);
        level.formation = formation;

        var laneObjects = Child("Carriles", mural, Vector3.zero);
        var lanes = laneObjects.gameObject.AddComponent<NoteLanes>();
        lanes.colors = Mages.Select(m => m.color).ToArray();
        lanes.fallHeight = FallHeight;
        lanes.laneWidth = LaneWidth;
        float centerX = (LaneX[0] + LaneX[3]) * 0.5f;
        var panel = Sr("Panel carriles", laneObjects, new Vector3(centerX, 0.15f, 0f), shapes.panel, 40);
        Stretch(panel, new Vector2(LaneX[3] - LaneX[0] + LaneWidth + 0.5f, 12f), new Color(0.06f, 0.03f, 0.16f, 0.72f));
        for (int i = 0; i < 4; i++)
        {
            var lane = Child("NoteLane_" + (i + 1), laneObjects, new Vector3(LaneX[i], LineY, 0f));
            lanes.lanes[i] = lane;
            var laneBackground = Sr("Fondo", lane, new Vector3(0f, 5.1f, 0f), shapes.panel, 41);
            var cf = Mages[i].color * 0.45f; cf.a = 0.45f;
            Stretch(laneBackground, new Vector2(LaneWidth, 11.2f), cf);
            var beam = Sr("Haz", lane, Vector3.zero, shapes.beam, 45);
            beam.transform.localScale = new Vector3(LaneWidth * 0.9f, 9f / 4f, 1f);
            lanes.beams[i] = beam;
            var bullseye = Sr("Diana", lane, Vector3.zero, shapes.hoop, 70);
            bullseye.color = Mages[i].color;
            bullseye.transform.localScale = Vector3.one * 1.1f;
            lanes.bullseyes[i] = bullseye;
            var letter = WorldText("Letra", lane, new Vector3(0f, -0.95f, 0f), Letters[i], fTitle, 4.5f, Color.Lerp(Mages[i].color, Color.white, 0.3f), 75);
            letter.outlineWidth = 0.2f;
        }
        var line = Sr("HitLine", laneObjects, new Vector3(centerX, LineY, 0f), shapes.panel, 50);
        Stretch(line, new Vector2(LaneX[3] - LaneX[0] + LaneWidth + 0.2f, 0.16f), new Color(1f, 0.82f, 0.35f, 0.95f));
        lanes.hitLine = line.transform;
        var frame = Sr("Marco jugador", laneObjects, new Vector3(LaneX[0], 0.6f, 0f), shapes.frame, 43);
        Stretch(frame, new Vector2(LaneWidth + 0.18f, 11.9f), Color.white);
        lanes.playerFrame = frame;
        lanes.playerLabel = WorldText("Etiqueta jugador", laneObjects, new Vector3(LaneX[0], LineY - 1.6f, 0f), "Tú", fTitle, 5f, Color.white, 76);
        level.lanes = lanes;

        var jury = Child("JuryBox", mural, new Vector3(-0.2f, -6.45f, 0f));
        var juryComponent = jury.gameObject.AddComponent<OwlsJury>();
        var bench = Sr("Banco", jury, Vector3.zero, shapes.panel, 20);
        Stretch(bench, new Vector2(12.9f, 2.2f), new Color(0.06f, 0.03f, 0.16f, 0.62f));
        WorldText("Título jurado", jury, new Vector3(-5.6f, 1.25f, 0f), "Jurado", fText, 3.2f, new Color(0.85f, 0.8f, 1f), 26);
        for (int i = 0; i < 7; i++)
        {
            var frameSprites = judges.TryGetValue(Owls[i], out var s) ? s : new Sprite[0];
            float x = (i - 3) * 1.75f;
            var owl = Sr("Owl_" + (i + 1), jury, new Vector3(x, -0.95f, 0f), frameSprites.FirstOrDefault(), 30);
            owl.transform.localScale = Vector3.one * (i == 3 ? 1.6f : 1.45f);
            var note = WorldText("Nota " + (i + 1), jury, new Vector3(x, 1.0f, 0f), "", fTitle, 4.2f, Color.white, 32);
            note.outlineWidth = 0.25f;
            juryComponent.owls.Add(new OwlsJury.Owl { body = owl, note = note, frameSprites = frameSprites });
        }
        level.jury = juryComponent;

        var effects = Child("Efectos", mural, Vector3.zero).gameObject.AddComponent<SwimmingEffects>();
        effects.font = fTitle;
        effects.effectsScale = 1f;
        effects.laneId = level.laneId;
        level.effects = effects;

        var cam = Child("Cam_P5", root.transform, new Vector3(0f, 0f, -10f)).gameObject.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 16.875f * 0.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.05f, 0.2f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.depth = 10;
        cam.enabled = false;
        level.sceneCamera = cam;

        level.hud = CreateHud(root.transform, fTitle, fText);
        return level;
    }

    static SwimmingHud CreateHud(Transform parent, TMP_FontAsset fTitle, TMP_FontAsset fText)
    {
        var go = new GameObject("HUD Natación", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920, 1080);
        esc.matchWidthOrHeight = 0.5f;
        var hud = go.AddComponent<SwimmingHud>();
        hud.climber = esc;

        var frame = new GameObject("Marco 16:9", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(go.transform, false);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(1920f, 1080f);
        hud.frame = frame;

        var band = Box("Banda figura", frame, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(860f, 132f), new Color(0.05f, 0.02f, 0.14f, 0.78f));
        hud.figure = Txt("Figura", band, fTitle, 50f, new Color(1f, 0.86f, 0.42f), "Figura 1 · Línea", new Vector2(0f, -8f), new Vector2(840f, 64f), new Vector2(0.5f, 1f));
        hud.subtitle = Txt("Subtítulo", band, fText, 22f, new Color(0.85f, 0.8f, 1f), "Natación artística · La Piscina de la Luna", new Vector2(0f, -66f), new Vector2(840f, 30f), new Vector2(0.5f, 1f));
        var bar = Box("Barra progreso", band, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(760f, 16f), new Color(1f, 1f, 1f, 0.15f));
        var fill = Box("Relleno", bar, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 0.84f, 0.35f));
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f); fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        hud.progress = fill;
        hud.progressFill = fill.GetComponent<Image>();
        for (int i = 0; i < 4; i++)
        {
            var mark = Box("Marca " + (i + 1), bar, new Vector2(0f, 0.5f), new Vector2(760f * (i + 1) / 4f - 2f, 0f), new Vector2(6f, 26f), new Color(1f, 1f, 1f, 0.35f));
            mark.pivot = new Vector2(1f, 0.5f);
            hud.marks[i] = mark.GetComponent<Image>();
        }
        hud.note = Txt("Nota", frame, fTitle, 40f, Color.white, "", new Vector2(0f, -156f), new Vector2(1000f, 56f), new Vector2(0.5f, 1f));

        var panel = Box("Panel puntos", frame, new Vector2(0f, 1f), new Vector2(24f, -78f), new Vector2(400f, 150f), new Color(0.05f, 0.02f, 0.14f, 0.72f));
        panel.pivot = new Vector2(0f, 1f);
        hud.points = Txt("Puntos", panel, fTitle, 40f, Color.white, "Puntos 0", new Vector2(20f, -10f), new Vector2(380f, 52f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        hud.combo = Txt("Combo", panel, fTitle, 32f, new Color(0.7f, 0.95f, 1f), "", new Vector2(20f, -62f), new Vector2(240f, 44f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        var icon = new GameObject("Estrella", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(panel, false);
        var ii = icon.GetComponent<Image>();
        ii.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarIcon);
        ii.color = new Color(1f, 0.85f, 0.3f);
        ii.preserveAspect = true;
        ii.raycastTarget = false;
        var ri = (RectTransform)icon.transform;
        ri.anchorMin = ri.anchorMax = ri.pivot = new Vector2(0f, 1f);
        ri.anchoredPosition = new Vector2(20f, -106f);
        ri.sizeDelta = new Vector2(36f, 36f);
        hud.stars = Txt("Estrellas", panel, fTitle, 32f, new Color(1f, 0.88f, 0.45f), "x 0", new Vector2(64f, -104f), new Vector2(200f, 42f), new Vector2(0f, 1f), TextAlignmentOptions.Left);

        var messageBackground = Box("Fondo mensaje", frame, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(0f, 230f), new Color(0.03f, 0.01f, 0.1f, 0.55f));
        messageBackground.anchorMin = new Vector2(0f, 0.5f); messageBackground.anchorMax = new Vector2(1f, 0.5f);
        messageBackground.sizeDelta = new Vector2(0f, 230f);
        messageBackground.gameObject.SetActive(false);
        hud.messageBackground = messageBackground.gameObject;
        hud.message = Txt("Mensaje", frame, fTitle, 100f, new Color(1f, 0.9f, 0.55f), "", new Vector2(0f, 40f), new Vector2(1600f, 300f), new Vector2(0.5f, 0.5f));
        hud.message.outlineWidth = 0.25f;
        hud.message.outlineColor = new Color32(30, 10, 60, 255);

        hud.help = Txt("Ayuda", frame, fText, 24f, new Color(1f, 1f, 1f, 0.9f), "", new Vector2(0f, 12f), new Vector2(1800f, 36f), new Vector2(0.5f, 0f));
        hud.help.outlineWidth = 0.2f;
        hud.help.outlineColor = new Color32(15, 8, 35, 255);
        go.SetActive(false);
        return hud;
    }

    static string CreateEntrance(ArtisticSwimmingLevel level)
    {
        var race = Object.FindFirstObjectByType<SwimmingRace>(FindObjectsInactive.Include);
        if (race == null) return "no encontré la carrera de natación (CarreraNatacion): falta la entrada";
        var go = Root(EntranceName);
        if (go == null)
        {
            go = new GameObject(EntranceName);
            Undo.RegisterCreatedObjectUndo(go, "Entrada natación artística");
            var group = race.transform.parent;
            if (group != null) go.transform.SetParent(group, false);
        }
        go.transform.SetPositionAndRotation(race.transform.position, race.transform.rotation);
        var raceBox = race.GetComponent<BoxCollider>();
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = Undo.AddComponent<BoxCollider>(go);
        box.isTrigger = true;
        box.center = raceBox != null ? raceBox.center : Vector3.zero;
        box.size = raceBox != null ? raceBox.size : new Vector3(10f, 2.4f, 40f);
        var entrance = go.GetComponent<ArtisticSwimmingEntrance>();
        if (entrance == null) entrance = Undo.AddComponent<ArtisticSwimmingEntrance>(go);
        entrance.level = level;
        entrance.race = race;
        entrance.edge = race.arrival;
        EditorUtility.SetDirty(entrance);
        return "entrada lista sobre la piscina (después de la carrera)";
    }

    static string PrepareCeremony(Sprite[] medals)
    {
        var c = Object.FindFirstObjectByType<PodiumCeremony>(FindObjectsInactive.Include);
        if (c == null) return "no encontré la ceremonia del podio";
        Undo.RecordObject(c, "Ceremonia");
        c.syncedMedals = medals;
        if (c.testsWithGap == null || !c.testsWithGap.Contains(GapTest))
        {
            c.testsWithGap = (c.testsWithGap ?? new string[0]).Concat(new[] { GapTest }).ToArray();
            c.gapNames = (c.gapNames ?? new string[0]).Concat(new[] { "Artística" }).ToArray();
        }
        EditorUtility.SetDirty(c);
        return "ceremonia del podio con las medallas de natación artística";
    }

    public static string EnsureAudio()
    {
        if (Object.FindFirstObjectByType<ArcaneGames.AudioManager>(FindObjectsInactive.Include) != null) return "la escena ya tiene AudioManager";
        var go = new GameObject("Audio del juego");
        Undo.RegisterCreatedObjectUndo(go, "Audio");
        var music = go.AddComponent<AudioSource>();
        music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Magolimpiadas/Audio/Musica/Mus_Menu.mp3");
        music.loop = true;
        music.playOnAwake = false;
        var effects = go.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        var am = go.AddComponent<ArcaneGames.AudioManager>();
        var so = new SerializedObject(am);
        so.FindProperty("musicSource").objectReferenceValue = music;
        so.FindProperty("sfxSource").objectReferenceValue = effects;
        so.FindProperty("buttonClickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Magolimpiadas/Audio/SFX/SFX_Click.wav");
        so.ApplyModifiedPropertiesWithoutUndo();
        return "añadí \"Audio del juego\" (música y clic)";
    }

    class Shapes { public Sprite panel, hoop, beam, ripple, petal, wave, frame; }

    static Shapes SaveShapes()
    {
        return new Shapes
        {
            panel = Save("panel", PixelShapes.Panel(), FilterMode.Bilinear, 32f, new Vector2(0.5f, 0.5f), new Vector4(10, 10, 10, 10)),
            hoop = Save("aro", PixelShapes.Hoop(32, 3), FilterMode.Point, 32f, new Vector2(0.5f, 0.5f), Vector4.zero),
            beam = Save("haz", PixelShapes.Beam(), FilterMode.Bilinear, 16f, new Vector2(0.5f, 0f), Vector4.zero),
            ripple = Save("onda", PixelShapes.Ripple(), FilterMode.Bilinear, 64f, new Vector2(0.5f, 0.5f), Vector4.zero),
            petal = Save("petalo", PixelShapes.Petal(), FilterMode.Bilinear, 32f, new Vector2(0.5f, 0.05f), Vector4.zero),
            wave = Save("ola", PixelShapes.WaveBand(), FilterMode.Bilinear, 16f, new Vector2(0.5f, 0.5f), Vector4.zero),
            frame = Save("marco", Frame(), FilterMode.Bilinear, 32f, new Vector2(0.5f, 0.5f), new Vector4(12, 12, 12, 12)),
        };
    }

    static Sprite Frame()
    {
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                float a = d < 3f ? 1f : Mathf.Clamp01(1f - (d - 3f) / 6f) * 0.5f;
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 32f);
    }

    static Sprite Save(string displayName, Sprite origin, FilterMode filter, float ppu, Vector2 pivotPoint, Vector4 edge)
    {
        string path = Generated + "/" + displayName + ".png";
        File.WriteAllBytes(path, origin.texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = filter;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivotPoint;
        settings.spriteBorder = edge;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static SpriteAnimation Anim(string path, int frameSprites, float fps, bool repeat) =>
        new SpriteAnimation { frames = Cut(path, frameSprites, MagesPpu, -1f), fps = fps, repeat = repeat };

    static Sprite[] Celebration(string file)
    {
        string path = Podium + "/personajes/" + file + "_celebracion.png";
        var s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
        if (s.Count < 2) return Cut(path, 6, 64f, -1f);
        return s.OrderBy(x => Number(x.name)).ToArray();
    }

    static int Number(string displayName) => int.TryParse(displayName.Substring(displayName.LastIndexOf('_') + 1), out int k) ? k : 0;

    static Sprite Unique(string path, float ppu, FilterMode filter)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[Natación artística] No encontré " + path); return null; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = filter;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite[] Cut(string path, int n, float ppu, float pivotY)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[Natación artística] No encontré " + path); return new Sprite[0]; }
        ti.GetSourceTextureWidthAndHeight(out int W, out int H);
        float py = pivotY >= 0f ? pivotY : NormalizedFeet(path);
        var rects = Enumerable.Range(0, n).Select(i => (Path.GetFileNameWithoutExtension(path) + "_" + i, new Rect(i * (W / n), 0, W / n, H))).ToArray();
        return Apply(ti, path, rects, new Vector2(0.5f, py), ppu);
    }

    static Sprite[] Grid(string path, int columns, int rows, float ppu)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[Natación artística] No encontré " + path); return new Sprite[0]; }
        ti.GetSourceTextureWidthAndHeight(out int W, out int H);
        int cw = W / columns, ch = H / rows;
        string b = Path.GetFileNameWithoutExtension(path);
        var rects = new List<(string, Rect)>();
        for (int f = 0; f < rows; f++)
            for (int c = 0; c < columns; c++)
                rects.Add((b + "_" + (f * columns + c), new Rect(c * cw, (rows - 1 - f) * ch, cw, ch)));
        return Apply(ti, path, rects.ToArray(), new Vector2(0.5f, 0.06f), ppu);
    }

    static Sprite[] Apply(TextureImporter ti, string path, (string displayName, Rect rect)[] rects, Vector2 pivotPoint, float ppu)
    {
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
        var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        var newItems = rects.Select(r => new SpriteRect
        {
            name = r.displayName, rect = r.rect, alignment = SpriteAlignment.Custom, pivot = pivotPoint,
            spriteID = ids.TryGetValue(r.displayName, out var id) ? id : GUID.Generate(),
        }).ToArray();
        dp.SetSpriteRects(newItems);
        var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idNames != null) idNames.SetNameFileIdPairs(newItems.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
        var byName = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        return rects.Select(r => byName.TryGetValue(r.displayName, out var s) ? s : null).Where(s => s != null).ToArray();
    }

    static float NormalizedFeet(string path)
    {
        var tex = new Texture2D(2, 2);
        if (!tex.LoadImage(File.ReadAllBytes(path))) { Object.DestroyImmediate(tex); return 0.04f; }
        var px = tex.GetPixels32();
        int w = tex.width, h = tex.height, down = 0;
        for (int y = 0; y < h && down == 0; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > 40) { down = Mathf.Max(1, y); break; }
        Object.DestroyImmediate(tex);
        return Mathf.Clamp01(down / (float)h);
    }

    static T Ensure<T>(string path) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    static GameObject Root(string n) => EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == n)
        ?? Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(t => t.gameObject).FirstOrDefault(g => g.name == n);

    static Transform Child(string displayName, Transform parent, Vector3 local)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        return go.transform;
    }

    static Transform Child(string displayName, Transform parent, Vector2 local) => Child(displayName, parent, (Vector3)local);

    static SpriteRenderer Sr(string displayName, Transform parent, Vector3 local, Sprite s, int order)
    {
        var sr = Child(displayName, parent, local).gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }

    static void Stretch(SpriteRenderer sr, Vector2 sizeValue, Color c)
    {
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = sizeValue;
        sr.color = c;
    }

    static TextMeshPro WorldText(string displayName, Transform parent, Vector3 local, string text, TMP_FontAsset f, float sizeValue, Color c, int order)
    {
        var go = Child(displayName, parent, local).gameObject;
        var t = go.AddComponent<TextMeshPro>();
        if (f != null) t.font = f;
        t.text = text;
        t.fontSize = sizeValue;
        t.color = c;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
        t.sortingOrder = order;
        t.outlineColor = new Color32(20, 8, 40, 255);
        return t;
    }

    static RectTransform Box(string displayName, Transform parent, Vector2 anchor, Vector2 pos, Vector2 sizeValue, Color c)
    {
        var go = new GameObject(displayName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeValue;
        return rt;
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
}
