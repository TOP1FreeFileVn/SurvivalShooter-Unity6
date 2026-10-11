using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Bảng chọn buff khi lên cấp: dừng game, hiện 3 thẻ ngẫu nhiên theo độ hiếm, chọn bằng chuột hoặc phím 1/2/3.
public class UpgradeManager : MonoBehaviour
{
    public enum Rarity { Common, Rare, Epic }

    public class Upgrade
    {
        public string name;
        public string description;
        public Rarity rarity;
        public int maxStacks = 99;
        public Action<PlayerStats, PlayerHealth> apply;
        public int taken;
    }

    [Header("UI")]
    public GameObject panel;
    public Text titleText;
    public Button[] cardButtons;
    public Text[] cardNames;
    public Text[] cardDescriptions;
    public Text[] cardRarities;
    public Image[] cardBackgrounds;

    [Header("Tham chiếu")]
    public PlayerStats stats;
    public PlayerHealth health;
    public Text levelText;
    public Slider xpSlider;
    public Text statsText;

    static readonly Color[] RarityColors =
    {
        new Color(0.75f, 0.75f, 0.8f),
        new Color(0.3f, 0.6f, 1f),
        new Color(0.75f, 0.35f, 1f),
    };
    static readonly string[] RarityNames = { "THUONG", "HIEM", "SU THI" };

    readonly List<Upgrade> pool = new List<Upgrade>();
    readonly Upgrade[] offered = new Upgrade[3];
    int chosen = -1;

    public bool IsChoosing { get; private set; }

