using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class GameTimer : MonoBehaviour
{
    [Header("References")]
    public CheckpointManager checkpointManager;
    public TextMeshProUGUI timerText;
    public LeaderboardUI leaderboardUI;

    private float elapsed = 0f;
    private bool running = true;

    void Start()
    {
        FindLeaderboard();
    }

    void Update()
    {
        // Re-find leaderboardUI if it's missing (happens on scene loads with persistent managers)
        if (leaderboardUI == null)
        {
            FindLeaderboard();
        }

        if (!running) return;

        if (checkpointManager != null && checkpointManager.IsComplete)
        {
            running = false;
            TriggerWin();
        }

        elapsed += Time.deltaTime;
        UpdateUI();
    }

    private void FindLeaderboard()
    {
        // Try finding active object first (fastest)
        leaderboardUI = Object.FindFirstObjectByType<LeaderboardUI>();

        // If not found, try finding inactive (slower but thorough)
        if (leaderboardUI == null)
        {
            leaderboardUI = Object.FindFirstObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
        }

        if (leaderboardUI != null)
        {
            Debug.Log($"[GameTimer] Successfully found LeaderboardUI: {leaderboardUI.name}");
        }
    }

    private void TriggerWin()
    {
        Debug.Log($"[GameTimer] TriggerWin called. Elapsed: {elapsed}");

        // Pause the game on win
        Time.timeScale = 0f;

        if (leaderboardUI != null)
        {
            Debug.Log($"[GameTimer] Calling leaderboardUI.ShowEntry. UI Object: {leaderboardUI.gameObject.name}, Active: {leaderboardUI.gameObject.activeInHierarchy}");
            leaderboardUI.ShowEntry(elapsed);
        }
        else
        {
            Debug.LogError("[GameTimer] LeaderboardUI reference is MISSING!");
        }
    }

    void UpdateUI()
    {
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        int hundredths  = (int)((elapsed * 100f) % 100f);
        timerText.text = $"Time: {minutes:00}:{seconds:00}.{hundredths}";
    }
}
