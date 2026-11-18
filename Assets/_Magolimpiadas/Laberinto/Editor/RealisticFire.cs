using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Labyrinth.Editor
{
    public static class RealisticFire
    {
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string FirePath = "Assets/Paquetes externos/portalefectos y mas/Prefab/FireSmall.prefab";
        const string FlameMaterialPath = "Assets/Paquetes externos/portalefectos y mas/Material/FX/Fire 1.mat";
        const string CauldronMaterialPath = "Assets/_Magolimpiadas/Laberinto/Materiales/M_FuegoCaldero.mat";
        internal const string Name = "FuegoRealista";

        internal static void FixFlameMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
            m.SetFloat("_ZTestMode", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            m.EnableKeyword("ENABLE_SOFTPARTICLES");
            m.SetFloat("_InvFade", 3f);
            EditorUtility.SetDirty(m);
        }

        internal static Material CauldronMaterial()
        {
            FixFlameMaterial();
            var m = AssetDatabase.LoadAssetAtPath<Material>(CauldronMaterialPath);
            if (m == null)
            {
                m = new Material(AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath));
                AssetDatabase.CreateAsset(m, CauldronMaterialPath);
            }
            m.CopyPropertiesFromMaterial(AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath));
            m.shaderKeywords = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath).shaderKeywords;
            m.SetFloat("_InvFade", 0.6f);
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static void GiveVolume(GameObject fire, float flamesPerSecond, float radius, Material material)
        {
            var flame = fire.transform.Find("Fire").GetComponent<ParticleSystem>();
            var principal = flame.main;
            float width = principal.startSizeX.constantMax, heightValue = principal.startSizeY.constantMax;
            principal.startSizeX = new ParticleSystem.MinMaxCurve(width * 0.7f, width);
            principal.startSizeY = new ParticleSystem.MinMaxCurve(heightValue * 0.75f, heightValue);
            principal.maxParticles = Mathf.CeilToInt(flamesPerSecond * 3f);

            var emission = flame.emission;
            emission.rateOverTime = flamesPerSecond;

            var shape = flame.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius / Mathf.Max(0.001f, flame.transform.lossyScale.x);
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            if (material != null) flame.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            EditorUtility.SetDirty(flame);
        }

        [MenuItem("Magolimpiadas/Fuego del Caldero con Volumen")]
        public static void CauldronVolume()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            var cinematic = Object.FindAnyObjectByType<IslandCinematic>();
            var fires = cinematic.cauldronFire.transform.Cast<Transform>().Where(t => t.name == Name).OrderByDescending(t => t.lossyScale.x).ToList();
            if (fires.Count == 0) { Debug.LogWarning("[LAB] El caldero no tiene fuegos"); return; }

            var cauldronRadius = IslandConfigurator.Limits(cinematic.cauldron).extents.x;
            GiveVolume(fires[0].gameObject, 5f, cauldronRadius * 0.32f, CauldronMaterial());
            foreach (var small in fires.Skip(1)) small.gameObject.SetActive(false);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LAB] Caldero: un fuego grande con volumen; " + (fires.Count - 1) + " fuegos chicos apagados");
        }

        const float FlameHeight = 0.35f;

        internal static GameObject Create(Transform parent, Vector3 base_, float heightValue, float rotationY = 0f, bool withSmoke = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirePath);
            var fire = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            fire.name = Name;
            fire.transform.SetPositionAndRotation(base_, Quaternion.Euler(0f, rotationY, 0f));
            float scale = heightValue / FlameHeight;
            var p = parent.lossyScale;
            fire.transform.localScale = new Vector3(scale / p.x, scale / p.y, scale / p.z);

            foreach (var ps in fire.GetComponentsInChildren<ParticleSystem>(true))
            {
                var principal = ps.main;
                principal.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            if (!withSmoke)
            {
                var smoke = fire.transform.Find("Smoke");
                if (smoke != null) smoke.gameObject.SetActive(false);
            }
            return fire;
        }

        static void DeletePrevious(Transform t)
        {
            foreach (var child in t.Cast<Transform>().Where(h => h.name == Name).ToList())
                Object.DestroyImmediate(child.gameObject);
        }

        [MenuItem("Magolimpiadas/Fuegos Realistas y Cielo")]
        public static void Configure()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Sky();
            FixFlameMaterial();
            int braziers = Braziers();
            Torch();
            Cauldron();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LAB] Fuegos realistas: " + braziers + " pebeteros, antorcha y caldero; cámaras con cielo");
        }

        static void Sky()
        {
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(cam);
            }
        }

        static int Braziers()
        {
            var flames = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.name.StartsWith("Kit_Pebetero_Llama")).ToList();
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Random.InitState(7);

            foreach (var flame in flames)
            {
                var b = flame.bounds;
                DeletePrevious(flame.transform);
                flame.enabled = false;
                EditorUtility.SetDirty(flame);
                Create(flame.transform, new Vector3(b.center.x, b.min.y + 0.05f, b.center.z), 0.95f, Random.Range(0f, 360f));

                foreach (var lightSource in lights.Where(l => l.type == LightType.Point && Vector3.Distance(l.transform.position, b.center) < 1.5f))
                {
                    var flicker = lightSource.GetComponent<LightFlicker>();
                    if (flicker == null)
                    {
                        flicker = lightSource.gameObject.AddComponent<LightFlicker>();
                        flicker.intensity = lightSource.intensity;
                    }
                    flicker.variation = 0.18f;
                    flicker.speed = 7f;
                    EditorUtility.SetDirty(flicker);
                }
            }
            return flames.Count;
        }

        static void Torch()
        {
            var playerMage = Object.FindAnyObjectByType<PlayerMage>();
            var tip = playerMage.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Fuego" && t.parent != null && t.parent.name == "Antorcha");
            if (tip == null) { Debug.LogWarning("[LAB] No encontré el fuego de la antorcha"); return; }
            ChangeTorch(tip);
        }

        internal static void ChangeTorch(Transform tip)
        {
            var render = tip.GetComponent<ParticleSystemRenderer>();
            if (render != null) Object.DestroyImmediate(render);
            var old = tip.GetComponent<ParticleSystem>();
            if (old != null) Object.DestroyImmediate(old);
            DeletePrevious(tip);
            Create(tip, tip.position - Vector3.up * 0.05f, 0.34f);
        }

        static void Cauldron()
        {
            var cinematic = Object.FindAnyObjectByType<IslandCinematic>();
            if (cinematic == null || cinematic.cauldron == null) { Debug.LogWarning("[LAB] No encontré la cinemática del caldero"); return; }
            cinematic.cauldronFire = IslandConfigurator.CreateFire(cinematic.transform, IslandConfigurator.Limits(cinematic.cauldron));
            EditorUtility.SetDirty(cinematic);
        }
    }
}
