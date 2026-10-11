using UnityEngine;

// Spawn quái định kỳ; tốc độ spawn tăng dần theo thời gian
public class EnemyManager : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public GameObject enemy;
    public float spawnTime = 3f;
    public float minSpawnTime = 1f;
    public float rampPerSpawn = 0.03f;
    public float firstSpawnDelay = 2f;
    public Transform[] spawnPoints;

    float timer;

    void Start()
    {
        timer = spawnTime - firstSpawnDelay;
    }

    void Update()
    {
        if (playerHealth.currentHealth <= 0) return;

        timer += Time.deltaTime;
        if (timer >= spawnTime)
        {
            timer = 0f;
            Spawn();
            spawnTime = Mathf.Max(minSpawnTime, spawnTime - rampPerSpawn);
        }
    }

    void Spawn()
    {
        int i = Random.Range(0, spawnPoints.Length);
        Instantiate(enemy, spawnPoints[i].position, spawnPoints[i].rotation);
    }
}
