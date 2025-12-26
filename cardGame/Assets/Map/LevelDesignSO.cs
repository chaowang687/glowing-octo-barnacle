// MapLayoutSO.cs
using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    [CreateAssetMenu(fileName = "NewMapLayout", menuName = "SlayTheSpire/Map Layout")]
    public class MapLayoutSO : ScriptableObject
    {
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
            }
            
            return $"节点 {index + 1}";
        }
    }
}