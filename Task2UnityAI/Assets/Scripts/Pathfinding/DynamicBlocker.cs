using UnityEngine;

public class DynamicBlocker : MonoBehaviour {
    public GridGraph graph;
    public RectInt rect;
    public bool blocked = true;

    void OnEnable()  { Apply(); }
    void OnDisable() { graph.SetWalkable(rect, true); }

    public void Toggle(bool isBlocked) {
        blocked = isBlocked; Apply();
    }

    private void Apply() {
        if (graph != null) graph.SetWalkable(rect, !blocked);
    }
}