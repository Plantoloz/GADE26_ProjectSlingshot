using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public float time;
    public string sceneName;
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private string savePath;
    private const int MaxEntries = 10;
    private LeaderboardData currentData = new LeaderboardData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
            LoadLeaderboard();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddEntry(string name, float time)
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        currentData.entries.Add(new LeaderboardEntry { playerName = name, time = time, sceneName = scene });

        // Keep only the top MaxEntries per scene
        var sceneEntries = currentData.entries
            .Where(e => e.sceneName == scene)
            .OrderBy(e => e.time)
            .Take(MaxEntries)
            .ToList();

        currentData.entries.RemoveAll(e => e.sceneName == scene);
        currentData.entries.AddRange(sceneEntries);

        SaveLeaderboard();
    }

    public List<LeaderboardEntry> GetEntries()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return currentData.entries
            .Where(e => e.sceneName == scene)
            .OrderBy(e => e.time)
            .ToList();
    }

    private void SaveLeaderboard()
    {
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(savePath, json);
    }

    private void LoadLeaderboard()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            currentData = JsonUtility.FromJson<LeaderboardData>(json);
        }
        else
        {
            currentData = new LeaderboardData();
        }
    }
}
