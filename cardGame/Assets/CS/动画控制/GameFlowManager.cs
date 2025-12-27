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

    [Header("UI 弹窗设置")]
    public GameObject turnPopup; 
    public TextMeshProUGUI popupText;
    public Animator popupAnimator; // 弹窗的动画组件
    
    [Header("结算页面设置")]
    public GameObject victoryPanel; // 胜利结算页
    public GameObject defeatPanel;  // 失败结算页
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI defeatTitleText;
    public Button victoryContinueButton;
    public Button defeatRestartButton;
    public Button defeatMainMenuButton;
    
    [Header("胜利奖励设置")]
    public GameObject rewardPanel; // 奖励面板
    public TextMeshProUGUI goldRewardText;
    public GameObject cardRewardDisplay;
    public GameObject relicRewardDisplay;
    
    [Header("动画时长设置")]
    public float popupShowDuration = 0.8f;  // 弹窗显示总时长
    public float showAnimDuration = 0.3f;   // 入场动画时长
    public float hideAnimDuration = 0.3f;   // 出场动画时长
    
    private CanvasGroup popupCanvasGroup; 
    private CharacterManager characterManager;
    
    // ⭐ 重要：使用独立的协程变量，确保不会被其他脚本意外停止
    private Coroutine currentPopupCoroutine; 

    private static readonly int ShowHash = Animator.StringToHash("Show");
    private static readonly int HideHash = Animator.StringToHash("Hide");

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

        if (turnPopup != null)
        {
            if (popupAnimator == null)
            {
                popupAnimator = turnPopup.GetComponent<Animator>();
                if (popupAnimator == null)
                {
                    Debug.LogWarning("[GameFlowManager] 未找到Animator组件！");
                }
            }
            
            popupCanvasGroup = turnPopup.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null) 
                popupCanvasGroup = turnPopup.AddComponent<CanvasGroup>();

            turnPopup.SetActive(false);
        }
        
        // 初始化结算页面
        InitializeResultPanels();
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
    /// 初始化结算页面
    /// </summary>
    private void InitializeResultPanels()
    {
        // 隐藏所有结算页面
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);
        
        // 设置按钮事件
        if (victoryContinueButton != null)
        {
            victoryContinueButton.onClick.RemoveAllListeners();
            victoryContinueButton.onClick.AddListener(OnVictoryContinue);
        }
        
        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.RemoveAllListeners();
            defeatRestartButton.onClick.AddListener(OnDefeatRestart);
        }
        
        if (defeatMainMenuButton != null)
        {
            defeatMainMenuButton.onClick.RemoveAllListeners();
            defeatMainMenuButton.onClick.AddListener(OnDefeatMainMenu);
        }
    }

    #region 胜利/失败结算
    
    /// <summary>
    /// 显示胜利结算页面
    /// </summary>
    public void ShowVictoryPanel(string title = "战斗胜利！")
    {
        if (victoryPanel == null)
        {
            Debug.LogWarning("[GameFlowManager] 胜利结算面板未设置！");
            return;
        }
        
        // 保存战斗结果
        SaveVictoryResult();
        
        StartCoroutine(VictorySequence(title));
    }
    
    private IEnumerator VictorySequence(string title)
    {
        // 等待一小段时间，让战斗动画播放完成
        yield return new WaitForSeconds(1f);
        
        // 显示胜利标题
        if (victoryTitleText != null) victoryTitleText.text = title;
        
        // 激活面板并播放入场动画
        victoryPanel.SetActive(true);
        
        // 如果有动画组件，播放动画
        Animator victoryAnimator = victoryPanel.GetComponent<Animator>();
        if (victoryAnimator != null)
        {
            victoryAnimator.SetTrigger("Show");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // 使用DOTween作为备用
            CanvasGroup victoryCanvas = victoryPanel.GetComponent<CanvasGroup>();
            if (victoryCanvas == null) victoryCanvas = victoryPanel.AddComponent<CanvasGroup>();
            
            victoryCanvas.alpha = 0f;
            victoryCanvas.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.5f);
        }
        
        // 暂停游戏或阻止其他输入
        Time.timeScale = 0f;
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
            
            // 显示奖励（可选）
            ShowRewardDisplay(goldReward, cardReward, relicReward);
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
    
    /// <summary>
    /// 显示奖励信息
    /// </summary>
    private void ShowRewardDisplay(int gold, string card, string relic)
    {
        if (rewardPanel != null)
        {
            if (goldRewardText != null) goldRewardText.text = $"+{gold} 金币";
            
            // 这里可以添加卡牌和遗物显示的代码
            Debug.Log($"胜利奖励: {gold}金币, 卡牌:{card}, 遗物:{relic}");
        }
    }
    
    /// <summary>
    /// 显示失败结算页面
    /// </summary>
    public void ShowDefeatPanel(string title = "战斗失败...")
    {
        if (defeatPanel == null)
        {
            Debug.LogWarning("[GameFlowManager] 失败结算面板未设置！");
            return;
        }
        
        // 保存失败结果
        SaveDefeatResult();
        
        StartCoroutine(DefeatSequence(title));
    }
    
    private IEnumerator DefeatSequence(string title)
    {
        // 等待一小段时间，让战斗动画播放完成
        yield return new WaitForSeconds(1f);
        
        // 显示失败标题
        if (defeatTitleText != null) defeatTitleText.text = title;
        
        // 激活面板并播放入场动画
        defeatPanel.SetActive(true);
        
        // 如果有动画组件，播放动画
        Animator defeatAnimator = defeatPanel.GetComponent<Animator>();
        if (defeatAnimator != null)
        {
            defeatAnimator.SetTrigger("Show");
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // 使用DOTween作为备用
            CanvasGroup defeatCanvas = defeatPanel.GetComponent<CanvasGroup>();
            if (defeatCanvas == null) defeatCanvas = defeatPanel.AddComponent<CanvasGroup>();
            
            defeatCanvas.alpha = 0f;
            defeatCanvas.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.5f);
        }
        
        // 暂停游戏或阻止其他输入
        Time.timeScale = 0f;
    }
    
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
    
    /// <summary>
    /// 胜利后继续游戏
    /// </summary>
    /// 
    private void UnlockNextNode(string currentNodeId)
{
    // 假设节点ID格式为：Node1, Node2, Node3...
    if (currentNodeId.StartsWith("Node"))
    {
        try
        {
            // 提取数字部分
            string numberStr = currentNodeId.Substring(4);
            int currentNumber = int.Parse(numberStr);
            int nextNumber = currentNumber + 1;
            string nextNodeId = $"Node{nextNumber}";
            
            // 解锁下一个节点
            GameDataManager.Instance.UnlockNode(nextNodeId);
            
            // 设置下一个节点为当前节点
            GameDataManager.Instance.SetCurrentNode(nextNodeId);
            
            Debug.Log($"解锁下一个节点: {nextNodeId}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"解析节点ID失败: {currentNodeId}, 错误: {e.Message}");
        }
    }
    else
    {
        Debug.LogWarning($"不支持的节点ID格式: {currentNodeId}");
    }
}
    private void CompleteCurrentNodeInGameData()
{
    if (GameDataManager.Instance != null)
    {
        // 1. 获取当前战斗节点ID
        string currentNodeId = GameDataManager.Instance.battleNodeId;
        
        if (!string.IsNullOrEmpty(currentNodeId))
        {
            Debug.Log($"完成节点: {currentNodeId}");
            
            // 2. 标记为已完成
            GameDataManager.Instance.CompleteNode(currentNodeId);
            
            // 3. 解锁下一个节点（假设线性顺序）
            UnlockNextNode(currentNodeId);
            
            // 4. 保存数据
            GameDataManager.Instance.SaveGameData();
            
            Debug.Log("节点状态已更新并保存");
        }
        else
        {
            Debug.LogWarning("当前战斗节点ID为空");
        }
    }
}
 private void OnVictoryContinue()
{
    // 恢复时间
    Time.timeScale = 1f;
    
    // 隐藏胜利面板
    if (victoryPanel != null) victoryPanel.SetActive(false);
    
    // 关键：更新节点状态并移动到下一关
    UpdateMapNodeAfterVictory();
    
    // 直接加载地图场景
    SceneManager.LoadScene("MapScene");
}


private void UpdateMapNodeAfterVictory()
{
    if (GameDataManager.Instance != null)
    {
        // 获取当前战斗的节点ID
        string battleNodeId = GameDataManager.Instance.battleNodeId;
        
        if (!string.IsNullOrEmpty(battleNodeId))
        {
            Debug.Log($"战斗胜利，处理节点 {battleNodeId} 完成");
            
            // 1. 标记节点完成
            GameDataManager.Instance.CompleteNode(battleNodeId);
            
            // 2. 解锁下一个节点并设置为当前节点
            // 这里需要根据你的地图结构，这里假设是线性关卡
            // 如果是分支结构，需要更复杂的逻辑
            
            // 简单线性逻辑：Node1 -> Node2 -> Node3
            string nextNodeId = GetNextNodeId(battleNodeId);
            
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                // 解锁下一个节点
                GameDataManager.Instance.UnlockNode(nextNodeId);
                
                // 设置下一个节点为当前节点
                GameDataManager.Instance.SetCurrentNode(nextNodeId);
                
                Debug.Log($"解锁并设置当前节点为: {nextNodeId}");
            }
            
            // 3. 保存数据
            GameDataManager.Instance.SaveGameData();
            
            // 4. 清除战斗数据
            GameDataManager.Instance.ClearBattleData();
        }
        else
        {
            Debug.LogWarning("当前战斗节点ID为空");
        }
    }
}
private string GetNextNodeId(string currentId)
{
    // 根据你的节点ID命名规则来获取下一个
    // 例如：Node1 -> Node2, Node2 -> Node3
    
    if (string.IsNullOrEmpty(currentId)) return "";
    
    // 提取数字部分
    string prefix = "Node";
    if (currentId.StartsWith(prefix))
    {
        string numberStr = currentId.Substring(prefix.Length);
        if (int.TryParse(numberStr, out int currentNumber))
        {
            int nextNumber = currentNumber + 1;
            return $"{prefix}{nextNumber}";
        }
    }
    
    // 如果不是标准格式，尝试其他逻辑
    // 例如：Boss1 -> Boss2 等
    
    return "";
}
    
    /// <summary>
    /// 失败后重新开始
    /// </summary>
    private void OnDefeatRestart()
{
    // 恢复时间
    Time.timeScale = 1f;
    
    // 隐藏失败面板
    if (defeatPanel != null) defeatPanel.SetActive(false);
    
    // 重新加载当前战斗场景
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void OnDefeatMainMenu()
{
    // 恢复时间
    Time.timeScale = 1f;
    
    // 隐藏失败面板
    if (defeatPanel != null) defeatPanel.SetActive(false);
    
    // 返回主菜单
    SceneManager.LoadScene("MainMenu"); // 确保你的主菜单场景叫这个名字
}
    
    /// <summary>
    /// 隐藏所有结算页面（用于重新开始等情况）
    /// </summary>
    public void HideAllResultPanels()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);
        
        // 确保时间恢复正常
        Time.timeScale = 1f;
    }

    #endregion

    #region 弹窗逻辑 (优化版)

    /// <summary>
    /// 显示回合/结果弹窗 - 简化版，更快消失
    /// </summary>
    public void ShowPopup(string message)
    {
        ShowPopup(message, popupShowDuration);
    }
    
    /// <summary>
    /// 显示回合/结果弹窗（可自定义时长）
    /// </summary>
    public void ShowPopup(string message, float duration)
    {
        if (turnPopup == null || popupText == null)
        {
            Debug.LogWarning("[GameFlowManager] 弹窗相关组件未赋值！");
            return;
        }

        // ⭐ 修复：如果当前有弹窗正在播，先强制停止旧的
        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
            // 立即隐藏弹窗
            if (popupAnimator != null)
            {
                popupAnimator.ResetTrigger(ShowHash);
                popupAnimator.ResetTrigger(HideHash);
            }
            turnPopup.SetActive(false);
        }
        
        popupText.text = message;
        currentPopupCoroutine = StartCoroutine(QuickPopupSequence(duration));
    }

    /// <summary>
    /// 快速弹窗序列 - 优化时间控制
    /// </summary>
    private IEnumerator QuickPopupSequence(float totalDuration)
    {
        // 计算停留时间：总时长减去入场和出场动画时间
        float stayDuration = Mathf.Max(0.1f, totalDuration - showAnimDuration - hideAnimDuration);
        
        // 1. 显示并播放入场动画
        turnPopup.SetActive(true);
        turnPopup.transform.SetAsLastSibling();
        
        if (popupAnimator != null && popupAnimator.runtimeAnimatorController != null)
        {
            // 重置动画状态
            popupAnimator.Rebind();
            popupAnimator.Update(0f);
            
            // 触发入场动画
            popupAnimator.SetTrigger(ShowHash);
            
            // 等待入场动画完成
            yield return new WaitForSeconds(showAnimDuration);
        }
        else
        {
            // 没有动画时的降级处理
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;
            yield return null;
        }

        // 2. 停留一段时间
        yield return new WaitForSeconds(stayDuration);

        // 3. 播放出场动画
        if (popupAnimator != null && popupAnimator.runtimeAnimatorController != null)
        {
            popupAnimator.SetTrigger(HideHash);
            
            // 等待出场动画完成
            yield return new WaitForSeconds(hideAnimDuration);
        }
        else
        {
            // 没有动画时的降级处理
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
        }

        // 4. 隐藏弹窗
        turnPopup.SetActive(false);
        currentPopupCoroutine = null;
    }

    /// <summary>
    /// 极速弹窗 - 适合回合切换提示
    /// </summary>
    public void ShowQuickTurnPopup(string message)
    {
        if (turnPopup == null || popupText == null)
        {
            Debug.LogWarning("[GameFlowManager] 弹窗相关组件未赋值！");
            return;
        }

        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
            turnPopup.SetActive(false);
        }
        
        popupText.text = message;
        currentPopupCoroutine = StartCoroutine(VeryQuickPopupSequence());
    }

    private IEnumerator VeryQuickPopupSequence()
    {
        // 极速模式：总时长0.8秒
        turnPopup.SetActive(true);
        turnPopup.transform.SetAsLastSibling();
        
        if (popupAnimator != null && popupAnimator.runtimeAnimatorController != null)
        {
            popupAnimator.Rebind();
            popupAnimator.Update(0f);
            popupAnimator.SetTrigger(ShowHash);
            yield return new WaitForSeconds(0.2f); // 快速入场
            yield return new WaitForSeconds(0.3f); // 短暂停留
            popupAnimator.SetTrigger(HideHash);
            yield return new WaitForSeconds(0.2f); // 快速出场
        }
        else
        {
            // 没有动画的极速模式
            if (popupCanvasGroup != null) 
            {
                popupCanvasGroup.alpha = 1f;
                yield return new WaitForSeconds(0.5f);
                popupCanvasGroup.alpha = 0f;
            }
        }
        
        turnPopup.SetActive(false);
        currentPopupCoroutine = null;
    }

    /// <summary>
    /// 备选方案：使用DOTween的快速弹窗效果
    /// </summary>
    public void ShowPopupWithTween(string message, float duration = 0.8f)
    {
        if (turnPopup == null || popupText == null)
        {
            Debug.LogWarning("[GameFlowManager] 弹窗相关组件未赋值！");
            return;
        }

        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
        }
        
        popupText.text = message;
        currentPopupCoroutine = StartCoroutine(QuickPopupWithTween(duration));
    }

    private IEnumerator QuickPopupWithTween(float d)
    {
        // 极速版本：总时长更短
        turnPopup.SetActive(true);
        turnPopup.transform.SetAsLastSibling();
        popupCanvasGroup.alpha = 0f;
        turnPopup.transform.localScale = Vector3.one * 0.8f;
        
        // 快速入场动画
        Sequence seq = DOTween.Sequence();
        seq.Append(popupCanvasGroup.DOFade(1f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(turnPopup.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack));
        seq.SetUpdate(true);
        
        yield return seq.WaitForCompletion();
        
        // 短暂停留
        yield return new WaitForSeconds(d * 0.5f);
        
        // 快速出场动画
        Sequence seqOut = DOTween.Sequence();
        seqOut.Append(popupCanvasGroup.DOFade(0f, 0.15f).SetEase(Ease.InQuad));
        seqOut.Join(turnPopup.transform.DOScale(Vector3.one * 0.8f, 0.15f).SetEase(Ease.InBack));
        seqOut.SetUpdate(true);
        seqOut.OnComplete(() => turnPopup.SetActive(false));
        
        yield return seqOut.WaitForCompletion();
        
        currentPopupCoroutine = null;
    }
  
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