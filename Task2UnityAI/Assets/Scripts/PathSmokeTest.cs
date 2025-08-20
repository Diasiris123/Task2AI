using System.Collections.Generic;
using UnityEngine;

public class PathSmokeTest : MonoBehaviour
{
    public GridGraph graph;
    public PathFollower follower;
    public AgentMovementProfile profile;
    public Transform target;
    public bool useGreedy = false;      // toggle to test both
    public float replanEvery = 0.3f;

    IPathfinder astar = new AStarPathfinder();
    IPathfinder greedy = new GreedyBestFirstPathfinder();
    float nextAt = 0f;

    void Update()
    {
        if (!graph || !follower || !profile || !target) return;
        if (Time.time < nextAt) return;
        nextAt = Time.time + Mathf.Max(0.1f, replanEvery);

        var start = follower.transform.position;
        var goal  = target.position;

        if (!graph.ContainsWorldPoint(start) || !graph.ContainsWorldPoint(goal))
        {
            Debug.LogWarning($"[SmokeTest] start/goal outside grid. start={start} goal={goal}");
            return;
        }

        var pf = useGreedy ? greedy : astar;
        if (pf.TryFindPath(graph, start, goal, profile, out List<Vector3> path, out _))
        {
            // Always set the path here to visualize it clearly
            follower.SetPath(path);
            Debug.Log($"[SmokeTest] Set path: {path.Count} points");
        }
        else
        {
            Debug.LogWarning("[SmokeTest] No path");
        }
    }
}