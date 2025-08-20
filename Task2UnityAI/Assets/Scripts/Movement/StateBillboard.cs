using UnityEngine;

public class StateBillboard : MonoBehaviour {
    public string stateText;
    void OnGUI() {
        var p = Camera.main.WorldToScreenPoint(transform.position + Vector3.up*2f);
        if (p.z < 0) return;
        var label = new Rect(p.x-50, Screen.height - p.y - 15, 100, 20);
        GUI.Label(label, stateText);
    }
    public void Set(string s) => stateText = s;
}

