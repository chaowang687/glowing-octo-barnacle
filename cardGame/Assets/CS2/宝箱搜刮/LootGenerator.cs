using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScavengingGame;

public class LootGenerator : MonoBehaviour
{
    [System.Serializable]
    public class LootTableEntry
    {
        public GameObject itemPrefab;
        public int weight = 100;
        public ItemRarity rarity = ItemRarity.Common;
        public int minLevel = 1;
        public int maxLevel = 5;
        public List<ChestType> allowedChestTypes = new List<ChestType> { ChestType.Wooden, ChestType.Iron, ChestType.Gold };
        
        // 调试信息
        public string GetDebugInfo()
        {
            return $"物品: {itemPrefab?.name}, 权重: {weight}, 稀有度: {rarity}, 等级: {minLevel}-{maxLevel}";
        }
    }

    [Header("=== 战利品表配置 ===")]
    [SerializeField] private List<LootTableEntry> lootTable = new List<LootTableEntry>();
    
    [Header("=== 稀有度概率配置 ===")]
    [Tooltip("普通, 优秀, 稀有, 史诗, 传奇 的概率")]
    [SerializeField] private float[] rarityProbabilities = new float[] { 0.5f, 0.3f, 0.12f, 0.06f, 0.02f };
    
    [Header("=== 物品数量配置 ===")]
    [Tooltip("每个等级最少物品数")]
    [SerializeField] private int[] minItemsByLevel = new int[] { 1, 2, 3, 4, 5 };
    
    [Tooltip("每个等级最多物品数")]
    [SerializeField] private int[] maxItemsByLevel = new int[] { 3, 4, 5, 5, 5 };
    
    [Header("=== 单个槽位空置率配置 ===")]
    [Tooltip("每个槽位为空的基础概率（0=总是有物品，1=总是空）")]
    [SerializeField] private float[] slotEmptyChanceByLevel = new float[] { 0.3f, 0.25f, 0.2f, 0.15f, 0.1f };
    
    [Tooltip("最少保证的物品数量")]
    [SerializeField] private int[] minGuaranteedItems = new int[] { 1, 1, 2, 2, 3 };
    
    [Tooltip("宝箱类型对空置率的影响系数")]
    [SerializeField] private float[] chestTypeMultipliers = new float[] { 1.0f, 0.8f, 0.6f };
    
    [Tooltip("是否启用单个槽位空置")]
    [SerializeField] private bool enableSlotEmptyChance = true;
    
    [Header("=== 搜索时间配置 ===")]
    [Tooltip("按稀有度配置搜索时间（秒）")]
    [SerializeField] private float[] searchDurationsByRarity = new float[] { 1.5f, 2.0f, 3.5f, 4.0f, 5.5f };
    
    [Tooltip("搜索时间随机波动范围（±百分比）")]
    [SerializeField] private float searchDurationVariance = 0.2f;
    
    [Header("=== 调试设置 ===")]
    [SerializeField] private bool verboseLogging = false;
    [SerializeField] private bool forceGenerateItems = false;
    [SerializeField] private int debugTestLevel = 3;
    [SerializeField] private ChestType debugTestType = ChestType.Wooden;
    
    // 缓存数据，提高性能
    private Dictionary<int, List<LootTableEntry>> cachedLootTablesByRarity = new Dictionary<int, List<LootTableEntry>>();
    private bool isCacheDirty = true;
    
    void Awake()
    {
        ValidateConfiguration();
        RebuildCacheIfNeeded();
    }
    
    void OnValidate()
    {
        isCacheDirty = true;
        ValidateConfiguration();
    }
    
    void ValidateConfiguration()
    {
        // 验证稀有度概率总和约为1
        float totalProbability = rarityProbabilities.Sum();
        if (Mathf.Abs(totalProbability - 1.0f) > 0.01f)
        {
            Debug.LogWarning($"稀有度概率总和为{totalProbability:F2}，建议调整为1.0");
        }
        
        // 验证数组长度
        if (rarityProbabilities.Length != 5)
        {
            Debug.LogWarning($"稀有度概率数组长度应为5，当前为{rarityProbabilities.Length}");
        }
        
        if (searchDurationsByRarity.Length != 5)
        {
            Debug.LogWarning($"搜索时间数组长度应为5，当前为{searchDurationsByRarity.Length}");
        }
        
        if (slotEmptyChanceByLevel.Length < 5)
        {
            Debug.LogWarning($"空置率数组长度不足，建议至少5个元素");
        }
    }
    
