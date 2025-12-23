
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ScavengingGame;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("UI Containers (Canvas Rects)")]
    public RectTransform heroVisualContainer; 
    public RectTransform enemyVisualContainer; 

    [Header("Prefabs")]
    public GameObject heroPrefab;      // 确保是 UI 预制体 (带 RectTransform)
    public CharacterBase enemyPrefab;  // 确保是 UI 预制体 (带 RectTransform)

    private CharacterManager characterManager;

    private void Awake()
    {
        // 修复单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"销毁重复的GameFlowManager实例: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        // 移除此处的延迟获取，改在 Start 或实际使用时获取
    }

    private void Start()
    {
        // 在 Start 中获取 CharacterManager，确保单例已初始化
        StartCoroutine(InitializeCharacterManager());
    }

    private System.Collections.IEnumerator InitializeCharacterManager()
    {
        // 等待一帧，确保所有 Awake 都执行完毕
        yield return null;
        
        characterManager = CharacterManager.Instance;
        
        if (characterManager == null)
        {
            // 如果仍然找不到，尝试查找现有的 CharacterManager
            characterManager = FindObjectOfType<CharacterManager>();
            
            if (characterManager == null)
            {
                Debug.LogError("GameFlowManager: 无法找到 CharacterManager 实例!");
                // 创建一个新的 CharacterManager
                GameObject managerObj = new GameObject("CharacterManager");
                characterManager = managerObj.AddComponent<CharacterManager>();
                DontDestroyOnLoad(managerObj);
                Debug.Log("已创建新的 CharacterManager 实例");
            }
        }
        
        Debug.Log("GameFlowManager: CharacterManager 初始化完成");
    }

    // --- 核心入口：由外部在战斗开始时调用 ---
    public void SetupEncounter(EnemyEncounterData encounterData)
    {
        // 确保 characterManager 已初始化
        if (characterManager == null)
        {
            characterManager = CharacterManager.Instance;
            
            if (characterManager == null)
            {
                Debug.LogError("Critical Error: CharacterManager Instance is missing in the scene!");
                return;
            }
        }

        if (encounterData == null)
        {
            Debug.LogError("SetupEncounter: encounterData is null");
            return;
        }

        // 1. 生成英雄 (如果 Canvas 里还没有)
        if (characterManager.activeHero == null)
        {
            SetupHero();
        }

        // 2. 清理旧敌人
        if (enemyVisualContainer != null)
        {
            foreach (Transform child in enemyVisualContainer) 
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
        
        if (characterManager.allEnemies != null)
            characterManager.allEnemies.Clear();

        // 3. 按照数据列表生成多个敌人
        if (encounterData.enemyList != null)
        {
            for (int i = 0; i < encounterData.enemyList.Count; i++)
            {
                EnemyData data = encounterData.enemyList[i];
                if (data != null)
                    SpawnEnemy(data, i);
            }
        }

        // 4. 【关键】通知 Canvas 重新计算布局
        // 这一步解决"生成的敌人重叠在 0,0 点"的问题
        Canvas.ForceUpdateCanvases();
        if (enemyVisualContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(enemyVisualContainer);
        
        Debug.Log($"战斗遭遇战已设置完成，生成 {encounterData.enemyList?.Count ?? 0} 个敌人");
    }

    private void SetupHero()
    {
        if (heroPrefab == null)
        {
            Debug.LogError("SetupHero: heroPrefab is null");
            return;
        }
        
        if (heroVisualContainer == null)
        {
            Debug.LogError("SetupHero: heroVisualContainer is null");
            return;
        }
        
        if (characterManager == null)
        {
            Debug.LogError("SetupHero: characterManager is null");
            return;
        }
        
        GameObject heroObj = Instantiate(heroPrefab, heroVisualContainer);
        if (heroObj == null)
        {
            Debug.LogError("SetupHero: 英雄实例化失败");
            return;
        }
        
        CharacterBase heroScript = heroObj.GetComponent<CharacterBase>();
        if (heroScript == null)
        {
            Debug.LogError("SetupHero: 英雄预制体缺少 CharacterBase 组件");
            Destroy(heroObj);
            return;
        }
        
        // 重置 RectTransform
        RectTransform rt = heroObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        
        characterManager.activeHero = heroScript;
        characterManager.RegisterHero(heroScript);

        // ⭐ 关键：确保UI显示组件正确绑定 ⭐
        CharacterUIDisplay uiDisplay = heroObj.GetComponentInChildren<CharacterUIDisplay>(true);
        if (uiDisplay != null)
        {
            uiDisplay.Initialize(heroScript);
            Debug.Log($"玩家 {heroScript.characterName} UI 已成功绑定");
        }
        else
        {
            // 如果预制体中没有，尝试在场景中查找
            uiDisplay = FindObjectOfType<CharacterUIDisplay>();
            if (uiDisplay != null)
            {
                uiDisplay.Initialize(heroScript);
                Debug.Log($"使用场景中的CharacterUIDisplay绑定玩家");
            }
            else
            {
                Debug.LogError($"玩家预制体缺少 CharacterUIDisplay 组件，且场景中也没有找到");
            }
        }
        
        Debug.Log($"英雄 {heroScript.characterName} 已生成");
    }

    private void SpawnEnemy(EnemyData data, int index)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("SpawnEnemy: enemyPrefab is null");
            return;
        }
        
        if (enemyVisualContainer == null)
        {
            Debug.LogError("SpawnEnemy: enemyVisualContainer is null");
            return;
        }
        
        if (data == null)
        {
            Debug.LogError("SpawnEnemy: EnemyData is null");
            return;
        }
        
        if (characterManager == null)
        {
            Debug.LogError("SpawnEnemy: characterManager is null");
            return;
        }

        // 实例化到 Canvas 容器中
        CharacterBase enemyInstance = Instantiate(enemyPrefab, enemyVisualContainer);
        if (enemyInstance == null)
        {
            Debug.LogError("SpawnEnemy: 敌人实例化失败");
            return;
        }
        
        // 重置 RectTransform
        RectTransform rt = enemyInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            
            // ⭐ 新增：为多个敌人设置位置偏移 ⭐
            // 根据索引设置位置，防止多个敌人重叠
            float spacing = 200f; // 敌人之间的间距
            float xPos = index * spacing - ((spacing * (characterManager.allEnemies.Count - 1)) / 2f);
            rt.anchoredPosition = new Vector2(xPos, 0);
        }

        // 初始化数据与 UI 绑定
        InitializeEnemy(enemyInstance, data, index);
        
        characterManager.RegisterEnemy(enemyInstance);
        
        Debug.Log($"敌人 {data.enemyName} 已生成，位置索引: {index}");
    }

    private void InitializeEnemy(CharacterBase instance, EnemyData data, int index)
    {
        if (instance == null || data == null)
        {
            Debug.LogError("InitializeEnemy: instance or data is null");
            return;
        }
        
        // 调用 CharacterBase 原有的 Initialize 设置名字/血量/原画
        instance.Initialize(data.enemyName, data.maxHp, data.artwork);

        // ⭐ 关键：确保CharacterUIDisplay初始化 ⭐
        CharacterUIDisplay uiDisplay = instance.GetComponentInChildren<CharacterUIDisplay>(true);
        if (uiDisplay != null)
        {
            uiDisplay.Initialize(instance);
            Debug.Log($"敌人 {data.enemyName} UI 已成功绑定");
        }
        else
        {
            Debug.LogWarning($"敌人 {data.enemyName} 缺少 CharacterUIDisplay 组件，血条和格挡可能不显示");
        }

        // 绑定 AI 和 UI
        EnemyAI ai = instance.GetComponent<EnemyAI>();
        EnemyDisplay display = instance.GetComponentInChildren<EnemyDisplay>(true);

        if (ai != null)
        {
            if (display != null)
            {
                ai.display = display;
                ai.SetEnemyDisplay(display); // 使用设置方法确保引用正确
                
                // ⭐ 关键修复：确保 CharacterAnimatorController 被正确设置 ⭐
                CharacterAnimatorController animController = instance.GetComponentInChildren<CharacterAnimatorController>(true);
                if (animController != null)
                {
                    // 如果有 Animator 组件，确保它被正确引用
                    Animator animator = instance.GetComponentInChildren<Animator>(true);
                    if (animator != null && animController.characterAnimator == null)
                    {
                        animController.SetAnimator(animator);
                    }
                    
                    // 确保 EnemyDisplay 引用了 CharacterAnimatorController
                    // ⭐ 修复：使用 SetAnimatorController 方法而不是直接赋值 ⭐
                    display.SetAnimatorController(animController);
                    
                    // ⭐ 新增：根据敌人类型设置不同的动画参数 ⭐
                    // 可以根据敌人名称或类型设置不同的动画
                    if (data.enemyName.Contains("Pirate"))
                    {
                        // 海盗特定动画设置
                        Debug.Log($"为海盗 {data.enemyName} 设置特定动画参数");
                    }
                }
                else
                {
                    // 如果没有 CharacterAnimatorController，尝试添加一个
                    animController = instance.gameObject.AddComponent<CharacterAnimatorController>();
                    Animator animator = instance.GetComponentInChildren<Animator>(true);
                    if (animator != null)
                    {
                        animController.characterAnimator = animator;
                        // ⭐ 修复：使用 SetAnimatorController 方法而不是直接赋值 ⭐
                        display.SetAnimatorController(animController);
                    }
                }
                
                // ⭐ 关键修复：这里传递 EnemyData！ ⭐
                // 原来是：display.Initialize(instance);
                // 修改为：传递 EnemyData 参数
                display.Initialize(instance, data);
                
                // ⭐ 额外：确保精灵图显示组件也正确设置 ⭐
                // 检查是否有 CharacterDisplay 组件（用于2D显示）
                CharacterDisplay charDisplay = instance.GetComponentInChildren<CharacterDisplay>(true);
                if (charDisplay != null)
                {
                    charDisplay.Initialize(instance);
                    if (data.artwork != null)
                    {
                        charDisplay.SetCharacterSprite(data.artwork);
                    }
                }
                
                Debug.Log($"敌人 {data.enemyName} 显示系统初始化完成");
            }
            else
            {
                Debug.LogWarning($"敌人 {data.enemyName} 缺少 EnemyDisplay 组件");
            }
            
            ai.Initialize(data, data.intentStrategy);
        }
        else
        {
            Debug.LogWarning($"敌人 {data.enemyName} 缺少 EnemyAI 组件");
        }
        
        // ⭐ 额外检查：确保 Animator 状态正确 ⭐
        Animator enemyAnimator = instance.GetComponentInChildren<Animator>();
        if (enemyAnimator != null)
        {
            Debug.Log($"敌人 {data.enemyName} 的 Animator 状态: enabled={enemyAnimator.enabled}, runtimeAnimatorController={enemyAnimator.runtimeAnimatorController != null}");
            
            // ⭐ 新增：确保 Animator 已启用 ⭐
            if (!enemyAnimator.enabled)
            {
                enemyAnimator.enabled = true;
                Debug.Log($"已启用敌人 {data.enemyName} 的 Animator");
            }
        }
        else
        {
            Debug.LogWarning($"敌人 {data.enemyName} 没有找到 Animator 组件");
        }
        
        // ⭐ 新增：设置敌人层级，防止重叠时的渲染问题 ⭐
        Canvas enemyCanvas = instance.GetComponent<Canvas>();
        if (enemyCanvas != null)
        {
            enemyCanvas.sortingOrder = 10 + index; // 根据索引设置渲染层级
        }
    }
    
    // ⭐ 新增：为敌人设置动画 ⭐
    public void TriggerEnemyAttackAnimation(CharacterBase enemy)
    {
        EnemyDisplay display = enemy.GetComponentInChildren<EnemyDisplay>();
        if (display != null)
        {
            // ⭐ 修复：EnemyDisplay中没有TriggerAttackAnimation方法，暂时注释掉 ⭐
            // display.TriggerAttackAnimation();
            Debug.Log($"触发 {enemy.characterName} 的攻击动画（方法已注释，等待后续实现）");
        }
    }
    
    // ⭐ 新增：为敌人设置受伤动画 ⭐
    public void TriggerEnemyHitAnimation(CharacterBase enemy)
    {
        EnemyDisplay display = enemy.GetComponentInChildren<EnemyDisplay>();
        if (display != null)
        {
            // 注意：Hit动画通常在HandleHit方法中自动触发
            Debug.Log($"{enemy.characterName} 已触发受伤动画");
        }
    }
}
