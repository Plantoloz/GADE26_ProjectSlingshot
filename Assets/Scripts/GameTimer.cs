using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("References")]
    public CheckpointManager checkpointManager;
    public TextMeshProUGUI timerText;

    private float elapsed = 0f;
    private bool running = true;

    void Update()
    {
        if (!running) return;

        if (checkpointManager != null && checkpointManager.IsComplete)
            running = false;

        elapsed += Time.deltaTime;
        UpdateUI();
    }

    void UpdateUI()
    {
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        int hundredths  = (int)((elapsed * 100f) % 100f);
        timerText.text = $"{minutes:00}:{seconds:00}.{hundredths}";
    }
}
