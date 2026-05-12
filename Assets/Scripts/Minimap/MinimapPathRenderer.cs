using UnityEngine;

/// <summary>
/// Reads waypoints from a MapPath component and renders them as a line on the minimap.
/// Place this anywhere in the scene and assign the MapPath source.
///
/// The LineRenderer is placed on the "Minimap" layer so only the minimap camera sees it.
/// Call Rebuild() at runtime if the path changes dynamically.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MinimapPathRenderer : MonoBehaviour
{
    [Header("Source")]
    public MapPath mapPath;

    [Header("Visual")]
    public Color startColor = new Color(0.3f, 0.9f, 1f, 0.9f);
    public Color endColor   = new Color(0.3f, 0.9f, 1f, 0.3f);
    [Min(0.1f)] public float lineWidth = 3f;

    [Header("Settings")]
    public string minimapLayer = "Minimap";
    [Tooltip("If true, the path loops back from last point to first")]
    public bool closedLoop = false;

    private LineRenderer _line;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        gameObject.layer = GetMinimapLayer();
        ConfigureRenderer();
        Rebuild();
    }

    void ConfigureRenderer()
    {
        _line.useWorldSpace  = true;
        _line.loop           = closedLoop;
        _line.startWidth     = lineWidth;
        _line.endWidth       = lineWidth;
        _line.startColor     = startColor;
        _line.endColor       = endColor;
        _line.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows     = false;
        _line.material           = new Material(Shader.Find("Sprites/Default"));
        _line.material.color     = Color.white;
        _line.sortingOrder       = 0;
    }

    /// <summary>Rebuild the path line from current MapPath waypoints.</summary>
    public void Rebuild()
    {
        if (_line == null) return;

        if (mapPath == null || mapPath.points == null || mapPath.points.Count < 2)
        {
            _line.positionCount = 0;
            return;
        }

        _line.positionCount = mapPath.points.Count;
        for (int i = 0; i < mapPath.points.Count; i++)
        {
            var p = mapPath.points[i];
            _line.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }

        _line.startColor = startColor;
        _line.endColor   = endColor;
        _line.startWidth = lineWidth;
        _line.endWidth   = lineWidth;
        _line.loop       = closedLoop;
    }

    int GetMinimapLayer()
    {
        int layer = LayerMask.NameToLayer(minimapLayer);
        if (layer < 0)
        {
            Debug.LogWarning($"[MinimapPathRenderer] Layer '{minimapLayer}' not found. Add it in Project Settings → Tags and Layers.", this);
            return 0;
        }
        return layer;
    }

    #if UNITY_EDITOR
    void OnValidate()
    {
        if (_line == null) _line = GetComponent<LineRenderer>();
        if (_line != null) Rebuild();
    }
    #endif
}