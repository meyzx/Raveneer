using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    [SerializeField] private float contactDistance = 0.9f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float despawnDistance = 45f;
    [SerializeField] private float lifetime = 40f;

    private EnemyStats stats;
    private PlayerController player;
    private float cooldown;
    private float age;

    public void Initialize(EnemyStats enemyStats, PlayerController target)
    {
        stats = enemyStats;
        player = target;
    }

    void Update()
    {
        if (stats == null || player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        if (distance > despawnDistance)
        {
            Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;
        if (age > lifetime && distance > 14f)
        {
            Destroy(gameObject);
            return;
        }

        cooldown -= Time.deltaTime;

        if (distance > 0.01f)
        {
            Vector2 direction = toPlayer / distance;
            transform.up = direction;
            transform.position += (Vector3)(direction * stats.speed * Time.deltaTime);
        }

        if (distance <= contactDistance && cooldown <= 0f)
        {
            cooldown = attackCooldown;
            player.ApplyHit(stats, "Atrapado por " + stats.enemyName);
        }
    }
}
