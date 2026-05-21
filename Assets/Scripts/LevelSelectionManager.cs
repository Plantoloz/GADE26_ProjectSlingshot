using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;

public class LevelSelectionManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The folder where level preview images are stored (inside a Resources folder).")]
    public string previewResourcePath = "LevelPreviews";
    
    [Header("UI References")]
    public GameObject levelButtonPrefab;
    public Transform gridParent;
    public Sprite defaultPreview;

    private GameObject firstButton;

    private void Awake()
    {
        Debug.Log("[LevelSelectionManager] Awake: Script is active and alive.");
    }

    private void Start()
    {
        Debug.Log("[LevelSelectionManager] Start: Calling PopulateLevelList.");
        PopulateLevelList();
    }

    private void OnEnable()
    {
        if (firstButton != null)
            EventSystem.current?.SetSelectedGameObject(firstButton);
    }

    [ContextMenu("Force Refresh")]
    public void Refresh()
    {
        PopulateLevelList();
    }

    public void PopulateLevelList()
    {
        Debug.Log("[LevelSelectionManager] Populating Level List...");
        
        // Clear existing buttons
        int childCount = gridParent.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"[LevelSelectionManager] Found {sceneCount} scenes in Build Settings.");
        
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Exclude the Main Menu and common test scenes
            if (sceneName.ToLower().Contains("menu") || sceneName.ToLower().Contains("samplescene"))
            {
                Debug.Log($"[LevelSelectionManager] Skipping system scene: {sceneName}");
                continue;
            }

            Debug.Log($"[LevelSelectionManager] Adding Level: {sceneName}");

            // Try to load a preview image from Resources/LevelPreviews/[SceneName]
            string resourcePath = $"{previewResourcePath}/{sceneName}";
            Sprite preview = Resources.Load<Sprite>(resourcePath);
            
            // Fallback: If it failed to load as a Sprite, try loading as Texture and converting
            if (preview == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(resourcePath);
                if (tex != null)
                {
                    preview = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    Debug.Log($"[LevelSelectionManager] Loaded {sceneName} via Texture2D fallback.");
                }
            }

            
            if (preview != null)
            {
                Debug.Log($"[LevelSelectionManager] Successfully loaded image for {sceneName}");
            }
            else
            {
                Debug.LogWarning($"[LevelSelectionManager] Failed to load image at: {resourcePath}. Make sure the image is inside a 'Resources' folder and has the correct name.");
                preview = defaultPreview;
            }

            // Instantiate and setup button
            GameObject buttonObj = Instantiate(levelButtonPrefab, gridParent);
            if (buttonObj.TryGetComponent<LevelButtonUI>(out var levelUI))
            {
                // Format the name nicely (e.g., Level02_Sector_Syren -> Sector Syren)
                string displayName = sceneName.Replace("Level", "").Replace("_", " ").Trim();

                // Remove leading numbers if they exist (e.g., "02 Sector Syren" -> "Sector Syren")
                if (displayName.Length > 2 && char.IsDigit(displayName[0]) && char.IsDigit(displayName[1]))
                    displayName = displayName.Substring(2).Trim();

                levelUI.Setup(displayName, preview, scenePath);

                if (firstButton == null)
                    firstButton = buttonObj;
            }
        }
    }
}
