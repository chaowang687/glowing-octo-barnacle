using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class SaveLoadManager : MonoBehaviour
    {
        // 存档键名
        private const string SAVE_KEY_PLAYER = "PlayerState";
        private const string SAVE_KEY_COMPLETED_NODES = "CompletedNodes";
        private const string SAVE_KEY_CURRENT_NODE = "CurrentNodeId";

        /// <summary>
        /// 保存游戏进度
        /// </summary>
        public void SaveBattleData(BattleData data)
        {
            // 将战斗数据转为 Json 存入 PlayerPrefs
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("MapBattle_Data", json);
            PlayerPrefs.Save();
            Debug.Log("[SaveLoad] 战斗数据已临时缓存");
        }



        public void LoadPlayerProgress(PlayerStateManager playerState, ref MapNodeData currentNode, MapNodeData[] allNodes)
{
    // 1. 加载玩家数值和卡组
    if (PlayerPrefs.HasKey(SAVE_KEY_PLAYER))
    {
        string playerJson = PlayerPrefs.GetString(SAVE_KEY_PLAYER);
        // 这里假设 PlayerStateManager 有一个用于从数据结构恢复的方法
        JsonUtility.FromJsonOverwrite(playerJson, playerState.GetPlayerState());
    }

    // 2. 恢复节点完成状态
    string completedNodesData = PlayerPrefs.GetString(SAVE_KEY_COMPLETED_NODES, "");
    // 将存储的 ID 字符串转回集合以便快速查询
    HashSet<string> completedIds = new HashSet<string>(completedNodesData.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

    // 3. 恢复当前位置 ID
    string currentNodeId = PlayerPrefs.GetString(SAVE_KEY_CURRENT_NODE, "");

    foreach (var node in allNodes)
    {
        // 如果 ID 在已完成列表中，标记为完成
        if (completedIds.Contains(node.nodeId))
        {
            node.isCompleted = true;
            node.isUnlocked = true;
        }

        // 匹配当前玩家站立的节点
        if (node.nodeId == currentNodeId)
        {
            currentNode = node;
        }
    }

    // 4. 关键：根据已完成节点的状态，重新刷新哪些后续节点是可点击的（Unlocked）
    RefreshUnlocks(allNodes);

    Debug.Log("[SaveLoad] 进度加载成功");
}
        public void LoadMapProgress(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes)
        {
            // 1. 保存玩家基础数值和卡组
            string playerJson = JsonUtility.ToJson(playerState.GetPlayerState());
            PlayerPrefs.SetString(SAVE_KEY_PLAYER, playerJson);

            // 2. 保存当前所在位置
            if (currentNode != null)
            {
                PlayerPrefs.SetString(SAVE_KEY_CURRENT_NODE, currentNode.nodeId);
            }

            // 3. 核心：只保存已完成节点的 ID 列表
            List<string> completedIds = allNodes
                .Where(n => n.isCompleted)
                .Select(n => n.nodeId)
                .ToList();
            
            // 将列表转为逗号分隔的字符串存储
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

            // 2. 恢复地图节点的“完成”和“解锁”状态
            string completedNodesData = PlayerPrefs.GetString(SAVE_KEY_COMPLETED_NODES, "");
            HashSet<string> completedIds = new HashSet<string>(completedNodesData.Split(','));

            string currentNodeId = PlayerPrefs.GetString(SAVE_KEY_CURRENT_NODE, "");

            foreach (var node in allNodes)
            {
                // 恢复完成状态
                if (completedIds.Contains(node.nodeId))
                {
                    node.isCompleted = true;
                    node.isUnlocked = true; // 已完成的节点必然是解锁过的
                }

                // 恢复当前位置
                if (node.nodeId == currentNodeId)
                {
                    currentNode = node;
                }
            }

            // 3. 这里的逻辑很关键：根据已完成的节点，重新计算哪些后续节点应该被解锁
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

            // 如果一个节点的前置节点中有任何一个是“已完成”，则该节点解锁
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
    }
}