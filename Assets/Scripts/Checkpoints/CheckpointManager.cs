using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Order")]
    [Tooltip("Assign Checkpoint components in the order the player must reach them.")]
    public List<CheckpointBase> checkpoints = new List<CheckpointBase>();

    [Header("References")]
    public TrajectoryManager trajectoryManager;

    [Header("Visibility")]
    [Tooltip("How many upcoming checkpoints are visible at once.")]
    [SerializeField] private int visibleAhead = 2;

    [Header("Events")]
    public UnityEvent onAllCheckpointsCompleted;

    public int  CurrentIndex { get; private set; } = -1;
    public bool IsComplete   { get; private set; } = false;

    private CheckpointBase lastCompletedCheckpoint;
    private Vector3        lastRespawnVelocity;

    void Start()
    {
        foreach (var cp in checkpoints)
            if (cp != null) cp.Deactivate();

        AdvanceToNext();
    }

    private void SetCheckpointVisible(CheckpointBase cp, bool visible)
    {
        if (cp == null) return;
        foreach (var r in cp.GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    private void UpdateVisibility()
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            bool show = i >= CurrentIndex && i < CurrentIndex + visibleAhead;
            SetCheckpointVisible(checkpoints[i], show);
        }
    }

    public void OnCheckpointReached(CheckpointBase cp)
    {
        if (IsComplete) return;
        if (CurrentIndex < 0 || CurrentIndex >= checkpoints.Count) return;
        if (checkpoints[CurrentIndex] != cp) return;

        lastCompletedCheckpoint = cp;
        if (trajectoryManager != null)
            lastRespawnVelocity = trajectoryManager.GetSimulatedVelocityAtCheckpoint(CurrentIndex);
        cp.Complete();
        AdvanceToNext();
    }

    void AdvanceToNext()
    {
        CurrentIndex++;

        if (CurrentIndex >= checkpoints.Count)
        {
            IsComplete = true;
            Debug.Log("All checkpoints completed!");
            onAllCheckpointsCompleted?.Invoke();
            return;
        }

        checkpoints[CurrentIndex].Activate();
        UpdateVisibility();
    }

    public void RespawnAtLastCheckpoint()
    {
        if (lastCompletedCheckpoint == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        ShipController ship = FindFirstObjectByType<ShipController>();
        if (ship == null) return;

        Rigidbody rb = ship.GetComponent<Rigidbody>();
        ship.transform.position = lastCompletedCheckpoint.RespawnPosition;
        if (rb != null)
        {
            Vector3 vel = lastRespawnVelocity.sqrMagnitude > 0.001f
                ? lastRespawnVelocity
                : lastCompletedCheckpoint.RespawnDirection * ship.startSpeed;
            rb.linearVelocity  = vel;
            rb.angularVelocity = Vector3.zero;
        }

        ship.enabled = true;

        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health != null) health.Respawn();

        EngineOverheat overheat = ship.GetComponent<EngineOverheat>();
        if (overheat != null) overheat.ResetHeat();

        SelfDestruct selfDestruct = ship.GetComponent<SelfDestruct>();
        if (selfDestruct != null) selfDestruct.ResetInput();
    }
}
