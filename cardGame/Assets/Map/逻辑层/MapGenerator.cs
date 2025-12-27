

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class MapGenerator : MonoBehaviour
    {
        
        public MapNodeData[] GenerateMap(MapLayoutSO layout)
        {
            if (layout == null)
            {
                Debug.LogError("MapLayoutSO 为空！");
                return new MapNodeData[0];
            }
            
            List<MapNodeData> nodesResult = new List<MapNodeData>();
            
            // 1. 创建所有节点
            for (int i = 0; i < layout.nodePositions.Count; i++)
            {
                var nodePos = layout.nodePositions[i];
                MapNodeData node = new MapNodeData();
                
                // 基础信息
                node.nodeId = $"node_{i}";
                node.position = nodePos.position;
                node.nodeName = GetNodeName(i, nodePos);
                
                // 核心：节点类型
                node.nodeType = nodePos.nodeType;
                node.isElite = nodePos.isElite;
                node.isBoss = nodePos.isBoss;
                node.isStartNode = nodePos.isStartNode;
                
                // 遭遇数据
                if (nodePos.presetEncounter != null)
                {
                    node.encounterData = nodePos.presetEncounter;
                }
                else
                {
                    node.encounterData = CreateSimpleEncounter(node);
                }
                
                nodesResult.Add(node);
            }
            
            // 2. 连接节点（保持原有逻辑）
            LinkDataNodes(nodesResult, layout);
            
            // 3. 解锁起始节点
            foreach (var node in nodesResult)
            {
                if (node.isStartNode)
                {
                    node.isUnlocked = true;
                    // 【新增】必须记录到全局列表，否则 NodeInteractionManager 查不到
                    if (GameDataManager.Instance != null)
                    {
                        if (!GameDataManager.Instance.unlockedNodeIds.Contains(node.nodeId))
                            GameDataManager.Instance.unlockedNodeIds.Add(node.nodeId);
                    }
                }
            }
            
            // 4. 输出统计
            LogNodeTypes(nodesResult);
            
            return nodesResult.ToArray();
            foreach (var node in nodesResult)
            {
                if (node.isStartNode)
                {
                    node.isUnlocked = true;
                    
                    // 【新增修复】：同步到全局管理器，确保判定逻辑能查到
                    if (GameDataManager.Instance != null && !GameDataManager.Instance.unlockedNodeIds.Contains(node.nodeId))
                    {
                        GameDataManager.Instance.unlockedNodeIds.Add(node.nodeId);
                    }
                }
            }
        }
        
        private string GetNodeName(int index, MapLayoutSO.ManualNodePosition nodePos)
        {
            if (nodePos.isBoss) return "Boss";
            if (nodePos.isElite) return "精英";
            if (nodePos.isStartNode) return "起点";
            
            return $"{nodePos.nodeType}节点{index}";
        }
        
        private EncounterData CreateSimpleEncounter(MapNodeData node)
        {
            EncounterData data = ScriptableObject.CreateInstance<EncounterData>();
            data.nodeType = node.nodeType;
            data.isElite = node.isElite;
            data.isBoss = node.isBoss;
            
            // 简单奖励设置
            switch (node.nodeType)
            {
                case NodeType.Combat: data.goldReward = 10; break;
                case NodeType.Elite: data.goldReward = 25; break;
                case NodeType.Boss: data.goldReward = 100; break;
                case NodeType.Rest: data.healthRewardPercent = 30; break;
                default: data.goldReward = 0; break;
            }
            
            return data;
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