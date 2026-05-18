using UnityEngine;

// Attach to a ProBuilder Torus GameObject.
// Ring normal = transform.up (ProBuilder torus default: hole faces Y).
// The inner radius is read automatically from the mesh vertices.
public class RingCheckpoint : CheckpointBase
{
    [Header("Detection")]
    [Tooltip("Auto-filled from mesh on Awake. Override manually if needed.")]
    public float ringOpeningRadius = 8f;

    [Header("Colors")]
    public Color inactiveColor  = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color activeColor    = new Color(1f, 0.9f, 0f, 1f);
    public Color completedColor = new Color(0f, 1f, 0.4f, 1f);

    private CheckpointManager manager;
    private Transform ship;
    private Vector3 prevShipPos;
    private Renderer[] renderers;
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    void Awake()
    {
        manager   = FindFirstObjectByType<CheckpointManager>();
        var sc    = FindFirstObjectByType<ShipController>();
        if (sc != null) ship = sc.transform;
        renderers = GetComponentsInChildren<Renderer>();

        ReadRadiusFromMesh();
        ApplyVisuals();
    }

    // ── Radius auto-detection ─────────────────────────────────────────────────

    [ContextMenu("Read Radius From Mesh")]
    void ReadRadiusFromMesh()
    {
        var mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        float minDist = float.MaxValue;
        foreach (var v in mf.sharedMesh.vertices)
        {
            // Convert vertex from MeshFilter local space → world → this local space
            Vector3 local = transform.InverseTransformPoint(mf.transform.TransformPoint(v));
            // Ring normal is local Y; project onto local XZ plane to get radial distance
            float dist = new Vector2(local.x, local.z).magnitude;
            if (dist > 0.01f && dist < minDist)
                minDist = dist;
        }

        if (minDist < float.MaxValue)
            ringOpeningRadius = minDist;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    public override void Activate()
    {
        CurrentState = CheckpointState.Active;
        if (ship != null) prevShipPos = ship.position;
        ApplyVisuals();
    }

    public override void Deactivate()
    {
        CurrentState = CheckpointState.Inactive;
        ApplyVisuals();
    }

    public override void Complete()
    {
        CurrentState = CheckpointState.Completed;
        ApplyVisuals();
    }

    // ── Detection ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (CurrentState != CheckpointState.Active || ship == null) return;

        Vector3 toNow  = ship.position - transform.position;
        Vector3 toPrev = prevShipPos   - transform.position;

        // Ring normal is transform.up (ProBuilder torus: hole faces local Y)
        float dotNow  = Vector3.Dot(toNow,  transform.up);
        float dotPrev = Vector3.Dot(toPrev, transform.up);

        if (dotNow * dotPrev < 0f) // crossed the plane (either direction)
        {
            float t = dotPrev / (dotPrev - dotNow);
            Vector3 crossing = Vector3.Lerp(prevShipPos, ship.position, t) - transform.position;
            Vector3 inPlane  = crossing - Vector3.Dot(crossing, transform.up) * transform.up;

            if (inPlane.magnitude <= ringOpeningRadius)
                manager?.OnCheckpointReached(this);
        }

        prevShipPos = ship.position;
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    void ApplyVisuals()
    {
        Color c = CurrentState switch
        {
            CheckpointState.Active    => activeColor,
            CheckpointState.Completed => completedColor,
            _                         => inactiveColor
        };

        var block = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor(ColorProp, c);
            r.SetPropertyBlock(block);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Disc in the ring plane (perpendicular to transform.up)
        int segments = 48;
        Vector3 prev = transform.position + transform.right * ringOpeningRadius;
        for (int i = 1; i <= segments; i++)
        {
            float a    = 2f * Mathf.PI * i / segments;
            Vector3 next = transform.position
                         + transform.right   * Mathf.Cos(a) * ringOpeningRadius
                         + transform.forward * Mathf.Sin(a) * ringOpeningRadius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        // Normal arrow so you can see which axis is the ring normal
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.up * ringOpeningRadius);
    }
}