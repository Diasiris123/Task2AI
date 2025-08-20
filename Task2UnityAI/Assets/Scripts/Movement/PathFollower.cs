using System.Collections.Generic;
using UnityEngine;

public enum FollowMover { CharacterController, Transform }

[RequireComponent(typeof(CharacterController))]
public class PathFollower : MonoBehaviour
{
    [Header("Movement Profile")]
    public AgentMovementProfile profile;

    [Header("Follower Settings")]
    public FollowMover mover = FollowMover.CharacterController;
    public bool useSimpleMoveForDebug = true;
    [Tooltip("Distance to consider a waypoint reached.")]
    public float waypointTolerance = 0.55f;
    [Tooltip("Extra tolerance applied only for adoption after a replan.")]
    public float adoptExtraTolerance = 0.25f;
    [Tooltip("If corner angle is above this, briefly slow/hold to turn.")]
    public float cornerSlowdownAngle = 50f;
    public bool lockY = true;
    public float gravity = -12f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public bool logState = false;

    private CharacterController cc;
    private readonly List<Vector3> _path = new(); // keep as list to index easily
    private int _index;                            // current waypoint index
    private Vector3 velocity;
    private Vector3 lastDir = Vector3.zero;
    private float turnCooldown;
    private float baseY;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        baseY = transform.position.y;

        cc.stepOffset = Mathf.Max(cc.stepOffset, 0.35f);
        cc.slopeLimit = Mathf.Max(cc.slopeLimit, 45f);
        cc.skinWidth = Mathf.Max(cc.skinWidth, 0.02f);

        if (profile == null)
            Debug.LogWarning("[PathFollower] No AgentMovementProfile assigned.");
    }

    /// <summary>Adopt a (possibly new) path but keep progress: start at the first waypoint that isn't already within tolerance.</summary>
    public void AdoptPath(List<Vector3> pts)
    {
        _path.Clear();
        if (pts == null || pts.Count == 0) return;

        // Normalize Y
        float y = lockY ? transform.position.y : pts[0].y;
        for (int i = 0; i < pts.Count; i++)
            _path.Add(new Vector3(pts[i].x, y, pts[i].z));

        // Find starting index based on current position
        _index = FindAdoptionIndex(ptsTolerance: waypointTolerance + adoptExtraTolerance);

        if (logState)
            Debug.Log($"[PathFollower] Adopted path len={_path.Count}, startIndex={_index}");
    }

    /// <summary>Simple setter (kept for API compatibility). Uses adoption logic.</summary>
    public void SetPath(List<Vector3> pts) => AdoptPath(pts);

    int FindAdoptionIndex(float ptsTolerance)
    {
        if (_path.Count == 0) return 0;

        Vector3 here = transform.position;
        if (lockY) here.y = _path[0].y;

        // Choose the first waypoint farther than tolerance
        for (int i = 0; i < _path.Count; i++)
        {
            float d = Vector3.Distance(new Vector3(here.x, 0f, here.z), new Vector3(_path[i].x, 0f, _path[i].z));
            if (d > ptsTolerance)
                return Mathf.Clamp(i, 0, _path.Count - 1);
        }

        // All points are within tolerance -> if path has more than one point, pick the last (goal).
        return Mathf.Max(0, _path.Count - 1);
    }

    void Update()
    {
        if (profile == null || _path.Count == 0) return;
        if (_index >= _path.Count) { velocity = Vector3.zero; return; }

        Vector3 pos = transform.position;
        Vector3 target = _path[_index];
        if (lockY) { pos.y = baseY; target.y = baseY; }

        Vector3 to = target - pos;
        float dist = to.magnitude;

        // Reached current waypoint?
        if (dist <= waypointTolerance + (cc ? cc.radius * 0.5f : 0f))
        {
            _index++;
            if (_index >= _path.Count) { velocity = Vector3.zero; return; }
            target = _path[_index];
            to = target - pos;
            dist = to.magnitude;
        }

        Vector3 dir = dist > 0.0001f ? to / dist : Vector3.zero;

        // Corner handling
        if (lastDir != Vector3.zero)
        {
            float angle = Vector3.Angle(lastDir, dir);
            if (angle > cornerSlowdownAngle) turnCooldown = profile.turnDelay;
        }
        if (turnCooldown > 0f)
        {
            turnCooldown -= Time.deltaTime;
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, profile.acceleration * 0.5f * Time.deltaTime);
            return;
        }

        // Accelerate toward desired velocity
        Vector3 desiredVel = dir * profile.maxSpeed;
        velocity = Vector3.MoveTowards(velocity, desiredVel, profile.acceleration * Time.deltaTime);

        // Move
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);

        if (mover == FollowMover.CharacterController && cc != null)
        {
            if (useSimpleMoveForDebug)
            {
                cc.SimpleMove(planar);
            }
            else
            {
                float vy = lockY ? 0f : gravity * Time.deltaTime;
                cc.Move(new Vector3(planar.x, vy, planar.z) * Time.deltaTime);
            }
        }
        else
        {
            transform.position += new Vector3(planar.x, lockY ? 0f : velocity.y, planar.z) * Time.deltaTime;
        }

        // Face movement direction
        if (planar.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
        }

        lastDir = dir;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || _path == null || _path.Count == 0) return;

        Gizmos.color = Color.yellow;
        Vector3 prev = transform.position;
        for (int i = _index; i < _path.Count; i++)
        {
            Vector3 q = new Vector3(_path[i].x, lockY ? (Application.isPlaying ? baseY : transform.position.y) : _path[i].y, _path[i].z);
            Gizmos.DrawLine(prev, q);
            Gizmos.DrawWireSphere(q, 0.1f);
            prev = q;
        }

        // tolerance ring
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_path[Mathf.Clamp(_index,0,_path.Count-1)], waypointTolerance + (cc ? cc.radius * 0.5f : 0f));
    }
#endif
}
