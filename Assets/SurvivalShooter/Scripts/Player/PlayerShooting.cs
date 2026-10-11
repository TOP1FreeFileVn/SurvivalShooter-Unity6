using UnityEngine;
using UnityEngine.InputSystem;

// Gắn vào GunBarrelEnd: bắn tia raycast khi giữ chuột trái
public class PlayerShooting : MonoBehaviour
{
    public int damagePerShot = 20;
    public float timeBetweenBullets = 0.15f;
    public float range = 100f;
    public float shotHeight = 0.6f;

    float timer;
    Ray shootRay;
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    Light gunLight;
    float effectsDisplayTime = 0.2f;

    void Awake()
    {
        shootableMask = LayerMask.GetMask("Shootable");
        gunParticles = GetComponentInChildren<ParticleSystem>();
        gunLine = GetComponent<LineRenderer>();
        gunAudio = GetComponent<AudioSource>();
        gunLight = GetComponent<Light>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        bool fire = (Mouse.current != null && Mouse.current.leftButton.isPressed)
                 || (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed);

        if (fire && timer >= timeBetweenBullets && Time.timeScale != 0)
            Shoot();

        if (timer >= timeBetweenBullets * effectsDisplayTime)
            DisableEffects();
    }

    public void DisableEffects()
    {
        gunLine.enabled = false;
        gunLight.enabled = false;
    }

    void Shoot()
    {
        timer = 0f;
        gunAudio.Play();
        gunLight.enabled = true;
        gunParticles.Stop();
        gunParticles.Play();

        // Bắn ngang ở tầm thân quái (animation có thể hạ nòng súng sát mặt đất)
        Vector3 origin = transform.position;
        origin.y = transform.root.position.y + shotHeight;
        Vector3 dir = transform.forward;
        dir.y = 0f;
        dir.Normalize();

        gunLine.enabled = true;
        gunLine.SetPosition(0, origin);

        shootRay.origin = origin;
        shootRay.direction = dir;

        if (Physics.Raycast(shootRay, out shootHit, range, shootableMask))
        {
            EnemyHealth enemyHealth = shootHit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(damagePerShot, shootHit.point);
            gunLine.SetPosition(1, shootHit.point);
        }
        else
        {
            gunLine.SetPosition(1, shootRay.origin + shootRay.direction * range);
        }
    }
}
