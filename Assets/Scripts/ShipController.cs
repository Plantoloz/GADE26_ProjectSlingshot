using System.Collections.Generic;
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

    [Header("Proximity Sensor (Casting)")]
    public float shipRadius = 1.5f;   // Radius for immediate vicinity check
    public Color sensorSafeColor = Color.green;
    public Color sensorDangerColor = Color.red;
    public Light sensorLight; 
    public float blinkSpeed = 15f;
    
    private Rigidbody rb;
    private Vector2 currentInput;
    private TrajectoryPredictor trajectory;
    private CameraFollow cameraFollow;
    private readonly HashSet<Transform> overlappingPlanets = new HashSet<Transform>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trajectory = GetComponent<TrajectoryPredictor>();
        cameraFollow = FindFirstObjectByType<CameraFollow>();
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
        // D. Perform proximity scan (Radius + Path check)
        PerformProximityScan();

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
    }

    void PerformProximityScan()
    {
        bool isImmediateDanger = false;
        bool isPathDanger = (trajectory != null && trajectory.pathCollisionDetected);

        // 1. Check small radius around the ship (Immediate Danger)
        Collider[] nearby = Physics.OverlapSphere(transform.position, shipRadius);
        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && col.CompareTag("Asteroid"))
            {
                isImmediateDanger = true;
                break;
            }
        }

        // 2. Visual Feedback
        if (sensorLight != null)
        {
            if (isImmediateDanger || isPathDanger)
            {
                // Blink Red (Urgent)
                float speed = isImmediateDanger ? blinkSpeed * 2f : blinkSpeed; // Double speed if it's right next to us
                float blink = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
                sensorLight.color = Color.Lerp(Color.black, sensorDangerColor, blink);
            }
            else
            {
                // Solid Green (Clear)
                sensorLight.color = sensorSafeColor;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Planet"))
            overlappingPlanets.Add(other.transform.parent);
        UpdateCameraFocus();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Planet"))
            overlappingPlanets.Remove(other.transform.parent);
        UpdateCameraFocus();
    }

    void UpdateCameraFocus()
    {
        if (cameraFollow == null) return;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var planet in overlappingPlanets)
        {
            float dist = Vector3.Distance(transform.position, planet.position);
            if (dist < minDist) { minDist = dist; nearest = planet; }
        }

        if (nearest != null) cameraFollow.SetFocusPlanet(nearest);
        else                  cameraFollow.ClearFocusPlanet();
    }

    // Draw the sensor radius in the Editor for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shipRadius);
    }
}