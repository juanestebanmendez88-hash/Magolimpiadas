using ArcaneGames.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneGames.UI
{
    public class WaitingScreen : MonoBehaviour
    {
        private const int SortingOrder = 900;
        private const float FlightFps = 12f;
        private const float BobAmplitude = 14f;
        private const float BobSpeed = 2.4f;
        private const float NoticeSeconds = 6f;
        private const float DefaultOwlSize = 260f;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 PanelSize = new Vector2(980f, 360f);
        private static readonly Vector2 OwlPosition = new Vector2(0f, 250f);
        private static readonly Color DimColor = new Color(0.05f, 0.03f, 0.12f, 0.75f);
        private static readonly Color TitleColor = new Color(1f, 0.84f, 0.38f);
        private static readonly Color TextColor = new Color(0.95f, 0.92f, 1f);
        private static readonly Color MissingColor = new Color(0.8f, 0.75f, 1f);

        private static WaitingScreen instance;

        private GameObject canvasObject;
        private RectTransform owl;
        private Image owlImage;
        private Sprite[] flight;
        private TMP_Text subtitle;
        private TMP_Text count;
        private TMP_Text missing;
        private PlayerMage frozenPlayer;

        public static void Show(OnlineTrial trial, int arrived, int total, string missingNames)
        {
            WaitingScreen screen = Instance();
            if (screen == null)
                return;

            screen.canvasObject.SetActive(true);
            screen.FreezePlayer();
            screen.subtitle.text = PurposeOf(trial);
            screen.count.text = arrived + " de " + total + " magos listos";
            screen.missing.text = string.IsNullOrEmpty(missingNames) ? "" : "Faltan: " + missingNames;
        }

        public static void Hide()
        {
            if (instance == null || !instance.canvasObject.activeSelf)
                return;

            instance.ReleasePlayer();
            instance.canvasObject.SetActive(false);
        }

        public static void NotifyWaitingFor(OnlineTrial trial, int arrived, int total)
        {
            WaitingScreen screen = Instance();
            if (screen == null || !OwlNotice.Free)
                return;

            string text = "¡Tus compañeros te esperan " + PlaceOf(trial) + "! Ya llegaron " + arrived + " de " + total + ".";
            screen.StartCoroutine(OwlNotice.Show(screen, text, NoticeSeconds));
        }

        private static WaitingScreen Instance()
        {
            if (instance != null)
                return instance;

            OwlGuide guide = OwlNotice.Owl;
            if (guide == null)
                return null;

            instance = new GameObject("Pantalla de espera").AddComponent<WaitingScreen>();
            instance.Build(guide);
            return instance;
        }

        private static string PurposeOf(OnlineTrial trial)
        {
            switch (trial)
            {
                case OnlineTrial.IslandArrival: return "para viajar juntos a la isla";
                case OnlineTrial.Temple: return "para entrar juntos al templo";
                case OnlineTrial.PingPong: return "para jugar juntos al ping pong";
                case OnlineTrial.SwimRace: return "para empezar la carrera de natación";
                case OnlineTrial.ArtisticSwimming: return "para la natación artística";
                default: return "";
            }
        }

        private static string PlaceOf(OnlineTrial trial)
        {
            switch (trial)
            {
                case OnlineTrial.IslandArrival: return "en la salida del laberinto";
                case OnlineTrial.Temple: return "en el portal del templo";
                case OnlineTrial.PingPong: return "en la mesa de ping pong";
                case OnlineTrial.SwimRace: return "en la piscina";
                case OnlineTrial.ArtisticSwimming: return "en la piscina para la natación artística";
                default: return "";
            }
        }

        private void OnDestroy()
        {
            ReleasePlayer();
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!canvasObject.activeSelf || flight == null || flight.Length == 0)
                return;

            owlImage.sprite = flight[(int)(Time.unscaledTime * FlightFps) % flight.Length];
            owl.anchoredPosition = OwlPosition + Vector2.up * Mathf.Sin(Time.unscaledTime * BobSpeed) * BobAmplitude;
        }

        private void FreezePlayer()
        {
            if (frozenPlayer != null)
                return;

            var player = FindFirstObjectByType<PlayerMage>();
            if (player == null || !player.enabled)
                return;

            player.StandStill();
            player.enabled = false;
            frozenPlayer = player;
        }

        private void ReleasePlayer()
        {
            if (frozenPlayer != null && !frozenPlayer.enabled)
                frozenPlayer.enabled = true;
            frozenPlayer = null;
        }

        private void Build(OwlGuide guide)
        {
            flight = guide.flight;

            canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            var dim = CreateImage("Fondo", canvasObject.transform, DimColor);
            Stretch(dim.rectTransform);

            var panel = CreateImage("Panel", canvasObject.transform, Color.white);
            Image panelSource = guide.panel != null ? guide.panel.GetComponent<Image>() : null;
            if (panelSource != null)
            {
                panel.sprite = panelSource.sprite;
                panel.type = panelSource.type;
                panel.color = panelSource.color;
            }
            panel.rectTransform.sizeDelta = PanelSize;
            panel.rectTransform.anchoredPosition = new Vector2(0f, -40f);

            owlImage = CreateImage("Búho", canvasObject.transform, Color.white);
            owlImage.preserveAspect = true;
            owlImage.raycastTarget = false;
            owl = owlImage.rectTransform;
            owl.sizeDelta = guide.owl != null ? guide.owl.sizeDelta : Vector2.one * DefaultOwlSize;
            owl.anchoredPosition = OwlPosition;

            TMP_Text titleSource = guide.title != null ? guide.title : guide.text;
            TMP_Text textSource = guide.text != null ? guide.text : guide.title;
            var title = CreateText("Título", panel.transform, titleSource, 58f, TitleColor, new Vector2(0f, 110f));
            title.text = "Esperando a los demás magos";
            subtitle = CreateText("Subtítulo", panel.transform, textSource, 34f, TextColor, new Vector2(0f, 45f));
            count = CreateText("Listos", panel.transform, textSource, 46f, TitleColor, new Vector2(0f, -30f));
            missing = CreateText("Faltan", panel.transform, textSource, 30f, MissingColor, new Vector2(0f, -100f));

            canvasObject.SetActive(false);
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            var image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string objectName, Transform parent, TMP_Text style, float size, Color color, Vector2 position)
        {
            var text = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            if (style != null)
            {
                text.font = style.font;
                text.fontSharedMaterial = style.fontSharedMaterial;
            }
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(PanelSize.x - 80f, size * 1.4f);
            text.rectTransform.anchoredPosition = position;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
