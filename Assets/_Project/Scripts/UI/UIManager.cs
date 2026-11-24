using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private PlayerController player;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Text survivalTimeText;
    [SerializeField] private Text heldItemText;
    [SerializeField] private Text nestCountText;
    [SerializeField] private GameObject notePanel;
    [SerializeField] private Text noteText;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        PlayerController.OnHealthChanged += SetHealth;
        PlayerController.OnHungerChanged += SetHunger;
    }

    void OnDisable()
    {
        PlayerController.OnHealthChanged -= SetHealth;
        PlayerController.OnHungerChanged -= SetHunger;
    }

    void Start()
    {
        RefreshNestCount();
    }

    void Update()
    {
        if (player == null) return;

        if (survivalTimeText != null) survivalTimeText.text = FormatTime(player.SurvivalTime);
        if (heldItemText != null) heldItemText.text = DescribeHeldItem();
    }

    private string DescribeHeldItem()
    {
        if (player.heldItem == null) return "Pico vacío";

        var pickup = player.heldItem.GetComponent<ItemPickup>();
        if (pickup != null && pickup.data != null) return pickup.data.displayName;
        return player.heldItem.name;
    }

    private void SetHealth(float normalized)
    {
        if (healthBar != null) healthBar.value = normalized;
    }

    private void SetHunger(float normalized)
    {
        if (hungerBar != null) hungerBar.value = normalized;
    }

    public void RefreshNestCount()
    {
        if (nestCountText == null || DataPersistence.Instance == null) return;

        SaveData data = DataPersistence.Instance.Data;
        nestCountText.text = "Tesoros " + data.nestItems.Count + "   Llaves " + data.keysCollected.Count + "/" + DataPersistence.TOTAL_KEYS;
    }

    public void ShowNote(string text)
    {
        if (noteText != null) noteText.text = text;
        if (notePanel != null) notePanel.SetActive(true);
    }

    public void HideNote()
    {
        if (notePanel != null) notePanel.SetActive(false);
    }

    public static string FormatTime(float seconds)
    {
        return ((int)(seconds / 60f)).ToString("00") + ":" + ((int)(seconds % 60f)).ToString("00");
    }
}
