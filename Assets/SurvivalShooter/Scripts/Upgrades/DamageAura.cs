using UnityEngine;

// Vũ khí phụ kiểu Garlic (Vampire Survivors): vòng lửa quanh người chơi, gây sát thương định kỳ.
public class DamageAura : MonoBehaviour
{
    public float baseRadius = 2.2f;
    public float radiusPerLevel = 0.5f;
    public int baseDamage = 8;
    public int damagePerLevel = 5;
    public float tickInterval = 0.5f;

    PlayerStats stats;
    LineRenderer ring;
    float timer;
    readonly Collider[] buffer = new Collider[64];
    int shootableMask;

    void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
        ring = GetComponent<LineRenderer>();
        shootableMask = LayerMask.GetMask("Shootable");
    }

    float Radius => baseRadius + radiusPerLevel * (stats.auraLevel - 1);

    void Update()
    {
        bool active = stats.auraLevel > 0;
        ring.enabled = active;
        if (!active) return;

        DrawRing();

        timer += Time.deltaTime;
        if (timer < tickInterval) return;
        timer = 0f;

        int dmg = Mathf.RoundToInt((baseDamage + damagePerLevel * (stats.auraLevel - 1)) * stats.damageMultiplier);
        int n = Physics.OverlapSphereNonAlloc(transform.position, Radius, buffer, shootableMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var enemy = buffer[i].GetComponent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead)
                enemy.TakeDamage(dmg, buffer[i].bounds.center, false);
        }
    }

    void DrawRing()
    {
        const int segments = 48;
        ring.positionCount = segments;
        float r = Radius;
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.03f;
        Vector3 c = transform.position;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments + Time.time * 0.8f;
            ring.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * pulse + Vector3.up * 0.1f);
        }
    }
}
