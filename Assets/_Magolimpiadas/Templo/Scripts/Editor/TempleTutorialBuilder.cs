using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static class TempleTutorialBuilder
{
    const string IconsSheet = "Assets/_Magolimpiadas/Templo/assets/iconos/iconos_elementos.png";
    const string IconsToken = "Assets/_Magolimpiadas/Templo/Resources/IconosElementos.asset";
    const string UI = "Assets/_Magolimpiadas/Arte/UI/";
    const string Enemies = "Assets/_Magolimpiadas/Templo/personajes/enemigos/";
    const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
    const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";

    static readonly Color Ink = new Color(0.3f, 0.17f, 0.09f);
    static readonly Color Golden = new Color(1f, 0.76f, 0.22f);
    static readonly Color NodeBackground = new Color(0.14f, 0.11f, 0.24f);
    static readonly Color CFire = new Color(1f, 0.5f, 0.12f);
    static readonly Color CNebula = new Color(0.7f, 0.4f, 1f);
    static readonly Color CPlant = new Color(0.5f, 0.85f, 0.15f);
    static readonly Color CLightning = new Color(0.2f, 0.8f, 1f);

    static TMP_FontAsset fTitle, fText;
    static Sprite knob;

    [MenuItem("Magolimpiadas/Templo 2D/Tutorial del templo (Búho)")]
    public static void Build()
    {
        var icons = CreateIcons();
        fTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
        fText = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var scene = EditorSceneManager.GetActiveScene();
        var previous = Object.FindFirstObjectByType<TempleTutorial>(FindObjectsInactive.Include);
        var oldTexts = previous != null ? previous.pages.Select(p => (p.title, p.lines.Select(l => l.text).ToList())).ToList() : null;
        foreach (var old in scene.GetRootGameObjects().Where(r => r.name == "Tutorial Templo").ToList())
            Undo.DestroyObjectImmediate(old);

        var tut = CreateInterface(icons);
        if (oldTexts != null)
            for (int i = 0; i < tut.pages.Count && i < oldTexts.Count; i++)
            {
                tut.pages[i].title = oldTexts[i].title;
                for (int j = 0; j < tut.pages[i].lines.Count && j < oldTexts[i].Item2.Count; j++)
                    tut.pages[i].lines[j].text = oldTexts[i].Item2[j];
            }
        EditorUtility.SetDirty(tut);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Tutorial templo] Listo: pergamino de 3 páginas con el búho guía, e iconos de elementos.");
    }

    static ElementsIcons CreateIcons()
    {
        var ti = AssetImporter.GetAtPath(IconsSheet) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[Tutorial templo] Falta " + IconsSheet); return null; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 100f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        var cuts = new (string displayName, RectInt r)[]
        {
            ("icono_fuego", new RectInt(65, 103, 428, 507)),
            ("icono_nebuloso", new RectInt(570, 103, 487, 507)),
            ("icono_planta", new RectInt(1128, 103, 464, 507)),
            ("icono_rayo", new RectInt(1729, 103, 338, 507)),
        };
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        var rects = cuts.Select(c => new SpriteRect
        {
            name = c.displayName,
            rect = new Rect(c.r.x, c.r.y, c.r.width, c.r.height),
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            spriteID = ids.TryGetValue(c.displayName, out var id) ? id : GUID.Generate(),
        }).ToArray();
        dp.SetSpriteRects(rects);
        var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(IconsSheet).OfType<Sprite>().ToDictionary(s => s.name);

        if (!AssetDatabase.IsValidFolder("Assets/_Magolimpiadas/Templo/Resources")) AssetDatabase.CreateFolder("Assets/_Magolimpiadas/Templo", "Resources");
        var token = AssetDatabase.LoadAssetAtPath<ElementsIcons>(IconsToken);
        if (token == null) { token = ScriptableObject.CreateInstance<ElementsIcons>(); AssetDatabase.CreateAsset(token, IconsToken); }
        token.fire = sprites.GetValueOrDefault("icono_fuego");
        token.nebula = sprites.GetValueOrDefault("icono_nebuloso");
        token.plant = sprites.GetValueOrDefault("icono_planta");
        token.lightning = sprites.GetValueOrDefault("icono_rayo");
        EditorUtility.SetDirty(token);
        AssetDatabase.SaveAssets();
        return token;
    }

    static TempleTutorial CreateInterface(ElementsIcons icons)
    {
        var root = new GameObject("Tutorial Templo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(root, "Tutorial Templo");
        root.layer = LayerMask.NameToLayer("UI");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var esc = root.GetComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;

        var tut = root.AddComponent<TempleTutorial>();
        tut.guide = Object.FindFirstObjectByType<OwlGuide>(FindObjectsInactive.Include);

        var content = Child("Contenido", root.transform);
        Stretch(content);
        tut.group = content.gameObject.AddComponent<CanvasGroup>();

        var background = Img("Fondo", content, null, new Color(0f, 0f, 0.02f, 0.65f));
        Stretch(background.rectTransform);

        var scrollImage = Img("Pergamino", content, Sprite(UI + "FantasyUI/panels/panel_parchment.png"), Color.white);
        scrollImage.type = Image.Type.Sliced;
        Anchor(scrollImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -22f), new Vector2(1560f, 700f));
        tut.scroll = scrollImage.rectTransform;

        tut.title = Txt("Titulo", scrollImage.transform, fTitle, 60f, Ink, "", TextAlignmentOptions.Center);
        Anchor(tut.title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(1100f, 70f));
        tut.pageNumber = Txt("Pagina", scrollImage.transform, fText, 22f, new Color(Ink.r, Ink.g, Ink.b, 0.7f), "1 / 3", TextAlignmentOptions.Right);
        Anchor(tut.pageNumber.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -50f), new Vector2(150f, 30f));

        var p1 = Page("Pagina 1 - Circulo", scrollImage.transform, out var g1);
        var signals1 = CirclePage(p1, tut, icons);
        var p2 = Page("Pagina 2 - Ataque", scrollImage.transform, out var g2);
        var signals2 = AttackPage(p2, tut);
        var p3 = Page("Pagina 3 - Dano", scrollImage.transform, out var g3);
        var signals3 = DamagePage(p3, tut, icons);

        tut.pages = new List<TempleTutorial.Page>
        {
            new TempleTutorial.Page { title = "El círculo elemental", content = g1, lines = new List<TempleTutorial.Line> {
                new TempleTutorial.Line { text = "¡Bienvenido al templo, {mago}! Antes de entrar, recuerda el círculo elemental.", highlight = signals1[0] },
                new TempleTutorial.Line { text = "Cada elemento vence al siguiente: el Fuego a la Planta, la Planta al Nebuloso, el Nebuloso al Rayo y el Rayo al Fuego.", highlight = signals1[1] },
                new TempleTutorial.Line { text = "Tú eres {elemento}: tu poder hace el doble de daño a los enemigos de {vence}. ¡Pero a los de tu mismo elemento no les hace nada!", highlight = signals1[2] },
            }},
            new TempleTutorial.Page { title = "Cómo atacar", content = g2, lines = new List<TempleTutorial.Line> {
                new TempleTutorial.Line { text = "Mira cómo se pelea: con el clic izquierdo lanzas tu poder hacia donde apunta el ratón.", highlight = signals2[0] },
                new TempleTutorial.Line { text = "Cuando un slime cae, se convierte en diana durante 5 segundos. Pulsa Q para cambiar al arco y dispárale antes de que vuelva a ser slime.", highlight = signals2[0] },
                new TempleTutorial.Line { text = "Estos son tus controles. Con E usas tu poder en las zonas especiales y con Tab cambias de mago.", highlight = signals2[1] },
            }},
            new TempleTutorial.Page { title = "¿Cuánto daño hace?", content = g3, lines = new List<TempleTutorial.Line> {
                new TempleTutorial.Line { text = "Las flechas no les hacen daño a los slimes: sirven para eliminar dianas y para clavarlas en las paredes de ladrillo y escalar.", highlight = signals3[0] },
                new TempleTutorial.Line { text = "A los jefes sí: la flecha les hace poco daño, tu poder algo más y el poder del elemento que los vence, ¡el doble!", highlight = signals3[1] },
                new TempleTutorial.Line { text = "Fíjate en el icono encima de cada enemigo: te dice qué elemento lo vence. ¡Buena suerte en el templo!", highlight = signals3[2] },
            }},
        };

        tut.fireSlime = Sheet(Enemies + "slime_de_fuego.png");
        tut.nebulaSlime = Sheet(Enemies + "slime_nebuloso.png");
        tut.plantSlime = Sheet(Enemies + "slime_de_planta.png");
        tut.lightningSlime = Sheet(Enemies + "slime_del_rayo.png");
        tut.bullseye = Sheet(Enemies + "diana_de_madera.png");
        tut.targetBreak = Sheet(Enemies + "diana_de_madera_se_rompe.png");

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = root.layer;
        content.gameObject.SetActive(false);
        return tut;
    }

    static RectTransform Page(string displayName, Transform parent, out CanvasGroup group)
    {
        var p = Child(displayName, parent);
        p.anchorMin = Vector2.zero; p.anchorMax = Vector2.one;
        p.offsetMin = new Vector2(70f, 55f);
        p.offsetMax = new Vector2(-70f, -105f);
        group = p.gameObject.AddComponent<CanvasGroup>();
        return p;
    }

    static RectTransform[] CirclePage(RectTransform p, TempleTutorial tut, ElementsIcons icons)
    {
        var center = Child("Circulo", p);
        Anchor(center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, 0f), new Vector2(10f, 10f));
        const float radius = 190f;

        for (float a = 0f; a < 360f; a += 7f)
        {
            float far = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(a, 0f)), Mathf.Abs(Mathf.DeltaAngle(a, 90f)), Mathf.Abs(Mathf.DeltaAngle(a, 180f)), Mathf.Abs(Mathf.DeltaAngle(a, 270f)));
            if (far < 24f) continue;
            var d = Img("Punto", center, knob, new Color(0.55f, 0.33f, 0.14f, 0.85f));
            float r = a * Mathf.Deg2Rad;
            Anchor(d.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * radius, new Vector2(8f, 8f));
        }
        foreach (float a in new[] { 45f, 315f, 225f, 135f })
        {
            var f = Img("Flecha", center, Sprite(UI + "FantasyUI/menu_icons/icon_play.png"), new Color(0.85f, 0.45f, 0.08f));
            float r = a * Mathf.Deg2Rad;
            Vector2 tangent = new Vector2(Mathf.Sin(r), -Mathf.Cos(r));
            Anchor(f.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * radius, new Vector2(34f, 34f));
            f.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
        }

        var fire = Node(center, "Fuego", CFire, icons != null ? icons.fire : null, new Vector2(0f, radius));
        Node(center, "Planta", CPlant, icons != null ? icons.plant : null, new Vector2(radius, 0f));
        Node(center, "Nebuloso", CNebula, icons != null ? icons.nebula : null, new Vector2(0f, -radius));
        Node(center, "Rayo", CLightning, icons != null ? icons.lightning : null, new Vector2(-radius, 0f));

        var t1 = Txt("Lema", center, fTitle, 30f, Ink, "Cada elemento\nvence al siguiente", TextAlignmentOptions.Center);
        Anchor(t1.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 14f), new Vector2(260f, 80f));
        var t2 = Txt("Pista", center, fText, 18f, new Color(Ink.r, Ink.g, Ink.b, 0.8f), "Empieza en Fuego\ny sigue las flechas", TextAlignmentOptions.Center);
        Anchor(t2.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -46f), new Vector2(240f, 50f));

        var card = Card("Tu mago", p, new Vector2(430f, 40f), new Vector2(470f, 260f));
        tut.yourMageIcon = Img("Icono", card, icons != null ? icons.nebula : null, Color.white);
        tut.yourMageIcon.preserveAspect = true;
        Anchor(tut.yourMageIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(110f, 110f));
        tut.yourMageText = Txt("Texto", card, fText, 30f, Color.white, "Tu mago", TextAlignmentOptions.MidlineLeft);
        Anchor(tut.yourMageText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(160f, 0f), new Vector2(-185f, -40f));

        var note = Txt("Nota", p, fText, 21f, Ink, "El Fuego también vence al <b>Hielo</b>.\nUn poder del <b>mismo elemento</b> no hace daño.", TextAlignmentOptions.Center);
        Anchor(note.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(430f, -165f), new Vector2(500f, 70f));

        return new[] { (RectTransform)t1.transform, fire, card };
    }

    static RectTransform Node(RectTransform parent, string displayName, Color color, Sprite icon, Vector2 pos)
    {
        var hoop = Img("Nodo " + displayName, parent, knob, color);
        Anchor(hoop.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, pos, new Vector2(128f, 128f));
        var inside = Img("Fondo", hoop.transform, knob, NodeBackground);
        Anchor(inside.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(116f, 116f));
        var ic = Img("Icono", hoop.transform, icon, Color.white);
        ic.preserveAspect = true;
        Anchor(ic.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 14f), new Vector2(60f, 60f));
        var t = Txt("Nombre", hoop.transform, fText, 19f, Color.Lerp(color, Color.white, 0.45f), "<b>" + displayName + "</b>", TextAlignmentOptions.Center);
        Anchor(t.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -34f), new Vector2(150f, 28f));
        return hoop.rectTransform;
    }

    static RectTransform[] AttackPage(RectTransform p, TempleTutorial tut)
    {
        var stage = Card("Escenario", p, new Vector2(-315f, 0f), new Vector2(760f, 500f));
        var ground = Img("Suelo", stage, null, new Color(0.42f, 0.32f, 0.5f, 0.6f));
        Anchor(ground.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -112f), new Vector2(660f, 6f));

        var demo = stage.gameObject.AddComponent<AttackDemo>();
        demo.mage = Img("Mago", stage, null, Color.white);
        demo.mage.preserveAspect = true;
        Anchor(demo.mage.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(-190f, 3f), new Vector2(230f, 230f));
        demo.enemy = Img("Enemigo", stage, null, Color.white);
        demo.enemy.preserveAspect = true;
        Anchor(demo.enemy.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(190f, -7f), new Vector2(210f, 210f));
        demo.ballSprite = Img("Poder", stage, knob, Color.white);
        Anchor(demo.ballSprite.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(36f, 36f));
        demo.arrow = Img("Flecha", stage, null, Color.white);
        demo.arrow.preserveAspect = true;
        Anchor(demo.arrow.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(80f, 26f));
        demo.number = Txt("Numero", stage, fTitle, 46f, Color.white, "", TextAlignmentOptions.Center);
        Anchor(demo.number.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(320f, 60f));

        demo.key = Img("Tecla", stage, Sprite(UI + "FantasyUI/buttons/button_square_normal.png"), Color.white);
        demo.key.preserveAspect = true;
        Anchor(demo.key.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(-300f, -178f), new Vector2(48f, 48f));
        demo.keyLetter = Txt("Letra", demo.key.transform, fText, 26f, new Color(1f, 0.95f, 0.85f), "Q", TextAlignmentOptions.Center);
        Stretch(demo.keyLetter.rectTransform);
        demo.legend = Txt("Leyenda", stage, fText, 26f, Color.white, "", TextAlignmentOptions.MidlineLeft);
        Anchor(demo.legend.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 0.5f), new Vector2(-262f, -178f), new Vector2(600f, 50f));
        demo.clickSprite = Sprite(UI + "Iconos/Mouse-Right.png");
        demo.keySprite = Sprite(UI + "FantasyUI/buttons/button_square_normal.png");
        demo.legend.gameObject.SetActive(true);
        tut.demo = demo;

        var controls = Child("Controles", p);
        Anchor(controls, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(400f, 0f), new Vector2(600f, 500f));
        var tc = Txt("Titulo", controls, fTitle, 40f, Ink, "Controles", TextAlignmentOptions.Left);
        Anchor(tc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 54f));
        var rows = new (string[] keys, string text)[]
        {
            (new[] { "A", "D" }, "Moverse  (con Shift, correr)"),
            (new[] { "W", "Espacio" }, "Saltar"),
            (new[] { "#izq" }, "Atacar: apunta con el ratón"),
            (new[] { "Q" }, "Cambiar: poder / arco"),
            (new[] { "E" }, "Usar el poder en zonas especiales"),
            (new[] { "Tab" }, "Cambiar de mago"),
            (new[] { "#der" }, "Hielo (solo el Mago Elemental)"),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float y = -84f - i * 60f;
            float x = 0f;
            foreach (var k in rows[i].keys)
            {
                if (k.StartsWith("#"))
                {
                    var mouse = Img("Raton", controls, Sprite(UI + (k == "#izq" ? "Iconos/Mouse-Right.png" : "Iconos/Mouse-Left.png")), Ink);
                    mouse.preserveAspect = true;
                    Anchor(mouse.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(x + 8f, y), new Vector2(36f, 50f));
                    x += 60f;
                    continue;
                }
                bool wide = k.Length > 1;
                var cap = Img("Tecla " + k, controls, CreateKeyIcon(), Color.white);
                cap.type = Image.Type.Sliced;
                cap.pixelsPerUnitMultiplier = 1.6f;
                Anchor(cap.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(x, y), new Vector2(wide ? 118f : 52f, 52f));
                var letter = Txt("Letra", cap.transform, fText, wide ? 20f : 26f, new Color(1f, 0.95f, 0.85f), k, TextAlignmentOptions.Center);
                Stretch(letter.rectTransform);
                x += (wide ? 118f : 52f) + 8f;
            }
            var tx = Txt("Texto", controls, fText, 24f, Ink, rows[i].text, TextAlignmentOptions.MidlineLeft);
            Anchor(tx.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(200f, y), new Vector2(400f, 50f));
        }
        return new[] { stage, controls };
    }

    static RectTransform[] DamagePage(RectTransform p, TempleTutorial tut, ElementsIcons icons)
    {
        var slime = Card("Slimes", p, new Vector2(-475f, 0f), new Vector2(440f, 510f));
        CardTitle(slime, "Slimes");
        Drawing(slime, Sheet(Enemies + "slime_nebuloso.png").FirstOrDefault(), new Vector2(0f, 75f), 160f);
        tut.slimeValues = Txt("Valores", slime, fText, 24f, Color.white, "", TextAlignmentOptions.TopLeft);
        Anchor(tut.slimeValues.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(-60f, 180f));

        var boss = Card("Jefes", p, new Vector2(0f, 0f), new Vector2(440f, 510f));
        CardTitle(boss, "Jefes");
        Drawing(boss, Sheet(Enemies + "golem_de_lava.png").FirstOrDefault(), new Vector2(0f, 75f), 180f);
        tut.bossValues = Txt("Valores", boss, fText, 24f, Color.white, "", TextAlignmentOptions.TopLeft);
        Anchor(tut.bossValues.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(-60f, 180f));

        var icon = Card("Icono", p, new Vector2(475f, 0f), new Vector2(440f, 510f));
        CardTitle(icon, "El icono de arriba");
        Drawing(icon, Sheet(Enemies + "slime_de_planta.png").FirstOrDefault(), new Vector2(0f, 55f), 150f);
        var ic = Img("Icono fuego", icon, icons != null ? icons.fire : null, Color.white);
        ic.preserveAspect = true;
        Anchor(ic.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 160f), new Vector2(54f, 54f));
        var tx = Txt("Texto", icon, fText, 23f, Color.white,
            "Encima de cada enemigo está el elemento que lo vence.\nEste slime de planta lleva el <b>Fuego</b>: el Mago Elemental le hace el doble.",
            TextAlignmentOptions.Top);
        Anchor(tx.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(-60f, 180f));
        return new[] { slime, boss, icon };
    }

    static void CardTitle(RectTransform card, string text)
    {
        var t = Txt("Titulo", card, fTitle, 36f, Golden, text, TextAlignmentOptions.Center);
        Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-40f, 46f));
    }

    static void Drawing(RectTransform parent, Sprite s, Vector2 pos, float sizeValue)
    {
        var d = Img("Dibujo", parent, s, Color.white);
        d.preserveAspect = true;
        Anchor(d.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, pos, new Vector2(sizeValue, sizeValue));
    }

    static RectTransform Card(string displayName, RectTransform parent, Vector2 pos, Vector2 sizeValue)
    {
        var t = Img(displayName, parent, Sprite(UI + "FantasyUI/panels/tooltip.png"), Color.white);
        t.type = Image.Type.Sliced;
        t.pixelsPerUnitMultiplier = 0.45f;
        Anchor(t.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, pos, sizeValue);
        return t.rectTransform;
    }

    static Sprite[] Sheet(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();

    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static Sprite CreateKeyIcon()
    {
        const string path = UI + "FantasyUI/buttons/button_square_normal.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && ti.spriteBorder == Vector4.zero) { ti.spriteBorder = new Vector4(16f, 22f, 16f, 16f); ti.SaveAndReimport(); }
        return Sprite(path);
    }

    static RectTransform Child(string displayName, Transform parent)
    {
        var go = new GameObject(displayName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image Img(string displayName, Transform parent, Sprite s, Color c)
    {
        var i = Child(displayName, parent).gameObject.AddComponent<Image>();
        i.sprite = s;
        i.color = c;
        i.raycastTarget = false;
        return i;
    }

    static TextMeshProUGUI Txt(string displayName, Transform parent, TMP_FontAsset f, float sizeValue, Color c, string text, TextAlignmentOptions align)
    {
        var t = Child(displayName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.fontSize = sizeValue;
        t.color = c;
        t.text = text;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivotPoint, Vector2 pos, Vector2 sizeValue)
    {
        r.anchorMin = min; r.anchorMax = max; r.pivot = pivotPoint;
        r.anchoredPosition = pos; r.sizeDelta = sizeValue;
    }
}
