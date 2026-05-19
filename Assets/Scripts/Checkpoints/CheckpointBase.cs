using UnityEngine;

public abstract class CheckpointBase : MonoBehaviour
{
    public enum CheckpointState { Inactive, Active, Completed }

    public CheckpointState CurrentState { get; protected set; } = CheckpointState.Inactive;

    [Header("Respawn")]
    [Tooltip("Distance behind the checkpoint ring to spawn the ship")]
    public float respawnOffset = 5f;

    // Spawn the ship slightly before the ring, flying through it in transform.up direction
    public Vector3 RespawnPosition => transform.position - transform.up * respawnOffset;
    public Vector3 RespawnDirection => transform.up;

    public abstract void Activate();
    public abstract void Deactivate();
    public abstract void Complete();
}