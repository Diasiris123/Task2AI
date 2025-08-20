using System.Collections.Generic;
using UnityEngine;

public class GridNode
{
    public Vector3 worldPos;
    public int x, y;
    public bool walkable = true;
    public float baseCost = 1f;   // terrain multiplier if you want to vary costs
    public GridNode parent;       // used by path reconstruction
}