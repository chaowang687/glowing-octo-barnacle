using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class MapGenerator : MonoBehaviour
    {
        public MapNodeData[] GenerateMap(MapLayoutSO layout)
        {
            List<MapNodeData> nodesResult = new List<MapNodeData>();
            
            // 1. 创建所有节点
            for (int i = 0; i < layout.nodePositions.Count; i++)
            {
                MapNodeData newNode = CreateNode(i, layout.nodePositions[i], layout);
                nodesResult.Add(newNode);
            }
            
            // 2. 关键：连接节点
            LinkDataNodes(nodesResult, layout);
            
            // 3. 解锁起始节点
            foreach (var node in nodesResult)
            {
                if (node.isStartNode)
                {
                    node.isUnlocked = true;
                    Debug.Log($"[MapGenerator] 已解锁起始节点: {node.nodeId}");
                }
            }
            
            return nodesResult.ToArray();
        }

        
       // MapGenerator.cs 优化
        private MapNodeData CreateNode(int index, MapLayoutSO.ManualNodePosition nodePos, MapLayoutSO layout)
        {
            MapNodeData node = new MapNodeData();
            node.nodeId = $"node_{index}";
            node.position = nodePos.position;
            node.nodeName = GetNodeName(index, nodePos, layout); // 添加节点名称
            
            // 关键：设置节点类型
            node.nodeType = GetNodeType(index, nodePos, layout);
            
            // 设置精英/Boss标记
            node.isElite = nodePos.isElite;
            node.isBoss = nodePos.isBoss;
            node.isStartNode = nodePos.isStartNode;
            
            // 设置战斗数据
            node.encounterData = nodePos.presetEncounter ?? CreateEncounterData(node);
            
            return node;
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
        
        public MapNodeData FindStartNode(MapNodeData[] nodes)
        {
            foreach (var node in nodes)
            {
                if (node.isStartNode)
                {
                    return node;
                }
            }
            
            // 如果没有标记起始节点，返回第一个
            return nodes.Length > 0 ? nodes[0] : null;
        }
        
        public void UnlockConnectedNodes(MapNodeData node)
        {
            foreach (var connected in node.connectedNodes)
            {
                if (!connected.isUnlocked)
                {
                    connected.isUnlocked = true;
                    Debug.Log($"解锁节点: {connected.nodeName}");
                }
            }
        }
        
        // 辅助方法
        private string GetNodeName(int index, MapLayoutSO.ManualNodePosition nodePos, MapLayoutSO layout)
        {
            if (nodePos.isBoss) return "最终Boss";
            if (nodePos.isElite) return "精英战";
            if (nodePos.isStartNode) return "起始点";
            return layout.GetNodeName(index) ?? $"节点 {index + 1}";
        }
        
        private NodeType GetNodeType(int index, MapLayoutSO.ManualNodePosition nodePos, MapLayoutSO layout)
        {
            // 1. 如果有固定类型，使用固定类型
            if (nodePos.isFixedType) return nodePos.nodeType;
            
            // 2. 如果是Boss或精英，返回对应类型
            if (nodePos.isBoss) return NodeType.Boss;
            if (nodePos.isElite) return NodeType.Elite;
            if (nodePos.isStartNode) return NodeType.Combat; // 起始点通常是战斗
            
            // 3. 如果没有预设，使用layout中的类型分布
            return layout.GetNodeType(index);
        }
        
        private EncounterData CreateEncounterData(MapNodeData node)
        {
            EncounterData data = ScriptableObject.CreateInstance<EncounterData>();
            data.nodeType = node.nodeType;
            data.isElite = node.isElite;
            data.isBoss = node.isBoss;
            
            // 设置基础奖励
            if (node.isBoss) data.goldReward = 100;
            else if (node.isElite) data.goldReward = 25;
            else data.goldReward = 10;
            
            if (node.nodeType == NodeType.Rest)
            {
                data.healthReward = 30; // 默认回复30%
            }
            
            return data;
        }
    }
}