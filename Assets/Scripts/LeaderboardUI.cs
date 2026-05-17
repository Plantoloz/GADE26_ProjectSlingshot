using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Input UI")]
    public GameObject inputPanel;
    public TMP_InputField nameInputField;
    public Button submitButton;

    [Header("Display UI")]
    public GameObject displayPanel;
    public TextMeshProUGUI leaderboardText;
    public Button closeButton;

    private float finalTime;

    private void Start()
    {
        inputPanel.SetActive(false);
        displayPanel.SetActive(false);
        submitButton.onClick.AddListener(OnSubmit);
        // Listener is now managed by InGameMenuManager for better navigation flow
    }

    public void ShowEntry(float time)
    {
        Debug.Log($"[LeaderboardUI] ShowEntry called. Time: {time}");
        finalTime = time;
        if (inputPanel != null)
        {
            inputPanel.SetActive(true);
            Debug.Log($"[LeaderboardUI] inputPanel activated. Object name: {inputPanel.name}");
        }
        else Debug.LogError("[LeaderboardUI] inputPanel reference is MISSING!");

        if (displayPanel != null) displayPanel.SetActive(false);
        
        CheckForEventSystem();
    }

    private void OnSubmit()
    {
        Debug.Log("[LeaderboardUI] OnSubmit clicked.");
        string playerName = string.IsNullOrEmpty(nameInputField.text) ? "Anonymous" : nameInputField.text;
        
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddEntry(playerName, finalTime);
        }

        if (inputPanel != null) inputPanel.SetActive(false);
        ShowLeaderboard();
    }

    public void ShowLeaderboard()
    {
        Debug.Log("[LeaderboardUI] ShowLeaderboard called.");
        if (displayPanel != null)
        {
            displayPanel.SetActive(true);
            Debug.Log($"[LeaderboardUI] displayPanel activated. Object name: {displayPanel.name}");
        }
        else Debug.LogError("[LeaderboardUI] displayPanel reference is MISSING!");

        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardUI] LeaderboardManager.Instance is null!");
            return;
        }

        List<LeaderboardEntry> entries = LeaderboardManager.Instance.GetEntries();
        leaderboardText.text = "RANKED TIMES\n\n";

        for (int i = 0; i < entries.Count; i++)
        {
            float t = entries[i].time;
            int minutes = (int)(t / 60f);
            int seconds = (int)(t % 60f);
            int hundredths = (int)((t * 100f) % 100f);
            
            leaderboardText.text += $"{i + 1}. {entries[i].playerName} - {minutes:00}:{seconds:00}.{hundredths}\n";
        }
        
        CheckForEventSystem();
    }

    private void CheckForEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("[LeaderboardUI] NO EVENTSYSTEM FOUND IN SCENE! UI will not be interactive.");
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
