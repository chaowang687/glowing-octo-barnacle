using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Placeholder to fix 'using CardDataEnums;' compiler error
using CardDataEnums; 

namespace ScavengingGame
{
    /// <summary>
    /// Game State Manager (Core Controller).
    /// Manages the main game flow (Exploration, Battle, Game Over).
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        // 1. Singleton 
        public static GameStateManager Instance { get; private set; }
        
        // 新增：存储当前遭遇数据，用于传递给战斗场景
        private EnemyEncounterData _currentEncounterData;

        // 2. Game State Enumeration
        public enum GameState
        {
            Loading,     // Initialization settings
            Exploration, // Map exploration, movement
            Battle,      // Card battle sequence
            GameOver,    // Game over
            Victory      // Game victory
        }

        [Header("Current State")]
        public GameState CurrentState = GameState.Loading;

        // 3. System References (全部使用 [SerializeField] 拖拽赋值)
        [Header("系统引用")]
        [SerializeField] private ScavengingController scavengingController; 
        [SerializeField] private InventoryManager playerInventory; 
        [SerializeField] private MapGridManager gridManager; 
        
        // 新增：战斗场景相关引用
        [Header("战斗场景配置")]
        [SerializeField] private string battleSceneName = "BattleScene";
        [SerializeField] private string mainSceneName = "MainScene";
        
        // 属性访问器 - 修改为可读可写属性
        public ScavengingController ScavengingController 
        { 
            get => scavengingController;
            set => scavengingController = value;
        }
        
        public InventoryManager PlayerInventory 
        { 
            get => playerInventory;
            set => playerInventory = value;
        }
        
        public MapGridManager GridManager 
        { 
            get => gridManager;
            set => gridManager = value;
        }

        // 4. Initialization
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 运行时检查：如果Inspector中未赋值，尝试自动查找
            if (scavengingController == null) 
            {
                scavengingController = FindObjectOfType<ScavengingController>();
                if (scavengingController != null) 
                    Debug.LogWarning("ScavengingController 未在Inspector中设置，已自动查找。");
            }
            
            if (playerInventory == null) 
            {
                playerInventory = FindObjectOfType<InventoryManager>();
                if (playerInventory != null) 
                    Debug.LogWarning("PlayerInventory 未在Inspector中设置，已自动查找。");
            }
            
            if (gridManager == null) 
            {
                gridManager = FindObjectOfType<MapGridManager>();
                if (gridManager != null) 
                    Debug.LogWarning("GridManager 未在Inspector中设置，已自动查找。");
            }
            
            // 检查所有必要引用
            ValidateReferences();
            
