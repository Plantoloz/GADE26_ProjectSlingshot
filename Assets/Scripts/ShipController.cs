using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    public float thrustForce = 15f;
    public float rotationSpeed = 200f; // Degrees per second
    public float brakePower = 2f;
    
    private Rigidbody rb;
    private Vector2 currentInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure angular drag doesn't fight our manual rotation
        rb.angularVelocity = Vector3.zero;
    }

    void OnMove(InputValue value)
    {
        currentInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        // 1. CONSTANT ROTATION (No momentum)
        if (currentInput.x != 0)
        {
            // Calculate how much to rotate this frame
            float rotationAmount = -currentInput.x * rotationSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0, 0, rotationAmount);
            
            // MoveRotation is the "best practice" for kinematic-style movement on a Rigidbody
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
        else
        {
            // Instantly kill any stray rotation momentum
            rb.angularVelocity = Vector3.zero;
        }

        // 2. MOVEMENT (Still has physics momentum)
        if (currentInput.y > 0) 
        {
            rb.AddForce(transform.up * thrustForce);
        }
        else if (currentInput.y < 0) 
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * brakePower);
        }
    }
}