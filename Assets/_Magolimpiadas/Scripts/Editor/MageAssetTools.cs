using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ArcaneGames.EditorTools
{
    public static class MageAssetTools
    {
        public const string Root = "Assets/_Magolimpiadas";
        public const string UiRoot = Root + "/Arte/UI";
        public const string FantasyUi = UiRoot + "/FantasyUI";
        public const string Icons = UiRoot + "/Iconos";
        public const string Fonts = Root + "/Fuentes";
        public const string FontsTmp = Fonts + "/TMP";
        public const string WizardDataFolder = Root + "/Arte/Magos/Datos";
        public const string ScenesFolder = Root + "/Escenas";

        public const string TitleFontPath = FontsTmp + "/F_Titulos.asset";
        public const string TextFontPath = FontsTmp + "/F_Texto.asset";
        public const string TitleOutlineMatPath = FontsTmp + "/M_Titulos_Outline.mat";
        public const string TextOutlineMatPath = FontsTmp + "/M_Texto_Outline.mat";
        public const string PreviewRtPath = UiRoot + "/RT_WizardPreview.renderTexture";

        [MenuItem("Juegos Arcanos/1. Configurar Assets")]
        public static void ConfigureAssets()
        {
            Debug.Log("[JuegosArcanos] Configuring assets...");
            ConfigureTextures();
            CreateTmpFonts();
            CreateWizardData();
            CreatePreviewRenderTexture();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[JuegosArcanos] Assets configured.");
        }

        private static void ConfigureTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool isBackground = path.Contains("/Fondos/");
                string fileName = Path.GetFileNameWithoutExtension(path);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = !isBackground;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 100f;

                if (isBackground)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                }
                else
                {
                    importer.filterMode = FilterMode.Point;
                    importer.maxTextureSize = 1024;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                }

                importer.spriteBorder = GetSpriteBorder(fileName);
                importer.SaveAndReimport();
            }
        }

        private static Vector4 GetSpriteBorder(string fileName)
        {
            if (fileName.StartsWith("panel_"))
                return new Vector4(56f, 56f, 56f, 56f);
            if (fileName.StartsWith("portrait_"))
                return new Vector4(48f, 48f, 48f, 48f);
            if (fileName == "slider_track")
                return new Vector4(18f, 0f, 18f, 0f);
            if (fileName == "slider_fill")
                return new Vector4(4f, 0f, 4f, 0f);
            return Vector4.zero;
        }

        private static void CreateTmpFonts()
        {
            EnsureTmpEssentials();
            EnsureFolder(FontsTmp);

            TMP_FontAsset titleFont = CreateTmpFont(Fonts + "/MagicSchoolOne.ttf", TitleFontPath);
            TMP_FontAsset textFont = CreateTmpFont(Fonts + "/Metamorphous-Regular.ttf", TextFontPath);

            if (titleFont != null && textFont != null)
            {
                if (titleFont.fallbackFontAssetTable == null)
                    titleFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!titleFont.fallbackFontAssetTable.Contains(textFont))
                    titleFont.fallbackFontAssetTable.Add(textFont);
                EditorUtility.SetDirty(titleFont);
            }

            CreateOutlineMaterial(titleFont, TitleOutlineMatPath, 0.22f, new Color(0.16f, 0.07f, 0f, 1f));
            CreateOutlineMaterial(textFont, TextOutlineMatPath, 0.18f, new Color(0.1f, 0.05f, 0f, 1f));
        }

        private static void EnsureTmpEssentials()
        {
            if (TMP_Settings.instance != null)
                return;

            Debug.Log("[JuegosArcanos] Importing TMP Essential Resources...");
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (TMP_Settings.instance == null)
                Debug.LogError("[JuegosArcanos] TMP Essential Resources are still missing; font asset creation will fail.");
        }

        private static TMP_FontAsset CreateTmpFont(string ttfPath, string outPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null)
                return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[JuegosArcanos] Font not found at {ttfPath}");
                return null;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            fontAsset.name = Path.GetFileNameWithoutExtension(outPath);

            AssetDatabase.CreateAsset(fontAsset, outPath);
            fontAsset.material.name = fontAsset.name + " Material";
            fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void CreateOutlineMaterial(TMP_FontAsset font, string path, float width, Color color)
        {
            if (font == null || AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return;

            var material = new Material(font.material);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            material.SetColor(ShaderUtilities.ID_OutlineColor, color);
            AssetDatabase.CreateAsset(material, path);
        }

        public static Material LoadOutlineMaterial(bool title)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(title ? TitleOutlineMatPath : TextOutlineMatPath);
        }

        public static readonly (string id, string displayName, string element, string colorHex, string modelFile, string description)[] WizardDefinitions =
        {
            ("elemental", "Elemental", "Fuego", "#FF7A2F", "Assets/_Magolimpiadas/Magos3D/Modelos/MagoElemental.fbx",
                "Forjado en las brasas del Estadio Flotante. Sus hechizos de fuego arrasan la pista."),
            ("nebulosa", "Nebulosa", "Cosmos", "#A55CFF", "Assets/_Magolimpiadas/Magos3D/Modelos/Maga_Nebulosa.fbx",
                "Llegó desde un pliegue del cielo nocturno. Abre portales y dobla la luz de la luna."),
            ("druida", "Druida", "Naturaleza", "#58C24A", "Assets/_Magolimpiadas/Magos3D/Modelos/Druida.fbx",
                "Guardián de los bosques colgantes. Las semillas que lanza germinan al instante."),
            ("rayo", "Rayo", "Tormenta", "#3FC9F0", "Assets/_Magolimpiadas/Magos3D/Modelos/Mago_del_Rayo.fbx",
                "Veloz como la tormenta que lo vio nacer. El trueno anuncia cada una de sus jugadas."),
        };

        private static void CreateWizardData()
        {
            EnsureFolder(WizardDataFolder);

            foreach (var def in WizardDefinitions)
            {
                string assetPath = $"{WizardDataFolder}/Mago_{def.displayName}.asset";
                var data = AssetDatabase.LoadAssetAtPath<WizardData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<WizardData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                data.wizardId = def.id;
                data.displayName = def.displayName;
                data.elementName = def.element;
                data.description = def.description;
                ColorUtility.TryParseHtmlString(def.colorHex, out Color color);
                data.themeColor = color;
                data.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.modelFile);
                if (data.modelPrefab == null)
                    Debug.LogWarning($"[JuegosArcanos] Modelo no encontrado: {def.modelFile}");
                EditorUtility.SetDirty(data);
            }
        }

        public static WizardData[] LoadWizardData()
        {
            var list = new List<WizardData>();
            foreach (var def in WizardDefinitions)
            {
                var data = AssetDatabase.LoadAssetAtPath<WizardData>($"{WizardDataFolder}/Mago_{def.displayName}.asset");
                if (data != null)
                    list.Add(data);
            }
            return list.ToArray();
        }

        private static void CreatePreviewRenderTexture()
        {
            if (AssetDatabase.LoadAssetAtPath<RenderTexture>(PreviewRtPath) != null)
                return;

            var rt = new RenderTexture(768, 1024, 24)
            {
                name = "RT_WizardPreview",
                antiAliasing = 4,
            };
            AssetDatabase.CreateAsset(rt, PreviewRtPath);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static Sprite LoadSprite(string relativePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiRoot + "/" + relativePath);
            if (sprite == null)
                Debug.LogWarning($"[JuegosArcanos] Sprite not found: {relativePath}");
            return sprite;
        }
    }
}
