using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    public int predictionSteps = 50;   // How far the line goes
    public float stepTime = 0.1f;      // Precision of the line
    private LineRenderer line;
    private ShipController ship;       // To get current velocity
    private Rigidbody rb;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody>();
        ship = GetComponent<ShipController>();
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
        line.positionCount = predictionSteps;
        
        Vector3 virtualPlayerPos = transform.position;
        Vector3 virtualPlayerVel = rb.linearVelocity;

        // Capture initial state of all relevant attractors
        List<VirtualBody> virtualAttractors = new List<VirtualBody>();
        foreach (var body in GravityBody.allBodies)
        {
            if (body.gameObject == gameObject || !body.isAttractor) continue;
            
            virtualAttractors.Add(new VirtualBody {
                body = body,
                position = body.rb.position,
                velocity = body.rb.linearVelocity
            });
        }

        for (int i = 0; i < predictionSteps; i++)
        {
            // Safety Check: Stop if we hit NaN or Infinity
            if (!float.IsFinite(virtualPlayerPos.x) || !float.IsFinite(virtualPlayerPos.y)) break;

            line.SetPosition(i, virtualPlayerPos);

            // 1. Calculate gravity acceleration from all virtual attractors
            Vector3 totalGravityAccel = Vector3.zero;
            foreach (var vBody in virtualAttractors)
            {
                Vector3 dir = vBody.position - virtualPlayerPos;
                float dist = dir.magnitude;

                if (dist > 0.01f && dist < GravityBody.maxInfluenceDistance)
                {
                    // Acceleration: a = G * m_other / r
                    float accelMag = GravityBody.G * vBody.body.rb.mass / Mathf.Max(dist, GravityBody.minForceDistance);
                    totalGravityAccel += dir.normalized * accelMag;
                }
            }

            // 2. Update virtual player physics
            virtualPlayerVel += totalGravityAccel * stepTime;
            virtualPlayerPos += virtualPlayerVel * stepTime;

            // 3. Update virtual attractor positions (move them forward linearly based on their velocity)
            for (int j = 0; j < virtualAttractors.Count; j++)
            {
                var vBody = virtualAttractors[j];
                vBody.position += vBody.velocity * stepTime;
                virtualAttractors[j] = vBody; // Update struct in list
            }
        }
    }
}