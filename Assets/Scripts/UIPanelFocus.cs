using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles UI focus for panels.
/// Attach this to every menu panel (Main Menu, Options Panel, etc.).
/// </summary>
public class UIPanelFocus : MonoBehaviour
{
    [Header("Focus Settings")]
    [Tooltip("The button that should be selected when this panel is opened.")]
    [SerializeField] private GameObject firstSelected;
    
    [Tooltip("If true, it will try to re-select the firstSelected if focus is lost.")]
    [SerializeField] private bool autoRefocus = true;

    // The button that opened this menu. Used to restore focus when going back.
    [HideInInspector] public GameObject previousSelected;

    private void OnEnable()
    {
        SetFocus(firstSelected);
    }

    private void OnDisable()
    {
        if (previousSelected != null)
        {
            SetFocus(previousSelected);
        }
    }

    private void Update()
    {
        if (autoRefocus && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (IsNavigationDetected())
            {
                SetFocus(firstSelected);
            }
        }
    }

    private bool IsNavigationDetected()
    {
        // Simple check for any navigation intent
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f || 
                Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f) return true;
        }
        
        if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame) return true;
        }

        return false;
    }

    public void SetFocus(GameObject element)
    {
        if (element == null || EventSystem.current == null) return;
        
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(element);
    }
}
