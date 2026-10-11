using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public float sinkSpeed = 2.5f;
    public int scoreValue = 10;
    public int xpValue = 1;
    public AudioClip deathClip;

    public bool IsDead => isDead;

    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
    bool isDead;
    bool isSinking;

    void Awake()
    {
        anim = GetComponent<Animator>();
        enemyAudio = GetComponent<AudioSource>();
        hitParticles = GetComponentInChildren<ParticleSystem>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        currentHealth = startingHealth;
    }

    // Gọi bởi WaveManager ngay sau khi spawn để quái mạnh dần theo wave
    public void Scale(float healthMul)
    {
        startingHealth = Mathf.RoundToInt(startingHealth * healthMul);
        currentHealth = startingHealth;
    }

    void Update()
    {
        if (isSinking)
            transform.Translate(-Vector3.up * sinkSpeed * Time.deltaTime);
    }

    public void TakeDamage(int amount, Vector3 hitPoint, bool crit = false)
    {
        if (isDead) return;

        enemyAudio.Play();
        currentHealth -= amount;
        DamagePopup.Show(hitPoint, amount, crit);

        if (hitParticles != null)
        {
            hitParticles.transform.position = hitPoint;
            hitParticles.Play();
        }

        if (currentHealth <= 0) Death(true);
    }

    // dropLoot = false khi quái bị dọn đi lúc hết wave
    public void Death(bool dropLoot)
    {
        if (isDead) return;
        isDead = true;
        capsuleCollider.isTrigger = true;
        anim.SetTrigger("Dead");
        if (dropLoot)
        {
            enemyAudio.clip = deathClip;
            enemyAudio.Play();
            ScoreManager.score += scoreValue;
            XpGem.Spawn(transform.position, xpValue);
        }
        // Dự phòng nếu model (vd. ZomBear) không có animation event
        Invoke(nameof(StartSinking), dropLoot ? 1.5f : 0.6f);
    }

    // Được gọi bởi Animation Event của clip Death
    public void StartSinking()
    {
        if (isSinking) return;
        isSinking = true;
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Rigidbody>().isKinematic = true;
        Destroy(gameObject, 2f);
    }
}
