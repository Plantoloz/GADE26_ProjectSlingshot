using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Order")]
    [Tooltip("Assign Checkpoint components in the order the player must reach them.")]
    public List<Checkpoint> checkpoints = new List<Checkpoint>();

    [Header("Events")]
    public UnityEvent onAllCheckpointsCompleted;

    public int  CurrentIndex { get; private set; } = -1;
    public bool IsComplete   { get; private set; } = false;

    void Start()
    {
        foreach (var cp in checkpoints)
            if (cp != null) cp.Deactivate();

        AdvanceToNext();
    }

    public void OnCheckpointReached(Checkpoint cp)
    {
        if (IsComplete) return;
        if (CurrentIndex < 0 || CurrentIndex >= checkpoints.Count) return;
        if (checkpoints[CurrentIndex] != cp) return;

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
    }
}
