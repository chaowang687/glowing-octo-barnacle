using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class SaveLoadManager : MonoBehaviour
    {
        // 存档键名 - 必须要有这些常量
        private const string SAVE_KEY_PLAYER = "PlayerState";
        private const string SAVE_KEY_COMPLETED_NODES = "CompletedNodes";
        private const string SAVE_KEY_CURRENT_NODE = "CurrentNodeId";

        /// <summary>
        /// 保存战斗数据
        /// </summary>
        public void SaveBattleData(BattleData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("MapBattle_Data", json);
            PlayerPrefs.Save();
            Debug.Log("[SaveLoad] 战斗数据已临时缓存");
        }

        /// <summary>
        /// 加载玩家进度
        /// </summary>
        public void LoadPlayerProgress(PlayerStateManager playerState, ref MapNodeData currentNode, MapNodeData[] allNodes)
        {
            // 1. 加载玩家数值和卡组
            if (PlayerPrefs.HasKey(SAVE_KEY_PLAYER))
            {
                string playerJson = PlayerPrefs.GetString(SAVE_KEY_PLAYER);
                JsonUtility.FromJsonOverwrite(playerJson, playerState.GetPlayerState());
            }

            // 2. 恢复节点完成状态
            string completedNodesData = PlayerPrefs.GetString(SAVE_KEY_COMPLETED_NODES, "");
            HashSet<string> completedIds = new HashSet<string>(completedNodesData.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

            // 3. 恢复当前位置 ID
            string currentNodeId = PlayerPrefs.GetString(SAVE_KEY_CURRENT_NODE, "");

            foreach (var node in allNodes)
            {
                if (completedIds.Contains(node.nodeId))
                {
                    node.isCompleted = true;
                    node.isUnlocked = true;
                }

                if (node.nodeId == currentNodeId)
                {
                    currentNode = node;
                }
            }

            // 4. 重新刷新哪些后续节点是可点击的
            RefreshUnlocks(allNodes);

            Debug.Log("[SaveLoad] 进度加载成功");
        }

        /// <summary>
        /// 保存地图进度到PlayerPrefs
        /// </summary>
        public void SaveMapProgress(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes)
        {
            // 1. 保存玩家基础数值和卡组
            string playerJson = JsonUtility.ToJson(playerState.GetPlayerState());
            PlayerPrefs.SetString(SAVE_KEY_PLAYER, playerJson);

            // 2. 保存当前所在位置
            if (currentNode != null)
            {
                PlayerPrefs.SetString(SAVE_KEY_CURRENT_NODE, currentNode.nodeId);
            }

            // 3. 保存已完成节点的 ID 列表
            List<string> completedIds = allNodes
                .Where(n => n.isCompleted)
                .Select(n => n.nodeId)
                .ToList();
            
            string completedNodesData = string.Join(",", completedIds);
            PlayerPrefs.SetString(SAVE_KEY_COMPLETED_NODES, completedNodesData);

            PlayerPrefs.Save();
            Debug.Log("存档成功：保存了 " + completedIds.Count + " 个节点的进度");
        }

        /// <summary>
        /// 加载进度并恢复地图状态
        /// </summary>
        public void LoadProgress(PlayerStateManager playerState, MapNodeData[] allNodes, out MapNodeData currentNode)
        {
            currentNode = null;

            // 1. 恢复玩家状态
            if (PlayerPrefs.HasKey(SAVE_KEY_PLAYER))
            {
                string playerJson = PlayerPrefs.GetString(SAVE_KEY_PLAYER);
                JsonUtility.FromJsonOverwrite(playerJson, playerState.GetPlayerState());
            }

            // 2. 恢复地图节点的"完成"和"解锁"状态
            string completedNodesData = PlayerPrefs.GetString(SAVE_KEY_COMPLETED_NODES, "");
            HashSet<string> completedIds = new HashSet<string>(completedNodesData.Split(','));

            string currentNodeId = PlayerPrefs.GetString(SAVE_KEY_CURRENT_NODE, "");

            foreach (var node in allNodes)
            {
                if (completedIds.Contains(node.nodeId))
                {
                    node.isCompleted = true;
                    node.isUnlocked = true;
                }

                if (node.nodeId == currentNodeId)
                {
                    currentNode = node;
                }
            }

            // 3. 根据已完成的节点，重新计算哪些后续节点应该被解锁
            RefreshUnlocks(allNodes);

            Debug.Log("读档完成");
        }

        private void RefreshUnlocks(MapNodeData[] allNodes)
        {
            // 起始节点默认解锁
            foreach (var node in allNodes.Where(n => n.isStartNode))
            {
                node.isUnlocked = true;
            }

            // 如果一个节点的前置节点中有任何一个是"已完成"，则该节点解锁
            foreach (var node in allNodes)
            {
                if (node.isCompleted)
                {
                    foreach (var next in node.connectedNodes)
                    {
                        next.isUnlocked = true;
                    }
                }
            }
        }

        /// <summary>
        /// 保存到GameDataManager（新增方法）
        /// </summary>
        public void SaveToGameDataManager(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes)
        {
            if (GameDataManager.Instance == null)
            {
                Debug.LogWarning("GameDataManager不存在，无法保存地图进度");
                return;
            }
            
            Debug.Log("保存地图进度到GameDataManager");
            
            // 保存当前节点
            if (currentNode != null)
            {
                GameDataManager.Instance.SetCurrentNode(currentNode.nodeId);
            }
            
            // 保存所有节点的完成状态
            foreach (var node in allNodes)
            {
                if (node.isCompleted && !GameDataManager.Instance.IsNodeCompleted(node.nodeId))
                {
                    GameDataManager.Instance.CompleteNode(node.nodeId);
                }
                
                if (node.isUnlocked && !GameDataManager.Instance.IsNodeUnlocked(node.nodeId))
                {
                    GameDataManager.Instance.UnlockNode(node.nodeId);
                }
            }
            
            // 保存数据
            GameDataManager.Instance.SaveGameData();
        }

        /// <summary>
        /// 从GameDataManager加载地图进度
        /// </summary>
        public void LoadMapProgressFromGameData(PlayerStateManager playerState, MapNodeData[] allNodes, out MapNodeData currentNode)
        {
            currentNode = null;
            
            if (GameDataManager.Instance == null)
            {
                Debug.LogError("GameDataManager不存在，无法加载地图进度");
                return;
            }
            
            Debug.Log("从GameDataManager加载地图进度");
            
            // 恢复所有节点的状态
            foreach (var node in allNodes)
            {
                string nodeId = node.nodeId;
                
                // 从GameDataManager获取完成状态
                node.isCompleted = GameDataManager.Instance.IsNodeCompleted(nodeId);
                
                // 从GameDataManager获取解锁状态
                node.isUnlocked = GameDataManager.Instance.IsNodeUnlocked(nodeId);
                
                // 恢复当前节点
                if (nodeId == GameDataManager.Instance.currentNodeId)
                {
                    currentNode = node;
                    Debug.Log($"设置当前节点: {node.nodeName}");
                }
            }
            
            // 确保起始节点解锁
            foreach (var node in allNodes.Where(n => n.isStartNode))
            {
                node.isUnlocked = true;
                if (!GameDataManager.Instance.IsNodeUnlocked(node.nodeId))
                {
                    GameDataManager.Instance.UnlockNode(node.nodeId);
                }
            }
        }

        /// <summary>
        /// 统一的保存方法，根据情况选择保存方式
        /// </summary>
        public void SaveGameProgress(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes, bool useGameDataManager = true)
        {
            if (useGameDataManager && GameDataManager.Instance != null)
            {
                SaveToGameDataManager(playerState, currentNode, allNodes);
            }
            else
            {
                SaveMapProgress(playerState, currentNode, allNodes);
            }
        }
    }
}