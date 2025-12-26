// MapNodeData.cs - 修复版本
using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    [System.Serializable]
    public class ShopInventory
    {
        public List<string> cardIds = new List<string>();
        public List<string> relicIds = new List<string>();
        public float priceMultiplier = 1.0f;
    }
    [CreateAssetMenu(fileName = "NewMapNode", menuName = "SlayTheSpire/Map Node")]
    public class MapNodeDataSO : ScriptableObject
    {
        [Header("商店配置")]
        public ShopInventory shopInventory; // 现在不会报错了
        [Header("节点基本信息")]
        public string nodeId;
        public string nodeName;
        public NodeType nodeType;
        public Vector2 position;
        
        [Header("敌人配置（战斗节点）")]
        public bool useRandomEnemy = true;
        [Tooltip("如果useRandomEnemy为false，则使用固定敌人配置")]
        public EncounterData fixedEncounterData;
        
        [Header("奖励配置")]
        public int baseGoldReward = 10;
        public int eliteGoldReward = 25;
        public int bossGoldReward = 100;
        
      
        
        [Header("事件配置")]
        public EventData eventData;
    }
}