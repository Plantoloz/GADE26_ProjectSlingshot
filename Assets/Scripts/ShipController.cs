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
    public float strafeDegreesPerSecond = 60f;
    public float rotationSpeed = 10f;

    [Header("Air Resistance (Acceleration Curve)")]
    [Tooltip("Speed at which acceleration starts to slow down")]
    public float speedDiminishingStart = 10f;
    [Tooltip("How quickly the power drops off")]
    public float accelerationDropOff = 0.1f;
    [Tooltip("The small linear growth force at high speeds")]
    public float linearResidueForce = 2f;
    [Tooltip("Multiplier for braking against velocity")]
    public float brakeForceMultiplier = 5f;

    [Header("Initial Game Settings")]
    public Vector3 initialVelocity = new(0f, 0f, 0f);

    [Header("Proximity Sensor (Casting)")]
    public float shipRadius = 1.5f;   // Radius for immediate vicinity check

    [Header("Thruster")]
    public ParticleSystem thruster;
    public AudioSource thrusterAudio;
    public float thrusterAudioFadeSpeed = 5f;

    public bool IsThrusting { get; private set; }
    public float LastAppliedThrustForce { get; private set; }

    [Header("References")]
    [Tooltip("Assign the TrajectoryPredictor from its dedicated GameObject.")]
    public TrajectoryPredictor trajectoryPredictor;

    private Rigidbody rb;
    private Animator sensorAnimator;
    private TrajectoryPredictor trajectory;
    private float strafeInput;
    private float thrustInput;
    private float targetThrusterVolume;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sensorAnimator = GetComponent<Animator>();
        trajectory = trajectoryPredictor != null ? trajectoryPredictor : GetComponent<TrajectoryPredictor>();
        rb.angularVelocity = Vector3.zero;

        if (thruster != null) thruster.Play();
        if (thrusterAudio != null)
        {
            thrusterAudio.loop = true;
            thrusterAudio.volume = 0f;
            thrusterAudio.Play();
        }

        // Configure gravity for the player
        GravityBody gb = GetComponent<GravityBody>();
        if (gb != null)
        {
            gb.isAttractor = false;
            gb.isAttractee = true;
        }
    }

    private void Start()
    {
        rb.linearVelocity = initialVelocity;
    }

    void OnStrafe(InputValue value)
    {
        strafeInput = value.Get<float>();
        //UpdateThrusterFeedback();
    }

    void OnThrust(InputValue value)
    {
        thrustInput = value.Get<float>();
        UpdateThrusterFeedback();
    }

    void UpdateThrusterFeedback()
    {
        bool thrusting = Mathf.Abs(thrustInput) > 0.05f;
        if (thruster != null)
        {
            if (thrusting) thruster.Play();
            else thruster.Stop();
        }
        targetThrusterVolume = thrusting ? 1f : 0f;
    }

    void FixedUpdate()
    {
        if (thrusterAudio != null)
            thrusterAudio.volume = Mathf.MoveTowards(thrusterAudio.volume, targetThrusterVolume, thrusterAudioFadeSpeed * Time.fixedDeltaTime);

        PerformProximityScan();

        IsThrusting = /*Mathf.Abs(strafeInput) > 0.01f ||*/ Mathf.Abs(thrustInput) > 0.01f;
        LastAppliedThrustForce = 0f;

        // Rotate toward next predicted trajectory point
        if (trajectory != null)
        {
            Vector3 toNext = trajectory.NextPredictedPoint - transform.position;
            if (toNext.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(toNext.y, toNext.x) * Mathf.Rad2Deg - 90f;
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (rb.linearVelocity.sqrMagnitude < 0.01f) return;

        Vector3 velDir = rb.linearVelocity.normalized;

        if (Mathf.Abs(strafeInput) > 0.05f)
        {
            float angle = -strafeInput * strafeDegreesPerSecond * Time.fixedDeltaTime;
            rb.linearVelocity = Quaternion.Euler(0f, 0f, angle) * rb.linearVelocity;
        }

        if (Mathf.Abs(thrustInput) > 0.05f)
        {
            // Along velocity (positive = accelerate, negative = brake)
            Vector3 thrustDir = velDir * thrustInput;
            float forceMag = CalculateEffectiveThrust(rb.linearVelocity, thrustDir.normalized, thrustForce * Mathf.Abs(thrustInput));
            rb.AddForce(thrustDir.normalized * forceMag);
            LastAppliedThrustForce += forceMag;
        }
    }

    public float CalculateEffectiveThrust(Vector3 currentVelocity, Vector3 thrustDirection, float baseThrust)
    {
        if (baseThrust <= 0.01f) return 0f;

        float currentSpeed = currentVelocity.magnitude;
        float velocityDotThrust = Vector3.Dot(currentVelocity.normalized, thrustDirection);

        if (currentSpeed > 0.1f && velocityDotThrust < -0.1f)
            return baseThrust * brakeForceMultiplier;

        float relevantSpeed = Mathf.Max(0f, currentSpeed * velocityDotThrust);
        float factor = 1f / (1f + accelerationDropOff * Mathf.Pow(Mathf.Max(0f, relevantSpeed - speedDiminishingStart), 2f));
        float effectiveForce = (baseThrust - linearResidueForce) * factor + linearResidueForce;

        return Mathf.Max(linearResidueForce, effectiveForce);
    }

    void PerformProximityScan()
    {
        bool isImmediateDanger = false;
        bool isPathDanger = trajectory != null && trajectory.pathCollisionDetected;

        Collider[] nearby = Physics.OverlapSphere(transform.position, shipRadius);
        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && (col.CompareTag("Asteroid") || col.CompareTag("Planet")))
            {
                isImmediateDanger = true;
                break;
            }
        }

        if (sensorAnimator != null)
        {
            int dangerLevel = isImmediateDanger ? 2 : isPathDanger ? 1 : 0;
            sensorAnimator.SetInteger("DangerLevel", dangerLevel);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shipRadius);

        if (initialVelocity.sqrMagnitude > 0.001f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, initialVelocity);
            Gizmos.DrawWireSphere(transform.position + initialVelocity, 0.3f);
        }
    }
}