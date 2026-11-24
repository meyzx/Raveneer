using UnityEngine;

public enum ItemType { Treasure, Key, Food }

[CreateAssetMenu(menuName = "Raveneer/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    public string id;
    public string displayName;
    public ItemType type;
    public float healAmount = 25f;
    public Sprite icon;
}
