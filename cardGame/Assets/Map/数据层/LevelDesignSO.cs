// MapLayoutSO.cs
using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    public enum MapGenerationType
    {
        Manual,         // 手动摆放
        ProceduralTree  // 程序化树状生成
    }

    [CreateAssetMenu(fileName = "NewMapLayout", menuName = "SlayTheSpire/Map Layout")]
    public class MapLayoutSO : ScriptableObject
    {
        [Header("生成模式")]
        public MapGenerationType generationType = MapGenerationType.Manual;
        
        [Header("程序化生成配置 (仅ProceduralTree模式)")]
        public int treeLayers = 10; // 层数（包括起点和Boss）
        public Vector2Int nodesPerLayer = new Vector2Int(3, 5); // 每层节点数范围
        public float layerDistanceY = 200f; // 层间距
        public float nodeDistanceX = 150f; // 同层节点间距
        public float xRandomOffset = 30f; // X轴随机偏移
        public float yRandomOffset = 30f; // Y轴随机偏移
        public int startNodeCount = 3; // 起始节点数量
        public bool generateBoss = true; // 是否生成Boss节点
        
        [Header("节点类型概率 (权重)")]
        public int combatWeight = 40;
        public int eliteWeight = 15;
        public int shopWeight = 5;
        public int restWeight = 10;
        public int eventWeight = 20;
        public int digWeight = 10;       // 挖掘场景权重

        [Header("关卡内容池 (通用配置)")]
        public List<EncounterData> easyEnemyPool = new List<EncounterData>();   // 简单敌人
        public List<EncounterData> normalEnemyPool = new List<EncounterData>(); // 普通敌人
        public List<EncounterData> hardEnemyPool = new List<EncounterData>();   // 困难敌人 (新增)
        public List<EncounterData> eliteEnemyPool = new List<EncounterData>();  // 精英敌人
        public List<EncounterData> bossEnemyPool = new List<EncounterData>();   // Boss
        public List<EncounterData> eventPool = new List<EncounterData>();       // 事件
        public List<EncounterData> shopPool = new List<EncounterData>();        // 商店(如有特殊)
        
        [Header("挖掘场景配置池")]
        public List<DigData> digPool = new List<DigData>();                     // 挖掘场景(新增)

        [Header("布局基本信息")]
        public string layoutName;
        public string sceneToLoad; // 关联的场景名称
        
        [Header("节点位置（手动摆放）")]
        public List<ManualNodePosition> nodePositions = new List<ManualNodePosition>();
        
        [Header("连线规则")]
        public bool autoConnectByDistance = true; // 根据距离自动连线
        public float maxConnectionDistance = 300f; // 最大连线距离
        public int minConnections = 1; // 每个节点最少连接数
        public int maxConnections = 3; // 每个节点最多连接数
        
        [Header("节点类型配置")]
        public bool useFixedTypes = false; // 是否使用固定节点类型
        public List<NodeType> fixedNodeTypes = new List<NodeType>(); // 固定类型列表
        
        [System.Serializable]
        public class ManualNodePosition
        {
            public Vector2 position;
            public NodeType nodeType = NodeType.Combat; // 手动指定的类型
            public bool isFixedType = false; // 是否固定此类型
            
            [Header("预设遭遇战（可选）")]
            public EncounterData presetEncounter;
            
            [Header("特殊标记")]
            public bool isBoss = false;
            public bool isElite = false;
            public bool isStartNode = false;
        }
        
        /// <summary>
        /// 获取指定索引的节点类型
        /// </summary>
        public NodeType GetNodeType(int index)
        {
            if (index >= 0 && index < nodePositions.Count)
            {
                if (nodePositions[index].isFixedType)
                {
                    return nodePositions[index].nodeType;
                }
            }
            
            // 如果使用固定类型列表，从列表中取
            if (useFixedTypes && fixedNodeTypes.Count > 0)
            {
                return fixedNodeTypes[index % fixedNodeTypes.Count];
            }
            
            // 默认返回战斗类型
            return NodeType.Combat;
        }
        
        /// <summary>
        /// 是否是起始节点
        /// </summary>
        public bool IsStartNode(int index)
        {
            if (index >= 0 && index < nodePositions.Count)
            {
                return nodePositions[index].isStartNode;
            }
            return index == 0; // 默认第一个是起始节点
        }
        
        /// <summary>
        /// 是否是Boss节点
        /// </summary>
        public bool IsBossNode(int index)
        {
            if (index >= 0 && index < nodePositions.Count)
            {
                return nodePositions[index].isBoss;
            }
            return index == nodePositions.Count - 1; // 默认最后一个是Boss
        }
        
        /// <summary>
        /// 获取节点名称
        /// </summary>
        public string GetNodeName(int index)
        {
            if (index >= 0 && index < nodePositions.Count)
            {
                if (nodePositions[index].isBoss) return "最终Boss";
                if (nodePositions[index].isElite) return "精英战";
                
                // 根据节点类型返回不同名称
                switch (nodePositions[index].nodeType)
                {
                    case NodeType.Dig:
                        return "挖掘场";
                    case NodeType.Combat:
                        return "战斗";
                    case NodeType.Event:
                        return "事件";
                    case NodeType.Shop:
                        return "商店";
                    case NodeType.Rest:
                        return "休息点";
                }
            }
            
            return $"节点 {index + 1}";
        }
    }
}