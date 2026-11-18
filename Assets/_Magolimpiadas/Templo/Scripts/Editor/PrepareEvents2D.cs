using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static partial class PrepareMages2D
{
    [MenuItem("Magolimpiadas/Templo 2D/Preparar zonas de poder y eventos")]
    public static void PrepareEvents()
    {
        var map = GameObject.Find(MapName);
        if (map == null) { EditorUtility.DisplayDialog("Eventos", "No encontré el mapa en la escena.", "Ok"); return; }
        var items = map.transform.Find("Objetos");
        var characters = map.transform.Find("Personajes");
        var terrain = map.transform.Find("Terreno");
        var combat = map.transform.Find("Combate");
        if (items == null || characters == null || terrain == null || combat == null)
        {
            EditorUtility.DisplayDialog("Eventos", "Falta \"Objetos\", \"Personajes\", \"Terreno\" o \"Combate\" (primero: Preparar enemigos y combate).", "Ok");
            return;
        }
        var elemental = AssetDatabase.LoadAssetAtPath<MageData>(DataFolder + "/DatosMago_Elemental.asset");
        var druid = AssetDatabase.LoadAssetAtPath<MageData>(DataFolder + "/DatosMago_Druida.asset");
        var lightning = AssetDatabase.LoadAssetAtPath<MageData>(DataFolder + "/DatosMago_Rayo.asset");
        var nebula = AssetDatabase.LoadAssetAtPath<MageData>(DataFolder + "/DatosMago_Nebulosa.asset");

        Vector3 o0 = map.transform.position;
        Vector3 Tile(float x, float y) => o0 + new Vector3(x, -y, 0f);

        var islandTiles = new Dictionary<Vector3Int, TileBase>();
        var old = map.transform.Find("Eventos");
        var oldElev = old != null ? old.Find("Fase 5 - Cadena de luz/Elevador (isla flotante)") : null;
        var terrainTilemap = terrain.GetComponent<Tilemap>();
        for (int x = 117; x <= 121; x++)
            for (int r = 21; r <= 22; r++)
            {
                var cell = new Vector3Int(x, -r - 1, 0);
                var t = oldElev != null ? oldElev.GetComponent<Tilemap>().GetTile(cell) : terrainTilemap.GetTile(cell);
                if (t != null) islandTiles[cell] = t;
            }
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var events = new GameObject("Eventos");
        Undo.RegisterCreatedObjectUndo(events, "Preparar eventos");
        events.transform.SetParent(map.transform, false);

        {
            var root = Child(events.transform, "Fase 2 - Lago congelado", Tile(64.5f, 23f));
            var ev = root.AddComponent<LakeEvent>();
            var pieces = new List<SpriteRenderer>();
            var leftEdge = Cut(Root + "/assets/tiles/hielo_izq.png", 1, 64f, 1f)[0];
            var cen = Cut(Root + "/assets/tiles/hielo_centro.png", 1, 64f, 1f)[0];
            var right = Cut(Root + "/assets/tiles/hielo_der.png", 1, 64f, 1f)[0];
            var order = new[] { (leftEdge, 58.5f), (cen, 61.5f), (cen, 64.5f), (cen, 67.5f), (right, 70.5f) };
            foreach (var (sp, x) in order)
            {
                var go = Child(root.transform, "Hielo", Tile(x, 23f));
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = 1;
                pieces.Add(sr);
            }
            ev.icePieces = pieces.ToArray();
            var ground = root.AddComponent<BoxCollider2D>();
            ground.size = new Vector2(15f, 1f);
            ground.offset = new Vector2(0f, -0.5f);
            ev.iceGround = ground;
            var enc = combat.GetComponentInChildren<Encounter>();
            Zone(root.transform, "Zona de poder - Elemental (congela el lago)", Tile(55f, 23f), new Vector2(4f, 5f), elemental, "congela el lago", true, ev, null);
        }

        {
            var root = Child(events.transform, "Fase 3 - Enredadera", Tile(78f, 23f));
            var ev = root.AddComponent<VineEvent>();
            var half = Cut(Root + "/assets/tiles/liana_media_1.png", 1, 64f, 0f)[0];
            var sup = Cut(Root + "/assets/tiles/liana_superior.png", 1, 64f, 0f)[0];
            var t1 = Child(root.transform, "Liana (abajo)", Tile(78f, 22f)).AddComponent<SpriteRenderer>(); t1.sprite = half; t1.sortingOrder = 6;
            var t2 = Child(root.transform, "Liana (arriba)", Tile(78f, 15f)).AddComponent<SpriteRenderer>(); t2.sprite = sup; t2.sortingOrder = 6;
            ev.sections = new[] { t1, t2 };
            var climb = Child(root.transform, "Zona para trepar", Vector3.zero);
            climb.transform.position = new Vector3(Tile(77.4f, 0f).x, (Tile(0f, 23f).y + Tile(0f, 8f).y + 2.2f) / 2f, 0f);
            var box = climb.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.6f, 15f + 2.2f);
            ev.climb = climb.AddComponent<Vine>();
            var walls = terrain.GetComponent<ClimbableWalls>();
            if (walls != null)
            {
                Undo.RecordObject(walls, "Preparar eventos");
                var r = new Rect(Tile(77.9f, 0f).x, Tile(0f, 23.1f).y, 3.2f, 15.2f);
                walls.nonClimbableZones.RemoveAll(z => Mathf.Abs(z.x - r.x) < 0.5f && Mathf.Abs(z.y - r.y) < 0.5f);
                walls.nonClimbableZones.Add(r);
            }
            var spirit = Find<Enemy>(characters, "Espíritu de planta");
            Zone(root.transform, "Zona de poder - Druida (haz crecer la enredadera)", Tile(75.8f, 23f), new Vector2(3.5f, 4f), druid, "haz crecer la enredadera", false, ev, spirit);
        }

        {
            var root = Child(events.transform, "Fase 4 - Zarza", Tile(93f, 23f));
            var ev = root.AddComponent<BrambleEvent>();
            ev.brambles = items.Cast<Transform>().Where(t => t.name == "espinas").Select(t => t.GetComponent<SpriteRenderer>()).Where(s => s != null).ToArray();
            ev.turnsOn = Cut(Root + "/assets/objetos/zarza_prende.png", 8, 64f, 0f);
            ev.turnsOff = Cut(Root + "/assets/objetos/zarza_apaga.png", 8, 64f, 0f);
            var block = root.AddComponent<BoxCollider2D>();
            block.size = new Vector2(10f, 7f);
            block.offset = new Vector2(0f, 3.5f);
            ev.block = block;
            ev.appear = new[] { Find<Enemy>(characters, "Poción carmesí"), Find<Enemy>(characters, "Golem de lava") }.Where(e => e != null).ToArray();
            Zone(root.transform, "Zona de poder - Elemental (quema la zarza)", Tile(85.8f, 23f), new Vector2(4.5f, 4f), elemental, "quema la zarza", false, ev, null);
        }

        {
            var root = Child(events.transform, "Fase 5 - Cadena de luz", Tile(114f, 15f));
            var ev = root.AddComponent<LightChainEvent>();
            ev.crystal = Find<SpriteRenderer>(items, "cristal_rayo");
            ev.mirror = Find<SpriteRenderer>(items, "espejo_prisma");
            ev.receptor = Find<SpriteRenderer>(items, "receptor");
            ev.crystalStates = Frames(Root + "/assets/objetos/cristal_rayo.png");
            ev.mirrorStates = Frames(Root + "/assets/objetos/espejo_prisma.png");
            ev.receptorStates = Frames(Root + "/assets/objetos/receptor.png");
            foreach (var (sr, est) in new[] { (ev.crystal, ev.crystalStates), (ev.mirror, ev.mirrorStates), (ev.receptor, ev.receptorStates) })
                if (sr != null && est.Length > 0) { Undo.RecordObject(sr, "Preparar eventos"); sr.sprite = est[0]; }

            var elev = new GameObject("Elevador (isla flotante)");
            elev.transform.SetParent(root.transform, false);
            elev.transform.position = o0;
            var tm = elev.AddComponent<Tilemap>();
            elev.AddComponent<TilemapRenderer>().sortingOrder = 0;
            Undo.RecordObject(terrainTilemap, "Preparar eventos");
            foreach (var kv in islandTiles) { tm.SetTile(kv.Key, kv.Value); terrainTilemap.SetTile(kv.Key, null); }
            var rb = elev.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            elev.AddComponent<TilemapCollider2D>();
            ev.elevator = elev.AddComponent<Elevator>();
            ev.elevator.riseHeight = 9f;
            var cc = terrain.GetComponent<CompositeCollider2D>();
            if (cc != null) cc.GenerateGeometry();

            var enemyCrystal = Find<Enemy>(characters, "Cristal del rayo");
            Zone(root.transform, "Zona de poder - Rayo (activa la cadena de luz)", Tile(110.5f, 15f), new Vector2(5f, 4f), lightning, "activa la cadena de luz", false, ev, enemyCrystal);
        }

        {
            var root = Child(events.transform, "Fase 6 - Portales", Tile(143f, 23f));
            var ev = root.AddComponent<PortalsEvent>();
            var ps = items.Cast<Transform>().Where(t => t.name == "portal").OrderBy(t => t.position.x).ToArray();
            var states = Frames(Root + "/assets/objetos/portal.png");
            var anim = Cut(Root + "/assets/objetos/portal_anim.png", 8, 64f, 0f);
            var list = new List<Portal>();
            foreach (var t in ps)
            {
                foreach (var c in t.GetComponents<BoxCollider2D>()) Undo.DestroyObjectImmediate(c);
                foreach (var p0 in t.GetComponents<Portal>()) Undo.DestroyObjectImmediate(p0);
                var box = Undo.AddComponent<BoxCollider2D>(t.gameObject);
                box.isTrigger = true; box.size = new Vector2(2f, 4f); box.offset = new Vector2(0f, 2f);
                var p = Undo.AddComponent<Portal>(t.gameObject);
                p.drawing = t.GetComponent<SpriteRenderer>();
                p.off = states.Length > 1 ? states[1] : null;
                p.activeAnimation = anim;
                list.Add(p);
            }
            if (list.Count == 2)
            {
                list[0].destination = list[1]; list[0].exit = new Vector2(-2.6f, 0.05f);
                list[1].destination = list[0]; list[1].exit = new Vector2(2.6f, 0.05f);
            }
            ev.portals = list.ToArray();
            var grate = items.Find("reja");
            if (grate != null)
            {
                foreach (var c in grate.GetComponents<BoxCollider2D>()) Undo.DestroyObjectImmediate(c);
                var box = Undo.AddComponent<BoxCollider2D>(grate.gameObject);
                box.size = new Vector2(2f, 10f); box.offset = new Vector2(0f, 5f);
            }
            var gloomy = Find<Enemy>(characters, "Slime sombrío");
            Zone(root.transform, "Zona de poder - Nebulosa (activa los portales)", Tile(135.3f, 23f), new Vector2(3.5f, 4f), nebula, "activa los portales", false, ev, gloomy);
        }

        foreach (var (displayName, phase, x) in new[] { ("Fase 3 (Espíritu)", "Fase 3", 73.5f), ("Fase 4 (Zarza)", "Fase 4", 83.5f), ("Fase 5 (Cristal)", "Fase 5", 101.5f),
                                                 ("Fase 6 (Slime sombrío)", "Fase 6", 126.5f), ("Fase 7 (Espectro y Golem)", "Fase 7", 152.5f), ("Fase 8 (Puerta)", "Fase 8", 163f) })
        {
            var v = combat.Find("Punto de control - " + displayName);
            if (v != null) Undo.DestroyObjectImmediate(v.gameObject);
            CreateCheckpoint(combat, "Punto de control - " + displayName, phase, Tile(x, 23f), 14f);
        }

        EditorSceneManager.MarkSceneDirty(map.scene);
        Selection.activeGameObject = events;
        Debug.Log("Zonas de poder y eventos listos en \"Eventos\" (lago, enredadera, zarza, cadena de luz + elevador, portales) y puntos de control de las fases 3 a 8. Guarda la escena (Ctrl+S).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar jefe Dragón de hielo")]
    public static void PrepareBoss()
    {
        var map = GameObject.Find(MapName);
        var characters = map != null ? map.transform.Find("Personajes") : null;
        var dragon = characters != null ? characters.Find("Dragón de hielo") : null;
        var encounter = map != null ? map.GetComponentInChildren<Encounter>(true) : null;
        if (dragon == null || encounter == null)
        {
            EditorUtility.DisplayDialog("Jefe", "Falta el \"Dragón de hielo\" en Personajes o el encuentro del lago (primero: Preparar enemigos y combate).", "Ok");
            return;
        }
        Vector3 o0 = map.transform.position;
        var go = dragon.gameObject;
        var anim = go.GetComponent<AnimatedSprite>();

        var rb = Ensure<Rigidbody2D>(go);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = Ensure<CapsuleCollider2D>(go);
        col.direction = CapsuleDirection2D.Horizontal;
        if (anim != null && anim.frames != null && anim.frames.Length > 0)
        {
            var b = anim.frames[0].bounds;
            col.size = new Vector2(b.size.x * 0.6f, b.size.y * 0.55f);
            col.offset = new Vector2(0f, b.min.y + b.size.y * 0.04f + col.size.y * 0.5f);
        }

        var e = Ensure<Enemy>(go);
        Undo.RecordObject(e, "Preparar jefe");
        e.displayName = "Dragón de hielo";
        e.family = MagicElement.Ice;
        e.maxHealth = 350f;
        e.contactDamage = 8f;
        e.movement = Enemy.Movement.Boss;
        e.isBoss = true;
        e.becomesTarget = false;
        e.drawing = go.GetComponent<SpriteRenderer>();

        var j = Ensure<DragonBoss>(go);
        Undo.RecordObject(j, "Preparar jefe");
        j.xMin = o0.x + 59f; j.xMax = o0.x + 71f;
        j.flightHeight = o0.y - 20f + 0.3f;
        j.platform = new Rect(o0.x + 66f, o0.y - 21f, 3f, 1f);

        Undo.RecordObject(encounter, "Preparar jefe");
        encounter.boss = e;
        encounter.bossMessage = "¡Cuidado! ¡El Dragón de hielo! Sube a la plataforma para pegarle";
        encounter.enemies.Remove(e);
        Undo.RecordObject(go, "Preparar jefe");
        go.SetActive(false);

        EditorSceneManager.MarkSceneDirty(map.scene);
        Selection.activeGameObject = go;
        Debug.Log("Jefe Dragón de hielo listo: aparece al terminar con los slimes del lago. Guarda la escena (Ctrl+S).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar puerta, runas y fin del nivel")]
    public static void PrepareDoor()
    {
        var map = GameObject.Find(MapName);
        var items = map != null ? map.transform.Find("Objetos") : null;
        var combat = map != null ? map.transform.Find("Combate") : null;
        var doorT = items != null ? items.Find("puerta_elemental") : null;
        var plateT = items != null ? items.Find("placa_elemental") : null;
        if (doorT == null || plateT == null || combat == null)
        {
            EditorUtility.DisplayDialog("Puerta", "Falta \"puerta_elemental\" o \"placa_elemental\" en Objetos, o \"Combate\" (primero: Preparar enemigos y combate).", "Ok");
            return;
        }
        Vector3 o0 = map.transform.position;
        Vector3 Tile(float x, float y) => o0 + new Vector3(x, -y, 0f);
        var glow = GlowSprite();
        var colors = new Dictionary<MagicElement, Color>
        {
            { MagicElement.Fire, new Color(1f, 0.55f, 0.15f, 0.85f) }, { MagicElement.Nebula, new Color(0.78f, 0.4f, 1f, 0.85f) },
            { MagicElement.Plant, new Color(0.55f, 0.95f, 0.3f, 0.85f) }, { MagicElement.Lightning, new Color(0.35f, 0.85f, 1f, 0.85f) },
        };
        var order = new[] { MagicElement.Fire, MagicElement.Nebula, MagicElement.Plant, MagicElement.Lightning };

        var go = doorT.gameObject;
        foreach (var c in go.GetComponents<BoxCollider2D>()) Undo.DestroyObjectImmediate(c);
        foreach (var p0 in go.GetComponents<ElementalDoor>()) Undo.DestroyObjectImmediate(p0);
        foreach (Transform h in doorT.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(h.gameObject);
        var block = Undo.AddComponent<BoxCollider2D>(go);
        block.size = new Vector2(0.4f, 9f); block.offset = new Vector2(0.45f, 4.5f);
        var door = Undo.AddComponent<ElementalDoor>(go);
        door.door = go.GetComponent<SpriteRenderer>();
        door.doorStates = Frames(Root + "/assets/objetos/puerta_elemental.png");
        door.lowering = Frames(Root + "/assets/objetos/puerta_bajando.png");
        door.block = block;
        float[] heights = { 376f / 64f, 286f / 64f, 196f / 64f, 111f / 64f };
        var circles = new List<ElementalCircle>();
        for (int i = 0; i < 4; i++)
        {
            var c = new GameObject("Círculo " + order[i]);
            Undo.RegisterCreatedObjectUndo(c, "Preparar puerta");
            c.transform.SetParent(doorT, false);
            c.transform.localPosition = new Vector3(0f, heights[i], 0f);
            c.AddComponent<CircleCollider2D>().radius = 0.55f;
            var b = new GameObject("Brillo");
            b.transform.SetParent(c.transform, false);
            var sr = b.AddComponent<SpriteRenderer>(); sr.sprite = glow; sr.color = colors[order[i]]; sr.sortingOrder = 12;
            b.transform.localScale = Vector3.one * 1.4f;
            var ce = c.AddComponent<ElementalCircle>();
            ce.element = order[i]; ce.glow = sr;
            circles.Add(ce);
        }
        door.circles = circles.ToArray();

        foreach (Transform h in plateT.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(h.gameObject);
        foreach (var c in plateT.GetComponents<BoxCollider2D>()) Undo.DestroyObjectImmediate(c);
        const float plateHeight = 1.4f;
        var floor = Undo.AddComponent<BoxCollider2D>(plateT.gameObject);
        floor.size = new Vector2(10.8f, plateHeight - 0.2f); floor.offset = new Vector2(0f, (plateHeight - 0.2f) / 2f);
        floor.edgeRadius = 0.1f;
        door.plate = plateT.GetComponent<SpriteRenderer>();
        door.plateStates = Frames(Root + "/assets/objetos/placa_elemental.png");
        float left = plateT.position.x - 6f;
        float[] xs = { 175f / 64f, 315f / 64f, 455f / 64f, 595f / 64f };
        var runes = new List<ElementalDoor.Rune>();
        for (int i = 0; i < 4; i++)
        {
            var b = new GameObject("Brillo runa " + order[i]);
            Undo.RegisterCreatedObjectUndo(b, "Preparar puerta");
            b.transform.SetParent(plateT, false);
            b.transform.position = new Vector3(left + xs[i], plateT.position.y + 117f / 64f, 0f);
            b.transform.localScale = new Vector3(2.2f, 1.6f, 1f);
            var sr = b.AddComponent<SpriteRenderer>(); sr.sprite = glow; sr.color = colors[order[i]]; sr.sortingOrder = 12;
            runes.Add(new ElementalDoor.Rune { element = order[i], point = new Vector2(left + xs[i], plateT.position.y + plateHeight), glow = sr });
        }
        door.runes = runes.ToArray();

        var old = combat.Find("Encuentro - Fase 7 (Espectro y Golem)");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var enc = new GameObject("Encuentro - Fase 7 (Espectro y Golem)");
        Undo.RegisterCreatedObjectUndo(enc, "Preparar puerta");
        enc.transform.SetParent(combat, false);
        enc.transform.position = Tile(159.8f, 15f);
        var zone = enc.AddComponent<BoxCollider2D>();
        zone.isTrigger = true; zone.size = new Vector2(7.5f, 16f);
        var encounter = enc.AddComponent<Encounter>();
        encounter.barriers.Add(Barrier(enc.transform, "Barrera izquierda", Tile(153.5f, 23f)));
        encounter.barriers.Add(Barrier(enc.transform, "Barrera derecha", Tile(163.7f, 23f)));
        encounter.messageOnFinish = "¡Paso libre! Ahora la puerta";

        EditorSceneManager.MarkSceneDirty(map.scene);
        Selection.activeGameObject = go;
        Debug.Log("Puerta elemental, runas y encuentro de la fase 7 listos. Guarda la escena (Ctrl+S).");
    }

    static readonly string[] platformPieces = { "cornisa_", "repisa_" };

    [MenuItem("Magolimpiadas/Templo 2D/Cornisas y repisas de un solo sentido")]
    public static void PreparePlatforms()
    {
        var map = GameObject.Find(MapName);
        var terrain = map != null ? map.transform.Find("Terreno") : null;
        if (terrain == null) { EditorUtility.DisplayDialog("Plataformas", "No encontré el \"Terreno\" del mapa.", "Ok"); return; }
        var terrainTilemap = terrain.GetComponent<Tilemap>();

        var platformsRoot = map.transform.Find("Plataformas");
        GameObject go;
        if (platformsRoot == null)
        {
            go = new GameObject("Plataformas");
            Undo.RegisterCreatedObjectUndo(go, "Plataformas");
            go.transform.SetParent(map.transform, false);
            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = terrain.GetComponent<TilemapRenderer>().sortingOrder;
            var tc = go.AddComponent<TilemapCollider2D>();
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
            var cc = go.AddComponent<CompositeCollider2D>();
            cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
            cc.usedByEffector = true;
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var ef = go.AddComponent<PlatformEffector2D>();
            ef.useOneWay = true; ef.surfaceArc = 150f; ef.useSideFriction = false;
        }
        else go = platformsRoot.gameObject;
        var tmPlat = go.GetComponent<Tilemap>();

        Undo.RecordObject(terrainTilemap, "Plataformas");
        Undo.RecordObject(tmPlat, "Plataformas");
        int n = 0;
        terrainTilemap.CompressBounds();
        foreach (var c in terrainTilemap.cellBounds.allPositionsWithin)
        {
            var t = terrainTilemap.GetTile(c);
            if (t == null) continue;
            string piece = ClimbableWalls.PieceName(t.name);
            if (!platformPieces.Any(p => piece.StartsWith(p))) continue;
            tmPlat.SetTile(c, t);
            terrainTilemap.SetTile(c, null);
            n++;
        }
        foreach (var g in new[] { terrain.gameObject, go })
        {
            var tc = g.GetComponent<TilemapCollider2D>(); if (tc != null) tc.ProcessTilemapChanges();
            var cc = g.GetComponent<CompositeCollider2D>(); if (cc != null) cc.GenerateGeometry();
        }
        EditorSceneManager.MarkSceneDirty(map.scene);
        Debug.Log("Cornisas y repisas de un solo sentido: " + n + " tiles movidos a \"Plataformas\". Guarda la escena (Ctrl+S).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar cambio de personaje (Tab)")]
    public static void PrepareCharacterSwitcher()
    {
        var change = Object.FindFirstObjectByType<WorldSwitcher>();
        if (change == null) { EditorUtility.DisplayDialog("Cambio de personaje", "Falta el objeto \"Cambio de mundo\".", "Ok"); return; }
        var cp = Ensure<CharacterSwitcher>(change.gameObject);
        Undo.RecordObject(cp, "Cambio de personaje");
        cp.mages.Clear();
        foreach (var displayName in new[] { "Maga Nebulosa", "Mago Elemental", "Druida Verde", "Mago del Rayo" })
            foreach (var m in Object.FindObjectsByType<Mage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (m.data != null && m.data.displayName == displayName) cp.mages.Add(m);
        var textIndicator = Object.FindFirstObjectByType<MageTextIndicator>(FindObjectsInactive.Include);
        if (textIndicator != null) { Undo.RecordObject(textIndicator, "Cambio de personaje"); textIndicator.controls = "Q cambiar modo · E usar poder · Tab cambiar de mago"; }
        EditorSceneManager.MarkSceneDirty(change.gameObject.scene);
        Debug.Log("Cambio de personaje listo (Tab): " + cp.mages.Count + " magos. Guarda la escena (Ctrl+S).");
    }

    [MenuItem("Magolimpiadas/Templo 2D/Preparar monedas (contador por mago)")]
    public static void PrepareCoins()
    {
        var map = GameObject.Find(MapName);
        var items = map != null ? map.transform.Find("Objetos") : null;
        if (items == null) { EditorUtility.DisplayDialog("Monedas", "No encontré \"Objetos\" en el mapa.", "Ok"); return; }
        int n = 0;
        foreach (Transform t in items)
        {
            if (t.name != "moneda") continue;
            var c = Ensure<CircleCollider2D>(t.gameObject);
            Undo.RecordObject(c, "Monedas");
            c.isTrigger = true; c.radius = 0.45f; c.offset = new Vector2(0f, 0.5f);
            Ensure<Coin>(t.gameObject);
            n++;
        }
        var hud = GameObject.Find("Interfaz Templo 2D");
        if (hud != null) Ensure<CoinCounter>(hud);
        EditorSceneManager.MarkSceneDirty(map.scene);
        Debug.Log("Monedas listas: " + n + ". El contador por mago está en \"Interfaz Templo 2D\". Guarda la escena (Ctrl+S).");
    }

    static Sprite GlowSprite()
    {
        string p = Root + "/_Generado/brillo_redondo.png";
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
        if (s != null) return s;
        Folder(Root + "/_Generado");
        const int t = 64;
        var tex = new Texture2D(t, t, TextureFormat.RGBA32, false);
        for (int y = 0; y < t; y++)
            for (int x = 0; x < t; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(t / 2f, t / 2f)) / (t / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 1.2f));
            }
        tex.Apply();
        System.IO.File.WriteAllBytes(p, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(p);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 64f;
        ti.filterMode = FilterMode.Bilinear;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    static GameObject Child(Transform parent, string displayName, Vector3 position)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go;
    }

    static PowerZone Zone(Transform parent, string displayName, Vector3 feet, Vector2 sizeValue, MageData mage, string action, bool secondary, LevelEvent ev, Enemy requirement)
    {
        var go = Child(parent, displayName, feet + new Vector3(0f, sizeValue.y / 2f, 0f));
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true; c.size = sizeValue;
        var z = go.AddComponent<PowerZone>();
        z.requiredMage = mage; z.action = action; z.useSecondaryPower = secondary; z.levelEvent = ev; z.requiredEnemy = requirement;
        return z;
    }

    static T Find<T>(Transform parent, string displayName) where T : Component
    {
        var t = parent.Cast<Transform>().FirstOrDefault(x => x.name == displayName);
        if (t == null) { Debug.LogWarning("No encontré \"" + displayName + "\" en " + parent.name); return null; }
        return t.GetComponent<T>();
    }

    static Sprite[] Frames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int i) ? i : 0).ToArray();
    }
}
