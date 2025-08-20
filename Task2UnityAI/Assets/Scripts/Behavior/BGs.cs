/*using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class BG_RequestPath : MonoBehaviour {
    public GridGraph graph;
    public PathFollower follower;
    public AgentMovementProfile profile;

    private readonly IPathfinder astar  = new AStarPathfinder();
    private readonly IPathfinder greedy = new GreedyBestFirstPathfinder();

    public bool Request(Blackboard bb) {
        Vector3 start = follower.transform.position;
        Vector3 goal  = bb.GetVector3("Destination");
        var algo = (PathAlgo)bb.GetInt("PathAlgo");

        var pf = algo == PathAlgo.AStar ? astar : greedy;
        if (pf.TryFindPath(graph, start, goal, profile, out List<Vector3> path, out var _)) {
            follower.SetPath(path);
            bb.SetBool("HasPath", true);
            return true;
        }
        bb.SetBool("HasPath", false);
        return false;
    }
}
} */