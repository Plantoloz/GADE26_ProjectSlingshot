using UnityEngine;

[ExecuteAlways]
public class TrajectoryManager : MonoBehaviour
{
    [Header("Sources")]
    public CheckpointSpawner   spawner;
    public TrajectoryPredictor predictor;
    public ShipController      ship;

    [Header("Simulation")]
    public int   predictionSteps = 600;
    public float stepTime        = 0.1f;

    [Header("Static Line")]
    public LineRenderer staticLine;
    public Color staticColor = Color.white;
    public Vector3      staticOffset = Vector3.zero;
    [Range(0f, 1f)] public float dashRatio = 0.5f;
    [Min(0.0001f)]  public float dashScale = 0.001f;
    [Tooltip("Reference to determine which checkpoint was last collected.")]
    public CheckpointManager checkpointManager;
    [Tooltip("How many simulation steps to show ahead of the last checkpoint.")]
    [Min(2)] public int visibleSteps = 80;

    [Header("Live Line")]
    public LineRenderer liveLine;
    public Gradient     liveGradient;
    [Min(0.1f)] public float liveWidth = 5f;

    [Header("Minimap Line")]
    public LineRenderer minimapLine;
    public Gradient     minimapGradient;
    [Min(0.1f)] public float minimapWidth = 3f;
    public string minimapLayer = "Minimap";

    // Full simulated path for play-mode window rendering
    private Vector3[] _allSimPoints;
    private int       _allSimCount;
    private int[]     _checkpointPathIndices;
    private int       _lastKnownCheckpointIndex = -1;

    void Reset()
    {
        liveGradient    = MakeGradient(new Color(0.3f, 0.9f, 1f, 0.9f), new Color(0.3f, 0.9f, 1f, 0f));
        minimapGradient = MakeGradient(new Color(0.3f, 0.9f, 1f, 0.9f), new Color(0.3f, 0.9f, 1f, 0.3f));
    }

    void OnEnable()
    {
        RefreshMinimapLine();
        ConfigureLiveLine();
    }

    void OnValidate()
    {
        RefreshMinimapLine();
        ConfigureLiveLine();
    }

    void Start()
    {
        if (!Application.isPlaying) return;

        SimulateFullPath();
        PrecomputeCheckpointIndices();
        _lastKnownCheckpointIndex = -1;
        UpdateStaticLineWindow();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;

        UpdateLiveLine();

        int currentIdx = checkpointManager != null ? checkpointManager.CurrentIndex : 0;
        if (currentIdx != _lastKnownCheckpointIndex)
            UpdateStaticLineWindow();
    }

    // ── Static line: full simulation stored, window shown per checkpoint ─────

    void SimulateFullPath()
    {
        _allSimPoints = new Vector3[predictionSteps];
        _allSimCount  = 0;

        if (ship == null) return;

        Vector3 pos = ship.transform.position;
        Vector3 vel = ship.StartVelocity;

        for (int i = 0; i < predictionSteps; i++)
        {
            if (!float.IsFinite(pos.x) || !float.IsFinite(pos.y)) break;
            _allSimPoints[_allSimCount++] = pos;

            Vector3 gravAccel = Vector3.zero;
            foreach (var body in GravityBody.allBodies)
            {
                if (body == null || !body.isAttractor) continue;
                Vector3 dir  = body.rb.position - pos;
                float   dist = dir.magnitude;
                if (dist > 0.01f && dist < body.influenceRadius)
                {
                    float mag = GravityBody.GRAVITY_CONSTANT * body.rb.mass
                                / Mathf.Pow(Mathf.Max(dist, GravityBody.minForceDistance), 2f);
                    gravAccel += dir.normalized * mag;
                }
            }

            vel += gravAccel * stepTime;
            pos += vel * stepTime;
        }
    }

    void PrecomputeCheckpointIndices()
    {
        if (checkpointManager == null || checkpointManager.checkpoints == null)
        {
            _checkpointPathIndices = new int[0];
            return;
        }

        int n = checkpointManager.checkpoints.Count;
        _checkpointPathIndices = new int[n];

        int searchFrom = 0;
        for (int c = 0; c < n; c++)
        {
            var cp = checkpointManager.checkpoints[c];
            if (cp == null) { _checkpointPathIndices[c] = searchFrom; continue; }

            Vector3 cpPos    = cp.transform.position;
            float   bestDist = float.MaxValue;
            int     bestIdx  = searchFrom;

            for (int p = searchFrom; p < _allSimCount; p++)
            {
                float d = (cpPos - _allSimPoints[p]).sqrMagnitude;
                if (d < bestDist) { bestDist = d; bestIdx = p; }
            }

            _checkpointPathIndices[c] = bestIdx;
            searchFrom = bestIdx; // checkpoints are ordered — don't search backwards
        }
    }

