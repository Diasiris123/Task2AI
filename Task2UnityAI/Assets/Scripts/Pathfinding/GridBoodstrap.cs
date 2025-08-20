using UnityEngine;
public class GridBootstrap : MonoBehaviour {
    public GridGraph graph;
    void Awake(){ if(!graph) graph = GetComponent<GridGraph>(); graph.Build(); }
}