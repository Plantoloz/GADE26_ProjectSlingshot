using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class Checkpoint : CheckpointBase
{
    [Header("Trigger")]
    public float checkpointRadius = 15f;

    [Header("Visual Ring")]
    public int circleSegments = 64;
    public float lineWidth  = 0.25f;

    [Header("Colors")]
    public Color inactiveColor  = new Color(0.5f, 0.5f, 0.5f, 0.4f);
    public Color activeColor    = new Color(1f, 0.9f, 0f, 1f);
    public Color completedColor = new Color(0f, 1f, 0.4f, 0.7f);

    private SphereCollider trigger;
    private LineRenderer ring;
    private CheckpointManager manager;

    void Awake()
    {
        // Own Rigidbody so Unity sends trigger callbacks to this child, not the planet root.
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        trigger          = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = checkpointRadius;
        trigger.enabled   = false;

        ring = GetComponent<LineRenderer>();
        ring.useWorldSpace     = false;
        ring.loop              = true;
        ring.material          = new Material(Shader.Find("Sprites/Default"));
        ring.widthMultiplier   = lineWidth;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows    = false;

        manager = FindFirstObjectByType<CheckpointManager>();

        BuildCircle();
        ApplyVisuals();
    }

    // ── State transitions (called by CheckpointManager) ──────────────────────

    public override void Activate()
    {
        CurrentState      = CheckpointState.Active;
        trigger.enabled   = true;
        ApplyVisuals();
    }

    public override void Deactivate()
    {
        CurrentState    = CheckpointState.Inactive;
        trigger.enabled = false;
        ApplyVisuals();
    }

    public override void Complete()
    {
        CurrentState    = CheckpointState.Completed;
        trigger.enabled = false;
        ApplyVisuals();
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
        ring.startColor = c;
        ring.endColor   = c;
    }

    void BuildCircle()
    {
        ring.positionCount = circleSegments;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = 2f * Mathf.PI * i / circleSegments;
            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * checkpointRadius,
                Mathf.Sin(angle) * checkpointRadius,
                0f
            ));
        }

        ring.widthCurve      = AnimationCurve.Constant(0f, 1f, 1f);
        ring.widthMultiplier = lineWidth;
    }

    // ── Trigger detection ─────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (CurrentState != CheckpointState.Active) return;
        if (other.GetComponent<ShipController>() == null) return;
        manager?.OnCheckpointReached(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkpointRadius);
    }
}
