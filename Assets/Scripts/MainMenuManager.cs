using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Start()
    {
        InitializeSliders();
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
            // Note: We use the multiplier here, not the combined volume
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    public void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
    }

    public void PlayGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    
    public void QuitGame() {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
