using UnityEngine;
using UnityEngine.SceneManagement;
using System; // 引入命名空间

namespace SlayTheSpireMap
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }
        public static event Action<MapNodeData> OnCurrentNodeChanged; // 新增事件
        // 定义事件：参数为受影响的节点数据
        public static event Action<MapNodeData> OnNodeStatusChanged;
        [Header("管理器引用")]
        public MapGenerator mapGenerator;
        public PlayerStateManager playerState;
        public NodeInteractionManager nodeInteraction;
        public SceneTransitionManager sceneTransition;
        public SaveLoadManager saveLoad;
        public UIManager ui;
        
        [Header("地图数据")]
        public MapLayoutSO currentMapLayout;
        public MapNodeData currentNode;
        public MapNodeData[] allNodes;
        [SerializeField] private StraightLineRenderer lineRenderer;
        private void Awake()
        {
            if(lineRenderer == null) lineRenderer = GetComponent<StraightLineRenderer>();
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 自动查找或创建组件
                InitializeComponents();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void InitializeComponents()
        {
            // 确保所有管理器存在
            if (mapGenerator == null) mapGenerator = GetComponent<MapGenerator>();
            if (playerState == null) playerState = GetComponent<PlayerStateManager>();
            if (nodeInteraction == null) nodeInteraction = GetComponent<NodeInteractionManager>();
            if (sceneTransition == null) sceneTransition = GetComponent<SceneTransitionManager>();
            if (saveLoad == null) saveLoad = GetComponent<SaveLoadManager>();
            if (ui == null) ui = GetComponent<UIManager>();
            
            // 如果还没有，创建它们
            if (mapGenerator == null) mapGenerator = gameObject.AddComponent<MapGenerator>();
            if (playerState == null) playerState = gameObject.AddComponent<PlayerStateManager>();
            if (nodeInteraction == null) nodeInteraction = gameObject.AddComponent<NodeInteractionManager>();
            if (sceneTransition == null) sceneTransition = gameObject.AddComponent<SceneTransitionManager>();
            if (saveLoad == null) saveLoad = gameObject.AddComponent<SaveLoadManager>();
            if (ui == null) ui = gameObject.AddComponent<UIManager>();
        }
        
        private void Start()
            {
                // 1. 生成基础地图数据 (MapGenerator)
                allNodes = mapGenerator.GenerateMap(currentMapLayout);
                
                // 2. 尝试加载存档并恢复状态 (SaveLoadManager)
                // 这一步会修改 allNodes 里的 isCompleted 和 isUnlocked
                saveLoad.LoadProgress(playerState, allNodes, out currentNode);
                
                // 3. 渲染视觉表现 (MapLineRenderer & UIManager)
               
                ui.UpdateAllUI();
            }
      public void ResetMapProgress()
    {
        currentNode = null;
        allNodes = null;
        // 如果有UI，也清空UI
        if(ui != null) ui.UpdateAllUI();
    }
        public void SetCurrentNode(MapNodeData node)
        {
            currentNode = node;
            // 触发当前节点变更事件
            OnCurrentNodeChanged?.Invoke(node);
        }
        public void CompleteNode(MapNodeData node)
        {
            if (node == null) return;
            node.isCompleted = true;
            OnNodeStatusChanged?.Invoke(node); // 通知UI变灰/打勾

            foreach (var nextNode in node.connectedNodes)
            {
                nextNode.isUnlocked = true;
                OnNodeStatusChanged?.Invoke(nextNode); // 通知下游节点亮起
            }
            saveLoad?.LoadMapProgress(playerState, currentNode, allNodes);
        }
        
// 补齐缺失的方法
        public string GetCurrentNodeInfo() => currentNode != null ? currentNode.nodeName : "未选择节点";

        public void OnUINodeClicked(UIMapNode uiNode)
        {
            // 处理点击UI逻辑
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
        /// <summary>
        /// 外部调用的便捷方法：完成当前玩家所在的节点
        /// </summary>
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

        /// <summary>
        /// 核心统一处理逻辑：标记完成 -> 解锁下游 -> 存盘 -> 更新UI
        /// </summary>
        /// <param name="node">要标记完成的节点</param>
        

        /// <summary>
        /// 内部辅助：解锁当前节点连接的所有后续节点
        /// </summary>
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
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MapScene")
            {
                // 重新生成地图？或者刷新UI？
                // 但是注意，MapManager的Start也会执行，所以这里可能需要避免重复初始化
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
                OnNodeClicked(currentNode);
            }
        }
        
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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