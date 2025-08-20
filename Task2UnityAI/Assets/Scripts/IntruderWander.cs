using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class IntruderWander : MonoBehaviour
{
    [Header("Wander Area (world-space AABB)")]
    public Vector3 areaCenter;
    public Vector3 areaSize = new Vector3(20, 0, 20);

    [Header("Movement")]
    public float maxSpeed = 3.5f;
    public float acceleration = 10f;
    public float turnSpeed = 10f;
    public float waypointReachDistance = 0.35f;
    public float pickIntervalMin = 3f;
    public float pickIntervalMax = 6f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;      // put your "Unwalkable" (and walls) here
    public float probeDistance = 1.2f;  // how far to look ahead
    public float sideProbeAngle = 35f;  // side checks left/right in degrees
    public float avoidStrength = 0.6f;  // how strongly to steer when detecting an obstacle

    [Header("Flee (optional)")]
    public Transform danger;            // assign the Guard here if you want flee behavior
    public float fleeRange = 10f;       // start fleeing under this distance
    public float fleeSpeedBoost = 1.25f;

    [Header("Stuck Handling")]
    public float repickIfStuckTime = 1.5f;
    public float minMovedForProgress = 0.15f;

    CharacterController cc;
    Vector3 target;
    Vector3 velocity;
    float nextPickTime;
    Vector3 lastPos;
    float stuckTimer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (areaSize.x < 0.1f) areaSize.x = 10;
        if (areaSize.z < 0.1f) areaSize.z = 10;
        PickNewTarget();
        lastPos = transform.position;
    }

    void Update()
    {
        // Re-pick periodically
        if (Time.time >= nextPickTime || Vector3.Distance(Flat(transform.position), Flat(target)) <= waypointReachDistance)
            PickNewTarget();

        // Flee logic (override target) if danger is close
        bool fleeing = false;
        if (danger != null)
        {
            Vector3 toDanger = danger.position - transform.position;
            if (toDanger.sqrMagnitude <= fleeRange * fleeRange)
            {
                fleeing = true;
                Vector3 fleeDir = (-toDanger).normalized;
                SetTarget(ClampInsideArea(transform.position + fleeDir * Mathf.Max(5f, fleeRange * 0.75f)));
            }
        }

        // Desired direction toward target
        Vector3 pos = transform.position;
        Vector3 to = Flat(target - pos);
        Vector3 desiredDir = to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;

        // Obstacle avoidance via raycasts (center + slight left/right)
        desiredDir = AvoidObstacles(desiredDir);

        // Accelerate toward desired velocity
        float topSpeed = fleeing ? maxSpeed * fleeSpeedBoost : maxSpeed;
        Vector3 desiredVel = desiredDir * topSpeed;
        velocity = Vector3.MoveTowards(velocity, desiredVel, acceleration * Time.deltaTime);

        // Move (keep Y unchanged)
        Vector3 move = velocity * Time.deltaTime;
        move.y = 0f;
        cc.Move(move);

        // Turn toward move direction
        Vector3 moveDir = Flat(velocity);
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // Stuck detection
        float moved = Vector3.Distance(Flat(transform.position), Flat(lastPos));
        if (moved < minMovedForProgress) stuckTimer += Time.deltaTime;
        else stuckTimer = 0f;

        if (stuckTimer >= repickIfStuckTime)
        {
            PickNewTarget(forceFarFrom: transform.position);
            stuckTimer = 0f;
        }

        lastPos = transform.position;
    }

    // ---------- Helpers ----------

    Vector3 AvoidObstacles(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(0.5f, cc.height * 0.3f);
        float radius = Mathf.Max(0.1f, cc.radius * 0.9f);

        // forward probe
        if (Physics.SphereCast(origin, radius, dir, out var hit, probeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // try to steer around: sample left/right
            Vector3 left = Quaternion.AngleAxis(-sideProbeAngle, Vector3.up) * dir;
            Vector3 right = Quaternion.AngleAxis(sideProbeAngle, Vector3.up) * dir;

            bool leftFree  = !Physics.SphereCast(origin, radius, left,  out _, probeDistance * 0.9f, obstacleMask, QueryTriggerInteraction.Ignore);
            bool rightFree = !Physics.SphereCast(origin, radius, right, out _, probeDistance * 0.9f, obstacleMask, QueryTriggerInteraction.Ignore);

            if (leftFree && !rightFree)  dir = Vector3.Slerp(dir, left,  avoidStrength);
            else if (!leftFree && rightFree) dir = Vector3.Slerp(dir, right, avoidStrength);
            else
            {
                // both blocked or both free: bias away from hit normal
                Vector3 away = Vector3.ProjectOnPlane(dir + hit.normal, Vector3.up).normalized;
                dir = Vector3.Slerp(dir, away, avoidStrength);
            }
        }

        return Flat(dir).normalized;
    }

    void PickNewTarget(Vector3? forceFarFrom = null)
    {
        // random point inside area box
        Vector3 half = areaSize * 0.5f;
        Vector3 rnd = new Vector3(
            Random.Range(-half.x, half.x),
            0f,
            Random.Range(-half.z, half.z)
        );
        Vector3 candidate = areaCenter + rnd;

        // if requested, ensure it's not too close
        if (forceFarFrom.HasValue)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Vector3.Distance(Flat(candidate), Flat(forceFarFrom.Value)) >= Mathf.Min(areaSize.x, areaSize.z) * 0.25f)
                    break;
                rnd = new Vector3(Random.Range(-half.x, half.x), 0f, Random.Range(-half.z, half.z));
                candidate = areaCenter + rnd;
            }
        }

        SetTarget(candidate);
        // schedule next pick
        nextPickTime = Time.time + Random.Range(pickIntervalMin, pickIntervalMax);
    }

    void SetTarget(Vector3 p)
    {
        target = ClampInsideArea(p);
    }

    Vector3 ClampInsideArea(Vector3 p)
    {
        Vector3 half = areaSize * 0.5f;
        return new Vector3(
            Mathf.Clamp(p.x, areaCenter.x - half.x, areaCenter.x + half.x),
            transform.position.y,
            Mathf.Clamp(p.z, areaCenter.z - half.z, areaCenter.z + half.z)
        );
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // area
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        Gizmos.DrawCube(areaCenter.y == 0 ? new Vector3(areaCenter.x, transform.position.y, areaCenter.z) : areaCenter,
                        new Vector3(areaSize.x, 0.05f, areaSize.z));
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(areaCenter.x, transform.position.y, areaCenter.z),
                            new Vector3(areaSize.x, 0.05f, areaSize.z));

        // target
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(target.x, transform.position.y, target.z), 0.25f);

        // forward probe
        if (Application.isPlaying)
        {
            Vector3 origin = transform.position + Vector3.up * Mathf.Max(0.5f, cc != null ? cc.height * 0.3f : 0.5f);
            Vector3 fwd = transform.forward * probeDistance;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + fwd);
        }
    }
#endif
}
