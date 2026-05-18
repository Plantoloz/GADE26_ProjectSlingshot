using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InGameMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject pauseMenuPanel;
    public GameObject optionsPanel;
    public LeaderboardUI leaderboardUI;
    public Button optionsBackButton;

    [Header("Audio Sliders (Optional)")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    private bool isPaused = false;

    private void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        
        InitializeVolumeSliders();
        SetupHandlers();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[InGameMenuManager] Scene Loaded: {scene.name}. Reconnecting references...");
        
        // Reset state
        isPaused = false;
        Time.timeScale = 1f;

        // Dynamically find references in the new scene
        FindReferences();
        
        // Hide panels initially
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        
        SetupHandlers();
    }

    private void FindReferences()
    {
        // 1. Find the LeaderboardUI if it's missing
        if (leaderboardUI == null)
        {
            leaderboardUI = Object.FindFirstObjectByType<LeaderboardUI>();
            if (leaderboardUI != null) Debug.Log("[InGameMenuManager] Found LeaderboardUI in scene.");
        }

        // 2. If we are missing panel references, try to find them by name
        if (pauseMenuPanel == null)
        {
            GameObject found = GameObject.Find("Pause Menu");
            if (found != null) pauseMenuPanel = found;
        }

        if (optionsPanel == null)
        {
            GameObject found = GameObject.Find("Options Menu");
            if (found != null) optionsPanel = found;
        }

        // 3. Find Options Back Button if missing
        if (optionsBackButton == null && optionsPanel != null)
        {
            Button[] buttons = optionsPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                if (b.name == "Back" || b.name == "BackButton" || b.name == "Close")
                {
                    optionsBackButton = b;
                    break;
                }
            }
        }
    }

    private void SetupHandlers()
    {
        if (leaderboardUI != null && leaderboardUI.closeButton != null)
        {
            leaderboardUI.closeButton.onClick.RemoveAllListeners();
            leaderboardUI.closeButton.onClick.AddListener(OnLeaderboardBack);
        }

        if (optionsBackButton != null)
        {
            optionsBackButton.onClick.RemoveAllListeners();
            optionsBackButton.onClick.AddListener(HideOptions);
        }
    }

    private void OnLeaderboardBack()
    {
        if (leaderboardUI != null && leaderboardUI.displayPanel != null) 
            leaderboardUI.displayPanel.SetActive(false);
        
        // Return to pause menu if we were paused (opened from there)
        if (isPaused && pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        else if (!isPaused)
        {
            // If we weren't in the pause menu, this was a Win/End state
            QuitToMainMenu();
        }
    }

    // Called by PlayerInput component or a dedicated InputAction
    public void OnCancel(InputValue value)
    {
        if (value.isPressed)
        {
            HandleMenuToggle();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleMenuToggle();
        }
    }

    private void HandleMenuToggle()
    {
        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            HideOptions();
        }
        else if (leaderboardUI != null && leaderboardUI.displayPanel.activeSelf)
        {
            OnLeaderboardBack();
        }
        else
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(isPaused);
        
        Time.timeScale = isPaused ? 0f : 1f;

        if (!isPaused)
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (leaderboardUI != null && leaderboardUI.displayPanel != null) 
                leaderboardUI.displayPanel.SetActive(false);
        }
    }

    public void ShowLeaderboard()
    {
        if (leaderboardUI != null)
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            leaderboardUI.ShowLeaderboard();
        }
    }

    public void ShowOptions()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void InitializeVolumeSliders()
    {
        if (AudioManager.Instance == null) return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
            sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
    }
}
