using UnityEngine;

// Toàn bộ chỉ số của người chơi + kinh nghiệm/cấp độ.
// Các buff khi lên cấp (UpgradeManager) chỉ việc sửa các giá trị ở đây.
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Tấn công")]
    public float damageMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public float critChance = 0.05f;
    public float critMultiplier = 2f;
    public int extraProjectiles = 0;
    public int pierce = 0;
    public float lifestealChance = 0f;
    public int lifestealAmount = 2;

    [Header("Sinh tồn")]
    public int maxHealth = 100;
    public int armor = 0;
    public float regenPerSecond = 0f;
    public float moveSpeedMultiplier = 1f;

    [Header("Kinh nghiệm")]
    public float pickupRange = 3f;
    public float xpMultiplier = 1f;
    public int level = 1;
    public int xp;
    public int xpToNextLevel;
    public int pendingLevelUps;

    [Header("Vũ khí phụ")]
    public int auraLevel = 0;

    void Awake()
    {
        Instance = this;
        xpToNextLevel = XpRequired(level);
    }

    public static int XpRequired(int lv) => 5 + (lv - 1) * 5;

    public void AddXp(int amount)
    {
        xp += Mathf.Max(1, Mathf.RoundToInt(amount * xpMultiplier));
        while (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            level++;
            xpToNextLevel = XpRequired(level);
            pendingLevelUps++;
        }
    }
}
