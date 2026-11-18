using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RailingsBuilder
{
    const string Name = "Barandas de los caminos";
    const string RailingPath = "Assets/_Magolimpiadas/Isla/Modelos/Kit_Props/Baranda.fbx";
    const string PostPath = "Assets/_Magolimpiadas/Isla/Modelos/Kit_Props/Baranda_Poste.fbx";

    const float Cell = 0.5f;
    const float Step = 1.2f;
    const float JumpGap = 6f;
    const float Sink = 0.3f;
    const float RailingHeight = 0.75f;
    const float WallHeight = 2.6f;
    const float Modulo = 2.70f;

    [MenuItem("Magolimpiadas/Isla Mágica/Poner barandas en los caminos")]
    static void Menu()
    {
        var old = Root(Name);
        if (old != null && !EditorUtility.DisplayDialog("Barandas",
            "Ya existe \"" + Name + "\". ¿Lo rehago? Se borra SOLO ese objeto (las barandas que puso este menú).", "Rehacer", "Cancelar"))
            return;
        var res = Build(out string report);
        Debug.Log("[Barandas] " + report + " Guarda la escena (Ctrl+S).");
        if (res != null) Selection.activeGameObject = res;
    }

    static GameObject Root(string n) => EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == n);

    class Cells
    {
        public float x0, z0;
        public int nx, nz;
        public float[] heightValue;
        public List<float>[] floors;
    }

    public static GameObject Build(out string report)
    {
        report = "";
        var railing = AssetDatabase.LoadAssetAtPath<GameObject>(RailingPath);
        var post = AssetDatabase.LoadAssetAtPath<GameObject>(PostPath);
        if (railing == null || post == null) { report = "No encontré Baranda.fbx o Baranda_Poste.fbx en Kit_Props."; return null; }

        var pieces = new List<Transform>();
        var example = GameObject.Find("IslaMagica/Caminos_Ejemplo");
        if (example != null) foreach (Transform t in example.transform) pieces.Add(t);
        foreach (var g in EditorSceneManager.GetActiveScene().GetRootGameObjects()) if (g.name.StartsWith("Camino_")) pieces.Add(g.transform);
        pieces = pieces.Where(t => t.gameObject.activeInHierarchy && !t.name.Contains("Arena") && !t.name.Contains("Salto")).ToList();
        var target = new HashSet<Collider>(pieces.SelectMany(t => t.GetComponentsInChildren<Collider>()).Where(c => !c.isTrigger));
        if (target.Count == 0) { report = "No encontré caminos con collider."; return null; }

        var old = Root(Name);
        if (old != null) Undo.DestroyObjectImmediate(old);
        Physics.SyncTransforms();

        Bounds zone = target.First().bounds;
        foreach (var c in target) zone.Encapsulate(c.bounds);
        zone.Expand(new Vector3(JumpGap * 2f + 2f, 0f, JumpGap * 2f + 2f));
        var g2 = new Cells { x0 = zone.min.x, z0 = zone.min.z };
        g2.nx = Mathf.CeilToInt(zone.size.x / Cell);
        g2.nz = Mathf.CeilToInt(zone.size.z / Cell);
        g2.heightValue = new float[g2.nx * g2.nz];
        g2.floors = new List<float>[g2.nx * g2.nz];
        float up = zone.max.y + 20f, length = zone.size.y + 60f;
        for (int i = 0; i < g2.nx; i++)
            for (int j = 0; j < g2.nz; j++)
            {
                int k = j * g2.nx + i;
                g2.heightValue[k] = float.NaN;
                var list = new List<float>();
                var p = new Vector3(g2.x0 + (i + 0.5f) * Cell, up, g2.z0 + (j + 0.5f) * Cell);
                foreach (var h in Physics.RaycastAll(p, Vector3.down, length, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.GetComponentInParent<CharacterController>() != null) continue;
                    list.Add(h.point.y);
                    if (target.Contains(h.collider) && (float.IsNaN(g2.heightValue[k]) || h.point.y > g2.heightValue[k])) g2.heightValue[k] = h.point.y;
                }
                g2.floors[k] = list;
            }

        var sides = new List<(Vector2Int a, Vector2Int b)>();
        int[] di = { 1, -1, 0, 0 }, dj = { 0, 0, 1, -1 };
        for (int i = 0; i < g2.nx; i++)
            for (int j = 0; j < g2.nz; j++)
            {
                float y = g2.heightValue[j * g2.nx + i];
                if (float.IsNaN(y)) continue;
                for (int d = 0; d < 4; d++)
                {
                    if (HasGround(g2, i + di[d], j + dj[d], y, Step)) continue;
                    if (OtherPlatform(g2, i, j, di[d], dj[d], y)) continue;
                    Vector2Int a, b;
                    if (d == 0) { a = new Vector2Int(i + 1, j); b = new Vector2Int(i + 1, j + 1); }
                    else if (d == 1) { a = new Vector2Int(i, j); b = new Vector2Int(i, j + 1); }
                    else if (d == 2) { a = new Vector2Int(i, j + 1); b = new Vector2Int(i + 1, j + 1); }
                    else { a = new Vector2Int(i, j); b = new Vector2Int(i + 1, j); }
                    sides.Add((a, b));
                }
            }

        var lines = Chain(sides);
        var root = new GameObject(Name);
        Undo.RegisterCreatedObjectUndo(root, "Barandas");
        int railingCount = 0, postCount = 0;
        var pillars = new List<Vector3>();
        var starts = new List<(Vector3 p, Vector3 dir)>();
        foreach (var line in lines)
        {
            var world = line.Select(c => new Vector2(g2.x0 + c.x * Cell, g2.z0 + c.y * Cell)).ToList();
            bool closed = line.Count > 3 && line[0] == line[line.Count - 1];
            var simple = Simplify(world, 0.35f);
            int m = simple.Count - 1;
            if (m < 1) continue;

            var normals = new Vector2?[m];
            for (int k = 0; k < m; k++)
            {
                Vector2 a = simple[k], b = simple[k + 1];
                if ((b - a).sqrMagnitude < 1e-4f) continue;
                Vector2 dir = (b - a).normalized, n = new Vector2(-dir.y, dir.x), middle = (a + b) * 0.5f;
                if (float.IsNaN(WalkwayHeight(g2, middle + n * 0.6f))) n = -n;
                if (float.IsNaN(WalkwayHeight(g2, middle + n * 0.6f))) continue;
                normals[k] = n;
            }

            var v = new Vector2[simple.Count];
            for (int k = 0; k < simple.Count; k++)
            {
                Vector2? prevNormal = k > 0 ? normals[k - 1] : (closed ? normals[m - 1] : null);
                Vector2? nextNormal = k < m ? normals[k] : (closed ? normals[0] : null);
                if (prevNormal.HasValue && nextNormal.HasValue)
                {
                    Vector2 bis = prevNormal.Value + nextNormal.Value;
                    if (bis.sqrMagnitude < 1e-4f) bis = nextNormal.Value;
                    bis.Normalize();
                    float cos = Mathf.Max(0.35f, Vector2.Dot(bis, nextNormal.Value));
                    v[k] = simple[k] + bis * (Sink / cos);
                }
                else if (nextNormal.HasValue) v[k] = simple[k] + nextNormal.Value * Sink;
                else if (prevNormal.HasValue) v[k] = simple[k] + prevNormal.Value * Sink;
                else v[k] = simple[k];
            }

            for (int k = 0; k < m; k++)
            {
                if (!normals[k].HasValue) continue;
                Vector2 a = v[k], b = v[k + 1];
                Vector2 dir = (b - a).normalized;
                if (!closed && k == 0) a += dir * 0.15f;
                if (!closed && k == m - 1) b -= dir * 0.15f;
                float L = Vector2.Distance(a, b);
                float already = FenceHeight(g2, a), yb = FenceHeight(g2, b);
                if (float.IsNaN(already) || float.IsNaN(yb)) continue;
                var A = new Vector3(a.x, already, a.y);
                var B = new Vector3(b.x, yb, b.y);
                starts.Add((A, B - A));
                if (L < 0.9f) { postCount += PutPost(post, root.transform, B, B - A, pillars); continue; }
                int parts = Mathf.Max(1, Mathf.RoundToInt(L / Modulo));
                for (int t = 0; t < parts; t++)
                {
                    var Pa = Vector3.Lerp(A, B, t / (float)parts);
                    var Pb = Vector3.Lerp(A, B, (t + 1) / (float)parts);
                    PutRailing(railing, root.transform, Pa, Pb);
                    pillars.Add(Pb);
                    railingCount++;
                }
            }
        }
        foreach (var ini in starts) postCount += PutPost(post, root.transform, ini.p, ini.dir, pillars);
        EditorSceneManager.MarkSceneDirty(root.scene);
        report = "Puse " + railingCount + " barandas y " + postCount + " postes en " + pieces.Count + " piezas de camino.";
        return root;
    }

    static bool HasGround(Cells g, int i, int j, float y, float margin)
    {
        if (i < 0 || j < 0 || i >= g.nx || j >= g.nz) return false;
        foreach (var s in g.floors[j * g.nx + i]) if (Mathf.Abs(s - y) < margin) return true;
        return false;
    }

    static bool OtherPlatform(Cells g, int i, int j, int di, int dj, float y)
    {
        int steps = Mathf.CeilToInt(JumpGap / Cell);
        for (int k = 2; k <= steps; k++)
            if (HasGround(g, i + di * k, j + dj * k, y, 2.5f)) return true;
        return false;
    }

    static float WalkwayHeight(Cells g, Vector2 p)
    {
        int i = Mathf.FloorToInt((p.x - g.x0) / Cell), j = Mathf.FloorToInt((p.y - g.z0) / Cell);
        if (i < 0 || j < 0 || i >= g.nx || j >= g.nz) return float.NaN;
        return g.heightValue[j * g.nx + i];
    }

    static float FenceHeight(Cells g, Vector2 p)
    {
        float y = WalkwayHeight(g, p);
        if (!float.IsNaN(y)) return y;
        for (int r = 1; r <= 2; r++)
            foreach (var o in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                y = WalkwayHeight(g, p + o * Cell * r);
                if (!float.IsNaN(y)) return y;
            }
        return float.NaN;
    }

    static List<List<Vector2Int>> Chain(List<(Vector2Int a, Vector2Int b)> sides)
    {
        var perPoint = new Dictionary<Vector2Int, List<int>>();
        for (int i = 0; i < sides.Count; i++)
        {
            foreach (var p in new[] { sides[i].a, sides[i].b })
            {
                if (!perPoint.TryGetValue(p, out var l)) perPoint[p] = l = new List<int>();
                l.Add(i);
            }
        }
        var used = new bool[sides.Count];
        var lines = new List<List<Vector2Int>>();
        for (int i = 0; i < sides.Count; i++)
        {
            if (used[i]) continue;
            used[i] = true;
            var line = new LinkedList<Vector2Int>();
            line.AddLast(sides[i].a);
            line.AddLast(sides[i].b);
            for (int direction = 0; direction < 2; direction++)
            {
                while (true)
                {
                    var tip = direction == 0 ? line.Last.Value : line.First.Value;
                    var free = perPoint[tip].Where(k => !used[k]).ToList();
                    if (free.Count != 1 || perPoint[tip].Count > 2) break;
                    int s = free[0];
                    used[s] = true;
                    var other = sides[s].a == tip ? sides[s].b : sides[s].a;
                    if (direction == 0) line.AddLast(other); else line.AddFirst(other);
                }
            }
            lines.Add(line.ToList());
        }
        return lines;
    }

    static List<Vector2> Simplify(List<Vector2> pts, float eps)
    {
        if (pts.Count < 3) return new List<Vector2>(pts);
        float maxD = 0f; int idx = 0;
        for (int i = 1; i < pts.Count - 1; i++)
        {
            float d = DistanceToSegment(pts[i], pts[0], pts[pts.Count - 1]);
            if (d > maxD) { maxD = d; idx = i; }
        }
        if (maxD <= eps) return new List<Vector2> { pts[0], pts[pts.Count - 1] };
        var a = Simplify(pts.GetRange(0, idx + 1), eps);
        var b = Simplify(pts.GetRange(idx, pts.Count - idx), eps);
        a.RemoveAt(a.Count - 1);
        a.AddRange(b);
        return a;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    static void PutRailing(GameObject model, Transform parent, Vector3 A, Vector3 B)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        go.name = "Baranda";
        Vector3 axis = B - A;
        float length = axis.magnitude;
        Vector3 x = axis / length;
        Vector3 y = Vector3.ProjectOnPlane(Vector3.up, x).normalized;
        var rot = Quaternion.LookRotation(Vector3.Cross(x, y), y);
        float sx = length / Modulo;
        go.transform.SetPositionAndRotation(B, rot);
        go.transform.localScale = new Vector3(sx, RailingHeight, 1f);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(-1.16f, WallHeight / RailingHeight * 0.5f, 0f);
        box.size = new Vector3(3.08f, WallHeight / RailingHeight, 0.6f);
    }

    static int PutPost(GameObject model, Transform parent, Vector3 p, Vector3 dir, List<Vector3> pillars)
    {
        foreach (var q in pillars) if ((q - p).sqrMagnitude < 0.2f) return 0;
        pillars.Add(p);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        go.name = "Poste";
        dir.y = 0f;
        go.transform.SetPositionAndRotation(p, dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(Vector3.Cross(dir.normalized, Vector3.up), Vector3.up) : Quaternion.identity);
        go.transform.localScale = new Vector3(1f, RailingHeight, 1f);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, WallHeight / RailingHeight * 0.5f, 0f);
        box.size = new Vector3(0.76f, WallHeight / RailingHeight, 0.76f);
        return 1;
    }
}
