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

    [SerializeField]
    private Camera cam;
    private Rigidbody playerRb;
    private float currentZoomVelocity;
    private Vector3 followVelocity;

    void Start()
    {
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Smooth follow
        Vector3 targetPosition = player.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);

        // Speed-based zoom
        if (cam != null && playerRb != null)
        {
            float speed = playerRb.linearVelocity.magnitude;
            float targetZoom = Mathf.Lerp(minZoom, maxZoom, speed / maxPlayerSpeed);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref currentZoomVelocity, zoomSmoothTime);
        }
    }
}