using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;

public class LevelScreenshotTool : EditorWindow
{
    [MenuItem("Tools/Level Screenshot Tool")]
    public static void ShowWindow()
    {
        GetWindow<LevelScreenshotTool>("Level Screenshot");
    }

    private void OnGUI()
    {
        GUILayout.Label("Take a screenshot of the current scene for the Level Select menu.", EditorStyles.wordWrappedLabel);
        
        if (GUILayout.Button("Capture Screenshot", GUILayout.Height(40)))
        {
            Capture();
        }
    }

    private void Capture()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string folderPath = "Assets/Resources/LevelPreviews";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"{sceneName}.png";
        string fullPath = Path.Combine(folderPath, fileName);

        // ScreenCapture.CaptureScreenshot works on the Game View
        ScreenCapture.CaptureScreenshot(fullPath);
        
        AssetDatabase.Refresh();
        
        // Ensure it's imported as a Sprite
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; // <--- CRITICAL FIX
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log($"Screenshot saved to: {fullPath}");
    }
}
