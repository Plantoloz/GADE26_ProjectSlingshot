using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(PlayerHealth))]
public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float thrustForce = 20f;
    public float rotationSpeed = 10f; // Radians per second
    public float brakePower = 2f;
    [Range(0, 1)]
    public float thrustAlignmentThreshold = 0.8f; // How aligned we must be to thrust at 100%
    [Tooltip("When enabled, W/A/S/D are relative to the current velocity vector instead of world axes")]
    public bool relativeToVelocity = false;
    
    [Header("Initial Game Settings")]
    public Vector3 initialVelocity = new (0f, 0f, 0f);

    [Header("Proximity Sensor (Casting)")]
    public float shipRadius = 1.5f;   // Radius for immediate vicinity check

    [Header("Thruster")]
    public ParticleSystem thruster;

    public bool IsThrusting { get; private set; }

    private Rigidbody rb;
    private Animator sensorAnimator;
    private Vector2 currentInput;
    private TrajectoryPredictor trajectory;
    private CameraFollow cameraFollow;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sensorAnimator = GetComponent<Animator>();
        trajectory = GetComponent<TrajectoryPredictor>();
        cameraFollow = FindFirstObjectByType<CameraFollow>();
        rb.angularVelocity = Vector3.zero;
        thruster.Play();

        // Configure gravity for the player
        GravityBody gb = GetComponent<GravityBody>();
        if (gb != null)
        {
            gb.isAttractor = false;
            gb.isAttractee = true;
        }
    }

    private void OnEnable()
    {
        thruster.Stop();
        rb.linearVelocity = initialVelocity;
    }

    void OnMove(InputValue value)
    {
        currentInput = value.Get<Vector2>();

        if (thruster == null) return;
        if (currentInput.sqrMagnitude > 0.01f)
            thruster.Play();
        else
            thruster.Stop();
    }

    void FixedUpdate()
    {
        // D. Perform proximity scan (Radius + Path check)
        PerformProximityScan();

        bool isProvidingInput = currentInput.sqrMagnitude > 0.01f;
        IsThrusting = isProvidingInput;

        if (isProvidingInput)
        {
            Vector3 targetDir = GetWorldInputDirection();

            // 1. ROTATION: Rotate towards input direction
            float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

            // Smoothly rotate towards the target
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

            // Instantly kill angular momentum while steering to keep it precise
            rb.angularVelocity = Vector3.zero;

            // 2. THRUST: Only thrust if we are somewhat aligned with our target direction
            float alignment = Vector3.Dot(transform.up, targetDir);

            if (alignment > 0)
            {
                float thrustMultiplier = Mathf.Clamp01((alignment - thrustAlignmentThreshold) / (1f - thrustAlignmentThreshold));
                rb.AddForce(transform.up * thrustForce * thrustMultiplier);
            }
        }
    }

    Vector3 GetWorldInputDirection()
    {
        Vector3 inputDir = new Vector3(currentInput.x, currentInput.y, 0).normalized;

        if (!relativeToVelocity || rb.linearVelocity.sqrMagnitude < 0.01f)
            return inputDir;

        // Rotate input by the velocity direction so W = forward along velocity
        float velocityAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, 0, velocityAngle - 90f) * inputDir;
    }

    void PerformProximityScan()
    {
        bool isImmediateDanger = false;
        bool isPathDanger = (trajectory != null && trajectory.pathCollisionDetected);

        // 1. Check small radius around the ship (Immediate Danger)
        Collider[] nearby = Physics.OverlapSphere(transform.position, shipRadius);
        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && (col.CompareTag("Asteroid") || col.CompareTag("Planet")))
            {
                isImmediateDanger = true;
                break;
            }
        }

        // 2. Visual Feedback
        if (sensorAnimator != null)
        {
            int dangerLevel = 0;
            if (isImmediateDanger)  dangerLevel = 2;
            else if (isPathDanger)  dangerLevel = 1;
            sensorAnimator.SetInteger("DangerLevel", dangerLevel);
            Debug.Log(dangerLevel);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CameraTrigger") && cameraFollow != null)
            cameraFollow.RegisterTrigger(other.transform.parent);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CameraTrigger") && cameraFollow != null)
            cameraFollow.UnregisterTrigger(other.transform.parent);
    }

    // Draw the sensor radius in the Editor for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shipRadius);
    }
}