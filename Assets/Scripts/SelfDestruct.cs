using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelfDestruct : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Leben pro Sekunde, die beim Halten abgezogen werden.")]
    public float damageRate = 30f;
    public float coolRate = 15f;

    [Header("References")]
    public PlayerHealth playerHealth;

    private float _charge; // 0..100

    void Update()
    {
        bool held = IsButtonHeld();

        if (held)
            _charge += damageRate * Time.deltaTime;
        else
            _charge -= coolRate * Time.deltaTime;

        _charge = Mathf.Clamp(_charge, 0f, 100f);

        if (held && playerHealth != null)
            playerHealth.TakeDamage(damageRate * Time.deltaTime);
    }

    bool IsButtonHeld()
    {
        if (Keyboard.current != null && Keyboard.current.xKey.isPressed)
            return true;

        if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
            return true;

        return false;
    }
}