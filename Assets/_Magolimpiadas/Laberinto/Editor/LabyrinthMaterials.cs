using UnityEditor;
using UnityEngine;

namespace Labyrinth.Editor
{
    public static class LabyrinthMaterials
    {
        const string Tex = "Assets/_Magolimpiadas/Laberinto/Texturas/";
        const string Mat = "Assets/_Magolimpiadas/Laberinto/Materiales/";
        const string Mod = "Assets/_Magolimpiadas/Laberinto/Modelos/";

        public const string WallPath09a = Mod + "09a-lowpoly-250726.fbx";
        public const string WallPath09b = Mod + "09b-lowpoly-250726.fbx";
        public const string WallPath09c = Mod + "09c-lowpoly-250726.fbx";
        public const string IslandPath = Mod + "floating_island_2k.fbx";
        public const string SpikesPath = Mod + "OPM0041.fbx";

        public static Material Floor => Load("M_Piso");
        public static Material WallCap => Load("M_TapaMuro");
        public static Material PitBackground => Load("M_FondoFoso");
        public static Material Shelter => Load("M_Refugio");
        public static Material Entrance => Load("M_Entrada");
        public static Material Exit => Load("M_Salida");
        public static Material Mechanism => Load("M_Mecanismo");

        static Material Load(string displayName) => AssetDatabase.LoadAssetAtPath<Material>(Mat + displayName + ".mat");

        public static void Prepare()
        {
            AdjustTextures();

            var wall09a = Lit("M_Pared09a", Tex + "Pared09a/09a-lowpoly-250726_Base_color.jpg",
                Tex + "Pared09a/09a-lowpoly-250726_Normal_DirectX.jpg",
                ao: Tex + "Pared09a/09a-highpoly-250726_Mixed_AO.jpg", smoothness: 0.12f);
            var wall09b = Lit("M_Pared09b", Tex + "Pared09b/09b-lowpoly-250726_Base_color.jpg",
                Tex + "Pared09b/09b-lowpoly-250726_Normal_DirectX.jpg",
                ao: Tex + "Pared09b/09b-lowpoly-250726_Mixed_AO.jpg", smoothness: 0.12f);
            var wall09c = Lit("M_Pared09c", Tex + "Pared09c/09c-lowpoly-250726_Base_color.jpg",
                Tex + "Pared09c/09c-lowpoly-250726_Normal_DirectX.jpg",
                ao: Tex + "Pared09c/09c-lowpoly-250726_Mixed_AO.jpg", smoothness: 0.12f);
            var island = Lit("M_Isla", Tex + "Isla/island_diffuse.jpeg", Tex + "Isla/island_normal.jpeg", smoothness: 0.1f);
            var rocks = Lit("M_Rocas", Tex + "Rocas/rocks_diffuse.jpeg", Tex + "Rocas/rocks_normal.jpeg", smoothness: 0.1f);
            var roots = Lit("M_Raices", Tex + "Raices/roots_diffuse.jpeg", Tex + "Raices/roots_normal.jpeg", smoothness: 0.1f);
            var spikes = Lit("M_Puas", Tex + "Puas/OPM0041_4K_BaseColor.png", Tex + "Puas/OPM0041_4K_Normal.png",
                metallic: Tex + "Puas/OPM0041_4K_Metallic.png", smoothness: 0.45f);
            spikes.SetColor("_BaseColor", new Color(0.6f, 0.62f, 0.66f));

            var floor = Lit("M_Piso", Tex + "Piso/monastery_stone_floor_diff_4k.jpg",
                Tex + "Piso/monastery_stone_floor_nor_gl_4k.exr", smoothness: 0.08f);
            floor.SetColor("_BaseColor", new Color(0.98f, 0.93f, 0.82f));

            var lid = Lit("M_TapaMuro", Tex + "Piso/monastery_stone_floor_diff_4k.jpg",
                Tex + "Piso/monastery_stone_floor_nor_gl_4k.exr", smoothness: 0.06f);
            lid.SetColor("_BaseColor", new Color(0.56f, 0.56f, 0.72f));

            Color(  "M_FondoFoso", new Color(0.02f, 0.05f, 0.06f), 0.1f);
            Color(  "M_Mecanismo", new Color(0.75f, 0.55f, 0.2f), 0.45f);
            Glow( "M_Refugio", new Color(0.1f, 0.85f, 0.7f), 0.2f, 0.12f);
            Glow( "M_Entrada", new Color(0.25f, 0.95f, 0.2f), 0.25f, 0.2f);
            Glow( "M_Salida", new Color(0.65f, 0.25f, 0.95f), 0.3f, 0.25f);

            Remap(WallPath09a, ("09a-lowpoly-250726_default", wall09a));
            Remap(WallPath09b, ("09b-lowpoly-250726_default", wall09b));
            Remap(WallPath09c, ("09c-lowpoly-250726_default", wall09c));
            Remap(IslandPath, ("Island", island), ("Rocks", rocks), ("Roots", roots));
            Remap(SpikesPath, ("OPM0041", spikes));
            AssetDatabase.SaveAssets();
        }

        static void AdjustTextures()
        {
            foreach (var path in new[]
            {
                Tex + "Pared09a/09a-highpoly-250726_Mixed_AO.jpg",
                Tex + "Pared09b/09b-lowpoly-250726_Mixed_AO.jpg",
                Tex + "Pared09c/09c-lowpoly-250726_Mixed_AO.jpg",
                Tex + "Puas/OPM0041_4K_Metallic.png",
            })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                if (ti.sRGBTexture) { ti.sRGBTexture = false; ti.SaveAndReimport(); }
            }

            foreach (var path in new[]
            {
                Tex + "Pared09a/09a-lowpoly-250726_Normal_DirectX.jpg",
                Tex + "Pared09b/09b-lowpoly-250726_Normal_DirectX.jpg",
                Tex + "Pared09c/09c-lowpoly-250726_Normal_DirectX.jpg",
            })
            {
                var normal = (TextureImporter)AssetImporter.GetAtPath(path);
                if (!normal.flipGreenChannel) { normal.flipGreenChannel = true; normal.SaveAndReimport(); }
            }

            foreach (var path in new[] { Tex + "Rocas/rocks_normal.jpeg", Tex + "Raices/roots_normal.jpeg" })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                if (ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
            }
        }

        static Material Create(string displayName)
        {
            string path = Mat + displayName + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material Lit(string displayName, string baseColor, string normal, string ao = null, string metallic = null, float smoothness = 0.2f)
        {
            var m = Create(displayName);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            m.SetColor("_BaseColor", UnityEngine.Color.white);
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", smoothness);

            if (ao != null)
            {
                m.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ao));
                m.SetFloat("_OcclusionStrength", 1f);
                m.EnableKeyword("_OCCLUSIONMAP");
            }
            if (metallic != null)
            {
                m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metallic));
                m.SetFloat("_Metallic", 1f);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Color(string displayName, Color color, float smoothness)
        {
            var m = Create(displayName);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
        }

        static void Glow(string displayName, Color color, float alpha, float emission)
        {
            var m = Create(displayName);
            color.a = alpha;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetColor("_EmissionColor", color * emission);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
        }

        static void Remap(string modelPath, params (string displayName, Material material)[] map)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            foreach (var (displayName, material) in map)
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), displayName), material);
            mi.SaveAndReimport();
        }
    }
}
