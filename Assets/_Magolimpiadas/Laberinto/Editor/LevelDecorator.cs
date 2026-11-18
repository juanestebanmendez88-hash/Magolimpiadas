using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Labyrinth.Editor
{
    public static class LevelDecorator
    {
        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string Mat = "Assets/_Magolimpiadas/Laberinto/Materiales/";
        const string Prefabs = "Assets/_Magolimpiadas/Laberinto/Prefabs/Ambiente/";
        const string GrassTex = "Assets/_Magolimpiadas/Laberinto/Texturas/Pasto/";
        const string IvyPath = "Assets/_Magolimpiadas/Laberinto/Modelos/ivy.fbx";
        const string BushMeshPath = "Assets/_Magolimpiadas/Laberinto/Modelos/MataPasto.asset";
        const string CurtainPath = "Assets/_Magolimpiadas/Laberinto/Cortina de runas/PA_Oldtext.prefab";
        const string PlatformPath = "Assets/_Magolimpiadas/Laberinto/Modelos/runic_platform.glb";

        const int Seed = 7;
        const float GrassProbability = 0.55f;
        const float DistanceBetweenIvys = 5.5f;
        const int MaxWallIvys = 12;
        const int MaxCornerIvys = 7;
        const int GroundIvys = 4;

        const string CirclePath = "Assets/Paquetes externos/Hovl Studio/Magic effects pack/Prefabs/Magic circles/Magic circle.prefab";
        const string TeleportEffectPath = "Assets/Paquetes externos/Hovl Studio/Magic effects pack/Prefabs/Environment/Teleport.prefab";
        const string ProfilePath = "Assets/_Magolimpiadas/Laberinto/Ajustes/PostProcesoLaberinto.asset";
        const string RunesPath = "Assets/_Magolimpiadas/Laberinto/Cortina de runas/OldText2.png";

        const float PlatformDiameter = 1.9f;
        const float CircleScale = 0.65f;
        const float CurtainHeight = 2.7f;

        static readonly Vector2Int[] Directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        [MenuItem("Magolimpiadas/Ambientar Laberinto")]
        public static void Decorate()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var random = new System.Random(Seed);
            var grassPrefabs = CreateGrassPrefabs();
            var prefabsIvy = CreatePrefabsIvy();

            var previous = GameObject.Find("Ambiente");
            if (previous != null) Object.DestroyImmediate(previous);
            var ambience = new GameObject("Ambiente").transform;

            var faces = WallFaces();
            var teleports = Object.FindObjectsByType<Teleporter>(FindObjectsSortMode.None);

            int grass = PlaceGrass(Group("Pasto", ambience), faces, grassPrefabs, teleports, random);
            int ivys = PlaceIvy(Group("Ivy", ambience), faces, prefabsIvy, teleports, random);
            int curtains = PlaceCurtains(Group("Cortinas", ambience));
            PlacePlatforms(teleports);
            ApplyEffectsInScene();

            EditorSceneManager.MarkSceneDirty(scene);
            if (Application.isBatchMode) EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LAB] Ambiente: " + grass + " matas, " + ivys + " ivys, " + curtains + " cortinas, " + teleports.Length + " plataformas");
        }

        struct Face
        {
            public int col, row;
            public Vector3 point, normal, tangent;
            public bool inShelter;
        }

        static bool Walkable(char c) => c == '.' || c == 'r' || c == 'E' || c == 'S';

        static List<Face> WallFaces()
        {
            var pieces = GameObject.Find("Muros").GetComponentsInChildren<Transform>()
                .Where(t => t.name.Contains("lowpoly")).Select(t => new Vector2(t.position.x, t.position.z)).ToList();

            var faces = new List<Face>();
            for (int f = LabyrinthBuilder.InitialRow; f < LabyrinthBuilder.InitialRow + LabyrinthBuilder.Rows; f++)
                for (int c = 0; c < LabyrinthBuilder.Columns; c++)
                {
                    char cell = LabyrinthBuilder.In(c, f);
                    if (!Walkable(cell)) continue;

                    foreach (var d in Directions)
                    {
                        if (LabyrinthBuilder.In(c + d.x, f + d.y) != '#') continue;

                        var towardWall = new Vector3(d.x, 0f, -d.y);
                        var point = LabyrinthBuilder.CellCenter(c, f) + towardWall * (LabyrinthBuilder.Cell * 0.5f);
                        var behind = new Vector2(point.x + towardWall.x * 0.75f, point.z + towardWall.z * 0.75f);
                        if (!pieces.Any(p => Vector2.Distance(p, behind) < 1.6f)) continue;

                        faces.Add(new Face
                        {
                            col = c, row = f, point = point, normal = -towardWall,
                            tangent = new Vector3(-towardWall.z, 0f, towardWall.x), inShelter = cell == 'r',
                        });
                    }
                }
            return faces;
        }

        static bool NearTeleporter(Vector3 point, Teleporter[] teleports, float distance) =>
            teleports.Any(t => Vector2.Distance(new Vector2(t.transform.position.x, t.transform.position.z), new Vector2(point.x, point.z)) < distance);

        static int PlaceGrass(Transform group, List<Face> faces, GameObject[] prefabs, Teleporter[] teleports, System.Random random)
        {
            int total = 0;
            foreach (var face in faces)
            {
                if (random.NextDouble() > GrassProbability) continue;

                int bushes = 1 + random.Next(3);
                for (int i = 0; i < bushes; i++)
                {
                    var point = face.point
                        + face.normal * Range(random, 0.12f, 0.45f)
                        + face.tangent * Range(random, -0.95f, 0.95f);
                    if (NearTeleporter(point, teleports, 1.6f)) continue;

                    bool mushroom = random.NextDouble() < (face.inShelter ? 0.45 : 0.1);
                    var prefab = mushroom ? prefabs[prefabs.Length - 1] : prefabs[random.Next(prefabs.Length - 1)];

                    var bush = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    bush.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f));
                    float size = mushroom ? Range(random, 0.35f, 0.6f) : Range(random, 0.45f, 0.85f);
                    bush.transform.localScale = new Vector3(size * 1.1f, size, size * 1.1f);
                    total++;
                }
            }
            return total;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static Mesh BushMesh()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(BushMeshPath);
            if (existing != null) return existing;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var rotation = Quaternion.Euler(0f, i * 60f, 0f);
                int b = vertices.Count;
                vertices.Add(rotation * new Vector3(-0.5f, 0f, 0f));
                vertices.Add(rotation * new Vector3(0.5f, 0f, 0f));
                vertices.Add(rotation * new Vector3(0.5f, 1f, 0f));
                vertices.Add(rotation * new Vector3(-0.5f, 1f, 0f));
                uvs.AddRange(new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
                triangles.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }

            var mesh = new Mesh { name = "MataPasto" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(Enumerable.Repeat(Vector3.up, vertices.Count).ToList());
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, BushMeshPath);
            return mesh;
        }

        static GameObject[] CreateGrassPrefabs()
        {
            Directory.CreateDirectory(Prefabs);
            var mesh = BushMesh();
            var green = new Color(0.42f, 0.62f, 0.28f);
            return new[]
            {
                FlatPrefab("Pasto_1", mesh, PlantMaterial("M_Pasto_1", GrassTex + "Grass_1.png", green, false)),
                FlatPrefab("Pasto_3", mesh, PlantMaterial("M_Pasto_3", GrassTex + "Grass_3.png", green * 0.9f, false)),
                FlatPrefab("Pasto_4", mesh, PlantMaterial("M_Pasto_4", GrassTex + "Grass_4.png", new Color(0.5f, 0.66f, 0.3f), false)),
                FlatPrefab("Hongos", mesh, PlantMaterial("M_Hongos", GrassTex + "Grass_5.png", Color.white, true)),
            };
        }

        static Material PlantMaterial(string displayName, string texturePath, Color color, bool shines)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if (ti.textureType != TextureImporterType.Default || !ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp)
            {
                ti.textureType = TextureImporterType.Default;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            var material = MaterialUrp(displayName);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.4f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.AlphaTest;
            if (shines)
            {
                material.SetTexture("_EmissionMap", texture);
                material.SetColor("_EmissionColor", Color.white * 0.8f);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material MaterialUrp(string displayName)
        {
            string path = Mat + displayName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject FlatPrefab(string displayName, Mesh mesh, Material material)
        {
            var item = new GameObject(displayName);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var render = item.AddComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            var prefab = PrefabUtility.SaveAsPrefabAsset(item, Prefabs + displayName + ".prefab");
            Object.DestroyImmediate(item);
            return prefab;
        }

        sealed class PrefabsIvy
        {
            public GameObject[] wall;
            public float[] wallWidth;
            public GameObject[] corner;
            public GameObject[] ground;
        }

        static PrefabsIvy CreatePrefabsIvy()
        {
            var sheets = MaterialUrp("M_Ivy");
            sheets.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Magolimpiadas/Laberinto/Texturas/Ivy/Ivy_Mat_512_BaseColor.png"));
            var normal = "Assets/_Magolimpiadas/Laberinto/Texturas/Ivy/Ivy_Mat_512_Normal.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(normal);
            if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            sheets.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            sheets.EnableKeyword("_NORMALMAP");
            sheets.SetColor("_BaseColor", new Color(0.85f, 0.95f, 0.8f));
            sheets.SetFloat("_Smoothness", 0.25f);
            sheets.SetFloat("_Cull", (float)CullMode.Off);
            EditorUtility.SetDirty(sheets);

            var stem = MaterialUrp("M_IvyTallo");
            stem.SetColor("_BaseColor", new Color(0.28f, 0.2f, 0.11f));
            stem.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(stem);

            var materials = new[] { sheets, stem };
            var meshes = AssetDatabase.LoadAllAssetsAtPath(IvyPath).OfType<Mesh>().ToDictionary(m => m.name);
            GameObject Piece(string mesh, string displayName)
            {
                var root = new GameObject(displayName);
                var child = new GameObject("Malla");
                child.transform.SetParent(root.transform, false);
                child.transform.localScale = Vector3.one * 100f;
                child.AddComponent<MeshFilter>().sharedMesh = meshes[mesh];
                var render = child.AddComponent<MeshRenderer>();
                render.sharedMaterials = materials;
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + displayName + ".prefab");
                Object.DestroyImmediate(root);
                return prefab;
            }

            return new PrefabsIvy
            {
                wall = new[] { Piece("Ivy_wall_2_LOD1", "Ivy_Muro_1"), Piece("Ivy_wall_3_LOD1", "Ivy_Muro_2"), Piece("Ivy_wall_1_LOD1", "Ivy_Muro_Ancho") },
                wallWidth = new[] { 3.09f, 2.48f, 4.85f },
                corner = new[] { Piece("Ivy_corner_1_LOD1", "Ivy_Esquina_1"), Piece("Ivy_corner_2_LOD1", "Ivy_Esquina_2") },
                ground = new[] { Piece("Ivy_ground_direct_1_LOD1", "Ivy_Suelo") },
            };
        }

        static int PlaceIvy(Transform group, List<Face> faces, PrefabsIvy prefabs, Teleporter[] teleports, System.Random random)
        {
            var used = new List<Vector3>();
            bool Free(Vector3 p) => used.All(u => Vector3.Distance(u, p) > DistanceBetweenIvys) && !NearTeleporter(p, teleports, 2f);
            bool HasFace(int c, int f, Vector3 normal) => faces.Any(x => x.col == c && x.row == f && x.normal == normal);
            int total = 0;

            foreach (var face in faces.OrderBy(_ => random.Next()))
            {
                if (total >= MaxWallIvys) break;
                if (!Free(face.point)) continue;

                var side = new Vector2Int(Mathf.RoundToInt(face.tangent.x), -Mathf.RoundToInt(face.tangent.z));
                bool width = HasFace(face.col + side.x, face.row + side.y, face.normal) && HasFace(face.col - side.x, face.row - side.y, face.normal);
                int type = width && random.NextDouble() < 0.4 ? 2 : random.Next(2);
                float scale = Mathf.Min(0.8f, (width ? 3f : 1.0f) * LabyrinthBuilder.Cell / prefabs.wallWidth[type]);

                var ivy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.wall[type], group);
                ivy.transform.SetPositionAndRotation(face.point - face.normal * 0.3f, Quaternion.LookRotation(face.normal));
                ivy.transform.localScale = new Vector3(scale, Mathf.Min(0.85f, scale * 1.1f), 1f);
                used.Add(face.point);
                total++;
            }

            int corners = 0;
            foreach (var face in faces.OrderBy(_ => random.Next()))
            {
                if (corners >= MaxCornerIvys) break;
                var other = faces.FirstOrDefault(x => x.col == face.col && x.row == face.row && Vector3.Dot(x.normal, face.normal) == 0f);
                if (other.normal == Vector3.zero) continue;

                var vertex = face.point + other.point - LabyrinthBuilder.CellCenter(face.col, face.row);
                if (!Free(vertex)) continue;

                var opens = face.normal + other.normal;
                float rotation = opens.x > 0 ? (opens.z > 0 ? 0f : 90f) : (opens.z < 0 ? 180f : 270f);
                var ivy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.corner[random.Next(prefabs.corner.Length)], group);
                ivy.transform.SetPositionAndRotation(vertex - opens * 0.3f, Quaternion.Euler(0f, rotation, 0f));
                ivy.transform.localScale = new Vector3(0.8f, 0.82f, 0.8f);
                used.Add(vertex);
                corners++;
            }

            int floors = 0;
            foreach (var face in faces.Where(c => !c.inShelter).OrderBy(_ => random.Next()))
            {
                if (floors >= GroundIvys) break;
                var point = face.point + face.normal * 0.6f;
                if (!Free(point)) continue;

                var ivy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.ground[0], group);
                ivy.transform.SetPositionAndRotation(point + Vector3.up * 0.01f, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f));
                ivy.transform.localScale = Vector3.one * 0.28f;
                used.Add(point);
                floors++;
            }

            return total + corners + floors;
        }

        static int PlaceCurtains(Transform group)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurtainPath);
            var zones = Object.FindObjectsByType<SafeZone>(FindObjectsSortMode.None).Select(z => z.GetComponent<Collider>().bounds).ToArray();
            bool InZone(int c, int f) => zones.Any(b => b.Contains(LabyrinthBuilder.CellCenter(c, f, 1f)));

            int total = 0;
            for (int f = LabyrinthBuilder.InitialRow; f < LabyrinthBuilder.InitialRow + LabyrinthBuilder.Rows; f++)
                for (int c = 0; c < LabyrinthBuilder.Columns; c++)
                {
                    if (!InZone(c, f)) continue;
                    foreach (var d in Directions)
                    {
                        int vc = c + d.x, vf = f + d.y;
                        if (InZone(vc, vf) || !Walkable(LabyrinthBuilder.In(vc, vf))) continue;

                        if (LabyrinthBuilder.In(vc + d.y, vf + d.x) != '#' || LabyrinthBuilder.In(vc - d.y, vf - d.x) != '#') continue;

                        var towardOutside = new Vector3(d.x, 0f, -d.y);
                        var edge = LabyrinthBuilder.CellCenter(c, f) + towardOutside * (LabyrinthBuilder.Cell * 0.5f);

                        var curtain = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                        curtain.name = "Cortina_" + c + "_" + f;
                        curtain.transform.rotation = Mathf.Abs(d.x) > 0 ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                        var render = curtain.GetComponentInChildren<Renderer>();
                        var size = render.bounds.size;
                        float width = Mathf.Abs(d.x) > 0 ? size.z : size.x;
                        float lateral = LabyrinthBuilder.Cell * 0.98f / width;
                        curtain.transform.localScale = new Vector3(lateral, CurtainHeight / size.y, lateral);
                        render.shadowCastingMode = ShadowCastingMode.Off;

                        var center = render.bounds.center;
                        curtain.transform.position += new Vector3(edge.x - center.x, CurtainHeight * 0.5f + 0.05f - center.y, edge.z - center.z);
                        total++;
                    }
                }
            return total;
        }

        static void PlacePlatforms(Teleporter[] teleports)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPath);
            foreach (var teleporter in teleports)
            {
                var cube = teleporter.gameObject;
                cube.GetComponent<MeshRenderer>().enabled = false;

                var box = cube.GetComponent<BoxCollider>();
                float side = PlatformDiameter * 0.85f;
                box.size = new Vector3(side / cube.transform.localScale.x, box.size.y, side / cube.transform.localScale.z);

                string displayName = cube.name + "_Plataforma";
                var previous = cube.transform.parent.Find(displayName);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);

                var platform = (GameObject)PrefabUtility.InstantiatePrefab(model, cube.transform.parent);
                platform.name = displayName;
                platform.transform.localScale = Vector3.one;
                platform.transform.position = Vector3.zero;
                var limits = Limits(platform);
                float scale = PlatformDiameter / Mathf.Max(limits.size.x, limits.size.z);
                platform.transform.localScale = Vector3.one * scale;

                float floor = cube.transform.position.y - cube.transform.lossyScale.y * 0.5f;
                limits = Limits(platform);
                platform.transform.position = new Vector3(cube.transform.position.x, floor + 0.04f - limits.max.y, cube.transform.position.z);
                foreach (var r in platform.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            }
        }

        [MenuItem("Magolimpiadas/Actualizar Efectos Mágicos")]
        public static void ApplyEffects()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ApplyEffectsInScene();

            EditorSceneManager.MarkSceneDirty(scene);
            if (Application.isBatchMode) EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void ApplyEffectsInScene()
        {
            ConfigureBloom();

            var curtains = GameObject.Find("Ambiente")?.transform.Find("Cortinas");
            if (curtains != null)
            {
                var curtainMaterial = CurtainMaterial();
                foreach (var r in curtains.GetComponentsInChildren<Renderer>()) r.sharedMaterial = curtainMaterial;

                var glow = curtains.GetComponent<MagicGlow>();
                bool isNew = glow == null || glow.colorProperty != "_BaseColor";
                glow = GetOrAddComponent<MagicGlow>(curtains.gameObject);
                glow.colorProperty = "_BaseColor";
                if (isNew)
                {
                    glow.color = new Color(1f, 0.55f, 0.12f);
                    glow.intensity = 2.5f;
                    glow.beat = 0.25f;
                    glow.beatSpeed = 0.6f;
                }
            }

            var platformMaterial = PlatformMaterial();
            var circle = AssetDatabase.LoadAssetAtPath<GameObject>(CirclePath);
            var effect = AssetDatabase.LoadAssetAtPath<GameObject>(TeleportEffectPath);
            int platforms = 0;

            foreach (var teleporter in Object.FindObjectsByType<Teleporter>(FindObjectsSortMode.None))
            {
                teleporter.teleportEffect = effect;
                EditorUtility.SetDirty(teleporter);

                var platform = teleporter.transform.parent.Find(teleporter.name + "_Plataforma");
                if (platform == null) continue;

                foreach (var r in platform.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = platformMaterial;
                var oldLight = platform.Find("BrilloRunas");
                if (oldLight != null) Object.DestroyImmediate(oldLight.gameObject);

                bool isNew = platform.GetComponent<MagicGlow>() == null;
                var glow = GetOrAddComponent<MagicGlow>(platform.gameObject);
                glow.colorProperty = "_EmissionColor";
                if (isNew)
                {
                    glow.color = new Color(0.25f, 0.55f, 1f);
                    glow.intensity = 4f;
                    glow.beat = 0.3f;
                    glow.beatSpeed = 0.5f;
                }

                string displayName = teleporter.name + "_Circulo";
                var previous = teleporter.transform.parent.Find(displayName);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(circle, teleporter.transform.parent);
                instance.name = displayName;
                var up = Limits(platform.gameObject).max.y;
                instance.transform.SetPositionAndRotation(new Vector3(platform.position.x, up + 0.02f, platform.position.z), Quaternion.identity);
                instance.transform.localScale = Vector3.one * CircleScale;
                foreach (var lightSource in instance.GetComponentsInChildren<Light>())
                {
                    lightSource.color = new Color(0.45f, 0.65f, 1f);
                    lightSource.intensity = 1.5f;
                    lightSource.range = 4f;
                }
                platforms++;
            }
            Debug.Log("[LAB] Efectos: cortinas=" + (curtains != null ? curtains.childCount : 0) + " plataformas=" + platforms);
        }

        static Material CurtainMaterial()
        {
            string path = Mat + "M_CortinaRunas.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RunesPath));
            material.SetColor("_BaseColor", new Color(1f, 0.55f, 0.12f) * 2.5f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material PlatformMaterial()
        {
            var textures = AssetDatabase.LoadAllAssetsAtPath(PlatformPath).OfType<Texture2D>().ToDictionary(t => t.name);
            var material = MaterialUrp("M_PlataformaRunica");
            material.SetTexture("_BaseMap", textures["image_0"]);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", textures["image_3"]);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", 0.35f);
            material.SetTexture("_EmissionMap", textures["image_2"]);
            material.SetColor("_EmissionColor", new Color(0.25f, 0.55f, 1f) * 4f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ConfigureBloom()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);

                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(1f);
                bloom.intensity.Override(1.1f);
                bloom.scatter.Override(0.7f);
                bloom.highQualityFiltering.Override(true);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.Neutral);
                foreach (var found in profile.components) AssetDatabase.AddObjectToAsset(found, profile);
                EditorUtility.SetDirty(profile);
            }

            var item = GameObject.Find("Volumen Global") ?? new GameObject("Volumen Global");
            var volume = GetOrAddComponent<Volume>(item);
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            var sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                var data = sceneCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(data);
            }
        }

        static T GetOrAddComponent<T>(GameObject item) where T : Component
        {
            var found = item.GetComponent<T>();
            return found != null ? found : item.AddComponent<T>();
        }

        static Bounds Limits(GameObject item)
        {
            var renders = item.GetComponentsInChildren<Renderer>();
            var b = renders[0].bounds;
            foreach (var r in renders) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform Group(string displayName, Transform parent)
        {
            var group = new GameObject(displayName).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
