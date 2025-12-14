using UnityEngine;

namespace ScavengingGame
{
    /// <summary>
    /// 装备数据类，继承自ItemData
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Scavenge/Equipment Data")]
    public class EquipmentData : ItemData
    {
        [Header("装备属性")]
        [Tooltip("装备部位")]
        public SlotType Slot = SlotType.Weapon;
        
        [Tooltip("攻击力加成")]
        public int AttackBonus = 0;
        
        [Tooltip("防御力加成")]
        public int DefenseBonus = 0;
        
        [Tooltip("生命值加成")]
        public int HealthBonus = 0;
        
        [Tooltip("魔法值加成")]
        public int ManaBonus = 0;
        
        [Header("装备要求")]
        [Tooltip("所需等级")]
        public int RequiredLevel = 1;
        
        [Tooltip("所需力量")]
        public int RequiredStrength = 0;
        
        [Tooltip("所需敏捷")]
        public int RequiredDexterity = 0;
        
        [Tooltip("所需智力")]
        public int RequiredIntelligence = 0;
        
        [Header("装备耐久")]
        [Tooltip("最大耐久度")]
        public int MaxDurability = 100;
        
        [HideInInspector]
        [Tooltip("当前耐久度（运行时）")]
        public int CurrentDurability = 100;
        
        [Tooltip("装备品质")]
        public Rarity EquipmentRarity = Rarity.Common;
        
        [Header("特殊效果")]
        [Tooltip("特殊能力描述")]
        [TextArea(1, 3)]
        public string SpecialAbility = "";
        
        /// <summary>
        /// 装备部位枚举
        /// </summary>
        public enum SlotType
        {
            Weapon,     // 武器
            Armor,      // 护甲/衣服
            Helmet,     // 头盔
            Gloves,     // 手套
            Boots,      // 靴子
            Shield,     // 盾牌
            Ring1,      // 戒指1
            Ring2,      // 戒指2
            Amulet1,    // 护符1
            Amulet2,    // 护符2
            Backpack,   // 背包
            Belt,       // 腰带
            Quiver,     // 箭袋
            Relic,      // 遗物
            Trinket     // 饰品
        }
        
        /// <summary>
        /// 装备品质枚举
        /// </summary>
        public enum Rarity
        {
            Common,     // 普通
            Uncommon,   // 优秀
            Rare,       // 稀有
            Epic,       // 史诗
            Legendary,  // 传说
            Artifact    // 神器
        }
        
        // 构造函数
        protected void OnEnable()
        {
            // 初始化当前耐久度
            CurrentDurability = MaxDurability;
        }
        
        /// <summary>
        /// 获取装备槽位的中文名称
        /// </summary>
        public string GetSlotName()
        {
            return GetSlotName(Slot);
        }
        
        /// <summary>
        /// 获取装备槽位的中文名称
        /// </summary>
        public static string GetSlotName(SlotType slot)
        {
            switch (slot)
            {
                case SlotType.Weapon: return "武器";
                case SlotType.Armor: return "护甲";
                case SlotType.Helmet: return "头盔";
                case SlotType.Gloves: return "手套";
                case SlotType.Boots: return "靴子";
                case SlotType.Shield: return "盾牌";
                case SlotType.Ring1: return "戒指1";
                case SlotType.Ring2: return "戒指2";
                case SlotType.Amulet1: return "护符1";
                case SlotType.Amulet2: return "护符2";
                case SlotType.Backpack: return "背包";
                case SlotType.Belt: return "腰带";
                case SlotType.Quiver: return "箭袋";
                case SlotType.Relic: return "遗物";
                case SlotType.Trinket: return "饰品";
                default: return "未知部位";
            }
        }
        
        /// <summary>
        /// 获取装备品质的颜色
        /// </summary>
        public Color GetRarityColor()
        {
            return GetRarityColor(EquipmentRarity);
        }
        
        /// <summary>
        /// 获取装备品质的颜色
        /// </summary>
        public static Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return Color.white;
                case Rarity.Uncommon: return Color.green;
                case Rarity.Rare: return new Color(0.3f, 0.6f, 1f); // 蓝色
                case Rarity.Epic: return new Color(0.7f, 0.2f, 0.9f); // 紫色
                case Rarity.Legendary: return new Color(1f, 0.5f, 0f); // 橙色
                case Rarity.Artifact: return new Color(1f, 0.2f, 0.2f); // 红色
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// 获取装备的详细属性描述
        /// </summary>
        public string GetDetailedStats()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"装备部位: {GetSlotName()}");
            sb.AppendLine($"品质: {EquipmentRarity}");
            
            if (AttackBonus != 0)
                sb.AppendLine($"攻击力: +{AttackBonus}");
            if (DefenseBonus != 0)
                sb.AppendLine($"防御力: +{DefenseBonus}");
            if (HealthBonus != 0)
                sb.AppendLine($"生命值: +{HealthBonus}");
            if (ManaBonus != 0)
                sb.AppendLine($"魔法值: +{ManaBonus}");
            
            if (RequiredLevel > 1)
                sb.AppendLine($"需要等级: {RequiredLevel}");
            
            sb.AppendLine($"耐久度: {CurrentDurability}/{MaxDurability}");
            
            if (!string.IsNullOrEmpty(SpecialAbility))
            {
                sb.AppendLine();
                sb.AppendLine($"特殊效果: {SpecialAbility}");
            }
            
            return sb.ToString().TrimEnd();
        }
        
        /// <summary>
        /// 使用装备（减少耐久度）
        /// </summary>
        public void UseEquipment(int durabilityLoss = 1)
        {
            CurrentDurability -= durabilityLoss;
            if (CurrentDurability < 0) CurrentDurability = 0;
        }
        
        /// <summary>
        /// 修复装备
        /// </summary>
        public void Repair(int amount)
        {
            CurrentDurability += amount;
            if (CurrentDurability > MaxDurability)
                CurrentDurability = MaxDurability;
        }
        
        /// <summary>
        /// 检查装备是否损坏
        /// </summary>
        public bool IsBroken()
        {
            return CurrentDurability <= 0;
        }
    }
}