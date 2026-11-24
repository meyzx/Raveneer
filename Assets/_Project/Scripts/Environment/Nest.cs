using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Nest : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Deposit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Deposit(other);
    }

    private void Deposit(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.heldItem == null) return;

        var pickup = player.heldItem.GetComponent<ItemPickup>();
        if (pickup == null || pickup.data == null) return;

        switch (pickup.data.type)
        {
            case ItemType.Treasure:
            case ItemType.Key:
                if (DataPersistence.Instance != null)
                    DataPersistence.Instance.AddToNest(pickup.data.id, pickup.data.type == ItemType.Key);
                player.ClearHeldItem(true);
                if (UIManager.Instance != null) UIManager.Instance.RefreshNestCount();
                break;

            case ItemType.Food:
                player.Eat(pickup.data.healAmount);
                player.ClearHeldItem(true);
                break;
        }
    }
}