    void Awake()
    {
        BuildPool();
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int idx = i;
            cardButtons[i].onClick.AddListener(() => chosen = idx);
        }
        panel.SetActive(false);
    }

    void BuildPool()
    {
        Add("Dan Cung", "+15% sat thuong", Rarity.Common, (s, h) => s.damageMultiplier += 0.15f);
        Add("Co Tay Nhanh", "+12% toc do ban", Rarity.Common, (s, h) => s.fireRateMultiplier += 0.12f);
        Add("Than The", "+20 mau toi da\nHoi 20 mau", Rarity.Common, (s, h) => { s.maxHealth += 20; h.Heal(20); });
        Add("Giay Chay", "+8% toc do chay", Rarity.Common, (s, h) => s.moveSpeedMultiplier += 0.08f, 6);
        Add("Nam Cham", "+1.5 tam hut EXP", Rarity.Common, (s, h) => s.pickupRange += 1.5f, 6);
        Add("Ao Giap", "+1 giap\n(giam sat thuong nhan vao)", Rarity.Common, (s, h) => s.armor += 1);
        Add("Hoi Phuc", "+0.6 mau / giay", Rarity.Common, (s, h) => s.regenPerSecond += 0.6f);
        Add("Mat Ung", "+6% ti le chi mang", Rarity.Common, (s, h) => s.critChance += 0.06f, 8);

        Add("Hoc Gioi", "+20% EXP nhan duoc", Rarity.Rare, (s, h) => s.xpMultiplier += 0.2f, 5);
        Add("Xuyen Giap", "Dan xuyen qua +1 quai", Rarity.Rare, (s, h) => s.pierce += 1, 4);
        Add("Ma Ca Rong", "+8% co hoi hoi 2 mau\nkhi ban trung", Rarity.Rare, (s, h) => s.lifestealChance += 0.08f, 5);
        Add("Sat Thu", "+50% sat thuong chi mang", Rarity.Rare, (s, h) => s.critMultiplier += 0.5f, 4);
        Add("Hao Quang Lua", "Vong lua quanh nguoi\ngay sat thuong lien tuc\n(nang cap: rong + manh hon)", Rarity.Rare, (s, h) => s.auraLevel += 1, 5);
        Add("Lieu Mang", "+35% sat thuong\n-10 mau toi da", Rarity.Rare, (s, h) => { s.damageMultiplier += 0.35f; s.maxHealth = Mathf.Max(20, s.maxHealth - 10); h.Heal(0); });

        Add("Song Sung", "+1 tia dan moi phat ban", Rarity.Epic, (s, h) => s.extraProjectiles += 1, 4);
        Add("Cuong No", "+30% toc do ban\n+20% sat thuong", Rarity.Epic, (s, h) => { s.fireRateMultiplier += 0.3f; s.damageMultiplier += 0.2f; });
        Add("Bat Tu", "+50 mau toi da\nHoi day mau", Rarity.Epic, (s, h) => { s.maxHealth += 50; h.Heal(9999); }, 3);
    }

    void Add(string name, string desc, Rarity r, Action<PlayerStats, PlayerHealth> apply, int maxStacks = 99)
    {
        pool.Add(new Upgrade { name = name, description = desc, rarity = r, apply = apply, maxStacks = maxStacks });
    }

    void Update()
    {
        if (levelText != null) levelText.text = "LV " + stats.level;
        if (xpSlider != null)
        {
            xpSlider.maxValue = stats.xpToNextLevel;
            xpSlider.value = stats.xp;
        }
        if (statsText != null) statsText.text = BuildStatsText();

        if (IsChoosing && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) chosen = 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) chosen = 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) chosen = 2;
        }
    }

    // Gọi bởi WaveManager khi hết wave
    public IEnumerator ProcessPendingLevelUps()
    {
        while (stats.pendingLevelUps > 0 && !health.IsDead)
        {
            stats.pendingLevelUps--;
            yield return Choose();
        }
    }

    IEnumerator Choose()
    {
        RollCards();
        IsChoosing = true;
        chosen = -1;
        panel.SetActive(true);
        titleText.text = "LEN CAP " + (stats.level - stats.pendingLevelUps) + "!  CHON 1 NANG CAP";
        float prevScale = Time.timeScale;
        Time.timeScale = 0f;

        while (chosen < 0) yield return null;

        var up = offered[chosen];
        up.apply(stats, health);
        up.taken++;

        panel.SetActive(false);
        Time.timeScale = prevScale == 0f ? 1f : prevScale;
        IsChoosing = false;
        // Tránh việc cú click chọn thẻ bị tính là phát bắn
        yield return null;
    }

    void RollCards()
    {
        var available = pool.FindAll(u => u.taken < u.maxStacks);
        for (int i = 0; i < offered.Length; i++)
        {
            Rarity r = RollRarity();
            var candidates = available.FindAll(u => u.rarity == r && Array.IndexOf(offered, u, 0, i) < 0);
            if (candidates.Count == 0) candidates = available.FindAll(u => Array.IndexOf(offered, u, 0, i) < 0);
            offered[i] = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            var u2 = offered[i];
            cardNames[i].text = u2.name;
            cardDescriptions[i].text = u2.description + (u2.taken > 0 ? "\n\n(Da co x" + u2.taken + ")" : "");
            cardRarities[i].text = "[" + (i + 1) + "]  " + RarityNames[(int)u2.rarity];
            cardRarities[i].color = RarityColors[(int)u2.rarity];
            cardBackgrounds[i].color = Color.Lerp(new Color(0.12f, 0.1f, 0.18f), RarityColors[(int)u2.rarity], 0.25f);
        }
    }

    Rarity RollRarity()
    {
        // Cấp càng cao càng dễ ra đồ hiếm
        float luck = Mathf.Min(0.15f, stats.level * 0.006f);
        float r = UnityEngine.Random.value;
        if (r < 0.08f + luck) return Rarity.Epic;
        if (r < 0.33f + luck * 2f) return Rarity.Rare;
        return Rarity.Common;
    }

    string BuildStatsText()
    {
        return "Sat thuong  x" + stats.damageMultiplier.ToString("0.00") +
               "\nToc do ban  x" + stats.fireRateMultiplier.ToString("0.00") +
               "\nChi mang  " + Mathf.RoundToInt(stats.critChance * 100) + "%" +
               "\nGiap  " + stats.armor +
               "\nHoi mau  " + stats.regenPerSecond.ToString("0.0") + "/s" +
               (stats.extraProjectiles > 0 ? "\nTia dan  " + (1 + stats.extraProjectiles) : "") +
               (stats.pierce > 0 ? "\nXuyen  " + stats.pierce : "") +
               (stats.auraLevel > 0 ? "\nHao quang  Lv" + stats.auraLevel : "");
    }
}
