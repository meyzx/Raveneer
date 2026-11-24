using UnityEngine;

public enum EnemyTimeSlot { Day, Night }

public enum EnemyMode { World, AR }

public enum DamageType { Instant, Poison, Stun }

[CreateAssetMenu(menuName = "Raveneer/Enemy Stats", fileName = "NewEnemy")]
public class EnemyStats : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;
    public EnemyMode mode;
    public EnemyTimeSlot activeDuring;
    public DamageType damageType;
    public bool flying;
    public float damage = 20f;
    public float effectSeconds = 4f;
    public float speed = 3f;
    public float detectionRadius = 12f;
    public float arApproachSeconds = 6f;
}
