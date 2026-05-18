using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects the MinimapCamera's RenderTexture to a UI RawImage.
/// Add this to the same GameObject as the RawImage.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MinimapUI : MonoBehaviour
{
    void Start()
    {
        if (MinimapCamera.Instance != null)
            GetComponent<RawImage>().texture = MinimapCamera.Instance.OutputTexture;
        else
            Debug.LogWarning("[MinimapUI] MinimapCamera.Instance not found. Make sure MinimapCamera is in the scene.");
    }
}