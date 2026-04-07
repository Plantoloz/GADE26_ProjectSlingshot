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

    private bool wasInitialized = false;

    void Start()
    {
        if (!wasInitialized)
        {
            // If not initialized by the manager (e.g. placed manually), use a random seed
            InitializeWithSeed(Random.Range(-100000, 100000));
        }
    }

    /// <summary>
    /// Deterministically initializes the asteroid based on a seed.
    /// This ensures that an asteroid at a specific location always looks and behaves the same.
    /// </summary>
    public void InitializeWithSeed(int seed)
    {
        wasInitialized = true;
        System.Random prng = new System.Random(seed);

        // 1. Generate a scale
        float randomScale = (float)(prng.NextDouble() * (maxSize - minSize) + minSize);
        transform.localScale = Vector3.one * randomScale;

        // 2. Set Initial Motion
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Linear Velocity
            float angle = (float)(prng.NextDouble() * Mathf.PI * 2);
            Vector2 randomDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float randomSpeed = (float)(prng.NextDouble() * (maxInitialVelocity - minInitialVelocity) + minInitialVelocity);
            rb.linearVelocity = new Vector3(randomDir.x, randomDir.y, 0) * randomSpeed;
            
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