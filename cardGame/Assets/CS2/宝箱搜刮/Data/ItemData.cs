using UnityEngine;

namespace ScavengingGame
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Scavenge/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("基本信息")]
        public string itemId;
        public string itemName = "Unknown Item";
        [TextArea]
        public string description = "A generic scavenged item.";
        public Sprite icon;
        //public int maxStackSize = 99;// 最大堆叠数
        
       
        
        [SerializeField] public int maxStackSize = 99;
        
        [Header("物品类别")]
        public ItemCategory category = ItemCategory.Miscellaneous;

        [Header("价值与重量")]
        public int value = 1;
        public float weight = 0.1f;

        [Header("稀有度")]
        public ItemRarity rarity = ItemRarity.Common;

        [Header("使用属性")]
        public bool isStackable = true;
        public bool isEquippable = false;
        public bool isConsumable = false;
        public AudioClip useSound;
        public AudioClip pickupSound;

        // 物品类型枚举
        public enum ItemCategory
        {
            Miscellaneous,
            Consumable,
            Equipment,
            Material,
            Quest,
            Key
        }
        
        // 使用物品
        public virtual void Use()
        {
            Debug.Log($"使用物品: {itemName}");
        }
        
        // 获取物品描述
        public virtual string GetTooltipText()
        {
            string tooltip = $"<b>{itemName}</b>\n";
            tooltip += $"{description}\n";
            tooltip += $"类别: {GetCategoryName(category)}\n";
            tooltip += $"稀有度: {GetRarityName(rarity)}\n";
            
            if (maxStackSize > 1)
            {
                tooltip += $"最大堆叠: {maxStackSize}\n";
            }
            
            tooltip += $"价值: {value}金币\n";
            tooltip += $"重量: {weight}kg";
            
            return tooltip;
        }
        
        // 获取类别名称
        private string GetCategoryName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Miscellaneous: return "杂项";
                case ItemCategory.Consumable: return "消耗品";
                case ItemCategory.Equipment: return "装备";
                case ItemCategory.Material: return "材料";
                case ItemCategory.Quest: return "任务物品";
                case ItemCategory.Key: return "钥匙";
                default: return "未知";
            }
        }
        
        // 获取稀有度名称
        public static string GetRarityName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "普通";
                case ItemRarity.Uncommon: return "优秀";
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Epic: return "史诗";
                case ItemRarity.Legendary: return "传奇";
                default: return "未知";
            }
        }
        
        // 获取稀有度颜色
        public static Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return new Color(0.6f, 0.2f, 0.8f); // 紫色
                case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f); // 橙色
                default: return Color.white;
            }
        }
        
        // 创建测试物品的方法
        public static ItemData CreateTestItem(string id, string name, Sprite icon = null)
        {
            ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.itemId = id;
            itemData.itemName = name;
            itemData.description = $"这是一个测试物品: {name}";
            itemData.icon = icon;
            itemData.maxStackSize = 99;
            itemData.value = 10;
            itemData.rarity = ItemRarity.Common;
            return itemData;
        }

         // ==================== 属性（用于修复兼容性问题） ====================
        
        // ItemName 属性（兼容代码中的 item.ItemName）
        public string ItemName
        {
            get { return itemName; }
            set { itemName = value; }
        }
        
        // Description 属性（兼容代码中的 item.Description）
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        
        // Icon 属性（兼容代码中的 item.Icon）
        public Sprite Icon
        {
            get { return icon; }
            set { icon = value; }
        }
        
        // StackSize 属性（兼容代码中的 item.StackSize）
        public int StackSize
        {
            get { return maxStackSize; }
            set { maxStackSize = value; }
        }
        
        // TermName 属性（供需要 TermName 的地方使用）
        
    }
}