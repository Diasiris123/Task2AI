using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Where the grid's Transform.position is anchored.</summary>
public enum GridAnchor { BottomLeft, Center }

/// <summary>
/// Build a walkability grid from colliders, map world to cells, and provide neighbors.
/// Designed for A*/Greedy on XZ (y ignored).
/// </summary>
public class GridGraph : MonoBehaviour
{
    [Header("Grid Size & Cell")]
    public Vector2Int size = new Vector2Int(60, 60);
    [Min(0.01f)] public float cellSize = 1f;

    [Header("Walkability (Physics)")]
    public LayerMask unwalkableMask;              // walls/obstacles here (NOT the ground)
    public float walkCheckHalfHeight = 0.5f;      // box cast half-height when checking obstacles

    [Header("Movement Rules")]
    public bool allowDiagonal = true;
    public bool preventDiagonalCornerCutting = true;

    [Header("Origin / Anchor")]
    public GridAnchor anchor = GridAnchor.Center; // Center is usually easiest
    public bool autoBuildOnAwake = true;          // build once at runtime automatically
    public bool rebuildOnValidate = true;         // editor QoL: rebuild when values change

    // Internal
    private GridNode[,] nodes;

    // 4- and 8-direction neighbor offsets
    private static readonly Vector2Int[] DIRS4 = {
        new Vector2Int( 1, 0), new Vector2Int(-1, 0),
        new Vector2Int( 0, 1), new Vector2Int( 0,-1)
    };
    private static readonly Vector2Int[] DIRS8 = {
        new Vector2Int( 1, 0), new Vector2Int(-1, 0),
        new Vector2Int( 0, 1), new Vector2Int( 0,-1),
        new Vector2Int( 1, 1), new Vector2Int( 1,-1),
        new Vector2Int(-1, 1), new Vector2Int(-1,-1)
    };

    private void Awake()
    {
        if (autoBuildOnAwake) Build();
    }

    /// <summary>World-space origin (bottom-left corner) derived from anchor & transform.</summary>
    private Vector3 GetOrigin()
    {
        var origin = transform.position;
        if (anchor == GridAnchor.Center)
            origin -= new Vector3(size.x * cellSize, 0f, size.y * cellSize) * 0.5f;
        return origin;
    }