    public List<LootItemData> GenerateLoot(ChestData chestData)
    {
        if (verboseLogging)
        {
            Debug.Log($"=== 开始生成战利品 ===");
            Debug.Log($"宝箱等级: {chestData.chestLevel}, 类型: {chestData.chestType}");
        }
        
        List<LootItemData> loot = new List<LootItemData>();
        
        // 1. 检查宝箱整体是否为空
        float chestEmptyChance = CalculateEmptyChance(chestData);
        if (Random.Range(0f, 1f) < chestEmptyChance && !forceGenerateItems)
        {
            if (verboseLogging) Debug.Log($"宝箱为空! (空箱率: {chestEmptyChance:P0})");
            return loot;
        }
        
        // 2. 确定物品槽位数量（考虑宝箱配置）
        int maxSlots = Mathf.Min(chestData.maxLootSlots, 10); // 限制最大10个槽位
        if (verboseLogging) Debug.Log($"最大槽位数: {maxSlots}");
        
        // 3. 计算槽位空置决策
        bool[] slotDecisions = CalculateSlotDecisions(chestData, maxSlots);
        
        // 4. 为每个不空的槽位生成物品
        for (int i = 0; i < slotDecisions.Length; i++)
        {
            if (!slotDecisions[i]) // 槽位不空
            {
                LootItemData item = GenerateSingleItem(chestData);
                if (item != null)
                {
                    loot.Add(item);
                    
                    if (verboseLogging)
                    {
                        Debug.Log($"槽位 {i+1}: 生成 [{item.rarity}] {item.itemName} x{item.quantity} (搜索:{item.findAnimationDuration:F2}s)");
                    }
                }
                else
                {
                    Debug.LogWarning($"槽位 {i+1}: 物品生成失败");
                }
            }
            else if (verboseLogging)
            {
                Debug.Log($"槽位 {i+1}: 空置");
            }
        }
        
        // 5. 如果没有生成任何物品，强制生成一个（避免完全空的宝箱）
        if (loot.Count == 0 && !forceGenerateItems)
        {
            Debug.LogWarning($"所有槽位都为空，强制生成一个物品");
            LootItemData fallbackItem = GenerateSingleItem(chestData);
            if (fallbackItem != null)
            {
                loot.Add(fallbackItem);
            }
        }
        
        if (verboseLogging)
        {
            Debug.Log($"生成完成: {loot.Count}/{maxSlots} 个物品");
            Debug.Log($"======================");
        }
        
        return loot;
    }
    
    private bool[] CalculateSlotDecisions(ChestData chestData, int totalSlots)
    {
        bool[] slotDecisions = new bool[totalSlots];
        
        if (!enableSlotEmptyChance || totalSlots <= 0)
        {
            // 如果禁用空置率，所有槽位都有物品
            return slotDecisions;
        }
        
        // 计算每个槽位的空置率
        float slotEmptyChance = GetSlotEmptyChance(chestData);
        
        // 计算最少保证物品数
        int minItems = GetMinGuaranteedItems(chestData.chestLevel);
        minItems = Mathf.Min(minItems, totalSlots); // 不能超过总槽位数
        
        int filledSlots = 0;
        List<int> emptySlotIndices = new List<int>();
        
        // 第一轮：随机决定每个槽位是否为空
        for (int i = 0; i < totalSlots; i++)
        {
            bool isEmpty = Random.Range(0f, 1f) < slotEmptyChance;
            slotDecisions[i] = isEmpty;
            
            if (isEmpty)
            {
                emptySlotIndices.Add(i);
            }
            else
            {
                filledSlots++;
            }
        }
        
        // 第二轮：确保最少物品数
        if (filledSlots < minItems)
        {
            int neededFilled = minItems - filledSlots;
            
            // 随机选择一些空槽位改为填充
            if (emptySlotIndices.Count > 0)
            {
                // 打乱空槽位列表
                for (int i = 0; i < emptySlotIndices.Count; i++)
                {
                    int randomIndex = Random.Range(i, emptySlotIndices.Count);
                    int temp = emptySlotIndices[i];
                    emptySlotIndices[i] = emptySlotIndices[randomIndex];
                    emptySlotIndices[randomIndex] = temp;
                }
                
                // 填充前neededFilled个空槽位
                int slotsToFill = Mathf.Min(neededFilled, emptySlotIndices.Count);
                for (int i = 0; i < slotsToFill; i++)
                {
                    slotDecisions[emptySlotIndices[i]] = false;
                    filledSlots++;
                }
            }
        }
        
        // 第三轮：防止所有槽位都满（增加随机性）
        if (filledSlots == totalSlots && totalSlots > 1 && Random.Range(0f, 1f) < 0.3f)
        {
            // 随机将一个槽位设为空
            int randomIndex = Random.Range(0, totalSlots);
            slotDecisions[randomIndex] = true;
        }
        
        if (verboseLogging)
        {
            Debug.Log($"空置率: {slotEmptyChance:P0}, 最少物品: {minItems}, 实际填充: {filledSlots}/{totalSlots}");
        }
        
        return slotDecisions;
    }
    
