using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    public TextMeshProUGUI goldRewardText;
    
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
    
    public void ShowRewardDisplay(int gold, string card, string relic)
    {
        if (rewardPanel != null)
        {
            if (goldRewardText != null) goldRewardText.text = $"+{gold} Gold";
            // TODO: Implement Card and Relic display logic here
            Debug.Log($"Victory Rewards: {gold} Gold, Card: {card}, Relic: {relic}");
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