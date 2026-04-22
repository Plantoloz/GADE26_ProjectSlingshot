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
    [Tooltip("Radius of the off-screen indicator circle in canvas units")]
    public float circleRadius = 400f;
    [Tooltip("World-space radius of the target planet, used to offset the arrow above it when on-screen")]
    public float planetRadius = 10f;

    private Image arrow;
    private Camera cam;
    private RectTransform rt;
    private RectTransform canvasRT;

    void Awake()
    {
        arrow    = GetComponent<Image>();
        rt       = GetComponent<RectTransform>();
        cam      = Camera.main;
        canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

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

        arrow.enabled = true;

        Vector3 targetWorld = checkpoints[idx].transform.position;
        Vector3 viewportPos = cam.WorldToViewportPoint(targetWorld);

        if (viewportPos.z < 0f)
            viewportPos = new Vector3(1f - viewportPos.x, 1f - viewportPos.y, 0f);

        bool isOnScreen = viewportPos.z > 0f
            && viewportPos.x > 0f && viewportPos.x < 1f
            && viewportPos.y > 0f && viewportPos.y < 1f;

        Vector2 canvasSize = canvasRT.rect.size;

        if (isOnScreen)
        {
            // Project a world-space point offset above the planet to get the viewport-space Y offset.
            float viewportOffsetY = cam.WorldToViewportPoint(targetWorld + cam.transform.up * planetRadius).y
                                    - viewportPos.y;

            rt.anchoredPosition = new Vector2(
                (viewportPos.x - 0.5f) * canvasSize.x,
                (viewportPos.y - 0.5f + viewportOffsetY) * canvasSize.y
            );

            // Arrow sprite points up at 0°, so 180° = pointing down toward the planet.
            rt.rotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
            rt.anchoredPosition = dir.normalized * circleRadius;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            rt.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}

