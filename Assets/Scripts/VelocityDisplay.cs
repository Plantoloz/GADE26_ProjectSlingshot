using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class VelocityDisplay : MonoBehaviour
{
    [Header("References")]
    public ShipController ship;

    [Header("Settings")]
    public string prefix = "Speed: ";
    public string suffix = " u/s";
    [Tooltip("Decimal places shown")]
    public int decimals = 1;

    private TextMeshProUGUI label;
    private Rigidbody shipRb;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        if (ship != null)
            shipRb = ship.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (shipRb == null) return;
        float speed = shipRb.linearVelocity.magnitude;
        label.text = $"{prefix}{speed.ToString($"F{decimals}")}{suffix}";
    }
}