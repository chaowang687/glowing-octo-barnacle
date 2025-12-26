using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    /// <summary>
    /// 运行时地图节点的数据表示。
    /// 区别于 ScriptableObject，这个类实例存在于内存或存档中，记录实时状态。
    /// </summary>
    [System.Serializable]
    public class MapNodeData
    {
        [Header("基础身份信息")]
        public string nodeId;           // 唯一的ID，用于读档定位
        public string nodeName;         // 节点显示名称
        public NodeType nodeType;       // 节点类型（战斗、商店、事件等）
        public Vector2 position;        // 在UI地图上的坐标位置

        [Header("连接关系")]
        // 该节点指向的下游节点列表
        [System.NonSerialized] 
        public List<MapNodeData> connectedNodes = new List<MapNodeData>();

        [Header("实时状态")]
        public bool isUnlocked = false;   // 是否已解锁（玩家可以点击进入）
        public bool isCompleted = false;  // 是否已完成（玩家已经挑战过）
        public bool isStartNode = false;  // 是否是第一层的起始节点
        public bool isBoss = false;       // 是否是最终Boss节点
        public bool isElite = false;      // 是否是精英节点

        [Header("具体内容引用")]
        // 该节点关联的战斗/遭遇数据
        public EncounterData encounterData;
        // 如果是事件节点，关联的事件数据
        public EventData eventData;

        /// <summary>
        /// 构造函数或初始化方法，用于从配置（MapNodeDataSO）生成运行时数据
        /// </summary>
        public MapNodeData() { }

       
        public void AddConnectedNode(MapNodeData targetNode)
        {
            if (targetNode != null && !connectedNodes.Contains(targetNode))
            {
                connectedNodes.Add(targetNode);
            }
        }
        
        
    }
}