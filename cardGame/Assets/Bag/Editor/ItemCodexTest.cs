using UnityEditor;
using UnityEngine;
using System;

namespace Bag.Editor
{
    public class ItemCodexTest
    {
        [MenuItem("Bag/Test Item Codex Integration")]
        public static void TestItemCodexIntegration()
        {
            Debug.Log("=== 开始测试物品图鉴集成 ===");
            
            // 1. 检查ItemCodexManager是否存在
            if (ItemCodexManager.Instance == null)
            {
                Debug.LogError("ItemCodexManager.Instance 为 null！请确保场景中有ItemCodexManager游戏对象。");
                return;
            }
            
            // 2. 检查图鉴数据是否正确设置
            if (ItemCodexManager.Instance.codexData == null)
            {
                Debug.LogError("ItemCodexManager.Instance.codexData 为 null！请在Inspector中设置图鉴数据。");
                return;
            }
            
            // 3. 检查背包管理器是否存在
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("InventoryManager.Instance 为 null！请确保场景中有InventoryManager游戏对象。");
                return;
            }
            
            // 4. 测试收集物品
            Debug.Log("测试收集物品功能...");
            
            // 5. 检查物品ID匹配
            Debug.Log("检查物品ID匹配...");
            int totalItems = ItemCodexManager.Instance.codexData.allItems.Count;
            int matchedItems = 0;
            
            foreach (var codexItem in ItemCodexManager.Instance.codexData.allItems)
            {
                // 模拟从背包获取物品（这里只是检查ID格式）
                Debug.Log($"图鉴物品: {codexItem.itemName} (ID: {codexItem.itemID})");
                
                // 检查是否已在收集列表中
                if (ItemCodexManager.Instance.IsCollected(codexItem.itemID))
                {
                    matchedItems++;
                }
            }
            
            Debug.Log($"物品ID检查完成：总共 {totalItems} 个物品，已收集 {matchedItems} 个");
            
            // 6. 检查收集完成度
            float completion = ItemCodexManager.Instance.GetCompletionPercentage();
            Debug.Log($"图鉴完成度: {completion:F1}%");
            
            Debug.Log("=== 测试完成 ===");
        }
    }
}
