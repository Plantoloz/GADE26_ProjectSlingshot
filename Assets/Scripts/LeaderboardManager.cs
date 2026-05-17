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
        currentData.entries.Add(new LeaderboardEntry { playerName = name, time = time });
        
        // Sort by time (ascending, since lower time is better in racing/slingshot)
        currentData.entries = currentData.entries
            .OrderBy(e => e.time)
            .Take(MaxEntries)
            .ToList();

        SaveLeaderboard();
    }

    public List<LeaderboardEntry> GetEntries()
    {
        return currentData.entries;
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
