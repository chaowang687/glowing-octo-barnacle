// ScavengingController.cs (修改后)
using UnityEngine;
using System.Collections.Generic;

namespace ScavengingGame
{
    public class ScavengingController : MonoBehaviour
    {
        [Header("奖励配置")]
        public List<ItemData> ItemDatabase = new List<ItemData>();
        public int MaxScavengeRewards = 3;

        [Header("事件配置")]
        [Range(0.0f, 1.0f)]
        public float globalEventChance = 0.1f;
        [Range(0.0f, 1.0f)]
        public float globalEncounterChance = 0.3f; // 保留，可用于普通节点遭遇

        void Awake()
        {
            // 初始化所有静态管理器
            RewardManager.ItemDatabase = ItemDatabase;
            RewardManager.MaxRewardsPerDrop = MaxScavengeRewards;

            RandomEventManager.GlobalEventChance = globalEventChance;

            Debug.Log("[ScavengingController] 所有探索管理器已初始化。");
        }

        // 保留旧方法签名用于兼容性，但实际逻辑已迁移。可逐渐弃用。
        public void HandleScavengeAction() { /* 留空或显示警告 */ }
        public void HandleMoveAction() { /* 留空或显示警告 */ }
    }
}