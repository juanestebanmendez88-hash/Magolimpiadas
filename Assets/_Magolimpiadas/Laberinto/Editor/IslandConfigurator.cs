using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Labyrinth.Editor
{
    public static class IslandConfigurator
    {
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";

        [MenuItem("Magolimpiadas/Configurar Isla y Portal")]
        public static void Configure()
        {
            LevelConfigurator.ImportHumanoid(LevelConfigurator.PlayerMageIgnite, LevelConfigurator.PlayerMageAvatar(), false);
            var control = AssetDatabase.LoadAssetAtPath<AnimatorController>(LevelConfigurator.PlayerMageControlPath);
            LevelConfigurator.AddIgnite(control);
            AssetDatabase.SaveAssets();
            float igniteDuration = LevelConfigurator.Clip(LevelConfigurator.PlayerMageIgnite).length;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var floorSwitch = Find("LaberintoMinotauro/Mecanismos/Interruptor");
            var portal = Find("Portales/PortalLaberinto1");

            ConfigureIslands(floorSwitch);
            ConfigureSwitch(floorSwitch, portal);
            var cinematic = ConfigureCinematic(igniteDuration);
            ConfigurePortal(portal, cinematic);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LAB] Isla y portal configurados (animación de encender: " + igniteDuration.ToString("F2") + " s)");
        }

        static Transform Find(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) throw new System.InvalidOperationException("No encontré " + path + " en la escena");
            return go.transform;
        }

        static T Get<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void ConfigureIslands(Transform floorSwitch)
        {
            var islands = Find("LaberintoMinotauro/FosoPuas/Islas");
            int i = 0;
            foreach (Transform island in islands)
            {
                var hover = Get<FloatingIsland>(island.gameObject);
                hover.height = 0.12f + 0.02f * (i % 3);
                hover.period = 3.8f + 0.45f * i;
                hover.offset = (i * 0.29f) % 1f;
                hover.companions.Clear();
                if (island.name == "Isla_Interruptor") hover.companions.Add(floorSwitch);
                EditorUtility.SetDirty(hover);
                i++;
            }
        }

        static void ConfigureSwitch(Transform root, Transform portal)
        {
            var floorSwitch = Get<FloorSwitch>(root.gameObject);
            floorSwitch.lever = root.Find("Palanca");
            floorSwitch.partsWithColor = new[] { root.Find("Base"), root.Find("Palanca") }
                .Where(t => t != null).Select(t => t.GetComponent<Renderer>()).ToArray();
            floorSwitch.activate = new[] { portal.gameObject };
            floorSwitch.glow = 0.9f;
            EditorUtility.SetDirty(floorSwitch);

            var zone = root.GetComponents<SphereCollider>().FirstOrDefault(c => c.isTrigger) ?? root.gameObject.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.6f, 0f);
            zone.radius = 1.1f;
        }

        static void ConfigurePortal(Transform portal, IslandCinematic cinematic)
        {
            var trip = Get<IslandTrip>(portal.gameObject);
            trip.cinematic = cinematic;
            EditorUtility.SetDirty(trip);

            var disc = portal.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault();
            var box = portal.GetComponents<BoxCollider>().FirstOrDefault(c => c.isTrigger) ?? portal.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            if (disc != null)
            {
                var b = disc.bounds;
                var min = Vector3.positiveInfinity; var max = Vector3.negativeInfinity;
                for (int k = 0; k < 8; k++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    var local = portal.InverseTransformPoint(corner);
                    min = Vector3.Min(min, local); max = Vector3.Max(max, local);
                }
                var sizeValue = max - min;
                if (sizeValue.x <= sizeValue.y && sizeValue.x <= sizeValue.z) sizeValue.x = 2f; else if (sizeValue.z <= sizeValue.y) sizeValue.z = 2f; else sizeValue.y = 2f;
                box.center = (min + max) * 0.5f;
                box.size = sizeValue;
            }
            else
            {
                box.size = new Vector3(2.4f, 2.6f, 2f);
            }
        }

        static IslandCinematic ConfigureCinematic(float igniteDuration)
        {
            var cauldron = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "Caldero_01");
            var playerMagePortal = Find("Portales/PortalMaga");
            var gameCamera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            var effect = Object.FindObjectsByType<Teleporter>(FindObjectsSortMode.None)
                .Select(t => t.teleportEffect).FirstOrDefault(e => e != null);

            var root = GameObject.Find("CinematicaIsla") ?? new GameObject("CinematicaIsla");
            var cinematic = Get<IslandCinematic>(root);

            var limits = Limits(cauldron);
            root.transform.position = limits.center;

            cinematic.gameCamera = gameCamera;
            cinematic.cinematicCamera = CreateCamera(root.transform, gameCamera.GetComponent<Camera>());
            cinematic.arrivalPortal = playerMagePortal;
            cinematic.cauldron = cauldron;
            cinematic.islandCenter = cauldron;
            cinematic.teleportEffect = effect;
            cinematic.cauldronFire = CreateFire(root.transform, limits);
            InterfaceConfigurator.Ensure(cinematic);
            cinematic.igniteDuration = igniteDuration;

            var island = Find("IslaMagica/Isla_MagOlimpiadas");
            var islandBounds = Limits(island);
            cinematic.orbitRadius = Mathf.Max(islandBounds.extents.x, islandBounds.extents.z) * 1.35f;
            cinematic.orbitHeight = islandBounds.extents.y * 0.9f + 8f;
            EditorUtility.SetDirty(cinematic);
            return cinematic;
        }

        internal static Bounds Limits(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Camera CreateCamera(Transform parent, Camera principal)
        {
            var t = parent.Find("CamaraCinematica") ?? new GameObject("CamaraCinematica").transform;
            t.SetParent(parent, false);
            var cam = Get<Camera>(t.gameObject);
            cam.fieldOfView = principal.fieldOfView;
            cam.nearClipPlane = principal.nearClipPlane;
            cam.farClipPlane = Mathf.Max(principal.farClipPlane, 600f);
            cam.clearFlags = principal.clearFlags;
            cam.backgroundColor = principal.backgroundColor;
            cam.enabled = false;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            return cam;
        }

        internal static GameObject CreateFire(Transform parent, Bounds cauldron)
        {
            var previous = parent.Find("FuegoCaldero");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);

            float radius = Mathf.Max(cauldron.extents.x, cauldron.extents.z);
            var fire = new GameObject("FuegoCaldero").transform;
            fire.SetParent(parent, false);
            fire.position = new Vector3(cauldron.center.x, cauldron.max.y - 0.25f, cauldron.center.z);

            var big = RealisticFire.Create(fire, fire.position + Vector3.down * 1.75f, radius * 1.7f);
            RealisticFire.GiveVolume(big, 5f, radius * 0.32f, RealisticFire.CauldronMaterial());

            var lightSource = new GameObject("Luz").transform;
            lightSource.SetParent(fire, false);
            lightSource.localPosition = Vector3.up * radius * 0.4f;
            var l = lightSource.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.55f, 0.2f);
            l.range = radius * 5.5f;
            l.intensity = 0f;
            l.shadows = LightShadows.None;
            var flicker = lightSource.gameObject.AddComponent<LightFlicker>();
            flicker.intensity = 10f;

            fire.gameObject.SetActive(false);
            return fire.gameObject;
        }
    }
}
