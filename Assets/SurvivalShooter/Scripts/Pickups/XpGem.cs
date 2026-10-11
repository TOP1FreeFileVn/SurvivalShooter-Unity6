using UnityEngine;

// Viên kinh nghiệm rơi ra khi quái chết. Vào tầm nam châm (PlayerStats.pickupRange) thì bay về người chơi.
public class XpGem : MonoBehaviour
{
    public static GameObject prefab;

    public int value = 1;
    public float flySpeed = 14f;
    public float collectDistance = 0.6f;

    Transform player;
    PlayerStats stats;
    bool attracted;
    float bobOffset;
    Vector3 basePos;

    public static void Spawn(Vector3 pos, int value)
    {
        if (prefab == null) return;
        pos.y = 0.4f;
        pos += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        var gem = Instantiate(prefab, pos, Quaternion.identity).GetComponent<XpGem>();
        gem.value = value;
        // Viên to hơn cho quái nhiều EXP
        gem.transform.localScale *= Mathf.Lerp(1f, 1.8f, Mathf.InverseLerp(1, 10, value));
    }

    void Start()
    {
        stats = PlayerStats.Instance;
        player = stats.transform;
        bobOffset = Random.value * 10f;
        basePos = transform.position;
    }

    void Update()
    {
        Vector3 target = player.position + Vector3.up * 0.6f;
        float dist = Vector3.Distance(transform.position, target);

        if (!attracted && dist <= stats.pickupRange) attracted = true;

        if (attracted)
        {
            flySpeed += 20f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * Time.deltaTime);
            if (dist <= collectDistance) Collect();
        }
        else
        {
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3f + bobOffset) * 0.12f;
        }
        transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
    }

    // Gọi khi hết wave để hút hết EXP còn trên sàn
    public void Attract() => attracted = true;

    void Collect()
    {
        stats.AddXp(value);
        PickupSound.Play();
        Destroy(gameObject);
    }
}
