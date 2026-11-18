using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static partial class PrepareMages2D
{
    const string Root = "Assets/_Magolimpiadas/Templo";
    const string DataFolder = "Assets/_Magolimpiadas/Templo/Magos";
    const string MapName = "Mapa - Magolimpiadas (según tu boceto)";
    const float CharactersScale = 0.62f;
    static float CharactersPpu => 64f / CharactersScale;

    struct Sheet
    {
        public string file; public int frames; public float fps; public bool repeat;
        public Sheet(string a, int n, float f, bool r = true) { file = a; frames = n; fps = f; repeat = r; }
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar los 4 magos (Nebulosa jugador + 3 bots)")]
    public static void PrepareAllFour()
    {
        var neb = NebulaToken();
        var ele = ElementalToken();
        var dru = DruidToken();
        var ray = LightningToken();
        AssetDatabase.SaveAssets();

        var player = PutMage(neb, "Inicio jugador - Maga Nebulosa", null, 0f);
        if (player == null) return;
        PutMage(ele, "Inicio jugador - Mago Elemental", player, 1.6f);
        PutMage(dru, "Inicio jugador - Druida Verde", player, 3.0f);
        PutMage(ray, "Inicio jugador - Mago del Rayo", player, 4.4f);
        MarkClimbableWalls();
        Selection.activeGameObject = player.gameObject;
        Debug.Log("Listos los 4 magos: la Maga Nebulosa la maneja el jugador y los otros 3 son bots. Guarda la escena (Ctrl+S).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Actualizar fichas de los magos (animaciones)")]
    public static void RefreshTokens()
    {
        NebulaToken(); ElementalToken(); DruidToken(); LightningToken();
        AssetDatabase.SaveAssets();
        Debug.Log("Fichas de los 4 magos actualizadas (no se tocó la escena).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar Maga Nebulosa (jugador)")]
    public static void PrepareNebula()
    {
        var d = NebulaToken();
        AssetDatabase.SaveAssets();
        PutMage(d, "Inicio jugador - Maga Nebulosa", null, 0f);
        MarkClimbableWalls();
    }

    static MageData NebulaToken()
    {
        string c = Root + "/personajes/maga_nebulosa/";
        var d = Token("DatosMago_Nebulosa");
        d.displayName = "Maga Nebulosa"; d.element = MagicElement.Nebula; d.color = new Color(0.72f, 0.38f, 1f);
        Animations(d, c,
            new Sheet("nebulosa_idle_de_lado_con_poder.png", 6, 8), new Sheet("nebulosa_caminar_de_lado.png", 6, 10),
            new Sheet("nebulosa_correr_de_lado.png", 6, 13), new Sheet("nebulosa_ataque_de_lado.png", 5, 12, false), 4,
            new Sheet("nebulosa_idle_con_arco_de_lado.png", 6, 8), new Sheet("nebulosa_caminar_con_arco.png", 6, 10),
            new Sheet("nebulosa_correr_con_arco.png", 6, 13), new Sheet("nebulosa_arco_de_lado.png", 6, 16, false), 4);
        d.power = new ProjectileData { element = MagicElement.Nebula, color = d.color, speed = 14f, reach = 12f, size = 0.7f, damage = 10f };
        d.shotPoint = new Vector2(1.0f, 1.05f);
        d.hasSecondaryPower = false;
        d.arrow = Arrows()[1];
        d.climb = Anim(c, new Sheet("nebulosa_escalar_con_arco.png", 6, 10));
        EditorUtility.SetDirty(d);
        return d;
    }

    static MageData ElementalToken()
    {
        string c = Root + "/personajes/mago_elemental/";
        var d = Token("DatosMago_Elemental");
        d.displayName = "Mago Elemental"; d.element = MagicElement.Fire; d.color = new Color(1f, 0.5f, 0.15f);
        Animations(d, c,
            new Sheet("elemental_idle_con_poderes.png", 4, 6), new Sheet("elemental_caminar_de_lado.png", 4, 8),
            new Sheet("elemental_correr_de_lado.png", 6, 13), new Sheet("elemental_fuego_de_lado.png", 6, 12, false), 3,
            new Sheet("elemental_idle_con_arco_de_lado.png", 6, 8), new Sheet("elemental_caminar_con_arco.png", 6, 10),
            new Sheet("elemental_correr_con_arco.png", 6, 13), new Sheet("elemental_arco_de_lado.png", 6, 16, false), 4);
        d.power = new ProjectileData { element = MagicElement.Fire, color = d.color, speed = 14f, reach = 12f, size = 0.7f, damage = 10f };
        d.hasSecondaryPower = true;
        d.secondaryPower = new ProjectileData { element = MagicElement.Ice, color = new Color(0.55f, 0.85f, 1f), speed = 12f, reach = 10f, size = 0.65f, damage = 6f, slows = true };
        d.secondaryAttack = Anim(c, new Sheet("elemental_hielo_de_lado.png", 6, 12, false));
        d.secondaryLaunchFrame = 3;
        d.shotPoint = new Vector2(1.0f, 1.0f);
        d.arrow = Arrows()[0];
        d.climb = Anim(c, new Sheet("elemental_escalar_con_arco.png", 6, 10));
        EditorUtility.SetDirty(d);
        return d;
    }

    static MageData DruidToken()
    {
        string c = Root + "/personajes/druida_verde/";
        var d = Token("DatosMago_Druida");
        d.displayName = "Druida Verde"; d.element = MagicElement.Plant; d.color = new Color(0.55f, 0.9f, 0.2f);
        Animations(d, c,
            new Sheet("druida_idle_de_lado_con_poder.png", 6, 8), new Sheet("druida_caminar_de_lado.png", 6, 10),
            new Sheet("druida_correr_de_lado.png", 6, 13), new Sheet("druida_ataque_de_lado.png", 6, 12, false), 4,
            new Sheet("druida_idle_con_arco_de_lado.png", 6, 8), new Sheet("druida_caminar_con_arco.png", 6, 10),
            new Sheet("druida_correr_con_arco.png", 6, 13), new Sheet("druida_arco_de_lado.png", 6, 16, false), 4);
        d.power = new ProjectileData { element = MagicElement.Plant, color = d.color, speed = 13f, reach = 11f, size = 0.7f, damage = 10f };
        d.hasSecondaryPower = false;
        d.shotPoint = new Vector2(1.0f, 1.0f);
        d.arrow = Arrows()[2];
        d.climb = Anim(c, new Sheet("druida_escalar_con_arco.png", 6, 10));
        EditorUtility.SetDirty(d);
        return d;
    }

    static MageData LightningToken()
    {
        string c = Root + "/personajes/mago_del_rayo/";
        var d = Token("DatosMago_Rayo");
        d.displayName = "Mago del Rayo"; d.element = MagicElement.Lightning; d.color = new Color(0.3f, 0.75f, 1f);
        Animations(d, c,
            new Sheet("rayo_idle_de_lado_con_poder.png", 6, 8), new Sheet("rayo_caminar_de_lado.png", 6, 10),
            new Sheet("rayo_correr_de_lado.png", 6, 13), new Sheet("rayo_ataque_de_lado.png", 6, 12, false), 3,
            new Sheet("rayo_idle_con_arco_de_lado.png", 6, 8), new Sheet("rayo_caminar_con_arco.png", 6, 10),
            new Sheet("rayo_correr_con_arco.png", 6, 13), new Sheet("rayo_arco_de_lado.png", 6, 16, false), 4);
        d.power = new ProjectileData { element = MagicElement.Lightning, color = d.color, speed = 18f, reach = 12f, size = 0.6f, damage = 10f };
        d.hasSecondaryPower = false;
        d.shotPoint = new Vector2(1.1f, 1.05f);
        d.arrow = Arrows()[3];
        d.climb = Anim(c, new Sheet("rayo_escalar_con_arco.png", 6, 10));
        EditorUtility.SetDirty(d);
        return d;
    }

    static void Animations(MageData d, string c, Sheet idle, Sheet walk, Sheet run, Sheet attack, int launchAttack,
                            Sheet bowIdle, Sheet walkBow, Sheet runBow, Sheet shot, int launchArrow)
    {
        d.idle = Anim(c, idle);
        d.walk = Anim(c, walk);
        d.run = Anim(c, run);
        d.air = Frame(d.run, 0);
        d.attack = Anim(c, attack);
        d.attackLaunchFrame = launchAttack;
        d.bowIdle = Anim(c, bowIdle);
        d.walkBow = Anim(c, walkBow);
        d.runBow = Anim(c, runBow);
        d.airBow = Frame(d.runBow, 0);
        d.bowShot = Anim(c, shot);
        d.arrowLaunchFrame = launchArrow;
    }

    struct EnemyType
    {
        public MagicElement family; public float health, damage, speed; public Enemy.Movement mov; public bool bullseye, boss, facesLeft;
        public EnemyType(MagicElement f, float v, float d, Enemy.Movement m, float vel, bool hasBullseye, bool isBoss = false, bool leftEdge = false)
        { family = f; health = v; damage = d; mov = m; speed = vel; bullseye = hasBullseye; boss = isBoss; facesLeft = leftEdge; }
    }

    static readonly Dictionary<string, EnemyType> Types = new Dictionary<string, EnemyType>
    {
        { "slime_de_hielo",     new EnemyType(MagicElement.Ice,    30, 10, Enemy.Movement.Skip, 1.6f, true) },
        { "slime_del_rayo",     new EnemyType(MagicElement.Lightning,     30, 10, Enemy.Movement.Skip, 1.6f, true) },
        { "slime_de_fuego",     new EnemyType(MagicElement.Fire,    30, 10, Enemy.Movement.Skip, 1.6f, true) },
        { "slime_de_planta",    new EnemyType(MagicElement.Plant,   30, 10, Enemy.Movement.Skip, 1.6f, true) },
        { "slime_nebuloso",     new EnemyType(MagicElement.Nebula, 30, 10, Enemy.Movement.Skip, 1.6f, true) },
        { "slime_sombrio",      new EnemyType(MagicElement.Nebula, 60, 15, Enemy.Movement.Skip, 1.8f, true, true) },
        { "espiritu_de_planta", new EnemyType(MagicElement.Plant,   70, 15, Enemy.Movement.Hover, 1.4f, false, true) },
        { "golem_de_lava",      new EnemyType(MagicElement.Fire,    90, 20, Enemy.Movement.Walk, 1.2f, false, true, true) },
        { "golem_de_roca",      new EnemyType(MagicElement.None,  90, 20, Enemy.Movement.Walk, 1.2f, true) },
        { "pocion_carmesi",     new EnemyType(MagicElement.None,  40, 10, Enemy.Movement.Skip, 1.4f, false, true) },
        { "espectro_carmesi",   new EnemyType(MagicElement.None,  50, 15, Enemy.Movement.Hover, 1.6f, false, true) },
        { "cristal_del_rayo",   new EnemyType(MagicElement.Lightning,     80, 15, Enemy.Movement.Hover, 1.2f, false, true) },
    };

    static int ConfigureEnemies(Transform characters)
    {
        var bullseye = Cut(Root + "/personajes/enemigos/diana_de_madera.png", 6, CharactersPpu, -1f);
        var breaks = Cut(Root + "/personajes/enemigos/diana_de_madera_se_rompe.png", 6, CharactersPpu, -1f);
        int n = 0;
        foreach (Transform t in characters)
        {
            var anim = t.GetComponent<AnimatedSprite>();
            if (anim == null || anim.frames == null || anim.frames.Length == 0 || t.name.StartsWith("Inicio jugador")) continue;
            string sheet = System.Text.RegularExpressions.Regex.Replace(anim.frames[0].name, @"_\d+$", "");
            if (!Types.TryGetValue(sheet, out var type)) continue;

            var go = t.gameObject;
            var rb = Ensure<Rigidbody2D>(go);
            Undo.RecordObject(rb, "Preparar enemigos");
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = type.mov == Enemy.Movement.Hover ? 0f : 3f;
            var col = Ensure<CapsuleCollider2D>(go);
            var b = anim.frames[0].bounds;
            col.size = new Vector2(b.size.x * 0.5f, b.size.y * 0.55f);
            col.offset = new Vector2(0f, b.min.y + b.size.y * 0.04f + col.size.y * 0.5f);

            var e = Ensure<Enemy>(go);
            Undo.RecordObject(e, "Preparar enemigos");
            e.displayName = t.name;
            e.family = type.family;
            e.maxHealth = type.health;
            e.contactDamage = type.damage;
            e.movement = type.mov;
            e.speed = type.speed;
            e.isBoss = type.boss;
            e.detectionRadius = type.boss ? 10f : 7f;
            e.drawingFacesLeft = type.facesLeft;
            e.becomesTarget = type.bullseye;
            e.bullseyeFrames = type.bullseye ? bullseye : null;
            e.targetBreakFrames = type.bullseye ? breaks : null;
            e.drawing = go.GetComponent<SpriteRenderer>();
            n++;
        }
        return n;
    }

    [MenuItem("Magolimpiadas/Templo 2D/Actualizar enemigos (sin rehacer encuentros)")]
    public static void RefreshEnemies()
    {
        var map = GameObject.Find(MapName);
        var characters = map != null ? map.transform.Find("Personajes") : null;
        if (characters == null) { EditorUtility.DisplayDialog("Enemigos", "No encontré \"Personajes\" dentro del mapa.", "Ok"); return; }
        int n = ConfigureEnemies(characters);
        if (characters.Find("Dragón de hielo") != null) PrepareBoss();
        ApplyDeaths();
        EditorSceneManager.MarkSceneDirty(map.scene);
        Debug.Log("Enemigos actualizados: " + n + " (más el Dragón). Guarda la escena (Ctrl+S).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar enemigos y combate")]
    public static void PrepareEnemies()
    {
        var map = GameObject.Find(MapName);
        var characters = map != null ? map.transform.Find("Personajes") : null;
        if (characters == null) { EditorUtility.DisplayDialog("Enemigos", "No encontré \"Personajes\" dentro del mapa.", "Ok"); return; }
        int n = ConfigureEnemies(characters);

        var old = map.transform.Find("Combate");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var combat = new GameObject("Combate");
        Undo.RegisterCreatedObjectUndo(combat, "Preparar enemigos");
        combat.transform.SetParent(map.transform, false);
        Vector3 o0 = map.transform.position;
        Vector3 Tile(float x, float y) => o0 + new Vector3(x, -y, 0f);

        CreateCheckpoint(combat.transform, "Punto de control - Fase 1 (Llegada)", "Fase 1", Tile(7.5f, 21f), 14f);
        CreateCheckpoint(combat.transform, "Punto de control - Fase 2 (Lago)", "Fase 2", Tile(50.5f, 23f), 14f);

        var enc = new GameObject("Encuentro - Fase 2 (Lago)");
        enc.transform.SetParent(combat.transform, false);
        enc.transform.position = Tile(64f, 15f);
        var zone = enc.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(18f, 16f);
        var encounter = enc.AddComponent<Encounter>();
        encounter.barriers.Add(Barrier(enc.transform, "Barrera izquierda", Tile(52.5f, 23f)));
        encounter.barriers.Add(Barrier(enc.transform, "Barrera derecha", Tile(73.5f, 23f)));

        var follow = Object.FindFirstObjectByType<FollowCamera2D>();
        float background = follow != null ? follow.minLimit.y - 1f : o0.y - 32f;
        foreach (var m in Object.FindObjectsByType<Mage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(m, "Preparar enemigos");
            m.minHeight = background;
        }
        var textIndicator = Object.FindFirstObjectByType<MageTextIndicator>(FindObjectsInactive.Include);
        if (textIndicator != null && textIndicator.healthText == null)
        {
            Undo.RecordObject(textIndicator, "Preparar enemigos");
            textIndicator.healthText = CreateText(textIndicator.transform, "Vida", 30, new Vector2(34, -118), FontStyle.Bold);
            textIndicator.healthText.text = "Vida: 100 / 100";
        }

        EditorSceneManager.MarkSceneDirty(map.scene);
        if (characters.Find("Dragón de hielo") != null) PrepareBoss();
        ApplyDeaths();
        Selection.activeGameObject = enc;
        Debug.Log("Enemigos listos: " + n + ". Encuentro del lago y puntos de control creados en \"Combate\". " +
                  "Si ya tenías los eventos, vuelve a usar \"Preparar zonas de poder y eventos\" (rehace los puntos de control de las fases 3 a 8). Guarda la escena (Ctrl+S).");
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    static void CreateCheckpoint(Transform parent, string displayName, string phase, Vector3 feet, float heightValue)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(parent, false);
        go.transform.position = feet + new Vector3(0f, 0.05f, 0f);
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(1f, heightValue);
        c.offset = new Vector2(0f, heightValue * 0.5f - 1f);
        go.AddComponent<Checkpoint>().phase = phase;
    }

    static GameObject Barrier(Transform parent, string displayName, Vector3 feet)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(0.6f, 16f);
        c.offset = new Vector2(0f, 8f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BarrierSprite();
        sr.sortingOrder = 15;
        sr.color = new Color(0.75f, 0.45f, 1f, 0.3f);
        go.transform.localScale = new Vector3(1f, 1.75f, 1f);
        c.size = new Vector2(0.6f, 16f / 1.75f);
        c.offset = new Vector2(0f, 8f / 1.75f);
        return go;
    }

    static Sprite BarrierSprite()
    {
        string p = Root + "/_Generado/barrera_magica.png";
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
        if (s != null) return s;
        Folder(Root + "/_Generado");
        const int w = 24, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - w / 2f + 0.5f) / (w / 2f);
                float a = Mathf.Clamp01(1f - dx * dx) * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(y * 0.35f)));
                float center = Mathf.Clamp01(1f - dx * 3f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(a, center)));
            }
        tex.Apply();
        File.WriteAllBytes(p, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(p);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32f;
        ti.spritePivot = new Vector2(0.5f, 0f);
        var st = new TextureImporterSettings(); ti.ReadTextureSettings(st); st.spriteAlignment = (int)SpriteAlignment.BottomCenter; ti.SetTextureSettings(st);
        ti.filterMode = FilterMode.Bilinear;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    [MenuItem("Magolimpiadas/Templo 2D/Marcar paredes escalables")]
    public static void MarkClimbableWalls()
    {
        var map = GameObject.Find(MapName);
        var terrain = map != null ? map.transform.Find("Terreno") : null;
        if (terrain == null) { EditorUtility.DisplayDialog("Paredes escalables", "No encontré el \"Terreno\" del mapa en la escena.", "Ok"); return; }
        var p = terrain.GetComponent<ClimbableWalls>();
        if (p == null) p = Undo.AddComponent<ClimbableWalls>(terrain.gameObject);
        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        Selection.activeGameObject = terrain.gameObject;
        Debug.Log("Paredes escalables listas en \"Terreno\". Las piezas en verde son de ladrillo; edita la lista en el Inspector.");
    }

    static Mage PutMage(MageData data, string startName, Mage leader, float distance)
    {
        bool isPlayer = leader == null;
        var map = GameObject.Find(MapName);
        if (map == null) { EditorUtility.DisplayDialog("Preparar magos", "No encontré \"" + MapName + "\" en la escena.", "Ok"); return null; }
        var start = map.transform.Find("Personajes/" + startName);
        var camGo = map.transform.Find("Camara Mapa 2D");
        var change = Object.FindFirstObjectByType<WorldSwitcher>();
        if (start == null || camGo == null || change == null)
        {
            EditorUtility.DisplayDialog("Preparar magos", "Falta algo en la escena: el inicio \"" + startName + "\", la \"Camara Mapa 2D\" o el objeto \"Cambio de mundo\" (etapa 0).", "Ok");
            return null;
        }
        var cam = camGo.GetComponent<Camera>();

        foreach (var r in start.GetComponents<SpriteRenderer>()) { Undo.RecordObject(r, "Preparar magos"); r.enabled = false; }
        foreach (var a in start.GetComponents<AnimatedSprite>()) { Undo.RecordObject(a, "Preparar magos"); a.enabled = false; }

        string displayName = data.displayName;
        foreach (var suffix in new[] { "", " (jugador)", " (bot)" })
        {
            var old = map.transform.Find(data.displayName + suffix);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }
        var go = new GameObject(displayName);
        Undo.RegisterCreatedObjectUndo(go, "Preparar magos");
        go.transform.SetParent(map.transform, false);
        go.transform.position = start.position;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = WithoutFriction();
        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 2.1f);
        col.offset = new Vector2(0f, 1.05f);

        var drawing = new GameObject("Dibujo");
        drawing.transform.SetParent(go.transform, false);
        var sr = drawing.AddComponent<SpriteRenderer>();
        sr.sprite = data.idle.frames.FirstOrDefault();
        sr.sortingOrder = isPlayer ? 23 : 21;

        var mage = go.AddComponent<Mage>();
        mage.data = data;
        mage.body = sr;
        mage.startPoint = start;

        if (!isPlayer)
        {
            var bot = go.AddComponent<ControlBot>();
            bot.leader = leader;
            bot.distance = distance;
            mage.control = bot;
            Undo.RecordObject(change, "Preparar magos");
            var l = (change.items2D ?? new GameObject[0]).Where(o => o != null && o.name != displayName).ToList();
            l.Add(go);
            change.items2D = l.ToArray();
            EditorSceneManager.MarkSceneDirty(go.scene);
            return mage;
        }

        var control = go.AddComponent<PlayerControl>();
        control.sceneCamera = cam;
        mage.control = control;

        var follow = camGo.GetComponent<FollowCamera2D>();
        if (follow == null) follow = Undo.AddComponent<FollowCamera2D>(camGo.gameObject);
        Undo.RecordObject(follow, "Preparar magos");
        follow.target = go.transform;
        MapLimits(map, out follow.minLimit, out follow.maxLimit);

        var hud = CreateIndicator(mage);

        Undo.RecordObject(change, "Preparar magos");
        change.player2D = mage;
        change.start2D = start;
        var list = (change.items2D ?? new GameObject[0]).Where(o => o != null && o.name != displayName && o.name != hud.name).ToList();
        list.Add(go);
        list.Add(hud);
        change.items2D = list.ToArray();

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("Listo: " + displayName + " en " + start.position + ". Guarda la escena (Ctrl+S).");
        return mage;
    }

    static GameObject CreateIndicator(Mage mage)
    {
        const string displayName = "Interfaz Templo 2D";
        var old = GameObject.Find(displayName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var go = new GameObject(displayName);
        Undo.RegisterCreatedObjectUndo(go, "Preparar magos");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scale = go.AddComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.matchWidthOrHeight = 0.5f;

        var textIndicator = go.AddComponent<MageTextIndicator>();
        textIndicator.mage = mage;
        textIndicator.modeText = CreateText(go.transform, "Mago y modo", 38, new Vector2(32, -24), FontStyle.Bold);
        textIndicator.controlsText = CreateText(go.transform, "Controles", 26, new Vector2(34, -76), FontStyle.Normal);
        textIndicator.healthText = CreateText(go.transform, "Vida", 30, new Vector2(34, -118), FontStyle.Bold);
        textIndicator.modeText.text = mage.data.displayName + " · Modo: PODER";
        textIndicator.controlsText.text = textIndicator.controls;
        textIndicator.healthText.text = "Vida: 100 / 100";
        return go;
    }

    static Text CreateText(Transform parent, string displayName, int sizeValue, Vector2 pos, FontStyle style)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = sizeValue;
        t.fontStyle = style;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        var shadow = go.AddComponent<Outline>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1200f, sizeValue * 1.5f);
        return t;
    }

    static void MapLimits(GameObject map, out Vector2 min, out Vector2 max)
    {
        bool has = false;
        Bounds b = new Bounds();
        foreach (var tm in map.GetComponentsInChildren<Tilemap>())
        {
            tm.CompressBounds();
            if (tm.cellBounds.size.x == 0) continue;
            var wb = new Bounds(tm.transform.TransformPoint(tm.localBounds.center), tm.transform.TransformVector(tm.localBounds.size));
            if (!has) { b = wb; has = true; } else b.Encapsulate(wb);
        }
        if (!has) b = new Bounds(map.transform.position + new Vector3(101f, -15.5f), new Vector3(202f, 31f));
        min = b.min; max = b.max;
        Vector3 corner = map.transform.position;
        min.x = Mathf.Min(min.x, corner.x);
        max.y = Mathf.Max(max.y, corner.y);
    }

    static PhysicsMaterial2D WithoutFriction()
    {
        string p = Root + "/Materiales/Mago_SinFriccion.physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(p);
        if (m != null) return m;
        Folder(Root + "/Materiales");
        m = new PhysicsMaterial2D("Mago_SinFriccion") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static MageData Token(string displayName)
    {
        Folder(DataFolder);
        string p = DataFolder + "/" + displayName + ".asset";
        var d = AssetDatabase.LoadAssetAtPath<MageData>(p);
        if (d == null) { d = ScriptableObject.CreateInstance<MageData>(); AssetDatabase.CreateAsset(d, p); }
        return d;
    }

    static SpriteAnimation Anim(string folder, Sheet h)
    {
        return new SpriteAnimation { frames = Cut(folder + h.file, h.frames, CharactersPpu, -1f), fps = h.fps, repeat = h.repeat };
    }

    static SpriteAnimation Frame(SpriteAnimation source, int i)
    {
        var f = source.frames != null && source.frames.Length > i ? new[] { source.frames[i] } : new Sprite[0];
        return new SpriteAnimation { frames = f, fps = 1f, repeat = true };
    }

    static Sprite[] Arrows()
    {
        return Cut(Root + "/assets/objetos/flecha_der.png", 4, 64f, 0.5f);
    }

    static Sprite[] Cut(string path, int n, float ppu, float pivotY)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("No encontré " + path); return new Sprite[0]; }
        PngSize(path, out int W, out int H);
        int cw = W / n;
        var pivot = new Vector2(0.5f, pivotY >= 0f ? pivotY : 10f / H);
        string b = Path.GetFileNameWithoutExtension(path);

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
        var rects = new List<SpriteRect>();
        for (int c = 0; c < n; c++)
        {
            string displayName = b + "_" + c;
            rects.Add(new SpriteRect
            {
                name = displayName,
                rect = new Rect(c * cw, 0, cw, H),
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

        var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        var list = new List<Sprite>();
        for (int c = 0; c < n; c++) if (all.TryGetValue(b + "_" + c, out var s)) list.Add(s);
        if (list.Count != n) Debug.LogWarning("En " + path + " esperaba " + n + " cuadros y salieron " + list.Count);
        return list.ToArray();
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
