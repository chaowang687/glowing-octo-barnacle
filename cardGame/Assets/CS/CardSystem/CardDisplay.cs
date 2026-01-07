using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; 
using UnityEngine.EventSystems; 
using System.Collections; 
using TMPro; 
using System;
using UnityEngine.Localization.Components; 

// ----------------------------------------------------------------------
// 修正后的 CardDisplay 组件 (已更新 TryPlayCard 的调用方式)
// ----------------------------------------------------------------------

/// <summary>
/// 卡牌的 UI 显示和交互控制组件。
/// 负责处理拖拽瞄准、打出判断和动画反馈。
/// 悬停/高亮逻辑已转移至 BattleManager 统一处理。
/// Card UI display and interaction control component.
/// Handles drag targeting, play logic, and animation feedback.
/// </summary>
public class CardDisplay : MonoBehaviour, 
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler,
    IPointerClickHandler
{
    [Header("Data")]
    private CardData cardData;
    private CharacterBase owner;
    
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI typeText; 
    public Image artworkImage;
    
    [Header("Localization")]
    public LocalizeStringEvent nameLocalizeEvent;
    public LocalizeStringEvent descriptionLocalizeEvent;
    
    private RectTransform rectTransform;

    [Header("Targeting")]
    private GameObject currentTargetObject = null; 
    private CharacterBase currentTargetCharacter = null;

    [Header("State Tracking")]
    private bool isDragging = false;
    private Vector3 originalLocalPosition; 
    private Quaternion originalLocalRotation;
    
    // --- Initialization ---
    private Vector2 dragOffset; // 解决 CS0103: dragOffset 不存在
    public bool IsDragging => isDragging; // 解决 CS1061: 供 BattleManager 访问
    public void Initialize(CardData data, CharacterBase characterOwner)
    {
        cardData = data;
        owner = characterOwner;
        
        if (BattleManager.Instance == null)
        {
            Debug.LogError("BattleManager instance does not exist. Cannot initialize card.");
            return;
        }

        // 成本和类型不需要本地化，直接设置
        if (costText != null) costText.text = data.energyCost.ToString();
        if (typeText != null) typeText.text = data.type.ToString();
        
        // 卡牌名称本地化
        if (nameLocalizeEvent != null)
        {
            // 检查是否使用了默认本地化键
            if (data.cardNameKey == "card_default_name" || data.cardNameKey == "card_default_description")
            {
                // 使用默认文本，不使用本地化
                if (nameText != null)
                {
                    nameText.text = data.cardName;
                }
            }
            else
            {
                // 使用本地化 - 卡牌专用表格
                nameLocalizeEvent.StringReference.TableReference = "Card";
                nameLocalizeEvent.StringReference.TableEntryReference = data.cardNameKey;
                
                // 确保OnUpdateString事件已配置
                if (nameLocalizeEvent.OnUpdateString.GetPersistentEventCount() == 0 && nameText != null)
                {
                    nameLocalizeEvent.OnUpdateString.AddListener(nameText.SetText);
                }
            }
        }
        else if (nameText != null)
        {
            // 备选方案：直接使用LocalizationManager获取本地化文本
            if (LocalizationManager.Instance != null && 
                data.cardNameKey != "card_default_name" && 
                data.cardNameKey != "card_default_description")
            {
                nameText.text = LocalizationManager.Instance.GetLocalizedString(data.cardNameKey);
            }
            else
            {
                nameText.text = data.cardName; // 回退到默认文本
            }
        }
        
        // 卡牌描述本地化
        if (descriptionLocalizeEvent != null)
        {
            // 检查是否使用了默认本地化键
            if (data.descriptionKey == "card_default_name" || data.descriptionKey == "card_default_description")
            {
                // 使用默认文本，不使用本地化
                if (descriptionText != null)
                {
                    descriptionText.text = data.description;
                }
            }
            else
            {
                // 使用本地化 - 卡牌专用表格
                descriptionLocalizeEvent.StringReference.TableReference = "Card";
                descriptionLocalizeEvent.StringReference.TableEntryReference = data.descriptionKey;
                
                // 确保OnUpdateString事件已配置
                if (descriptionLocalizeEvent.OnUpdateString.GetPersistentEventCount() == 0 && descriptionText != null)
                {
                    descriptionLocalizeEvent.OnUpdateString.AddListener(descriptionText.SetText);
                }
            }
        }
        else if (descriptionText != null)
        {
            // 备选方案：直接使用LocalizationManager获取本地化文本
            if (LocalizationManager.Instance != null && 
                data.descriptionKey != "card_default_name" && 
                data.descriptionKey != "card_default_description")
            {
                descriptionText.text = LocalizationManager.Instance.GetLocalizedString(data.descriptionKey);
            }
            else
            {
                descriptionText.text = data.description; // 回退到默认文本
            }
        }
        
        // 卡牌图片设置（不需要本地化）
        if (artworkImage != null)
        {
            if (data.artwork != null)
            {
                artworkImage.sprite = data.artwork;
                artworkImage.enabled = true;
            }
            else
            {
                artworkImage.enabled = false;
            }
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

    // --- 2. Drag Start Logic (IBeginDragHandler) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 计算鼠标点击位置与卡牌中心点的偏移
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
        rectTransform, eventData.position, eventData.pressEventCamera, out dragOffset);
        if (cardData == null || BattleManager.Instance == null || BattleManager.Instance.cardSystem == null) return;
        
        if (BattleManager.Instance.cardSystem.CurrentEnergy < cardData.energyCost)
        {
            return; // Insufficient energy, prevent drag
        }
        
        if (!BattleManager.Instance.cardSystem.CardNeedsSelectedTarget(cardData))
        {
            // Cards that play automatically should not be dragged for aiming.
            return; 
        }

        isDragging = true;
        // ⭐ 关键点 1：彻底杀死当前卡牌的所有动画，防止“拉扯”
        transform.DOKill(true);
        originalLocalPosition = rectTransform.localPosition;
        originalLocalRotation = rectTransform.localRotation;
        
        BattleManager.Instance.UnhighlightCard(this); 
        
        transform.SetAsLastSibling(); 
        
        DOTween.Kill(transform); 
        
        Debug.Log("--- Dragging started: Targeting activated ---");
    }

    // --- 3. Drag In Progress Logic (IDragHandler) ---
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (BattleManager.Instance == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localCursor
        ))
        {
            // 减去偏移量，卡牌就不会“瞬移”到鼠标中心了
            rectTransform.localPosition = localCursor - dragOffset;
        }

        rectTransform.localRotation = Quaternion.identity;
        float scale = (BattleVisualizer.Instance != null) ? BattleVisualizer.Instance.hoverScale : 1.2f;
        rectTransform.localScale = Vector3.one * scale;
        
        currentTargetObject = eventData.pointerCurrentRaycast.gameObject;
        CharacterBase hitCharacter = null;
        
        if (currentTargetObject != null)
        {
            hitCharacter = currentTargetObject.GetComponentInParent<CharacterBase>();
        }

        if (hitCharacter != currentTargetCharacter)
        {
            currentTargetCharacter = hitCharacter;
            
            bool isValid = currentTargetCharacter != null && BattleManager.Instance.IsValidTarget(cardData, currentTargetCharacter);
        }
    }

    // --- 4. 拖拽结束/放手逻辑 (IEndDragHandler) ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (BattleManager.Instance == null) return;

        isDragging = false;
        
        CharacterBase finalTarget = currentTargetCharacter;
        
        // ⭐ 修正：传入当前 CardDisplay 实例 (this) ⭐
        if (finalTarget != null && BattleManager.Instance.TryPlayCard(this, finalTarget))
        {
            DOTween.Kill(transform);
            Debug.Log($"Successfully played card: {cardData.cardName}, target: {finalTarget.characterName}");
        }
        else
        {
            Debug.Log($"Play failed or target invalid, returning card: {cardData.cardName}");
            ReturnToHand(originalLocalPosition, originalLocalRotation);
        }
        
        currentTargetCharacter = null;
        currentTargetObject = null;
    }
    
    // --- 5. 辅助方法：返回手牌 ---
    private void ReturnToHand(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
    {
        if (BattleManager.Instance == null) return;
        float returnDuration = (BattleVisualizer.Instance != null) ? BattleVisualizer.Instance.repositionDuration : 0.3f; 

        DOTween.Kill(transform);
        
        transform.DOLocalMove(targetLocalPosition, returnDuration).SetEase(Ease.OutBack);
        transform.DOLocalRotateQuaternion(targetLocalRotation, returnDuration);
        
        transform.DOScale(Vector3.one, returnDuration)
            .OnComplete(() =>
            {
                BattleManager.Instance.UpdateHandLayout(0f); 
            });
    }

    // --- 6. IPointerClickHandler: 自动打出 (无目标卡牌) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return; 
        
        if (cardData == null || BattleManager.Instance == null || BattleManager.Instance.cardSystem == null || CharacterManager.Instance == null) return;

        if (BattleManager.Instance.GetHighlightedCard() != this)
        {
            return;
        }

        bool needsExplicitTarget = BattleManager.Instance.cardSystem.CardNeedsSelectedTarget(cardData);

        if (!needsExplicitTarget)
        {
            CharacterBase player = CharacterManager.Instance.GetActiveHero();
            
            // ⭐ 修正：传入当前 CardDisplay 实例 (this) ⭐
            if (player != null && BattleManager.Instance.TryPlayCard(this, player))
            {
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
