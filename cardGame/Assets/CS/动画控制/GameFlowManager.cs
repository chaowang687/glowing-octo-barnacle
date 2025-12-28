using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ScavengingGame;
using System.Collections;
using System.Linq;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement; // 添加场景管理命名空间
using SlayTheSpireMap;

/// <summary>
/// 游戏流程管理器：负责角色生成、UI弹窗动画、回合切换视觉效果
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("UI 容器设置")]
    public RectTransform heroVisualContainer; 
    public RectTransform enemyVisualContainer; 

    [Header("预制体引用")]
    public GameObject heroPrefab;      
    public CharacterBase enemyPrefab;  

    // UI 组件已迁移至 BattleUIManager
    // 原有的弹窗和结算面板引用已移除
    
    private CharacterManager characterManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 确保 BattleUIManager 存在（如果在同一物体上，尝试获取；或者依赖场景中的单例）
        if (BattleUIManager.Instance == null)
        {
            // 如果没有找到，尝试在自身或子物体查找
            var uiManager = GetComponent<BattleUIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[GameFlowManager] 场景中缺少 BattleUIManager，部分UI功能可能无法使用。");
            }
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeCharacterManager());
    }




    // GameFlowManager.cs
    // 提供给 BattleManager 调用的统一接口
    public void InitializeBattleFromData(EnemyEncounterData encounter)
    {
        if (encounter == null)
        {
            Debug.LogError("InitializeBattleFromData: 传入的 encounter 数据为空！");
            return;
        }
        
        if (encounter.enemyList == null || encounter.enemyList.Count == 0)
        {
             Debug.LogWarning("InitializeBattleFromData: 敌人列表为空！");
             return;
        }

        // 清理容器中已有的测试敌人
        foreach (Transform child in enemyVisualContainer)
        {
            Destroy(child.gameObject);
        }

        // 循环生成敌人
        for (int i = 0; i < encounter.enemyList.Count; i++)
        {
            // 调用类内部已有的 SpawnEnemy 方法
            SpawnEnemy(encounter.enemyList[i], i);
        }
        
        Debug.Log($"[GameFlow] 已根据数据生成遭遇战：{encounter.encounterName} (敌人数量: {encounter.enemyList.Count})");
    }
    public void SetupBattlefield()
{
    // 1. 获取当前节点对应的遭遇战数据
    var encounter = GameDataManager.Instance.battleEncounterData as EnemyEncounterData;
    if (encounter == null) return;

    // 2. 遍历敌人列表，动态生成
    for (int i = 0; i < encounter.enemyList.Count; i++)
    {
        EnemyData data = encounter.enemyList[i];
        
        // 生成敌人预制体
        CharacterBase enemy = Instantiate(enemyPrefab, enemyVisualContainer);
        
        // 关键：将 SO 里的数据注入到实例中
        enemy.Initialize(data.enemyName, data.maxHp, data.artwork);
        
        // 如果有 AI 或显示逻辑，也在这里初始化
        enemy.GetComponent<EnemyAI>().Initialize(data, data.intentStrategy);
    }
}
    private IEnumerator InitializeCharacterManager()
    {
        yield return null; 
        characterManager = CharacterManager.Instance;
        
        // 监听战斗结束事件
        if (characterManager != null)
        {
            // 监听角色死亡事件来判断战斗结束
            // 这需要根据你的CharacterManager实际实现来调整
        }
    }
    // GameFlowManager.cs 约 125 行左右
        public void InitializeBattle() 
        {
            // 1. 获取基础数据
            var baseEncounter = GameDataManager.Instance.battleEncounterData;
            if (baseEncounter == null) return;

            // 2. 关键修复：将其强制转换为你的敌人遭遇战类型
            EnemyEncounterData encounter = baseEncounter as EnemyEncounterData;
            
            if (encounter != null)
            {
                // 现在可以访问 enemyList 了
                for (int i = 0; i < encounter.enemyList.Count; i++)
                {
                    SpawnEnemy(encounter.enemyList[i], i);
                }
            }
            else
            {
                Debug.LogError("当前遭遇战数据不是 EnemyEncounterData 类型！");
            }
        }
    /// <summary>
    /// 初始化结算页面 - 已废弃，逻辑移至 BattleUIManager
    /// </summary>
    private void InitializeResultPanels()
    {
        // 委托给 BattleUIManager
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideAllResultPanels();
        }
    }

    #region 胜利/失败结算
    
    /// <summary>
    /// 显示胜利结算页面
    /// </summary>
    public void ShowVictoryPanel(string title = "victory！")
    {
        // 保存战斗结果
        SaveVictoryResult();
        
        // 调用 UI 管理器显示面板
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.ShowVictoryPanel(title);
        }
        else
        {
            Debug.LogError("[GameFlowManager] 找不到 BattleUIManager，无法显示胜利面板！");
        }
    }
    
    /// <summary>
    /// 保存胜利结果到BattleDataManager
    /// </summary>
    private void SaveVictoryResult()
    {
        if (BattleDataManager.Instance != null)
        {
            // 计算胜利奖励
            int goldReward = CalculateGoldReward();
            string cardReward = GetRandomCardReward();
            string relicReward = GetRandomRelicReward();
            
            // 保存结果
            BattleDataManager.Instance.SaveBattleResult(true, goldReward, cardReward, relicReward);
            
            // 显示奖励（委托给 UI Manager）
            if (BattleUIManager.Instance != null)
            {
                BattleUIManager.Instance.ShowRewardDisplay(goldReward, cardReward, relicReward);
            }
        }
    }
    
    /// <summary>
    /// 计算金币奖励
    /// </summary>
    private int CalculateGoldReward()
    {
        // 根据敌人难度计算金币奖励
        int baseReward = 50;
        int difficultyBonus = 0;
        
        if (characterManager != null && characterManager.allEnemies != null)
        {
            difficultyBonus = characterManager.allEnemies.Count * 10;
        }
        
        return baseReward + difficultyBonus;
    }
    
    /// <summary>
    /// 获取随机卡牌奖励
    /// </summary>
    private string GetRandomCardReward()
    {
        // 这里可以定义你的卡牌池
        string[] cardPool = { "强力攻击", "防御强化", "治疗术", "火焰冲击", "冰霜新星" };
        int randomIndex = Random.Range(0, cardPool.Length);
        return cardPool[randomIndex];
    }
    
    /// <summary>
    /// 获取随机遗物奖励
    /// </summary>
    private string GetRandomRelicReward()
    {
        // 这里可以定义你的遗物池
        string[] relicPool = { "生命之环", "力量护符", "智慧之书", "幸运硬币", "守护者之盾" };
        int randomIndex = Random.Range(0, relicPool.Length);
        return relicPool[randomIndex];
    }
    
    private void ShowRewardDisplay(int gold, string card, string relic)
    {
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.ShowRewardDisplay(gold, card, relic);
        }
    }
    
    /// <summary>
    /// 显示失败结算页面
    /// </summary>
    public void ShowDefeatPanel(string title = "you fail")
    {
        // 保存失败结果
        SaveDefeatResult();
        
        // 调用 UI 管理器显示面板
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.ShowDefeatPanel(title);
        }
        else
        {
            Debug.LogError("[GameFlowManager] 找不到 BattleUIManager，无法显示失败面板！");
        }
    }
    
    // 移除了具体的 Sequence 协程方法 (VictorySequence, DefeatSequence, DefeatSequence)，
    // 它们现在是 BattleUIManager 的私有实现。
    
    /// <summary>
    /// 保存失败结果到BattleDataManager
    /// </summary>
    private void SaveDefeatResult()
    {
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.SaveBattleResult(false);
        }
    }

    // OnVictoryContinue, OnDefeatRestart, OnDefeatMainMenu 
    // 这些方法需要保留为 public，供 BattleUIManager 的按钮事件回调调用
    public void OnVictoryContinue()
    {
        // 恢复时间
        Time.timeScale = 1f;
        
        // 隐藏所有面板
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideAllResultPanels();
        }
        
        // 关键：更新节点状态并移动到下一关
        UpdateMapNodeAfterVictory();
        
        // 保存数据
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGameData();
        }
        
        // 直接加载地图场景
        SceneManager.LoadScene("MapScene");
    }
    
    private void UpdateMapNodeAfterVictory()
    {
        if (GameDataManager.Instance != null)
        {
            string battleNodeId = GameDataManager.Instance.battleNodeId;
            if (!string.IsNullOrEmpty(battleNodeId))
            {
                // 关键修复：只调用 CompleteNode，由它负责根据地图连接关系解锁邻居
                // 移除了错误的 GetNextNodeId 线性推测逻辑
                GameDataManager.Instance.CompleteNode(battleNodeId);
                
                GameDataManager.Instance.SaveGameData();
                GameDataManager.Instance.ClearBattleData();
            }
        }
    }

    public void OnDefeatRestart()
    {
        // 恢复时间
        Time.timeScale = 1f;
        
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideAllResultPanels();
        }
        
        // 重新加载当前战斗场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void OnDefeatMainMenu()
    {
        // 恢复时间
        Time.timeScale = 1f;
        
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideAllResultPanels();
        }
        
        // 返回主菜单
        SceneManager.LoadScene("MainMenu"); 
    }
    
    public void HideAllResultPanels()
    {
        if (BattleUIManager.Instance != null)
        {
            BattleUIManager.Instance.HideAllResultPanels();
        }
    }

    #endregion

    #region 弹窗逻辑 (委托模式)

    public void ShowPopup(string message)
    {
        if (BattleUIManager.Instance != null)
            BattleUIManager.Instance.ShowPopup(message);
    }
    
    public void ShowPopup(string message, float duration)
    {
        if (BattleUIManager.Instance != null)
            BattleUIManager.Instance.ShowPopup(message, duration);
    }

    public void ShowQuickTurnPopup(string message)
    {
        if (BattleUIManager.Instance != null)
            BattleUIManager.Instance.ShowQuickTurnPopup(message);
    }

    public void ShowPopupWithTween(string message, float duration = 0.8f)
    {
        // 这里可以直接复用 ShowPopup，或者在 BattleUIManager 里也实现一个 Tween 版本
        // 目前为了简化，直接调用 ShowPopup
        if (BattleUIManager.Instance != null)
            BattleUIManager.Instance.ShowPopup(message, duration);
    }

    // 移除了所有具体的协程实现 (QuickPopupSequence, VeryQuickPopupSequence, QuickPopupWithTween)

    #endregion

    #region 角色生成与布局
    public void SetupEncounter(EnemyEncounterData encounterData)
    {
        if (characterManager == null) characterManager = CharacterManager.Instance;
        if (encounterData == null || characterManager == null) return;

        if (characterManager.activeHero == null) SetupHero();

        ClearExistingEnemies();

        if (encounterData.enemyList != null)
        {
            for (int i = 0; i < encounterData.enemyList.Count; i++)
            {
                SpawnEnemy(encounterData.enemyList[i], i);
            }
        }

        RefreshLayout();
    }

    private void ClearExistingEnemies()
    {
        if (enemyVisualContainer != null)
        {
            foreach (Transform child in enemyVisualContainer) Destroy(child.gameObject);
        }
        characterManager.allEnemies?.Clear();
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (enemyVisualContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(enemyVisualContainer);
    }

    private void SetupHero()
{
    if (heroPrefab == null || heroVisualContainer == null) return;

    // 1. 生成英雄
    GameObject heroObj = Instantiate(heroPrefab, heroVisualContainer);
    
    // 2. 关键修复：获取 Hero 子类脚本，而不是 CharacterBase
    Hero heroScript = heroObj.GetComponent<Hero>();

    if (heroScript != null)
    {
        // 3. 核心修复：先同步数据（血量从存档里读出 30）
        heroScript.SyncFromGlobal();

        // 4. 注册到管理器
        characterManager.RegisterHero(heroScript);
        
        // 5. 最后初始化 UI（此时 UI 能读到正确的 30 血量）
        heroObj.GetComponentInChildren<CharacterUIDisplay>(true)?.Initialize(heroScript);
        
        Debug.Log($"[SetupHero] 英雄生成并同步完成，当前血量: {heroScript.currentHp}");
    }
    else
    {
        Debug.LogError("预制体上没挂 Hero 脚本！");
    }
}
    // GameFlowManager.cs 内部
    // 添加这个公共方法供 BattleManager 调用
    public void SpawnHeroFromData()
    {
        if (heroPrefab == null)
        {
            Debug.LogError("SpawnHeroFromData: heroPrefab 为空！请在 Inspector 中赋值。");
            return;
        }
        if (heroVisualContainer == null)
        {
            Debug.LogError("SpawnHeroFromData: heroVisualContainer 为空！请在 Inspector 中赋值。");
            return;
        }

        // 1. 生成英雄实例
        GameObject heroObj = Instantiate(heroPrefab, heroVisualContainer);
        Hero heroScript = heroObj.GetComponent<Hero>();

        if (heroScript != null)
        {
            if (characterManager == null) characterManager = CharacterManager.Instance;
            if (characterManager != null)
            {
                // 2. 注册到角色管理器
                characterManager.RegisterHero(heroScript);
            }
            else
            {
                Debug.LogError("SpawnHeroFromData: 找不到 CharacterManager 实例！");
            }

            // 3. 【关键】先同步数据！！
            heroScript.SyncFromGlobal();

            // 4. 【关键】最后初始化 UI
            heroObj.GetComponentInChildren<CharacterUIDisplay>(true)?.Initialize(heroScript);

            Debug.Log($"[GameFlow] 英雄生成成功，当前血量: {heroScript.currentHp}");
        }
        else
        {
             Debug.LogError("SpawnHeroFromData: 生成的英雄预制体上没有 Hero 脚本！");
        }
    }
    private void SpawnEnemy(EnemyData data, int index)
    {
        // 增加详细的排查日志
        if (data == null) { Debug.LogError("SpawnEnemy: 传入的 EnemyData 是空的！"); return; }
        if (enemyPrefab == null) { Debug.LogError("GameFlowManager: Enemy Prefab 引用丢失！"); return; }
        if (enemyVisualContainer == null) { Debug.LogError("GameFlowManager: Enemy Visual Container 引用丢失！"); return; }
        if (characterManager == null) { 
            // 尝试自动找一下，防止没拖拽
            characterManager = FindObjectOfType<CharacterManager>();
            if(characterManager == null) {
                Debug.LogError("GameFlowManager: 找不到 CharacterManager！请在场景中创建并拖入引用。"); 
                return; 
            }
        }

        CharacterBase enemyInstance = Instantiate(enemyPrefab, enemyVisualContainer);
        InitializeEnemy(enemyInstance, data, index);
        characterManager.RegisterEnemy(enemyInstance);
    }

    private void InitializeEnemy(CharacterBase instance, EnemyData data, int index)
    {
        instance.Initialize(data.enemyName, data.maxHp, data.artwork);
        instance.GetComponentInChildren<CharacterUIDisplay>(true)?.Initialize(instance);

        EnemyAI ai = instance.GetComponent<EnemyAI>();
        EnemyDisplay display = instance.GetComponentInChildren<EnemyDisplay>(true);

        if (ai != null && display != null)
        {
            ai.display = display;
            ai.SetEnemyDisplay(display);
            
            CharacterAnimatorController animCtrl = instance.GetComponentInChildren<CharacterAnimatorController>(true);
            if (animCtrl != null)
            {
                Animator anim = instance.GetComponentInChildren<Animator>(true);
                if (anim != null) animCtrl.SetAnimator(anim);
                display.SetAnimatorController(animCtrl);
            }

            display.Initialize(instance, data);
            ai.Initialize(data, data.intentStrategy);
        }
    }

    #endregion
}