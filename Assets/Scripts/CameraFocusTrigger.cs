using UnityEngine;

/// <summary>
/// Place on a planet (or a child GameObject) with a Collider set as Trigger.
/// When the ship enters, both CameraFollow and MinimapCamera zoom to frame
/// the ship + planet together. On exit both cameras revert to normal follow.
///
/// focusTarget defaults to this Transform — override it if the collider sits
/// on a child object but the planet root is what should be framed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CameraFocusTrigger : MonoBehaviour
{
    [Tooltip("Transform to frame (planet root). Defaults to this GameObject.")]
    public Transform focusTarget;

    private CameraFollow _cameraFollow;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        if (focusTarget == null)
            focusTarget = transform;

        _cameraFollow = FindFirstObjectByType<CameraFollow>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ShipController>() == null) return;

        _cameraFollow?.RegisterTrigger(focusTarget);
        MinimapCamera.Instance?.RegisterTrigger(focusTarget);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<ShipController>() == null) return;

        _cameraFollow?.UnregisterTrigger(focusTarget);
        MinimapCamera.Instance?.UnregisterTrigger(focusTarget);
    }

    void OnDrawGizmos()
    {
        if (focusTarget != null && focusTarget != transform)
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
            Gizmos.DrawLine(transform.position, focusTarget.position);
            Gizmos.DrawWireSphere(focusTarget.position, 1f);
        }
    }
}