using UnityEngine;
using UnityEngine.InputSystem;

public class SelfDestruct : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Leben pro Sekunde, die beim Halten abgezogen werden.")]
    public float damageRate = 30f;
    public float coolRate = 15f;

    [Header("References")]
    public PlayerHealth playerHealth;

    private float _charge;
    private bool  _held;

    public void ResetInput()
    {
        _held   = false;
        _charge = 0f;
    }

    void OnSelfDestruct(InputValue value)
    {
        _held = value.Get<float>() > 0.5f;
    }

    void Update()
    {
        if (_held)
            _charge += damageRate * Time.deltaTime;
        else
            _charge -= coolRate * Time.deltaTime;

        _charge = Mathf.Clamp(_charge, 0f, 100f);

        if (_held && playerHealth != null)
            playerHealth.TakeDamage(damageRate * Time.deltaTime);
    }
}