    void UpdateStaticLineWindow()
    {
        if (staticLine == null || _allSimPoints == null || _allSimCount < 2) return;

        int currentIdx = checkpointManager != null ? checkpointManager.CurrentIndex : 0;
        _lastKnownCheckpointIndex = currentIdx;

        // Index of last collected checkpoint in the path (0 = ship start before first checkpoint)
        int startPathIdx = 0;
        if (checkpointManager != null
            && _checkpointPathIndices != null
            && currentIdx > 0
            && currentIdx - 1 < _checkpointPathIndices.Length)
        {
            startPathIdx = _checkpointPathIndices[currentIdx - 1];
        }

        int count = Mathf.Min(visibleSteps, _allSimCount - startPathIdx);
        if (count < 2) { staticLine.positionCount = 0; return; }

        staticLine.useWorldSpace  = true;
        staticLine.positionCount  = count;
        for (int i = 0; i < count; i++)
            staticLine.SetPosition(i, _allSimPoints[startPathIdx + i]);

        ApplyDashedMaterial(staticLine, staticColor, dashRatio, dashScale);
        ApplyFadeGradient(staticLine, staticColor);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public Vector3 GetSimulatedVelocityAtCheckpoint(int checkpointIndex)
    {
        if (_checkpointPathIndices == null || _allSimPoints == null) return Vector3.zero;
        if (checkpointIndex < 0 || checkpointIndex >= _checkpointPathIndices.Length) return Vector3.zero;

        int idx = _checkpointPathIndices[checkpointIndex];
        if (idx + 1 >= _allSimCount) return Vector3.zero;

        return (_allSimPoints[idx + 1] - _allSimPoints[idx]) / stepTime;
    }

    // ── Public API — called by editor bake step only ─────────────────────────

    public void RefreshStaticLine()
    {
        if (staticLine == null) return;

        staticLine.useWorldSpace = true;

        if (spawner != null && spawner.bakedPath != null && spawner.bakedPath.Count >= 2)
        {
            staticLine.positionCount = spawner.bakedPath.Count;
            Vector3[] offsetPoints = new Vector3[spawner.bakedPath.Count];
            for (int i = 0; i < spawner.bakedPath.Count; i++)
                offsetPoints[i] = spawner.bakedPath[i] + staticOffset;
            
            staticLine.SetPositions(offsetPoints);
        }

        ApplyDashedMaterial(staticLine, staticColor, dashRatio, dashScale);
    }

    public void RefreshMinimapLine()
    {
        if (minimapLine == null) return;

        int layerIdx = LayerMask.NameToLayer(minimapLayer);
        if (layerIdx >= 0) minimapLine.gameObject.layer = layerIdx;

        EnsureSolidMaterial(minimapLine, "MinimapLineMat");
        if (minimapGradient != null) minimapLine.colorGradient = minimapGradient;
        minimapLine.startWidth         = minimapWidth;
        minimapLine.endWidth           = minimapWidth;
        minimapLine.textureMode        = LineTextureMode.Stretch;
        minimapLine.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        minimapLine.receiveShadows     = false;

        var path = spawner != null ? spawner.mapPath : null;
        if (path == null || path.points == null || path.points.Count < 2)
        {
            minimapLine.positionCount = 0;
            return;
        }

        minimapLine.positionCount = path.points.Count;
        for (int i = 0; i < path.points.Count; i++)
            minimapLine.SetPosition(i, new Vector3(path.points[i].x, path.points[i].y, 0f));
    }

    // ── Private ───────────────────────────────────────────────────────────────

    void ConfigureLiveLine()
    {
        if (liveLine == null) return;

        EnsureSolidMaterial(liveLine, "LiveLineMat");

        if (liveGradient != null) liveLine.colorGradient = liveGradient;
        liveLine.startWidth        = liveWidth;
        liveLine.endWidth          = liveWidth * 0.3f;
        liveLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        liveLine.receiveShadows    = false;
    }

    void UpdateLiveLine()
    {
        if (liveLine == null || predictor == null) return;

        int count = predictor.SimulatedPointCount;
        if (count < 2) { liveLine.positionCount = 0; return; }

        liveLine.positionCount = count;
        var pts = predictor.SimulatedPoints;
        for (int i = 0; i < count; i++)
            liveLine.SetPosition(i, pts[i]);
    }

    static Gradient MakeGradient(Color start, Color end)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) }
        );
        return g;
    }

    static void ApplyFadeGradient(LineRenderer line, Color color)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        line.colorGradient = gradient;
    }

    static void ApplyDashedMaterial(LineRenderer line, Color color, float dashRatio, float dashScale)
    {
        const int texW   = 64;
        int       dashPx = Mathf.Clamp(Mathf.RoundToInt(texW * dashRatio), 1, texW - 1);

        var tex = new Texture2D(texW, 1, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
            hideFlags  = HideFlags.HideAndDontSave
        };
        var pixels = new Color[texW];
        for (int i = 0; i < texW; i++)
            pixels[i] = i < dashPx ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();

        var mat = new Material(Shader.Find("Sprites/Default"))
        {
            name      = "StaticLineMat",
            hideFlags = HideFlags.HideAndDontSave
        };
        mat.mainTexture = tex;
        mat.color       = color;

        line.sharedMaterial = mat;
        line.textureMode    = LineTextureMode.Tile;
        line.textureScale   = new Vector2(dashScale, 1f);
    }

    static void EnsureSolidMaterial(LineRenderer line, string matName)
    {
        if (line.sharedMaterial != null && line.sharedMaterial.name == matName) return;
        line.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            name      = matName,
            hideFlags = HideFlags.HideAndDontSave,
            color     = Color.white
        };
    }
}