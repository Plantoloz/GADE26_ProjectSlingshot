using System.Collections.Generic;
using UnityEngine;

public class CheckpointSpawner : MonoBehaviour
{
    [Header("Simulation")]
    public Vector3 startDirection   = Vector3.right;
    public float   startSpeed       = 10f;
    public float   minSpeed         = 0f;
    public float   maxSpeed         = 1000;
    public int     predictionSteps  = 600;
    public float   stepTime         = 0.1f;

    public Vector3 StartVelocity => startDirection.normalized * startSpeed;

    [Header("Checkpoint Placement")]
    public GameObject checkpointPrefab;
    public int        checkpointCount  = 12;
    public Transform  checkpointParent;

    [Header("References")]
    public Transform         ship;
    public CheckpointManager checkpointManager;
    public MapPath           mapPath;
    public LineRenderer      pathLine;

    public Vector3 StartPosition => ship != null ? ship.position : transform.position;

    // Baked path — serialized so it survives domain reloads
    [HideInInspector] public List<Vector3> bakedPath = new List<Vector3>();
}