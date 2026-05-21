using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Bulletproof In-Game Menu Manager.
/// Uses explicit Pause/Resume states to prevent "Double Toggling" and input conflicts.
/// </summary>
public class InGameMenuManager : MonoBehaviour
{
    public static InGameMenuManager Instance { get; private set; }

    [Header("Menu Panels")]
    public GameObject pauseMenuPanel;
    public GameObject optionsPanel;
    public LeaderboardUI leaderboardUI;

    [Header("Audio Sliders (Optional)")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    private InputActionAsset actionsAsset;
    private InputActionMap playerMap;
    private InputActionMap uiMap;
    private InputAction cancelAction;
    
    private Stack<GameObject> menuHistory = new Stack<GameObject>();
    private GameObject currentActivePanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        InitializeInput();
        InitializeVolumeSliders();
        
        // Hard reset all UI states on start
        ForceCloseAllMenus();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isPaused = false;
        Time.timeScale = 1f;

        FindSceneReferences();
        ForceCloseAllMenus();
        InitializeInput();
    }

    private void FindSceneReferences()
    {
        leaderboardUI = Object.FindFirstObjectByType<LeaderboardUI>(FindObjectsInactive.Include);

        if (pauseMenuPanel == null || !pauseMenuPanel.scene.IsValid())
        {
            var found = GameObject.Find("Pause Menu");
            if (found != null) pauseMenuPanel = found;
        }

        if (optionsPanel == null || !optionsPanel.scene.IsValid())
        {
            var found = GameObject.Find("Options Menu");
            if (found != null) optionsPanel = found;
        }
    }

    private void InitializeInput()
    {
        if (EventSystem.current == null) return;
        var uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        if (uiModule != null && uiModule.actionsAsset != null)
        {
            actionsAsset = uiModule.actionsAsset;
            playerMap = actionsAsset.FindActionMap("Player");
            uiMap = actionsAsset.FindActionMap("UI");

            // Use the global Cancel action for B button
            cancelAction = uiMap.FindAction("Cancel");
            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancelPerformed;
                cancelAction.performed += OnCancelPerformed;
            }
        }
    }

    private void Update()
    {
        // Fail-safe hardware check for Toggle (Start / Escape)
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || 
            Gamepad.current?.startButton.wasPressedThisFrame == true)
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        // B button ONLY goes back if there's history. 
        if (_isPaused && menuHistory.Count > 0)
        {
            GoBack();
        }
    }

    public void PauseGame()
    {
        if (_isPaused) return;
        _isPaused = true;
        
        Time.timeScale = 0f;
        
        playerMap?.Disable();
        uiMap?.Enable();

        // Open root pause menu
        currentActivePanel = pauseMenuPanel;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

        UpdateCursorState();
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;
        _isPaused = false;
        
        Time.timeScale = 1f;
        
        uiMap?.Disable();
        playerMap?.Enable();

        ForceCloseAllMenus();
        UpdateCursorState();

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void ForceCloseAllMenus()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (leaderboardUI != null && leaderboardUI.displayPanel != null) leaderboardUI.displayPanel.SetActive(false);
        
        menuHistory.Clear();
        currentActivePanel = null;
    }

    private void UpdateCursorState()
    {
        Cursor.visible = _isPaused;
        Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OpenSubMenu(GameObject subMenu)
    {
        if (subMenu == null || subMenu == currentActivePanel) return;

        if (currentActivePanel != null)
        {
            // Focus hand-off
            if (subMenu.TryGetComponent<UIPanelFocus>(out var nextFocus))
            {
                nextFocus.previousSelected = EventSystem.current.currentSelectedGameObject;
            }

            menuHistory.Push(currentActivePanel);
            currentActivePanel.SetActive(false);
        }

        currentActivePanel = subMenu;
        currentActivePanel.SetActive(true);
    }

    public void GoBack()
    {
        if (menuHistory.Count == 0) return;

        if (currentActivePanel != null)
        {
            currentActivePanel.SetActive(false);
        }

        currentActivePanel = menuHistory.Pop();
        currentActivePanel.SetActive(true);
    }

    // Explicit helper methods for UI Buttons to prevent mis-clicks
    public void UI_ShowOptions() => OpenSubMenu(optionsPanel);
    public void UI_ShowLeaderboard() { if (leaderboardUI != null) OpenSubMenu(leaderboardUI.displayPanel); }
    public void UI_GoBack() => GoBack();
    public void UI_Resume() => ResumeGame();

    public void UI_Restart() 
    { 
        Time.timeScale = 1f; 
        _isPaused = false;
        ForceCloseAllMenus(); // Hard cleanup before restart
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void UI_Quit() 
    { 
        Time.timeScale = 1f; 
        _isPaused = false;
        ForceCloseAllMenus();
        SceneManager.LoadScene(0); 
    }

    private void InitializeVolumeSliders()
    {
        if (AudioManager.Instance == null) return;
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
    }
}
