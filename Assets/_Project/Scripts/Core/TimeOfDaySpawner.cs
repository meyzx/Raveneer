using System.Collections.Generic;
using UnityEngine;

public class TimeOfDaySpawner : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform container;
    [SerializeField] private List<EnemyStats> enemies = new List<EnemyStats>();
    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 25f;
    [SerializeField] private float firstDelayMin = 3f;
    [SerializeField] private float firstDelayMax = 9f;
    [SerializeField] private float spawnRadius = 14f;
    [SerializeField] private int maxAlive = 3;
    [SerializeField] private float warningSeconds = 3f;

    private float timer;
    private float pendingTimer;
    private int pendingCount;
    private EnemyStats pending;
    private float nextDelay;
    private readonly List<GameObject> alive = new List<GameObject>();

    void Start()
    {
        nextDelay = Random.Range(firstDelayMin, firstDelayMax);
    }

    public static EnemyTimeSlot CurrentSlot()
    {
        return DayCycle.IsNight ? EnemyTimeSlot.Night : EnemyTimeSlot.Day;
    }

    void Update()
    {
        if (player == null) return;

        if (ARCameraToggle.Active)
        {
            pending = null;
            return;
        }

        if (pending != null)
        {
            pendingTimer -= Time.deltaTime;
            if (pendingTimer > 0f) return;

            alive.RemoveAll(e => e == null);
            for (int i = 0; i < pendingCount && alive.Count < maxAlive; i++) Spawn(pending);
            pending = null;
            return;
        }

        timer += Time.deltaTime;
        if (timer < nextDelay) return;

        timer = 0f;
        nextDelay = Random.Range(minDelay, maxDelay);

        alive.RemoveAll(e => e == null);
        if (alive.Count >= maxAlive) return;

        EnemyTimeSlot slot = CurrentSlot();
        var candidates = enemies.FindAll(e => e != null && e.prefab != null && e.mode == EnemyMode.World && e.activeDuring == slot);
        if (candidates.Count == 0) return;

        pending = candidates[Random.Range(0, candidates.Count)];
        pendingCount = Random.value < 0.25f ? 2 : 1;
        pendingTimer = warningSeconds;
        DangerAlert.Raise(pending.enemyName, warningSeconds);
    }

    private void Spawn(EnemyStats stats)
    {
        Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.8f, spawnRadius * 1.2f);
        Vector3 position = player.transform.position + new Vector3(ring.x, ring.y, 0f);

        GameObject instance = Instantiate(stats.prefab, position, Quaternion.identity, container);
        var behaviour = instance.GetComponent<EnemyBehaviour>();
        if (behaviour != null) behaviour.Initialize(stats, player);
        alive.Add(instance);
    }
}
