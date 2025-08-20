using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "RequestPath (Timed + Snap)",
    story: "Reads Destination/PathAlgo, snaps to walkable if needed, solves, and feeds PathFollower periodically.",
    category: "Action/Pathfinding",
    id: "request_path_timed_snap_v2")]
public partial class RequestPathAction : Action
{
    // Scene refs from Blackboard
    [SerializeReference] public BlackboardVariable<GameObject> GraphGO;       // GridGraph holder
    [SerializeReference] public BlackboardVariable<GameObject> FollowerGO;    // has PathFollower
    [SerializeReference] public BlackboardVariable<AgentMovementProfile> Profile;

    // Inputs
    [SerializeReference] public BlackboardVariable<Vector3> Destination;
    [SerializeReference] public BlackboardVariable<int>    PathAlgo;   // 0=A*, 1=Greedy

    // Output
    [SerializeReference] public BlackboardVariable<bool>   HasPath;    // WRITE (optional)

    // Settings
    public float replanInterval = 0.35f; // chase ~0.3, patrol ~0.7–1.0 (you can set two nodes with different intervals if desired)
    public bool  verbose = false;

    // Snapping radius (in cells)
    public int startSnapRadiusCells = 6;
    public int goalSnapRadiusCells  = 8;

    // Internals
    private GridGraph _graph;
    private PathFollower _follower;
    private AgentMovementProfile _profile;
    private float _nextAt;

    private readonly IPathfinder _astar  = new AStarPathfinder();
    private readonly IPathfinder _greedy = new GreedyBestFirstPathfinder();

    protected override Status OnStart()
    {
        Resolve();
        _nextAt = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!EnsureRefs())
        {
            WriteHasPath(false);
            return Status.Running;
        }

        if (Time.time < _nextAt) return Status.Running;
        _nextAt = Time.time + Mathf.Max(0.05f, replanInterval);

        Vector3 goal = Destination != null ? Destination.Value : _follower.transform.position;
        Vector3 start = _follower.transform.position;

        // Validate inside grid
        if (!_graph.ContainsWorldPoint(start) || !_graph.ContainsWorldPoint(goal))
        {
            if (verbose) Debug.LogWarning("[RequestPath] start/goal outside grid.");
            WriteHasPath(false);
            return Status.Running;
        }

        // Snap to nearest walkable if needed
        if (!IsWalkableWorld(_graph, start) && !TrySnapToNearestWalkable(_graph, start, startSnapRadiusCells, out start))
        {
            if (verbose) Debug.LogWarning("[RequestPath] No walkable start nearby.");
            WriteHasPath(false);
            return Status.Running;
        }
        if (!IsWalkableWorld(_graph, goal) && !TrySnapToNearestWalkable(_graph, goal, goalSnapRadiusCells, out goal))
        {
            if (verbose) Debug.LogWarning("[RequestPath] No walkable goal nearby.");
            WriteHasPath(false);
            return Status.Running;
        }

        int algo = (PathAlgo != null) ? PathAlgo.Value : 0;
        var pf = (algo == 1) ? _greedy : _astar;

        if (verbose) Debug.Log($"[RequestPath] start={start} goal={goal} algo={(algo==1?"Greedy":"A*")}");

        if (pf.TryFindPath(_graph, start, goal, _profile, out List<Vector3> path, out _))
        {
            _follower.SetPath(path);
            WriteHasPath(true);
            if (verbose) Debug.Log($"[RequestPath] path len={path.Count}");
        }
        else
        {
            WriteHasPath(false);
            if (verbose) Debug.LogWarning("[RequestPath] no path");
        }

        return Status.Running;
    }

    // -------- helpers --------
    void Resolve()
    {
        if (_graph == null)
        {
            var go = GraphGO != null ? GraphGO.Value : null;
            _graph = go ? go.GetComponent<GridGraph>() : null;
        }
        if (_follower == null)
        {
            var fo = FollowerGO != null ? FollowerGO.Value : null;
            _follower = fo ? fo.GetComponent<PathFollower>() : null;
        }
        if (_profile == null && Profile != null) _profile = Profile.Value;

        if (verbose) Debug.Log($"[RequestPath] resolve graph={_graph} follower={_follower} profile={_profile}");
    }

    bool EnsureRefs()
    {
        if (_graph == null || _follower == null || _profile == null) Resolve();
        return _graph != null && _follower != null && _profile != null;
    }

    void WriteHasPath(bool v) { if (HasPath != null) HasPath.Value = v; }

    bool IsWalkableWorld(GridGraph g, Vector3 world)
    {
        if (!g.ContainsWorldPoint(world)) return false;
        return g.WorldToNode(world).walkable;
    }

    bool TrySnapToNearestWalkable(GridGraph g, Vector3 world, int maxDepth, out Vector3 snapped)
    {
        snapped = world;
        if (!g.ContainsWorldPoint(world)) return false;

        var startNode = g.WorldToNode(world);
        if (startNode.walkable) { snapped = startNode.worldPos; return true; }

        var q = new Queue<(GridNode node, int depth)>();
        var visited = new HashSet<GridNode>();
        q.Enqueue((startNode, 0));
        visited.Add(startNode);

        while (q.Count > 0)
        {
            var (n, d) = q.Dequeue();
            if (d > maxDepth) break;

            foreach (var nb in g.GetNeighbors(n))
            {
                if (visited.Contains(nb)) continue;
                visited.Add(nb);

                if (nb.walkable) { snapped = nb.worldPos; return true; }
                q.Enqueue((nb, d + 1));
            }
        }
        return false;
    }
}