    private LootItemData GenerateSingleItem(ChestData chestData)
    {
        // 1. 选择稀有度
        ItemRarity rarity = SelectRarity();
        
        // 2. 获取符合条件的物品列表（使用缓存）
        List<LootTableEntry> eligibleItems = GetEligibleItems(rarity, chestData);
        
        if (eligibleItems.Count == 0)
        {
            Debug.LogWarning($"没有找到符合条件的物品: 稀有度={rarity}, 等级={chestData.chestLevel}, 类型={chestData.chestType}");
            
            // 尝试放宽条件：忽略宝箱类型限制
            eligibleItems = GetEligibleItemsIgnoreType(rarity, chestData.chestLevel);
            if (eligibleItems.Count == 0) return null;
        }
        
        // 3. 加权随机选择
        LootTableEntry selectedEntry = WeightedRandomSelect(eligibleItems);
        
        // 4. 从预制体获取物品数据
        if (selectedEntry.itemPrefab == null)
        {
            Debug.LogError("选择的物品预制体为空!");
            return null;
        }
        
        Item itemComponent = selectedEntry.itemPrefab.GetComponent<Item>();
        if (itemComponent == null || itemComponent.Data == null)
        {
            Debug.LogError($"物品预制体 '{selectedEntry.itemPrefab.name}' 缺少 Item 组件或 ItemData");
            return null;
        }
        
        ItemData itemData = itemComponent.Data;
        
        // 5. 计算物品数量
        int quantity = GetRandomQuantity(rarity);
        
        // 6. 计算搜索时间（考虑随机波动）
        float searchDuration = GetSearchDurationWithVariance(rarity);
        
        // 7. 创建战利品数据
        return new LootItemData
        {
            itemId = itemData.itemId,
            itemName = itemData.itemName,
            icon = itemData.icon,
            quantity = quantity,
            rarity = rarity,
            findAnimationDuration = searchDuration,
            itemType = (int)itemData.category,
            value = itemData.value
        };
    }
    
    private List<LootTableEntry> GetEligibleItems(ItemRarity rarity, ChestData chestData)
    {
        RebuildCacheIfNeeded();
        
        int rarityIndex = (int)rarity;
        if (!cachedLootTablesByRarity.ContainsKey(rarityIndex))
        {
            return new List<LootTableEntry>();
        }
        
        // 过滤符合条件的物品
        return cachedLootTablesByRarity[rarityIndex].Where(entry =>
            chestData.chestLevel >= entry.minLevel &&
            chestData.chestLevel <= entry.maxLevel &&
            entry.allowedChestTypes.Contains(chestData.chestType)
        ).ToList();
    }
    
    private List<LootTableEntry> GetEligibleItemsIgnoreType(ItemRarity rarity, int chestLevel)
    {
        RebuildCacheIfNeeded();
        
        int rarityIndex = (int)rarity;
        if (!cachedLootTablesByRarity.ContainsKey(rarityIndex))
        {
            return new List<LootTableEntry>();
        }
        
        return cachedLootTablesByRarity[rarityIndex].Where(entry =>
            chestLevel >= entry.minLevel &&
            chestLevel <= entry.maxLevel
        ).ToList();
    }
    
    private void RebuildCacheIfNeeded()
    {
        if (!isCacheDirty && cachedLootTablesByRarity.Count > 0)
            return;
        
        cachedLootTablesByRarity.Clear();
        
        // 按稀有度分组缓存
        foreach (LootTableEntry entry in lootTable)
        {
            if (entry.itemPrefab == null) continue;
            
            int rarityIndex = (int)entry.rarity;
            if (!cachedLootTablesByRarity.ContainsKey(rarityIndex))
            {
                cachedLootTablesByRarity[rarityIndex] = new List<LootTableEntry>();
            }
            
            cachedLootTablesByRarity[rarityIndex].Add(entry);
        }
        
        isCacheDirty = false;
        
        if (verboseLogging)
        {
            Debug.Log("LootTable 缓存重建完成");
            foreach (var kvp in cachedLootTablesByRarity)
            {
                Debug.Log($"稀有度 {(ItemRarity)kvp.Key}: {kvp.Value.Count} 个物品");
            }
        }
    }
    
