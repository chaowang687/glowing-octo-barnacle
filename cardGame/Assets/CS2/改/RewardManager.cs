// RewardManager.cs
using UnityEngine;
using System.Collections.Generic;

namespace ScavengingGame
{
    public static class RewardManager
    {
        // 奖励配置（由ScavengingController初始化时注入）
        public static List<ItemData> ItemDatabase { get; set; }
        public static int MaxRewardsPerDrop { get; set; } = 3;

        /// <summary>
        /// 根据配置生成随机奖励列表
        /// </summary>
        public static List<ItemData> GenerateRandomRewards()
        {
            List<ItemData> rewards = new List<ItemData>();
            if (ItemDatabase == null || ItemDatabase.Count == 0)
            {
                Debug.LogWarning("[RewardManager] 物品数据库未初始化或为空。");
                return rewards;
            }

            int numRewards = Random.Range(1, MaxRewardsPerDrop + 1);
            for (int i = 0; i < numRewards; i++)
            {
                int index = Random.Range(0, ItemDatabase.Count);
                rewards.Add(ItemDatabase[index]);
            }
            Debug.Log($"[RewardManager] 生成了 {rewards.Count} 项奖励。");
            return rewards;
        }

        /// <summary>
        /// 将奖励列表添加到玩家库存
        /// </summary>
        public static void GrantRewardsToPlayer(List<ItemData> rewards, InventoryManager inventory)
        {
            if (inventory == null)
            {
                Debug.LogError("[RewardManager] 库存管理器为空，无法发放奖励。");
                return;
            }

            foreach (var item in rewards)
            {
                inventory.AddItem(item);
            }
        }
    }
}