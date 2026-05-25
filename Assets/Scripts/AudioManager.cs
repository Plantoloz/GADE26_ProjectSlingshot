using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SoundEffect
{
    public string name;
    public AudioClip clip;
    [Range(0f, 2f)] public float volumeMultiplier = 1f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Global Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolumeMultiplier = 1f;
    [Range(0f, 1f)] public float sfxVolumeMultiplier = 1f;

    [Header("Sound Library")]
    public List<SoundEffect> soundLibrary = new List<SoundEffect>();

    [Header("Quick Access Clips (Legacy Support)")]
    public AudioClip backgroundMusic;
    public AudioClip collisionSound;

    private Dictionary<string, SoundEffect> soundDict = new Dictionary<string, SoundEffect>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLibrary();
            LoadVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        // Help populate the library if it's empty but clips are assigned to legacy fields
        if (soundLibrary.Count == 0)
        {
            if (backgroundMusic != null) soundLibrary.Add(new SoundEffect { name = "Music", clip = backgroundMusic, volumeMultiplier = 1f });
            if (collisionSound != null) soundLibrary.Add(new SoundEffect { name = "Collision", clip = collisionSound, volumeMultiplier = 0.8f });
        }
    }

    private void InitializeLibrary()
    {
        soundDict.Clear();
        foreach (var sound in soundLibrary)
        {
            if (!string.IsNullOrEmpty(sound.name) && !soundDict.ContainsKey(sound.name))
            {
                soundDict.Add(sound.name, sound);
            }
        }
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
        UpdateMusicVolume();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null) return;
        
        float clipMultiplier = GetClipMultiplier(clip);
        sfxSource.PlayOneShot(clip, volumeScale * clipMultiplier * GetSFXVolume());
    }

    public void PlaySFX(string soundName, float volumeScale = 1f)
    {
        if (sfxSource == null) return;

        if (soundDict.TryGetValue(soundName, out SoundEffect sound))
        {
            sfxSource.PlayOneShot(sound.clip, volumeScale * sound.volumeMultiplier * GetSFXVolume());
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found in AudioManager library.");
        }
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        PlaySFX(clip, volumeScale);
    }

    public void PlaySFXAtPoint(string soundName, Vector3 position, float volumeScale = 1f)
    {
        PlaySFX(soundName, volumeScale);
    }

    public float GetClipMultiplier(AudioClip clip)
    {
        foreach (var sound in soundLibrary)
        {
            if (sound.clip == clip) return sound.volumeMultiplier;
        }
        return 1f;
    }

    public float GetSoundVolume(string soundName)
    {
        if (soundDict.TryGetValue(soundName, out SoundEffect sound))
        {
            return sound.volumeMultiplier;
        }
        return 1f;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolumeMultiplier = volume;
        UpdateMusicVolume();
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolumeMultiplier = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolumeMultiplier * masterVolume;
        }
    }

    public float GetMusicVolume() => musicVolumeMultiplier;
    public float GetSFXVolume() => sfxVolumeMultiplier * masterVolume;

    private void LoadVolume()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolumeMultiplier = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolumeMultiplier = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        UpdateMusicVolume();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        UpdateMusicVolume();
    }

    // Call this if library is modified at runtime
    public void RefreshLibrary() => InitializeLibrary();
}
