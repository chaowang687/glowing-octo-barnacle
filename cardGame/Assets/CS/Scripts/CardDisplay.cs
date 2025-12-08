using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; 
using System.Collections.Generic;
using System.Linq;
using TMPro; // 使用 TextMeshPro
using UnityEngine.UI; // 用于 Image 组件

/// <summary>
/// 卡牌的 UI 显示和交互控制组件。
/// 负责处理拖拽瞄准、打出判断和动画反馈。
/// 悬停/高亮逻辑已转移至 BattleManager 统一处理。
/// </summary>
public class CardDisplay : MonoBehaviour, 
    // IPointerEnterHandler, <-- 移除，悬停判定由 BattleManager 接管
    // IPointerExitHandler, <-- 移除，悬停判定由 BattleManager 接管
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Data")]
    private CardData cardData;
    private CharacterBase owner;
    
    [Header("UI References")]
    // ⭐ 使用 TextMeshProUGUI 来显示文本 ⭐
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI typeText; // 新增：显示卡牌类型
    public Image artworkImage;        // ⭐ 新增：显示卡牌插画 ⭐
    
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
    /// <summary>
    /// 将卡牌数据绑定到 UI 元素。
    /// </summary>
    public void Initialize(CardData data, CharacterBase characterOwner)
    {
        cardData = data;
        owner = characterOwner;
        
        if (BattleManager.Instance == null)
        {
            Debug.LogError("BattleManager instance does not exist. Cannot initialize card.");
            return;
        }

        // 绑定文本信息
        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        // 使用 CardData 中新的 description 字段
        if (descriptionText != null) descriptionText.text = data.description; 
        if (typeText != null) typeText.text = data.type.ToString();

        // ⭐ 绑定卡牌插画 ⭐
        if (artworkImage != null)
        {
            if (data.artwork != null)
            {
                artworkImage.sprite = data.artwork;
                artworkImage.enabled = true;
            }
            else
            {
                // 如果没有配置图片，可以隐藏 Image 组件或使用默认图
                artworkImage.enabled = false;
            }
        }
    }
    public void OnClickOrRelease()
{
    // ...
    BattleManager.Instance.TryPlayCard(
        this.cardData, 
        null, // ⭐ 传入 null，让 BattleManager 自动查找目标 ⭐
        this.gameObject
    );
}
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("CardDisplay requires a RectTransform component.");
        }
    }

    // --- 1. 悬停/高亮逻辑 (已移除 OnPointerEnter/Exit，由 BattleManager 接管) ---
    // 卡牌自身不再处理悬停事件，避免抖动。
    
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
            // 自动打出的卡牌不应被拖拽瞄准，除非需要拖拽到打出区域
            return; 
        }

        isDragging = true;
        originalLocalPosition = rectTransform.localPosition;
        originalLocalRotation = rectTransform.localRotation;
        
        // 1. 立即通知 BattleManager 取消高亮状态 (避免卡牌在拖拽时仍被高亮布局影响)
        BattleManager.Instance.UnhighlightCard(this); 
        
        // 2. 提升卡牌的渲染层级
        transform.SetAsLastSibling(); 
        
        // 3. 停止 DOTween 动画
        DOTween.Kill(transform); 
        
        Debug.Log("--- Dragging started: Targeting activated ---");
    }

    // --- 3. 拖拽进行中逻辑 (IDragHandler) ---
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (BattleManager.Instance == null) return;

        // 核心修正：将卡牌移动到鼠标位置，实现真正的拖拽
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        ))
        {
            rectTransform.localPosition = localPoint;
        }

        // 保持卡牌正对屏幕且放大
        rectTransform.localRotation = Quaternion.identity;
        float scale = (BattleManager.Instance.hoverScale > 0) ? BattleManager.Instance.hoverScale : 1.2f;
        rectTransform.localScale = Vector3.one * scale;
        
        // 检查鼠标悬停在哪个 CharacterBase 目标上
        currentTargetObject = eventData.pointerCurrentRaycast.gameObject;
        CharacterBase hitCharacter = null;
        
        if (currentTargetObject != null)
        {
            hitCharacter = currentTargetObject.GetComponentInParent<CharacterBase>();
        }

        // 仅当目标发生变化时才更新状态
        if (hitCharacter != currentTargetCharacter)
        {
            // 取消旧目标的高亮 (假设 CharacterBase 有 Unhighlight 方法)
            // if (currentTargetCharacter != null) currentTargetCharacter.Unhighlight(); 
            
            currentTargetCharacter = hitCharacter;
            
            // 检查新目标是否有效
            bool isValid = currentTargetCharacter != null && BattleManager.Instance.IsValidTarget(cardData, currentTargetCharacter);
            
            // 高亮新目标 (假设 CharacterBase 有 Highlight 方法)
            // if (isValid) currentTargetCharacter.Highlight(isValid); 
        }
    }

    // --- 4. 拖拽结束/放手逻辑 (IEndDragHandler) ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (BattleManager.Instance == null) return;

        isDragging = false;
        
        CharacterBase finalTarget = currentTargetCharacter;
        
        // 尝试打出卡牌
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

        // 无论成功与否，都要取消当前目标的高亮
        // if (currentTargetCharacter != null) currentTargetCharacter.Unhighlight(); 
        
        currentTargetCharacter = null;
        currentTargetObject = null;
    }
    
    // --- 5. 辅助方法：返回手牌 ---
    private void ReturnToHand(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
    {
        if (BattleManager.Instance == null) return;
        float returnDuration = BattleManager.Instance.repositionDuration; 

        DOTween.Kill(transform);
        
        transform.DOLocalMove(targetLocalPosition, returnDuration).SetEase(Ease.OutBack);
        transform.DOLocalRotateQuaternion(targetLocalRotation, returnDuration);
        
        transform.DOScale(Vector3.one, returnDuration)
            .OnComplete(() =>
            {
                // 动画完成后，通知 BattleManager 重新布局，恢复所有卡牌的正确状态
                BattleManager.Instance.UpdateHandLayout(true); 
            });
    }

    // --- 6. IPointerClickHandler: 自动打出 (无目标卡牌) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return; 
        
        if (cardData == null || BattleManager.Instance == null || BattleManager.Instance.cardSystem == null || CharacterManager.Instance == null) return;

        // ⭐ 只有当当前卡牌是 BattleManager 确定的高亮卡牌时，点击才有效 ⭐
        if (BattleManager.Instance.GetHighlightedCard() != this)
        {
            return;
        }

        // 检查卡牌是否需要明确的 Selected 目标
        bool needsExplicitTarget = BattleManager.Instance.cardSystem.CardNeedsSelectedTarget(cardData);

        if (!needsExplicitTarget)
        {
            // 对于无目标卡牌，使用主角作为默认目标
            CharacterBase player = CharacterManager.Instance.GetActiveHero();
            
            if (player != null && BattleManager.Instance.TryPlayCard(cardData, player, gameObject))
            {
                // 自动播放成功
                Debug.Log($"Automatically playing card (No target required): {cardData.cardName}");
            }
            else
            {
                 Debug.Log($"Automatic play failed for card: {cardData.cardName}. Energy or other requirements not met.");
            }
        }
    }
    
    public CardData GetCardData() => cardData;
}