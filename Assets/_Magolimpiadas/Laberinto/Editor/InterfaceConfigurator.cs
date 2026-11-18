using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Labyrinth.Editor
{
    public static class InterfaceConfigurator
    {
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string FontPath = "Assets/_Magolimpiadas/Fuentes/Otras/Magician SDF.asset";

        [MenuItem("Magolimpiadas/Crear Interfaz (Canvas y Paneles)")]
        public static void Configure()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            Ensure(Object.FindAnyObjectByType<IslandCinematic>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void Ensure(IslandCinematic cinematic)
        {
            var ui = CreateCanvas();
            CreateEventSystem();

            var title = Panel(ui, "PanelTitulo", out bool newTitle);
            var titlePanel = title.GetComponent<PanelUI>();
            if (newTitle)
            {
                titlePanel.entranceDuration = 1.5f;
                titlePanel.exitDuration = 1.5f;
                titlePanel.growOnAppear = true;
            }
            if (title.GetComponentInChildren<TMP_Text>(true) == null)
            {
                var old = cinematic != null ? cinematic.transform.Find("Titulo") : null;
                var text = old != null ? old.GetComponentInChildren<TMP_Text>(true) : null;
                if (text != null)
                {
                    text.transform.SetParent(title, false);
                    var oldGroup = text.GetComponent<CanvasGroup>();
                    if (oldGroup != null) Object.DestroyImmediate(oldGroup);
                    text.gameObject.SetActive(true);
                    Debug.Log("[LAB] Título movido a Interfaz/PanelTitulo");
                }
                else
                {
                    CreateText(title);
                    Debug.Log("[LAB] Título nuevo en Interfaz/PanelTitulo");
                }
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var fade = Panel(ui, "PanelFundido", out bool newFade);
            var fadePanel = fade.GetComponent<PanelUI>();
            if (newFade)
            {
                fadePanel.entranceDuration = 0.7f;
                fadePanel.exitDuration = 0.7f;
                fadePanel.growOnAppear = false;
                var image = fade.gameObject.AddComponent<Image>();
                image.color = new Color(0.22f, 0.08f, 0.32f, 1f);
                image.raycastTarget = false;
            }
            fade.SetAsLastSibling();

            title.gameObject.SetActive(false);
            fade.gameObject.SetActive(false);

            if (cinematic != null)
            {
                cinematic.titlePanel = titlePanel;
                cinematic.fadePanel = fadePanel;
                EditorUtility.SetDirty(cinematic);
            }
        }

        static Transform CreateCanvas()
        {
            var existing = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.name == "Interfaz" && c.transform.parent == null);
            if (existing != null) return existing.transform;

            var go = new GameObject("Interfaz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var climber = go.GetComponent<CanvasScaler>();
            climber.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            climber.referenceResolution = new Vector2(1920f, 1080f);
            climber.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        static void CreateEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static Transform Panel(Transform ui, string displayName, out bool isNew)
        {
            var t = ui.Find(displayName);
            isNew = t == null;
            if (isNew)
            {
                var go = new GameObject(displayName, typeof(RectTransform), typeof(CanvasGroup), typeof(PanelUI));
                t = go.transform;
                t.SetParent(ui, false);
                var rect = (RectTransform)t;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            if (t.GetComponent<CanvasGroup>() == null) t.gameObject.AddComponent<CanvasGroup>();
            if (t.GetComponent<PanelUI>() == null) t.gameObject.AddComponent<PanelUI>();
            return t;
        }

        static void CreateText(Transform panel)
        {
            var text = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(panel, false);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.58f);
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.text = "MAGOLIMPIADAS";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 60f;
            tmp.fontSizeMax = 170f;
            tmp.characterSpacing = 6f;
            tmp.color = Color.white;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(
                new Color(1f, 0.95f, 0.7f), new Color(1f, 0.95f, 0.7f),
                new Color(1f, 0.62f, 0.18f), new Color(1f, 0.62f, 0.18f));
            tmp.raycastTarget = false;
        }
    }
}
