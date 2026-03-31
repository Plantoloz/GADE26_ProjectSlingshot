using System.Collections.Generic;
using UnityEngine;

public class AsteroidManager : MonoBehaviour
{
    public GameObject asteroidPrefab;
    public Transform player;
    public int targetAsteroidCount = 10;
    
    public float spawnRadius = 25f; 
    public float despawnRadius = 35f; 

    private List<GameObject> activeAsteroids = new List<GameObject>();

    void Update()
    {
        if (player == null) return;

        for (int i = activeAsteroids.Count - 1; i >= 0; i--)
        {
            GameObject asteroid = activeAsteroids[i];
            
            if (asteroid == null) 
            {
                activeAsteroids.RemoveAt(i);
                continue;
            }

            if (Vector3.Distance(player.position, asteroid.transform.position) > despawnRadius)
            {
                Destroy(asteroid);
                activeAsteroids.RemoveAt(i);
            }
        }

        while (activeAsteroids.Count < targetAsteroidCount)
        {
            SpawnAsteroid();
        }
    }

    void SpawnAsteroid()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = player.position + new Vector3(randomDir.x, randomDir.y, 0) * spawnRadius;
        
        // Instantiate and set parent to this manager's transform in one line
        GameObject newAsteroid = Instantiate(asteroidPrefab, spawnPos, Quaternion.identity, transform);
        newAsteroid.tag = "Asteroid";
        activeAsteroids.Add(newAsteroid);
    }
}