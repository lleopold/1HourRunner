using UnityEngine;
using UnityEditor;

public class ZombieDebugLabel : MonoBehaviour
{
    void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        style.fontSize = 12;

        Handles.Label(transform.position + Vector3.up * 2, name, style);
    }
}