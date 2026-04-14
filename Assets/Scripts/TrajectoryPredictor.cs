using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    public int predictionSteps = 50;         // How far the line goes
    public float stepTime = 0.1f;            // Precision of the line
    public float predictionRadius = 0.8f;    // Estimated width of the ship for collision checking

    [Header("Dash Settings")]
    public float dashLength = 0.5f; // World-space length of one visible dash
    public float gapLength  = 0.3f; // World-space length of one invisible gap
    public float lineWidth  = 0.1f; // World-space line width

    public bool pathCollisionDetected { get; private set; }

    private LineRenderer line;
    private ShipController ship;
    private Rigidbody rb;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        rb   = GetComponent<Rigidbody>();
        ship = GetComponent<ShipController>();

        // Replace whatever material is on the renderer with a plain white unlit one
        line.material           = new Material(Shader.Find("Sprites/Default"));
        line.material.color     = Color.white;
        line.startColor         = Color.white;
        line.endColor           = Color.white;
        line.widthMultiplier    = lineWidth;
    }

    void LateUpdate()
    {
        DrawProjection();
    }

    private struct VirtualBody
    {
        public GravityBody body;
        public Vector3 position;
        public Vector3 velocity;
    }

    void DrawProjection()
    {
        pathCollisionDetected = false;

        Vector3 virtualPlayerPos = transform.position;
        Vector3 virtualPlayerVel = rb.linearVelocity;

        // Capture initial state of all relevant attractors
        List<VirtualBody> virtualAttractors = new List<VirtualBody>();
        foreach (var body in GravityBody.allBodies)
        {
            if (body == null || body.gameObject == gameObject || !body.isAttractor) continue;

            virtualAttractors.Add(new VirtualBody {
                body     = body,
                position = body.rb.position,
                velocity = body.rb.linearVelocity
            });
        }

        // Collect positions first, then build the dashed line from them
        List<Vector3> simPositions = new List<Vector3>(predictionSteps);

        for (int i = 0; i < predictionSteps; i++)
        {
            // Safety Check: Stop if we hit NaN or Infinity
            if (!float.IsFinite(virtualPlayerPos.x) || !float.IsFinite(virtualPlayerPos.y)) break;

            simPositions.Add(virtualPlayerPos);

            // Collision check – 3D (Asteroids) and 2D (Planets use PolygonCollider2D)
            if (!pathCollisionDetected)
            {
                foreach (var hit in Physics.OverlapSphere(virtualPlayerPos, predictionRadius))
                    if (hit.gameObject != gameObject && (hit.CompareTag("Asteroid") || hit.CompareTag("Planet")))
                        { pathCollisionDetected = true; break; }

                foreach (var hit in Physics2D.OverlapCircleAll((Vector2)virtualPlayerPos, predictionRadius))
                    if (hit.gameObject != gameObject && (hit.CompareTag("Asteroid") || hit.CompareTag("Planet")))
                        { pathCollisionDetected = true; break; }

                if (pathCollisionDetected) break;
            }

            // 1. Calculate gravity acceleration from all virtual attractors
            Vector3 totalGravityAccel = Vector3.zero;
            foreach (var vBody in virtualAttractors)
            {
                Vector3 dir  = vBody.position - virtualPlayerPos;
                float   dist = dir.magnitude;

                if (dist > 0.01f && dist < vBody.body.influenceRadius)
                {
                    float accelMag = GravityBody.GRAVITY_CONSTANT * vBody.body.rb.mass / Mathf.Pow(Mathf.Max(dist, GravityBody.minForceDistance), 2f);
                    totalGravityAccel += dir.normalized * accelMag;
                }
            }

            // 2. Update virtual player physics
            virtualPlayerVel += totalGravityAccel * stepTime;
            virtualPlayerPos += virtualPlayerVel * stepTime;

            // 3. Update virtual attractor positions (linear drift)
            for (int j = 0; j < virtualAttractors.Count; j++)
            {
                var vBody = virtualAttractors[j];
                vBody.position += vBody.velocity * stepTime;
                virtualAttractors[j] = vBody;
            }
        }

        // Set positions on the line renderer
        line.positionCount = simPositions.Count;
        line.SetPositions(simPositions.ToArray());

        // Build a stepped width curve based on accumulated world-space distance,
        // so dash/gap lengths stay constant regardless of ship speed.
        float cycleLen        = dashLength + gapLength;
        int   count           = simPositions.Count;
        float distAccum       = 0f;
        AnimationCurve widthCurve = new AnimationCurve();

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                distAccum += Vector3.Distance(simPositions[i], simPositions[i - 1]);

            float t          = count > 1 ? (float)i / (count - 1) : 0f;
            float posInCycle = distAccum % cycleLen;
            float w          = posInCycle < dashLength ? 1f : 0f;

            widthCurve.AddKey(new Keyframe(t, w, float.PositiveInfinity, float.PositiveInfinity));
        }

        line.widthCurve      = widthCurve;
        line.widthMultiplier = lineWidth;
    }

}