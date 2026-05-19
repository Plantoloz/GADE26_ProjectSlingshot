using UnityEngine;

/// <summary>
/// Add to any scene object (planet, ship, checkpoint) to display an icon on the minimap.
/// Creates a child SpriteRenderer on the "Minimap" layer — invisible to the main camera.
///
/// Requirements: a layer named "Minimap" must exist in Project Settings → Tags and Layers.
/// </summary>
[ExecuteAlways]
public class MinimapIcon : MonoBehaviour
{
    [Header("Icon")]
    public Sprite sprite;
    public Color  color     = Color.white;
    [Min(0.1f), Tooltip("Size in world units on the minimap")]
    public float  worldSize = 5f;

    [Header("Settings")]
    public string minimapLayer = "Minimap";
    [Tooltip("Offset from the parent object's position")]
    public Vector3 worldOffset = Vector3.zero;

    private GameObject     _iconGO;
    private SpriteRenderer _sr;

    void OnEnable()  => EnsureIcon();
    void OnDisable() => SetIconActive(false);
    void OnDestroy() => DestroyIcon();

    void Update()
    {
        if (_iconGO == null) EnsureIcon();
        if (_sr == null) return;

        _iconGO.transform.position   = transform.position + worldOffset;
        _iconGO.transform.rotation   = Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
        _iconGO.transform.localScale = Vector3.one * worldSize;
        _sr.sprite = sprite;
        _sr.color  = color;
    }

    void EnsureIcon()
    {
        if (_iconGO != null) { SetIconActive(true); return; }

        _iconGO = new GameObject($"[MinimapIcon:{gameObject.name}]")
        {
            layer    = GetMinimapLayer(),
            hideFlags = HideFlags.DontSave // keep out of scene save, managed by this component
        };

        _sr           = _iconGO.AddComponent<SpriteRenderer>();
        _sr.sprite    = sprite;
        _sr.color     = color;
        _sr.sortingOrder = 1;
        _iconGO.transform.localScale = Vector3.one * worldSize;
    }

    void SetIconActive(bool active)
    {
        if (_iconGO != null) _iconGO.SetActive(active);
    }

    void DestroyIcon()
    {
        if (_iconGO == null) return;
        if (Application.isPlaying) Destroy(_iconGO);
        else DestroyImmediate(_iconGO);
        _iconGO = null;
    }

    int GetMinimapLayer()
    {
        int layer = LayerMask.NameToLayer(minimapLayer);
        if (layer < 0)
        {
            Debug.LogWarning($"[MinimapIcon] Layer '{minimapLayer}' not found. Add it in Project Settings → Tags and Layers.", this);
            return 0;
        }
        return layer;
    }
}