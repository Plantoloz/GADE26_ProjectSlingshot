using UnityEngine;

public class TrajectoryPredictor : MonoBehaviour
{
    [Header("Ship Reference")]
    [Tooltip("The Ship's Rigidbody. Required — TrajectoryPredictor must sit on a child GO of the Ship.")]
    public Rigidbody shipRigidbody;

    [Header("Simulation")]
    public int   predictionSteps   = 50;
    public float stepTime          = 0.1f;
    public float predictionRadius  = 0.8f;

    [Header("Speed Increase")]
    [Tooltip("If enabled, the ship's speed grows by speedIncreaseRatio each simulated timestep.")]
    public bool  useSpeedIncrease    = false;
    [Tooltip("Multiplicative factor applied to velocity magnitude each timestep (e.g. 1.01 = +1 %/step).")]
    public float speedIncreaseRatio  = 1.01f;

    public bool  pathCollisionDetected { get; private set; }
    public float timeToImpact          { get; private set; } = float.PositiveInfinity;
    public Vector3 NextPredictedPoint  { get; private set; }

    public int     SimulatedPointCount { get; private set; }
    public Vector3[] SimulatedPoints  => simPositionBuffer;

    // Pre-allocated buffers — zero per-frame heap allocations
    private Rigidbody      rb;
    private Vector3[]      simPositionBuffer;
    private VirtualBody[]  attractorBuffer  = new VirtualBody[32];
    private Collider[]     overlapBuffer3D  = new Collider[16];
    private Collider2D[]   overlapBuffer2D  = new Collider2D[16];

    private struct VirtualBody
    {
        public GravityBody body;
        public Vector3 position;
        public Vector3 velocity;
    }

    void Awake()
    {
        rb = shipRigidbody;
        simPositionBuffer = new Vector3[predictionSteps];
        NextPredictedPoint = rb != null ? rb.position + shipRigidbody.transform.up : Vector3.zero;
    }

    void LateUpdate() => DrawProjection();

    // ── Simulation ────────────────────────────────────────────────────────────

    void DrawProjection()
    {
        if (rb == null) return;

        pathCollisionDetected = false;
        timeToImpact          = float.PositiveInfinity;
        NextPredictedPoint    = rb.position + shipRigidbody.transform.up;

        Vector3 virtualPos = rb.position;
        Vector3 virtualVel = rb.linearVelocity;

        int attractorCount = 0;
        foreach (var body in GravityBody.allBodies)
        {
            if (body == null || body.gameObject == shipRigidbody.gameObject || !body.isAttractor) continue;
            if (attractorCount >= attractorBuffer.Length) break;
            attractorBuffer[attractorCount++] = new VirtualBody {
                body     = body,
                position = body.rb.position,
                velocity = body.rb.linearVelocity
            };
        }

        int simCount = 0;

        for (int i = 0; i < predictionSteps; i++)
        {
            if (!float.IsFinite(virtualPos.x) || !float.IsFinite(virtualPos.y)) break;

            simPositionBuffer[simCount++] = virtualPos;

            if (!pathCollisionDetected)
            {
                int n3D = Physics.OverlapSphereNonAlloc(virtualPos, predictionRadius, overlapBuffer3D);
                for (int k = 0; k < n3D; k++)
                {
                    if (overlapBuffer3D[k].gameObject != shipRigidbody.gameObject &&
                        (overlapBuffer3D[k].CompareTag("Asteroid") || overlapBuffer3D[k].CompareTag("Planet")))
                    { pathCollisionDetected = true; break; }
                }

                if (!pathCollisionDetected)
                {
                    int n2D = Physics2D.OverlapCircleNonAlloc((Vector2)virtualPos, predictionRadius, overlapBuffer2D);
                    for (int k = 0; k < n2D; k++)
                    {
                        if (overlapBuffer2D[k].gameObject != shipRigidbody.gameObject &&
                            (overlapBuffer2D[k].CompareTag("Asteroid") || overlapBuffer2D[k].CompareTag("Planet")))
                        { pathCollisionDetected = true; break; }
                    }
                }

                if (pathCollisionDetected)
                {
                    timeToImpact = i * stepTime;
                    break;
                }
            }

            Vector3 gravAccel = Vector3.zero;
            for (int j = 0; j < attractorCount; j++)
            {
                Vector3 dir  = attractorBuffer[j].position - virtualPos;
                float   dist = dir.magnitude;
                if (dist > 0.01f && dist < attractorBuffer[j].body.influenceRadius)
                {
                    float mag = GravityBody.GRAVITY_CONSTANT * attractorBuffer[j].body.rb.mass
                                / Mathf.Pow(Mathf.Max(dist, GravityBody.minForceDistance), 2f);
                    gravAccel += dir.normalized * mag;
                }
            }

            virtualVel += gravAccel * stepTime;
            virtualPos += virtualVel * stepTime;

            for (int j = 0; j < attractorCount; j++)
                attractorBuffer[j].position += attractorBuffer[j].velocity * stepTime;
        }

        SimulatedPointCount = simCount;
        if (simCount > 1)
            NextPredictedPoint = simPositionBuffer[1];
    }
    
    
    
}