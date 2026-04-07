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
    private Transform focusPlanet;

    void Start()
    {
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    public void SetFocusPlanet(Transform planet) => focusPlanet = planet;
    public void ClearFocusPlanet()               => focusPlanet = null;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition;
        float targetZoom;

        if (focusPlanet != null)
        {
            // Frame both ship and planet
            Vector3 midpoint = (player.position + focusPlanet.position) * 0.5f;
            targetPosition = midpoint + offset;

            // Calculate required orthographic size accounting for 16:9 aspect ratio.
            // orthographicSize = half-height; half-width = orthographicSize * aspectRatio.
            // We need: halfHeight >= |dy|/2 + planetRadius  AND  halfHeight * aspect >= |dx|/2 + planetRadius
            float planetRadius = focusPlanet.localScale.x * 0.5f;
            Vector3 delta = focusPlanet.position - player.position;
            float halfDx = Mathf.Abs(delta.x) * 0.5f + planetRadius;
            float halfDy = Mathf.Abs(delta.y) * 0.5f + planetRadius;
            const float aspect = 16f / 9f;
            float sizeFromHeight = halfDy;
            float sizeFromWidth  = halfDx / aspect;
            targetZoom = Mathf.Clamp(Mathf.Max(sizeFromHeight, sizeFromWidth) * focusPadding, minZoom, maxZoom);
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