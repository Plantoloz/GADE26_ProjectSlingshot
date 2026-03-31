using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    public static List<GravityBody> allBodies = new List<GravityBody>();
    
    [Header("Gravity Settings")]
    public bool isAttractor = true;
    public bool isAttractee = false;

    // 1. The actual shared constant used in calculations
    public static float G = 10f; 

    // 2. The variable visible in the Unity Inspector
    [SerializeField, Tooltip("Changes G for ALL objects simultaneously")] 
    private float globalGravity = 10f;

    // 3. Automatically syncs the static G when you type a new number in the Editor
    void OnValidate() 
    {
        G = globalGravity;
    }
    public Rigidbody rb { get; private set; }

    // Triggered automatically when added in the Unity Editor
    void Reset() => ConfigureRigidbody();
    
    // Triggered automatically when the game starts
    void Awake() => ConfigureRigidbody();

    void ConfigureRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        // Lock to 2.5D plane
        rb.constraints = RigidbodyConstraints.FreezePositionZ | 
                         RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezeRotationY;
    }

    void OnEnable() => allBodies.Add(this);
    void OnDisable() => allBodies.Remove(this);

    void FixedUpdate()
    {
        if (!isAttractee) return;

        foreach (var body in allBodies)
        {
            // If the other body is an attractor and not myself, it pulls me
            if (body != this && body.isAttractor) 
            {
                body.Attract(this);
            }
        }
    }

    public void Attract(GravityBody objToAttract)
    {
        Rigidbody rbToAttract = objToAttract.rb;
        Vector3 direction = rb.position - rbToAttract.position;
        float distance = direction.magnitude;

        if (distance == 0f) return;

        float forceMagnitude = G * (rb.mass * rbToAttract.mass) / Mathf.Pow(distance, 2);
        Vector3 force = direction.normalized * forceMagnitude;

        rbToAttract.AddForce(force);
    }
}