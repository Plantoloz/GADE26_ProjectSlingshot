using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    public static List<GravityBody> allBodies = new List<GravityBody>();
    
    [Header("Gravity Settings")]
    public bool isAttractor = true;
    public bool isAttractee = false;

    public static float GRAVITY_CONSTANT = 9.81f; 

    [Header("Optimization")]
    public float influenceRadiusMultiplier = 5f; 
    public static float minForceDistance = 1.0f; 
    
    public Rigidbody rb 
    { 
        get 
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            return _rb;
        }
    }
    private Rigidbody _rb;

    // Triggered automatically when added in the Unity Editor
    void Reset() => ConfigureRigidbody();
    
    // Triggered automatically when the game starts
    void Awake() => ConfigureRigidbody();

    void ConfigureRigidbody()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) return;
        
        _rb.useGravity = false;
        // Lock to 2.5D plane
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | 
                         RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezeRotationY;
    }

    void OnEnable() 
    {
        if (!allBodies.Contains(this)) allBodies.Add(this);
    }
    void OnDisable() => allBodies.Remove(this);

    void OnDrawGizmosSelected()
    {
        if (!isAttractor) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, influenceRadius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, influenceRadius);
    }

    public float influenceRadius
    {
        get { return rb != null ? rb.mass * influenceRadiusMultiplier : 0f; }
    }

    void FixedUpdate()
    {
        if (!isAttractee || rb == null) return;

        // Apply acceleration directly: Force = m * a  =>  a = F / m
        // Our GetGravityAccelerationAtPoint already returns (G * m_other / r)
        Vector3 gravityAcceleration = GetGravityAccelerationAtPoint(rb.position, this);
        rb.AddForce(gravityAcceleration, ForceMode.Acceleration);
    }

    /// <summary>
    /// Calculates the acceleration caused by gravity at a specific point.
    /// Formula: a = G * m_attractor / r²
    /// </summary>
    public static Vector3 GetGravityAccelerationAtPoint(Vector3 position, GravityBody ignoreBody = null)
    {
        Vector3 totalAcceleration = Vector3.zero;

        foreach (var body in allBodies)
        {
            if (body == null || body == ignoreBody || !body.isAttractor || body.rb == null) continue;

            Vector3 direction = body.rb.position - position;
            float distance = direction.magnitude;

            if (distance == 0f || distance > body.influenceRadius) continue;

            // Linear Acceleration: a = G * m_attractor / r
            // float accelerationMagnitude = GRAVITY_CONSTANT * body.rb.mass / Mathf.Max(distance, minForceDistance);
            
            // Inverse-Square Acceleration: a = G * m_attractor / r²
            float accelerationMagnitude = GRAVITY_CONSTANT * body.rb.mass / Mathf.Pow(Mathf.Max(distance, minForceDistance), 2f);
            totalAcceleration += direction.normalized * accelerationMagnitude;
        }

        return totalAcceleration;
    }
}