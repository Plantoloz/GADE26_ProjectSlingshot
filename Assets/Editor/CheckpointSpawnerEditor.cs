using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ── Static watcher — runs regardless of selection ─────────────────────────────

[InitializeOnLoad]
static class CheckpointSpawnerWatcher
{
    static readonly Dictionary<GravityBody, Vector3> _cache = new();
    static CheckpointSpawner _spawner;

    static CheckpointSpawnerWatcher()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sv)
    {
        if (_spawner == null)
            _spawner = Object.FindAnyObjectByType<CheckpointSpawner>();
        if (_spawner == null) return;

        if (HaveBodyPositionsChanged())
        {
            CacheBodyPositions();
            CheckpointSpawnerEditor.BakePath(_spawner);
            CheckpointSpawnerEditor.UpdateLineRenderer(_spawner);
            EditorUtility.SetDirty(_spawner);
            sv.Repaint();
        }

        CheckpointSpawnerEditor.DrawGizmos(_spawner);
    }

    static bool HaveBodyPositionsChanged()
    {
        var bodies = Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        if (bodies.Length != _cache.Count) return true;
        foreach (var body in bodies)
        {
            if (body == null) continue;
            if (!_cache.TryGetValue(body, out Vector3 cached) || cached != body.transform.position)
                return true;
        }
        return false;
    }

    internal static void CacheBodyPositions()
    {
        _cache.Clear();
        foreach (var body in Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None))
            if (body != null) _cache[body] = body.transform.position;
    }

    internal static void InvalidateSpawner() => _spawner = null;
}

// ── Custom Inspector ──────────────────────────────────────────────────────────

[CustomEditor(typeof(CheckpointSpawner))]
public class CheckpointSpawnerEditor : Editor
{
    void OnEnable() => CheckpointSpawnerWatcher.InvalidateSpawner();

    public override void OnInspectorGUI()
    {
        var s = (CheckpointSpawner)target;
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject,
            "startDirection", "startSpeed", "minSpeed", "maxSpeed",
            "predictionSteps", "stepTime", "bakedPath");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        s.startDirection = EditorGUILayout.Vector3Field("Direction", s.startDirection);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Speed", GUILayout.Width(EditorGUIUtility.labelWidth));
        s.startSpeed = EditorGUILayout.Slider(s.startSpeed, s.minSpeed, s.maxSpeed);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        s.minSpeed = EditorGUILayout.FloatField("Min", s.minSpeed);
        s.maxSpeed = EditorGUILayout.FloatField("Max", s.maxSpeed);
        EditorGUILayout.EndHorizontal();

        s.predictionSteps = EditorGUILayout.IntField("Prediction Steps", s.predictionSteps);
        s.stepTime        = EditorGUILayout.FloatField("Step Time", s.stepTime);

        if (EditorGUI.EndChangeCheck())
        {
            BakePath(s);
            UpdateLineRenderer(s);
            CheckpointSpawnerWatcher.CacheBodyPositions();
            EditorUtility.SetDirty(s);
            SceneView.RepaintAll();
        }

        GUI.enabled = false;
        EditorGUILayout.Vector3Field("  → Velocity", s.StartVelocity);
        GUI.enabled = true;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Checkpoint Generation", EditorStyles.boldLabel);

        bool hasPath = s.bakedPath != null && s.bakedPath.Count > 1;
        GUI.enabled = hasPath;
        if (GUILayout.Button("Generate Checkpoints"))
        {
            if (s.checkpointPrefab == null)
                EditorUtility.DisplayDialog("Missing Prefab", "Assign a Checkpoint Prefab first.", "OK");
            else
            {
                GenerateCheckpoints(s);
                EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
            }
        }
        GUI.enabled = true;

