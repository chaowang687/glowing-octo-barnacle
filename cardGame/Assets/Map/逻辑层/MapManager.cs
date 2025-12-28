using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpireMap
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }
        public static event Action<MapNodeData> OnCurrentNodeChanged;
        public static event Action<MapNodeData> OnNodeStatusChanged;
        // 添加这些常量定义
        private const string SAVE_KEY_PLAYER = "PlayerState";
        private const string SAVE_KEY_COMPLETED_NODES = "CompletedNodes";
        private const string SAVE_KEY_CURRENT_NODE = "CurrentNodeId";
        
     
        [Header("管理器引用")]
        public MapGenerator mapGenerator;
        public PlayerStateManager playerState;
        public NodeInteractionManager nodeInteraction;
        public MapUI mapUI; // 将原来的 UIManager ui 改为 MapUI mapUI，或者新增这个
        public SaveLoadManager saveLoad;
        public MapUIManager ui;
        
        [Header("地图数据")]
        public MapLayoutSO currentMapLayout;
        
        // 【重要】添加 NonSerialized 防止 Unity 序列化 Inspector 中的脏数据
        // 之前因为 MapNodeData 类型变更导致序列化不兼容，可能读取到全0数据
        [System.NonSerialized] public MapNodeData currentNode;
        [System.NonSerialized] public MapNodeData[] allNodes;
        
        [SerializeField] private StraightLineRenderer lineRenderer;
        
        private void Awake()
        {
            // 只要进入地图场景，UI 实例化后立刻告诉 MapManager：“我是新的 UI，请用我！”
    if (MapManager.Instance != null)
    {
       // MapManager.Instance.mapUI = this; 
    }
            if(lineRenderer == null) lineRenderer = GetComponent<StraightLineRenderer>();
    
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 自动查找或创建组件
                InitializeComponents();
                
                // 重要：只在这里生成一次地图
                GenerateMapOnce();

                // 订阅场景加载事件（用于从战斗返回地图时的重建）
                SceneManager.sceneLoaded += HandleSceneLoaded;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        




        private void GenerateMapOnce()
        {
            // 1. 验证现有数据是否有效
            bool isDataValid = allNodes != null && allNodes.Length > 0;
            
            // 额外检查：如果第一个节点位置是 (0,0) 且 ID 为空，说明可能是序列化残留的无效数据
            if (isDataValid && (allNodes[0] == null || (allNodes[0].position == Vector2.zero && string.IsNullOrEmpty(allNodes[0].nodeId))))
            {
                isDataValid = false;
                Debug.LogWarning("[MapManager] 检测到无效的地图数据（可能是序列化兼容性问题），将重新生成。");
            }

            if (isDataValid)
            {
                // 数据有效，直接使用
                return;
            }

            // 2. 如果数据无效或为空，重新生成
            if (currentMapLayout != null)
            {
                allNodes = mapGenerator.GenerateMap(currentMapLayout);
                Debug.Log($"首次生成地图，共 {allNodes?.Length ?? 0} 个节点");
            }
            else
            {
                Debug.LogError("currentMapLayout 未设置，无法生成地图");
            }

            // 3. 刷新 UI（如果有）
            if (mapUI != null) 
            {
                mapUI.GenerateMapUI(); 
            }
        }

// 放到 GenerateMap 方法的结束大括号之后
    private string GetNodeName(int index, MapLayoutSO.ManualNodePosition nodePos)
    {
        if (nodePos.isBoss) return "Boss";
        if (nodePos.isElite) return "精英怪";
        if (nodePos.isStartNode) return "起点";
        
        switch (nodePos.nodeType)
        {
            case NodeType.Combat: return $"战斗节点 {index}";
            case NodeType.Elite: return $"精英节点 {index}";
            case NodeType.Shop: return $"商店 {index}";
            case NodeType.Rest: return $"休息点 {index}";
            case NodeType.Event: return $"未知事件 {index}";
            case NodeType.Boss: return "最终Boss";
            default: return $"节点 {index}";
        }
    }
public void InitializeFirstRun()
{
    if (string.IsNullOrEmpty(GameDataManager.Instance.currentNodeId))
    {
        // 找到所有标记为 StartNode 的节点并加入解锁列表
        foreach (var node in allNodes)
        {
            if (node.isStartNode)
            {
                if (!GameDataManager.Instance.unlockedNodeIds.Contains(node.nodeId))
                {
                    GameDataManager.Instance.unlockedNodeIds.Add(node.nodeId);
                }
            }
        }
        GameDataManager.Instance.SaveGameData();
    }
}
        private void InitializeComponents()
        {
            // 确保所有管理器存在
            if (mapGenerator == null) mapGenerator = GetComponent<MapGenerator>();
            if (playerState == null) playerState = GetComponent<PlayerStateManager>();
            if (nodeInteraction == null) nodeInteraction = GetComponent<NodeInteractionManager>();
            if (saveLoad == null) saveLoad = GetComponent<SaveLoadManager>();
            if (ui == null) ui = GetComponent<MapUIManager>();
            
            // 如果还没有，创建它们
            if (mapGenerator == null) mapGenerator = gameObject.AddComponent<MapGenerator>();
            if (playerState == null) playerState = gameObject.AddComponent<PlayerStateManager>();
            if (nodeInteraction == null) nodeInteraction = gameObject.AddComponent<NodeInteractionManager>();
            if (saveLoad == null) saveLoad = gameObject.AddComponent<SaveLoadManager>();
            if (ui == null) ui = gameObject.AddComponent<MapUIManager>();
        }
        
        private void LoadFromGameDataManagerDirectly()
{
    if (GameDataManager.Instance == null)
    {
        Debug.LogError("GameDataManager不存在！");
        return;
    }
    
    // 打印GameDataManager的当前状态
    Debug.Log("=== GameDataManager状态 ===");
    Debug.Log($"当前节点ID: {GameDataManager.Instance.currentNodeId}");
    Debug.Log($"已完成节点: {string.Join(", ", GameDataManager.Instance.completedNodeIds)}");
    Debug.Log($"已解锁节点: {string.Join(", ", GameDataManager.Instance.unlockedNodeIds)}");
    
    // 遍历所有节点，从GameDataManager恢复状态
    foreach (var node in allNodes)
    {
        string nodeId = node.nodeId;
        
        // 完成状态
        node.isCompleted = GameDataManager.Instance.IsNodeCompleted(nodeId);
        
        // 解锁状态
        node.isUnlocked = GameDataManager.Instance.IsNodeUnlocked(nodeId);
        
        // 如果这是当前节点
        if (nodeId == GameDataManager.Instance.currentNodeId)
        {
            currentNode = node;
            Debug.Log($"找到当前节点: {node.nodeName}");
        }
        
                // 如果是起始节点，确保解锁
                if (node.isStartNode)
                {
                    node.isUnlocked = true;
                    if (!GameDataManager.Instance.IsNodeUnlocked(nodeId))
                    {
                        GameDataManager.Instance.UnlockNode(nodeId);
                    }
                }
            }
            
            // 关键修复：当从 GameDataManager 加载完所有节点状态后，
            // 必须立刻更新 currentNode 指针，否则后续的 UI 刷新（包括 ContinueButton）会读到 null
            if (!string.IsNullOrEmpty(GameDataManager.Instance.currentNodeId))
            {
                var targetNode = allNodes.FirstOrDefault(n => n.nodeId == GameDataManager.Instance.currentNodeId);
                if (targetNode != null)
                {
                    currentNode = targetNode;
                    Debug.Log($"[MapManager] 强制同步当前节点指针: {currentNode.nodeName} ({currentNode.nodeId})");
                    // 触发事件通知 UI 更新
                    OnCurrentNodeChanged?.Invoke(currentNode);
                }
            }
            else
            {
                // 如果没有当前节点（比如新游戏），尝试找到起点
                var startNode = mapGenerator.FindStartNode(allNodes);
                if (startNode != null)
                {
                    currentNode = startNode;
                    Debug.Log($"[MapManager] 无存档进度，自动定位到起点: {startNode.nodeName}");
                    OnCurrentNodeChanged?.Invoke(currentNode);
                }
            }
        }

private void PrintNodeStates()
{
    if (allNodes == null) return;
    
    Debug.Log("=== 节点状态 ===");
    foreach (var node in allNodes)
    {
        Debug.Log($"节点: {node.nodeName}, ID: {node.nodeId}, 完成: {node.isCompleted}, 解锁: {node.isUnlocked}");
    }
}


     private void Start()
{
    Debug.Log("=== MapManager 初始化开始 ===");
    
    // 1. 生成基础地图数据
    allNodes = mapGenerator.GenerateMap(currentMapLayout);
    Debug.Log($"生成 {allNodes?.Length ?? 0} 个节点");
    
    // 2. 直接从GameDataManager加载，不使用SaveLoadManager
    LoadFromGameDataManagerDirectly();
    
    // 3. 如果没有当前节点，设置起始节点
    if (currentNode == null && allNodes != null)
    {
        foreach (var node in allNodes)
        {
            if (node.isStartNode)
            {
                currentNode = node;
                node.isUnlocked = true;
                
                // 确保在GameDataManager中也设置
                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.SetCurrentNode(node.nodeId);
                    GameDataManager.Instance.UnlockNode(node.nodeId);
                }
                
                Debug.Log($"设置起始节点: {node.nodeName}");
                break;
            }
        }
    }
    
    // 4. 打印节点状态（调试用）
    PrintNodeStates();
    RefreshAllMapNodesUI();
    // 5. 渲染视觉表现
    ui.UpdateAllUI();
    
    Debug.Log("=== MapManager 初始化完成 ===");
}


