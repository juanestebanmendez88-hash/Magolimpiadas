using System.Collections.Generic;
using ArcaneGames.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ArcaneGames.EditorTools
{
    public static class MenuSceneBuilder
    {
        private const string PreviewLayerName = "WizardPreview";
        private const float ButtonTipFraction = 0.2f;

        internal static readonly Color Cream = new Color(0.96f, 0.90f, 0.78f, 1f);
        internal static readonly Color Gold = new Color(1f, 0.84f, 0.35f, 1f);
        private static readonly Color DarkNight = new Color(0.051f, 0.039f, 0.118f, 1f);
        private static readonly Color PreviewBg = new Color(0.078f, 0.063f, 0.15f, 0.95f);

        internal static TMP_FontAsset titleFont;
        internal static TMP_FontAsset textFont;
        internal static Material titleOutlineMat;
        internal static Material textOutlineMat;

        [MenuItem("Juegos Arcanos/Crear Todo (1+2+3)")]
        public static void BuildAll()
        {
            MageAssetTools.ConfigureAssets();
            BuildMainMenuScene();
            BuildRemainingScenes();
            Debug.Log("[JuegosArcanos] BuildAll finished.");
        }

        [MenuItem("Juegos Arcanos/2. Crear Escena 00_Inicio")]
        public static void BuildMainMenuScene()
        {
            EnsureConfigured();
            LoadCommonAssets();

            int previewLayer = EnsureLayer(PreviewLayerName);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMainCamera(previewLayer);
            CreateAudioManager();
            CreateEventSystem();
            GameObject[] previewModels = BuildPreviewRig(previewLayer);

            var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGo = CreateImage("Background", canvasGo.transform, MageAssetTools.LoadSprite("Fondos/Tex_FondoMenu.jpeg"));
            Stretch(bgGo.rectTransform, 0f, 0f, 0f, 0f);
            var fitter = bgGo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            var controller = canvasGo.AddComponent<MainMenuController>();

            GameObject mainPanel = BuildMainPanel(canvasGo.transform, controller);
            GameObject settingsPanel = BuildSettingsPanel(canvasGo.transform, controller);
            GameObject charSelectPanel = BuildCharacterSelectPanel(canvasGo.transform, controller, previewModels);

            var so = new SerializedObject(controller);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("characterSelectPanel").objectReferenceValue = charSelectPanel;
            so.ApplyModifiedPropertiesWithoutUndo();

            settingsPanel.SetActive(false);
            charSelectPanel.SetActive(false);
            OnlineMenuBuilder.EnsureNetworkAssets();
            OnlineMenuBuilder.BuildPanels(canvasGo.transform, controller);

            MageAssetTools.EnsureFolder(MageAssetTools.ScenesFolder);
            EditorSceneManager.SaveScene(scene, MageAssetTools.ScenesFolder + "/00_Inicio.unity");
            Debug.Log("[JuegosArcanos] 00_Inicio built.");
        }

        internal static void EnsureConfigured()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MageAssetTools.TitleFontPath) == null)
                MageAssetTools.ConfigureAssets();
        }

        internal static void LoadCommonAssets()
        {
            titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MageAssetTools.TitleFontPath);
            textFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MageAssetTools.TextFontPath);
            titleOutlineMat = MageAssetTools.LoadOutlineMaterial(true);
            textOutlineMat = MageAssetTools.LoadOutlineMaterial(false);
        }

        private static void CreateMainCamera(int previewLayer)
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = DarkNight;
            cam.cullingMask = 0;
            cam.transform.position = new Vector3(0f, 1f, -10f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        private static AudioManager CreateAudioManager()
        {
            var go = new GameObject("AudioManager");
            var music = go.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MageAssetTools.Root + "/Audio/Musica/Mus_Menu.mp3");

            var sfx = go.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.loop = false;
            sfx.spatialBlend = 0f;

            var manager = go.AddComponent<AudioManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("musicSource").objectReferenceValue = music;
            so.FindProperty("sfxSource").objectReferenceValue = sfx;
            so.FindProperty("buttonClickClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(MageAssetTools.Root + "/Audio/SFX/SFX_Click.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static GameObject BuildMainPanel(Transform canvas, MainMenuController controller)
        {
            var panel = CreateUIObject("MainPanel", canvas);
            Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            var title = CreateText("Title", panel.transform, "MAGOLIMPIADAS", titleFont, 110f, Gold,
                TextAlignmentOptions.Center, 60f, 120f, titleOutlineMat);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60f), new Vector2(1500f, 160f));

            var subtitle = CreateText("Subtitle", panel.transform, "Los Juegos Arcanos", textFont, 38f, Cream,
                TextAlignmentOptions.Center, 0f, 0f, textOutlineMat);
            subtitle.characterSpacing = 6f;
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230f), new Vector2(900f, 56f));

            var column = CreateUIObject("ButtonsColumn", panel.transform);
            var columnRect = column.GetComponent<RectTransform>();
            Anchor(columnRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(420f, 620f));
            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = OnlineMenuBuilder.MainButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button play = CreateFantasyButton("PlayButton", column.transform, OnlineMenuBuilder.SoloLabel, OnlineMenuBuilder.MainButtonSize, 42f);
            OnlineMenuBuilder.CreateOnlineButton(column.transform, controller);
            Button settings = CreateFantasyButton("SettingsButton", column.transform, "Ajustes", OnlineMenuBuilder.MainButtonSize, 42f);
            Button quit = CreateFantasyButton("QuitButton", column.transform, "Salir", OnlineMenuBuilder.MainButtonSize, 42f);

            UnityEventTools.AddPersistentListener(play.onClick, new UnityAction(controller.ShowCharacterSelect));
            UnityEventTools.AddPersistentListener(settings.onClick, new UnityAction(controller.ShowSettings));
            UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(controller.QuitGame));

            var version = CreateText("Version", panel.transform, "v0.1 · prototipo", textFont, 22f,
                new Color(Cream.r, Cream.g, Cream.b, 0.75f), TextAlignmentOptions.Left, 0f, 0f, null);
            Anchor(version.rectTransform, Vector2.zero, Vector2.zero, new Vector2(26f, 20f), new Vector2(600f, 30f));
            version.rectTransform.pivot = Vector2.zero;
            version.rectTransform.anchoredPosition = new Vector2(26f, 20f);

            return panel;
        }

        private static GameObject BuildSettingsPanel(Transform canvas, MainMenuController controller)
        {
            var panel = CreateUIObject("SettingsPanel", canvas);
            Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            var backdrop = CreateImage("Backdrop", panel.transform, null, true);
            backdrop.color = new Color(0f, 0f, 0f, 0.65f);
            Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);

            var window = CreateImage("Window", panel.transform, MageAssetTools.LoadSprite("FantasyUI/panels/panel_stone.png"), true, Image.Type.Sliced);
            Anchor(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(860f, 980f));

            CreateHeaderBanner(window.transform, "AJUSTES");

            var settingsComponent = panel.AddComponent<SettingsPanel>();

            var content = CreateUIObject("Content", window.transform);
            var contentRect = content.GetComponent<RectTransform>();
            Stretch(contentRect, 70f, 70f, 130f, 40f);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Slider musicSlider = CreateLabeledSlider(content.transform, "MusicRow", "Música");
            Slider sfxSlider = CreateLabeledSlider(content.transform, "SfxRow", "Sonido");
            Toggle fullscreenToggle = CreateLabeledToggle(content.transform, "FullscreenRow", "Pantalla completa");

            CreateDivider(content.transform);

            var controlsTitle = CreateText("ControlsTitle", content.transform, "CONTROLES", textFont, 34f, Gold,
                TextAlignmentOptions.Center, 0f, 0f, textOutlineMat);
            controlsTitle.fontStyle = FontStyles.Bold;
            controlsTitle.rectTransform.sizeDelta = new Vector2(0f, 46f);

            BuildControlRow(content.transform, "Moverse", row =>
            {
                foreach (string key in new[] { "W", "A", "S", "D" })
                    CreateKeycap(row, key, 52f);
            });
            BuildControlRow(content.transform, "Saltar", row => CreateKeycap(row, "ESPACIO", 170f));
            BuildControlRow(content.transform, "Lanzar hechizo", row =>
            {
                CreateIcon(row, "Iconos/Mouse-Left.png", new Vector2(44f, 44f), Color.white);
                var label = CreateText("Label", row, "Clic izquierdo", textFont, 22f, Cream, TextAlignmentOptions.Left, 0f, 0f, null);
                SetLayoutSize(label.gameObject, 150f, 44f);
            });
            BuildControlRow(content.transform, "Interactuar", row => CreateKeycap(row, "E", 52f));
            BuildControlRow(content.transform, "Pausa", row => CreateKeycap(row, "ESC", 82f));

            var closeRow = CreateUIObject("CloseRow", content.transform);
            closeRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 112f);
            Button closeButton = CreateFantasyButton("CloseButton", closeRow.transform, "Volver", new Vector2(260f, 102f), 34f);
            Anchor(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(260f, 102f));
            UnityEventTools.AddPersistentListener(closeButton.onClick, new UnityAction(controller.ShowMain));

            Button closeX = CreateIconButton("CloseX", window.transform, "FantasyUI/menu_icons/icon_close_medallion.png", new Vector2(64f, 64f));
            Anchor(closeX.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(-18f, -18f), new Vector2(64f, 64f));
            closeX.GetComponent<RectTransform>().pivot = Vector2.one;
            closeX.GetComponent<RectTransform>().anchoredPosition = new Vector2(-18f, -18f);
            UnityEventTools.AddPersistentListener(closeX.onClick, new UnityAction(controller.ShowMain));

            var so = new SerializedObject(settingsComponent);
            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(musicSlider.onValueChanged, new UnityAction<float>(settingsComponent.OnMusicChanged));
            UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged, new UnityAction<float>(settingsComponent.OnSfxChanged));
            UnityEventTools.AddPersistentListener(fullscreenToggle.onValueChanged, new UnityAction<bool>(settingsComponent.OnFullscreenChanged));

            return panel;
        }

        private static GameObject BuildCharacterSelectPanel(Transform canvas, MainMenuController controller, GameObject[] previewModels)
        {
            var panel = CreateUIObject("CharacterSelectPanel", canvas);
            Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            var backdrop = CreateImage("Backdrop", panel.transform, null, true);
            backdrop.color = new Color(0f, 0f, 0f, 0.6f);
            Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);

            CreateHeaderBanner(panel.transform, "ELIGE A TU MAGO");

            var frame = CreateImage("PreviewFrame", panel.transform, MageAssetTools.LoadSprite("FantasyUI/panels/panel_wood.png"), false, Image.Type.Sliced);
            Anchor(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450f, -60f), new Vector2(560f, 760f));

            var previewBg = CreateImage("PreviewBg", frame.transform, null);
            previewBg.color = PreviewBg;
            Stretch(previewBg.rectTransform, 26f, 26f, 26f, 26f);

            var rawGo = CreateUIObject("PreviewImage", frame.transform);
            var raw = rawGo.AddComponent<RawImage>();
            raw.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MageAssetTools.PreviewRtPath);
            raw.raycastTarget = false;
            Stretch(raw.rectTransform, 26f, 26f, 26f, 26f);

            var info = CreateUIObject("InfoColumn", panel.transform);
            Anchor(info.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, -60f), new Vector2(840f, 780f));
            var layout = info.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var nameText = CreateText("WizardName", info.transform, "Elemental", titleFont, 72f, Gold,
                TextAlignmentOptions.Center, 44f, 76f, titleOutlineMat);
            nameText.rectTransform.sizeDelta = new Vector2(800f, 92f);

            var elementText = CreateText("WizardElement", info.transform, "Fuego", textFont, 30f, Cream,
                TextAlignmentOptions.Center, 0f, 0f, textOutlineMat);
            elementText.rectTransform.sizeDelta = new Vector2(800f, 40f);

            var descText = CreateText("WizardDescription", info.transform, "", textFont, 28f, Cream,
                TextAlignmentOptions.Center, 0f, 0f, null);
            descText.rectTransform.sizeDelta = new Vector2(760f, 150f);

            var slotsRow = CreateUIObject("SlotsRow", info.transform);
            slotsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 150f);
            var slotsLayout = slotsRow.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.spacing = 30f;
            slotsLayout.childAlignment = TextAnchor.MiddleCenter;
            slotsLayout.childControlWidth = false;
            slotsLayout.childControlHeight = false;
            slotsLayout.childForceExpandWidth = false;
            slotsLayout.childForceExpandHeight = false;

            WizardData[] wizards = MageAssetTools.LoadWizardData();
            var selectCharacter = panel.AddComponent<CharacterSelectPanel>();
            var selectionFrames = new Image[wizards.Length];

            Sprite slotNormal = MageAssetTools.LoadSprite("FantasyUI/slots/slot_normal.png");
            Sprite slotSelected = MageAssetTools.LoadSprite("FantasyUI/slots/slot_selected.png");

            for (int i = 0; i < wizards.Length; i++)
            {
                var slotImage = CreateImage($"Slot_{wizards[i].displayName}", slotsRow.transform, slotNormal, true);
                slotImage.rectTransform.sizeDelta = new Vector2(150f, 150f);
                var slotButton = slotImage.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slotImage;
                slotImage.gameObject.AddComponent<UIButtonFx>();

                var selFrame = CreateImage("SelectedFrame", slotImage.transform, slotSelected);
                Stretch(selFrame.rectTransform, 0f, 0f, 0f, 0f);
                selFrame.enabled = false;
                selectionFrames[i] = selFrame;

                var slotLabel = CreateText("Name", slotImage.transform, wizards[i].displayName, textFont, 24f,
                    wizards[i].themeColor, TextAlignmentOptions.Center, 16f, 28f, null);
                Stretch(slotLabel.rectTransform, 10f, 10f, 10f, 10f);

                UnityEventTools.AddIntPersistentListener(slotButton.onClick, new UnityAction<int>(selectCharacter.Select), i);
            }

            var confirmRow = CreateUIObject("ConfirmRow", info.transform);
            confirmRow.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 150f);
            Button confirm = CreateFantasyButton("ConfirmButton", confirmRow.transform, "¡Elegir!", new Vector2(360f, 141f), 44f);
            Anchor(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 141f));
            UnityEventTools.AddPersistentListener(confirm.onClick, new UnityAction(selectCharacter.Confirm));

            Button back = CreateIconButton("BackButton", panel.transform, "FantasyUI/menu_icons/icon_back_medallion.png", new Vector2(84f, 84f));
            var backRect = back.GetComponent<RectTransform>();
            Anchor(backRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(84f, 84f));
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(30f, -30f);
            UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(controller.ShowMain));

            var so = new SerializedObject(selectCharacter);
            FillArray(so.FindProperty("wizards"), wizards);
            FillArray(so.FindProperty("previewModels"), previewModels);
            FillArray(so.FindProperty("selectionFrames"), selectionFrames);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("elementText").objectReferenceValue = elementText;
            so.FindProperty("descriptionText").objectReferenceValue = descText;
            so.FindProperty("nextSceneName").stringValue = "LaberintoMinotauro";
            so.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        internal static void CreateHeaderBanner(Transform parent, string title)
        {
            var banner = CreateImage("HeaderBanner", parent, MageAssetTools.LoadSprite("FantasyUI/banners/banner_title.png"));
            Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(640f, 180f));

            var text = CreateText("Title", banner.transform, title, textFont, 40f, Color.white,
                TextAlignmentOptions.Center, 24f, 44f, textOutlineMat);
            text.fontStyle = FontStyles.Bold;
            Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(420f, 70f));
        }

        private static GameObject[] BuildPreviewRig(int previewLayer)
        {
            var rig = new GameObject("WizardPreviewRig");
            rig.transform.position = new Vector3(1000f, 0f, 0f);

            var camGo = new GameObject("PreviewCamera", typeof(Camera));
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.1f, 3.2f);
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 25f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << previewLayer;
            cam.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MageAssetTools.PreviewRtPath);
            camGo.transform.LookAt(rig.transform.position + new Vector3(0f, 0.92f, 0f));
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;

            CreatePreviewLight(rig.transform, "PreviewKeyLight", new Vector3(28f, 160f, 0f), 1.4f,
                new Color(1f, 0.96f, 0.88f), previewLayer, LightShadows.Soft);
            CreatePreviewLight(rig.transform, "PreviewFillLight", new Vector3(10f, 320f, 0f), 0.55f,
                new Color(0.62f, 0.7f, 1f), previewLayer, LightShadows.None);

            var turntable = new GameObject("Turntable", typeof(ModelTurntable));
            turntable.transform.SetParent(rig.transform, false);

            var previews = new List<GameObject>();
            foreach (var def in MageAssetTools.WizardDefinitions)
            {
                var holder = new GameObject("Preview_" + def.displayName, typeof(WizardPreviewAnimator));
                holder.transform.SetParent(turntable.transform, false);
                holder.GetComponent<WizardPreviewAnimator>().idle =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Labyrinth.Editor.MageLabyrinthsBuilder.RestPath(def.modelFile));

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.modelFile);
                GameObject instance;
                if (prefab != null)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.SetParent(holder.transform, false);
                }
                else
                {
                    instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    instance.name = def.displayName + "_Placeholder";
                    Object.DestroyImmediate(instance.GetComponent<Collider>());
                    instance.transform.SetParent(holder.transform, false);
                    instance.transform.localPosition = new Vector3(0f, 1f, 0f);
                    ColorUtility.TryParseHtmlString(def.colorHex, out Color color);
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                    instance.GetComponent<Renderer>().sharedMaterial = material;
                }

                NormalizeHolder(holder, instance);
                SetLayerRecursively(holder, previewLayer);
                previews.Add(holder);
            }

            for (int i = 0; i < previews.Count; i++)
                previews[i].SetActive(i == 0);

            return previews.ToArray();
        }

        private static void NormalizeHolder(GameObject holder, GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            Vector3 relCenter = bounds.center - holder.transform.position;
            float relMinY = relCenter.y - bounds.size.y * 0.5f;
            float scale = 1.7f / Mathf.Max(bounds.size.y, 0.01f);

            holder.transform.localScale = Vector3.one * scale;
            holder.transform.localPosition = new Vector3(-relCenter.x * scale, -relMinY * scale, -relCenter.z * scale);
        }

        private static void CreatePreviewLight(Transform parent, string name, Vector3 euler, float intensity, Color color, int layer, LightShadows shadows)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.cullingMask = 1 << layer;
            light.shadows = shadows;
        }

        [MenuItem("Juegos Arcanos/3. Build Settings del juego")]
        public static void BuildRemainingScenes()
        {
            SortProject.PutScenesInBuild();
            Debug.Log("[JuegosArcanos] Build Settings: 00_Inicio, LaberintoMinotauro y 99_CeremoniaFinal.");
        }

        internal static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        internal static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, anchorMin.y == 1f && anchorMax.y == 1f ? 1f : 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        internal static Image CreateImage(string name, Transform parent, Sprite sprite, bool raycast = false, Image.Type type = Image.Type.Simple)
        {
            var go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = raycast;
            if (sprite != null)
                image.SetNativeSize();
            return image;
        }

        internal static TextMeshProUGUI CreateText(string name, Transform parent, string value, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions alignment, float autoMin, float autoMax, Material material)
        {
            var go = CreateUIObject(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            if (font != null)
                text.font = font;
            if (material != null)
                text.fontSharedMaterial = material;
            text.fontSize = size;
            if (autoMax > 0f)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = autoMin;
                text.fontSizeMax = autoMax;
            }
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        internal static Button CreateFantasyButton(string name, Transform parent, string label, Vector2 size, float fontSize)
        {
            var image = CreateImage(name, parent, MageAssetTools.LoadSprite("FantasyUI/buttons/button_normal.png"), true);
            image.rectTransform.sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = MageAssetTools.LoadSprite("FantasyUI/buttons/button_hover.png"),
                pressedSprite = MageAssetTools.LoadSprite("FantasyUI/buttons/button_pressed.png"),
                selectedSprite = MageAssetTools.LoadSprite("FantasyUI/buttons/button_hover.png"),
                disabledSprite = MageAssetTools.LoadSprite("FantasyUI/buttons/button_disabled.png"),
            };
            image.gameObject.AddComponent<UIButtonFx>();

            var text = CreateText("Label", image.transform, label, textFont, fontSize, Cream,
                TextAlignmentOptions.Center, fontSize * 0.6f, fontSize, textOutlineMat);
            FitLabelBetweenTips(text, size.x);

            return button;
        }

        internal static void FitLabelBetweenTips(TMP_Text label, float buttonWidth)
        {
            float side = buttonWidth * ButtonTipFraction;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(label.rectTransform, side, side, 14f, 22f);
        }

        internal static Button CreateIconButton(string name, Transform parent, string spritePath, Vector2 size)
        {
            var image = CreateImage(name, parent, MageAssetTools.LoadSprite(spritePath), true);
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            image.gameObject.AddComponent<UIButtonFx>();
            return button;
        }

        private static Image CreateIcon(Transform parent, string spritePath, Vector2 size, Color tint)
        {
            var image = CreateImage("Icon", parent, MageAssetTools.LoadSprite(spritePath));
            image.rectTransform.sizeDelta = size;
            image.color = tint;
            SetLayoutSize(image.gameObject, size.x, size.y);
            return image;
        }

        private static void CreateKeycap(Transform parent, string label, float width)
        {
            var cap = CreateImage("Key_" + label, parent, MageAssetTools.LoadSprite("FantasyUI/buttons/button_square_normal.png"));
            cap.rectTransform.sizeDelta = new Vector2(width, 52f);
            SetLayoutSize(cap.gameObject, width, 52f);

            var text = CreateText("Label", cap.transform, label, textFont, 24f, Cream,
                TextAlignmentOptions.Center, 12f, 24f, null);
            Stretch(text.rectTransform, 6f, 6f, 6f, 10f);
        }

        private static void SetLayoutSize(GameObject go, float width, float height)
        {
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
        }

        private static GameObject CreateRow(Transform parent, string name, float height)
        {
            var row = CreateUIObject(name, parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            return row;
        }

        private static TextMeshProUGUI CreateRowLabel(Transform row, string label, float fontSize)
        {
            var text = CreateText("Label", row, label, textFont, fontSize, Cream, TextAlignmentOptions.Left, 0f, 0f, null);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(330f, 44f);
            rect.anchoredPosition = Vector2.zero;
            return text;
        }

        private static Slider CreateLabeledSlider(Transform parent, string name, string label)
        {
            GameObject row = CreateRow(parent, name, 56f);
            CreateRowLabel(row.transform, label, 32f);

            var sliderGo = CreateUIObject("Slider", row.transform);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(1f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(1f, 0.5f);
            sliderRect.sizeDelta = new Vector2(390f, 48f);
            sliderRect.anchoredPosition = Vector2.zero;

            var background = CreateImage("Background", sliderGo.transform, MageAssetTools.LoadSprite("FantasyUI/controls/slider_track.png"), false, Image.Type.Sliced);
            Stretch(background.rectTransform, 0f, 0f, 4f, 4f);

            var fillArea = CreateUIObject("Fill Area", sliderGo.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), 12f, 12f, 18f, 18f);

            var fill = CreateImage("Fill", fillArea.transform, MageAssetTools.LoadSprite("FantasyUI/controls/slider_fill.png"), false, Image.Type.Sliced);
            Stretch(fill.rectTransform, 0f, 0f, 0f, 0f);

            var handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), 14f, 14f, 0f, 0f);

            var handle = CreateImage("Handle", handleArea.transform, MageAssetTools.LoadSprite("FantasyUI/controls/slider_knob.png"), true);
            handle.rectTransform.sizeDelta = new Vector2(34f, 50f);

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return slider;
        }

        private static Toggle CreateLabeledToggle(Transform parent, string name, string label)
        {
            GameObject row = CreateRow(parent, name, 56f);
            CreateRowLabel(row.transform, label, 32f);

            var background = CreateImage("Background", row.transform, MageAssetTools.LoadSprite("FantasyUI/controls/checkbox_off.png"), true);
            var bgRect = background.rectTransform;
            bgRect.anchorMin = new Vector2(1f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(1f, 0.5f);
            bgRect.sizeDelta = new Vector2(52f, 52f);
            bgRect.anchoredPosition = new Vector2(-6f, 0f);

            var checkmark = CreateImage("Checkmark", background.transform, MageAssetTools.LoadSprite("FantasyUI/controls/checkbox_on.png"));
            Stretch(checkmark.rectTransform, 0f, 0f, 0f, 0f);

            var toggle = background.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = true;

            return toggle;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject row = CreateRow(parent, "Divider", 22f);
            var divider = CreateImage("Line", row.transform, MageAssetTools.LoadSprite("FantasyUI/banners/divider.png"));
            divider.preserveAspect = true;
            var rect = divider.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 18f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void BuildControlRow(Transform parent, string label, System.Action<Transform> buildKeys)
        {
            GameObject row = CreateRow(parent, "Control_" + label.Replace(" ", ""), 58f);
            CreateRowLabel(row.transform, label, 28f);

            var keys = CreateUIObject("Keys", row.transform);
            var keysRect = keys.GetComponent<RectTransform>();
            keysRect.anchorMin = new Vector2(1f, 0.5f);
            keysRect.anchorMax = new Vector2(1f, 0.5f);
            keysRect.pivot = new Vector2(1f, 0.5f);
            keysRect.sizeDelta = new Vector2(380f, 56f);
            keysRect.anchoredPosition = Vector2.zero;

            var layout = keys.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            buildKeys(keys.transform);
        }

        internal static void FillArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing != -1)
                return existing;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layerProp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            Debug.LogError("[JuegosArcanos] No free layer slot for " + layerName);
            return 0;
        }
    }
}
