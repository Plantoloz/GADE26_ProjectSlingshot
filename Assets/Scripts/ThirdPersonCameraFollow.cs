using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    public Transform target;
    
    [Header("Position")]
    public float radius = 5f;
    public float height = 8f;
    public float smoothTime = 0.2f;

    [Header("Lookahead")]
    [Tooltip("How far ahead of the ship the camera looks (world units along velocity)")]
    public float lookaheadDistance = 5f;
    [Tooltip("How slowly the lookahead offset follows direction changes")]
    public float lookSmoothTime = 0.3f;

    [Header("Roll Damping")]
    [Tooltip("Larger = less camera roll during turns")]
    public float rollSmoothTime = 0.5f;

    private Rigidbody _rb;
    private Vector3   _posVelocity;
    private Vector3   _smoothedLookahead;
    private Vector3   _lookaheadVelocity;
    private Vector3   _smoothUp;
    private Vector3   _smoothUpVelocity;

    Vector3 ShipFacing()
    {
        Vector3 f = new Vector3(target.up.x, target.up.y, 0f);
        return f.sqrMagnitude < 0.001f ? Vector3.up : f.normalized;
    }

    void Start()
    {
        if (target == null) return;
        _rb = target.GetComponent<Rigidbody>();

        Vector3 facing = ShipFacing();
        _smoothUp = facing;

        // Snap camera to correct position immediately so forward vector is valid from frame 1
        transform.position = target.position - facing * radius + new Vector3(0f, 0f, -height);

        Vector3 toShip = target.position - transform.position;
        if (toShip.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toShip, facing);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 shipFacing = ShipFacing();

        // ── Position ──────────────────────────────────────────────────────────
        Vector3 targetPos = target.position - shipFacing * radius + new Vector3(0f, 0f, -height);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _posVelocity, smoothTime);

        // ── Lookahead offset (velocity-based, XY only) ────────────────────────
        // Adds an offset to the look target so the ship appears lower in frame,
        // revealing more space ahead. Never changes the forward-to-up angle enough
        // to destabilise LookRotation because camera height (Z) always dominates.
        Vector3 velDir = Vector3.zero;
        if (_rb != null)
        {
            Vector2 v2 = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y);
            if (v2.sqrMagnitude > 0.01f)
                velDir = new Vector3(v2.normalized.x, v2.normalized.y, 0f);
        }

        _smoothedLookahead = Vector3.SmoothDamp(
            _smoothedLookahead, velDir * lookaheadDistance, ref _lookaheadVelocity, lookSmoothTime);

        Vector3 lookTarget = target.position + _smoothedLookahead;

        // ── Roll: up vector with its own slower smooth time ───────────────────
        _smoothUp = Vector3.SmoothDamp(_smoothUp, shipFacing, ref _smoothUpVelocity, rollSmoothTime);
        if (_smoothUp.sqrMagnitude < 0.001f) _smoothUp = Vector3.up;
        else _smoothUp.Normalize();

        // ── Rotation ──────────────────────────────────────────────────────────
        Vector3 forward = lookTarget - transform.position;
        if (forward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(forward, _smoothUp);
    }
}