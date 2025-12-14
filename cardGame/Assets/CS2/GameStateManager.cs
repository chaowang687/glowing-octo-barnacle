using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ScavengingGame
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }
        
        private EnemyEncounterData _currentEncounterData;

        public enum GameState
        {
            Loading, Exploration, Battle, GameOver, Victory
        }

        [Header("Current State")]
        public GameState CurrentState = GameState.Loading;

        [Header("系统引用")]
        [SerializeField] private ScavengingController scavengingController; 
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private MapGridManager gridManager; 
        
        // 删除未使用的mainSceneName字段，只保留battleSceneName
        [Header("战斗场景配置")]
        [SerializeField] private string battleSceneName = "BattleScene";
        
        public ScavengingController ScavengingController 
        { 
            get => scavengingController;
            set => scavengingController = value;
        }
        
        public IInventoryService PlayerInventory 
        { 
            get => inventoryManager;
        }
        
        public MapGridManager GridManager 
        { 
            get => gridManager;
            set => gridManager = value;
        }

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
                scavengingController = FindAnyObjectByType<ScavengingController>();
            
            if (inventoryManager == null) 
                inventoryManager = FindAnyObjectByType<InventoryManager>();
            
            if (gridManager == null) 
                gridManager = FindAnyObjectByType<MapGridManager>();
            
            ValidateReferences();
            SwitchState(GameState.Exploration);
        }

        private void ValidateReferences()
        {
            if (scavengingController == null)
                Debug.LogError("GameStateManager: ScavengingController 引用缺失！");
            if (inventoryManager == null)
                Debug.LogError("GameStateManager: PlayerInventory (InventoryManager) 引用缺失！");
            if (gridManager == null)
                Debug.LogError("GameStateManager: GridManager 引用缺失！");
        }

        // State Management Core
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
                    if (gridManager != null && gridManager.Dice != null)
                    {
                        gridManager.Dice.SetDiceRollEnabled(true); 
                    }
                    break;
                case GameState.GameOver:
                    Debug.Log("Game Over triggered.");
                    break;
            }
        }

        private void OnStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Exploration:
                    if (gridManager != null && gridManager.Dice != null)
                    {
                        gridManager.Dice.SetDiceRollEnabled(false);
                    }
                    break;
            }
        }

        // ====================================================================
        // Phase Specific Logic
        // ====================================================================

        public void InitiateBattle(EnemyEncounterData encounterData = null)
        {
            _currentEncounterData = encounterData;
            SwitchState(GameState.Battle);
            StartCoroutine(LoadBattleSceneAsync());
        }
        
        private System.Collections.IEnumerator LoadBattleSceneAsync()
        {
            Debug.Log($"[GameStateManager] 开始加载战斗场景: {battleSceneName}");
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
            
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Debug.Log($"战斗场景加载进度: {progress * 100}%");
                yield return null;
            }
            
            Debug.Log("[GameStateManager] 战斗场景加载完成");
            InitializeBattleInLoadedScene();
        }
        
        private void InitializeBattleInLoadedScene()
        {
            BattleManager battleManager = FindBattleManagerInScene();
            
            if (battleManager == null)
            {
                Debug.LogError("[GameStateManager] 在战斗场景中未找到BattleManager！");
                EndBattle(false, new List<ItemData>());
                return;
            }
            
            battleManager.InitializeBattle(_currentEncounterData);
            Debug.Log("[GameStateManager] 战斗已初始化，等待战斗结果...");
        }
        
        private BattleManager FindBattleManagerInScene()
        {
            BattleManager[] battleManagers = FindObjectsByType<BattleManager>(FindObjectsSortMode.None);
            
            if (battleManagers.Length == 0)
            {
                Debug.LogError("未在任何场景中找到BattleManager组件");
                return null;
            }
            
            foreach (BattleManager manager in battleManagers)
            {
                if (manager.gameObject.scene.name == battleSceneName)
                {
                    return manager;
                }
            }
            
            return battleManagers[0];
        }

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
                
                if (PlayerInventory != null)
                {
                    foreach(var item in rewards)
                    {
                        PlayerInventory.AddItem(item, 1);
                    }
                }
                
                StartCoroutine(UnloadBattleSceneAndReturnToExploration(rewards));
            }
            else
            {
                Debug.Log("[GameStateManager] 战斗失败！");
                StartCoroutine(UnloadBattleSceneAndGameOver());
            }
        }
        
        private System.Collections.IEnumerator UnloadBattleSceneAndReturnToExploration(List<ItemData> rewards)
        {
            Debug.Log("[GameStateManager] 开始卸载战斗场景");
            
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(battleSceneName);
            
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
            
            Debug.Log("[GameStateManager] 战斗场景卸载完成");
            
            SwitchState(GameState.Exploration);
            
            if (gridManager != null)
            {
                gridManager.OnBattleFinished(true);
            }
            
            _currentEncounterData = null;
        }
        
        private System.Collections.IEnumerator UnloadBattleSceneAndGameOver()
        {
            Debug.Log("[GameStateManager] 战斗失败，卸载战斗场景");
            
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(battleSceneName);
            
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
            
            SwitchState(GameState.GameOver);
            
            if (gridManager != null)
            {
                gridManager.OnBattleFinished(false);
            }
            
            _currentEncounterData = null;
        }
    }
}