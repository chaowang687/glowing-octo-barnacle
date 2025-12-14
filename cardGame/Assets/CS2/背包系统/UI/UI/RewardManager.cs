using UnityEngine;
using System.Collections.Generic;

namespace ScavengingGame
{
    public static class RewardManager
    {
        // 添加缺失的静态字段
        public static List<ItemData> ItemDatabase = new List<ItemData>();
        public static int MaxRewardsPerDrop = 3; // 默认每次掉落最多3个奖励
        
        // 生成随机奖励
        public static List<ItemData> GenerateRandomRewards()
        {
            List<ItemData> rewards = new List<ItemData>();
            
            // 如果ItemDatabase为空，直接返回空列表
            if (ItemDatabase == null || ItemDatabase.Count == 0)
            {
                Debug.LogWarning("RewardManager: ItemDatabase为空，无法生成奖励");
                return rewards;
            }
            
            // 随机决定奖励数量 (1到MaxRewardsPerDrop之间)
            int rewardCount = Random.Range(1, MaxRewardsPerDrop + 1);
            
            for (int i = 0; i < rewardCount; i++)
            {
                // 从ItemDatabase中随机选择一个物品
                int randomIndex = Random.Range(0, ItemDatabase.Count);
                ItemData randomItem = ItemDatabase[randomIndex];
                
                if (randomItem != null)
                {
                    rewards.Add(randomItem);
                }
            }
            
            Debug.Log($"生成了{rewards.Count}个随机奖励");
            return rewards;
        }
        
        // 接受IInventoryService接口的方法
        public static void GrantRewardsToPlayer(List<ItemData> rewards, IInventoryService inventory)
        {
            if (inventory == null)
            {
                Debug.LogError("GrantRewardsToPlayer: 库存服务为空");
                return;
            }
            
            if (rewards == null || rewards.Count == 0)
            {
                Debug.Log("没有奖励可发放");
                return;
            }
            
            int successCount = 0;
            foreach (var reward in rewards)
            {
                if (reward != null)
                {
                    bool added = inventory.AddItem(reward, 1);
                    if (added)
                    {
                        successCount++;
                        Debug.Log($"发放奖励: {reward.ItemName}");
                    }
                    else
                    {
                        Debug.LogWarning($"无法添加奖励: {reward.ItemName}");
                    }
                }
            }
            
            Debug.Log($"成功发放了 {successCount}/{rewards.Count} 个奖励");
        }
        
        // 重载版本：接受InventoryManager类型（向后兼容）
        public static void GrantRewardsToPlayer(List<ItemData> rewards, InventoryManager inventoryManager)
        {
            // 调用接口版本
            GrantRewardsToPlayer(rewards, inventoryManager as IInventoryService);
        }
        
        // 初始化ItemDatabase的方法（可以在游戏启动时调用）
        public static void InitializeItemDatabase(List<ItemData> availableItems)
        {
            if (availableItems == null)
            {
                Debug.LogError("InitializeItemDatabase: 可用物品列表为空");
                return;
            }
            
            ItemDatabase = new List<ItemData>(availableItems);
            Debug.Log($"RewardManager初始化完成，物品数据库包含{ItemDatabase.Count}个物品");
        }
        
        // 添加单个物品到数据库
        public static void AddItemToDatabase(ItemData item)
        {
            if (item == null) return;
            
            if (ItemDatabase == null)
                ItemDatabase = new List<ItemData>();
            
            if (!ItemDatabase.Contains(item))
            {
                ItemDatabase.Add(item);
                Debug.Log($"添加物品到奖励数据库: {item.ItemName}");
            }
        }
    }
}