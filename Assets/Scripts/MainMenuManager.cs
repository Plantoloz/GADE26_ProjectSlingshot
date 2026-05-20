using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Initialization")]
    [SerializeField] private GameObject initialPanel;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private Stack<GameObject> menuHistory = new Stack<GameObject>();
    private GameObject currentPanel;
    private InputAction cancelAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeSliders();
        InitializeInput();
        
        // Setup initial panel
        if (initialPanel != null)
        {
            currentPanel = initialPanel;
            initialPanel.SetActive(true);
        }
    }

    private void InitializeInput()
    {
        // Pull the Cancel action from the EventSystem's UI Input Module automatically
        if (EventSystem.current != null)
        {
            var uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (uiModule != null)
            {
                cancelAction = uiModule.cancel.action;
                if (cancelAction != null)
                {
                    cancelAction.performed += _ => GoBack();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (cancelAction != null)
        {
            cancelAction.performed -= _ => GoBack();
        }
    }

    /// <summary>
    /// Opens a new menu and pushes the current one onto the history stack.
    /// </summary>
    public void OpenMenu(GameObject nextMenu)
// ... rest of the file ...
    {
        if (nextMenu == null || nextMenu == currentPanel) return;

        // If we lost track of the current panel (e.g. null on start), 
        // try to find what's currently active in our hierarchy.
        if (currentPanel == null)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf && child.gameObject != nextMenu)
                {
                    currentPanel = child.gameObject;
                    break;
                }
            }
        }

        if (currentPanel != null)
        {
            // Store where we are coming from and what button was selected
            if (nextMenu.TryGetComponent<UIPanelFocus>(out var nextFocus))
            {
                nextFocus.previousSelected = EventSystem.current.currentSelectedGameObject;
            }

            menuHistory.Push(currentPanel);
            currentPanel.SetActive(false);
        }

        currentPanel = nextMenu;
        currentPanel.SetActive(true);
    }

    /// <summary>
    /// Automatically returns to the previous menu in the history.
    /// </summary>
    public void GoBack()
    {
        if (menuHistory.Count == 0) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        currentPanel = menuHistory.Pop();
        currentPanel.SetActive(true);
    }

    private void InitializeSliders()
    {
        if (AudioManager.Instance == null) return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    public void OnMasterVolumeChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
    public void OnMusicVolumeChanged(float value) => AudioManager.Instance?.SetMusicVolume(value);
    public void OnSFXVolumeChanged(float value) => AudioManager.Instance?.SetSFXVolume(value);

    public void PlayGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public void QuitGame() => Application.Quit();
}
