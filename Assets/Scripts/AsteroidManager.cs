using System.Collections.Generic;
using UnityEngine;

public class AsteroidManager : MonoBehaviour
{
    public GameObject asteroidPrefab;
    public Transform player;
    public MapPath mapPath;

    [Header("Noise Map Settings")]
    public float noiseScale = 0.05f;      // How "stretched" the noise is
    public float noiseThreshold = 0.6f;  // Higher = fewer asteroids (0 to 1)
    
    [Header("Grid Settings")]
    public float cellSize = 8f;          // Size of each potential asteroid "slot"
    public float viewDistance = 60f;     // How far to spawn/keep asteroids
    
    [Header("Visual Randomness")]
    public float positionJitter = 3f;    // Max offset from cell center

    private Dictionary<Vector2Int, GameObject> activeAsteroids = new Dictionary<Vector2Int, GameObject>();

    void Update()
    {
        if (player == null) return;

        // 1. Calculate the range of cells around the player
        Vector2Int playerCell = WorldToCell(player.position);
        int cellRadius = Mathf.CeilToInt(viewDistance / cellSize);

        HashSet<Vector2Int> requiredCells = new HashSet<Vector2Int>();

        // 2. Scan for cells that SHOULD have an asteroid
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int cellCoord = new Vector2Int(playerCell.x + x, playerCell.y + y);
                Vector3 cellWorldPos = CellToWorld(cellCoord);

                // Check distance
                if (Vector3.Distance(player.position, cellWorldPos) <= viewDistance)
                {
                    if (ShouldSpawnAt(cellCoord, cellWorldPos))
                    {
                        requiredCells.Add(cellCoord);
                    }
                }
            }
        }

        // 3. Despawn asteroids that are no longer needed
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var entry in activeAsteroids)
        {
            if (!requiredCells.Contains(entry.Key))
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (var cell in toRemove)
        {
            if (activeAsteroids[cell] != null)
            {
                Destroy(activeAsteroids[cell]);
            }
            activeAsteroids.Remove(cell);
        }

        // 4. Spawn new asteroids in required cells
        foreach (var cell in requiredCells)
        {
            if (!activeAsteroids.ContainsKey(cell))
            {
                SpawnAsteroid(cell);
            }
        }
    }

    private bool ShouldSpawnAt(Vector2Int cell, Vector3 worldPos)
    {
        // Use Perlin noise to determine if this cell has an asteroid
        // We use the cell coordinates to ensure the noise is stable/deterministic
        float noise = Mathf.PerlinNoise(cell.x * noiseScale + 1000f, cell.y * noiseScale + 1000f);
        
        if (noise < noiseThreshold) return false;

        // Still respect the "Empty Path" logic
        if (mapPath != null && mapPath.IsPositionInsidePath(worldPos))
        {
            return false;
        }

        return true;
    }

    private void SpawnAsteroid(Vector2Int cell)
    {
        // Use a deterministic seed based on cell coordinates for all "random" properties
        // This ensures the same asteroid spawns in the same spot with the same size every time
        int seed = cell.x * 73856093 ^ cell.y * 19349663; 
        System.Random prng = new System.Random(seed);

        // Jitter the position within the cell so it doesn't look like a perfect grid
        float offsetX = (float)(prng.NextDouble() * 2 - 1) * positionJitter;
        float offsetY = (float)(prng.NextDouble() * 2 - 1) * positionJitter;
        Vector3 spawnPos = CellToWorld(cell) + new Vector3(offsetX, offsetY, 0);

        GameObject newAsteroid = Instantiate(asteroidPrefab, spawnPos, Quaternion.identity, transform);
        newAsteroid.tag = "Asteroid";
        
        // Pass the seed to the asteroid properties so it can randomize itself deterministically
        AsteroidProperties props = newAsteroid.GetComponent<AsteroidProperties>();
        if (props != null)
        {
            props.InitializeWithSeed(seed);
        }

        activeAsteroids.Add(cell, newAsteroid);
    }

    private Vector2Int WorldToCell(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x / cellSize), Mathf.FloorToInt(pos.y / cellSize));
    private Vector3 CellToWorld(Vector2Int cell) => new Vector3(cell.x * cellSize + cellSize / 2f, cell.y * cellSize + cellSize / 2f, 0);
}
