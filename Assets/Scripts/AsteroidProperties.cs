using UnityEngine;

public class AsteroidProperties : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 3.0f;
    public float massMultiplier = 2.0f; // Adjust how heavy they are relative to size
    
    [Header("Initial Motion")]
    public float minInitialVelocity = 1f;
    public float maxInitialVelocity = 5f;
    public float minAngularVelocity = 5f;  // Deg/s
    public float maxAngularVelocity = 30f; // Deg/s

    private Rigidbody rb;
    private Renderer asteroidRenderer;
    private Vector3 manualVelocity;
    private bool wasInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        asteroidRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        if (!wasInitialized)
        {
            InitializeWithSeed(Random.Range(-100000, 100000));
        }
    }

    /// <summary>
    /// Switches between full physics and cheap manual drift.
    /// </summary>
    public void SetPhysicsActive(bool active)
    {
        if (rb == null) return;

        if (active && rb.isKinematic)
        {
            // Turn physics ON
            rb.isKinematic = false;
            rb.linearVelocity = manualVelocity;
        }
        else if (!active && !rb.isKinematic)
        {
            // Turn physics OFF (Manual drift mode)
            manualVelocity = rb.linearVelocity;
            rb.isKinematic = true;
        }

        // Optimization: If very far, we can even disable the renderer
        // asteroidRenderer.enabled = ...
    }

    public void UpdateManualDrift(float deltaTime)
    {
        if (rb != null && rb.isKinematic)
        {
            transform.position += manualVelocity * deltaTime;
        }
    }

    public void InitializeWithSeed(int seed)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        wasInitialized = true;
        System.Random prng = new System.Random(seed);
        
        // 1. Generate a random scale
        float randomScale = (float)(prng.NextDouble() * (maxSize - minSize) + minSize);
        transform.localScale = Vector3.one * randomScale;

        // 2. Set Initial Motion
        if (rb != null)
        {
            // Linear Velocity
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2);
            Vector2 randomDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float randomSpeed = (float)(prng.NextDouble() * (maxInitialVelocity - minInitialVelocity) + minInitialVelocity);
            
            // Store this as our base velocity
            manualVelocity = new Vector3(randomDir.x, randomDir.y, 0) * randomSpeed;
            
            // Angular Velocity
            float randomSpin = (float)(prng.NextDouble() * (maxAngularVelocity - minAngularVelocity) + minAngularVelocity);
            if (prng.NextDouble() > 0.5) randomSpin *= -1f;
            rb.angularVelocity = new Vector3(0, 0, randomSpin * Mathf.Deg2Rad);

            SyncMass();
        }

        // 3. Visuals
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            float colorMultiplier = (float)(prng.NextDouble() * 0.4 + 0.8); // 0.8 to 1.2
            renderer.material.color *= colorMultiplier;
        }
        
        // Random initial static rotation
        float rotX = (float)(prng.NextDouble() * 360);
        float rotY = (float)(prng.NextDouble() * 360);
        float rotZ = (float)(prng.NextDouble() * 360);
        transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
    }

    public void SyncMass()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Update mass based on current scale and multiplier
            rb.mass = transform.localScale.x * massMultiplier;
        }
    }
}