    /// <summary>Rebuild the grid and sample walkability from physics.</summary>
    public void Build()
    {
        if (size.x <= 0 || size.y <= 0 || cellSize <= 0f)
        {
            Debug.LogWarning("[GridGraph] Invalid size/cellSize; skipping Build().");
            nodes = null;
            return;
        }

        nodes = new GridNode[size.x, size.y];
        Vector3 origin = GetOrigin();

        // Precompute extents for the obstacle check box
        Vector3 halfExtents = new Vector3(cellSize * 0.45f, walkCheckHalfHeight, cellSize * 0.45f);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector3 center = origin + new Vector3(x * cellSize + cellSize * 0.5f, 0f, y * cellSize + cellSize * 0.5f);

                bool blocked = Physics.CheckBox(
                    center,
                    halfExtents,
                    Quaternion.identity,
                    unwalkableMask,
                    QueryTriggerInteraction.Ignore);

                nodes[x, y] = new GridNode
                {
                    x = x,
                    y = y,
                    worldPos = center,
                    walkable = !blocked,
                    baseCost = 1f
                };
            }
        }
    }

    /// <summary>Returns true if world position is inside grid XY bounds (XZ in world).</summary>
    public bool ContainsWorldPoint(Vector3 world)
    {
        Vector3 origin = GetOrigin();
        Vector3 local = world - origin;
        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.z / cellSize);
        return x >= 0 && y >= 0 && x < size.x && y < size.y;
    }

    /// <summary>Clamp world to nearest node on the grid.</summary>
    public GridNode WorldToNode(Vector3 world)
    {
        Vector3 origin = GetOrigin();
        Vector3 local = world - origin;
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / cellSize), 0, size.x - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(local.z / cellSize), 0, size.y - 1);
        return nodes[x, y];
    }

    /// <summary>Enumerate neighbors for a node with 4- or 8-connectivity.</summary>
    public IEnumerable<GridNode> GetNeighbors(GridNode n)
    {
        if (nodes == null) yield break;
        var dirs = allowDiagonal ? DIRS8 : DIRS4;
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = n.x + dirs[i].x;
            int ny = n.y + dirs[i].y;

            if (nx < 0 || ny < 0 || nx >= size.x || ny >= size.y)
                continue;

            GridNode m = nodes[nx, ny];
            if (!m.walkable)
                continue;

            // Optional: prevent cutting corners between two blocked orthogonals.
            if (allowDiagonal && preventDiagonalCornerCutting && Mathf.Abs(dirs[i].x) + Mathf.Abs(dirs[i].y) == 2)
            {
                // If either adjacent orthogonal is blocked, skip this diagonal
                int sx = n.x + dirs[i].x;
                int sy = n.y;
                int tx = n.x;
                int ty = n.y + dirs[i].y;

                bool side1Blocked = (sx < 0 || sx >= size.x) ? true : !nodes[sx, sy].walkable;
                bool side2Blocked = (ty < 0 || ty >= size.y) ? true : !nodes[tx, ty].walkable;

                if (side1Blocked || side2Blocked) continue;
            }

            yield return m;
        }
    }

    /// <summary>Bulk set walkability for a rectangle (e.g., doors/safe zones).</summary>
    public void SetWalkable(RectInt rect, bool value)
    {
        if (nodes == null) return;

        int xMin = Mathf.Max(0, rect.xMin);
        int yMin = Mathf.Max(0, rect.yMin);
        int xMax = Mathf.Min(size.x, rect.xMax);
        int yMax = Mathf.Min(size.y, rect.yMax);

        for (int x = xMin; x < xMax; x++)
            for (int y = yMin; y < yMax; y++)
                nodes[x, y].walkable = value;
    }

    /// <summary>World-space bounds of the entire grid.</summary>
    public Bounds GetWorldBounds()
    {
        Vector3 origin = GetOrigin();
        Vector3 sizeWorld = new Vector3(size.x * cellSize, 0.01f, size.y * cellSize);
        return new Bounds(origin + new Vector3(sizeWorld.x * 0.5f, 0f, sizeWorld.z * 0.5f), sizeWorld);
    }

    /// <summary>Convenience: center grid between two world points and rebuild.</summary>
    public void CenterBetween(Vector3 a, Vector3 b, int marginCells = 2)
    {
        anchor = GridAnchor.Center;

        var min = Vector3.Min(a, b);
        var max = Vector3.Max(a, b);

        float width = Mathf.Max(2f, max.x - min.x);
        float depth = Mathf.Max(2f, max.z - min.z);

        int nx = Mathf.CeilToInt(width / cellSize) + marginCells * 2;
        int ny = Mathf.CeilToInt(depth / cellSize) + marginCells * 2;

        size = new Vector2Int(Mathf.Max(4, nx), Mathf.Max(4, ny));
        transform.position = new Vector3((min.x + max.x) * 0.5f, transform.position.y, (min.z + max.z) * 0.5f);
        Build();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!rebuildOnValidate || Application.isPlaying) return;

        // Delay to avoid rebuilding multiple times per inspector change
        EditorApplication.delayCall += () =>
        {
            if (this != null) Build();
        };
    }
#endif

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw outer bounds (cyan)
        var bounds = GetWorldBounds();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Draw cells if built
        if (nodes == null) return;
        foreach (var n in nodes)
        {
            Gizmos.color = n.walkable ? Color.white : Color.red;
            Gizmos.DrawWireCube(n.worldPos, new Vector3(cellSize * 0.9f, 0.01f, cellSize * 0.9f));
        }
    }
#endif
}
