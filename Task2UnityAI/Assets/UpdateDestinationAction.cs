using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "ChooseDestination (Simple)",
    story: "Writes Destination from Patrol or Intruder with hysteresis; no last-seen memory.",
    category: "Action/Decision",
    id: "choose_dest_simple_stable_v1_nogoto")]
public partial class UpdateDestinationAction : Action
{
    // Inputs
    [SerializeReference] public BlackboardVariable<bool>      CanSeeTarget;
    [SerializeReference] public BlackboardVariable<Transform> Guard;       // to compute distance gating
    [SerializeReference] public BlackboardVariable<Transform> Intruder;
    [SerializeReference] public BlackboardVariable<Transform> PatrolPoint;
    [SerializeReference] public BlackboardVariable<float>     Range;

    // Outputs
    [SerializeReference] public BlackboardVariable<Vector3> Destination;  // WRITE
    [SerializeReference] public BlackboardVariable<bool>   IsChasing;    // WRITE

    // Hysteresis (to avoid flicker)
    public float seeConfirmTime = 0.35f;  // must continuously see this long to enter Chase
    public float loseGraceTime  = 0.80f;  // must continuously lose this long to leave Chase
    public float minModeTime    = 0.50f;  // dwell time before switching

    // Internals
    private enum Mode { Patrol, Chase }
    private Mode _mode = Mode.Patrol;
    private float _modeNoSwitchBefore;
    private float _seenSince = -1f, _lostSince = -1f;

    protected override Status OnStart()
    {
        _mode = Mode.Patrol;
        _modeNoSwitchBefore = 0f;
        _seenSince = _lostSince = -1f;

        // Safe initial destination
        if (Destination != null)
        {
            Vector3 d = (PatrolPoint != null && PatrolPoint.Value != null)
                ? PatrolPoint.Value.position
                : (Guard != null && Guard.Value != null ? Guard.Value.position : Vector3.zero);
            Destination.Value = d;
        }
        if (IsChasing != null) IsChasing.Value = false;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        bool canSeeBB = CanSeeTarget != null && CanSeeTarget.Value;

        // Extra distance gate
        bool withinRange = false;
        if (Guard != null && Guard.Value != null && Intruder != null && Intruder.Value != null)
        {
            float r = Range?.Value ?? 0f;
            float dist = Vector3.Distance(Flat(Guard.Value.position), Flat(Intruder.Value.position));
            withinRange = r > 0f && dist <= r;
        }
        bool canSee = canSeeBB && withinRange;

        float now = Time.time;
        if (canSee)
        {
            if (_seenSince < 0f) _seenSince = now;
            _lostSince = -1f;
        }
        else
        {
            if (_lostSince < 0f) _lostSince = now;
            _seenSince = -1f;
        }

        // FSM with hysteresis (no memory mode)
        if (now >= _modeNoSwitchBefore)
        {
            if (_mode == Mode.Patrol)
            {
                if (canSee && _seenSince >= 0f && (now - _seenSince) >= seeConfirmTime)
                    SwitchMode(Mode.Chase);
            }
            else // Mode.Chase
            {
                if (!canSee && _lostSince >= 0f && (now - _lostSince) >= loseGraceTime)
                    SwitchMode(Mode.Patrol);
            }
        }

        // Choose Destination (no goto)
        Vector3 dest;
        bool chasing = (_mode == Mode.Chase);

        if (chasing && Intruder != null && Intruder.Value != null)
        {
            dest = Intruder.Value.position;
        }
        else
        {
            dest = (PatrolPoint != null && PatrolPoint.Value != null)
                ? PatrolPoint.Value.position
                : (Guard != null && Guard.Value != null ? Guard.Value.position : Vector3.zero);
        }

        if (Destination != null) Destination.Value = dest;
        if (IsChasing != null)   IsChasing.Value   = chasing;

        return Status.Success;
    }

    void SwitchMode(Mode m)
    {
        _mode = m;
        _modeNoSwitchBefore = Time.time + Mathf.Max(0f, minModeTime);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