        if (hasPath)
            EditorGUILayout.HelpBox(
                $"{s.bakedPath.Count} steps  |  vel: {s.StartVelocity:F1}", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    // ── Simulation (internal static — called by watcher and inspector) ─────────

    internal static void BakePath(CheckpointSpawner s)
    {
        var sceneBodies = Object.FindObjectsByType<GravityBody>(FindObjectsSortMode.None);

        Vector3 pos = s.StartPosition;
        Vector3 vel = s.StartVelocity;
        float   dt  = s.stepTime;
        var points  = new List<Vector3>(s.predictionSteps);

        for (int i = 0; i < s.predictionSteps; i++)
        {
            if (!float.IsFinite(pos.x) || !float.IsFinite(pos.y)) break;
            points.Add(pos);

            Vector3 gravAccel = Vector3.zero;
            foreach (var body in sceneBodies)
            {
                if (body == null || !body.isAttractor) continue;
                Rigidbody bodyRb = body.GetComponent<Rigidbody>();
                if (bodyRb == null) continue;

                Vector3 bodyPos = Application.isPlaying ? bodyRb.position : body.transform.position;
                Vector3 dir     = bodyPos - pos;
                float   dist    = dir.magnitude;

                if (dist > 0.01f && dist < body.influenceRadius)
                {
                    float mag = GravityBody.GRAVITY_CONSTANT * bodyRb.mass
                                / Mathf.Pow(Mathf.Max(dist, GravityBody.minForceDistance), 2f);
                    gravAccel += dir.normalized * mag;
                }
            }

            vel += gravAccel * dt;
            pos += vel * dt;
        }

        s.bakedPath = points;

        if (s.mapPath != null)
        {
            Undo.RecordObject(s.mapPath, "Update MapPath");
            s.mapPath.points = new List<Vector3>(points);
            EditorUtility.SetDirty(s.mapPath);
        }
    }

    // Live update — no Undo to avoid flooding the undo stack
    internal static void UpdateLineRenderer(CheckpointSpawner s)
    {
        if (s.pathLine == null || s.bakedPath == null || s.bakedPath.Count < 2) return;
        s.pathLine.positionCount = s.bakedPath.Count;
        s.pathLine.SetPositions(s.bakedPath.ToArray());
        EditorUtility.SetDirty(s.pathLine);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    internal static void DrawGizmos(CheckpointSpawner s)
    {
        if (s.bakedPath == null || s.bakedPath.Count < 2) return;

        Handles.color = new Color(1f, 0.85f, 0f, 0.6f);
        Handles.DrawPolyLine(s.bakedPath.ToArray());

        float startSize = HandleUtility.GetHandleSize(s.bakedPath[0]) * 0.15f;
        Handles.color = Color.green;
        Handles.SphereHandleCap(0, s.bakedPath[0], Quaternion.identity, startSize, EventType.Repaint);

        float endSize = HandleUtility.GetHandleSize(s.bakedPath[^1]) * 0.15f;
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, s.bakedPath[^1], Quaternion.identity, endSize, EventType.Repaint);

        if (s.checkpointCount > 0)
        {
            float totalLength = 0f;
            var   arcLengths  = new float[s.bakedPath.Count];
            for (int i = 1; i < s.bakedPath.Count; i++)
            {
                totalLength   += Vector3.Distance(s.bakedPath[i], s.bakedPath[i - 1]);
                arcLengths[i]  = totalLength;
            }

            int   n       = Mathf.Max(1, s.checkpointCount);
            float spacing = totalLength / n;
            Handles.color = new Color(0.3f, 0.9f, 1f, 0.9f);

            for (int c = 0; c < n; c++)
            {
                Vector3 pos = SamplePath(s.bakedPath, arcLengths, totalLength, spacing * (c + 0.5f), out Vector3 tangent);
                float   sz  = HandleUtility.GetHandleSize(pos) * 0.1f;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, sz, EventType.Repaint);
                Handles.DrawLine(pos, pos + tangent * sz * 3f);
            }
        }
    }

    // ── Checkpoint generation ─────────────────────────────────────────────────

    private void GenerateCheckpoints(CheckpointSpawner s)
    {
        Transform parent = s.checkpointParent != null ? s.checkpointParent : s.transform;
        var toDelete = new List<GameObject>();
        foreach (Transform child in parent)
            if (child.GetComponent<CheckpointBase>() != null)
                toDelete.Add(child.gameObject);
        foreach (var go in toDelete)
            Undo.DestroyObjectImmediate(go);

        var   path        = s.bakedPath;
        float totalLength = 0f;
        var   arcLengths  = new float[path.Count];
        for (int i = 1; i < path.Count; i++)
        {
            totalLength   += Vector3.Distance(path[i], path[i - 1]);
            arcLengths[i]  = totalLength;
        }

        int   n       = Mathf.Max(1, s.checkpointCount);
        float spacing = totalLength / n;
        var   generated = new List<CheckpointBase>();

        for (int c = 0; c < n; c++)
        {
            Vector3    cpPos = SamplePath(path, arcLengths, totalLength, spacing * (c + 0.5f), out Vector3 tangent);
            Quaternion rot   = tangent != Vector3.zero
                ? Quaternion.LookRotation(Vector3.Cross(tangent, Vector3.back), tangent)
                : Quaternion.identity;

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(s.checkpointPrefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Generate Checkpoint");
            go.name = $"Checkpoint_{c:00}";
            go.transform.SetPositionAndRotation(cpPos, rot);

            var cp = go.GetComponent<CheckpointBase>();
            if (cp != null) generated.Add(cp);
        }

        if (s.checkpointManager != null)
        {
            Undo.RecordObject(s.checkpointManager, "Update CheckpointManager");
            s.checkpointManager.checkpoints = generated;
            EditorUtility.SetDirty(s.checkpointManager);
        }

        Debug.Log($"[CheckpointSpawner] Generated {generated.Count} checkpoints.");
    }

    private static Vector3 SamplePath(List<Vector3> path, float[] arcLengths, float totalLength, float targetDist, out Vector3 tangent)
    {
        targetDist = Mathf.Clamp(targetDist, 0f, totalLength);

        for (int i = 1; i < path.Count; i++)
        {
            if (arcLengths[i] >= targetDist)
            {
                float segLen = arcLengths[i] - arcLengths[i - 1];
                float t      = segLen > 0f ? (targetDist - arcLengths[i - 1]) / segLen : 0f;
                tangent      = (path[i] - path[i - 1]).normalized;
                return Vector3.Lerp(path[i - 1], path[i], t);
            }
        }

        tangent = path.Count > 1 ? (path[^1] - path[^2]).normalized : Vector3.right;
        return path[^1];
    }
}