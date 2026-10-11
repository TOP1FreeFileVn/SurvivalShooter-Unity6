using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Gắn vào GunBarrelEnd: bắn tia raycast khi giữ chuột trái.
// Hỗ trợ bắn nhiều tia (extraProjectiles), xuyên quái (pierce), chí mạng và hút máu từ PlayerStats.
public class PlayerShooting : MonoBehaviour
{
    public int damagePerShot = 20;
    public float timeBetweenBullets = 0.15f;
    public float range = 100f;
    public float shotHeight = 0.6f;
    public float spreadAngle = 8f;

    float timer;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    Light gunLight;
    PlayerStats stats;
    PlayerHealth health;
    float effectsDisplayTime = 0.2f;
    readonly List<LineRenderer> lines = new List<LineRenderer>();
    readonly RaycastHit[] hitBuffer = new RaycastHit[32];

    float Cooldown => timeBetweenBullets / stats.fireRateMultiplier;

    void Awake()
    {
        shootableMask = LayerMask.GetMask("Shootable");
        gunParticles = GetComponentInChildren<ParticleSystem>();
        gunLine = GetComponent<LineRenderer>();
        gunAudio = GetComponent<AudioSource>();
        gunLight = GetComponent<Light>();
        stats = GetComponentInParent<PlayerStats>();
        health = GetComponentInParent<PlayerHealth>();
        lines.Add(gunLine);
    }

    void Update()
    {
        timer += Time.deltaTime;

        bool fire = (Mouse.current != null && Mouse.current.leftButton.isPressed)
                 || (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed);

        if (fire && timer >= Cooldown && Time.timeScale != 0)
            Shoot();

        if (timer >= Cooldown * effectsDisplayTime)
            DisableEffects();
    }

    public void DisableEffects()
    {
        foreach (var l in lines) l.enabled = false;
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
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        int count = 1 + stats.extraProjectiles;
        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) / 2f) * spreadAngle;
            FireRay(GetLine(i), origin, Quaternion.Euler(0f, angle, 0f) * forward);
        }
    }

    void FireRay(LineRenderer line, Vector3 origin, Vector3 dir)
    {
        int n = Physics.RaycastNonAlloc(origin, dir, hitBuffer, range, shootableMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hitBuffer, 0, n, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

        Vector3 end = origin + dir * range;
        int piercesLeft = stats.pierce;
        for (int k = 0; k < n; k++)
        {
            var hit = hitBuffer[k];
            var enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy == null)
            {
                end = hit.point; // trúng tường/đồ vật
                break;
            }
            Damage(enemy, hit.point);
            if (piercesLeft-- <= 0)
            {
                end = hit.point;
                break;
            }
        }

        line.enabled = true;
        line.SetPosition(0, origin);
        line.SetPosition(1, end);
    }

    void Damage(EnemyHealth enemy, Vector3 point)
    {
        bool crit = Random.value < stats.critChance;
        int dmg = Mathf.RoundToInt(damagePerShot * stats.damageMultiplier * (crit ? stats.critMultiplier : 1f));
        enemy.TakeDamage(dmg, point, crit);
        if (stats.lifestealChance > 0f && Random.value < stats.lifestealChance)
            health.Heal(stats.lifestealAmount);
    }

    LineRenderer GetLine(int index)
    {
        while (lines.Count <= index)
        {
            var go = new GameObject("ExtraGunLine");
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = gunLine.sharedMaterial;
            l.widthMultiplier = gunLine.widthMultiplier;
            l.positionCount = 2;
            l.useWorldSpace = true;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.enabled = false;
            lines.Add(l);
        }
        return lines[index];
    }
}
