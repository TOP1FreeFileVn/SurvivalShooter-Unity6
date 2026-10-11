using UnityEngine;
using UnityEngine.AI;

// Quái đuổi theo người chơi bằng NavMesh
public class EnemyMovement : MonoBehaviour
{
    Transform player;
    PlayerHealth playerHealth;
    EnemyHealth enemyHealth;
    NavMeshAgent nav;

    void Awake()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        player = p.transform;
        playerHealth = p.GetComponent<PlayerHealth>();
        enemyHealth = GetComponent<EnemyHealth>();
        nav = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!nav.enabled || !nav.isOnNavMesh) return;
        if (enemyHealth.currentHealth > 0 && playerHealth.currentHealth > 0)
            nav.SetDestination(player.position);
        else
            nav.isStopped = true;
    }
}
