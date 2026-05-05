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

    [Header("Air Resistance (Acceleration Curve)")]
    public float speedDiminishingStart = 10f; // Speed at which acceleration starts to slow down
    public float accelerationDropOff = 0.1f;   // How quickly the power drops off
    public float linearResidueForce = 2f;      // The "small linear growth" force at high speeds
    public float brakeForceMultiplier = 5f;    // Multiplier for braking against velocity

    [Header("Initial Game Settings")]
    public Vector3 initialVelocity = new (0f, 0f, 0f);

    [Header("Proximity Sensor (Casting)")]
    public float shipRadius = 1.5f;   // Radius for immediate vicinity check

    [Header("Thruster")]
    public ParticleSystem thruster;

    public bool IsThrusting { get; private set; }
    public float LastAppliedThrustForce { get; private set; }

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
        LastAppliedThrustForce = 0f;

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
                float forceMag = CalculateEffectiveThrust(rb.linearVelocity, transform.up, thrustForce * thrustMultiplier);
                
                LastAppliedThrustForce = forceMag;
                rb.AddForce(transform.up * forceMag);
            }
        }
    }

    public float CalculateEffectiveThrust(Vector3 currentVelocity, Vector3 thrustDirection, float baseThrust)
    {
        if (baseThrust <= 0.01f) return 0f;

        float currentSpeed = currentVelocity.magnitude;
        float velocityDotThrust = Vector3.Dot(currentVelocity.normalized, thrustDirection);

        // Case 1: BRAKING (Thrusting opposite to movement)
        if (currentSpeed > 0.1f && velocityDotThrust < -0.1f)
        {
            // Use high power to slow down fast
            return baseThrust * brakeForceMultiplier;
        }

        // Case 2: ACCELERATING (or thrusting sideways)
        // We calculate diminishing returns based on speed in the thrust direction
        float relevantSpeed = Mathf.Max(0, currentSpeed * velocityDotThrust);
        
        // Diminishing returns curve: F = (Base - Residue) / (1 + k * speed^2) + Residue
        // This ensures it never hits zero, maintaining that "small linear growth".
        float factor = 1f / (1f + accelerationDropOff * Mathf.Pow(Mathf.Max(0, relevantSpeed - speedDiminishingStart), 2f));
        float effectiveForce = ((baseThrust - linearResidueForce) * factor) + linearResidueForce;

        return Mathf.Max(linearResidueForce, effectiveForce);
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