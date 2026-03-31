using UnityEngine;

public class AsteroidProperties : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 3.0f;
    public float massMultiplier = 2.0f; // Adjust how heavy they are relative to size
    
    [Header("Initial Motion")]
    public float minInitialVelocity = 1f;
    public float maxInitialVelocity = 5f;

    void Start()
    {
        // 1. Generate a random scale
        float randomScale = Random.Range(minSize, maxSize);
        transform.localScale = Vector3.one * randomScale;

        // 2. Correlate Mass to Size
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.mass = randomScale * massMultiplier * rb.mass;

        // 3. Set Initial Velocity
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomSpeed = Random.Range(minInitialVelocity, maxInitialVelocity);
        rb.linearVelocity = new Vector3(randomDir.x, randomDir.y, 0) * randomSpeed;

        // Optional: Randomize initial rotation for visual variety
        // Randomize color slightly to distinguish between different "densities"
        GetComponent<Renderer>().material.color *= Random.Range(0.8f, 1.2f);
        
        transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
    }
}