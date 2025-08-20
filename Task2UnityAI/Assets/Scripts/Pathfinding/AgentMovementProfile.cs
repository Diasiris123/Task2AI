using UnityEngine;

[CreateAssetMenu(menuName="AI/Movement Profile")]
public class AgentMovementProfile : ScriptableObject {
    [Header("Movement Rules")]
    public bool allowDiagonal = true;
    [Range(0f, 5f)] public float turnPenalty = 0.5f;    
    [Range(0f, 2f)] public float continueBonus = 0.2f; 
    [Range(1f, 3f)] public float diagonalCost = 1.414f;  

    [Header("Follower")]
    public float maxSpeed = 4f;
    public float acceleration = 12f;
    public float turnDelay = 0.05f; 
}

