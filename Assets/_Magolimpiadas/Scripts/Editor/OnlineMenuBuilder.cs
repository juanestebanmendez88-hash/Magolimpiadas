using Fusion;
using ArcaneGames.Network;
using ArcaneGames.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ArcaneGames.EditorTools
{
    public static class OnlineMenuBuilder
    {
        public const string SoloLabel = "Jugar solo";
        public const string OnlineLabel = "Jugar en línea";
        public const float MainButtonSpacing = 20f;
        public static readonly Vector2 MainButtonSize = new Vector2(340f, 133f);

        private const string MenuScenePath = MageAssetTools.ScenesFolder + "/00_Inicio.unity";
        private const string NetworkFolder = MageAssetTools.Root + "/Network";
        private const string RosterPrefabPath = NetworkFolder + "/Prefabs/MageRoster.prefab";
        private const string AvatarPrefabPath = NetworkFolder + "/Prefabs/MageAvatar.prefab";
        private const string SettingsPath = NetworkFolder + "/Resources/OnlineSessionSettings.asset";
        private const string OnlinePanelName = "OnlinePanel";
        private const string LobbyPanelName = "LobbyPanel";
        private const string OnlineButtonName = "OnlineButton";
        private const string PanelSprite = "FantasyUI/panels/panel_stone.png";
        private const string CardSprite = "FantasyUI/panels/panel_wood.png";
        private const string InputSprite = "FantasyUI/panels/tooltip.png";
        private const string DividerSprite = "FantasyUI/banners/divider.png";
        private const string BackSprite = "FantasyUI/menu_icons/icon_back_medallion.png";
        private const string ReadySprite = "FantasyUI/controls/checkbox_on.png";
        private const string SlotSprite = "FantasyUI/slots/slot_normal.png";
        private const string SlotSelectedSprite = "FantasyUI/slots/slot_selected.png";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 ActionButtonSize = new Vector2(340f, 133f);

        [MenuItem("Juegos Arcanos/4. Agregar modo en línea a 00_Inicio")]
        public static void AddToMenuScene()
        {
            MenuSceneBuilder.EnsureConfigured();
            MenuSceneBuilder.LoadCommonAssets();
            EnsureNetworkAssets();

            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("[JuegosArcanos] 00_Inicio has no MainMenuController.");
                return;
            }

            RemoveChild(controller.transform, OnlinePanelName);
            RemoveChild(controller.transform, LobbyPanelName);
            UpdateMainButtons(controller);
            BuildPanels(controller.transform, controller);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[JuegosArcanos] Online mode added to 00_Inicio.");
        }

        public static void EnsureNetworkAssets()
        {
            MageAssetTools.EnsureFolder(NetworkFolder + "/Prefabs");
            MageAssetTools.EnsureFolder(NetworkFolder + "/Resources");

            GameObject prefab = PrefabAt(RosterPrefabPath, typeof(NetworkObject), typeof(MageRoster));
            GameObject avatar = PrefabAt(AvatarPrefabPath, typeof(NetworkObject), typeof(NetworkTransform), typeof(MageAvatarState));

            var rosterData = new SerializedObject(prefab.GetComponent<MageRoster>());
            MenuSceneBuilder.FillArray(rosterData.FindProperty("mages"), MageAssetTools.LoadWizardData());
            rosterData.ApplyModifiedPropertiesWithoutUndo();

            var settings = AssetDatabase.LoadAssetAtPath<OnlineSessionSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<OnlineSessionSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var settingsData = new SerializedObject(settings);
            settingsData.FindProperty("rosterPrefab").objectReferenceValue = prefab.GetComponent<NetworkObject>();
            settingsData.FindProperty("avatarPrefab").objectReferenceValue = avatar.GetComponent<NetworkObject>();
            settingsData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static GameObject PrefabAt(string path, params System.Type[] components)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var instance = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path), components);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        public static Button CreateOnlineButton(Transform column, MainMenuController controller)
        {
            Button online = MenuSceneBuilder.CreateFantasyButton(OnlineButtonName, column, OnlineLabel, MainButtonSize, 42f);
            UnityEventTools.AddPersistentListener(online.onClick, new UnityAction(controller.ShowOnline));
            return online;
        }

        public static void BuildPanels(Transform canvas, MainMenuController controller)
        {
            GameObject online = BuildOnlinePanel(canvas, controller);
            GameObject lobby = BuildLobbyPanel(canvas, controller);

            var controllerData = new SerializedObject(controller);
            controllerData.FindProperty("onlinePanel").objectReferenceValue = online;
            controllerData.FindProperty("lobbyPanel").objectReferenceValue = lobby;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            online.SetActive(false);
            lobby.SetActive(false);
        }

        private static void UpdateMainButtons(MainMenuController controller)
        {
            Transform column = FindDeep(controller.transform, "ButtonsColumn");
            if (column == null)
                return;

            var layout = column.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
                layout.spacing = MainButtonSpacing;
            ((RectTransform)column).sizeDelta = new Vector2(420f, 620f);

            Transform play = column.Find("PlayButton");
            if (play != null)
                play.GetComponentInChildren<TMP_Text>().text = SoloLabel;

            if (column.Find(OnlineButtonName) == null)
            {
                Button online = CreateOnlineButton(column, controller);
                online.transform.SetSiblingIndex(play != null ? play.GetSiblingIndex() + 1 : 0);
            }

            foreach (RectTransform button in column)
            {
                button.sizeDelta = MainButtonSize;
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    MenuSceneBuilder.FitLabelBetweenTips(label, MainButtonSize.x);
            }
        }

        private static GameObject BuildOnlinePanel(Transform canvas, MainMenuController controller)
        {
            GameObject panel = CreatePanelRoot(OnlinePanelName, canvas, "JUGAR EN LÍNEA");
            var online = panel.AddComponent<OnlineMenuPanel>();

            Image window = MenuSceneBuilder.CreateImage("Window", panel.transform, MageAssetTools.LoadSprite(PanelSprite), false, Image.Type.Sliced);
            MenuSceneBuilder.Anchor(window.rectTransform, Center, Center, new Vector2(0f, -40f), new Vector2(1000f, 720f));
            Transform content = window.transform;

            Label(content, "Tu nombre de mago", MenuSceneBuilder.Gold, 34f, new Vector2(0f, 255f), new Vector2(700f, 50f));
            TMP_InputField nameInput = CreateInputField("PlayerNameInput", content, "Escribe tu nombre", 34f, new Vector2(0f, 185f), new Vector2(580f, 86f));

            Image divider = MenuSceneBuilder.CreateImage("Divider", content, MageAssetTools.LoadSprite(DividerSprite));
            divider.preserveAspect = true;
            MenuSceneBuilder.Anchor(divider.rectTransform, Center, Center, new Vector2(0f, 110f), new Vector2(560f, 56f));

            Label(content, "Nueva sala", MenuSceneBuilder.Gold, 34f, new Vector2(-235f, 45f), new Vector2(420f, 50f));
            Label(content, "Te damos un código para tus amigos", MenuSceneBuilder.Cream, 24f, new Vector2(-235f, -5f), new Vector2(420f, 40f));
            Button create = ActionButton("CreateButton", content, "Crear sala", new Vector2(-235f, -150f));

            Label(content, "Tengo un código", MenuSceneBuilder.Gold, 34f, new Vector2(235f, 45f), new Vector2(420f, 50f));
            TMP_InputField codeInput = CreateInputField("RoomCodeInput", content, "ABCD", 44f, new Vector2(235f, -15f), new Vector2(260f, 86f));
            codeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            Button join = ActionButton("JoinButton", content, "Unirse", new Vector2(235f, -150f));

            TextMeshProUGUI status = Label(content, string.Empty, MenuSceneBuilder.Cream, 28f, new Vector2(0f, -285f), new Vector2(900f, 80f));

            UnityEventTools.AddPersistentListener(create.onClick, new UnityAction(online.CreateRoom));
            UnityEventTools.AddPersistentListener(join.onClick, new UnityAction(online.JoinRoom));
            AddBackButton(panel.transform, new UnityAction(online.Back));

            var data = new SerializedObject(online);
            data.FindProperty("menu").objectReferenceValue = controller;
            data.FindProperty("playerNameInput").objectReferenceValue = nameInput;
            data.FindProperty("roomCodeInput").objectReferenceValue = codeInput;
            data.FindProperty("createButton").objectReferenceValue = create;
            data.FindProperty("joinButton").objectReferenceValue = join;
            data.FindProperty("statusText").objectReferenceValue = status;
            data.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static GameObject BuildLobbyPanel(Transform canvas, MainMenuController controller)
        {
            GameObject panel = CreatePanelRoot(LobbyPanelName, canvas, "SALA DE ESPERA");
            var lobby = panel.AddComponent<LobbyPanel>();
            Transform root = panel.transform;

            Label(root, "Código de la sala · compártelo con tus amigos", MenuSceneBuilder.Cream, 28f, new Vector2(0f, 345f), new Vector2(1000f, 44f));
            TextMeshProUGUI code = MenuSceneBuilder.CreateText("RoomCode", root, "ABCD", MenuSceneBuilder.titleFont, 96f, MenuSceneBuilder.Gold,
                TextAlignmentOptions.Center, 0f, 0f, MenuSceneBuilder.titleOutlineMat);
            code.characterSpacing = 18f;
            MenuSceneBuilder.Anchor(code.rectTransform, Center, Center, new Vector2(0f, 270f), new Vector2(600f, 110f));

            var slots = new LobbySlotView[MageRoster.MaxPlayers];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = CreateSlotCard(root, i, new Vector2(-630f + i * 420f, 85f));

            Label(root, "Elige tu mago", MenuSceneBuilder.Gold, 34f, new Vector2(0f, -85f), new Vector2(600f, 50f));
            WizardData[] mages = MageAssetTools.LoadWizardData();
            var mageButtons = new Button[mages.Length];
            var mageFrames = new Image[mages.Length];
            for (int i = 0; i < mages.Length; i++)
                mageButtons[i] = CreateMageButton(root, mages[i], new Vector2(-270f + i * 180f, -190f), lobby, i, out mageFrames[i]);

            TextMeshProUGUI status = Label(root, string.Empty, MenuSceneBuilder.Cream, 28f, new Vector2(0f, -305f), new Vector2(1100f, 44f));
            Button leave = ActionButton("LeaveButton", root, "Salir", new Vector2(-420f, -400f));
            Button ready = ActionButton("ReadyButton", root, "¡Listo!", new Vector2(420f, -400f));
            Button start = ActionButton("StartButton", root, "Empezar", new Vector2(420f, -400f));

            UnityEventTools.AddPersistentListener(leave.onClick, new UnityAction(lobby.Leave));
            UnityEventTools.AddPersistentListener(ready.onClick, new UnityAction(lobby.ToggleReady));
            UnityEventTools.AddPersistentListener(start.onClick, new UnityAction(lobby.StartMatch));

            var data = new SerializedObject(lobby);
            data.FindProperty("menu").objectReferenceValue = controller;
            data.FindProperty("roomCodeText").objectReferenceValue = code;
            data.FindProperty("statusText").objectReferenceValue = status;
            MenuSceneBuilder.FillArray(data.FindProperty("slotViews"), slots);
            MenuSceneBuilder.FillArray(data.FindProperty("mageButtons"), mageButtons);
            MenuSceneBuilder.FillArray(data.FindProperty("mageSelectedFrames"), mageFrames);
            data.FindProperty("readyButton").objectReferenceValue = ready;
            data.FindProperty("readyLabel").objectReferenceValue = ready.GetComponentInChildren<TMP_Text>();
            data.FindProperty("startButton").objectReferenceValue = start;
            data.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static LobbySlotView CreateSlotCard(Transform parent, int index, Vector2 position)
        {
            Image card = MenuSceneBuilder.CreateImage("Slot_" + (index + 1), parent, MageAssetTools.LoadSprite(CardSprite), false, Image.Type.Sliced);
            MenuSceneBuilder.Anchor(card.rectTransform, Center, Center, position, new Vector2(390f, 240f));
            var view = card.gameObject.AddComponent<LobbySlotView>();

            TextMeshProUGUI playerName = Label(card.transform, "Jugador", MenuSceneBuilder.Cream, 32f, new Vector2(0f, 62f), new Vector2(330f, 46f));
            TextMeshProUGUI mageName = MenuSceneBuilder.CreateText("MageName", card.transform, string.Empty, MenuSceneBuilder.textFont, 36f, Color.white,
                TextAlignmentOptions.Center, 24f, 36f, MenuSceneBuilder.textOutlineMat);
            mageName.fontStyle = FontStyles.Bold;
            MenuSceneBuilder.Anchor(mageName.rectTransform, Center, Center, new Vector2(0f, 5f), new Vector2(330f, 56f));
            TextMeshProUGUI state = Label(card.transform, string.Empty, MenuSceneBuilder.Cream, 22f, new Vector2(0f, -62f), new Vector2(330f, 60f));

            Image readyIcon = MenuSceneBuilder.CreateImage("ReadyIcon", card.transform, MageAssetTools.LoadSprite(ReadySprite));
            MenuSceneBuilder.Anchor(readyIcon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -22f), new Vector2(46f, 46f));

            var data = new SerializedObject(view);
            data.FindProperty("frame").objectReferenceValue = card;
            data.FindProperty("readyIcon").objectReferenceValue = readyIcon;
            data.FindProperty("playerNameText").objectReferenceValue = playerName;
            data.FindProperty("mageNameText").objectReferenceValue = mageName;
            data.FindProperty("stateText").objectReferenceValue = state;
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Button CreateMageButton(Transform parent, WizardData mage, Vector2 position, LobbyPanel lobby, int index, out Image selectedFrame)
        {
            Image slot = MenuSceneBuilder.CreateImage("Mage_" + mage.wizardId, parent, MageAssetTools.LoadSprite(SlotSprite), true);
            MenuSceneBuilder.Anchor(slot.rectTransform, Center, Center, position, new Vector2(160f, 160f));
            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot;
            slot.gameObject.AddComponent<UIButtonFx>();

            selectedFrame = MenuSceneBuilder.CreateImage("SelectedFrame", slot.transform, MageAssetTools.LoadSprite(SlotSelectedSprite));
            MenuSceneBuilder.Stretch(selectedFrame.rectTransform, 0f, 0f, 0f, 0f);
            selectedFrame.enabled = false;

            TextMeshProUGUI label = MenuSceneBuilder.CreateText("Name", slot.transform, mage.displayName, MenuSceneBuilder.textFont, 24f,
                mage.themeColor, TextAlignmentOptions.Center, 16f, 26f, null);
            MenuSceneBuilder.Stretch(label.rectTransform, 12f, 12f, 12f, 12f);

            UnityEventTools.AddIntPersistentListener(button.onClick, new UnityAction<int>(lobby.ChooseMage), index);
            return button;
        }

        private static GameObject CreatePanelRoot(string name, Transform canvas, string title)
        {
            GameObject panel = MenuSceneBuilder.CreateUIObject(name, canvas);
            MenuSceneBuilder.Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            Image backdrop = MenuSceneBuilder.CreateImage("Backdrop", panel.transform, null, true);
            backdrop.color = new Color(0f, 0f, 0f, 0.6f);
            MenuSceneBuilder.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);

            MenuSceneBuilder.CreateHeaderBanner(panel.transform, title);
            return panel;
        }

        private static Button ActionButton(string name, Transform parent, string label, Vector2 position)
        {
            Button button = MenuSceneBuilder.CreateFantasyButton(name, parent, label, ActionButtonSize, 40f);
            MenuSceneBuilder.Anchor(button.GetComponent<RectTransform>(), Center, Center, position, ActionButtonSize);
            return button;
        }

        private static void AddBackButton(Transform panel, UnityAction onBack)
        {
            Button back = MenuSceneBuilder.CreateIconButton("BackButton", panel, BackSprite, new Vector2(84f, 84f));
            var rect = back.GetComponent<RectTransform>();
            MenuSceneBuilder.Anchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(84f, 84f));
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(30f, -30f);
            UnityEventTools.AddPersistentListener(back.onClick, onBack);
        }

        private static TextMeshProUGUI Label(Transform parent, string text, Color color, float size, Vector2 position, Vector2 box)
        {
            TextMeshProUGUI label = MenuSceneBuilder.CreateText("Label", parent, text, MenuSceneBuilder.textFont, size, color,
                TextAlignmentOptions.Center, size * 0.6f, size, MenuSceneBuilder.textOutlineMat);
            MenuSceneBuilder.Anchor(label.rectTransform, Center, Center, position, box);
            return label;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText, float fontSize, Vector2 position, Vector2 size)
        {
            Image background = MenuSceneBuilder.CreateImage(name, parent, MageAssetTools.LoadSprite(InputSprite), true, Image.Type.Sliced);
            MenuSceneBuilder.Anchor(background.rectTransform, Center, Center, position, size);
            var input = background.gameObject.AddComponent<TMP_InputField>();

            GameObject area = MenuSceneBuilder.CreateUIObject("TextArea", background.transform);
            area.AddComponent<RectMask2D>();
            MenuSceneBuilder.Stretch(area.GetComponent<RectTransform>(), 22f, 22f, 8f, 8f);

            TextMeshProUGUI placeholder = MenuSceneBuilder.CreateText("Placeholder", area.transform, placeholderText, MenuSceneBuilder.textFont, fontSize,
                new Color(MenuSceneBuilder.Cream.r, MenuSceneBuilder.Cream.g, MenuSceneBuilder.Cream.b, 0.4f), TextAlignmentOptions.Center, 0f, 0f, null);
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            MenuSceneBuilder.Stretch(placeholder.rectTransform, 0f, 0f, 0f, 0f);

            TextMeshProUGUI text = MenuSceneBuilder.CreateText("Text", area.transform, string.Empty, MenuSceneBuilder.textFont, fontSize,
                MenuSceneBuilder.Cream, TextAlignmentOptions.Center, 0f, 0f, null);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            MenuSceneBuilder.Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

            input.textViewport = area.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = background;
            input.fontAsset = MenuSceneBuilder.textFont;
            input.pointSize = fontSize;
            input.customCaretColor = true;
            input.caretColor = MenuSceneBuilder.Gold;
            input.selectionColor = new Color(MenuSceneBuilder.Gold.r, MenuSceneBuilder.Gold.g, MenuSceneBuilder.Gold.b, 0.35f);
            return input;
        }

        private static void RemoveChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
