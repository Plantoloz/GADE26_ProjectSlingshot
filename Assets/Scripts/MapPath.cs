using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapPath : MonoBehaviour
{
    [Header("Tilemap Settings (Drawing in Scene)")]
    public bool useTilemap = false;
    public Tilemap pathTilemap;

    [Header("Texture Mask Settings")]
    public bool useTextureMask = false;
    public Texture2D pathMask;
    public Vector2 mapWorldSize = new Vector2(200, 200); 
    public Vector2 mapWorldOffset = Vector2.zero;      
    [Range(0, 1)]
    public float maskThreshold = 0.5f;                 

    [Header("Waypoint Settings (Fallback)")]
    public List<Vector3> points = new List<Vector3>();
    public float pathWidth = 10f;
    public Color pathColor = Color.cyan;

    [ContextMenu("Add Current Position to Path")]
    private void AddCurrentPosition()
    {
        points.Add(transform.position);
    }

    /// <summary>
    /// Checks if a given position is within the path's width, mask, or tilemap.
    /// </summary>
    public bool IsPositionInsidePath(Vector3 position)
    {
        // 1. Tilemap Check (Fastest for in-editor drawing)
        if (useTilemap && pathTilemap != null)
        {
            Vector3Int cellPos = pathTilemap.WorldToCell(position);
            return pathTilemap.HasTile(cellPos);
        }

        // 2. Texture Mask Check
        if (useTextureMask && pathMask != null)
        {
            return IsInsideTextureMask(position);
        }

        // 3. Waypoint Check
        if (points != null && points.Count >= 2)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (IsPointNearSegment(position, points[i], points[i + 1], pathWidth))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private bool IsInsideTextureMask(Vector3 position)
    {
        // 1. Convert world position to 0-1 UV coordinates
        float u = (position.x - mapWorldOffset.x + mapWorldSize.x / 2f) / mapWorldSize.x;
        float v = (position.y - mapWorldOffset.y + mapWorldSize.y / 2f) / mapWorldSize.y;

        // 2. If out of bounds of the map, it's not in the path
        if (u < 0 || u > 1 || v < 0 || v > 1) return false;

        // 3. Sample the texture (Requires "Read/Write Enabled" in Import Settings)
        try 
        {
            Color pixel = pathMask.GetPixelBilinear(u, v);
            // Assuming Black (0) is path and White (1) is asteroids
            return pixel.grayscale < maskThreshold;
        }
        catch (System.Exception)
        {
            Debug.LogError("MapPath: Ensure 'Read/Write Enabled' is checked in the Texture Import Settings for " + pathMask.name);
            useTextureMask = false;
            return false;
        }
    }

    private bool IsPointNearSegment(Vector3 p, Vector3 a, Vector3 b, float threshold)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq == 0) return Vector3.Distance(p, a) < threshold;

        // Project p onto line ab, clamped between 0 and 1
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / lengthSq);
        Vector3 projection = a + t * ab;

        return Vector3.Distance(p, projection) < threshold;
    }

    private void OnDrawGizmos()
    {
        // Draw Map Bounds
        Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.2f);
        Gizmos.DrawWireCube(new Vector3(mapWorldOffset.x, mapWorldOffset.y, 0), new Vector3(mapWorldSize.x, mapWorldSize.y, 0.1f));

        if (useTextureMask || useTilemap) return;

        if (points == null || points.Count < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
            // Draw path boundaries
            Vector3 dir = (points[i+1] - points[i]).normalized;
            Vector3 normal = new Vector3(-dir.y, dir.x, 0) * pathWidth;
            
            Gizmos.DrawLine(points[i] + normal, points[i+1] + normal);
            Gizmos.DrawLine(points[i] - normal, points[i+1] - normal);
        }

        // Draw circles at waypoints for better visibility
        foreach (var point in points)
        {
            Gizmos.DrawWireSphere(point, 1f);
        }
    }
}
