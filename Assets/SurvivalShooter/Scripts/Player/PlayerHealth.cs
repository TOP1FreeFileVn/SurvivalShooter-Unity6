using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth;
    public Slider healthSlider;
    public Text healthText;
    public Image damageImage;
    public AudioClip deathClip;
    public float flashSpeed = 5f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.1f);

    public bool IsDead => isDead;

    PlayerStats stats;
    Animator anim;
    AudioSource playerAudio;
    PlayerMovement playerMovement;
    PlayerShooting playerShooting;
    bool isDead;
    bool damaged;
    float regenBuffer;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        anim = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooting = GetComponentInChildren<PlayerShooting>();
        currentHealth = stats.maxHealth;
    }

    void Update()
    {
        if (damageImage != null)
        {
            damageImage.color = damaged
                ? flashColour
                : Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        damaged = false;

        if (!isDead && stats.regenPerSecond > 0f && currentHealth < stats.maxHealth)
        {
            regenBuffer += stats.regenPerSecond * Time.deltaTime;
            if (regenBuffer >= 1f)
            {
                int amount = (int)regenBuffer;
                regenBuffer -= amount;
                Heal(amount);
            }
        }

        RefreshUI();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        damaged = true;
        // Giáp trừ thẳng vào sát thương, nhưng luôn mất ít nhất 1 máu
        currentHealth -= Mathf.Max(1, amount - stats.armor);
        playerAudio.Play();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Death();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, stats.maxHealth);
    }

    void RefreshUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = stats.maxHealth;
            healthSlider.value = currentHealth;
        }
        if (healthText != null)
            healthText.text = currentHealth + " / " + stats.maxHealth;
    }

    void Death()
    {
        isDead = true;
        playerShooting.DisableEffects();
        anim.SetTrigger("Die");
        playerAudio.clip = deathClip;
        playerAudio.Play();
        playerMovement.enabled = false;
        playerShooting.enabled = false;
    }

    // Được gọi bởi Animation Event cuối clip Death (giữ tên để khớp với asset).
    // Việc restart do GameOverManager đảm nhận.
    public void RestartLevel() { }
}
