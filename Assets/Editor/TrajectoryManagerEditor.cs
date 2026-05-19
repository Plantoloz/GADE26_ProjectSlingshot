using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrajectoryManager))]
public class TrajectoryManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var tm = (TrajectoryManager)target;
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        if (changed)
        {
            var spawner = tm.spawner;
            if (spawner != null)
            {
                CheckpointSpawnerEditor.BakePath(spawner);
                CheckpointSpawnerEditor.UpdateLineRenderer(spawner);
                CheckpointSpawnerWatcher.CacheBodyPositions();
                CheckpointSpawnerWatcher.CacheShipVelocity();
                EditorUtility.SetDirty(spawner);
                SceneView.RepaintAll();
            }
            else
            {
                tm.RefreshStaticLine();
                tm.RefreshMinimapLine();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}