            SwitchState(GameState.Exploration);
        }

        /// <summary>
        /// 验证所有必要引用是否存在
        /// </summary>
        private void ValidateReferences()
        {
            if (scavengingController == null)
                Debug.LogError("GameStateManager: ScavengingController 引用缺失！");
            if (playerInventory == null)
                Debug.LogError("GameStateManager: PlayerInventory 引用缺失！");
            if (gridManager == null)
                Debug.LogError("GameStateManager: GridManager 引用缺失！");
        }

        // 5. State Management Core
        public void SwitchState(GameState newState)
        {
            if (CurrentState == newState) return;

            Debug.Log($"[Game State] Switching from {CurrentState} to {newState}");

            OnStateExit(CurrentState);
            CurrentState = newState;
            OnStateEnter(newState);
        }

        private void OnStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Exploration:
                    // 确保骰子可用
                    if (gridManager != null && gridManager.Dice != null)
                    {
                        gridManager.Dice.SetDiceRollEnabled(true); 
                    }
                    break;
                case GameState.GameOver:
                    Debug.Log("Game Over triggered.");
                    // TODO: Show GameOver screen
                    break;
            }
        }

        private void OnStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Exploration:
                    // 离开探索模式时，禁用骰子
                    if (gridManager != null && gridManager.Dice != null)
                    {
                        gridManager.Dice.SetDiceRollEnabled(false);
                    }
                    break;
            }
        }

        // ====================================================================
        // 6. Phase Specific Logic - 独立战斗场景方案
        // ====================================================================

        /// <summary>
        /// 初始化战斗，使用独立场景
        /// </summary>
        /// <param name="encounterData">敌人遭遇数据（可选）</param>
        public void InitiateBattle(EnemyEncounterData encounterData = null)
        {
            // 保存当前遭遇数据
            _currentEncounterData = encounterData;
            
            // 切换到战斗状态
            SwitchState(GameState.Battle);
            
            // 加载战斗场景
            StartCoroutine(LoadBattleSceneAsync());
        }
        
        /// <summary>
        /// 异步加载战斗场景
        /// </summary>
        private System.Collections.IEnumerator LoadBattleSceneAsync()
        {
            Debug.Log($"[GameStateManager] 开始加载战斗场景: {battleSceneName}");
            
            // 异步加载战斗场景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
            
            // 等待场景加载完成
            while (!asyncLoad.isDone)
            {
                // 可以在这里显示加载进度
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Debug.Log($"战斗场景加载进度: {progress * 100}%");
                yield return null;
            }
            
            Debug.Log("[GameStateManager] 战斗场景加载完成");
            
            // 场景加载完成后，查找并初始化BattleManager
            InitializeBattleInLoadedScene();
        }
        
        /// <summary>
        /// 在加载的战斗场景中初始化战斗
        /// </summary>
        private void InitializeBattleInLoadedScene()
        {
            // 查找战斗场景中的BattleManager
            BattleManager battleManager = FindBattleManagerInScene();
            
            if (battleManager == null)
            {
                Debug.LogError("[GameStateManager] 在战斗场景中未找到BattleManager！");
                EndBattle(false, new List<ItemData>());
                return;
            }
            
            // 初始化战斗
            battleManager.InitializeBattle(_currentEncounterData);
            
            Debug.Log("[GameStateManager] 战斗已初始化，等待战斗结果...");
        }
        
        /// <summary>
        /// 在战斗场景中查找BattleManager
        /// </summary>
        private BattleManager FindBattleManagerInScene()
        {
            // 查找当前活动场景中的所有BattleManager
            BattleManager[] battleManagers = FindObjectsOfType<BattleManager>();
            
            if (battleManagers.Length == 0)
            {
                Debug.LogError("未在任何场景中找到BattleManager组件");
                return null;
            }
            
            // 如果有多个，尝试找到在战斗场景中的那个
            foreach (BattleManager manager in battleManagers)
            {
                // 简单的检查：如果manager的游戏对象在战斗场景中
                if (manager.gameObject.scene.name == battleSceneName)
                {
                    return manager;
                }
            }
            
            // 如果没找到，返回第一个
            return battleManagers[0];
        }

        /// <summary>
        /// 从BattleManager接收战斗结束回调
        /// </summary>
        /// <param name="isVictory">True if the player won.</param>
        /// <param name="rewards">List of ItemData rewards.</param>
        public void EndBattle(bool isVictory, List<ItemData> rewards)
        {
            if (CurrentState != GameState.Battle) 
            {
                Debug.LogWarning($"尝试在非战斗状态({CurrentState})结束战斗");
                return;
            }

            if (isVictory)
            {
                Debug.Log($"[GameStateManager] 战斗胜利，获得{rewards.Count}个奖励");
                
                // 添加奖励到背包
                if (playerInventory != null)
                {
                    foreach(var item in rewards)
                    {
                        playerInventory.AddItem(item);
                    }
                }
                
                // 卸载战斗场景
                StartCoroutine(UnloadBattleSceneAndReturnToExploration(rewards));
            }
            else
            {
                Debug.Log("[GameStateManager] 战斗失败！");
                StartCoroutine(UnloadBattleSceneAndGameOver());
            }
        }
        
        /// <summary>
        /// 卸载战斗场景并返回探索状态
        /// </summary>
        private System.Collections.IEnumerator UnloadBattleSceneAndReturnToExploration(List<ItemData> rewards)
        {
            Debug.Log("[GameStateManager] 开始卸载战斗场景");
            
            // 异步卸载战斗场景
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(battleSceneName);
            
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
            
            Debug.Log("[GameStateManager] 战斗场景卸载完成");
            
            // 切换到探索状态
            SwitchState(GameState.Exploration);
            
            // 通知MapGridManager战斗结束并传递奖励
            if (gridManager != null)
            {
                // 注意：这里调用一个参数的版本，因为原MapGridManager中只有一个参数的OnBattleFinished
                gridManager.OnBattleFinished(true);
            }
            
            // 清空当前遭遇数据
            _currentEncounterData = null;
        }
        
        /// <summary>
        /// 卸载战斗场景并进入游戏结束状态
        /// </summary>
        private System.Collections.IEnumerator UnloadBattleSceneAndGameOver()
        {
            Debug.Log("[GameStateManager] 战斗失败，卸载战斗场景");
            
            // 异步卸载战斗场景
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(battleSceneName);
            
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
            
            // 切换到游戏结束状态
            SwitchState(GameState.GameOver);
            
            // 通知MapGridManager战斗失败
            if (gridManager != null)
            {
                // 注意：这里调用一个参数的版本
                gridManager.OnBattleFinished(false);
            }
            
            // 清空当前遭遇数据
            _currentEncounterData = null;
        }
    }
}