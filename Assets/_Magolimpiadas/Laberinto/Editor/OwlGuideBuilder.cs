using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Labyrinth.Editor
{
    public static class OwlGuideBuilder
    {
        const string OwlFolder = "Assets/_Magolimpiadas/Laberinto/UI/Buho/";
        const string UI = "Assets/_Magolimpiadas/Arte/UI/FantasyUI/";
        const string TitleFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Titulos.asset";
        const string TitleMaterial = "Assets/_Magolimpiadas/Fuentes/TMP/M_Titulos_Outline.mat";
        const string TextFont = "Assets/_Magolimpiadas/Fuentes/TMP/F_Texto.asset";
        static readonly Color Golden = new Color(1f, 0.76f, 0.22f);

        [MenuItem("Magolimpiadas/Guía Búho e Intro del Laberinto")]
        public static void Build()
        {
            var speak = Cut("buho_idle_hablar.png", 256, 256, new Vector2(0.5f, 0.0391f));
            var leftEdge = Cut("buho_senalar_izq.png", 256, 256, new Vector2(0.5f, 0.0391f));
            var right = Cut("buho_senalar_der.png", 256, 256, new Vector2(0.5f, 0.0391f));
            var flight = Cut("buho_vuelo.png", 256, 264, new Vector2(0.5f, 0.0379f));

            var panelSprite = OutlinedSprite(UI + "panels/tooltip.png", 14);

            var scene = EditorSceneManager.GetActiveScene();
            var previous = Object.FindFirstObjectByType<OwlGuide>();
            float letters = previous != null ? previous.lettersPerSecond : 40f;
            float pause = previous != null ? previous.scorePause : 0.15f;
            var sound = previous != null ? previous.letterSound : null;
            foreach (var old in scene.GetRootGameObjects().Where(r => r.name == "Guía Búho").ToList())
                Undo.DestroyObjectImmediate(old);
            var guide = CreateInterface(panelSprite, speak, leftEdge, right, flight);
            guide.lettersPerSecond = letters;
            guide.scorePause = pause;
            guide.letterSound = sound;

            var intro = Object.FindFirstObjectByType<LabyrinthIntro>();
            if (intro == null)
            {
                var go = new GameObject("Intro del laberinto");
                Undo.RegisterCreatedObjectUndo(go, "Intro del laberinto");
                intro = go.AddComponent<LabyrinthIntro>();
            }
            Undo.RecordObject(intro, "Intro del laberinto");
            intro.guide = guide;
            var cinematic = Object.FindFirstObjectByType<IslandCinematic>();
            if (cinematic != null) intro.sceneCamera = cinematic.cinematicCamera;
            if (intro.steps == null || intro.steps.Count == 0) intro.steps = InitialSteps(scene);
            else FillTargets(intro.steps, scene);
            EditorUtility.SetDirty(intro);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Guía Búho] Listo: interfaz del búho y intro del laberinto (" + intro.steps.Count + " pasos).");
        }

        static List<LabyrinthIntro.Step> InitialSteps(UnityEngine.SceneManagement.Scene scene)
        {
            var p = new List<LabyrinthIntro.Step>
            {
                new LabyrinthIntro.Step {
                    text = "¡Hola, {mago}! Para comenzar, lleva la llama olímpica a través del laberinto hasta el pebetero de la isla.",
                    isPlayer = true, fromFront = true, distance = 3.2f, height = 0.9f, gazeHeight = 1.3f, rotation = 20f,
                    highlight = LabyrinthIntro.Highlight.PlayerFlame },
                new LabyrinthIntro.Step {
                    text = "Este portal te llevará a la isla, pero todavía está dormido: primero tendrás que activarlo.",
                    distance = 7f, height = 3.5f, gazeHeight = 2f },
                new LabyrinthIntro.Step {
                    text = "Busca el interruptor: está en la zona de las islas flotantes y las púas. Al tocarlo, el portal despertará.",
                    distance = 9f, height = 7f, gazeHeight = 0.6f },
                new LabyrinthIntro.Step {
                    text = "Pero ten cuidado: el minotauro vigila el laberinto y no dejará que pases así como así.",
                    fromFront = true, distance = 6.5f, height = 3.5f, gazeHeight = 1.8f },
                new LabyrinthIntro.Step {
                    text = "En algunos rincones brilla un hechizo de runas: es una zona segura. Mientras estés dentro, el minotauro no podrá verte.",
                    distance = 6f, height = 5f, gazeHeight = 0.3f },
                new LabyrinthIntro.Step {
                    text = "Y no estás solo: las plataformas de teletransporte te llevarán, al azar, a otra parte del laberinto.",
                    distance = 5f, height = 5f, gazeHeight = 0.6f },
                new LabyrinthIntro.Step {
                    text = "¡Buena suerte, {mago}!",
                    isPlayer = true, fromFront = true, distance = 3.5f, height = 1.2f, gazeHeight = 1.2f, rotation = -15f,
                    highlight = LabyrinthIntro.Highlight.Nothing },
            };
            FillTargets(p, scene);
            return p;
        }

        static void FillTargets(List<LabyrinthIntro.Step> steps, UnityEngine.SceneManagement.Scene scene)
        {
            string[] paths = { null, "Portales/PortalSeven", "LaberintoMinotauro/Mecanismos/Interruptor", "Minotauro",
                               "LaberintoMinotauro/Zonas/Refugio_1", "LaberintoMinotauro/Mecanismos/Placa_Este", null };
            for (int i = 0; i < steps.Count && i < paths.Length; i++)
            {
                if (steps[i].target != null || paths[i] == null) continue;
                steps[i].target = Find(scene, paths[i]);
                if (steps[i].target == null) Debug.LogWarning("[Guía Búho] No encontré " + paths[i]);
            }
        }

        static Transform Find(UnityEngine.SceneManagement.Scene scene, string path)
        {
            var parts = path.Split(new[] { '/' }, 2);
            var root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == parts[0]);
            if (root == null) return null;
            return parts.Length == 1 ? root.transform : root.transform.Find(parts[1]);
        }

        static OwlGuide CreateInterface(Sprite panelSprite, Sprite[] speak, Sprite[] leftEdge, Sprite[] right, Sprite[] flight)
        {
            var root = new GameObject("Guía Búho", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(root, "Guía Búho");
            root.layer = LayerMask.NameToLayer("UI");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;
            var climber = root.GetComponent<CanvasScaler>();
            climber.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            climber.referenceResolution = new Vector2(1920f, 1080f);
            climber.matchWidthOrHeight = 0.5f;

            var titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFont);
            var textFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFont);

            var panel = Child("Panel", root.transform);
            Anchor(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(1060f, 290f));
            var background = panel.gameObject.AddComponent<Image>();
            background.sprite = panelSprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 0.45f;
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var title = CreateText("Titulo", panel, titleFont, 58f, Golden, "Búho guía");
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -14f), new Vector2(-200f, 66f));
            var titleMat = AssetDatabase.LoadAssetAtPath<Material>(TitleMaterial);
            if (titleMat != null) title.fontSharedMaterial = titleMat;

            var divisor = Child("Divisor", panel).gameObject.AddComponent<Image>();
            divisor.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "banners/divider.png");
            divisor.preserveAspect = true;
            Anchor(divisor.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -78f), new Vector2(560f, 30f));

            var text = CreateText("Texto", panel, textFont, 30f, Color.white, "");
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 4f;
            Anchor(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            text.rectTransform.offsetMin = new Vector2(150f, 60f);
            text.rectTransform.offsetMax = new Vector2(-46f, -112f);

            var proceed = Child("Continuar", panel);
            Anchor(proceed, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 18f), new Vector2(240f, 44f));
            var label = CreateText("Etiqueta", proceed, textFont, 30f, Golden, "Continuar");
            label.alignment = TextAlignmentOptions.MidlineRight;
            Anchor(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), new Vector2(-46f, 0f), new Vector2(-46f, 0f));
            var arrow = Child("Flecha", proceed).gameObject.AddComponent<Image>();
            arrow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "menu_icons/icon_play.png");
            arrow.preserveAspect = true;
            Anchor(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(36f, 36f));

            var notice = CreateText("Aviso", panel, textFont, 20f, new Color(1f, 1f, 1f, 0.55f), "");
            notice.alignment = TextAlignmentOptions.TopRight;
            Anchor(notice.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -22f), new Vector2(260f, 30f));

            var owl = Child("Buho", root.transform);
            Anchor(owl, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.04f), new Vector2(-575f, -500f), new Vector2(400f, 400f));
            var image = owl.gameObject.AddComponent<Image>();
            image.sprite = speak.FirstOrDefault();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var guide = root.AddComponent<OwlGuide>();
            guide.soundSource = root.AddComponent<AudioSource>();
            guide.soundSource.playOnAwake = false;
            guide.canvas = root.GetComponent<RectTransform>();
            guide.panel = panel;
            guide.panelGroup = group;
            guide.title = title;
            guide.text = text;
            guide.proceed = proceed;
            guide.notice = notice;
            guide.owl = owl;
            guide.owlImage = image;
            guide.housePosition = new Vector2(-575f, -500f);
            guide.targetSeparation = 250f;
            guide.flightArcHeight = 180f;
            guide.screenMargin = 200f;
            guide.speak = speak;
            guide.pointLeft = leftEdge;
            guide.pointRight = right;
            guide.flight = flight;
            foreach (var g in root.GetComponentsInChildren<Transform>(true)) g.gameObject.layer = root.layer;
            return guide;
        }

        static RectTransform Child(string displayName, Transform parent)
        {
            var go = new GameObject(displayName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static TextMeshProUGUI CreateText(string displayName, Transform parent, TMP_FontAsset font, float sizeValue, Color color, string content)
        {
            var t = Child(displayName, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = sizeValue;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            return t;
        }

        static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivotPoint, Vector2 position, Vector2 sizeValue)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.pivot = pivotPoint;
            r.anchoredPosition = position;
            r.sizeDelta = sizeValue;
        }

        static Sprite[] Cut(string file, int width, int heightValue, Vector2 pivotPoint)
        {
            string path = OwlFolder + file;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("[Guía Búho] Falta " + path); return new Sprite[0]; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 100f;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();

            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();
            var ids = dp.GetSpriteRects().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First().spriteID);
            string b = System.IO.Path.GetFileNameWithoutExtension(file);
            var rects = new List<SpriteRect>();
            for (int c = 0; c < w / width; c++)
            {
                string displayName = b + "_" + c;
                rects.Add(new SpriteRect
                {
                    name = displayName,
                    rect = new Rect(c * width, h - heightValue, width, heightValue),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivotPoint,
                    spriteID = ids.TryGetValue(displayName, out var id) ? id : GUID.Generate(),
                });
            }
            dp.SetSpriteRects(rects.ToArray());
            var idNames = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (idNames != null) idNames.SetNameFileIdPairs(rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
            dp.Apply();
            ti.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        }

        static Sprite OutlinedSprite(string path, int edge)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && ti.spriteBorder == Vector4.zero)
            {
                ti.spriteBorder = new Vector4(edge, edge, edge, edge);
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
