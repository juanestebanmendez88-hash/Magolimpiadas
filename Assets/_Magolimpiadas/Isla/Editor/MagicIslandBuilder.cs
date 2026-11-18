using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MageOlympics.Editor
{
    public static class MagicIslandBuilder
    {
        const string Root = "Assets/_Magolimpiadas/Isla/";
        const string Tex = Root + "Texturas/";
        const string Mat = Root + "Materiales/";
        const string Mod = Root + "Modelos/";
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string SceneRootName = "IslaMagica";
        const string LabyrinthName = "LaberintoMinotauro";
        const float LabyrinthSeparation = 25f;

        static readonly string[] WithoutCollider =
            { "Llama", "Superficie", "Simbolo", "Cascada", "Hiedra", "Arbusto", "Enredadera", "Buho", "Decoracion", "Maceta" };

        [MenuItem("Magolimpiadas/Isla Mágica/Preparar assets")]
        public static void PrepareAssets()
        {
            AssetDatabase.Refresh();
            AdjustTextures();
            var materials = CreateMaterials();
            foreach (var path in Directory.GetFiles(Mod, "*.fbx", SearchOption.AllDirectories))
                Remap(path.Replace('\\', '/'), materials);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Magolimpiadas/Isla Mágica/Volver a colocar isla (reemplaza la actual)")]
        public static void Reposition()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Volver a colocar la isla",
                    "Esto borra 'IslaMagica' de la escena y la crea de nuevo. Se pierden los cambios hechos a mano (posición, barandas movidas, etc.).",
                    "Reemplazar", "Cancelar"))
                return;
            PrepareAssets();
            PlaceInScene(replace: true);
        }

        public static void PlaceBatch()
        {
            PrepareAssets();
            PlaceInScene(replace: false);
            Debug.Log("[IslaMagica] Listo.");
        }

        static void AdjustTextures()
        {
            foreach (var path in Directory.GetFiles(Tex, "*.png").Select(r => r.Replace('\\', '/')))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                if (ti == null) continue;
                bool change = false;
                if (path.EndsWith("_N.png") && ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap; change = true;
                }
                if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; change = true; }
                if (change) ti.SaveAndReimport();
            }
        }

        static Dictionary<string, Material> CreateMaterials()
        {
            var m = new Dictionary<string, Material>();
            bool Exists(string displayName)
            {
                var prior = AssetDatabase.LoadAssetAtPath<Material>(Mat + displayName + ".mat");
                if (prior != null) m[displayName] = prior;
                return prior != null;
            }
            void L(string displayName, string tex, bool normal, float smoothness, float metallic = 0f, string tint = null)
            { if (!Exists(displayName)) m[displayName] = Lit(displayName, tex, normal, smoothness, metallic, tint); }
            void C(string displayName, string hex, float smoothness = 0.15f)
            { if (!Exists(displayName)) m[displayName] = Flatten(displayName, hex, smoothness); }
            void E(string displayName, string hex, float intensity, string tex = null)
            { if (!Exists(displayName)) m[displayName] = Emissive(displayName, hex, intensity, tex); }

            L("Piedra_Morada", "T_Piedra_Morada", true, 0.15f);
            L("Piedra_Morada_Oscura", "T_Piedra_Morada_Oscura", true, 0.12f);
            L("Piedra_Beige", "T_Piedra_Beige", true, 0.2f);
            L("Suelo_Lavanda", "T_Suelo_Lavanda", true, 0.25f);
            L("Suelo_Lavanda_Oscuro", "T_Suelo_Lavanda_Oscuro", true, 0.25f);
            L("Caldero_Morado", "T_Caldero_Morado", true, 0.4f);
            L("Roca_Isla", "T_Roca_Isla", true, 0.08f);
            L("Cesped", "T_Cesped", false, 0.05f);
            L("Tela_Azul", "T_Tela_Azul", false, 0.1f);
            L("Oro", "T_Oro", false, 0.5f, 0.6f);
            L("Piedra_Estatua", "T_Piedra_Estatua", true, 0.1f);
            L("Piedra_Estatua_Oscura", "T_Piedra_Estatua", true, 0.08f, 0f, "#73665C");
            L("Tronco", "T_Tronco", false, 0.1f);

            C("Caldero_Interior", "#231B38", 0.1f);
            C("Hoja_Verde", "#3F9139"); C("Hoja_Clara", "#86C443");
            C("Pino", "#2C5E3E"); C("Pino_Claro", "#3D7A4C");
            C("Flor_Rosa", "#F26BA9", 0.3f); C("Flor_Amarilla", "#F7C944", 0.3f); C("Flor_Crema", "#F8ECCB", 0.3f);
            C("Buho_Cafe", "#9E6C52"); C("Buho_Crema", "#EAD2AC"); C("Buho_Pupila", "#24160C", 0.7f); C("Buho_Pico", "#E9AA2B", 0.4f);

            E("Buho_Ojo", "#F5A21A", 0.4f);
            E("FX_Llama", "#FFA526", 6f);
            E("FX_Portal_Fuego", "#FF4A10", 3f);
            E("FX_Portal_Vacio", "#9B2CFF", 3f);
            E("FX_Portal_Rayo", "#1F8BFF", 3f);
            E("FX_Portal_Naturaleza", "#36D24A", 3f);
            E("FX_Simbolo", "#FFF3D0", 4f);
            E("FX_Agua", "#7FD0FF", 1.5f, "T_Agua");

            L("Piedra_Rosada", "T_Piedra_Beige", true, 0.2f, 0f, "#F2D1FF");
            C("Panel_Morado", "#4A2C82", 0.3f);
            C("Piedra_Lila", "#A78CCF", 0.25f);
            E("Gema_Morada", "#8B3BC4", 0.4f);
            E("FX_Puerta_Panel", "#3A1C6B", 0.8f);
            E("FX_Borde_Dorado", "#FFD43B", 4f);
            E("FX_Borde_Agua", "#5FF2FF", 3f);
            E("FX_Agua_Piscina", "#3FD0D8", 0.35f, "T_Agua_Piscina");

            L("Azulejo_Piscina", "T_Azulejo_Piscina", true, 0.6f);
            L("Azulejo_Piscina_Fondo", "T_Azulejo_Piscina_Fondo", true, 0.6f);
            E("Nube", "#B79DEB", 0.35f);
            if (!Exists("FX_Agua_Profunda")) m["FX_Agua_Profunda"] = Transparent("FX_Agua_Profunda", "T_Agua_Piscina", "#2BC4E0", 0.55f, 0.25f);

            if (!Exists("Plata")) m["Plata"] = Metallic("Plata", "#C9CED6", 0.7f, 0.65f);
            if (!Exists("Bronce")) m["Bronce"] = Metallic("Bronce", "#C47A4E", 0.6f, 0.6f);
            return m;
        }

        static Material Create(string displayName)
        {
            string path = Mat + displayName + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = displayName };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static Material Lit(string displayName, string tex, bool normal, float smoothness, float metallic, string tint)
        {
            var m = Create(displayName);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + ".png"));
            m.SetColor("_BaseColor", tint != null ? Hex(tint) : Color.white);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (normal)
            {
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + "_N.png"));
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Flatten(string displayName, string hex, float smoothness)
        {
            var m = Create(displayName);
            m.SetColor("_BaseColor", Hex(hex));
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Emissive(string displayName, string hex, float intensity, string tex)
        {
            var m = Create(displayName);
            var c = Hex(hex);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.3f);
            if (tex != null)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + ".png");
                m.SetTexture("_BaseMap", t);
                m.SetTexture("_EmissionMap", t);
                m.SetColor("_BaseColor", Color.white);
            }
            m.SetColor("_EmissionColor", c * intensity);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Metallic(string displayName, string hex, float metallic, float smoothness)
        {
            var m = Create(displayName);
            m.SetColor("_BaseColor", Hex(hex));
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Transparent(string displayName, string tex, string hex, float alpha, float emission)
        {
            var m = Create(displayName);
            var c = Hex(hex);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + ".png");
            m.SetTexture("_BaseMap", t);
            m.SetColor("_BaseColor", new Color(1f, 1f, 1f, alpha));
            m.SetFloat("_Smoothness", 0.85f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetTexture("_EmissionMap", t);
            m.SetColor("_EmissionColor", c * emission);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void PrepareBatch()
        {
            PrepareAssets();
            Debug.Log("[IslaMagica] Assets preparados (sin tocar escenas).");
        }

        static void Remap(string modelPath, Dictionary<string, Material> materials)
        {
            var mi = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (mi == null) return;
            var map = mi.GetExternalObjectMap();
            bool isNew = map.Count == 0;
            bool change = false;
            if (isNew)
            {
                mi.importAnimation = false;
                mi.importCameras = false;
                mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                change = true;
            }
            var fonts = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>()
                .Select(m => new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name))
                .Distinct().ToList();
            foreach (var font in fonts)
            {
                if (map.ContainsKey(font) || !materials.TryGetValue(font.name, out var mat)) continue;
                mi.AddRemap(font, mat);
                change = true;
            }
            if (change) mi.SaveAndReimport();
        }

        static void PlaceInScene(bool replace)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var previous = scene.GetRootGameObjects().FirstOrDefault(g => g.name == SceneRootName);
            if (previous != null)
            {
                if (!replace)
                {
                    Debug.Log("[IslaMagica] La isla ya está en la escena; no se modifica.");
                    return;
                }
                Object.DestroyImmediate(previous);
            }

            var root = new GameObject(SceneRootName);
            MoveToScene(root, scene);

            var island = Spawn(Mod + "Isla_MagOlimpiadas.fbx", root.transform);
            var walkways = Spawn(Mod + "Caminos_Ejemplo.fbx", root.transform);

            foreach (var t in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (WithoutCollider.Any(n => t.name.Contains(n))) continue;
                if (t.GetComponent<Collider>() == null) t.gameObject.AddComponent<MeshCollider>();
            }

            int lightCount = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.name.Contains("Llama")) continue;
                var go = new GameObject("Luz");
                go.transform.position = r.bounds.center;
                go.transform.SetParent(r.transform, true);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.62f, 0.25f);
                l.range = 7f;
                l.intensity = 3f;
                l.shadows = LightShadows.None;
                lightCount++;
            }

            var lab = scene.GetRootGameObjects().FirstOrDefault(g => g.name == LabyrinthName);
            var islandBounds = Limits(root);
            Vector3 destination = Vector3.zero;
            if (lab != null)
            {
                var bLab = Limits(lab);
                destination.x = bLab.max.x + LabyrinthSeparation - islandBounds.min.x;
                destination.z = bLab.center.z - islandBounds.center.z;
                Debug.Log($"[IslaMagica] Laberinto: centro {bLab.center} tamaño {bLab.size}");
            }
            root.transform.position = destination;

            Debug.Log($"[IslaMagica] Isla: tamaño {islandBounds.size}, colocada en {destination}. " +
                      $"Colliders: {root.GetComponentsInChildren<MeshCollider>(true).Length}, luces: {lightCount}. " +
                      $"Portal fuego en {Find(island, "Portal_Fuego")}, salida inferior hacia caminos en {Find(walkways, "Camino_01_Recto")}.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void MoveToScene(GameObject go, UnityEngine.SceneManagement.Scene scene)
            => UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        static GameObject Spawn(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError("[IslaMagica] No se encontró " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        static Bounds Limits(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static string Find(GameObject root, string displayName)
        {
            if (root == null) return "-";
            var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == displayName);
            return t == null ? "-" : t.position.ToString();
        }
    }
}
