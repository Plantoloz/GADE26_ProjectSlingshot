using UnityEngine;

// Attach to a ProBuilder Torus GameObject.
// Ring normal = transform.up (ProBuilder torus default: hole faces Y).
// The inner radius is read automatically from the mesh vertices.
public class RingCheckpoint : CheckpointBase
{
    [Header("Detection")]
    [Tooltip("Auto-filled from mesh on Awake. Override manually if needed.")]
    public float ringOpeningRadius = 8f;
    [Tooltip("Distance from center for 'Good' rating (must be < ringOpeningRadius)")]
    public float outerRingOpeningRadius = 5f;
    [Tooltip("Distance from center for 'Excellent' rating (must be < outerRingOpeningRadius)")]
    public float innerRingOpeningRadius = 2f;

    [Header("Hit Sounds")]
    public AudioClip excellentSound;
    public AudioClip goodSound;
    public AudioClip missSound;

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
    }

    // ── State transitions ─────────────────────────────────────────────────────

    public override void Activate()
    {
        CurrentState = CheckpointState.Active;
        if (ship != null) prevShipPos = ship.position;
    }

    public override void Deactivate()
    {
        CurrentState = CheckpointState.Inactive;
    }

    public override void Complete()
    {
        CurrentState = CheckpointState.Completed;
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
            {
                PlayHitSound(inPlane.magnitude);
                manager?.OnCheckpointReached(this);
            }
        }

        prevShipPos = ship.position;
    }

    private void PlayHitSound(float hitDistance)
    {
        if (AudioManager.Instance == null) return;
        AudioClip clip = hitDistance <= innerRingOpeningRadius ? excellentSound
                       : hitDistance <= outerRingOpeningRadius ? goodSound
                       : missSound;
        AudioManager.Instance.PlaySFX(clip);
    }

    void OnDrawGizmosSelected()
    {
        int segments = 48;
        void DrawDisc(float radius, Color color)
        {
            Gizmos.color = color;
            Vector3 p = transform.position + transform.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                Vector3 n = transform.position
                          + transform.right   * Mathf.Cos(a) * radius
                          + transform.forward * Mathf.Sin(a) * radius;
                Gizmos.DrawLine(p, n);
                p = n;
            }
        }

        DrawDisc(ringOpeningRadius,      Color.yellow);
        DrawDisc(outerRingOpeningRadius, Color.green);
        DrawDisc(innerRingOpeningRadius, Color.cyan);
        // Normal arrow so you can see which axis is the ring normal
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.up * ringOpeningRadius);
    }
}