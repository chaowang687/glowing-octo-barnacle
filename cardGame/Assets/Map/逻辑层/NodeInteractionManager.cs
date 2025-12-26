using UnityEngine;

namespace SlayTheSpireMap
{
    public class NodeInteractionManager : MonoBehaviour
    {
        /// <summary>
        /// 当节点在UI上被点击时调用
        /// </summary>
        public void OnNodeClicked(MapNodeData node, MapManager mapManager)
        {
            // 1. 路径与状态校验（核心准入逻辑）
            if (!IsMoveValid(node, mapManager))
            {
                Debug.Log($"[Map] 节点 {node.nodeName} 当前不可访问。");
                return;
            }
            mapManager.ui.UpdateAllUI();
            // 2. 状态同步：锁定位置并存盘
            // 只要点击成功，玩家的当前位置就固定了，防止“悔棋”
            mapManager.currentNode = node;
            SyncProgressToDisk(mapManager);

            // 3. 逻辑分发
            HandleNodeEffect(node, mapManager);
            
           
        
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
                mapManager.saveLoad.LoadMapProgress(
                    mapManager.playerState, 
                    mapManager.currentNode, 
                    mapManager.allNodes
                );
            }
        }

        private void HandleNodeEffect(MapNodeData node, MapManager mapManager)
        {
            Debug.Log($"[Map] 进入节点类型: {node.nodeType}");

            switch (node.nodeType)
            {
                case NodeType.Combat:
                case NodeType.Elite:
                case NodeType.Boss:
                    EnterBattle(node, mapManager);
                    break;

                case NodeType.Rest:
                    ExecuteRest(node, mapManager);
                    break;

                case NodeType.Shop:
                    // 逻辑略：跳转商店界面
                    break;

                case NodeType.Event:
                    // 逻辑略：触发随机事件
                    break;
            }
        }

        private void EnterBattle(MapNodeData node, MapManager mapManager)
        {
            // 封装最简战斗数据：ID + 具体的遭遇配置
            var battleData = new BattleData
            {
                nodeId = node.nodeId,
                encounter = node.encounterData 
            };
            
            mapManager.saveLoad.SaveBattleData(battleData);
            mapManager.sceneTransition.GoToBattleScene();
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