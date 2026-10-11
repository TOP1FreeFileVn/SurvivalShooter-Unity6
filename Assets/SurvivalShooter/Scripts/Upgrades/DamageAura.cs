using UnityEngine;

// Vũ khí phụ kiểu Garlic (Vampire Survivors): vòng lửa quanh người chơi, gây sát thương định kỳ.
// Hình ảnh là một quad nằm trên sàn dùng texture vòng lửa, xoay + phập phồng.
public class DamageAura : MonoBehaviour
{
    public float baseRadius = 2.2f;
    public float radiusPerLevel = 0.5f;
    public int baseDamage = 8;
    public int damagePerLevel = 5;
    public float tickInterval = 0.5f;
    public Renderer ringRenderer;

    PlayerStats stats;
    float timer;
    float flash;
    readonly Collider[] buffer = new Collider[64];
    int shootableMask;
    MaterialPropertyBlock block;

    void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
        shootableMask = LayerMask.GetMask("Shootable");
        block = new MaterialPropertyBlock();
    }

    float Radius => baseRadius + radiusPerLevel * (stats.auraLevel - 1);

    void Update()
    {
        bool active = stats.auraLevel > 0;
        ringRenderer.enabled = active;
        if (!active) return;

        // Quad không xoay theo người chơi; chỉ tự quay quanh trục Y
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.03f + flash * 0.08f;
        ringRenderer.transform.SetPositionAndRotation(
            transform.position + Vector3.up * 0.05f,
            Quaternion.Euler(90f, Time.time * 40f, 0f));
        ringRenderer.transform.localScale = Vector3.one * Radius * 2f * pulse;

        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 4f);
        block.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.75f + flash * 0.25f));
        ringRenderer.SetPropertyBlock(block);

        timer += Time.deltaTime;
        if (timer < tickInterval) return;
        timer = 0f;

        int dmg = Mathf.RoundToInt((baseDamage + damagePerLevel * (stats.auraLevel - 1)) * stats.damageMultiplier);
        int n = Physics.OverlapSphereNonAlloc(transform.position, Radius, buffer, shootableMask, QueryTriggerInteraction.Ignore);
        bool hitAny = false;
        for (int i = 0; i < n; i++)
        {
            var enemy = buffer[i].GetComponent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(dmg, buffer[i].bounds.center, false);
                hitAny = true;
            }
        }
        if (hitAny) flash = 1f;
    }
}
