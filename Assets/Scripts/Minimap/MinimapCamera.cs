using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orthographic top-down camera that mirrors the CameraFollow logic and renders
/// to a RenderTexture for Picture-in-Picture display.
///
/// Setup:
///   1. Create a Camera GameObject, add this component.
///   2. Set the Camera's Culling Mask to only the "Minimap" layer.
///   3. Set Clear Flags to Solid Color.
///   4. Assign the player Transform.
///   5. Point a UI RawImage at OutputTexture via the MinimapUI component.
///
/// Triggering focus: use CameraFocusTrigger on planet colliders — it calls
/// RegisterTrigger / UnregisterTrigger on both CameraFollow and this camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    public static MinimapCamera Instance { get; private set; }

    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public float followSmoothTime = 0.15f;

    [Header("Zoom Settings")]
    [Tooltip("Zoom when ship is stationary")]
    public float minZoom = 20f;
    [Tooltip("Zoom at max ship speed (and default overview)")]
    public float maxZoom = 120f;
    public float maxPlayerSpeed = 20f;
    public float zoomSmoothTime = 0.4f;

    [Header("Planet Focus")]
    [Tooltip("Fixed orthographic size used when locked onto a planet trigger")]
    public float focusZoom = 60f;

    [Header("Render Texture")]
    [Min(64)] public int textureWidth  = 512;
    [Min(64)] public int textureHeight = 512;

    public RenderTexture OutputTexture { get; private set; }

    private Camera                  _cam;
    private Rigidbody               _playerRb;
    private Vector3                 _followVelocity;
    private float                   _zoomVelocity;
    private readonly HashSet<Transform> _activePlanets = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _cam = GetComponent<Camera>();
        OutputTexture        = new RenderTexture(textureWidth, textureHeight, 16);
        _cam.targetTexture   = OutputTexture;
        _cam.orthographic    = true;
        _cam.orthographicSize = maxZoom;
    }

    void Start()
    {
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody>();
    }

    void OnDestroy()
    {
        if (OutputTexture != null) OutputTexture.Release();
        if (Instance == this) Instance = null;
    }

    // ── Public API (mirrors CameraFollow) ─────────────────────────────────────

    public void RegisterTrigger(Transform planet)   => _activePlanets.Add(planet);
    public void UnregisterTrigger(Transform planet) => _activePlanets.Remove(planet);

    // ── Follow logic ──────────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (player == null) return;

        Transform focusPlanet = ClosestActivePlanet();
        Vector3 targetPos;
        float   targetZoom;

        if (focusPlanet != null)
        {
            // Static lock onto planet — no follow, no zoom
            transform.position    = WithZ(focusPlanet.position, transform.position.z);
            _cam.orthographicSize = focusZoom;
            _followVelocity       = Vector3.zero;
            _zoomVelocity         = 0f;
            return;
        }

        // Follow ship; zoom based on speed
        targetPos = WithZ(player.position, transform.position.z);
        float speed = _playerRb != null ? _playerRb.linearVelocity.magnitude : 0f;
        targetZoom  = Mathf.Lerp(minZoom, maxZoom, speed / maxPlayerSpeed);

        transform.position    = Vector3.SmoothDamp(transform.position, targetPos, ref _followVelocity, followSmoothTime);
        _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, targetZoom, ref _zoomVelocity, zoomSmoothTime);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Transform ClosestActivePlanet()
    {
        Transform closest = null;
        float     minDist = float.MaxValue;
        foreach (Transform planet in _activePlanets)
        {
            float dist = Vector3.Distance(player.position, planet.position);
            if (dist < minDist) { minDist = dist; closest = planet; }
        }
        return closest;
    }

    static Vector3 WithZ(Vector3 v, float z) => new(v.x, v.y, z);

    // ── Scene gizmo ───────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        float size   = _cam != null ? _cam.orthographicSize : maxZoom;
        float aspect = textureHeight > 0 ? (float)textureWidth / textureHeight : 1f;

        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireCube(
            new Vector3(transform.position.x, transform.position.y, 0f),
            new Vector3(size * 2f * aspect, size * 2f, 0.1f));
    }
}