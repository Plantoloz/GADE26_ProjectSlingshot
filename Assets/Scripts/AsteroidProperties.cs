using UnityEngine;

public class AsteroidProperties : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 3.0f;
    public float massMultiplier = 2.0f; // Adjust how heavy they are relative to size

    void Start()
    {
        // 1. Generate a random scale
        float randomScale = Random.Range(minSize, maxSize);
        transform.localScale = Vector3.one * randomScale;

        // 2. Correlate Mass to Size
        // In 3D, mass is volume-based (scale^3), but for 2.5D gameplay, 
        // scale^2 or a linear scale often feels better for balance.
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.mass = randomScale * massMultiplier;

        // Optional: Randomize initial rotation for visual variety
        // Randomize color slightly to distinguish between different "densities"
        GetComponent<Renderer>().material.color *= Random.Range(0.8f, 1.2f);
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
    }
}