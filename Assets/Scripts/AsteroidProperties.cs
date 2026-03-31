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

    void Start()
    {
        InitializeAsteroid();

        // Randomize color slightly
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.material.color *= Random.Range(0.8f, 1.2f);
        
        // Random initial static rotation
        transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
    }

    // Allows you to see changes immediately when tweaking in the Inspector
    void OnValidate()
    {
        if (Application.isPlaying) SyncMass();
    }

    void InitializeAsteroid()
    {
        // 1. Generate a random scale
        float randomScale = Random.Range(minSize, maxSize);
        transform.localScale = Vector3.one * randomScale;

        // 2. Set Initial Motion
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Linear Velocity
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomSpeed = Random.Range(minInitialVelocity, maxInitialVelocity);
            rb.linearVelocity = new Vector3(randomDir.x, randomDir.y, 0) * randomSpeed;
            
            // Angular Velocity (Continuous spin)
            float randomSpin = Random.Range(minAngularVelocity, maxAngularVelocity);
            if (Random.value > 0.5f) randomSpin *= -1f;
            rb.angularVelocity = new Vector3(0, 0, randomSpin * Mathf.Deg2Rad);

            SyncMass();
        }
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