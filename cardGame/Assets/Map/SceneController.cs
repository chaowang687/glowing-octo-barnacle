using UnityEngine;
// SceneController.cs 修改部分
namespace SlayTheSpireMap
{
    public class SceneController : MonoBehaviour
    {
        // 删除原来的SceneData类，改用EncounterData
        
        private EncounterData currentEncounterData;
        
        // 修改LoadNodeScene方法
        public void LoadNodeScene(NodeType nodeType, int layer, int totalLayers)
        {
            // 创建EncounterData
            currentEncounterData = new EncounterData()
            {
                nodeType = nodeType,
                currentLayer = layer,
                totalLayers = totalLayers,
                isElite = (nodeType == NodeType.Elite),
                isBoss = (nodeType == NodeType.Boss),
                encounterIndex = layer - 1 // 根据层数确定encounter索引
            };
            
            // 设置奖励（可以根据节点类型设置不同的奖励）
            switch(nodeType)
            {
                case NodeType.Combat:
                    currentEncounterData.goldReward = 10;
                    break;
                case NodeType.Elite:
                    currentEncounterData.goldReward = 25;
                    currentEncounterData.relicReward = "RandomRelic";
                    break;
                case NodeType.Boss:
                    currentEncounterData.goldReward = 100;
                    currentEncounterData.relicReward = "BossRelic";
                    break;
                case NodeType.Shop:
                    currentEncounterData.goldReward = 0;
                    break;
                case NodeType.Rest:
                    currentEncounterData.healthReward = 30; // 回复30%最大生命值
                    break;
                case NodeType.Event:
                    // 随机事件，奖励随机
                    currentEncounterData.goldReward = Random.Range(-10, 30);
                    break;
            }
            
            // 加载对应场景
            switch(nodeType)
            {
                case NodeType.Combat:
                case NodeType.Elite:
                case NodeType.Boss:
                    StartCoroutine(TransitionToScene(battleSceneName));
                    break;
                    
                case NodeType.Shop:
                    StartCoroutine(TransitionToScene(shopSceneName));
                    break;
                    
                case NodeType.Event:
                    StartCoroutine(TransitionToScene(eventSceneName));
                    break;
                    
                case NodeType.Rest:
                    // 休息点直接处理
                    HandleRestNode();
                    break;
            }
        }
        
        // 休息点处理
        void HandleRestNode()
        {
            Debug.Log("在休息点回复生命");
            
            // 这里可以添加具体的回复逻辑
            // 比如调用MapManager的某个方法来回复玩家生命值
            
            // 然后直接返回地图，不需要场景切换
            ReturnToMapScene(true);
        }
        
        // 修改InitializeBattleScene方法
        void InitializeBattleScene()
        {
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager != null && currentEncounterData != null)
            {
                // 现在传递EncounterData
                battleManager.InitializeBattle(currentEncounterData);
            }
        }
        
        // 获取当前EncounterData
        public EncounterData GetCurrentEncounterData()
        {
            return currentEncounterData;
        }
    }
}