using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "HasLineOfSight",
    story: "Writes CanSeeTarget and updates LastSeen* when intruder is visible within Range.",
    category: "Action/Sense",
    id: "los_basic_v3")]
public partial class HasLineOfSightAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Guard;
    [SerializeReference] public BlackboardVariable<Transform> Intruder;

    [SerializeReference] public BlackboardVariable<float> Range;         // meters
    [SerializeReference] public BlackboardVariable<int> OccludersMask;   // layer mask as int

    [SerializeReference] public BlackboardVariable<bool> CanSeeTarget;   // WRITE
    [SerializeReference] public BlackboardVariable<Vector3> LastSeenPos; // WRITE when seen
    [SerializeReference] public BlackboardVariable<float>  LastSeenTime; // WRITE when seen

    public float eyeHeight = 1.6f;

    protected override Status OnUpdate()
    {
        var guardGO = Guard?.Value;
        var intr    = Intruder?.Value;
        float range = Range?.Value ?? 0f;
        int mask    = OccludersMask?.Value ?? ~0;

        bool canSee = false;

        if (guardGO != null && intr != null && range > 0f)
        {
            Vector3 eye = guardGO.transform.position + Vector3.up * eyeHeight;
            Vector3 to  = intr.position - eye;
            float dist  = to.magnitude;

            if (dist <= range)
            {
                Vector3 dir = (dist > 0.0001f) ? to / dist : Vector3.forward;

                // If we hit ANY occluder first -> blocked; if we hit nothing -> clear.
                bool blocked = Physics.Raycast(eye, dir, dist, mask, QueryTriggerInteraction.Ignore);
                canSee = !blocked;
            }
        }

        if (CanSeeTarget != null) CanSeeTarget.Value = canSee;

        if (canSee && intr != null)
        {
            if (LastSeenPos  != null) LastSeenPos.Value  = intr.position;
            if (LastSeenTime != null) LastSeenTime.Value = Time.time;
        }

        // Always succeed so downstream nodes still run this frame.
        return Status.Success;
    }
}
