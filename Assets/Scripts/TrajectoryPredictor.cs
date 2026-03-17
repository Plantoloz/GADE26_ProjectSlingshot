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

    void DrawProjection()
    {
        line.positionCount = predictionSteps;
        
        Vector3 virtualPos = transform.position;
        Vector3 virtualVel = rb.linearVelocity;

        for (int i = 0; i < predictionSteps; i++)
        {
            line.SetPosition(i, virtualPos);

            // Calculate gravity from all active attractors at this virtual point
            Vector3 totalGravity = Vector3.zero;
            foreach (var attractor in GravityBody.attractors)
            {
                if (attractor.gameObject == gameObject) continue;

                Vector3 dir = attractor.rb.position - virtualPos;
                float distSq = dir.sqrMagnitude;
                if (distSq > 0.1f) // Avoid division by zero
                {
                    float force = GravityBody.G * (rb.mass * attractor.rb.mass) / distSq;
                    totalGravity += dir.normalized * (force / rb.mass);
                }
            }

            // Update virtual physics
            virtualVel += totalGravity * stepTime;
            virtualPos += virtualVel * stepTime;
        }
    }
}