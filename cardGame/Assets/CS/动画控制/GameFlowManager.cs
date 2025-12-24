using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ScavengingGame;
using System.Collections;
using System.Linq;
using TMPro;
using DG.Tweening;

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
    
    [Header("动画时长设置")]
    public float popupShowDuration = 0.8f;  // 弹窗显示总时长
    public float showAnimDuration = 2.2f;   // 入场动画时长
    public float hideAnimDuration = 0.6f;   // 出场动画时长
    
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
    }

    private void Start()
    {
        StartCoroutine(InitializeCharacterManager());
    }

    private IEnumerator InitializeCharacterManager()
    {
        yield return null; 
        characterManager = CharacterManager.Instance;
    }

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
            Debug.Log($"[UI动画] 显示: {popupText.text}");
            
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
            Debug.Log($"[UI动画] 隐藏");
            
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
        GameObject heroObj = Instantiate(heroPrefab, heroVisualContainer);
        CharacterBase heroScript = heroObj.GetComponent<CharacterBase>();
        if (heroScript != null)
        {
            characterManager.activeHero = heroScript;
            characterManager.RegisterHero(heroScript);
            heroObj.GetComponentInChildren<CharacterUIDisplay>(true)?.Initialize(heroScript);
        }
    }

    private void SpawnEnemy(EnemyData data, int index)
    {
        if (enemyPrefab == null || enemyVisualContainer == null) return;
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