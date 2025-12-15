// EquipmentData.cs
using UnityEngine;

namespace ScavengingGame
{
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Scavenge/Equipment Data")]
    public class EquipmentData : ItemData
    {
        public enum SlotType
        {
            Weapon = 0,
            Armor = 1,
            Helmet = 2,
            Gloves = 3,
            Boots = 4,
            Shield = 5,
            Ring1 = 6,
            Ring2 = 7,
            Amulet1 = 8,
            Amulet2 = 9
        }
        
        [Header("装备属性")]
        public SlotType slotType;
        public int attackBonus = 0;
        public int defenseBonus = 0;
        public int healthBonus = 0;
        public int manaBonus = 0;
        
        // ==================== 属性（用于修复兼容性问题） ====================
        
        // Slot 属性（兼容代码中的 equipment.Slot）
        public SlotType Slot
        {
            get { return slotType; }
            set { slotType = value; }
        }
        
        // Icon 属性（兼容代码中的 equipment.Icon）
        public new Sprite Icon
        {
            get { return icon; }
            set { icon = value; }
        }
        
        // ItemName 属性（兼容代码中的 equipment.ItemName）
        public new string ItemName
        {
            get { return itemName; }
            set { itemName = value; }
        }
        
        // Description 属性（兼容代码中的 equipment.Description）
        public new string Description
        {
            get { return description; }
            set { description = value; }
        }
        
        // AttackBonus 属性（兼容代码中的 equipment.AttackBonus）
        public int AttackBonus
        {
            get { return attackBonus; }
            set { attackBonus = value; }
        }
        
        // DefenseBonus 属性（兼容代码中的 equipment.DefenseBonus）
        public int DefenseBonus
        {
            get { return defenseBonus; }
            set { defenseBonus = value; }
        }
        
        // StackSize 属性（兼容代码中的 equipment.StackSize）
        public new int StackSize
        {
            get { return maxStackSize; }
            set { maxStackSize = value; }
        }
        
        // TermName 属性（供需要 TermName 的地方使用）
        public string TermName
        {
            get { return itemName; }
            set { itemName = value; }
        }
        
        // ==================== 构造函数 ====================
        
        public EquipmentData()
        {
            isEquippable = true;
            category = ItemCategory.Equipment;
        }
        
        // ==================== 方法重写 ====================
        
        public override void Use()
        {
            Debug.Log($"装备物品: {itemName}");
        }
        
        public override string GetTooltipText()
        {
            string tooltip = base.GetTooltipText();
            
            tooltip += "\n<color=yellow>装备属性:</color>";
            
            if (attackBonus > 0)
                tooltip += $"\n攻击力: +{attackBonus}";
            else if (attackBonus < 0)
                tooltip += $"\n攻击力: {attackBonus}";
                
            if (defenseBonus > 0)
                tooltip += $"\n防御力: +{defenseBonus}";
            else if (defenseBonus < 0)
                tooltip += $"\n防御力: {defenseBonus}";
                
            if (healthBonus > 0)
                tooltip += $"\n生命值: +{healthBonus}";
            else if (healthBonus < 0)
                tooltip += $"\n生命值: {healthBonus}";
                
            if (manaBonus > 0)
                tooltip += $"\n魔法值: +{manaBonus}";
            else if (manaBonus < 0)
                tooltip += $"\n魔法值: {manaBonus}";
            
            tooltip += $"\n装备位置: {GetSlotName(slotType)}";
            
            return tooltip;
        }
        
        // ==================== 静态方法 ====================
        
        private string GetSlotName(SlotType slotType)
        {
            switch (slotType)
            {
                case SlotType.Weapon: return "Weapon";
                case SlotType.Armor: return "护甲";
                case SlotType.Helmet: return "头盔";
                case SlotType.Gloves: return "手套";
                case SlotType.Boots: return "靴子";
                case SlotType.Shield: return "盾牌";
                case SlotType.Ring1: return "戒指1";
                case SlotType.Ring2: return "戒指2";
                case SlotType.Amulet1: return "护符1";
                case SlotType.Amulet2: return "护符2";
                default: return "未知";
            }
        }
        
        // 获取槽位显示名称（静态方法）
        public static string GetSlotDisplayName(SlotType slotType)
        {
            switch (slotType)
            {
                case SlotType.Weapon: return "Weapon";
                case SlotType.Armor: return "护甲";
                case SlotType.Helmet: return "头盔";
                case SlotType.Gloves: return "手套";
                case SlotType.Boots: return "靴子";
                case SlotType.Shield: return "盾牌";
                case SlotType.Ring1: return "戒指1";
                case SlotType.Ring2: return "戒指2";
                case SlotType.Amulet1: return "护符1";
                case SlotType.Amulet2: return "护符2";
                default: return "未知";
            }
        }
        
        // 获取槽位图标名称
        public static string GetSlotIconName(SlotType slotType)
        {
            switch (slotType)
            {
                case SlotType.Weapon: return "Icon_Weapon";
                case SlotType.Armor: return "Icon_Armor";
                case SlotType.Helmet: return "Icon_Helmet";
                case SlotType.Gloves: return "Icon_Gloves";
                case SlotType.Boots: return "Icon_Boots";
                case SlotType.Shield: return "Icon_Shield";
                case SlotType.Ring1: return "Icon_Ring";
                case SlotType.Ring2: return "Icon_Ring";
                case SlotType.Amulet1: return "Icon_Amulet";
                case SlotType.Amulet2: return "Icon_Amulet";
                default: return "Icon_Default";
            }
        }
        
        // 创建测试装备
        public static EquipmentData CreateTestEquipment(string id, string name, SlotType slotType, Sprite icon = null)
        {
            EquipmentData equipment = ScriptableObject.CreateInstance<EquipmentData>();
            equipment.itemId = id;
            equipment.itemName = name;
            equipment.description = $"这是一件测试装备: {name}";
            equipment.icon = icon;
            equipment.maxStackSize = 1;
            equipment.slotType = slotType;
            equipment.attackBonus = Random.Range(1, 10);
            equipment.defenseBonus = Random.Range(1, 10);
            equipment.rarity = ItemRarity.Common;
            equipment.category = ItemCategory.Equipment;
            equipment.isEquippable = true;
            
            return equipment;
        }
        
        // 创建稀有装备
        public static EquipmentData CreateRareEquipment(string id, string name, SlotType slotType, ItemRarity rarity, Sprite icon = null)
        {
            EquipmentData equipment = CreateTestEquipment(id, name, slotType, icon);
            equipment.rarity = rarity;
            
            // 根据稀有度调整属性
            switch (rarity)
            {
                case ItemRarity.Common:
                    equipment.attackBonus = Random.Range(1, 5);
                    equipment.defenseBonus = Random.Range(1, 5);
                    equipment.value = 100;
                    break;
                case ItemRarity.Uncommon:
                    equipment.attackBonus = Random.Range(5, 10);
                    equipment.defenseBonus = Random.Range(5, 10);
                    equipment.value = 300;
                    break;
                case ItemRarity.Rare:
                    equipment.attackBonus = Random.Range(10, 20);
                    equipment.defenseBonus = Random.Range(10, 20);
                    equipment.value = 800;
                    break;
                case ItemRarity.Epic:
                    equipment.attackBonus = Random.Range(20, 35);
                    equipment.defenseBonus = Random.Range(20, 35);
                    equipment.value = 2000;
                    break;
                case ItemRarity.Legendary:
                    equipment.attackBonus = Random.Range(35, 50);
                    equipment.defenseBonus = Random.Range(35, 50);
                    equipment.value = 5000;
                    break;
            }
            
            return equipment;
        }
    }
}