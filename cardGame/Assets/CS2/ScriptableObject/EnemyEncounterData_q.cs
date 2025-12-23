using UnityEngine;
using System.Collections.Generic;

namespace ScavengingGame
{
    /// <summary>
    /// 敌人遭遇数据，用于在场景间传递战斗信息
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyEncounter", menuName = "Scavenge/Enemy Encounter")]
    public class EnemyEncounterData_q : ScriptableObject
    {
        [Header("敌人配置")]
        public string encounterName = "普通遭遇";
        public List<EnemyUnitData> enemyUnits = new List<EnemyUnitData>();
        
        [Header("战斗奖励")]
        public List<ItemData> guaranteedRewards = new List<ItemData>();
        [Range(0, 5)] public int randomRewardCount = 2;
        public List<ItemData> randomRewardPool = new List<ItemData>();
        
        [Header("战斗设置")]
        public int difficultyLevel = 1;
        public bool isBossEncounter = false;
        
        /// <summary>
        /// 生成战斗奖励
        /// </summary>
        public List<ItemData> GenerateRewards()
        {
            List<ItemData> rewards = new List<ItemData>();
            
            // 添加固定奖励
            rewards.AddRange(guaranteedRewards);
            
            // 添加随机奖励
            for (int i = 0; i < randomRewardCount && randomRewardPool.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, randomRewardPool.Count);
                rewards.Add(randomRewardPool[randomIndex]);
            }
            
            return rewards;
        }
    }

    /// <summary>
    /// 敌人单位数据（示例结构）
    /// </summary>
    [System.Serializable]
    public class EnemyUnitData
    {
        public string enemyName;
        public int maxHealth;
        public int attackPower;
        public Sprite enemySprite;
        // 可以添加更多属性：防御、技能等
    }
}