using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityBody))]
public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float thrustForce = 20f;
    public float rotationSpeed = 10f; // Radians per second
    public float brakePower = 2f;
    [Range(0, 1)]
    public float thrustAlignmentThreshold = 0.8f; // How aligned we must be to thrust at 100%
    
    private Rigidbody rb;
    private Vector2 currentInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularVelocity = Vector3.zero;

        // Configure gravity for the player
        GravityBody gb = GetComponent<GravityBody>();
        if (gb != null)
        {
            gb.isAttractor = false;
            gb.isAttractee = true;
        }
    }

    void OnMove(InputValue value)
    {
        currentInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        bool isProvidingInput = currentInput.sqrMagnitude > 0.01f;

        if (isProvidingInput)
        {
            // 1. ROTATION: Rotate towards input direction
            float targetAngle = Mathf.Atan2(currentInput.y, currentInput.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            
            // Smoothly rotate towards the target
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            
            // Instantly kill angular momentum while steering to keep it precise
            rb.angularVelocity = Vector3.zero;

            // 2. THRUST: Only thrust if we are somewhat aligned with our target direction
            Vector3 targetDir = new Vector3(currentInput.x, currentInput.y, 0).normalized;
            float alignment = Vector3.Dot(transform.up, targetDir);

            if (alignment > 0)
            {
                float thrustMultiplier = Mathf.Clamp01((alignment - thrustAlignmentThreshold) / (1f - thrustAlignmentThreshold));
                rb.AddForce(transform.up * thrustForce * thrustMultiplier);
            }
        }
        // No else block with braking - we want to drift like in Outer Wilds!
    }
}