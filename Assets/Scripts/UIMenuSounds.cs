using UnityEngine;
using UnityEngine.UI;

// Attach once to a menu panel root. Automatically adds UIButtonSounds to all child Buttons.
public class UIMenuSounds : MonoBehaviour
{
    private void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>(includeInactive: true))
        {
            if (button.GetComponent<UIButtonSounds>() == null)
                button.gameObject.AddComponent<UIButtonSounds>();
        }
    }
}