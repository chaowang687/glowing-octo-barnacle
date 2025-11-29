using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; 
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// 继承 UI 事件接口，用于处理鼠标交互
public class CardDisplay : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler, 
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Data")]
    private CardData cardData;
    private CharacterBase owner;
    
    [Header("UI References")]
    public Text nameText;
    public Text costText;
    public Text descriptionText;
    private RectTransform rectTransform;

    [Header("Targeting")]
    // 目标追踪
    private GameObject currentTargetObject = null; 
    private CharacterBase currentTargetCharacter = null;

    // 状态追踪
    private bool isDragging = false;
    private Vector3 originalLocalPosition; 
    private Quaternion originalLocalRotation;
    
    // --- 初始化 ---
    public void Initialize(CardData data, CharacterBase characterOwner)
    {
        cardData = data;
        owner = characterOwner;
        
        if (BattleManager.Instance == null)
        {
            Debug.LogError("BattleManager instance does not exist. Cannot initialize card.");
            return;
        }

        // 尝试更新 UI 文本
        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        
        // 假设卡牌描述是基于第一个 Action 的值
        if (descriptionText != null && data.actions.Count > 0)
        {
            var action = data.actions[0];
            descriptionText.text = $"{action.effectType.ToString()} {action.value} to {action.targetType.ToString()}";
        }
    }
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("CardDisplay requires a RectTransform component.");
        }
    }

    // --- 1. 悬停/高亮逻辑 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        BattleManager.Instance?.HighlightCard(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        BattleManager.Instance?.UnhighlightCard(this);
    }
    
    // --- 2. 拖拽开始逻辑 (IBeginDragHandler) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardData == null || BattleManager.Instance == null || BattleManager.Instance.cardSystem == null) return;
        
        if (BattleManager.Instance.cardSystem.CurrentEnergy < cardData.energyCost)
        {
            return; // 能量不足，阻止拖拽
        }
        
        // 检查卡牌是否需要目标
        if (!BattleManager.Instance.cardSystem.CardNeedsSelectedTarget(cardData))
        {
            return; // 不需要明确目标，不启动拖拽瞄准
        }

        isDragging = true;
        originalLocalPosition = rectTransform.localPosition;
        originalLocalRotation = rectTransform.localRotation;
        
        // 1. 立即取消 BattleManager 中的高亮状态，将卡牌强制放回布局位置
        BattleManager.Instance.UnhighlightCard(this); 
        
        // 2. 提升卡牌的渲染层级
        transform.SetAsLastSibling(); 
        
        // 3. 停止 DOTween 动画
        DOTween.Kill(transform); 
        
        Debug.Log("--- Dragging started: Targeting activated (No arrow visual) ---");
    }

    // --- 3. 拖拽进行中逻辑 (IDragHandler) ---
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (BattleManager.Instance == null) return;

        // 保持卡牌在手牌布局位置，但执行放大和去旋转
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one * BattleManager.Instance.hoverScale;
        
        // 检查鼠标悬停在哪个 CharacterBase 目标上
        currentTargetObject = eventData.pointerCurrentRaycast.gameObject;
        CharacterBase hitCharacter = null;
        
        if (currentTargetObject != null)
        {
            // 尝试在鼠标射线击中的对象及其父级中查找 CharacterBase
            hitCharacter = currentTargetObject.GetComponentInParent<CharacterBase>();
        }

        // 仅当目标发生变化时才更新状态
        if (hitCharacter != currentTargetCharacter)
        {
            currentTargetCharacter = hitCharacter;
            
            // 检查新目标是否有效
            bool isValid = currentTargetCharacter != null && BattleManager.Instance.IsValidTarget(cardData, currentTargetCharacter);
            
            // ⭐ 可选：根据 isValid 状态高亮或取消高亮目标角色 ⭐
            // 此时需要依赖 CharacterBase 上的高亮/取消高亮方法
        }
    }

    // --- 4. 拖拽结束/放手逻辑 (IEndDragHandler) ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (BattleManager.Instance == null) return;

        isDragging = false;
        
        // 1. 使用 OnDrag 中追踪的最终目标
        CharacterBase finalTarget = currentTargetCharacter;
        
        // 2. 尝试打出卡牌
        if (finalTarget != null && BattleManager.Instance.TryPlayCard(cardData, finalTarget, gameObject))
        {
            // TryPlayCard 成功：BattleManager 接管卡牌的动画和销毁
            DOTween.Kill(transform);
            
            Debug.Log($"Successfully played card: {cardData.cardName}, target: {finalTarget.characterName}");
        }
        else
        {
            // 失败：目标无效或能量不足
            Debug.Log($"Play failed or target invalid, returning card: {cardData.cardName}");
            
            // 动画卡牌返回手牌布局
            ReturnToHand(originalLocalPosition, originalLocalRotation);
        }

        currentTargetCharacter = null;
        currentTargetObject = null;
    }
    
    // --- 5. 辅助方法：返回手牌 ---
    private void ReturnToHand(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
    {
        if (BattleManager.Instance == null) return;
        float returnDuration = BattleManager.Instance.repositionDuration; 

        transform.DOLocalMove(targetLocalPosition, returnDuration).SetEase(Ease.OutBack);
        transform.DOLocalRotateQuaternion(targetLocalRotation, returnDuration);
        
        transform.DOScale(Vector3.one, returnDuration)
            .OnComplete(() =>
            {
                // 动画完成后，通知 BattleManager 重新布局
                BattleManager.Instance.UpdateHandLayout(true); 
            });
    }

    // --- 6. IPointerClickHandler: 自动打出 (无目标卡牌) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardData == null || BattleManager.Instance == null || BattleManager.Instance.cardSystem == null || CharacterManager.Instance == null) return;

        // 检查卡牌是否需要明确的 Selected 目标
        bool needsExplicitTarget = BattleManager.Instance.cardSystem.CardNeedsSelectedTarget(cardData);

        if (!needsExplicitTarget)
        {
            // ⭐ 修正: 对于无目标卡牌，我们使用主角作为默认目标 (即便 effect 可能不作用于主角) ⭐
            // BattleManager.Instance.activeHero 已经被移除，应使用 CharacterManager.Instance.GetActiveHero()
            CharacterBase player = CharacterManager.Instance.GetActiveHero();
            
            if (player != null && BattleManager.Instance.TryPlayCard(cardData, player, gameObject))
            {
                // 自动播放成功
                Debug.Log($"Automatically playing card (No target required): {cardData.cardName}");
            }
        }
    }
    
    public CardData GetCardData() => cardData;
}