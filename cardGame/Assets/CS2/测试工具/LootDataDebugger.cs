// 创建调试脚本检查数据

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class LootDataDebugger : MonoBehaviour
{
    void Start()
    {
        // 测试数据生成
        TestLootGeneration();
    }
    
    void TestLootGeneration()
    {
        ChestData testChest = new ChestData
        {
            chestLevel = 3,
            chestType = ChestType.Wooden,
            maxLootSlots = 5
        };
        
        LootGenerator generator = FindFirstObjectByType<LootGenerator>();
        if (generator == null)
        {
            Debug.LogError("未找到 LootGenerator");
            return;
        }
        
        List<LootItemData> lootItems = generator.GenerateLoot(testChest);
        
        Debug.Log($"=== 战利品生成测试 ===");
        Debug.Log($"生成数量: {lootItems.Count}");
        
        for (int i = 0; i < lootItems.Count; i++)
        {
            LootItemData item = lootItems[i];
            Debug.Log($"物品 {i+1}:");
            Debug.Log($"  - ID: {item.itemId}");
            Debug.Log($"  - 名称: {item.itemName}");
            Debug.Log($"  - 图标: {(item.icon != null ? item.icon.name : "NULL")}");
            Debug.Log($"  - 数量: {item.quantity}");
            Debug.Log($"  - 稀有度: {item.rarity}");
            
            // 检查icon是否为null
            if (item.icon == null)
            {
                Debug.LogError($"  ❌ 物品 {item.itemName} 的图标为空！");
            }
        }
        
        if (lootItems.Count == 0)
        {
            Debug.LogWarning("⚠️ 生成的战利品列表为空！可能是空箱概率过高。");
        }
    }
}