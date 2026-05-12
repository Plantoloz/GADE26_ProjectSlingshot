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

    [Header("Gameplay Line")]
    [Tooltip("LineRenderer on the 'Gameplay Line' child GO (Default layer)")]
    public LineRenderer gameplayLine;
    public float dashLength = 0.5f;
    public float gapLength  = 0.3f;
    public float lineWidth  = 0.1f;

    [Header("Minimap Line")]
    [Tooltip("LineRenderer on the 'Minimap Line' child GO (Minimap layer)")]
    public LineRenderer minimapLine;
    public float minimapLineWidth = 2f;
    public Color minimapLineColor = new Color(0.4f, 0.9f, 1f, 0.8f);

    public bool  pathCollisionDetected { get; private set; }
    public float timeToImpact          { get; private set; } = float.PositiveInfinity;
    public Vector3 NextPredictedPoint  { get; private set; }

    // Pre-allocated buffers — zero per-frame heap allocations
    private Rigidbody      rb;
    private Vector3[]      simPositionBuffer;
    private Keyframe[]     keyframeBuffer;
    private VirtualBody[]  attractorBuffer  = new VirtualBody[32];
    private Collider[]     overlapBuffer3D  = new Collider[16];
    private Collider2D[]   overlapBuffer2D  = new Collider2D[16];
    private AnimationCurve widthCurve       = new AnimationCurve();

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
        keyframeBuffer    = new Keyframe[predictionSteps];

        SetupGameplayLine();
        SetupMinimapLine();

        NextPredictedPoint = rb != null ? rb.position + shipRigidbody.transform.up : Vector3.zero;
    }

    void LateUpdate() => DrawProjection();

    // ── Line setup ────────────────────────────────────────────────────────────

    void SetupGameplayLine()
    {
        if (gameplayLine == null) return;
        gameplayLine.material            = new Material(Shader.Find("Sprites/Default"));
        gameplayLine.material.color      = Color.white;
        gameplayLine.startColor          = Color.white;
        gameplayLine.endColor            = Color.white;
        gameplayLine.widthMultiplier     = lineWidth;
        gameplayLine.useWorldSpace       = true;
        gameplayLine.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
        gameplayLine.receiveShadows      = false;
    }

    void SetupMinimapLine()
    {
        if (minimapLine == null) return;
        minimapLine.material            = new Material(Shader.Find("Sprites/Default"));
        minimapLine.material.color      = Color.white;
        minimapLine.startColor          = minimapLineColor;
        minimapLine.endColor            = new Color(minimapLineColor.r, minimapLineColor.g, minimapLineColor.b, 0f);
        minimapLine.widthMultiplier     = minimapLineWidth;
        minimapLine.useWorldSpace       = true;
        minimapLine.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
        minimapLine.receiveShadows      = false;
    }

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

        if (simCount > 1)
            NextPredictedPoint = simPositionBuffer[1];

        WriteToGameplayLine(simCount);
        WriteToMinimapLine(simCount);
    }

    // ── Line writers ──────────────────────────────────────────────────────────

    void WriteToGameplayLine(int simCount)
    {
        if (gameplayLine == null) return;

        gameplayLine.positionCount = simCount;
        gameplayLine.SetPositions(simPositionBuffer);

        // Dashed width curve
        float cycleLen  = dashLength + gapLength;
        float distAccum = 0f;
        for (int i = 0; i < simCount; i++)
        {
            if (i > 0)
                distAccum += Vector3.Distance(simPositionBuffer[i], simPositionBuffer[i - 1]);
            float t = simCount > 1 ? (float)i / (simCount - 1) : 0f;
            float w = distAccum % cycleLen < dashLength ? 1f : 0f;
            keyframeBuffer[i] = new Keyframe(t, w, float.PositiveInfinity, float.PositiveInfinity);
        }
        widthCurve.keys      = keyframeBuffer[0..simCount];
        gameplayLine.widthCurve      = widthCurve;
        gameplayLine.widthMultiplier = lineWidth;
    }

    void WriteToMinimapLine(int simCount)
    {
        if (minimapLine == null) return;

        minimapLine.positionCount = simCount;
        minimapLine.SetPositions(simPositionBuffer);
    }
}