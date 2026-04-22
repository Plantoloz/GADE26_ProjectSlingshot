using System.Collections.Generic;
using UnityEngine;

public class CameraFollowVelocity : MonoBehaviour
{
    public Transform player;

    [Header("Perspective Position")]
    [Tooltip("Height above the game plane (Y-Achse)")]
    public float height = 8f;
    [Tooltip("Z-Abstand hinter der Spielebene")]
    public float depth = 10f;
    [Tooltip("Wie weit die Kamera hinter dem Schiff (in Flugrichtung) zurückbleibt")]
    public float behindDistance = 3f;

    [Header("Zoom (Field of View)")]
    public float minFOV = 40f;
    public float maxFOV = 70f;
    public float maxPlayerSpeed = 20f;
    public float zoomSmoothTime = 0.3f;

    [Header("Follow Settings")]
    public float followSmoothTime = 0.2f;
    public float rotationSmoothTime = 0.15f;

    [Header("Velocity Look-Ahead")]
    [Tooltip("Wie weit die Kamera der Flugrichtung vorausschaut")]
    public float lookAheadDistance = 4f;
    [Tooltip("Wie träge die Look-Ahead-Richtung folgt")]
    public float lookAheadSmoothTime = 0.4f;

    [Header("Planet Focus")]
    [Tooltip("Extra Abstandsfaktor beim Einrahmen von Schiff + Planet")]
    public float focusPadding = 1.3f;

    [SerializeField]
    private Camera cam;
    private Rigidbody playerRb;

    private float currentFOVVelocity;
    private Vector3 followVelocity;
    private Vector3 smoothedLookAhead;
    private Vector3 lookAheadVelocity;
    private Vector3 lastVelocityDir = Vector3.right;

    private readonly HashSet<Transform> activePlanets = new HashSet<Transform>();

    void Start()
    {
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    public void RegisterTrigger(Transform planet)   => activePlanets.Add(planet);
    public void UnregisterTrigger(Transform planet) => activePlanets.Remove(planet);

    Transform ClosestPlanet()
    {
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (Transform planet in activePlanets)
        {
            float dist = Vector3.Distance(player.position, planet.position);
            if (dist < minDist) { minDist = dist; closest = planet; }
        }
        return closest;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Velocity direction in XY plane
        if (playerRb != null)
        {
            Vector2 vel2D = new Vector2(playerRb.linearVelocity.x, playerRb.linearVelocity.y);
            if (vel2D.sqrMagnitude > 0.01f)
                lastVelocityDir = new Vector3(vel2D.normalized.x, vel2D.normalized.y, 0f);
        }

        // Smooth look-ahead target
        Vector3 targetLookAhead = lastVelocityDir * lookAheadDistance;
        smoothedLookAhead = Vector3.SmoothDamp(smoothedLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

        Transform focusPlanet = activePlanets.Count > 0 ? ClosestPlanet() : null;

        Vector3 targetPosition;
        float targetFOV;

        if (focusPlanet != null)
        {
            // Frame both ship and planet from same angle
            Vector3 midpoint = (player.position + focusPlanet.position) * 0.5f;
            targetPosition = midpoint + new Vector3(0f, height, -depth);

            float halfDist = Vector3.Distance(player.position, focusPlanet.position) * 0.5f;
            float planetRadius = focusPlanet.localScale.x * 0.5f;
            // Approximate FOV to fit the scene
            float requiredSize = (halfDist + planetRadius) * focusPadding;
            targetFOV = Mathf.Clamp(requiredSize * 4f, minFOV, maxFOV);
        }
        else
        {
            // Camera sits behind and above the ship along its velocity direction
            Vector3 behindOffset = -lastVelocityDir * behindDistance;
            Vector3 heightOffset = new Vector3(0f, height, -depth);
            targetPosition = player.position + behindOffset + heightOffset;

            float speed = (playerRb != null) ? playerRb.linearVelocity.magnitude : 0f;
            targetFOV = Mathf.Lerp(minFOV, maxFOV, speed / maxPlayerSpeed);
        }

        // Apply smoothed position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);

        // Always look at the ship + look-ahead point
        Vector3 lookTarget = player.position + smoothedLookAhead;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);

        // FOV-based zoom
        if (cam != null)
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFOV, ref currentFOVVelocity, zoomSmoothTime);
    }

    // Convenience method called by the Editor button
    public void AlignToPlayer()
    {
        if (player == null) return;
        transform.position = player.position + new Vector3(0f, height, -depth);
        transform.rotation = Quaternion.LookRotation(player.position - transform.position, Vector3.up);
    }
}
