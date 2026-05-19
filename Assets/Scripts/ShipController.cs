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
    public float strafeForce = 15f;
    public float rotationSpeed = 10f;

    [Header("Input Curve")]
    [Tooltip("Exponent for analog stick response (1 = linear, 2 = quadratic, …)")]
    [Min(1f)] public float inputExponent = 2f;

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
    public Vector3 startDirection = Vector3.right;
    [Min(0f)] public float startSpeed = 0f;
    public Vector3 StartVelocity => startDirection.normalized * startSpeed;

    [Header("Proximity Sensor (Casting)")]
    public float shipRadius = 1.5f;   // Radius for immediate vicinity check

    [Header("Thruster")]
    public ParticleSystem thruster;
    public AudioSource thrusterAudio;
    public float thrusterAudioFadeSpeed = 5f;
    public string thrusterSoundName = "Thruster";

    public bool IsThrusting { get; private set; }
    public float LastAppliedThrustForce { get; private set; }

    [Header("Banking")]
    [Tooltip("How strongly the ship tilts into a turn")]
    public float bankingMultiplier = 0.5f;
    [Tooltip("Maximum tilt angle in degrees")]
    public float maxBankAngle = 30f;
    [Tooltip("How quickly the ship tilts in and out of turns")]
    public float bankingSmoothSpeed = 5f;

    [Header("References")]
    [Tooltip("Assign the TrajectoryPredictor from its dedicated GameObject.")]
    public TrajectoryPredictor trajectoryPredictor;

    private Rigidbody rb;
    private Animator sensorAnimator;
    private TrajectoryPredictor trajectory;
    private float strafeInput;
    private float thrustInput;
    private float targetThrusterVolume;
    private Vector3 prevVelocity;
    private float currentBankAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sensorAnimator = GetComponent<Animator>();
        trajectory = trajectoryPredictor != null ? trajectoryPredictor : GetComponent<TrajectoryPredictor>();
        rb.angularVelocity = Vector3.zero;

        if (thruster != null) thruster.Stop();
        if (thrusterAudio != null)
        {
            thrusterAudio.loop = true;
            thrusterAudio.volume = 0f;
            thrusterAudio.spatialBlend = 0f;
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
        rb.linearVelocity = StartVelocity;
        prevVelocity = StartVelocity;
    }

    float ApplyInputCurve(float v) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), inputExponent);

    void OnStrafe(InputValue value)
    {
        strafeInput = ApplyInputCurve(value.Get<float>());
    }

    void OnThrust(InputValue value)
    {
        thrustInput = ApplyInputCurve(value.Get<float>());
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
        {
            float sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : 1f;
            float clipMultiplier = AudioManager.Instance != null ? AudioManager.Instance.GetSoundVolume(thrusterSoundName) : 1f;
            float currentTarget = targetThrusterVolume * sfxVolume * clipMultiplier;
            
            thrusterAudio.volume = Mathf.MoveTowards(thrusterAudio.volume, currentTarget, thrusterAudioFadeSpeed * Time.fixedDeltaTime);
        }

        PerformProximityScan();

        IsThrusting = /*Mathf.Abs(strafeInput) > 0.01f ||*/ Mathf.Abs(thrustInput) > 0.01f;
        LastAppliedThrustForce = 0f;

        // Centripetal acceleration = change in velocity direction (perpendicular to velocity)
        Vector3 accel = (rb.linearVelocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = rb.linearVelocity;

        // Rotate toward next predicted trajectory point
        if (trajectory != null)
        {
            Vector3 toNext = trajectory.NextPredictedPoint - transform.position;
            if (toNext.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(toNext.y, toNext.x) * Mathf.Rad2Deg - 90f;

                // Bank: project centripetal accel onto the ship's right axis (perpendicular to velocity in XY)
                Vector3 velDir2D = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : Vector3.up;
                Vector3 shipRight = new Vector3(velDir2D.y, -velDir2D.x, 0f);
                float centripetal = Vector3.Dot(accel, shipRight);
                float targetBank = Mathf.Clamp(-centripetal * bankingMultiplier, -maxBankAngle, maxBankAngle);
                currentBankAngle = Mathf.Lerp(currentBankAngle, targetBank, bankingSmoothSpeed * Time.fixedDeltaTime);

                // Roll around the nose axis (velocity direction = local +Y), not the wing axis
                Quaternion headingRot = Quaternion.Euler(0f, 0f, targetAngle);
                Quaternion bankRot = Quaternion.AngleAxis(currentBankAngle, velDir2D);
                Quaternion targetRotation = bankRot * headingRot;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (rb.linearVelocity.sqrMagnitude < 0.01f) return;

        Vector3 velDir = rb.linearVelocity.normalized;

        if (Mathf.Abs(strafeInput) > 0.05f)
        {
            // Perpendicular to velocity (right = positive strafe)
            Vector3 strafeDir = new Vector3(velDir.y, -velDir.x, 0f) * strafeInput;
            float forceMag = CalculateEffectiveThrust(rb.linearVelocity, strafeDir.normalized, strafeForce * Mathf.Abs(strafeInput));
            rb.AddForce(strafeDir.normalized * forceMag);
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

        if (StartVelocity.sqrMagnitude > 0.001f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, StartVelocity);
            Gizmos.DrawWireSphere(transform.position + StartVelocity, 0.3f);
        }
    }
}