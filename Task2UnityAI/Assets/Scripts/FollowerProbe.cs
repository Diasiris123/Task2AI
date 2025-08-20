using System;
using System.Reflection;
using UnityEngine;

public class FollowerProbe : MonoBehaviour
{
    public PathFollower follower;
    public CharacterController cc;
    public bool show = true;

    void Reset()
    {
        follower = GetComponent<PathFollower>();
        cc = GetComponent<CharacterController>();
    }

    void OnGUI()
    {
        if (!show || follower == null) return;

        // reflect private "velocity" field from PathFollower
        Vector3 vel = Vector3.zero;
        try
        {
            FieldInfo fi = follower.GetType().GetField("velocity",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null)
            {
                object o = fi.GetValue(follower);
                if (o is Vector3 v) vel = v;
            }
        }
        catch { /* ignore */ }

        var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.UpperLeft };
        var rect  = new Rect(15, 15, 460, 140);

        string text = "Follower probe\n";
        text += $"- Profile assigned: {(follower.profile ? "YES" : "NO")}\n";
        text += $"- Mover: {follower.mover}  SimpleMove: {follower.useSimpleMoveForDebug}\n";
        text += $"- Waypoint tolerance: {follower.waypointTolerance:F2}\n";
        text += $"- Velocity: {vel}\n";
        if (cc != null)
            text += $"- CC radius: {cc.radius:F2}, step: {cc.stepOffset:F2}, slope: {cc.slopeLimit:F0}\n";

        GUI.Box(rect, text, style);
    }
}