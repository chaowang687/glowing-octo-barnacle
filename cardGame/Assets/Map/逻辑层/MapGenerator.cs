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

            // 根据配置选择生成模式
            if (layout.generationType == MapGenerationType.ProceduralTree)
            {
                return GenerateProceduralTree(layout);
            }
            else
            {
                return GenerateManualMap(layout);
            }
        }

        /// <summary>
        /// 程序化树状生成 (Slay the Spire 风格)
        /// </summary>
        private MapNodeData[] GenerateProceduralTree(MapLayoutSO layout)
        {
            List<MapNodeData> allNodes = new List<MapNodeData>();
            List<List<MapNodeData>> layers = new List<List<MapNodeData>>();
            int layerCount = Mathf.Max(2, layout.treeLayers); // 至少有起点和Boss

            // 1. 生成节点
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                List<MapNodeData> currentLayerNodes = new List<MapNodeData>();
                int nodeCount = 0;

                // 确定本层节点数量
                if (layerIndex == 0) 
                    nodeCount = layout.startNodeCount;
                else if (layerIndex == layerCount - 1 && layout.generateBoss) 
                    nodeCount = 1;
                else 
                    nodeCount = Random.Range(layout.nodesPerLayer.x, layout.nodesPerLayer.y + 1);

                // 生成该层节点
                for (int i = 0; i < nodeCount; i++)
                {
                    MapNodeData node = new MapNodeData();
                    node.nodeId = $"node_{layerIndex}_{i}";
                    
                    // 计算位置 (树状结构自底向上，或者从左向右，这里假设从下向上 Y轴增长)
                    // X轴居中分布
                    float xPos = (i - (nodeCount - 1) / 2f) * layout.nodeDistanceX;
                    // 加上随机偏移
                    xPos += Random.Range(-layout.xRandomOffset, layout.xRandomOffset);
                    float yPos = layerIndex * layout.layerDistanceY;
                    yPos += Random.Range(-layout.yRandomOffset, layout.yRandomOffset);
                    
                    node.position = new Vector2(xPos, yPos);

                    // 确定类型
                    // 修改：传入父节点信息以平衡生成比例
                    SetNodeType(node, layerIndex, layerCount, layout, allNodes);
                    
                    // 初始化 Encounter (从池中随机抽取)
                    node.encounterData = GetRandomEncounterFromPool(node.nodeType, layerIndex, layout);
                    if (node.encounterData == null)
                    {
                        // 保底逻辑
                        node.encounterData = CreateSimpleEncounter(node);
                    }
                    
                    currentLayerNodes.Add(node);
                    allNodes.Add(node);
                }
                layers.Add(currentLayerNodes);
            }

            // 2. 建立连接 (层与层之间)
            for (int i = 0; i < layers.Count - 1; i++)
            {
                List<MapNodeData> currentLayer = layers[i];
                List<MapNodeData> nextLayer = layers[i + 1];

                // 2.1 正向连接：确保每个当前层节点至少连向一个下一层节点
                foreach (var node in currentLayer)
                {
                    // 策略：连接 X 轴距离最近的节点
                    // 也可以增加随机性，比如连接 1-3 个邻居
                    MapNodeData target = GetClosestNode(node, nextLayer);
                    if (target != null)
                    {
                        node.AddConnectedNode(target);
                    }
                    
                    // 随机尝试连接更多邻居 (增加分叉)
                    foreach (var nextNode in nextLayer)
                    {
                        if (nextNode == target) continue;
                        if (Vector2.Distance(node.position, nextNode.position) < layout.nodeDistanceX * 1.5f)
                        {
                            if (Random.value < 0.3f) // 30% 概率产生额外分叉
                            {
                                node.AddConnectedNode(nextNode);
                            }
                        }
                    }
                }

                // 2.2 反向检查：确保每个下一层节点至少被连接一次 (防止出现孤岛)
                foreach (var nextNode in nextLayer)
                {
                    bool isConnected = false;
                    foreach (var prevNode in currentLayer)
                    {
                        // 修复：MapNodeData.connectedNodes 是 List<MapNodeData>，所以应该传入 MapNodeData 对象
                        if (prevNode.connectedNodes.Contains(nextNode))
                        {
                            isConnected = true;
                            break;
                        }
                    }

                    if (!isConnected)
                    {
                        // 强制找一个最近的上层节点连过来
                        MapNodeData closestPrev = GetClosestNode(nextNode, currentLayer);
                        if (closestPrev != null)
                        {
                            closestPrev.AddConnectedNode(nextNode);
                        }
                    }
                }
            }

            // 3. 全局解锁处理
            foreach (var node in layers[0]) // 第一层全部解锁
            {
                node.isUnlocked = true;
                if (GameDataManager.Instance != null && !GameDataManager.Instance.unlockedNodeIds.Contains(node.nodeId))
                {
                    GameDataManager.Instance.unlockedNodeIds.Add(node.nodeId);
                }
            }

            LogNodeTypes(allNodes);
            return allNodes.ToArray();
        }

        private MapNodeData GetClosestNode(MapNodeData source, List<MapNodeData> candidates)
        {
            MapNodeData best = null;
            float minDst = float.MaxValue;
            foreach (var c in candidates)
            {
                float d = Vector2.Distance(source.position, c.position);
                if (d < minDst)
                {
                    minDst = d;
                    best = c;
                }
            }
            return best;
        }

        private void SetNodeType(MapNodeData node, int layerIndex, int layerCount, MapLayoutSO layout, List<MapNodeData> allNodes = null)
        {
            // 特殊层级固定类型
            if (layerIndex == 0)
            {
                node.nodeType = NodeType.Combat; // 起点通常是简单战斗
                node.isStartNode = true;
                node.nodeName = "起点";
                return;
            }
            if (layerIndex == layerCount - 1 && layout.generateBoss)
            {
                node.nodeType = NodeType.Boss;
                node.isBoss = true;
                node.nodeName = "最终Boss";
                return;
            }

            // 优化生成逻辑：统计连线上已有的节点类型分布
            // 如果传入了 allNodes，我们可以尝试根据前一层的类型来调整当前权重（简单实现）
            // Slay the Spire 规则：
            // - 连续Combat节点不能太多
            // - Shop 和 Rest 通常不能连续
            // - Elite 在前几层较少
            
            int combatW = layout.combatWeight;
            int eliteW = layout.eliteWeight;
            int shopW = layout.shopWeight;
            int restW = layout.restWeight;
            int eventW = layout.eventWeight;

            // 动态调整权重
            float progress = (float)layerIndex / layerCount;
            
            // 精英怪权重随进度增加
            // 前 20% 层数不刷精英
            if (progress < 0.2f) 
            {
                eliteW = 0;
            }
            else
            {
                // 线性增加权重：从 0.2f 开始，权重逐渐从 0 增加到 2 * baseEliteWeight
                float eliteFactor = Mathf.InverseLerp(0.2f, 0.8f, progress);
                eliteW = Mathf.RoundToInt(layout.eliteWeight * (0.5f + eliteFactor * 1.5f));
            }
            
            // 简单防连续逻辑（但这需要在连接之后判断才准，生成时还不知道父节点）
            // 在树状生成中，我们是一层层生成的，此时还没建立连接关系。
            // 所以只能根据层级做宏观调控，或者在 SetNodeType 时传入“可能的父节点”
            // 但因为我们是先生成节点再连接，所以无法精确得知父节点。
            
            // 替代方案：根据层索引做分布
            // 例如：商店通常出现在中间层或者最后几层之前
            if (layerIndex == Mathf.FloorToInt(layerCount * 0.5f)) restW += 50; // 中间层休息概率大增
            
            // 归一化权重计算
            int totalWeight = combatW + eliteW + shopW + restW + eventW;
            int rnd = Random.Range(0, totalWeight);

            if (rnd < combatW) { node.nodeType = NodeType.Combat; node.nodeName = "敌人"; }
            else if (rnd < combatW + eliteW) { node.nodeType = NodeType.Elite; node.isElite = true; node.nodeName = "精英"; }
            else if (rnd < combatW + eliteW + shopW) { node.nodeType = NodeType.Shop; node.nodeName = "商店"; }
            else if (rnd < combatW + eliteW + shopW + restW) { node.nodeType = NodeType.Rest; node.nodeName = "休息"; }
            else { node.nodeType = NodeType.Event; node.nodeName = "未知"; }
        }

        // 原有的 GenerateMap 改名为 GenerateManualMap
        private MapNodeData[] GenerateManualMap(MapLayoutSO layout)
        {
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
                    // 如果手动模式没配，则尝试从池中随机生成（兼容混合模式）
                    node.encounterData = GetRandomEncounterFromPool(node.nodeType, 0, layout);
                    if (node.encounterData == null)
                    {
                        node.encounterData = CreateSimpleEncounter(node);
                    }
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

        private EncounterData GetRandomEncounterFromPool(NodeType type, int layerIndex, MapLayoutSO layout)
        {
            List<EncounterData> pool = null;

            switch (type)
            {
                case NodeType.Combat:
                    // 细分难度逻辑：Easy -> Normal -> Hard
                    int totalLayers = layout.treeLayers > 0 ? layout.treeLayers : 15;
                    float progress = (float)layerIndex / totalLayers;

                    if (progress < 0.25f)
                    {
                        // 前 25%: Easy
                        pool = layout.easyEnemyPool;
                    }
                    else if (progress < 0.6f)
                    {
                        // 25% - 60%: Normal
                        pool = layout.normalEnemyPool;
                    }
                    else
                    {
                        // 60%+: Hard
                        // 优先尝试 Hard，如果为空则降级为 Normal
                        if (layout.hardEnemyPool != null && layout.hardEnemyPool.Count > 0)
                        {
                            pool = layout.hardEnemyPool;
                        }
                        else
                        {
                            pool = layout.normalEnemyPool;
                        }
                    }
                    
                    // 兜底：如果选中的池子是空的，尝试其他池子
                    if (pool == null || pool.Count == 0)
                    {
                         if (layout.easyEnemyPool != null && layout.easyEnemyPool.Count > 0) pool = layout.easyEnemyPool;
                         else if (layout.normalEnemyPool != null && layout.normalEnemyPool.Count > 0) pool = layout.normalEnemyPool;
                    }
                    break;
                case NodeType.Elite:
                    pool = layout.eliteEnemyPool;
                    break;
                case NodeType.Boss:
                    pool = layout.bossEnemyPool;
                    break;
                case NodeType.Event:
                    pool = layout.eventPool;
                    break;
                case NodeType.Shop:
                    pool = layout.shopPool;
                    break;
            }

            if (pool != null && pool.Count > 0)
            {
                int rnd = Random.Range(0, pool.Count);
                return pool[rnd];
            }

            return null;
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
