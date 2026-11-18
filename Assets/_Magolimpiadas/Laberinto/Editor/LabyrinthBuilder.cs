using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Labyrinth.Editor
{
    public static class LabyrinthBuilder
    {
        static readonly string[] Flatten =
        {
            "            ###  ",
            "#############S#  ",
            "#~~~~~#.......#  ",
            "#~~~~~#.#####.#  ",
            "#~~~~~#.#rrr#.#  ",
            "#~~~~~...rrr#.#  ",
            "#######.#####.#  ",
            "#.#.........#.#  ",
            "#...###.#####.#  ",
            "#.#####.#.....#  ",
            "#.#rrr#.#...###  ",
            "#.#rrr..#...#rr  ",
            "#.#####.###.#rr  ",
            "#............rr  ",
            "#EE############  ",
            "#EE#             ",
            "####             ",
        };

        internal const int InitialRow = 0;

        internal const float Cell = 2.205f;

        const float StraightLength = 2.898f;
        const float StraightBackground = 0.752f;
        const float OuterArm = 2.15f;
        const float InnerArm = 0.52f;
        static readonly Vector3 OuterVertex = new Vector3(1.103f, 0f, 1.102f);
        static readonly Vector3 InnerVertex = new Vector3(0.473f, 0f, 0.473f);

        const float WallHeightScale = 1f;
        const float WallHeight = 3.014f * WallHeightScale;

        const float PieceBackground = 1.5f;
        const float LidWithdrawal = 0.35f;
        const float LidThickness = 0.25f;
        const float PitBottomHeight = -2f * WallHeight;
        const float FloorRepeatMeters = 4.41f;

        const string ScenePath = "Assets/_Magolimpiadas/Escenas/LaberintoMinotauro.unity";
        const string FloorMeshPath = "Assets/_Magolimpiadas/Laberinto/Modelos/PisoLaberinto.asset";
        const string LidMeshPath = "Assets/_Magolimpiadas/Laberinto/Modelos/TapaMuros.asset";

        static readonly Vector2 SwitchIslandCell = new Vector2(1.8f, 2.8f);
        static readonly Vector2 IslandCell2 = new Vector2(3.2f, 3.7f);
        static readonly Vector2 IslandCell3 = new Vector2(4.2f, 4.55f);
        static readonly Vector2 PlateIslandCell = new Vector2(5.35f, 5.4f);

        internal static int Rows => Flatten.Length;
        internal static int Columns => Flatten[0].Length;

        internal static char In(int col, int row)
        {
            int f = row - InitialRow;
            if (f < 0 || f >= Rows || col < 0 || col >= Columns) return ' ';
            return Flatten[f][col];
        }

        static bool IsWall(int col, int row) => In(col, row) == '#';

        static bool IsPitWall(int col, int row) => In(col, row) != '~';

        internal static Vector3 World(float col, float row, float y = 0f) =>
            new Vector3((col - Columns * 0.5f) * Cell, y, (InitialRow + Rows * 0.5f - row) * Cell);

        public static Vector3 CellCenter(int col, int row, float y = 0f) => World(col + 0.5f, row + 0.5f, y);

        static Vector3 CellPoint(Vector2 cell, float y = 0f) => World(cell.x, cell.y, y);

        [MenuItem("Magolimpiadas/Construir Laberinto")]
        static void BuildFromMenu()
        {
            bool confirmed = EditorUtility.DisplayDialog("Construir Laberinto",
                "Esto regenera toda la escena desde el plano y borra lo agregado después: " +
                "maga, minotauro, waypoints y cambios hechos a mano. ¿Continuar?",
                "Regenerar", "Cancelar");
            if (confirmed) Build();
        }

        public static void Build()
        {
            ValidateFlat();
            LabyrinthMaterials.Prepare();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("LaberintoMinotauro").transform;

            CreateFloor(root);
            CreateWalls(root);
            CreatePit(root);
            CreateZones(root);
            CreateMechanisms(root);
            CreateAmbience(root);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[LAB] Escena guardada en " + ScenePath);
        }

        static void ValidateFlat()
        {
            foreach (var row in Flatten)
                if (row.Length != Columns)
                    throw new InvalidOperationException("Fila con largo incorrecto: '" + row + "'");

            for (int f = InitialRow; f < InitialRow + Rows - 1; f++)
                for (int c = 0; c < Columns - 1; c++)
                    if (IsWall(c, f) == IsWall(c + 1, f + 1) && IsWall(c + 1, f) == IsWall(c, f + 1)
                        && IsWall(c, f) != IsWall(c + 1, f))
                        throw new InvalidOperationException("Muros en diagonal sin esquina posible en " + c + "," + f);

            int colStart = -1, rowStart = -1, walkable = 0;
            for (int f = InitialRow; f < InitialRow + Rows; f++)
                for (int c = 0; c < Columns; c++)
                {
                    if (!IsWalkable(In(c, f))) continue;
                    walkable++;
                    if (In(c, f) == 'E' && colStart < 0) { colStart = c; rowStart = f; }
                }

            var visited = new HashSet<(int, int)> { (colStart, rowStart) };
            var pending = new Queue<(int c, int f)>();
            pending.Enqueue((colStart, rowStart));
            while (pending.Count > 0)
            {
                var (c, f) = pending.Dequeue();
                foreach (var (dc, df) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var next = (c + dc, f + df);
                    if (IsWalkable(In(c + dc, f + df)) && visited.Add(next))
                        pending.Enqueue(next);
                }
            }

            if (visited.Count != walkable)
                throw new InvalidOperationException(
                    "Hay zonas aisladas: " + visited.Count + " de " + walkable + " celdas alcanzables.");
            Debug.Log("[LAB] Plano válido: " + walkable + " celdas transitables, todas conectadas con la entrada.");
        }

        static bool IsWalkable(char c) => c == '.' || c == 'r' || c == 'E' || c == 'S';

        static void CreateFloor(Transform root)
        {
            var mesh = new MeshBuilder();
            for (int f = InitialRow; f < InitialRow + Rows; f++)
                for (int c = 0; c < Columns; c++)
                {
                    char cell = In(c, f);
                    if (cell == ' ' || cell == '~') continue;
                    mesh.Face(World(c, f), World(c + 1, f), World(c + 1, f + 1), World(c, f + 1), PlantUv);
                }

            var floor = mesh.Create("Piso", FloorMeshPath, root, LabyrinthMaterials.Floor);
            floor.AddComponent<MeshCollider>().sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
        }

        static Vector2 PlantUv(Vector3 v) => new Vector2(v.x, v.z);
        static Vector2 SideUvX(Vector3 v) => new Vector2(v.z, v.y);
        static Vector2 SideUvZ(Vector3 v) => new Vector2(v.x, v.y);

        static void CreateWalls(Transform root)
        {
            var walls = Group("Muros", root);
            var count = CoverOutline(walls, IsWall, 0f);
            Debug.Log("[LAB] Muros: " + count.straight + " rectas 09a, " + count.outer + " esquinas 09b, " + count.inner + " esquinas 09c");

            CreateWallsFill(walls);

            var collision = new GameObject("ColisionMuros");
            collision.transform.SetParent(walls, false);
            for (int f = InitialRow; f < InitialRow + Rows; f++)
                for (int c = 0; c < Columns; c++)
                {
                    if (!IsWall(c, f)) continue;
                    var box = collision.AddComponent<BoxCollider>();
                    box.center = CellCenter(c, f, WallHeight * 0.5f);
                    box.size = new Vector3(Cell, WallHeight, Cell);
                }
        }

        struct EdgeInfo
        {
            public int colA, rowA, colB, rowB;
            public Vector3 normal;
            public bool horizontal;
        }

        static (int straight, int outer, int inner) CoverOutline(
            Transform parent, Func<int, int, bool> solid, float yBase)
        {
            var straight = Group("Rectas_09a", parent);
            var outer = Group("EsquinasExteriores_09b", parent);
            var inner = Group("EsquinasInteriores_09c", parent);
            int straightTotal = 0, outerTotal = 0, innerTotal = 0;

            int Full(int c, int f) =>
                (solid(c - 1, f - 1) ? 1 : 0) + (solid(c, f - 1) ? 1 : 0) + (solid(c - 1, f) ? 1 : 0) + (solid(c, f) ? 1 : 0);

            var arms = new Dictionary<(int, int), Vector2>();

            foreach (var edge in ListEdges(solid))
            {
                var a = World(edge.colA, edge.rowA, yBase);
                var b = World(edge.colB, edge.rowB, yBase);
                float length = Vector3.Distance(a, b);
                bool outerA = Full(edge.colA, edge.rowA) == 1, outerB = Full(edge.colB, edge.rowB) == 1;

                float marginA = outerA ? OuterArm : InnerArm;
                float marginB = outerB ? OuterArm : InnerArm;
                if (marginA + marginB > length)
                {
                    if (outerA && outerB) marginA = marginB = length * 0.5f;
                    else if (outerA) marginA = length - marginB;
                    else if (outerB) marginB = length - marginA;
                }

                if (outerA) ReduceArm(arms, (edge.colA, edge.rowA), edge.horizontal, marginA / OuterArm);
                if (outerB) ReduceArm(arms, (edge.colB, edge.rowB), edge.horizontal, marginB / OuterArm);

                straightTotal += Section(straight, a, b, edge.normal, marginA, marginB, !outerA, !outerB);
            }

            for (int f = InitialRow; f <= InitialRow + Rows; f++)
                for (int c = 0; c <= Columns; c++)
                {
                    bool nw = solid(c - 1, f - 1), ne = solid(c, f - 1), sw = solid(c - 1, f), se = solid(c, f);
                    int full = Full(c, f);
                    var vertex = World(c, f, yBase);

                    if (full == 3)
                    {
                        int dx = !nw || !sw ? -1 : 1, dz = !nw || !ne ? 1 : -1;
                        var piece = Corner(LabyrinthMaterials.WallPath09c, inner, Rotation(dx, dz));
                        piece.transform.position = vertex - Quaternion.Euler(0f, Rotation(dx, dz), 0f) * InnerVertex;
                        piece.transform.localScale = new Vector3(1f, 1f, WallHeightScale);
                        innerTotal++;
                    }
                    else if (full == 1)
                    {
                        int dx = nw || sw ? 1 : -1, dz = sw || se ? 1 : -1;
                        float rotation = Rotation(dx, dz);
                        arms.TryGetValue((c, f), out var scales);
                        if (scales == Vector2.zero) scales = Vector2.one;

                        bool crossed = Mathf.Approximately(rotation % 180f, 90f);
                        float scaleX = crossed ? scales.y : scales.x, scaleZ = crossed ? scales.x : scales.y;

                        var piece = Corner(LabyrinthMaterials.WallPath09b, outer, rotation);
                        var localVertex = new Vector3(OuterVertex.x * scaleX, 0f, OuterVertex.z * scaleZ);
                        piece.transform.position = vertex - Quaternion.Euler(0f, rotation, 0f) * localVertex;
                        piece.transform.localScale = new Vector3(scaleX, scaleZ, WallHeightScale);
                        outerTotal++;
                    }
                }

            return (straightTotal, outerTotal, innerTotal);
        }

        static void ReduceArm(Dictionary<(int, int), Vector2> arms, (int, int) vertex, bool horizontal, float scale)
        {
            if (!arms.TryGetValue(vertex, out var actual)) actual = Vector2.one;
            if (horizontal) actual.x = Mathf.Min(actual.x, scale);
            else actual.y = Mathf.Min(actual.y, scale);
            arms[vertex] = actual;
        }

        static List<EdgeInfo> ListEdges(Func<int, int, bool> solid)
        {
            var edges = new List<EdgeInfo>();
            int columnEnd = Columns, rowEnd = InitialRow + Rows;

            for (int f = InitialRow; f <= rowEnd; f++)
            {
                int start = -1, previousNormal = 0;
                for (int c = 0; c <= columnEnd; c++)
                {
                    int normal = c == columnEnd ? 0 : Normal(solid(c, f), solid(c, f - 1));
                    if (normal == previousNormal) continue;
                    if (previousNormal != 0)
                        edges.Add(new EdgeInfo
                        {
                            colA = start, rowA = f, colB = c, rowB = f,
                            normal = new Vector3(0f, 0f, previousNormal), horizontal = true,
                        });
                    start = c;
                    previousNormal = normal;
                }
            }

            for (int c = 0; c <= columnEnd; c++)
            {
                int start = -1, previousNormal = 0;
                for (int f = InitialRow; f <= rowEnd; f++)
                {
                    int normal = f == rowEnd ? 0 : Normal(solid(c - 1, f), solid(c, f));
                    if (normal == previousNormal) continue;
                    if (previousNormal != 0)
                        edges.Add(new EdgeInfo
                        {
                            colA = c, rowA = start, colB = c, rowB = f,
                            normal = new Vector3(previousNormal, 0f, 0f), horizontal = false,
                        });
                    start = f;
                    previousNormal = normal;
                }
            }

            return edges;
        }

        static int Normal(bool negativeSolid, bool positiveSolid)
        {
            if (negativeSolid == positiveSolid) return 0;
            return negativeSolid ? 1 : -1;
        }

        static float Rotation(int dx, int dz)
        {
            if (dx > 0) return dz > 0 ? 0f : 90f;
            return dz < 0 ? 180f : 270f;
        }

        static GameObject Corner(string modelPath, Transform parent, float rotation) =>
            Piece(modelPath, parent, Quaternion.Euler(0f, rotation, 0f));

        static int Section(Transform parent, Vector3 a, Vector3 b, Vector3 normal, float marginA, float marginB, bool innerA, bool innerB)
        {
            var direction = (b - a).normalized;
            float distance = Vector3.Distance(a, b);
            float usefulLength = distance - marginA - marginB;
            if (usefulLength < 0.3f) return 0;

            if (usefulLength / StraightLength < 0.7f)
            {
                if (innerA) marginA = 0f;
                if (innerB) marginB = 0f;
                usefulLength = distance - marginA - marginB;
            }

            float ideal = usefulLength / StraightLength;
            int less = Mathf.Max(1, Mathf.FloorToInt(ideal)), more = less + 1;
            int amount = Mathf.Abs(Mathf.Log(ideal / less)) <= Mathf.Abs(Mathf.Log(ideal / more)) ? less : more;
            float length = usefulLength / amount;

            var orientation = Quaternion.LookRotation(normal, Vector3.up);
            var start = a + direction * marginA;
            for (int i = 0; i < amount; i++)
            {
                var piece = Piece(LabyrinthMaterials.WallPath09a, parent, orientation);
                piece.transform.position = start + direction * ((i + 0.5f) * length) - normal * StraightBackground;
                piece.transform.localScale = new Vector3(length / StraightLength, 1f, WallHeightScale);
            }
            return amount;
        }

        static GameObject Piece(string modelPath, Transform parent, Quaternion orientation)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var piece = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            piece.transform.rotation = orientation * model.transform.rotation;
            return piece;
        }

        static void CreateWallsFill(Transform walls)
        {
            var lid = new MeshBuilder();
            float up = WallHeight + 0.02f, down = up - LidThickness;

            for (int f = InitialRow; f < InitialRow + Rows; f++)
                for (int c = 0; c < Columns; c++)
                    if (IsWall(c, f))
                        Box2D(lid, c, f, LidWithdrawal, down, up);

            lid.Create("TapaMuros", LidMeshPath, walls, LabyrinthMaterials.WallCap);
        }

        static void Box2D(MeshBuilder mesh, int c, int f, float withdrawal, float down, float up)
        {
            bool north = IsWall(c, f - 1), south = IsWall(c, f + 1), west = IsWall(c - 1, f), east = IsWall(c + 1, f);
            float step = withdrawal / Cell;
            float col0 = c + (west ? 0f : step), col1 = c + 1 - (east ? 0f : step);
            float row0 = f + (north ? 0f : step), row1 = f + 1 - (south ? 0f : step);

            Vector3 nw = World(col0, row0), ne = World(col1, row0), se = World(col1, row1), sw = World(col0, row1);
            Vector3 Height(Vector3 p) => new Vector3(p.x, up, p.z);
            Vector3 Low(Vector3 p) => new Vector3(p.x, down, p.z);

            mesh.Face(Height(nw), Height(ne), Height(se), Height(sw), PlantUv);
            mesh.Face(Low(sw), Low(se), Low(ne), Low(nw), PlantUv);
            if (!north) mesh.Face(Low(ne), Height(ne), Height(nw), Low(nw), SideUvZ);
            if (!south) mesh.Face(Low(sw), Height(sw), Height(se), Low(se), SideUvZ);
            if (!west) mesh.Face(Low(nw), Height(nw), Height(sw), Low(sw), SideUvX);
            if (!east) mesh.Face(Low(se), Height(se), Height(ne), Low(ne), SideUvX);
        }

        static void CreatePit(Transform root)
        {
            var pit = Group("FosoPuas", root);

            int colMin = int.MaxValue, colMax = int.MinValue, minRow = int.MaxValue, maxRow = int.MinValue;
            for (int f = InitialRow; f < InitialRow + Rows; f++)
                for (int c = 0; c < Columns; c++)
                    if (In(c, f) == '~')
                    {
                        colMin = Mathf.Min(colMin, c); colMax = Mathf.Max(colMax, c);
                        minRow = Mathf.Min(minRow, f); maxRow = Mathf.Max(maxRow, f);
                    }

            int cellsWidth = colMax - colMin + 1, cellsHeight = maxRow - minRow + 1;
            float pitWidth = cellsWidth * Cell, pitLength = cellsHeight * Cell;
            var center = World(colMin + cellsWidth * 0.5f, minRow + cellsHeight * 0.5f);

            CoverOutline(Group("MuroInterior_1", pit), IsPitWall, -WallHeight);
            CoverOutline(Group("MuroInterior_2", pit), IsPitWall, -2f * WallHeight);

            Box("Fondo", pit, center + Vector3.up * (PitBottomHeight - 0.25f),
                new Vector3(pitWidth + 2f * Cell, 0.5f, pitLength + 2f * Cell), LabyrinthMaterials.PitBackground);

            float heightValue = -PitBottomHeight - 0.05f, withdrawn = PieceBackground * 2f;
            var centerHeight = Vector3.up * (PitBottomHeight + heightValue * 0.5f);
            var material = LabyrinthMaterials.PitBackground;
            Box("RespaldoNorte", pit, center + centerHeight + Vector3.forward * (pitLength + withdrawn) * 0.5f, new Vector3(pitWidth + withdrawn, heightValue, 0.2f), material);
            Box("RespaldoSur", pit, center + centerHeight + Vector3.back * (pitLength + withdrawn) * 0.5f, new Vector3(pitWidth + withdrawn, heightValue, 0.2f), material);
            Box("RespaldoOeste", pit, center + centerHeight + Vector3.left * (pitWidth + withdrawn) * 0.5f, new Vector3(0.2f, heightValue, pitLength + withdrawn), material);
            Box("RespaldoEste", pit, center + centerHeight + Vector3.right * (pitWidth + withdrawn) * 0.5f, new Vector3(0.2f, heightValue, pitLength + withdrawn), material);

            CreateSpikes(pit, colMin, minRow, cellsWidth, cellsHeight);
            CreateIslands(pit);
        }

        static void CreateSpikes(Transform pit, int colMin, int minRow, int cellsWidth, int cellsHeight)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(LabyrinthMaterials.SpikesPath);
            var group = Group("Puas", pit);
            const float ModelSide = 0.6f;
            const float ModelHeight = 0.185f;
            const float SpikesHeight = 2f;

            int tilesX = Mathf.CeilToInt(cellsWidth * 0.5f);
            int tilesZ = Mathf.CeilToInt(cellsHeight * 0.5f);
            float sideX = cellsWidth * Cell / tilesX;
            float sideZ = cellsHeight * Cell / tilesZ;

            for (int x = 0; x < tilesX; x++)
                for (int z = 0; z < tilesZ; z++)
                {
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(model, group);
                    tile.name = "Puas_" + x + "_" + z;
                    float col = colMin + (x + 0.5f) * cellsWidth / tilesX;
                    float row = minRow + (z + 0.5f) * cellsHeight / tilesZ;
                    tile.transform.position = World(col, row, PitBottomHeight);
                    var scale = tile.transform.localScale;
                    tile.transform.localScale = new Vector3(
                        scale.x * sideX / ModelSide, scale.y * sideZ / ModelSide, scale.z * SpikesHeight / ModelHeight);
                }
        }

        static void CreateIslands(Transform pit)
        {
            var group = Group("Islas", pit);
            Island(group, "Isla_Interruptor", CellPoint(SwitchIslandCell), 0.13f, 20f);
            Island(group, "Isla_2", CellPoint(IslandCell2), 0.085f, 95f);
            Island(group, "Isla_3", CellPoint(IslandCell3), 0.085f, 200f);
            Island(group, "Isla_Placa", CellPoint(PlateIslandCell), 0.095f, 300f);
        }

        static void Island(Transform parent, string displayName, Vector3 position, float scale, float rotation)
        {
            const float ModelSurfaceHeight = 1.3f;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(LabyrinthMaterials.IslandPath);
            var island = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            island.name = displayName;
            island.transform.position = position + Vector3.down * (ModelSurfaceHeight * scale);
            island.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            island.transform.localScale = Vector3.one * scale;

            var body = island.transform.Find("Island");
            body.gameObject.AddComponent<MeshCollider>().sharedMesh = body.GetComponent<MeshFilter>().sharedMesh;
        }

        static void CreateZones(Transform root)
        {
            var zones = Group("Zonas", root);
            Zone(zones, "Refugio_1", 9, 4, 3, 2, LabyrinthMaterials.Shelter);
            Zone(zones, "Refugio_2", 3, 10, 3, 2, LabyrinthMaterials.Shelter);
            Zone(zones, "Refugio_3", 13, 11, 2, 3, LabyrinthMaterials.Shelter);
            Zone(zones, "Entrada", 1, 14, 2, 2, LabyrinthMaterials.Entrance);
            Zone(zones, "Salida", 13, 1, 1, 1, LabyrinthMaterials.Exit);
        }

        static void Zone(Transform parent, string displayName, int col, int row, int cellsWidth, int cellsHeight, Material material)
        {
            var center = World(col + cellsWidth * 0.5f, row + cellsHeight * 0.5f, 0.02f);
            var zone = GameObject.CreatePrimitive(PrimitiveType.Quad);
            zone.name = displayName;
            Object.DestroyImmediate(zone.GetComponent<Collider>());
            zone.transform.SetParent(parent, false);
            zone.transform.SetPositionAndRotation(center, Quaternion.Euler(90f, 0f, 0f));
            zone.transform.localScale = new Vector3(cellsWidth * Cell, cellsHeight * Cell, 1f);
            zone.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void CreateMechanisms(Transform root)
        {
            var group = Group("Mecanismos", root);
            var material = LabyrinthMaterials.Mechanism;

            var floorSwitch = Group("Interruptor", group);
            floorSwitch.position = CellPoint(SwitchIslandCell);
            Box("Base", floorSwitch, floorSwitch.position + Vector3.up * 0.2f, new Vector3(0.9f, 0.4f, 0.9f), material);
            var lever = Box("Palanca", floorSwitch, floorSwitch.position + new Vector3(0.15f, 0.8f, 0f), new Vector3(0.12f, 0.9f, 0.12f), material);
            lever.transform.rotation = Quaternion.Euler(0f, 0f, -25f);

            var plateSize = new Vector3(1.3f, 0.12f, 1.3f);
            Box("Placa_Isla", group, CellPoint(PlateIslandCell, 0.1f), plateSize, material);
            Box("Placa_Este", group, CellCenter(14, 7, 0.06f), plateSize, material);
            Box("Placa_Sur", group, CellCenter(9, 13, 0.06f), plateSize, material);
        }

        static void CreateAmbience(Transform root)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f, 0.53f, 0.62f);

            var sun = new GameObject("Luz Direccional").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.83f);
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(62f, -28f, 0f);

            var lights = Group("Luces", root);
            var torch = new Color(1f, 0.62f, 0.28f);
            var turquoise = new Color(0.15f, 1f, 0.8f);
            Point(lights, "Antorcha_Norte", CellCenter(10, 2, 2.2f), torch, 14f, 13f);
            Point(lights, "Antorcha_Oeste", CellCenter(1, 9, 2.2f), torch, 14f, 13f);
            Point(lights, "Antorcha_Este", CellCenter(15, 9, 2.2f), torch, 14f, 13f);
            Point(lights, "Antorcha_Sur", CellCenter(8, 13, 2.2f), torch, 14f, 13f);
            Point(lights, "Antorcha_Centro", CellCenter(7, 7, 2.2f), torch, 14f, 13f);
            Point(lights, "Brillo_Refugio_1", CellCenter(10, 5, 1.6f), turquoise, 6f, 7f);
            Point(lights, "Brillo_Refugio_2", CellCenter(4, 11, 1.6f), turquoise, 6f, 7f);
            Point(lights, "Brillo_Refugio_3", CellCenter(13, 12, 1.6f), turquoise, 6f, 7f);
            Point(lights, "Brillo_Foso", CellCenter(3, 4, -2.5f), turquoise, 16f, 14f);
            Point(lights, "Brillo_Entrada", CellCenter(1, 14, 1.6f), new Color(0.3f, 1f, 0.2f), 8f, 8f);
            Point(lights, "Brillo_Salida", CellCenter(13, 2, 1.6f), new Color(0.7f, 0.3f, 1f), 8f, 8f);

            var sceneCamera = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = sceneCamera.AddComponent<Camera>();
            sceneCamera.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.13f);
            cam.fieldOfView = 35f;
            cam.farClipPlane = 200f;
            var target = World(8.5f, 8.5f);
            sceneCamera.transform.position = target + new Vector3(0f, 58f, -24f);
            sceneCamera.transform.LookAt(target);
        }

        static void Point(Transform parent, string displayName, Vector3 position, Color color, float intensity, float reach)
        {
            var lightSource = new GameObject(displayName).AddComponent<Light>();
            lightSource.transform.SetParent(parent, false);
            lightSource.transform.position = position;
            lightSource.type = LightType.Point;
            lightSource.color = color;
            lightSource.intensity = intensity;
            lightSource.range = reach;
        }

        static Transform Group(string displayName, Transform parent)
        {
            var group = new GameObject(displayName).transform;
            group.SetParent(parent, false);
            return group;
        }

        static GameObject Box(string displayName, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = displayName;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        sealed class MeshBuilder
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int> triangles = new List<int>();

            public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Func<Vector3, Vector2> uv)
            {
                int i = vertices.Count;
                foreach (var v in new[] { a, b, c, d })
                {
                    vertices.Add(v);
                    uvs.Add(uv(v) / FloorRepeatMeters);
                }
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            public GameObject Create(string displayName, string path, Transform parent, Material material)
            {
                var mesh = new Mesh { name = displayName, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);

                var item = new GameObject(displayName);
                item.transform.SetParent(parent, false);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterial = material;
                return item;
            }
        }
    }
}