public void RefreshAllMapNodesUI()
{
    // 这里的 allNodesUI 是你生成地图时保存的 UIMapNode 列表
    foreach (var uiNode in FindObjectsOfType<UIMapNode>()) 
    {
        // 确保 UI 节点里的数据是最新的
        if (uiNode.linkedNodeData != null)
        {
            // 从全局数据管理器同步最新状态
            uiNode.linkedNodeData.isUnlocked = GameDataManager.Instance.unlockedNodeIds.Contains(uiNode.linkedNodeData.nodeId);
            uiNode.linkedNodeData.isCompleted = GameDataManager.Instance.completedNodeIds.Contains(uiNode.linkedNodeData.nodeId);
        }
        uiNode.UpdateVisuals();
    }
}
                private void LoadSavedProgress()
{
    if (GameDataManager.Instance == null)
    {
        Debug.LogWarning("GameDataManager未找到，无法加载存档");
        return;
    }
    
    // 如果GameDataManager中有当前节点，恢复它
    if (!string.IsNullOrEmpty(GameDataManager.Instance.currentNodeId))
    {
        // 查找对应的节点
        foreach (var node in allNodes)
        {
            if (node.nodeId == GameDataManager.Instance.currentNodeId)
            {
                currentNode = node;
                break;
            }
        }
    }
    
    // 恢复节点状态
    foreach (var node in allNodes)
    {
        if (GameDataManager.Instance.IsNodeCompleted(node.nodeId))
        {
            node.isCompleted = true;
            node.isUnlocked = false;
        }
        else if (GameDataManager.Instance.IsNodeUnlocked(node.nodeId))
        {
            node.isUnlocked = true;
        }
    }
    
    // 如果没有当前节点，设置起始节点
    if (currentNode == null)
    {
        currentNode = mapGenerator.FindStartNode(allNodes);
        if (currentNode != null)
        {
            currentNode.isUnlocked = true;
            GameDataManager.Instance.SetCurrentNode(currentNode.nodeId);
        }
    }
    
    Debug.Log($"地图进度已加载，当前节点: {currentNode?.nodeName}");
}
        
        /// <summary>
        /// 处理场景加载事件 - 重命名以避免冲突
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MapScene")
            {
                // 1. 重新获取场景内引用
                mapUI = FindObjectOfType<MapUI>();
                lineRenderer = FindObjectOfType<StraightLineRenderer>();

                // 2. 如果上一场景的节点对象已被销毁，allNodes 会包含 null，需重建
                bool needRebuild = allNodes == null || allNodes.Length == 0;
                if (!needRebuild)
                {
                    for (int i = 0; i < allNodes.Length; i++)
                    {
                        if (allNodes[i] == null)
                        {
                            needRebuild = true;
                            break;
                        }
                    }
                }

                // 3. 设置 MapGenerator 的父容器（ScrollRect 的 content）
                var sr = FindObjectOfType<ScrollRect>();
                if (sr != null && mapGenerator != null)
                {
                    mapGenerator.parentContainer = sr.content;
                }

                // 4. 需要重建时，重新生成地图数据
                if (needRebuild)
                {
                    if (currentMapLayout != null)
                    {
                        Debug.Log("[MapManager] 开始重建地图数据...");
                        allNodes = mapGenerator.GenerateMap(currentMapLayout);
                    }
                    else
                    {
                        Debug.LogError("[MapManager] currentMapLayout 未设置，无法重建地图！");
                    }
                }
                
                // 关键修复：无论是否重建了地图数据，只要切回地图场景，都必须从 GameDataManager 
                // 重新同步最新的状态（因为在战斗场景里 currentNodeId 已经变了）
                RestoreMapStateFromGameData();
                Debug.Log($"[MapManager] 地图状态已同步，当前节点指针: {currentNode?.nodeId}");

                // 5. 刷新 UI
                if (mapUI != null)
                {
                    mapUI.GenerateMapUI();
                }
                else
                {
                    Debug.LogError("[MapManager] 在场景中没找到 MapUI，请检查场景中是否有 MapUI 脚本！");
                }
            }
        }
        
        /// <summary>
        /// 从GameDataManager恢复地图状态
        /// </summary>
        private void RestoreMapStateFromGameData()
        {
            if (allNodes == null) return;
            
            foreach (var node in allNodes)
            {
                if (GameDataManager.Instance != null)
                {
                    node.isCompleted = GameDataManager.Instance.IsNodeCompleted(node.nodeId);
                    node.isUnlocked = GameDataManager.Instance.IsNodeUnlocked(node.nodeId);
                }
            }
            
            // 设置当前节点（关键逻辑：必须确保恢复指针）
            if (GameDataManager.Instance != null && !string.IsNullOrEmpty(GameDataManager.Instance.currentNodeId))
            {
                var target = allNodes.FirstOrDefault(n => n.nodeId == GameDataManager.Instance.currentNodeId);
                if (target != null)
                {
                    currentNode = target;
                    // 【关键修复】恢复完状态后，必须立刻通知 UI，否则 ContinueButton 不知道目标变了
                    OnCurrentNodeChanged?.Invoke(currentNode);
                }
            }
        }
        private void SaveToGameDataManager(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes)
{
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

private void SaveToPlayerPrefs(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes)
{
    // 1. 保存玩家基础数值和卡组
    string playerJson = JsonUtility.ToJson(playerState.GetPlayerState());
    PlayerPrefs.SetString(SAVE_KEY_PLAYER, playerJson);

    // 2. 保存当前所在位置
    if (currentNode != null)
    {
        PlayerPrefs.SetString(SAVE_KEY_CURRENT_NODE, currentNode.nodeId);
    }

    // 3. 保存已完成节点的ID列表
    List<string> completedIds = allNodes
        .Where(n => n.isCompleted)
        .Select(n => n.nodeId)
        .ToList();
    
    string completedNodesData = string.Join(",", completedIds);
    PlayerPrefs.SetString(SAVE_KEY_COMPLETED_NODES, completedNodesData);

    PlayerPrefs.Save();
    Debug.Log("存档成功：保存了 " + completedIds.Count + " 个节点的进度");
}

        public void SaveMapProgress(PlayerStateManager playerState, MapNodeData currentNode, MapNodeData[] allNodes)
        {
            // 如果有GameDataManager，优先使用它
            if (GameDataManager.Instance != null)
            {
                SaveToGameDataManager(playerState, currentNode, allNodes);
            }
            else
            {
                // 否则使用旧的PlayerPrefs方法
                SaveToPlayerPrefs(playerState, currentNode, allNodes);
            }
        }
        public void ResetMapProgress()
        {
            currentNode = null;
            allNodes = null;
            if(ui != null) ui.UpdateAllUI();
        }
        
        public void SetCurrentNode(MapNodeData node)
        {
            currentNode = node;
            OnCurrentNodeChanged?.Invoke(node);
        }
        
   public void CompleteNode(MapNodeData node)
{
    if (node == null) return;
    
    node.isCompleted = true;
    OnNodeStatusChanged?.Invoke(node);

    // 解锁连接的节点
    foreach (var nextNode in node.connectedNodes)
    {
        if (nextNode != null)
        {
            nextNode.isUnlocked = true;
            OnNodeStatusChanged?.Invoke(nextNode);
            
            // 自动设置第一个解锁的节点为当前节点
            if (currentNode == node)
            {
                currentNode = nextNode;
                Debug.Log($"自动设置当前节点为: {nextNode.nodeName}");
            }
        }
    }
    
    // 保存到GameDataManager
    if (GameDataManager.Instance != null)
    {
        GameDataManager.Instance.CompleteNode(node.nodeId);
        
        // 解锁连接的节点
        foreach (var nextNode in node.connectedNodes)
        {
            if (nextNode != null)
            {
                GameDataManager.Instance.UnlockNode(nextNode.nodeId);
            }
        }
        
        // 设置新的当前节点（使用第一个连接的节点）
        if (node.connectedNodes.Count > 0 && node.connectedNodes[0] != null)
        {
            GameDataManager.Instance.SetCurrentNode(node.connectedNodes[0].nodeId);
            Debug.Log($"GameDataManager 当前节点更新为: {node.connectedNodes[0].nodeId}");
        }
        
        GameDataManager.Instance.SaveGameData();
    }
    
    // 调用SaveLoadManager的保存方法（现在这个方法存在了）
    if (saveLoad != null)
    {
        // 使用新的统一保存方法，优先使用GameDataManager
        saveLoad.SaveGameProgress(playerState, currentNode, allNodes, true);
    }
    else
    {
        Debug.LogWarning("SaveLoadManager 为空，无法保存地图进度");
    }
}
        
        public string GetCurrentNodeInfo() => currentNode != null ? currentNode.nodeName : "未选择节点";

        public void OnUINodeClicked(UIMapNode uiNode)
        {
            OnNodeClicked(uiNode.linkedNodeData);
        }
        
        private void LoadGame()
        {
            // 1. 加载玩家状态
            saveLoad.LoadPlayerProgress(playerState, ref currentNode, allNodes);
            
            // 2. 生成地图
            if (currentMapLayout != null)
            {
                allNodes = mapGenerator.GenerateMap(currentMapLayout);
            }
            else
            {
                Debug.LogWarning("没有地图布局配置！");
                return;
            }
            
            // 3. 设置当前节点
            currentNode = mapGenerator.FindStartNode(allNodes);
            
            // 4. 更新UI
            ui.UpdateAllUI();
            
            Debug.Log("游戏加载完成");
        }
        
        public void CompleteCurrentNode()
        {
            if (currentNode != null)
            {
                CompleteNode(currentNode);
            }
            else
            {
                Debug.LogWarning("[MapManager] 尝试完成当前节点，但 currentNode 为空。");
            }
        }

        private void UnlockNextSteps(MapNodeData node)
        {
            if (node.connectedNodes == null) return;

            foreach (var nextNode in node.connectedNodes)
            {
                if (nextNode != null)
                {
                    nextNode.isUnlocked = true;
                    Debug.Log($"[MapManager] 已解锁下游节点: {nextNode.nodeName}");
                }
            }
        }
        
        // 公共接口
        public void OnNodeClicked(MapNodeData node)
        {
            nodeInteraction.OnNodeClicked(node, this);
        }
        
        public void OnContinueButtonClicked()
        {
            if (currentNode != null)
            {
                Debug.Log($"[MapManager] 继续按钮点击，进入当前节点: {currentNode.nodeName} ({currentNode.nodeId})");
                OnNodeClicked(currentNode);
            }
            else
            {
                // 如果当前没有选中节点，尝试找到已解锁但未完成的节点（可能是下一层）
                // 这种情况发生在刚打完一关，currentNodeId 虽然更新了，但 MapManager.currentNode 指针可能还没刷新
                // 或者是玩家需要手动选择下一关
                
                // 尝试重新同步一下
                if (GameDataManager.Instance != null && !string.IsNullOrEmpty(GameDataManager.Instance.currentNodeId))
                {
                    var targetNode = allNodes.FirstOrDefault(n => n.nodeId == GameDataManager.Instance.currentNodeId);
                    if (targetNode != null)
                    {
                        currentNode = targetNode;
                        Debug.Log($"[MapManager] 继续按钮自动修正目标为: {currentNode.nodeName}");
                        OnNodeClicked(currentNode);
                        return;
                    }
                }
                
                Debug.LogWarning("[MapManager] 继续按钮点击无效：当前没有选中的节点。");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅场景加载事件
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
        
        // 获取游戏状态（供其他系统使用）
        public GameState GetGameState()
        {
            return new GameState
            {
                playerState = playerState.GetPlayerState(),
                currentNode = currentNode,
                allNodes = allNodes
            };
        }
        
        public struct GameState
        {
            public PlayerStateManager.PlayerState playerState;
            public MapNodeData currentNode;
            public MapNodeData[] allNodes;
        }
    }
}
