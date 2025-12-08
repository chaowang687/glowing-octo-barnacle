using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // 引入 UnityEngine.UI 命名空间以使用 RectTransform 和 Image
using System; // 引入 System 命名空间以使用 Action

/// <summary>
/// 负责初始化、设置角色、绑定 UI 视图和启动战斗流程。
/// GameFlowManager 负责桥接数据 (CharacterBase.artwork) 和视图 (Image 组件)。
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
    public float UI_OFFSET_Y = 150f; // 150 pixels above the character pivot
    
    // ⭐ 新增：玩家初始生命值设置 ⭐
    [Header("玩家初始设置")]
    [Tooltip("玩家角色的初始最大生命值。")]
    public int playerInitialHP = 100;

    // 引用 BattleManager 中的 Prefabs
    [Header("角色 Prefabs")]
    public CharacterBase playerPrefab; 
    public GameObject playerDisplayPrefab; // 包含 CharacterUIDisplay 脚本的 UI 视图
    public CharacterBase enemyPrefab; 
    public GameObject enemyDisplayPrefab; // 包含 CharacterUIDisplay 脚本的 UI 视图

    [Header("UI 容器 (RectTransform)")]
    [Tooltip("玩家角色模型 (PlayerPrefab) 将被实例化到此 Canvas/RectTransform 容器中。")]
    public RectTransform playerVisualContainer; 
    [Tooltip("敌人角色模型 (EnemyPrefab) 将被实例化到此 Canvas/RectTransform 容器中。")]
    public RectTransform enemyVisualContainer; 
    
    private CharacterManager characterManager;
    private BattleManager battleManager;
    
    private bool _hasSetupRun = false; 

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
        
        // 2. 启动战斗
        StartBattleSequence();
    }

    private void SetupAllCharactersAndUI()
    {
        if (_hasSetupRun) 
        {
            Debug.LogWarning("SetupAllCharactersAndUI 已被调用，跳过重复设置。请检查场景中是否有重复的 GameFlowManager。");
            return;
        }
        _hasSetupRun = true; 
        
        // --- 容器检查 ---
        if (playerVisualContainer == null || enemyVisualContainer == null)
        {
            Debug.LogError("UI 容器未设置。请确保 playerVisualContainer 和 enemyVisualContainer 已在 Inspector 中链接到 Canvas 下的 RectTransform。");
            return;
        }
        
        // --- 玩家生成 ---
        if (playerPrefab != null)
        {
            SetupPlayer();
        }
        else
        {
            Debug.LogError("Player Prefab 未设置在 GameFlowManager 上，无法生成玩家！");
        }

        // --- 敌人生成 ---
        if (enemyPrefab != null)
        {
            if (battleManager.defaultEnemyDataAsset != null)
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
                Debug.LogError("BattleManager 的 defaultEnemyDataAsset 未设置，无法初始化敌人数据！");
            }
        }
        else
        {
            Debug.LogWarning("Enemy Prefab 未设置，跳过敌人生成。");
        }
    }
    
    /// <summary>
    /// 实例化角色模型和 UI 显示组件，并进行基础初始化。
    /// 【已重新加入：将 Sprite 赋值给 Image 组件的逻辑】
    /// </summary>
    /// <param name="prefab">角色模型 Prefab (包含 CharacterBase)。</param>
    /// <param name="displayPrefab">角色 UI Prefab (包含 CharacterUIDisplay)。</param>
    /// <param name="container">实例化模型的父级 RectTransform 容器。</param>
    /// <param name="anchoredPosition">模型在容器内的锚定位置。</param>
    /// <param name="name">角色的名称。</param>
    /// <param name="maxHp">角色的最大生命值。</param>
    /// <param name="artwork">角色美术资源 Sprite（敌人的立绘）。</param>
    /// <returns>实例化后的 CharacterBase 实例。</returns>
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
            Debug.LogWarning($"角色 {name} Prefab 似乎不是 UI 元素 (缺少 RectTransform)。");
        }
        
        // 3. 调用 CharacterBase.Initialize() 设置基础数据
        characterInstance.Initialize(name, maxHp, artwork); 

        // ⭐ 关键修正: 4. GameFlowManager 职责：将 Sprite 赋值给 Image 组件 (View) ⭐
        if (artwork != null)
        {
            // 尝试在根对象或其子对象上查找 Image 组件 (用于 Canvas UI)
            Image uiImage = characterInstance.GetComponentInChildren<Image>();
            
            if (uiImage != null)
            {
                // --- 赋值逻辑 ---
                uiImage.sprite = artwork;
                
                // 强制设置颜色为不透明白色，防止 Inspector 设置为透明 (Alpha=0)
                uiImage.color = Color.white; 
                
                Debug.Log($"[Artwork SUCCESS] {name}: Artwork '{artwork.name}' successfully assigned to Image component.");
            }
            else
            {
                // 如果是 SpriteRenderer (用于 Scene 3D/2D) - 备用逻辑
                SpriteRenderer sr = characterInstance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = artwork;
                    Debug.Log($"[Artwork] {name}: Artwork successfully assigned to SpriteRenderer component.");
                }
                else
                {
                    Debug.LogError($"[Artwork FAILED] {name}: Model prefab does not contain an Image (UI) or SpriteRenderer component to display the artwork. Check prefab and image component existence/size.");
                }
            }
        }
        // ⭐ 关键修正结束 ⭐


        // 5. 实例化 UI (View) 作为 CharacterBase Model 的子对象
        GameObject displayInstance = Instantiate(displayPrefab, characterInstance.transform); 
        
        // 6. 设置 UI Display 自身的 RectTransform 局部位置 (使用 UI_OFFSET_Y)
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
            displayScript.Initialize(characterInstance);
            Debug.Log($"[Instantiate] {name} 模型和 UI 绑定成功。");
        }
        else
        {
             Debug.LogError($"角色 {name} 的 UI Prefab 缺少 CharacterUIDisplay 组件！");
        }
        
        return characterInstance;
    }

    /// <summary>
    /// 实例化、定位并注册玩家角色。
    /// </summary>
    private void SetupPlayer()
    {
        // 玩家的美术资源 (artwork) 传入 null，因为其 Sprite 应该预先设置在 PlayerPrefab 的 Image 组件上
        CharacterBase playerInstance = InstantiateCharacterVisualsAndUI(
            playerPrefab, 
            playerDisplayPrefab, 
            playerVisualContainer, 
            new Vector2(-300, 0), // 玩家在左侧
            "Ironclad", 
            playerInitialHP, 
            null 
        );

        if (playerInstance == null) return;
        
        // 注册到 CharacterManager (玩家特有逻辑)
        characterManager.activeHero = playerInstance;
        if (!characterManager.allHeroes.Contains(playerInstance))
        {
            characterManager.allHeroes.Add(playerInstance);
        }
    }

    /// <summary>
    /// 实例化并注册单个敌人角色。
    /// </summary>
    private void SpawnSingleEnemy(CharacterBase enemyToSpawn, EnemyData enemyData)
    {
        // 敌人的美术资源 (artwork) 传入 enemyData.artwork
        CharacterBase enemyInstance = InstantiateCharacterVisualsAndUI(
            enemyToSpawn, 
            enemyDisplayPrefab, 
            enemyVisualContainer, 
            new Vector2(300, 0), // 敌人在右侧
            enemyData.enemyName, 
            enemyData.maxHp, 
            enemyData.artwork // <--- 关键：传入 Sprite
        );
        
        if (enemyInstance == null) return;
        
        // 注册到 CharacterManager (敌人特有逻辑)
        if (!characterManager.allEnemies.Contains(enemyInstance))
        {
            characterManager.allEnemies.Add(enemyInstance);
        }
        
        // 初始化 EnemyAI 并设置战斗策略 (敌人特有逻辑)
        EnemyAI enemyAI = enemyInstance.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Initialize(enemyData, enemyData.intentStrategy);
        }
    }

    /// <summary>
    /// 检查条件是否满足，然后启动战斗。
    /// </summary>
    private void StartBattleSequence()
    {
        if (characterManager.GetActiveHero() == null)
        {
            Debug.LogError("无法启动战斗：CharacterManager 中没有有效的活着的 activeHero。");
            return;
        }
        
        if (battleManager != null)
        {
            // 确保牌堆设置完成
            if (battleManager.cardSystem != null)
            {
                battleManager.cardSystem.SetupDeck(); 
            }
            
            Debug.Log("GameFlowManager 正在调用 BattleManager.StartBattle()");
            battleManager.StartBattle(); 
        }
    }
}