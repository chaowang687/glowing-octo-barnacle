using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>
/// 战斗场景 UI 管理器：负责所有 UI 弹窗、面板和动画的控制。
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    [Header("回合弹窗设置")]
    public GameObject turnPopup; 
    public TextMeshProUGUI popupText;
    public Animator popupAnimator; 
    public float popupShowDuration = 0.8f;
    public float showAnimDuration = 0.3f;
    public float hideAnimDuration = 0.3f;
    
    [Header("结算页面设置")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI defeatTitleText;
    public Button victoryContinueButton;
    public Button defeatRestartButton;
    public Button defeatMainMenuButton;
    
    [Header("胜利奖励设置")]
    public GameObject rewardPanel;
    public Image rewardBackground; // 新增：用于遮罩的背景图片
    public TextMeshProUGUI goldRewardText;
    public Transform rewardCardContainer; // 补回变量
    public GameObject cardPrefab;         // 补回变量
    public Button rewardContinueButton;   // 新增：奖励页面的继续按钮

    [Header("Fly Animation")]
    public RectTransform flyTargetPosition; // 手动指定位置
    public float flyDuration = 0.5f;
    public Ease flyEase = Ease.InBack;
    
    private CanvasGroup popupCanvasGroup; 
    private Coroutine currentPopupCoroutine;
    private static readonly int ShowHash = Animator.StringToHash("Show");
    private static readonly int HideHash = Animator.StringToHash("Hide");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        
        // 尝试自动查找引用（如果 Inspector 中未赋值）
        if (turnPopup == null) turnPopup = GameObject.Find("TurnPopup"); // 假设场景里叫这个名字
        if (victoryPanel == null) victoryPanel = GameObject.Find("VictoryPanel");
        if (defeatPanel == null) defeatPanel = GameObject.Find("DefeatPanel");
        // ... 其他引用查找可以根据实际 prefab 名字添加
        
        InitializePopupComponents();
    }

    private void InitializePopupComponents()
    {
        if (turnPopup != null)
        {
            if (popupAnimator == null) popupAnimator = turnPopup.GetComponent<Animator>();
            popupCanvasGroup = turnPopup.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null) popupCanvasGroup = turnPopup.AddComponent<CanvasGroup>();
            turnPopup.SetActive(false);
        }
    }

    private void Start()
    {
        // 将按钮事件绑定移至 Start，确保 GameFlowManager.Instance 已准备好
        InitializeResultPanels();
    }
    
    // ...

    private void InitializeResultPanels()
    {
        HideAllResultPanels();
        
        // 增加空值检查，防止报错
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("BattleUIManager: GameFlowManager.Instance 为空！按钮事件无法绑定。");
            return;
        }

        if (victoryContinueButton != null)
        {
            victoryContinueButton.onClick.RemoveAllListeners();
            victoryContinueButton.onClick.AddListener(() => GameFlowManager.Instance.OnVictoryContinue());
        }
        
        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.RemoveAllListeners();
            defeatRestartButton.onClick.AddListener(() => GameFlowManager.Instance.OnDefeatRestart());
        }
        
        if (defeatMainMenuButton != null)
        {
            defeatMainMenuButton.onClick.RemoveAllListeners();
            defeatMainMenuButton.onClick.AddListener(() => GameFlowManager.Instance.OnDefeatMainMenu());
        }
    }

    public void HideAllResultPanels()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);
    }

    #region 弹窗逻辑
    
    public void ShowPopup(string message, float duration)
    {
        if (turnPopup == null || popupText == null) return;

        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
            if (popupAnimator != null)
            {
                popupAnimator.ResetTrigger(ShowHash);
                popupAnimator.ResetTrigger(HideHash);
            }
            turnPopup.SetActive(false);
        }
        
        popupText.text = message;
        currentPopupCoroutine = StartCoroutine(PopupSequence(duration));
    }

    public void ShowPopup(string message)
    {
        ShowPopup(message, popupShowDuration);
    }

    public void ShowQuickTurnPopup(string message)
    {
        // 增加默认显示时长到 1.2 秒
        ShowPopup(message, 1.2f);
    }

    private IEnumerator PopupSequence(float totalDuration)
    {
        float stayDuration = Mathf.Max(0.1f, totalDuration - showAnimDuration - hideAnimDuration);
        
        turnPopup.SetActive(true);
        turnPopup.transform.SetAsLastSibling();
        
        if (popupAnimator != null && popupAnimator.runtimeAnimatorController != null)
        {
            popupAnimator.Rebind();
            popupAnimator.Update(0f);
            popupAnimator.SetTrigger(ShowHash);
            yield return new WaitForSeconds(showAnimDuration);
        }
        else
        {
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(stayDuration);

        if (popupAnimator != null && popupAnimator.runtimeAnimatorController != null)
        {
            popupAnimator.SetTrigger(HideHash);
            yield return new WaitForSeconds(hideAnimDuration);
        }
        else
        {
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
        }

        turnPopup.SetActive(false);
        currentPopupCoroutine = null;
    }
    #endregion

    #region 结算面板逻辑

    public void ShowVictoryPanel(string title = "Victory!")
    {
        StartCoroutine(VictorySequence(title));
    }

    private IEnumerator VictorySequence(string title)
    {
        yield return new WaitForSeconds(1f);
        
        if (victoryTitleText != null) victoryTitleText.text = title;
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            AnimatePanelShow(victoryPanel);
        }
        
        // 恢复为旧逻辑：等待动画播放完毕后再暂停时间，这是最稳妥的
        yield return new WaitForSeconds(0.6f); 
        Time.timeScale = 0f;
    }
    
    // 新增：悬停/取消悬停逻辑
    private void SetupRewardCardInteraction(GameObject cardObj, CardDisplay display)
    {
        // 1. 添加 EventTrigger 用于悬停效果
        UnityEngine.EventSystems.EventTrigger trigger = cardObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        
        // 确保有一个 Canvas 组件用于 Sort Order 控制 (解决闪烁问题的关键)
        Canvas canvas = cardObj.GetComponent<Canvas>();
        if (canvas == null) canvas = cardObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        
        // VITAL FIX: 获取父 Canvas 的 Order 并在此基础上增加
        // 这样可以保证子 Canvas 一定在父 Canvas 之上，避免被背景 Image 遮挡
        int baseOrder = 0;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null) baseOrder = parentCanvas.sortingOrder;
        
        // 默认 layer = 父 Order + 1
        canvas.sortingOrder = baseOrder + 1; 
        
        // 还需要 GraphicRaycaster 才能接收点击
        if (cardObj.GetComponent<GraphicRaycaster>() == null) cardObj.AddComponent<GraphicRaycaster>();

        // 悬停进入
        UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => {
            // 放大 (修改为 1.2 倍)
            cardObj.transform.DOScale(Vector3.one * 1.2f, 0.2f).SetEase(Ease.OutBack);
            // 悬停时 = 父 Order + 100
            canvas.sortingOrder = baseOrder + 100;
        });
        trigger.triggers.Add(entryEnter);

        // 悬停离开
        UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => {
            // 恢复
            cardObj.transform.DOScale(Vector3.one, 0.2f);
            canvas.sortingOrder = baseOrder + 1;
        });
        trigger.triggers.Add(entryExit);
    }

    public void ShowRewardDisplay(int gold, List<CardData> cardOptions, System.Action<CardData> onCardSelected)
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            AnimatePanelShow(rewardPanel);
            
            // 确保背景遮罩完全不透明，防止露出底图
            if (rewardBackground != null)
            {
                // 1. 确保物体被激活
                rewardBackground.gameObject.SetActive(true);
                // 2. 确保 Image 组件被启用
                rewardBackground.enabled = true;
                // 3. 强制设为纯黑色，完全不透明
                rewardBackground.color = new Color(0, 0, 0, 1f);
                
                // 4. 如果 Background 是 Panel 的子物体且 Panel 有 CanvasGroup，
                // 那么 CanvasGroup 的 Alpha 会影响它。
                // 如果想要背景立即变黑而不受淡入动画影响（或者避免半透明阶段），
                // 可以在这里特殊处理，但通常跟随 Panel 淡入是可以接受的。
                // 只要最终 Alpha 为 1 即可。
                
                // Debug 帮助定位问题
                Debug.Log($"[BattleUIManager] Reward Background Activated. Color: {rewardBackground.color}, Active: {rewardBackground.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning("[BattleUIManager] Reward Background is NOT assigned in Inspector!");
            }
            
            // 初始化时隐藏继续按钮，确保玩家先选卡
            if (rewardContinueButton != null) 
            {
                rewardContinueButton.gameObject.SetActive(false);
                rewardContinueButton.onClick.RemoveAllListeners();
                rewardContinueButton.onClick.AddListener(() => {
                    // 点击继续按钮，执行原本胜利后的跳转逻辑
                    GameFlowManager.Instance.OnVictoryContinue();
                });
            }
            
            if (goldRewardText != null) goldRewardText.text = $"+{gold} Gold";

            // Fallback: If container is null, try to find one or create one dynamically
            if (rewardCardContainer == null)
            {
                 Transform found = rewardPanel.transform.Find("CardContainer");
                 if (found != null) 
                 {
                     rewardCardContainer = found;
                 }
                 else
                 {
                     GameObject containerObj = new GameObject("CardContainer");
                     containerObj.transform.SetParent(rewardPanel.transform, false);
                     rewardCardContainer = containerObj.transform;
                     
                     HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
                     layout.childAlignment = TextAnchor.MiddleCenter;
                     layout.spacing = 50;
                     layout.childControlWidth = false;
                     layout.childControlHeight = false;
                     
                     RectTransform rt = containerObj.GetComponent<RectTransform>();
                     rt.anchorMin = new Vector2(0.1f, 0.2f);
                     rt.anchorMax = new Vector2(0.9f, 0.8f);
                     rt.offsetMin = Vector2.zero;
                     rt.offsetMax = Vector2.zero;
                 }
            }

            // 清理旧的选项
            foreach (Transform child in rewardCardContainer)
            {
                Destroy(child.gameObject);
            }

            // 生成新的卡牌选项
            if (cardOptions != null && cardPrefab != null)
                {
                    foreach (CardData cardData in cardOptions)
                    {
                        GameObject cardObj = Instantiate(cardPrefab, rewardCardContainer);
                        
                        // 初始化卡牌显示
                        CardDisplay display = cardObj.GetComponent<CardDisplay>();
                        if (display != null)
                        {
                            // 借用 BattleManager 的引用来初始化，或者修改 Initialize 不依赖 BattleManager
                            // 这里假设 BattleManager 存在，因为是战斗结束
                            // 注意：我们需要 CharacterOwner，这里可以传 null 或玩家英雄
                            CharacterBase hero = (CharacterManager.Instance != null) ? CharacterManager.Instance.GetActiveHero() : null;
                            display.Initialize(cardData, hero);
                            
                            // 禁用战斗交互逻辑
                            display.enabled = false;
                        }

                        // 配置悬停交互
                        SetupRewardCardInteraction(cardObj, display);

                        // 添加点击选择逻辑
                        Button btn = cardObj.GetComponent<Button>();
                        if (btn == null) btn = cardObj.AddComponent<Button>();
                        
                        btn.onClick.AddListener(() => {
                            Debug.Log($"[BattleUIManager] CLICKED reward card: {cardData.cardName}. Executing Save Callback...");
                            
                            // 禁用按钮防止连点，禁用所有卡牌交互
                            foreach(Transform child in rewardCardContainer) {
                                Button b = child.GetComponent<Button>();
                                if(b) b.interactable = false;
                                // 同时禁用 EventTrigger 以防悬停效果干扰
                                UnityEngine.EventSystems.EventTrigger et = child.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                                if(et) et.enabled = false;
                            }
                            
                            // 核心修复：立即执行回调保存数据，不再等待动画完成
                            // 这样可以防止动画中断或时间暂停导致数据丢失
                            try 
                            {
                                onCardSelected?.Invoke(cardData);
                                Debug.Log("[BattleUIManager] Save Callback Executed Successfully.");
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[BattleUIManager] Error executing save callback: {ex.Message}");
                            }
                            
                            // 飞向牌库动画
                            Vector3 targetPos;
                            if (flyTargetPosition != null)
                            {
                                targetPos = flyTargetPosition.position;
                            }
                            else
                            {
                                // 默认右下角
                                targetPos = new Vector3(Screen.width * 0.9f, Screen.height * 0.1f, 0);
                            }
                            
                            // 先Kill掉之前的Scale动画
                            cardObj.transform.DOKill();
                            
                            Sequence seq = DOTween.Sequence();
                            // 确保动画不受 TimeScale 影响，防止 VictoryPanel 暂停时间导致动画卡住
                            seq.SetUpdate(true);

                            // 关键：将卡牌 Canvas 提升到最高层，防止飞行过程中被其他 UI 遮挡
                            Canvas c = cardObj.GetComponent<Canvas>();
                            if (c != null) c.sortingOrder = 9999;
                            
                            // 1. 稍微放大确认 (使用配置的放大倍数或默认稍大一点)
                            seq.Append(cardObj.transform.DOScale(Vector3.one * 1.5f, 0.2f));
                            // 2. 飞向目标并缩小 (使用配置的参数)
                            seq.Append(cardObj.transform.DOMove(targetPos, flyDuration).SetEase(flyEase));
                            seq.Join(cardObj.transform.DOScale(0f, flyDuration));
                            
                            seq.OnComplete(() => {
                                // onCardSelected?.Invoke(cardData); // 已移至点击时立即触发
                                
                                // 不关闭面板，显示继续按钮
                                // rewardPanel.SetActive(false); 
                                if (rewardContinueButton != null) 
                                {
                                    rewardContinueButton.gameObject.SetActive(true);
                                    // 简单的出现动画
                                    rewardContinueButton.transform.localScale = Vector3.zero;
                                    rewardContinueButton.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                                }
                                
                                // 可选：隐藏或销毁其他没被选中的卡牌
                                foreach(Transform child in rewardCardContainer) {
                                    if(child.gameObject != cardObj) {
                                        child.DOScale(0f, 0.3f);
                                    }
                                }
                            });
                        });
                        
                        // 确保缩放正常 (CardDisplay 可能修改了缩放)
                        cardObj.transform.localScale = Vector3.one; 
                        
                        // 移除出现动画
                        // cardObj.transform.localScale = Vector3.zero;
                        // cardObj.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.1f);
                    }
                }
        }
    }

    public void ShowDefeatPanel(string title = "Defeat")
    {
        StartCoroutine(DefeatSequence(title));
    }

    private IEnumerator DefeatSequence(string title)
    {
        yield return new WaitForSeconds(1f);
        
        if (defeatTitleText != null) defeatTitleText.text = title;
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            AnimatePanelShow(defeatPanel);
        }
        
        // 恢复为旧逻辑：等待动画播放完毕后再暂停时间
        yield return new WaitForSeconds(0.6f);
        Time.timeScale = 0f;
    }

    private void AnimatePanelShow(GameObject panel)
    {
        // 确保面板能够接收点击事件
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true; // 强制开启射线检测
        cg.interactable = true;

        Animator animator = panel.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Show");
            // 保持 UnscaledTime 以防万一，但主要依靠协程等待
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        else
        {
            cg.alpha = 0f;
            cg.DOFade(1f, 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }
    #endregion
}