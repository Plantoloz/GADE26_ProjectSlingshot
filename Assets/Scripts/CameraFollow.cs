using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Zoom Settings")]
    public float minZoom = 5f;
    public float maxZoom = 15f;
    public float zoomSpeed = 100f;
    public float maxPlayerSpeed = 20f;
    public float zoomSmoothTime = 0.3f;

    [Header("Follow Settings")]
    public float followSmoothTime = 0.2f;

    [Header("Planet Focus")]
    [Tooltip("Extra margin factor around both objects (> 1 = more padding)")]
    public float focusPadding = 1.3f;

    [SerializeField]
    private Camera cam;
    private Rigidbody playerRb;
    private float currentZoomVelocity;
    private Vector3 followVelocity;

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
            if (dist < minDist)
            {
                minDist = dist;
                closest = planet;
            }
        }
        return closest;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition;
        float targetZoom;

        Transform focusPlanet = activePlanets.Count > 0 ? ClosestPlanet() : null;

        if (focusPlanet != null)
        {
            // Frame both ship and planet
            Vector3 midpoint = (player.position + focusPlanet.position) * 0.5f;
            targetPosition = midpoint + offset;

            // Circular framing: radius = half the ship-planet distance + planet radius.
            // For orthographic camera in landscape, orthoSize == radius guarantees the
            // circle fits vertically (and horizontally because width > height).
            float halfDist = Vector3.Distance(player.position, focusPlanet.position) * 0.5f;
            float planetRadius = focusPlanet.localScale.x * 0.5f;
            targetZoom = Mathf.Clamp((halfDist + planetRadius) * focusPadding, minZoom, maxZoom);
        }
        else
        {
            // Normal: follow ship, speed-based zoom
            targetPosition = player.position + offset;
            float speed = (playerRb != null) ? playerRb.linearVelocity.magnitude : 0f;
            targetZoom = Mathf.Lerp(minZoom, maxZoom, speed / maxPlayerSpeed);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);

        if (cam != null)
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref currentZoomVelocity, zoomSmoothTime);
    }
}