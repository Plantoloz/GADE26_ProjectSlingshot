using UnityEngine;

/// Attached to a Camera that is a child of the Ship.
/// Follows the ship's heading (Z-rotation) but strips the banking roll (X/Y) so the view stays stable.
public class ShipChildCamera : MonoBehaviour
{
    public float rotationSmoothSpeed = 8f;

    private Transform ship;
    private Vector3 smoothVelocity;
    private Quaternion smoothRotation;

    void Awake()
    {
        ship = transform.parent;
        smoothRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (ship == null) return;

        // Strip banking: keep only the Z heading rotation, discard X/Y tilt
        Quaternion headingOnly = Quaternion.Euler(0f, 0f, ship.eulerAngles.z);

        Vector3 toShip = ship.position - transform.position;
        if (toShip.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toShip, headingOnly * Vector3.up);
            smoothRotation = Quaternion.Slerp(smoothRotation, targetRot, rotationSmoothSpeed * Time.deltaTime);
            transform.rotation = smoothRotation;
        }
    }
}