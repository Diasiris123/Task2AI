using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GreedyBestFirstPathfinder : IPathfinder {
    public bool TryFindPath(GridGraph graph, Vector3 startW, Vector3 goalW,
        AgentMovementProfile profile, out List<Vector3> path, out List<GridNode> searched) {

        path = null; searched = new List<GridNode>();
        var start = graph.WorldToNode(startW);
        var goal  = graph.WorldToNode(goalW);

        var open = new List<GridNode> { start };
        var closed = new HashSet<GridNode>();
        start.parent = null;

        while (open.Count > 0) {
            open = open.OrderBy(n => CostHeuristics.Heuristic(n, goal)).ToList();
            var current = open[0]; open.RemoveAt(0);
            searched.Add(current);

            if (current == goal) {
                path = CostHeuristics.Reconstruct(goal, graph.cellSize);
                return true;
            }

            closed.Add(current);
            foreach (var nb in graph.GetNeighbors(current)) {
                if (closed.Contains(nb)) continue;
                
                float cost = CostHeuristics.StepCost(current, nb, current.parent, profile) +
                             0.001f * CostHeuristics.Heuristic(nb, goal); 
                if (nb.parent == null || cost < CostHeuristics.StepCost(nb.parent, nb, nb.parent?.parent, profile)) {
                    nb.parent = current;
                }
                if (!open.Contains(nb)) open.Add(nb);
            }
        }
        return false;
    }
}