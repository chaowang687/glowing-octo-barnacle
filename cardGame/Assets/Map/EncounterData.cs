// EncounterData.cs
using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    [System.Serializable]
    public class EncounterData
    {
        [Header("基础信息")]
        public NodeType nodeType;
        public int currentLayer;
        public int totalLayers;
        
        [Header("战斗信息")]
        public bool isElite;
        public bool isBoss;
        public int encounterIndex;
        
        [Header("奖励信息")]
        public int goldReward;
        public int healthReward; // 休息点回复量
        public List<string> cardRewards;
        public string relicReward;
        
        [Header("玩家状态")]
        public int playerHealth;
        public int playerMaxHealth;
        public int playerGold;
        
        public EncounterData()
        {
            nodeType = NodeType.Combat;
            currentLayer = 1;
            totalLayers = 16;
            isElite = false;
            isBoss = false;
            encounterIndex = 0;
            goldReward = 0;
            healthReward = 0;
            cardRewards = new List<string>();
            playerHealth = 0;
            playerMaxHealth = 0;
            playerGold = 0;
        }
    }
}