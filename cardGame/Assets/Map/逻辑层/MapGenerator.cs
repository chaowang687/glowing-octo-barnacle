using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("生成设置")]
        public GameObject nodePrefab;       
        public Transform parentContainer;  
        
        public MapNodeData[] GenerateMap(MapLayoutSO layout)
        {
            if (layout == null)
            {
                Debug.LogError("MapLayoutSO 为空！");
                return new MapNodeData[0];
            }
            
            List<MapNodeData> nodesResult = new List<MapNodeData>();
            
            // 1. 创建所有节点（纯数据）
            for (int i = 0; i < layout.nodePositions.Count; i++)
            {
                var nodePos = layout.nodePositions[i];

                // 实例化纯数据对象
                MapNodeData node = new MapNodeData();
                
                // --- 同步基础属性 ---
                node.nodeId = $"node_{i}";
                node.encounterData = nodePos.presetEncounter;
                node.nodeName = GetNodeName(i, nodePos);
                node.nodeType = nodePos.nodeType;
                node.isElite = nodePos.isElite;
                node.isBoss = nodePos.isBoss;
                node.isStartNode = nodePos.isStartNode;
                node.position = nodePos.position; 

                // --- 确保每一关的敌人配置被正确塞入实例 ---
                if (nodePos.presetEncounter != null)
                {
                    node.encounterData = nodePos.presetEncounter;
                    Debug.Log($"[Map] 节点 {node.nodeId} 已加载配置: {node.encounterData.name}");
                }
                else
                {
                    // 如果没配，则自动生成（保底逻辑）
                    node.encounterData = CreateSimpleEncounter(node);
                }
                
                nodesResult.Add(node);
            }
            
            // 2. 连接节点
            LinkDataNodes(nodesResult, layout);
            
            // 3. 解锁逻辑
            foreach (var node in nodesResult)
            {
                if (node.isStartNode)
                {
                    node.isUnlocked = true;
                    // 同步到全局管理器
                    if (GameDataManager.Instance != null)
                    {
                        if (!GameDataManager.Instance.unlockedNodeIds.Contains(node.nodeId))
                            GameDataManager.Instance.unlockedNodeIds.Add(node.nodeId);
                    }
                }
            }
            
            LogNodeTypes(nodesResult);
            return nodesResult.ToArray();
        }

        private EncounterData CreateSimpleEncounter(MapNodeData node)
        {
            // 创建 EnemyEncounterData 实例
            EnemyEncounterData data = ScriptableObject.CreateInstance<EnemyEncounterData>();
            data.nodeType = node.nodeType;
            data.enemyList = new List<EnemyData>(); // 即使是空的也要初始化，防止报错
            return data;
        }
        private string GetNodeName(int index, MapLayoutSO.ManualNodePosition nodePos)
        {
            if (nodePos.isBoss) return "Boss";
            if (nodePos.isElite) return "精英怪";
            if (nodePos.isStartNode) return "起点";
            
            switch (nodePos.nodeType)
            {
                case NodeType.Combat: return $"战斗 {index}";
                case NodeType.Elite: return $"精英 {index}";
                case NodeType.Shop: return $"商店 {index}";
                case NodeType.Rest: return $"休息点 {index}";
                case NodeType.Event: return $"事件 {index}";
                case NodeType.Boss: return "最终Boss";
                default: return $"节点 {index}";
            }
        }
        private void LinkDataNodes(List<MapNodeData> nodes, MapLayoutSO layout)
        {
            // 简单策略：按X坐标排序，每个节点连接右侧的几个节点
            nodes.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            
            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeData current = nodes[i];
                
                // 找到右侧的节点
                List<MapNodeData> rightNodes = new List<MapNodeData>();
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (layout.autoConnectByDistance)
                    {
                        float distance = Vector2.Distance(current.position, nodes[j].position);
                        if (distance <= layout.maxConnectionDistance)
                        {
                            rightNodes.Add(nodes[j]);
                        }
                    }
                    else
                    {
                        rightNodes.Add(nodes[j]);
                    }
                }
                
                // 确定连接数量
                int minConn = Mathf.Max(1, layout.minConnections);
                int maxConn = Mathf.Min(layout.maxConnections, rightNodes.Count);
                int targetConnections = Random.Range(minConn, maxConn + 1);
                
                // 随机选择连接
                for (int j = 0; j < targetConnections && rightNodes.Count > 0; j++)
                {
                    int randomIndex = Random.Range(0, rightNodes.Count);
                    current.AddConnectedNode(rightNodes[randomIndex]);
                    rightNodes.RemoveAt(randomIndex);
                }
                
                // 确保至少连接一个
                if (current.connectedNodes.Count == 0 && i < nodes.Count - 1)
                {
                    current.AddConnectedNode(nodes[i + 1]);
                }
            }
        }
        
        private void LogNodeTypes(List<MapNodeData> nodes)
        {
            Debug.Log($"=== 地图生成完成，共 {nodes.Count} 个节点 ===");
            foreach (var node in nodes)
            {
                Debug.Log($"- {node.nodeName}: {node.nodeType} {(node.isElite ? "[精英]" : "")}{(node.isBoss ? "[Boss]" : "")}");
            }
        }
        
        public MapNodeData FindStartNode(MapNodeData[] nodes)
        {
            foreach (var node in nodes)
            {
                if (node.isStartNode) return node;
            }
            return nodes.Length > 0 ? nodes[0] : null;
        }
    }
}
