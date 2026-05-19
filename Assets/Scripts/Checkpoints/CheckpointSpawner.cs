using System.Collections.Generic;
using UnityEngine;

public class CheckpointSpawner : MonoBehaviour
{
    [Header("Checkpoint Placement")]
    public GameObject checkpointPrefab;
    public int        checkpointCount  = 12;
    public Transform  checkpointParent;

    [Header("References")]
    public Transform         ship;
    public CheckpointManager checkpointManager;
    public MapPath           mapPath;

    public Vector3 StartPosition => ship != null ? ship.position : transform.position;

    // Baked path — serialized so it survives domain reloads
    [HideInInspector] public List<Vector3> bakedPath = new List<Vector3>();
}