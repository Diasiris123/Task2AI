using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "SetAlgorithm",
    story: "Writes PathAlgo from IsChasing. 0=A*, 1=Greedy.",
    category: "Action/Decision",
    id: "set_algo_clean_v1")]
public partial class SetAlgorithmAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> IsChasing; // READ
    [SerializeReference] public BlackboardVariable<int>  PathAlgo;  // WRITE

    public int calmAlgo = 0;   // 0=A*
    public int chaseAlgo = 1;  // 1=Greedy

    protected override Status OnUpdate()
    {
        bool chasing = IsChasing != null && IsChasing.Value;
        if (PathAlgo != null) PathAlgo.Value = chasing ? chaseAlgo : calmAlgo;
        return Status.Success;
    }
}