using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CheckpointIndicator : MonoBehaviour
{
    [Header("References")]
    public CheckpointManager checkpointManager;

    [Header("Settings")]
    [Tooltip("0 = current checkpoint, 1 = next checkpoint, etc.")]
    public int checkpointOffset = 0;
    [Tooltip("Distance from screen edge in pixels (at 1920x1080)")]
    public float edgePadding = 60f;

    private Image arrow;
    private Camera cam;
    private RectTransform rt;
    private RectTransform canvasRt;

    void Awake()
    {
        arrow = GetComponent<Image>();
        rt    = GetComponent<RectTransform>();
        cam   = Camera.main;

        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRt = canvas.rootCanvas.GetComponent<RectTransform>();

        // Arrow must be anchored to canvas center so anchoredPosition is center-relative.
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
    }

    void LateUpdate()
    {
        if (checkpointManager == null || checkpointManager.IsComplete)
        {
            arrow.enabled = false;
            return;
        }

        int idx = checkpointManager.CurrentIndex + checkpointOffset;
        var checkpoints = checkpointManager.checkpoints;

        if (idx < 0 || idx >= checkpoints.Count || checkpoints[idx] == null)
        {
            arrow.enabled = false;
            return;
        }

        // Viewport coordinates (0-1) are scale-independent and work regardless of CanvasScaler settings.
        Vector3 viewportPos = cam.WorldToViewportPoint(checkpoints[idx].transform.position);

        // If behind the camera, flip so the arrow points away from the ship.
        if (viewportPos.z < 0f)
            viewportPos = new Vector3(1f - viewportPos.x, 1f - viewportPos.y, 0f);

        bool isOnScreen = viewportPos.z > 0f
            && viewportPos.x > 0f && viewportPos.x < 1f
            && viewportPos.y > 0f && viewportPos.y < 1f;

        arrow.enabled = !isOnScreen;
        if (!isOnScreen)
        {
            // Map viewport [0,1] → canvas units centered at origin.
            Vector2 canvasSize = canvasRt.rect.size;
            Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);

            rt.anchoredPosition = ClampToEdge(dir, canvasSize * 0.5f);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            rt.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // Returns the position on the canvas rectangle edge along the given direction.
    Vector2 ClampToEdge(Vector2 dir, Vector2 halfExtents)
    {
        float halfW = halfExtents.x - edgePadding;
        float halfH = halfExtents.y - edgePadding;

        if (dir == Vector2.zero) return Vector2.zero;

        Vector2 normalized = dir.normalized;

        float scaleX = Mathf.Abs(normalized.x) > 0.0001f ? halfW / Mathf.Abs(normalized.x) : float.MaxValue;
        float scaleY = Mathf.Abs(normalized.y) > 0.0001f ? halfH / Mathf.Abs(normalized.y) : float.MaxValue;

        return normalized * Mathf.Min(scaleX, scaleY);
    }
}
