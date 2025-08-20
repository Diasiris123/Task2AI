using System.Collections.Generic;
using UnityEngine;

public interface IPathfinder {
    bool TryFindPath(GridGraph graph, Vector3 start, Vector3 goal,
        AgentMovementProfile profile, out List<Vector3> path,
        out List<GridNode> searchedNodes);
}

public static class CostHeuristics {
    public static float Heuristic(GridNode a, GridNode b) {
        // Octile distance (good for 8-dir grids)
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        int min = Mathf.Min(dx, dy), max = Mathf.Max(dx, dy);
        return 1.414f * min + (max - min);
    }

    public static float StepCost(GridNode from, GridNode to, GridNode parent,
        AgentMovementProfile prof) {
        bool diagonal = (from.x != to.x) && (from.y != to.y);
        float move = diagonal ? prof.diagonalCost : 1f;

        // Turning penalty/continuation bonus
        float turn = 0f;
        if (parent != null) {
            int px = from.x - parent.x; int py = from.y - parent.y;
            int cx = to.x - from.x;     int cy = to.y - from.y;
            bool same = (px == cx && py == cy);
            turn += same ? -prof.continueBonus : prof.turnPenalty;
        }
        return Mathf.Max(0.01f, move + turn + to.baseCost);
    }

    public static List<Vector3> Reconstruct(GridNode end, float cellSize) {
        var list = new List<Vector3>();
        var cur = end;
        while (cur != null) {
            list.Add(cur.worldPos);
            cur = cur.parent;
        }
        list.Reverse();
        return list;
    }
}

