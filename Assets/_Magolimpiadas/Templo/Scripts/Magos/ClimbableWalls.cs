using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class ClimbableWalls : MonoBehaviour
{
    public List<string> climbablePieces = new List<string>
    {
        "pilar_", "muro_", "ladrillos", "esquina_L_", "bloque_laurel", "suelo_relleno_", "suelo_base_",
    };

    public List<Rect> nonClimbableZones = new List<Rect>();

    public bool showInScene = true;

    Tilemap map;
    readonly Dictionary<TileBase, bool> cache = new Dictionary<TileBase, bool>();
    static readonly Regex cellSuffix = new Regex(@"_\d+_\d+$");

    void Awake() { map = GetComponent<Tilemap>(); }
    void OnValidate() { cache.Clear(); }

    public bool IsClimbable(Vector2 point, Vector2 normal)
    {
        if (map == null) map = GetComponent<Tilemap>();
        foreach (var z in nonClimbableZones) if (z.Contains(point - normal * 0.1f)) return false;
        for (float deep = 0.05f; deep <= 0.45f; deep += 0.2f)
        {
            var tile = map.GetTile(map.WorldToCell(point - normal * deep));
            if (tile != null) return Climbable(tile);
        }
        return false;
    }

    bool Climbable(TileBase tile)
    {
        if (cache.TryGetValue(tile, out bool si)) return si;
        string piece = PieceName(tile.name);
        si = false;
        foreach (var p in climbablePieces)
            if (!string.IsNullOrEmpty(p) && piece.StartsWith(p)) { si = true; break; }
        cache[tile] = si;
        return si;
    }

    public static string PieceName(string tileName) => cellSuffix.Replace(tileName, "");

    void OnDrawGizmosSelected()
    {
        if (!showInScene) return;
        if (map == null) map = GetComponent<Tilemap>();
        cache.Clear();
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.35f);
        var b = map.cellBounds;
        foreach (var c in b.allPositionsWithin)
        {
            var t = map.GetTile(c);
            if (t == null || !Climbable(t)) continue;
            Vector3 center = map.GetCellCenterWorld(c);
            bool excluded = false;
            foreach (var z in nonClimbableZones) if (z.Contains(center)) excluded = true;
            if (excluded) continue;
            Gizmos.DrawCube(center, map.layoutGrid.cellSize * 0.95f);
        }
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        foreach (var z in nonClimbableZones) Gizmos.DrawWireCube(z.center, z.size);
    }
}
