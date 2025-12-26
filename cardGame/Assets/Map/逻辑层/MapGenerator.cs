using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class MapGenerator : MonoBehaviour
    {
        public MapNodeData[] GenerateMap(MapLayoutSO layout)
        {
            if (layout == null || layout.nodePositions.Count == 0)
            {
                Debug.LogError("地图布局无效！");
                return new MapNodeData[0];
            }
            
            List<MapNodeData> nodes = new List<MapNodeData>();
            
            // 1. 创建节点
            for (int i = 0; i < layout.nodePositions.Count; i++)
            {
                var nodePos = layout.nodePositions[i];
                MapNodeData node = CreateNode(i, nodePos, layout);
                nodes.Add(node);
            }
            
            // 2. 创建连接
            LinkDataNodes(nodes, layout);
            
            return nodes.ToArray();
        }
        
       // MapGenerator.cs 优化
        private MapNodeData CreateNode(int index, MapLayoutSO.ManualNodePosition nodePos, MapLayoutSO layout)
        {
            MapNodeData node = new MapNodeData();
            node.nodeId = $"node_{index}"; // 稳定的ID用于存档
            node.position = nodePos.position;
            // 优先使用预设的战斗数据，如果没有，再动态创建一个基础的
            node.encounterData = nodePos.presetEncounter != null ? 
                                nodePos.presetEncounter : 
                                CreateEncounterData(node);
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
            if (nodePos.isFixedType) return nodePos.nodeType;
            if (nodePos.isBoss) return NodeType.Boss;
            if (nodePos.isElite) return NodeType.Elite;
            return layout.GetNodeType(index);
        }
        
        private EncounterData CreateEncounterData(MapNodeData node)
        {
            EncounterData data = new EncounterData();
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