using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Labyrinth.Editor
{
    public static class MageLabyrinthsBuilder
    {
        const string Prefix = "Laberinto de ";
        const string NightSky = "Assets/_Magolimpiadas/Cielo/Cold Night/Materials/Cold Night Equirect.mat";
        const string PinkSky = "Assets/_Magolimpiadas/Cielo/Epic_GloriousPink/Materials/Epic_GloriousPink_EquiRect.mat";

        static readonly (string id, string displayName, string model, string portal, string bow, Vector3 displacement, Vector3 place)[] Mages =
        {
            ("elemental", "Mago Elemental", "Assets/_Magolimpiadas/Magos3D/Modelos/MagoElemental.fbx", "PortalElemental", "Arco Elemental", new Vector3(-400f, 0f, 0f), new Vector3(1.6f, 0f, -1.8f)),
            ("druida", "Druida Verde", "Assets/_Magolimpiadas/Magos3D/Modelos/Druida.fbx", "PortalDruida", "Arco Druida", new Vector3(-800f, 0f, 0f), new Vector3(-1.6f, 0f, -1.8f)),
            ("rayo", "Mago del Rayo", "Assets/_Magolimpiadas/Magos3D/Modelos/Mago_del_Rayo.fbx", "PortalRayo", "Arco Rayo", new Vector3(-1200f, 0f, 0f), new Vector3(0f, 0f, -3.2f)),
        };

        [MenuItem("Magolimpiadas/Laberintos de los Magos (3D)")]
        public static void Build()
        {
            var playerMage = Object.FindFirstObjectByType<PlayerMage>();
            var original = Root(playerMage, "LaberintoMinotauro");
            var portals = Root(playerMage, "Portales");
            var ambience = Root(playerMage, "Ambiente");
            var bows = Root(playerMage, "Arcos");
            var cinematic = Object.FindFirstObjectByType<IslandCinematic>();
            if (playerMage == null || original == null || portals == null || cinematic == null)
            {
                EditorUtility.DisplayDialog("Laberintos de los magos", "Falta la maga, el laberinto (LaberintoMinotauro), los Portales o la CinematicaIsla en la escena.", "OK");
                return;
            }

            foreach (var r in playerMage.gameObject.scene.GetRootGameObjects().Where(r => r.name.StartsWith(Prefix)).ToList())
                Undo.DestroyObjectImmediate(r);

            Bounds zone = playerMage.labyrinthZone;
            float scale = ModelsScale(playerMage);

            PrepareAnimations("Assets/_Magolimpiadas/Magos3D/Modelos/Maga_Nebulosa.fbx", null);
            var controls = Mages.ToDictionary(x => x.displayName, x => PrepareAnimations(x.model, playerMage.animator.runtimeAnimatorController));
            var bots = new List<BotMage3D>();
            var zones = new List<Bounds> { zone };
            var hide = LabyrinthObjects(playerMage, original, ambience, portals, zone);

            foreach (var m in Mages)
            {
                var root = new GameObject(Prefix + m.displayName);
                Undo.RegisterCreatedObjectUndo(root, "Laberintos de los magos");

                Duplicate(original, root.transform);
                if (ambience != null) Prune(Duplicate(ambience, root.transform), zone, 2);
                Prune(Duplicate(portals, root.transform), zone, 1);
                Clear(root);
                root.transform.position = m.displacement;

                var bot = CreateBot(m.displayName, m.model, playerMage, controls[m.displayName], scale, root.transform,
                                   playerMage.transform.position + m.displacement, playerMage.transform.rotation);
                bot.islandPortal = portals.transform.Find(m.portal);
                bot.placeNextToLeader = m.place;
                bot.mageId = m.id;
                if (bot.islandPortal == null) Debug.LogWarning("[Laberintos] No encontré el portal " + m.portal);
                if (bows != null) bot.bow = PrepareBow(bows.transform, m.bow, bot);
                bots.Add(bot);

                var z = zone;
                z.center += m.displacement;
                zones.Add(z);
                hide.Add(root);
            }

            Undo.RecordObject(cinematic, "Laberintos de los magos");
            cinematic.otherMages = bots.ToArray();
            EditorUtility.SetDirty(cinematic);

            PrepareSky(playerMage, zones, hide);

            if (Object.FindFirstObjectByType<ChosenMage>() == null)
            {
                var chosen = new GameObject("Mago elegido");
                Undo.RegisterCreatedObjectUndo(chosen, "Mago elegido");
                chosen.AddComponent<ChosenMage>();
            }

            EditorSceneManager.MarkSceneDirty(playerMage.gameObject.scene);
            Debug.Log("[Laberintos] Listo: 3 laberintos con su mago (" + string.Join(", ", Mages.Select(x => x.displayName)) + ").");
        }

        static GameObject Root(PlayerMage playerMage, string displayName) =>
            playerMage == null ? null : playerMage.gameObject.scene.GetRootGameObjects().FirstOrDefault(r => r.name == displayName);

        static GameObject Duplicate(GameObject original, Transform parent)
        {
            var copy = Object.Instantiate(original, original.transform.position, original.transform.rotation, parent);
            Undo.RegisterCreatedObjectUndo(copy, "Laberintos de los magos");
            copy.name = original.name;
            return copy;
        }

        static List<GameObject> LabyrinthObjects(PlayerMage playerMage, GameObject original, GameObject ambience, GameObject portals, Bounds zone)
        {
            var list = new List<GameObject> { original };
            void AddIfInside(Transform t) { if (Inside(t.position, zone)) list.Add(t.gameObject); }
            if (ambience != null)
                foreach (Transform child in ambience.transform)
                {
                    if (child.childCount == 0) { AddIfInside(child); continue; }
                    foreach (Transform grandchild in child) AddIfInside(grandchild);
                }
            foreach (Transform p in portals.transform) AddIfInside(p);
            foreach (var displayName in new[] { "Minotauro", "Entrada Templo 2D (cubo)" })
            {
                var r = Root(playerMage, displayName);
                if (r != null) list.Add(r);
            }
            return list;
        }

        static bool Inside(Vector3 p, Bounds zone) { p.y = zone.center.y; return zone.Contains(p); }

        static void Prune(GameObject copy, Bounds zone, int levels)
        {
            foreach (Transform child in copy.transform.Cast<Transform>().ToList())
            {
                if (levels > 1 && child.childCount > 0) { Prune(child.gameObject, zone, levels - 1); continue; }
                Vector3 p = child.position;
                p.y = zone.center.y;
                if (!zone.Contains(p)) Object.DestroyImmediate(child.gameObject);
            }
        }

        static void Clear(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<NavMeshSurface>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<IslandTrip>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<FloorSwitch>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Teleporter>(true)) Object.DestroyImmediate(c);
        }

        const string AnimationsFolder = "Assets/_Magolimpiadas/Magos3D/Animaciones";

        static readonly (string mixamo, string blender, bool loop)[] Equivalences =
        {
            ("Standing Torch Idle 01", "Standing Torch Idle 01", true),
            ("Standing Torch Walk Forward", "Standing Torch Walk Forward", true),
            ("Standing Torch Run Forward", "Standing Torch Run Forward", true),
            ("Standing Torch Jump", "Standing Torch Jump", false),
            ("Standing Torch Light Torch From Wall", "Standing Torch Light Torch From Wall", false),
            ("Idle (3)", "Idle 3", true),
            ("Walking (2)", "Walking 2", true),
            ("Slow Run", "Slow Run", true),
            ("Sitting Yell", "Sitting Yell", false),
            ("Standing Draw Arrow", "Standing Draw Arrow", false),
        };

        static string Name(string fbxPath) => System.IO.Path.GetFileNameWithoutExtension(fbxPath);

        public static string ControlPath(string fbxPath) => AnimationsFolder + "/" + Name(fbxPath) + ".overrideController";

        public static string RestPath(string fbxPath) => AnimationsFolder + "/" + Name(fbxPath) + "_Reposo.controller";

        public static RuntimeAnimatorController PrepareAnimations(string fbxPath, RuntimeAnimatorController playerMageControl)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                string displayName = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.IndexOf('|') + 1) : c.takeName;
                var eq = Equivalences.FirstOrDefault(e => e.blender == displayName);
                c.name = displayName;
                c.loopTime = eq.blender != null && eq.loop;
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
            var ownItems = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);

            if (!AssetDatabase.IsValidFolder("Assets/_Magolimpiadas/Magos3D")) AssetDatabase.CreateFolder("Assets", "Magos3D");
            if (!AssetDatabase.IsValidFolder(AnimationsFolder)) AssetDatabase.CreateFolder("Assets/_Magolimpiadas/Magos3D", "Animaciones");

            AnimatorOverrideController over = null;
            if (playerMageControl != null)
            {
                over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ControlPath(fbxPath));
                if (over == null)
                {
                    over = new AnimatorOverrideController(playerMageControl);
                    AssetDatabase.CreateAsset(over, ControlPath(fbxPath));
                }
                over.runtimeAnimatorController = playerMageControl;
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                over.GetOverrides(pairs);
                for (int i = 0; i < pairs.Count; i++)
                {
                    string file = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(pairs[i].Key));
                    var eq = Equivalences.FirstOrDefault(e => file.EndsWith(e.mixamo));
                    if (eq.blender != null && ownItems.TryGetValue(eq.blender, out var own))
                        pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, own);
                    else
                        Debug.LogWarning("[Laberintos] Sin animación equivalente para " + file);
                }
                over.ApplyOverrides(pairs);
                EditorUtility.SetDirty(over);
            }

            if (ownItems.TryGetValue("Idle 3", out var idle))
            {
                var menu = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(RestPath(fbxPath));
                if (menu == null) UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(RestPath(fbxPath), idle);
                else
                {
                    var state = menu.layers[0].stateMachine.defaultState;
                    state.motion = idle;
                    EditorUtility.SetDirty(state);
                    EditorUtility.SetDirty(menu);
                }
            }
            AssetDatabase.SaveAssets();
            return over;
        }

        static float ModelsScale(PlayerMage playerMage)
        {
            var playerMageHip = playerMage.animator.GetBoneTransform(HumanBodyBones.Hips);
            float playerMageHeight = playerMageHip.position.y - playerMage.transform.position.y;
            var test = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Magolimpiadas/Magos3D/Modelos/Maga_Nebulosa.fbx"));
            var hip = test.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
            float fbxHeight = hip.position.y - test.transform.position.y;
            Object.DestroyImmediate(test);
            return fbxHeight > 0.01f ? playerMageHeight / fbxHeight : 1f;
        }

        static BotMage3D CreateBot(string displayName, string modelPath, PlayerMage playerMage, RuntimeAnimatorController control, float scale, Transform parent, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject(displayName);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(position, rotation);

            var playerController = playerMage.GetComponent<CharacterController>();
            var cc = root.AddComponent<CharacterController>();
            cc.height = playerController.height;
            cc.radius = playerController.radius;
            cc.center = playerController.center;
            cc.slopeLimit = playerController.slopeLimit;
            cc.stepOffset = playerController.stepOffset;
            cc.skinWidth = playerController.skinWidth;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * scale;
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = control;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var bot = root.AddComponent<BotMage3D>();
            bot.displayName = displayName;
            bot.animator = animator;
            bot.torch = CopyTorch(playerMage, animator);
            return bot;
        }

        static GameObject CopyTorch(PlayerMage playerMage, Animator animator)
        {
            if (playerMage.torch == null) return null;
            var playerMageHand = playerMage.animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var t = playerMage.torch.transform;
            Vector3 displacement = Quaternion.Inverse(playerMageHand.rotation) * (t.position - playerMageHand.position);
            Quaternion rotation = Quaternion.Inverse(playerMageHand.rotation) * t.rotation;
            Vector3 worldScale = t.lossyScale;

            var copy = Object.Instantiate(playerMage.torch);
            copy.name = playerMage.torch.name;
            copy.SetActive(true);
            copy.transform.SetPositionAndRotation(hand.position + hand.rotation * displacement, hand.rotation * rotation);
            copy.transform.SetParent(hand, true);
            var l = hand.lossyScale;
            copy.transform.localScale = new Vector3(worldScale.x / l.x, worldScale.y / l.y, worldScale.z / l.z);
            return copy;
        }

        static Collectible PrepareBow(Transform bows, string bowName, BotMage3D owner)
        {
            var bow = bows.Find(bowName);
            var model = bows.GetComponentsInChildren<Collectible>(true).FirstOrDefault(r => r.owner == null);
            if (bow == null) { Debug.LogWarning("[Laberintos] No encontré " + bowName); return null; }

            var rec = bow.GetComponent<Collectible>();
            if (rec == null) rec = Undo.AddComponent<Collectible>(bow.gameObject);
            Undo.RecordObject(rec, "Arco");
            if (model != null && model != rec) EditorUtility.CopySerialized(model, rec);
            rec.item = bow.Find("Arco");
            var circle = bow.Find("Círculo mágico");
            rec.turnOffToCollect = circle != null ? new[] { circle.gameObject } : new GameObject[0];
            rec.owner = owner;
            EditorUtility.SetDirty(rec);

            var cap = bow.GetComponent<CapsuleCollider>();
            if (cap == null) cap = Undo.AddComponent<CapsuleCollider>(bow.gameObject);
            var modelCapsule = model != null ? model.GetComponent<CapsuleCollider>() : null;
            if (modelCapsule != null && modelCapsule != cap)
            {
                cap.center = modelCapsule.center;
                cap.radius = modelCapsule.radius;
                cap.height = modelCapsule.height;
                cap.direction = modelCapsule.direction;
            }
            cap.isTrigger = true;
            return rec;
        }

        static void PrepareSky(PlayerMage playerMage, List<Bounds> zones, List<GameObject> hide)
        {
            var sky = Object.FindFirstObjectByType<ZoneSky>();
            if (sky == null)
            {
                var go = new GameObject("Cielo por zona");
                Undo.RegisterCreatedObjectUndo(go, "Cielo");
                sky = go.AddComponent<ZoneSky>();
                sky.islandAmbience = RenderSettings.ambientLight;
                var sun = GameObject.Find("Luz Direccional");
                if (sun != null && sun.GetComponent<Light>() != null)
                {
                    sky.sun = sun.GetComponent<Light>();
                    sky.islandSun = sky.sun.intensity;
                    sky.islandSunColor = sky.sun.color;
                }
            }
            Undo.RecordObject(sky, "Cielo");
            sky.follow = playerMage.transform;
            if (sky.sceneCamera == null && Camera.main != null) sky.sceneCamera = Camera.main;
            sky.labyrinthZones = zones;
            sky.labyrinths = hide;
            if (sky.labyrinthSky == null) sky.labyrinthSky = AssetDatabase.LoadAssetAtPath<Material>(NightSky);
            if (sky.islandSky == null) sky.islandSky = AssetDatabase.LoadAssetAtPath<Material>(PinkSky);
            EditorUtility.SetDirty(sky);
        }
    }
}
