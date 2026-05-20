using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI levelNameText;
    public Image previewImage;
    public Button button;

    private string scenePath;

    public void Setup(string name, Sprite preview, string path)
    {
        // Fail-safe: Try to find components if they weren't assigned in the Inspector
        if (levelNameText == null) levelNameText = GetComponentInChildren<TextMeshProUGUI>();
        if (previewImage == null) previewImage = GetComponentInChildren<Image>();
        if (button == null) button = GetComponent<Button>();

        if (levelNameText != null) levelNameText.text = name;
        if (previewImage != null)
        {
            previewImage.sprite = preview;
            // Force the image to be fully opaque and white-tinted (original colors)
            previewImage.color = Color.white; 
        }
        
        scenePath = path;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
        else
        {
            Debug.LogError($"[LevelButtonUI] No Button component found on {gameObject.name}!", gameObject);
        }
    }

    private void OnClicked()
    {
        Debug.Log($"Loading level: {scenePath}");
        SceneManager.LoadScene(scenePath);
    }
}
