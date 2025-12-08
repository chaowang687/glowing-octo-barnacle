using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // 引入 UnityEngine.UI 命名空间以使用 RectTransform 和 Image
using System; // 引入 System 命名空间以使用 Action

/// <summary>
/// 负责初始化、设置角色、绑定 UI 视图和启动战斗流程。
/// GameFlowManager 负责桥接数据 (CharacterBase.artwork) 和视图 (Image 组件)。
/// 注意：战斗回合流程的驱动逻辑已移至 BattleManager。
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    // --- 1. 强制单例模式 (Singleton Implementation) ---
    public static GameFlowManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // --- 单例模式结束 ---
    
    [Header("UI 定位设置")] 
    [Tooltip("角色血条UI相对于角色模型锚点的垂直偏移量 (Y轴)。")]
    public float UI_OFFSET_Y = 150f; 
    
    [Header("玩家初始设置")]
    [Tooltip("玩家角色的初始最大生命值。")]
    public int playerInitialHP = 100;

    // 引用 Prefabs
    [Header("角色 Prefabs")]
    public CharacterBase playerPrefab; 
    public GameObject playerDisplayPrefab; 
    public CharacterBase enemyPrefab; 
    public GameObject enemyDisplayPrefab; 

    [Header("UI 容器 (RectTransform)")]
    public RectTransform playerVisualContainer; 
    public RectTransform enemyVisualContainer; 
    
    // 关键系统引用
    private CharacterManager characterManager;
    private BattleManager battleManager;
    
    private bool _hasSetupRun = false; 
    
    // 移除 BattleState 和 CurrentRound 变量，将控制权交给 BattleManager
    
    private void Start()
    {
        // 确保单例管理器已被引用
        characterManager = CharacterManager.Instance;
        battleManager = BattleManager.Instance;
        
        if (characterManager == null || battleManager == null)
        {
            Debug.LogError("CharacterManager 或 BattleManager 未找到，请确保它们在场景中并已Awake。");
            return;
        }

        // 1. 设置所有角色 (实例化并注册)
        SetupAllCharactersAndUI();
        
        // 2. 启动战斗 (流程控制交给 BattleManager)
        StartBattleSequence();
    }

    private void SetupAllCharactersAndUI()
    {
        if (_hasSetupRun) 
        {
            Debug.LogWarning("SetupAllCharactersAndUI 已被调用，跳过重复设置。");
            return;
        }
        _hasSetupRun = true; 
        
        // --- 容器检查 ---
        if (playerVisualContainer == null || enemyVisualContainer == null)
        {
            Debug.LogError("UI 容器未设置。");
            return;
        }
        
        // --- 玩家生成 ---
        if (playerPrefab != null)
        {
            SetupPlayer();
        }
        else
        {
            Debug.LogError("Player Prefab 未设置！");
        }

        // --- 敌人生成 ---
        if (enemyPrefab != null && battleManager.defaultEnemyDataAsset != null)
        {
             if (battleManager.defaultEnemyDataAsset is EnemyData enemyData)
             {
                SpawnSingleEnemy(enemyPrefab, enemyData); 
             }
             else
             {
                Debug.LogError("defaultEnemyDataAsset 存在，但不是有效的 EnemyData 类型！");
             }
        }
        else
        {
            Debug.LogError("敌人 Prefab 或 EnemyData 未设置，跳过敌人生成。");
        }
        
        // ⭐ 修复 CS1061：初始化 CharacterManager 的 ActiveEnemies 列表 ⭐
        // 将所有已注册的敌人添加到活动列表中（假定一个简单的单波战斗）
        characterManager.ActiveEnemies.Clear();
        foreach (var enemy in characterManager.allEnemies)
        {
            if (enemy != null)
            {
                characterManager.ActiveEnemies.Add(enemy);
            }
        }
    }
    
    /// <summary>
    /// 实例化角色模型和 UI 显示组件，并进行基础初始化。
    /// </summary>
    private CharacterBase InstantiateCharacterVisualsAndUI(
        CharacterBase prefab, 
        GameObject displayPrefab, 
        RectTransform container, 
        Vector2 anchoredPosition, 
        string name, 
        int maxHp, 
        Sprite artwork)
    {
        // 1. 实例化角色 (Model)
        CharacterBase characterInstance = Instantiate(prefab, container);

        if (characterInstance == null)
        {
            Debug.LogError($"实例化角色 {name} 失败！");
            return null;
        }

        // 2. 设置模型 RectTransform 位置
        RectTransform characterRect = characterInstance.GetComponent<RectTransform>();
        if (characterRect != null)
        {
             characterRect.localPosition = Vector3.zero; 
             characterRect.anchoredPosition = anchoredPosition; 
             characterRect.localScale = Vector3.one;
        }
        else
        {
            characterInstance.transform.localPosition = Vector3.zero; 
            Debug.LogWarning($"角色 {name} Prefab 似乎不是 UI 元素。");
        }
        
        // 3. 调用 CharacterBase.Initialize() 设置基础数据
        // ⭐ 修复 CS1061：依赖 CharacterBase 中正确的 Initialize 签名 ⭐
        characterInstance.Initialize(name, maxHp, artwork); 

        // 4. GameFlowManager 职责：将 Sprite 赋值给 Image 组件 (View)
        if (artwork != null)
        {
            Image uiImage = characterInstance.GetComponentInChildren<Image>();
            
            if (uiImage != null)
            {
                uiImage.sprite = artwork;
                uiImage.color = Color.white; 
            }
            else
            {
                SpriteRenderer sr = characterInstance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = artwork;
                }
                else
                {
                    Debug.LogError($"[Artwork FAILED] {name}: Prefab 缺少 Image 或 SpriteRenderer 组件。");
                }
            }
        }

        // 5. 实例化 UI (View) 作为 CharacterBase Model 的子对象
        GameObject displayInstance = Instantiate(displayPrefab, characterInstance.transform); 
        
        // 6. 设置 UI Display 自身的 RectTransform 局部位置
        RectTransform displayRect = displayInstance.GetComponent<RectTransform>();
        if (displayRect != null)
        {
             displayRect.anchoredPosition = new Vector2(0, UI_OFFSET_Y); 
             displayRect.localScale = Vector3.one;
        }
        
        // 7. 绑定 View 到 Model
        CharacterUIDisplay displayScript = displayInstance.GetComponent<CharacterUIDisplay>();
    
        if (displayScript != null)
        {
            // ⭐ 修复 CS1061：依赖 CharacterUIDisplay 中正确的 Initialize 签名 ⭐
            displayScript.Initialize(characterInstance);
        }
        else
        {
             Debug.LogError($"角色 {name} 的 UI Prefab 缺少 CharacterUIDisplay 组件！");
        }
        
        return characterInstance;
    }

    private void SetupPlayer()
    {
        // 玩家的美术资源 (artwork) 传入 null，因为其 Sprite 应该预先设置在 PlayerPrefab 上
        CharacterBase playerInstance = InstantiateCharacterVisualsAndUI(
            playerPrefab, 
            playerDisplayPrefab, 
            playerVisualContainer, 
            new Vector2(-300, 0), 
            "Ironclad", 
            playerInitialHP, 
            null 
        );

        if (playerInstance == null) return;
        
        // 注册到 CharacterManager
        characterManager.activeHero = playerInstance;
        if (!characterManager.allHeroes.Contains(playerInstance))
        {
            characterManager.allHeroes.Add(playerInstance);
        }
    }

    private void SpawnSingleEnemy(CharacterBase enemyToSpawn, EnemyData enemyData)
    {
        CharacterBase enemyInstance = InstantiateCharacterVisualsAndUI(
            enemyToSpawn, 
            enemyDisplayPrefab, 
            enemyVisualContainer, 
            new Vector2(300, 0), 
            enemyData.enemyName, 
            enemyData.maxHp, 
            enemyData.artwork 
        );
        
        if (enemyInstance == null) return;
        
        // 注册到 CharacterManager
        if (!characterManager.allEnemies.Contains(enemyInstance))
        {
            characterManager.allEnemies.Add(enemyInstance);
        }
        
        // 初始化 EnemyAI 并设置战斗策略
        EnemyAI enemyAI = enemyInstance.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            // ⭐ 修复 CS1061：依赖 EnemyAI 中正确的 Initialize 签名 ⭐
            enemyAI.Initialize(enemyData, enemyData.intentStrategy);
        }
    }

    private void StartBattleSequence()
    {
        if (characterManager.GetActiveHero() == null)
        {
            Debug.LogError("无法启动战斗：没有有效的 activeHero。");
            return;
        }
        
        if (battleManager != null)
        {
            Debug.Log("GameFlowManager: 场景设置完毕，将战斗控制权移交给 BattleManager.StartBattle()");
            // BattleManager 负责调用 cardSystem.SetupDeck() 和 StartNewTurn()
            battleManager.StartBattle(); 
        }
    }
    
    // 以下流程方法已从 GameFlowManager 移除或被 BattleManager 接管，
    // 以保证只有一个地方驱动回合流程。
    
    /*
    // 移除 StartPlayerTurn
    // 移除 StartEnemyTurn
    // 移除 PerformAllEnemyActions
    // 移除 EndEnemyTurn
    // 移除 CalculateAllEnemyIntents
    */
}