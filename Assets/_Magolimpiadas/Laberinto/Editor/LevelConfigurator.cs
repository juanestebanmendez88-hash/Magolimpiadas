using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Labyrinth.Editor
{
    public static class LevelConfigurator
    {
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string PlayerMage = "Assets/_Magolimpiadas/Magos3D/Maga Nebulosoa/";
        const string Mino = "Assets/_Magolimpiadas/Minotauro/";
        const string Mat = "Assets/_Magolimpiadas/Laberinto/Materiales/";
        const string Anim = "Assets/_Magolimpiadas/Laberinto/Animaciones/";
        const string FireTexturePath = "Assets/_Magolimpiadas/Laberinto/Texturas/Fuego/particula_fuego.png";
        const string NavMeshPath = "Assets/_Magolimpiadas/Laberinto/NavMesh-Laberinto.asset";

        const string PlayerMageIdle = PlayerMage + "Maga_Nebulosa Standing Torch Idle 01.fbx";
        const string PlayerMageWalk = PlayerMage + "Maga_Nebulosa  Standing Torch Walk Forward.fbx";
        const string PlayerMageRun = PlayerMage + "Maga_Nebulosa  Standing Torch Run Forward.fbx";
        const string PlayerMageJump = PlayerMage + "Maga_Nebulosa  Standing Torch Jump.fbx";
        internal const string PlayerMageIgnite = PlayerMage + "Maga_Nebulosa  Standing Torch Light Torch From Wall.fbx";
        internal const string PlayerMageIdleFree = PlayerMage + "Maga_Nebulosa  Idle (3).fbx";
        internal const string PlayerMageWalkFree = PlayerMage + "Maga_Nebulosa  Walking (2).fbx";
        internal const string PlayerMageRunFree = PlayerMage + "Maga_Nebulosa  Slow Run.fbx";
        internal const string PlayerMageControlPath = Anim + "Maga.controller";

        const string MinoWalk = Mino + "Mutant Walking (1).fbx";
        const string MinoRun = Mino + "Mutant Run.fbx";
        const string MinoHit = Mino + "Mutant Punch.fbx";

        const int IgnoreRaycastLayer = 2;

        static readonly Vector3 TorchRotation = new Vector3(297.5f, 221.8f, 58.9f);

        [MenuItem("Magolimpiadas/Configurar Nivel")]
        public static void Configure()
        {
            ImportCharacter(PlayerMageIdle, new[] { PlayerMageWalk, PlayerMageRun, PlayerMageJump, PlayerMageIgnite, PlayerMageIdleFree, PlayerMageWalkFree, PlayerMageRunFree },
                              new[] { PlayerMageJump, PlayerMageIgnite });
            ImportCharacter(MinoWalk, new[] { MinoRun, MinoHit }, new[] { MinoHit });

            var playerMageMaterial = CreateCharacterMaterial("M_Maga", PlayerMage + "Meshy_AI_Moonspell_Mage_20261104_texture.png", PlayerMageIdle);
            var materialMino = CreateCharacterMaterial("M_Minotauro", Mino + "Meshy_AI_Emberhorn_Warlord_20261104_texture.png", MinoWalk);

            var playerMageControl = CreatePlayerMageControl();
            var controlMino = CreateMinotaurControl();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var playerMage = ConfigurePlayerMage(playerMageControl, playerMageMaterial);
            var minotaur = ConfigureMinotaur(controlMino, materialMino);
            var sceneCamera = ConfigureCamera(playerMage);

            playerMage.sceneCamera = sceneCamera.transform;
            playerMage.minotaur = minotaur;

            ConfigureShelters();
            ConfigureTeleports();
            BuildNavMesh(playerMage, minotaur);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LAB] Nivel configurado");
        }

        static void ImportCharacter(string basePath, string[] other, string[] withoutLoop)
        {
            ImportHumanoid(basePath, null, !withoutLoop.Contains(basePath));
            var avatar = AssetDatabase.LoadAllAssetsAtPath(basePath).OfType<Avatar>().First();
            if (!avatar.isValid || !avatar.isHuman)
                throw new System.InvalidOperationException("Avatar humanoide no válido: " + basePath);

            foreach (var path in other)
                ImportHumanoid(path, avatar, !withoutLoop.Contains(path));
        }

        internal static Avatar PlayerMageAvatar() => AssetDatabase.LoadAllAssetsAtPath(PlayerMageIdle).OfType<Avatar>().First();

        internal static void ImportHumanoid(string path, Avatar avatar, bool loop)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType = ModelImporterAnimationType.Human;
            if (avatar == null)
            {
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            else
            {
                mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                mi.sourceAvatar = avatar;
            }
            mi.SaveAndReimport();

            var clips = mi.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = loop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }

        internal static AnimationClip Clip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

        static Material CreateCharacterMaterial(string displayName, string texturePath, string modelPath)
        {
            var material = MaterialUrp(displayName, "Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);

            var mi = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            foreach (var original in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
            mi.SaveAndReimport();
            return material;
        }

        static Material MaterialUrp(string displayName, string shader)
        {
            string path = Mat + displayName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static AnimatorController CreatePlayerMageControl()
        {
            Directory.CreateDirectory(Anim);
            var control = AnimatorController.CreateAnimatorControllerAtPath(Anim + "Maga.controller");
            control.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            control.AddParameter("EnSuelo", AnimatorControllerParameterType.Bool);
            control.AddParameter("Saltar", AnimatorControllerParameterType.Trigger);

            var movement = control.CreateBlendTreeInController("Movimiento", out BlendTree tree);
            tree.blendParameter = "Velocidad";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(PlayerMageIdle), 0f);
            tree.AddChild(Clip(PlayerMageWalk), 1f);
            tree.AddChild(Clip(PlayerMageRun), 2f);

            var machine = control.layers[0].stateMachine;
            var jump = machine.AddState("Saltar");
            jump.motion = Clip(PlayerMageJump);

            var enter = machine.AddAnyStateTransition(jump);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Saltar");
            enter.duration = 0.1f;
            enter.canTransitionToSelf = false;

            var exit = jump.AddTransition(movement);
            exit.AddCondition(AnimatorConditionMode.If, 0f, "EnSuelo");
            exit.duration = 0.15f;

            AddIgnite(control);
            AddWithoutTorch(control);
            EditorUtility.SetDirty(control);
            return control;
        }

        internal static void AddWithoutTorch(AnimatorController control)
        {
            if (!control.parameters.Any(p => p.name == "ConAntorcha"))
                control.AddParameter(new AnimatorControllerParameter { name = "ConAntorcha", type = AnimatorControllerParameterType.Bool, defaultBool = true });

            var machine = control.layers[0].stateMachine;
            if (machine.states.Any(s => s.state.name == "MovimientoLibre")) return;

            AnimatorState State(string displayName) => machine.states.First(s => s.state.name == displayName).state;
            var withTorch = State("Movimiento");

            var free = control.CreateBlendTreeInController("MovimientoLibre", out BlendTree tree);
            tree.blendParameter = "Velocidad";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(PlayerMageIdleFree), 0f);
            tree.AddChild(Clip(PlayerMageWalkFree), 1f);
            tree.AddChild(Clip(PlayerMageRunFree), 2f);

            var drop = withTorch.AddTransition(free);
            drop.hasExitTime = false;
            drop.duration = 0.25f;
            drop.AddCondition(AnimatorConditionMode.IfNot, 0f, "ConAntorcha");
            var take = free.AddTransition(withTorch);
            take.hasExitTime = false;
            take.duration = 0.25f;
            take.AddCondition(AnimatorConditionMode.If, 0f, "ConAntorcha");

            foreach (var origin in new[] { "Saltar", "Encender" })
            {
                var state = machine.states.FirstOrDefault(s => s.state.name == origin).state;
                if (state == null) continue;
                foreach (var t in state.transitions.Where(t => t.destinationState == withTorch).ToList())
                {
                    t.AddCondition(AnimatorConditionMode.If, 0f, "ConAntorcha");
                    var copy = state.AddTransition(free);
                    copy.hasExitTime = t.hasExitTime;
                    copy.exitTime = t.exitTime;
                    copy.duration = t.duration;
                    foreach (var c in t.conditions.Where(c => c.parameter != "ConAntorcha")) copy.AddCondition(c.mode, c.threshold, c.parameter);
                    copy.AddCondition(AnimatorConditionMode.IfNot, 0f, "ConAntorcha");
                }
            }
            EditorUtility.SetDirty(control);
        }

        internal static void AddIgnite(AnimatorController control)
        {
            if (!control.parameters.Any(p => p.name == "Encender"))
                control.AddParameter("Encender", AnimatorControllerParameterType.Trigger);

            var machine = control.layers[0].stateMachine;
            if (machine.states.Any(s => s.state.name == "Encender")) return;

            var movement = machine.states.First(s => s.state.name == "Movimiento").state;
            var ignite = machine.AddState("Encender");
            ignite.motion = Clip(PlayerMageIgnite);

            var enter = machine.AddAnyStateTransition(ignite);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Encender");
            enter.duration = 0.2f;
            enter.canTransitionToSelf = false;

            var exit = ignite.AddTransition(movement);
            exit.hasExitTime = true;
            exit.exitTime = 0.92f;
            exit.duration = 0.25f;
            EditorUtility.SetDirty(control);
        }

        static AnimatorController CreateMinotaurControl()
        {
            Directory.CreateDirectory(Anim);
            var control = AnimatorController.CreateAnimatorControllerAtPath(Anim + "Minotauro.controller");
            control.AddParameter("Persiguiendo", AnimatorControllerParameterType.Bool);
            control.AddParameter("Atrapar", AnimatorControllerParameterType.Trigger);

            var machine = control.layers[0].stateMachine;
            var walk = machine.AddState("Caminar");
            walk.motion = Clip(MinoWalk);
            var run = machine.AddState("Correr");
            run.motion = Clip(MinoRun);
            var hit = machine.AddState("Golpe");
            hit.motion = Clip(MinoHit);
            machine.defaultState = walk;

            var toRun = walk.AddTransition(run);
            toRun.AddCondition(AnimatorConditionMode.If, 0f, "Persiguiendo");
            toRun.duration = 0.15f;

            var toWalk = run.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Persiguiendo");
            toWalk.duration = 0.2f;

            var toHit = machine.AddAnyStateTransition(hit);
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Atrapar");
            toHit.duration = 0.1f;
            toHit.canTransitionToSelf = false;

            var finish = hit.AddTransition(walk);
            finish.hasExitTime = true;
            finish.exitTime = 0.95f;
            finish.duration = 0.15f;

            EditorUtility.SetDirty(control);
            return control;
        }

        static GameObject Wrap(Animator model, string displayName)
        {
            var parent = model.transform.parent;
            if (parent != null && parent.name == displayName) return parent.gameObject;

            var root = new GameObject(displayName);
            root.transform.SetPositionAndRotation(model.transform.position, model.transform.rotation);
            model.transform.SetParent(root.transform, true);
            return root;
        }

        static Animator FindModel(string namePart) =>
            Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(a => a.name.Contains(namePart));

        static T GetOrAddComponent<T>(GameObject item) where T : Component
        {
            var found = item.GetComponent<T>();
            return found != null ? found : item.AddComponent<T>();
        }

        static void AssignMaterial(Animator model, Material material)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponentInParent<ParticleSystem>() != null || r.name == "Mango") continue;
                r.sharedMaterial = material;
            }
        }

        static float MeasureHeight(Animator model)
        {
            var max = float.MinValue;
            var min = float.MaxValue;
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                max = Mathf.Max(max, r.bounds.max.y);
                min = Mathf.Min(min, r.bounds.min.y);
            }
            return max - min;
        }

        static PlayerMage ConfigurePlayerMage(AnimatorController control, Material material)
        {
            var model = FindModel("Maga_Nebulosa");
            model.runtimeAnimatorController = control;
            model.applyRootMotion = false;
            model.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            AssignMaterial(model, material);

            var root = Wrap(model, "Maga");
            root.tag = "Player";

            float height = MeasureHeight(model);
            Debug.Log("[LAB] Altura maga: " + height.ToString("F2"));

            var body = GetOrAddComponent<CharacterController>(root);
            body.height = 2.1f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 1.09f, 0f);
            body.stepOffset = 0.3f;
            body.skinWidth = 0.04f;
            body.minMoveDistance = 0f;

            var player = GetOrAddComponent<PlayerMage>(root);
            player.animator = model;
            player.model = model.gameObject;

            CreateTorch(model);
            return player;
        }

        static Minotaur ConfigureMinotaur(AnimatorController control, Material material)
        {
            var model = FindModel("Mutant");
            model.runtimeAnimatorController = control;
            model.applyRootMotion = false;
            AssignMaterial(model, material);

            var root = Wrap(model, "Minotauro");
            Debug.Log("[LAB] Altura minotauro: " + MeasureHeight(model).ToString("F2"));

            var agent = GetOrAddComponent<NavMeshAgent>(root);
            agent.speed = 3f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.radius = 0.5f;
            agent.height = 2.4f;
            agent.stoppingDistance = 0.1f;

            var minotaur = GetOrAddComponent<Minotaur>(root);
            var points = GameObject.Find("Waypoints").transform;
            minotaur.Configure(agent, model, points.Cast<Transform>().OrderBy(t => t.name).ToArray(), null);
            return minotaur;
        }

        static ThirdPersonCamera ConfigureCamera(PlayerMage playerMage)
        {
            var item = Camera.main.gameObject;
            var sceneCamera = item.GetComponent<Camera>();
            sceneCamera.fieldOfView = 60f;
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 120f;

            var tracking = GetOrAddComponent<ThirdPersonCamera>(item);
            tracking.target = playerMage.transform;

            var gaze = playerMage.transform.position + Vector3.up * tracking.gazeHeight;
            item.transform.position = gaze + playerMage.transform.rotation * Quaternion.Euler(tracking.tilt, 0f, 0f) * Vector3.back * tracking.distance;
            item.transform.LookAt(gaze);
            return tracking;
        }

        static void ConfigureShelters()
        {
            foreach (var zone in GameObject.Find("Zonas").transform.Cast<Transform>().Where(t => t.name.StartsWith("Refugio")))
            {
                zone.gameObject.layer = IgnoreRaycastLayer;

                var box = GetOrAddComponent<BoxCollider>(zone.gameObject);
                box.isTrigger = true;
                box.center = new Vector3(0f, 0f, -1.5f);
                box.size = new Vector3(1f, 1f, 3f);
                GetOrAddComponent<SafeZone>(zone.gameObject);

                var child = zone.Find("BloqueoNavMesh");
                if (child == null)
                {
                    child = new GameObject("BloqueoNavMesh").transform;
                    child.SetParent(zone, false);
                }
                var block = GetOrAddComponent<NavMeshModifierVolume>(child.gameObject);
                block.center = new Vector3(0f, 0f, -1.5f);
                block.size = new Vector3(1f, 1f, 4f);
                block.area = 1;
            }
        }

        static void ConfigureTeleports()
        {
            var cubes = new[] { "Placa_Isla", "Placa_Este", "Placa_Sur" }.Select(GameObject.Find).ToArray();
            var portals = cubes.Select(GetOrAddComponent<Teleporter>).ToArray();

            for (int i = 0; i < cubes.Length; i++)
            {
                cubes[i].layer = IgnoreRaycastLayer;

                var box = cubes[i].GetComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0f, 8f, 0f);
                box.size = new Vector3(1f, 20f, 1f);

                portals[i].destinations = portals.Where(p => p != portals[i]).ToArray();
            }
        }

        static void BuildNavMesh(PlayerMage playerMage, Minotaur minotaur)
        {
            var root = GameObject.Find("LaberintoMinotauro");
            var surface = GetOrAddComponent<NavMeshSurface>(root);
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << IgnoreRaycastLayer);

            playerMage.gameObject.SetActive(false);
            minotaur.gameObject.SetActive(false);
            surface.BuildNavMesh();
            playerMage.gameObject.SetActive(true);
            minotaur.gameObject.SetActive(true);

            AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
        }

        static void CreateTorch(Animator model)
        {
            var hand = model.GetBoneTransform(HumanBodyBones.LeftHand);
            var finger = model.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);

            foreach (var previous in model.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Antorcha").ToArray())
                Object.DestroyImmediate(previous.gameObject);

            var torch = new GameObject("Antorcha").transform;
            torch.SetParent(hand, false);
            var scale = hand.lossyScale;
            torch.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            torch.position = Vector3.Lerp(hand.position, finger.position, 0.8f);
            torch.localRotation = Quaternion.Euler(TorchRotation);

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Mango";
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            handle.transform.SetParent(torch, false);
            handle.transform.localScale = new Vector3(0.05f, 0.35f, 0.05f);
            handle.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            var handleMaterial = MaterialUrp("M_AntorchaMango", "Universal Render Pipeline/Lit");
            handleMaterial.SetColor("_BaseColor", new Color(0.3f, 0.17f, 0.08f));
            handleMaterial.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(handleMaterial);
            handle.GetComponent<Renderer>().sharedMaterial = handleMaterial;

            var tip = new GameObject("Fuego").transform;
            tip.SetParent(torch, false);
            tip.localPosition = new Vector3(0f, 0.5f, 0f);
            RealisticFire.Create(tip, tip.position - Vector3.up * 0.05f, 0.34f);

            var lightSource = tip.gameObject.AddComponent<Light>();
            lightSource.type = LightType.Point;
            lightSource.color = new Color(1f, 0.6f, 0.25f);
            lightSource.intensity = 4f;
            lightSource.range = 7f;
        }

        static void CreateFire(GameObject item)
        {
            var fire = item.AddComponent<ParticleSystem>();
            fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var principal = fire.main;
            principal.loop = true;
            principal.playOnAwake = true;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            principal.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            principal.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
            principal.startColor = new Color(1f, 0.75f, 0.35f);
            principal.gravityModifier = -0.08f;
            principal.simulationSpace = ParticleSystemSimulationSpace.Local;
            principal.maxParticles = 80;

            var emission = fire.emission;
            emission.rateOverTime = 45f;

            var shape = fire.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var color = fire.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.7f, 0.12f, 0.03f), 1f),
                },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var size = fire.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            var render = item.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = FireMaterial();
            render.renderMode = ParticleSystemRenderMode.Billboard;
        }

        static Material FireMaterial()
        {
            var material = MaterialUrp("M_AntorchaFuego", "Universal Render Pipeline/Particles/Unlit");
            material.SetTexture("_BaseMap", FireTexture());
            material.SetColor("_BaseColor", Color.white);

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D FireTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(FireTexturePath);
            if (existing != null) return existing;

            const int Side = 64;
            var texture = new Texture2D(Side, Side, TextureFormat.RGBA32, false);
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                {
                    float dx = (x + 0.5f) / Side - 0.5f, dy = (y + 0.5f) / Side - 0.5f;
                    float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, t * t * (3f - 2f * t)));
                }
            Directory.CreateDirectory(Path.GetDirectoryName(FireTexturePath));
            File.WriteAllBytes(FireTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(FireTexturePath);
            var ti = (TextureImporter)AssetImporter.GetAtPath(FireTexturePath);
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(FireTexturePath);
        }
    }
}
