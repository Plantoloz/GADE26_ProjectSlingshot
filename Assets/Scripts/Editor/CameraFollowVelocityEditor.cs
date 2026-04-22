using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraFollowVelocity))]
public class CameraFollowVelocityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        CameraFollowVelocity script = (CameraFollowVelocity)target;

        GUI.enabled = script.player != null;

        if (GUILayout.Button("Kamera ausrichten", GUILayout.Height(30)))
        {
            Undo.RecordObject(script.transform, "Kamera ausrichten");
            script.AlignToPlayer();
            EditorUtility.SetDirty(script.transform);
        }

        if (script.player == null)
            EditorGUILayout.HelpBox("Kein Player zugewiesen.", MessageType.Warning);

        GUI.enabled = true;
    }
}
