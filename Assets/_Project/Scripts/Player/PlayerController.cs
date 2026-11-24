using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static event Action<string> OnPlayerDied;
    public static event Action<float> OnHealthChanged;
    public static event Action<float> OnHungerChanged;

    [Header("Supervivencia")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float dayLengthSeconds = 240f;

    [Header("Vuelo")]
    [SerializeField] private float flySpeed = 4.5f;
    [SerializeField] private float turnSpeed = 280f;
    [SerializeField] private float swipeDeadzone = 1.5f;
    [SerializeField] private float swipeSmoothing = 0.35f;
    [SerializeField] private float worldRadius = 60f;
    [SerializeField] private float pickupRadius = 3.5f;
    [SerializeField] private float throwDistance = 3f;

    [Header("Almacenamiento (Máximo 1 Item)")]
    public GameObject heldItem;

    [Header("Efectos de Sonido")]
    [SerializeField] private AudioSource crowAudioSource;
    [SerializeField] private AudioClip laughClip;

    private float heading;
    private float targetHeading;
    private Vector2 swipeDirection = Vector2.up;
    private bool isDead;
    private float foodTimer;
    private float poisonLeft;
    private float poisonRate;
    private float stunTimer;
    private string poisonSource = "Veneno";

    public bool FlightEnabled { get; set; } = true;
    public float SurvivalTime { get; private set; }

    private HurtFeedback feedback;

    void Awake()
    {
        feedback = gameObject.AddComponent<HurtFeedback>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        heading = transform.eulerAngles.z + 90f;
        targetHeading = heading;
        OnHealthChanged?.Invoke(1f);
        OnHungerChanged?.Invoke(1f);
    }

    void Update()
    {
        if (isDead) return;
        if (ARCameraToggle.Active) return;

        SurvivalTime += Time.deltaTime;
        ManageHunger();
        ManageEffects();
        Fly();
    }

    private void ManageHunger()
    {
        foodTimer += Time.deltaTime;
        OnHungerChanged?.Invoke(Mathf.Clamp01(1f - foodTimer / dayLengthSeconds));
        if (foodTimer >= dayLengthSeconds) Die("Sin comida durante el día");
    }

    private void ManageEffects()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;

        if (poisonLeft <= 0f) return;

        float tick = Mathf.Min(poisonLeft, poisonRate * Time.deltaTime);
        poisonLeft -= tick;
        if (tick > 0f && feedback != null) feedback.Hit(DamageType.Poison);
        TakeDamage(tick, poisonSource);
    }

    private void Fly()
    {
        if (!FlightEnabled || stunTimer > 0f) return;

        heading = Mathf.MoveTowardsAngle(heading, targetHeading, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, heading - 90f);

        Vector2 direction = new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad));
        Vector2 position = (Vector2)transform.position + direction * flySpeed * Time.deltaTime;

        if (position.magnitude > worldRadius) position = position.normalized * worldRadius;

        transform.position = new Vector3(position.x, position.y, 0f);
    }

    public void ProcessSwipe(Vector2 delta)
    {
        if (delta.sqrMagnitude < swipeDeadzone * swipeDeadzone) return;

        swipeDirection = Vector2.Lerp(swipeDirection, delta.normalized, swipeSmoothing);
        if (swipeDirection.sqrMagnitude < 0.0001f) return;

        targetHeading = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
    }

    public bool TryHold(GameObject item)
    {
        if (heldItem != null || item == null) return false;

        heldItem = item;
        heldItem.SetActive(false);
        var pickup = item.GetComponent<ItemPickup>();
        if (pickup != null && !pickup.counted && DataPersistence.Instance != null)
        {
            pickup.counted = true;
            DataPersistence.Instance.AddItemCollected();
        }
        return true;
    }

    public void TryPickUpItem()
    {
        if (heldItem != null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
        GameObject best = null;
        float bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Collectible") && !hit.CompareTag("Consumable")) continue;
            if (!hit.gameObject.activeInHierarchy) continue;

            float distance = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = hit.gameObject;
        }

        if (best != null) TryHold(best);
    }

    public void TryThrowItem()
    {
        if (heldItem == null) return;

        if (crowAudioSource != null && laughClip != null)
            crowAudioSource.PlayOneShot(laughClip, PauseMenu.SfxVolume);

        Vector3 position = transform.position + transform.up * throwDistance;
        position.z = 0f;

        heldItem.transform.position = position;
        heldItem.SetActive(true);
        heldItem = null;
    }

    public void ClearHeldItem(bool destroy)
    {
        if (heldItem != null && destroy) Destroy(heldItem);
        heldItem = null;
    }

    public void Eat(float amount)
    {
        foodTimer = 0f;
        OnHungerChanged?.Invoke(1f);
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(Mathf.Clamp01(currentHealth / maxHealth));
    }

    public void ApplyHit(EnemyStats stats, string source)
    {
        if (isDead || stats == null) return;

        if (feedback != null) feedback.Hit(stats.damageType);

        switch (stats.damageType)
        {
            case DamageType.Poison:
                poisonLeft += stats.damage;
                poisonRate = stats.damage / Mathf.Max(0.5f, stats.effectSeconds);
                poisonSource = source;
                break;

            case DamageType.Stun:
                stunTimer = Mathf.Max(stunTimer, stats.effectSeconds);
                TakeDamage(stats.damage, source);
                break;

            default:
                TakeDamage(stats.damage, source);
                break;
        }
    }

    public void TakeDamage(float amount, string source)
    {
        if (isDead) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(Mathf.Clamp01(currentHealth / maxHealth));
        if (currentHealth <= 0f) Die(source);
    }

    private void Die(string reason)
    {
        if (isDead) return;

        isDead = true;
        OnPlayerDied?.Invoke(reason);
    }
}
