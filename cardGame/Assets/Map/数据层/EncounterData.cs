using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    /// <summary>
    /// 遭遇战数据：定义战斗内容和奖励
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "SlayTheSpire/Encounter Data")]
    public class EncounterData : ScriptableObject
    {
        [Header("基础信息")]
        public string encounterName; // 遭遇名称，如 "三只野狼"
        public NodeType nodeType;    // 对应的节点类型（Combat, Elite, Boss）

        [Header("敌人配置")]
        public List<EnemyConfig> enemyConfigs = new List<EnemyConfig>();

        [Header("奖励信息")]
        public int minGold = 10;
        public int maxGold = 20;
        
        [Tooltip("休息点回复生命值的百分比")]
        public int healthRewardPercent = 30; 
        
        public List<string> cardPoolIds; // 该战斗可能掉落的卡牌池 ID
        public string relicId;           // 固定掉落的遗物 ID（通常用于精英/波斯）
        public bool isElite; 
        public bool isBoss;
        public int goldReward;   // 或者在 MapGenerator 里用 (minGold + maxGold)/2
        public int healthReward; // 对应错误中的 healthReward
        /// <summary>
        /// 运行时生成随机金币奖励
        /// </summary>
        public int GetRandomGold() => Random.Range(minGold, maxGold + 1);
    }

    [System.Serializable]
    public class EnemyConfig
    {
        public string enemyName;
        public GameObject enemyPrefab; // 敌人预制体
        public int health = 50;
        public int damage = 10;
        // 可以进一步引用更详细的 EnemyDataSO
    }
}