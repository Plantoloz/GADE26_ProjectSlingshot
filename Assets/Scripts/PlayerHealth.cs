using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("UI References")]
    public Slider healthSlider;
    public Image healthFill;
    public Gradient healthGradient;
    
    [Header("Damage Settings")]
    public float minImpactForce = 2f;
    public float damageMultiplier = 5f;
    public string collisionSoundName = "Collision";

    private bool isGameOver = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isGameOver) return;

        // Check if we hit an asteroid or planet
        if (collision.gameObject.CompareTag("Asteroid") || collision.gameObject.CompareTag("Planet"))
        {
            // Calculate damage based on the impact force
            float impactForce = collision.relativeVelocity.magnitude;
            
            if (impactForce > minImpactForce)
            {
                float damage = (impactForce - minImpactForce) * damageMultiplier;
                TakeDamage(damage);
                
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPoint(collisionSoundName, transform.position);
                }
                
                Debug.Log($"Impact! Damage: {damage:F1} | Health: {currentHealth:F1}");
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        UpdateUI();

        if (currentHealth <= 0 && !isGameOver)
        {
            GameOver();
        }
    }

    void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFill != null)
            healthFill.color = healthGradient.Evaluate(currentHealth / maxHealth);
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER!");

        ShipController controller = GetComponent<ShipController>();
        if (controller != null) controller.enabled = false;

        CheckpointManager cm = FindFirstObjectByType<CheckpointManager>();
        if (cm != null)
            Invoke("RespawnFromCheckpoint", 3f);
        else
            Invoke("RestartLevel", 3f);
    }

    void RespawnFromCheckpoint()
    {
        CheckpointManager cm = FindFirstObjectByType<CheckpointManager>();
        cm?.RespawnAtLastCheckpoint();
    }

    public void Respawn()
    {
        CancelInvoke();
        isGameOver = false;
        currentHealth = maxHealth;
        UpdateUI();
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}