    private float CalculateEmptyChance(ChestData chestData)
    {
        float baseEmptyChance = 0.1f;
        float levelModifier = chestData.chestLevel * 0.01f;
        float typeModifier = (int)chestData.chestType * 0.02f;
        
        return Mathf.Clamp01(baseEmptyChance - levelModifier - typeModifier);
    }
    
    private float GetSlotEmptyChance(ChestData chestData)
    {
        if (!enableSlotEmptyChance) return 0f;
        
        // 根据等级获取基础空置率
        int levelIndex = Mathf.Clamp(chestData.chestLevel - 1, 0, slotEmptyChanceByLevel.Length - 1);
        float baseChance = slotEmptyChanceByLevel[levelIndex];
        
        // 宝箱类型影响
        int typeIndex = (int)chestData.chestType;
        if (typeIndex < chestTypeMultipliers.Length)
        {
            baseChance *= chestTypeMultipliers[typeIndex];
        }
        
        return Mathf.Clamp01(baseChance);
    }
    
    private int GetMinGuaranteedItems(int chestLevel)
    {
        int levelIndex = Mathf.Clamp(chestLevel - 1, 0, minGuaranteedItems.Length - 1);
        return minGuaranteedItems[levelIndex];
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
        if (entries.Count == 0) return null;
        if (entries.Count == 1) return entries[0];
        
        int totalWeight = entries.Sum(entry => entry.weight);
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
    
    private int GetRandomQuantity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Random.Range(1, 11);
            case ItemRarity.Uncommon: return Random.Range(1, 6);
            case ItemRarity.Rare: return Random.Range(1, 4);
            case ItemRarity.Epic: return Random.Range(1, 3);
            case ItemRarity.Legendary: return 1;
            default: return 1;
        }
    }
    
    private float GetSearchDurationWithVariance(ItemRarity rarity)
    {
        int rarityIndex = (int)rarity;
        if (rarityIndex < 0 || rarityIndex >= searchDurationsByRarity.Length)
        {
            return 1.5f;
        }
        
        float baseDuration = searchDurationsByRarity[rarityIndex];
        
        // 添加随机波动
        float variance = Random.Range(-searchDurationVariance, searchDurationVariance);
        return baseDuration * (1 + variance);
    }
    
    // ==================== 调试方法 ====================
    
    [ContextMenu("测试生成")]
    public void TestGeneration()
    {
        ChestData testData = new ChestData
        {
            chestLevel = debugTestLevel,
            chestType = debugTestType,
            maxLootSlots = 5
        };
        
        var loot = GenerateLoot(testData);
        Debug.Log($"测试生成: 等级{debugTestLevel}, 类型{debugTestType}");
        Debug.Log($"生成 {loot.Count} 个物品:");
        
        foreach (var item in loot)
        {
            Debug.Log($"  - {item.itemName} x{item.quantity} [{item.rarity}] ({item.findAnimationDuration:F2}s)");
        }
    }
    
    [ContextMenu("打印配置信息")]
    public void PrintConfiguration()
    {
        Debug.Log("=== LootGenerator 配置信息 ===");
        Debug.Log($"物品表条目数: {lootTable.Count}");
        Debug.Log($"稀有度概率: {string.Join(", ", rarityProbabilities.Select(p => p.ToString("F2")))}");
        Debug.Log($"槽位空置率: {string.Join(", ", slotEmptyChanceByLevel.Select(p => p.ToString("F2")))}");
        Debug.Log($"搜索时间: {string.Join(", ", searchDurationsByRarity.Select(d => $"{d:F1}s"))}");
    }
    
    [ContextMenu("验证物品表")]
    public void ValidateLootTable()
    {
        int validCount = 0;
        int invalidCount = 0;
        
        foreach (var entry in lootTable)
        {
            if (entry.itemPrefab == null)
            {
                Debug.LogError($"物品表条目无效: 预制体为空");
                invalidCount++;
                continue;
            }
            
            var item = entry.itemPrefab.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogError($"物品 {entry.itemPrefab.name}: 缺少 Item 组件");
                invalidCount++;
                continue;
            }
            
            if (item.Data == null)
            {
                Debug.LogError($"物品 {entry.itemPrefab.name}: ItemData 为空");
                invalidCount++;
                continue;
            }
            
            validCount++;
        }
        
        Debug.Log($"物品表验证: {validCount} 个有效, {invalidCount} 个无效");
    }
}