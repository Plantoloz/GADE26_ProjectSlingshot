using System.Collections.Generic;
using UnityEngine;

public class AsteroidManager : MonoBehaviour
{
    public GameObject asteroidPrefab;
    public Transform player;
    public MapPath mapPath;
    public bool invertPathMode = false; // If true, asteroids ONLY spawn on tiles. If false, they spawn EVERYWHERE EXCEPT on tiles.

    [Header("Grid Settings")]
    public float cellSize = 10f;          
    public float viewDistance = 100f;     // Visual distance
    public float physicsRadius = 35f;     // Distance for active physics
    public float positionJitter = 4f;    

    private Dictionary<Vector2Int, GameObject> activeAsteroids = new Dictionary<Vector2Int, GameObject>();
    private Stack<GameObject> asteroidPool = new Stack<GameObject>();

    void Update()
    {
        if (player == null) return;

        Vector2Int playerCell = WorldToCell(player.position);
        int cellRadius = Mathf.CeilToInt(viewDistance / cellSize);
        HashSet<Vector2Int> requiredCells = new HashSet<Vector2Int>();

        // 1. Scan for cells
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int cellCoord = new Vector2Int(playerCell.x + x, playerCell.y + y);
                Vector3 cellWorldPos = CellToWorld(cellCoord);

                float dist = Vector3.Distance(player.position, cellWorldPos);
                if (dist <= viewDistance)
                {
                    if (ShouldSpawnAt(cellCoord, cellWorldPos))
                    {
                        requiredCells.Add(cellCoord);
                    }
                }
            }
        }

        // 2. Despawn/Release to Pool
        List<Vector2Int> toRelease = new List<Vector2Int>();
        foreach (var entry in activeAsteroids)
        {
            if (!requiredCells.Contains(entry.Key))
            {
                toRelease.Add(entry.Key);
            }
            else
            {
                // Manage Physics Bubble for still-active asteroids
                float dist = Vector3.Distance(player.position, entry.Value.transform.position);
                AsteroidProperties props = entry.Value.GetComponent<AsteroidProperties>();
                if (props != null)
                {
                    props.SetPhysicsActive(dist <= physicsRadius);
                    props.UpdateManualDrift(Time.deltaTime);
                }
            }
        }

        foreach (var cell in toRelease)
        {
            ReleaseToPool(cell);
        }

        // 3. Get from Pool
        foreach (var cell in requiredCells)
        {
            if (!activeAsteroids.ContainsKey(cell))
            {
                SpawnFromPool(cell);
            }
        }
    }

    private void SpawnFromPool(Vector2Int cell)
    {
        GameObject asteroid;
        if (asteroidPool.Count > 0)
        {
            asteroid = asteroidPool.Pop();
            asteroid.SetActive(true);
        }
        else
        {
            asteroid = Instantiate(asteroidPrefab, transform);
        }

        int seed = cell.x * 73856093 ^ cell.y * 19349663; 
        System.Random prng = new System.Random(seed);
        float offsetX = (float)(prng.NextDouble() * 2 - 1) * positionJitter;
        float offsetY = (float)(prng.NextDouble() * 2 - 1) * positionJitter;
        asteroid.transform.position = CellToWorld(cell) + new Vector3(offsetX, offsetY, 0);

        AsteroidProperties props = asteroid.GetComponent<AsteroidProperties>();
        if (props != null)
        {
            props.InitializeWithSeed(seed);
            props.SetPhysicsActive(false); // Start kinematic until physics bubble check
        }

        activeAsteroids.Add(cell, asteroid);
    }

    private void ReleaseToPool(Vector2Int cell)
    {
        GameObject asteroid = activeAsteroids[cell];
        asteroid.SetActive(false);
        asteroidPool.Push(asteroid);
        activeAsteroids.Remove(cell);
    }

    private bool ShouldSpawnAt(Vector2Int _, Vector3 worldPos)
    {
        if (mapPath != null)
        {
            bool isInside = mapPath.IsPositionInsidePath(worldPos);
            if (invertPathMode ? !isInside : isInside) return false;
        }

        return true;
    }

    private Vector2Int WorldToCell(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x / cellSize), Mathf.FloorToInt(pos.y / cellSize));
    private Vector3 CellToWorld(Vector2Int cell) => new Vector3(cell.x * cellSize + cellSize / 2f, cell.y * cellSize + cellSize / 2f, 0);
}
