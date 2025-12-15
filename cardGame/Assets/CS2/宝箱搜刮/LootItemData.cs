// LootItemData.cs
using UnityEngine;
using System;

[Serializable]
public class LootItemData
{
    [Header("基础信息")]
    public string itemId;                      // 物品唯一ID
    public string itemName;                    // 物品名称
    public Sprite icon;                        // 物品图标
    public int quantity = 1;                   // 物品数量
    public ItemRarity rarity = ItemRarity.Common; // 物品稀有度
    
    [Header("物品类型")]
    public int itemType = 1;  
    
    [Header("动画和视觉")]
    public float findAnimationDuration = 1.0f; // 查找动画时长
    public Color glowColor = Color.white;      // 发光颜色
    
    [Header("物品属性")]
    public bool isEquipment = false;           // 是否是装备
    public bool isConsumable = false;          // 是否是消耗品
    public bool isQuestItem = false;           // 是否是任务物品
    public int itemLevel = 1;                  // 物品等级
    public int value = 10;                     // 物品价值
    
    // 构造函数
    public LootItemData() { }
    
    public LootItemData(string id, string name, Sprite icon, ItemRarity rarity, int quantity = 1)
    {
        this.itemId = id;
        this.itemName = name;
        this.icon = icon;
        this.rarity = rarity;
        this.quantity = quantity;
        
        SetDefaultsByRarity();
    }
    
    private void SetDefaultsByRarity()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                findAnimationDuration = 0.5f;
                glowColor = Color.white;
                value = 10;
                break;
            case ItemRarity.Uncommon:
                findAnimationDuration = 1.0f;
                glowColor = Color.green;
                value = 50;
                break;
            case ItemRarity.Rare:
                findAnimationDuration = 1.5f;
                glowColor = Color.blue;
                value = 200;
                break;
            case ItemRarity.Epic:
                findAnimationDuration = 2.0f;
                glowColor = new Color(0.6f, 0.2f, 0.8f);
                value = 1000;
                break;
            case ItemRarity.Legendary:
                findAnimationDuration = 2.5f;
                glowColor = new Color(1f, 0.5f, 0f);
                value = 5000;
                break;
        }
    }
    
    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
            case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f);
            default: return Color.white;
        }
    }
    
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
}