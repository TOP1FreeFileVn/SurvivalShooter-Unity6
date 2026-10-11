using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Vòng lặp kiểu Brotato: mỗi wave có thời gian cố định, quái spawn liên tục và mạnh dần.
// Hết giờ -> dọn quái, hút EXP, mở bảng nâng cấp cho các lần lên cấp còn tồn, rồi sang wave mới.
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyType
    {
        public GameObject prefab;
        public int unlockWave = 1;
        public float weight = 1f;
    }

    public static WaveManager Instance { get; private set; }

    public enum State { Fighting, Intermission, Won, Lost }

    [Header("Wave")]
    public int totalWaves = 20;
    public float baseWaveDuration = 25f;
    public float waveDurationGrowth = 2f;
    public float maxWaveDuration = 60f;
    public float intermission = 2.5f;

    [Header("Spawn")]
    public EnemyType[] enemies;
    public Transform[] spawnPoints;
    public float baseSpawnInterval = 1.6f;
    public float minSpawnInterval = 0.25f;
    public int maxAlive = 40;

    [Header("Độ khó tăng mỗi wave")]
    public float healthGrowth = 0.18f;
    public float damageGrowth = 0.08f;

    [Header("Tham chiếu")]
    public PlayerHealth playerHealth;
    public UpgradeManager upgradeManager;
    public Text waveText;
    public Text timerText;
    public Text bannerText;
    public GameObject xpGemPrefab;
    public RectTransform popupContainer;
    public Font popupFont;

    public int Wave { get; private set; }
    public State Current { get; private set; }

    float waveTimer;
    float spawnTimer;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        XpGem.prefab = xpGemPrefab;
        DamagePopup.container = popupContainer;
        DamagePopup.font = popupFont;
    }

    void Start()
    {
        StartCoroutine(Run());
    }

    float WaveDuration(int w) => Mathf.Min(maxWaveDuration, baseWaveDuration + (w - 1) * waveDurationGrowth);
    float SpawnInterval(int w) => Mathf.Max(minSpawnInterval, baseSpawnInterval * Mathf.Pow(0.88f, w - 1));

    IEnumerator Run()
    {
        for (Wave = 1; Wave <= totalWaves; Wave++)
        {
            yield return ShowBanner("WAVE " + Wave, 1.2f);

            Current = State.Fighting;
            waveTimer = WaveDuration(Wave);
            spawnTimer = 0f;
            while (waveTimer > 0f)
            {
                if (playerHealth.IsDead)
                {
                    Current = State.Lost;
                    yield break;
                }
                waveTimer -= Time.deltaTime;
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    spawnTimer = SpawnInterval(Wave);
                    TrySpawn();
                    // Đôi khi spawn theo cụm
                    if (Random.value < Mathf.Min(0.5f, Wave * 0.04f)) TrySpawn();
                }
                UpdateHud();
                yield return null;
            }

            Current = State.Intermission;
            UpdateHud();
            ClearWave();
            yield return ShowBanner(Wave == totalWaves ? "" : "WAVE CLEAR!", intermission);

            // Dồn các lần lên cấp lại để chọn giữa hai wave, giống Brotato
            yield return upgradeManager.ProcessPendingLevelUps();
        }

        Current = State.Won;
    }

    void TrySpawn()
    {
        if (CountAlive() >= maxAlive) return;
        var type = PickEnemy();
        if (type == null) return;

        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
        var go = Instantiate(type.prefab, point.position + offset, point.rotation);

        float hpMul = 1f + healthGrowth * (Wave - 1);
        float dmgMul = 1f + damageGrowth * (Wave - 1);
        go.GetComponent<EnemyHealth>().Scale(hpMul);
        go.GetComponent<EnemyAttack>().Scale(dmgMul);
    }

    EnemyType PickEnemy()
    {
        float total = 0f;
        foreach (var e in enemies) if (Wave >= e.unlockWave) total += e.weight;
        float r = Random.value * total;
        foreach (var e in enemies)
        {
            if (Wave < e.unlockWave) continue;
            r -= e.weight;
            if (r <= 0f) return e;
        }
        return null;
    }

    static int CountAlive()
    {
        int n = 0;
        foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            if (!e.IsDead) n++;
        return n;
    }

    void ClearWave()
    {
        foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            e.Death(false);
        foreach (var g in FindObjectsByType<XpGem>(FindObjectsSortMode.None))
            g.Attract();
    }

    void UpdateHud()
    {
        if (waveText != null) waveText.text = "WAVE " + Mathf.Min(Wave, totalWaves) + " / " + totalWaves;
        if (timerText != null)
        {
            int t = Mathf.CeilToInt(Mathf.Max(0f, waveTimer));
            timerText.text = t.ToString();
            timerText.color = t <= 5 && Current == State.Fighting ? new Color(1f, 0.4f, 0.3f) : Color.white;
        }
    }

    IEnumerator ShowBanner(string text, float duration)
    {
        if (bannerText == null || string.IsNullOrEmpty(text))
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }
        bannerText.text = text;
        bannerText.gameObject.SetActive(true);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Min(1f, t * 4f) * Mathf.Min(1f, (duration - t) * 4f);
            var c = bannerText.color;
            c.a = a;
            bannerText.color = c;
            bannerText.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Mathf.Min(1f, t * 4f));
            yield return null;
        }
        bannerText.gameObject.SetActive(false);
    }
}
