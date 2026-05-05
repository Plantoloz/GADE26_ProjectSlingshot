using UnityEngine;
using UnityEngine.UI;

public class EngineOverheat : MonoBehaviour
{
    [Header("Settings")]
    public float heatRate = 20f;
    public float coolRate = 10f;

    [Header("Overheat Damage")]
    [Tooltip("Leben pro Sekunde, die abgezogen werden wenn Overheat voll und Schub aktiv")]
    public float overheatingDamageRate = 10f;

    [Header("UI References")]
    public Slider overheatSlider;
    public Image overheatFill;
    public Gradient overheatGradient;

    private float heat = 0f;
    private ShipController ship;
    private PlayerHealth playerHealth;

    void Awake()
    {
        ship = GetComponent<ShipController>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        if (overheatSlider != null)
        {
            overheatSlider.minValue = 0f;
            overheatSlider.maxValue = 100f;
            overheatSlider.value = 0f;
        }
    }

    void Update()
    {
        if (ship.IsThrusting)
            heat += heatRate * Time.deltaTime;
        else
            heat -= coolRate * Time.deltaTime;

        heat = Mathf.Clamp(heat, 0f, 100f);

        UpdateUI();

        if (heat >= 100f && ship.IsThrusting)
            playerHealth.TakeDamage(overheatingDamageRate * Time.deltaTime);
    }

    void UpdateUI()
    {
        if (overheatSlider != null)
            overheatSlider.value = heat;

        if (overheatFill != null)
            overheatFill.color = overheatGradient.Evaluate(heat / 100f);
    }
}
