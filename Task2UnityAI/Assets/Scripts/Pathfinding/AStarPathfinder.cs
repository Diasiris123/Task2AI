using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AStarPathfinder : IPathfinder {
    private class Rec { public GridNode node; public float g, f; }

    public bool TryFindPath(GridGraph graph, Vector3 startW, Vector3 goalW,
        AgentMovementProfile profile, out List<Vector3> path, out List<GridNode> searched) {

        path = null; searched = new List<GridNode>();
        var start = graph.WorldToNode(startW);
        var goal  = graph.WorldToNode(goalW);

        var open = new List<Rec>();
        var gScore = new Dictionary<GridNode, float>();
        var fScore = new Dictionary<GridNode, float>();

        gScore[start] = 0f;
        fScore[start] = CostHeuristics.Heuristic(start, goal);
        open.Add(new Rec { node = start, g = 0f, f = fScore[start] });
        start.parent = null;

        while (open.Count > 0) {
            open = open.OrderBy(r => r.f).ToList();
            var current = open[0]; open.RemoveAt(0);
            searched.Add(current.node);
            if (current.node == goal) {
                path = CostHeuristics.Reconstruct(goal, graph.cellSize);
                return true;
            }
            foreach (var nb in graph.GetNeighbors(current.node)) {
                float tentative = gScore[current.node] +
                                  CostHeuristics.StepCost(current.node, nb, current.node.parent, profile);
                if (!gScore.TryGetValue(nb, out float gOld) || tentative < gOld) {
                    nb.parent = current.node;
                    gScore[nb] = tentative;
                    float f = tentative + CostHeuristics.Heuristic(nb, goal);
                    fScore[nb] = f;
                    if (!open.Any(r => r.node == nb))
                        open.Add(new Rec { node = nb, g = tentative, f = f });
                }
            }
        }
        return false;
    }
}

