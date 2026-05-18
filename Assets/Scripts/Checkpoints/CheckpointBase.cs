using UnityEngine;

public abstract class CheckpointBase : MonoBehaviour
{
    public enum CheckpointState { Inactive, Active, Completed }

    public CheckpointState CurrentState { get; protected set; } = CheckpointState.Inactive;

    public abstract void Activate();
    public abstract void Deactivate();
    public abstract void Complete();
}