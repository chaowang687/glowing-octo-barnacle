using System.Collections.Generic;
using UnityEngine;
using ScavengingGame;

public class LootGenerator : MonoBehaviour
{
    [System.Serializable]
    public class LootTableEntry
    {
        public GameObject itemPrefab;
        public int weight;
        public ItemRarity rarity;
        public int minLevel;
        public int maxLevel;
        public List<ChestType> allowedChestTypes;
    }
    
    [SerializeField] private List<LootTableEntry> lootTable;
    [SerializeField] private float[] rarityProbabilities = new float[] { 0.5f, 0.3f, 0.12f, 0.06f, 0.02f };
    [SerializeField] private int[] minItemsByLevel = new int[] { 1, 2, 3, 4, 5 };
    [SerializeField] private int[] maxItemsByLevel = new int[] { 3, 4, 5, 5, 5 };
    
    public List<LootItemData> GenerateLoot(ChestData chestData)
    {
        List<LootItemData> loot = new List<LootItemData>();
        
        // 有可能箱子是空的（根据等级和类型决定概率）
        float emptyChance = CalculateEmptyChance(chestData);
        if (Random.Range(0f, 1f) < emptyChance)
        {
            return loot; // 返回空列表
        }
        
        // 确定物品数量
        int itemCount = Random.Range(
            minItemsByLevel[Mathf.Min(chestData.chestLevel, minItemsByLevel.Length - 1)],
            maxItemsByLevel[Mathf.Min(chestData.chestLevel, maxItemsByLevel.Length - 1)] + 1
        );
        
        itemCount = Mathf.Min(itemCount, chestData.maxLootSlots);
        
        for (int i = 0; i < itemCount; i++)
        {
            LootItemData item = GenerateSingleItem(chestData);
            if (item != null)
            {
                loot.Add(item);
            }
        }
        
        return loot;
    }
    
    private LootItemData GenerateSingleItem(ChestData chestData)
    {
        // 根据稀有度概率选择物品
        ItemRarity rarity = SelectRarity();
        
        // 过滤符合条件的物品
        List<LootTableEntry> eligibleItems = lootTable.FindAll(entry => 
            entry.rarity == rarity &&
            chestData.chestLevel >= entry.minLevel &&
            chestData.chestLevel <= entry.maxLevel &&
            entry.allowedChestTypes.Contains(chestData.chestType)
        );
        
        if (eligibleItems.Count == 0) return null;
        
        // 加权随机选择
        LootTableEntry selectedEntry = WeightedRandomSelect(eligibleItems);
        
        // --- CS1061 修复开始 ---
        // 1. 获取 Item 组件 (CS0246 错误已通过创建 Item.cs 解决)
        Item itemComponent = selectedEntry.itemPrefab.GetComponent<Item>();
        
        // 检查 Item 组件和 ItemData 是否存在，以确保代码健壮性
        if (itemComponent == null || itemComponent.Data == null)
        {
            Debug.LogError($"Loot item prefab '{selectedEntry.itemPrefab.name}' is missing the Item component or its ItemData is null.", selectedEntry.itemPrefab);
            return null;
        }
        
        // 2. 从 Item 组件获取 ItemData 实例
        ScavengingGame.ItemData itemData = itemComponent.Data;

        return new LootItemData
        {
            // FIX: 从 ItemData 中获取正确的物品信息（而不是 Prefab 名称）
            itemId = itemData.itemId,
            itemName = itemData.itemName,
            
            // FIX: CS1061 修复：通过 itemData.icon 访问图标
            icon = itemData.icon,
            
            quantity = Random.Range(1, GetMaxQuantityByRarity(rarity) + 1),
            rarity = rarity,
            findAnimationDuration = GetAnimationDurationByRarity(rarity),
            
            // FIX: itemType 很可能对应 ItemData 中的 ItemCategory
            itemType = (int)itemData.category 
        };
    }
    
    private ItemRarity SelectRarity()
    {
        float randomValue = Random.Range(0f, 1f);
        float cumulative = 0f;
        
        for (int i = 0; i < rarityProbabilities.Length; i++)
        {
            cumulative += rarityProbabilities[i];
            if (randomValue <= cumulative)
            {
                return (ItemRarity)i;
            }
        }
        
        return ItemRarity.Common;
    }
    
    private LootTableEntry WeightedRandomSelect(List<LootTableEntry> entries)
    {
        int totalWeight = 0;
        foreach (var entry in entries)
        {
            totalWeight += entry.weight;
        }
        
        int randomWeight = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var entry in entries)
        {
            currentWeight += entry.weight;
            if (randomWeight < currentWeight)
            {
                return entry;
            }
        }
        
        return entries[0];
    }
    
    private float CalculateEmptyChance(ChestData chestData)
    {
        // 基础空箱率，可根据等级和类型调整
        float baseEmptyChance = 0.1f;
        float levelModifier = chestData.chestLevel * 0.01f; // 等级越高，空箱率越低
        float typeModifier = (int)chestData.chestType * 0.02f; // 类型越好，空箱率越低
        
        return Mathf.Clamp01(baseEmptyChance - levelModifier - typeModifier);
    }
    
    private int GetMaxQuantityByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 10;
            case ItemRarity.Uncommon: return 5;
            case ItemRarity.Rare: return 3;
            case ItemRarity.Epic: return 2;
            case ItemRarity.Legendary: return 1;
            default: return 1;
        }
    }
    
    private float GetAnimationDurationByRarity(ItemRarity rarity)
    {
        // 越稀有查找时间越长
        switch (rarity)
        {
            case ItemRarity.Common: return 0.5f;
            case ItemRarity.Uncommon: return 1.0f;
            case ItemRarity.Rare: return 1.5f;
            case ItemRarity.Epic: return 2.0f;
            case ItemRarity.Legendary: return 2.5f;
            default: return 0.5f;
        }
    }
}