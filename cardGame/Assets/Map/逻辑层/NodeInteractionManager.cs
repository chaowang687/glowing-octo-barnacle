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
            
            // 判定：是否是合法可进入的节点
            // 使用 MapManager 统一的进度验证逻辑，不再依赖 loose 的 unlockedNodeIds
            bool canAccess = mapManager.IsNodeInteractable(node);
            
            // 特殊情况：如果是当前已选中的节点，当然可以再次点击（虽然状态不变）
            if (node.nodeId == dataManager.currentNodeId) canAccess = true;

            if (!canAccess)
            {
                Debug.Log($"[Map] 节点 {node.nodeName} 不可访问 (不符合进度规则)。");
                return; 
            }

            // --- 修改逻辑：点击节点仅“选中”并设为当前节点，不直接跳转 ---
            Debug.Log($"[Map] 选中节点: {node.nodeName} ({node.nodeId})");

            // 1. 更新当前节点状态
            dataManager.currentNodeId = node.nodeId;
            mapManager.SetCurrentNode(node); // 让 MapManager 知道当前选中了谁

            // 2. 刷新 UI：更新所有节点的视觉状态（高亮当前节点，置灰不可达节点）
            // 关键：必须调用 mapManager.ui.UpdateAllUI() 或 mapManager.RefreshAllMapNodesUI()
            // RefreshAllMapNodesUI 会遍历所有 UIMapNode 并调用 UpdateVisuals -> SyncFromData -> SetAsCurrentNode
            // 这样就能确保旧节点收到“我不再是 CurrentNode”的通知，从而关闭高亮
            
            if (mapManager.ui != null && mapManager.ui.mapUI != null)
            {
                // 使用 MapUI 的 UpdateUI 方法更直接
                mapManager.ui.mapUI.UpdateUI();
            }
            else
            {
                // 兜底
                mapManager.RefreshAllMapNodesUI();
            }
            
            // 3. 激活 Continue 按钮（如果有 MapUIManager）
            if (mapManager.ui != null)
            {
                // 让 UI 管理器刷新 Continue 按钮状态（例如变成 "Enter Battle"）
                mapManager.ui.SetContinueButtonState(true);
            }
        }
private void EnterActualNode(MapNodeData node, MapManager mapManager)
        {
            Debug.Log($"[Map] 成功进入节点: {node.nodeName}");

            // 设置全局数据，让系统知道我们“正在”打这一关
            GameDataManager.Instance.currentNodeId = node.nodeId;
            GameDataManager.Instance.battleNodeId = node.nodeId;
            GameDataManager.Instance.battleEncounterData = node.encounterData;
            
            // --- 关键补充：解锁子节点逻辑 ---
            // 当真正进入一个节点时（或完成它时），通常我们希望在这里预先解锁它的连接关系
            // 但在 Slay the Spire 逻辑中，只有当节点“完成”后才解锁下一层。
            // 所以这里的逻辑应该是：
            // 1. 玩家进入节点 -> 战斗/事件场景
            // 2. 玩家获胜/完成 -> 调用 GameFlowManager.UpdateMapNodeAfterVictory -> GameDataManager.CompleteNode
            // 3. CompleteNode 方法内部会查找所有 connectedNodes 并将它们设为 isUnlocked = true
            
            // 所以这里不需要手动解锁，只需确保 GameDataManager.CompleteNode 正确工作即可。
            
            // 跳转场景
            if (node.nodeType == NodeType.Combat || node.isElite || node.isBoss)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
            }
            else if (node.nodeType == NodeType.Rest || node.nodeType == NodeType.Shop || node.nodeType == NodeType.Event)
            {
                 // 使用 SceneTransitionManager 跳转非战斗场景
                 SceneTransitionManager.Instance.GoToSceneByNodeType(node.nodeType);
            }
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
    // 调试：在这里打印一下，看看到底抓到的是哪关的数据
    Debug.Log($"[Map] 点击了节点: {node.nodeId}, 准备加载敌人: {node.encounterData?.encounterName}");
    if (GameDataManager.Instance != null)
    {
        // 关键：把当前节点物体的配置塞进全局单例
        GameDataManager.Instance.battleEncounterData = node.encounterData;
        GameDataManager.Instance.currentNodeId = node.nodeId;
        // 如果这里 node.encounterData 还是空的，它就不会更新单例里的数据
        // 导致战斗场景加载时，从单例里拿到的还是上一关剩下的数据
        //GameDataManager.Instance.SetBattleData(node.nodeId, node.encounterData);
    }
    Debug.Log($"进入战斗: {node.nodeName}");

    // --- 修复逻辑开始 ---
    // 如果当前节点没有敌人配置，尝试从地图布局资源中找回它
    if (node.encounterData == null && mapManager.currentMapLayout != null)
    {
        Debug.LogWarning($"[Map] 节点 {node.nodeName} 缺少实时配置，尝试从 MapLayout 中匹配数据...");
        // 假设你的 MapLayout 有个方法能按 ID 或索引返回配置
        // 如果没有现成方法，你需要确保生成时就塞进去
    }
    // --- 修复逻辑结束 ---

    if (GameDataManager.Instance != null)
    {
        GameDataManager.Instance.SetCurrentNode(node.nodeId);
        // 关键：确保这里传过去的数据不是 null
        GameDataManager.Instance.SetBattleData(node.nodeId, node.encounterData);
        GameDataManager.Instance.SaveGameData(); 
    }
    
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