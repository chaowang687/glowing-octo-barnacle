using UnityEngine;

namespace ScavengingGame
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Scavenge/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("基本信息")]
        public string ItemName = "Unknown Item";
        [TextArea]
        public string Description = "A generic scavenged item.";
        public Sprite Icon; 
        public int StackSize = 1;

        [Header("物品类别")]
        public ItemCategory category = ItemCategory.Miscellaneous;

        [Header("价值与重量")]
        public int Value = 1;
        public float Weight = 0.1f;

        [Header("使用属性")]
        public bool IsStackable = true;
        public bool IsConsumable = false;
        public AudioClip UseSound;
        public AudioClip PickupSound;
    }

    public enum ItemCategory
    {
        Miscellaneous,
        Consumable,
        Equipment,
        Material,
        Quest,
        Key
    }
}