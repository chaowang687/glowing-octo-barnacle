using UnityEngine;
using UnityEngine.SceneManagement;  // 添加这一行

using System.Collections.Generic; // 如果需要使用List，添加这行
namespace SlayTheSpireMap
{
    public class NodeInteractionManager : MonoBehaviour
    {
        /// <summary>
        /// 当节点在UI上被点击时调用
        /// </summary>
       // NodeInteractionManager.cs

// NodeInteractionManager.cs
public void OnNodeClicked(MapNodeData node, MapManager mapManager)
{
    var dataManager = GameDataManager.Instance;
    
    // 判定逻辑
    bool isUnlockedInList = dataManager.unlockedNodeIds.Contains(node.nodeId);
    bool canAccess = isUnlockedInList || (node.isStartNode && string.IsNullOrEmpty(dataManager.currentNodeId));

    if (!canAccess)
    {
        Debug.Log($"[Map] 节点 {node.nodeName} 不可访问。");
        return; 
    }

    // --- 执行进入和场景跳转 ---
    Debug.Log($"[Map] 正在进入: {node.nodeName}");

    // 1. 设置当前节点数据
    dataManager.currentNodeId = node.nodeId;
    dataManager.battleNodeId = node.nodeId;
    dataManager.battleEncounterData = node.encounterData;
    dataManager.SaveGameData(); // 必须保存，否则跳回来数据就丢了

    // 2. 场景跳转
    if (node.nodeType == NodeType.Combat || node.isElite || node.isBoss)
    {
        SceneManager.LoadScene("BattleScene"); // 确保你的场景名叫这个
    }
}
private void EnterActualNode(MapNodeData node, MapManager mapManager)
{
    Debug.Log($"[Map] 成功进入节点: {node.nodeName}");

    // 设置全局数据，让系统知道我们“正在”打这一关
    GameDataManager.Instance.currentNodeId = node.nodeId;
    GameDataManager.Instance.battleNodeId = node.nodeId;
    GameDataManager.Instance.battleEncounterData = node.encounterData;

    // 跳转场景
    if (node.nodeType == NodeType.Combat || node.isElite || node.isBoss)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
    }
    // ... 其他节点跳转逻辑
}
        /// <summary>
        /// 简化的合法性判定：只关心“是不是下一步”
        /// </summary>
        private bool IsMoveValid(MapNodeData targetNode, MapManager mapManager)
        {
            // 如果节点本身没解锁，绝对不能进
            if (targetNode == null || !targetNode.isUnlocked) return false;

            // 情况 A：玩家刚开始游戏，没有当前节点 -> 必须点击起始节点
            if (mapManager.currentNode == null)
            {
                return targetNode.isStartNode;
            }

            // 情况 B：已经在某个节点上，且该节点未完成 -> 只能重新进入当前节点
            if (!mapManager.currentNode.isCompleted)
            {
                return targetNode == mapManager.currentNode;
            }

            // 情况 C：当前节点已完成 -> 目标必须是当前节点的下游邻居
            return mapManager.currentNode.connectedNodes.Contains(targetNode);
        }

        /// <summary>
        /// 状态同步：立即持久化当前位置
        /// </summary>
        private void SyncProgressToDisk(MapManager mapManager)
        {
            if (mapManager.saveLoad != null)
            {
                // 保存玩家位置，即便不保存血量，也要保证重进游戏时停在地图的这个点上
                mapManager.saveLoad.SaveMapProgress(
                    mapManager.playerState, 
                    mapManager.currentNode, 
                    mapManager.allNodes
                );
            }
        }

        private void HandleNodeEffect(MapNodeData node, MapManager mapManager)
        {
            switch (node.nodeType)
            {
                case NodeType.Combat:
                case NodeType.Elite:
                case NodeType.Boss:
                    EnterBattle(node, mapManager);
                    break;

                case NodeType.Rest:
                    SceneTransitionManager.Instance.GoToSceneByNodeType(node.nodeType);
                    break;

                case NodeType.Shop:
                    SceneTransitionManager.Instance.GoToSceneByNodeType(node.nodeType);
                    break;

                case NodeType.Event:
                    SceneTransitionManager.Instance.GoToSceneByNodeType(node.nodeType);
                    break;
            }
        }

        private void EnterBattle(MapNodeData node, MapManager mapManager)
        {
            Debug.Log($"进入战斗: {node.nodeName}");
            
            // 保存当前节点到GameDataManager
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SetCurrentNode(node.nodeId);
                GameDataManager.Instance.SetBattleData(node.nodeId, node.encounterData);
                GameDataManager.Instance.SaveGameData(); // 立即保存
            }
            
            // 跳转到战斗场景
            SceneManager.LoadScene("BattleScene");
        }

        private void ExecuteRest(MapNodeData node, MapManager mapManager)
        {
            // 休息点逻辑：先回血，再标记完成
            int healPercent = node.encounterData?.healthRewardPercent ?? 30;
            mapManager.playerState.HealPercentage(healPercent);
            
            // 关键：完成后需要调用 MapManager 的统一完成方法来解锁后续
            mapManager.CompleteCurrentNode(); 
        }
    }
    [System.Serializable]
    public class BattleData
    {
        public string nodeId;
        public EncounterData encounter;
    }
}