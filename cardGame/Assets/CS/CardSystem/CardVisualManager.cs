using UnityEngine;
using System.Collections;
using DG.Tweening; // Re-introduced DOTween dependency
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages all visual aspects of the card hand: layout geometry calculation, 
/// card animations (fly out, return, discard). Requires DOTween.
/// 管理所有卡牌的视觉方面：布局几何计算、卡牌动画（飞出、返回、弃牌）。需要 DOTween。
/// </summary>
public class CardVisualManager : MonoBehaviour
{
    [Header("Layout Settings")]
    [Tooltip("The container RectTransform for the cards in hand.")]
    public RectTransform handContainer; 

    [Tooltip("The maximum width the cards can spread across.")]
    public float maxHandWidth = 1000f; 

    [Tooltip("The vertical offset for the cards in the center of the arc.")]
    public float arcHeight = 100f; 

    [Tooltip("The rotation angle limit for the outermost cards (e.g., 10 degrees).")]
    public float maxRotationAngle = 10f; 

    [Header("Animation Settings")]
    public float repositionDuration = 0.3f; // Duration for layout rearrangement
    public float playDuration = 0.5f;       // Duration for card flying to play zone
    public float flyUpYOffset = 150f;       // Y offset for the card flying up before moving to target

    [Header("Play Zone Targets")]
    public Transform playZoneTarget;        // Target location for the card on the field
    public Transform discardZoneTarget;     // Target location for the card to fly to after effect

    void Awake()
    {
        if (handContainer == null)
        {
            Debug.LogError("Hand Container is not assigned in CardVisualManager.");
        }
    }

    /// <summary>
    /// Adds the new card to the hand container, ready for layout.
    /// 将新卡牌添加到手牌容器中，准备进行布局。
    /// </summary>
    public void AddCardToHand(Transform newCardTransform)
    {
        // Purely visual setup
        newCardTransform.SetParent(handContainer, false); 
        newCardTransform.SetAsLastSibling(); 
    }

    /// <summary>
    /// Calculates new positions and rotations for all cards in the hand using an arc shape
    /// and applies them using DOTween. (Original complex layout geometry code is here)
    /// 计算手牌中所有卡牌的弧形布局（位置和旋转），并应用 DOTween 动画。
    /// </summary>
    public void UpdateHandLayout(float duration)
    {
        if (handContainer == null) return;

        // Get all active card RectTransforms
        var cardTransforms = handContainer.GetComponentsInChildren<RectTransform>()
            .Where(rt => rt.gameObject != handContainer.gameObject && rt.gameObject.activeInHierarchy)
            .Select(rt => rt as Transform)
            .ToList();

        int cardCount = cardTransforms.Count;
        if (cardCount == 0) return;

        // --- Complex Layout Calculation Geometry ---
        float currentWidth = Mathf.Min(maxHandWidth, handContainer.rect.width * 0.9f);
        float totalRotation = maxRotationAngle * 2;
        float rotationStep = cardCount > 1 ? totalRotation / (cardCount - 1) : 0;
        float startRotation = -maxRotationAngle;

        for (int i = 0; i < cardCount; i++)
        {
            Transform card = cardTransforms[i];
            float normalizedPosition = cardCount > 1 ? (float)i / (cardCount - 1) : 0.5f;
            
            float posX = normalizedPosition * currentWidth - (currentWidth / 2f);
            
            // Parabola calculation
            float t = (posX / (currentWidth / 2f)); 
            float posY = arcHeight * (1f - t * t);
            
            float rotZ = startRotation + (i * rotationStep);

            // --- DOTween Animation Application ---
            card.DOKill(true); 
            card.DOLocalMove(new Vector3(posX, posY, 0f), duration).SetEase(Ease.OutQuad);
            card.DOLocalRotate(new Vector3(0f, 0f, rotZ), duration).SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// Animates a card back to its calculated position in the hand on drag cancellation.
    /// 卡牌返回手牌位置的动画（用于拖拽取消）。
    /// </summary>
    public void ReturnCardToHand(Transform cardTransform, Vector3 originalLocalPosition, Quaternion originalLocalRotation)
    {
        cardTransform.DOKill(true); 

        // DOTween Animation
        cardTransform.DOLocalMove(originalLocalPosition, repositionDuration).SetEase(Ease.OutQuad);
        cardTransform.DOLocalRotate(originalLocalRotation.eulerAngles, repositionDuration).SetEase(Ease.OutQuad);
        
        // Trigger a layout update after the card returns
        UpdateHandLayout(repositionDuration * 2); 
    }

    /// <summary>
    /// The master animation sequence for a card being played.
    /// 卡牌打出时的主动画序列。
    /// </summary>
    public IEnumerator PlayCardSequence(GameObject cardObject, Action onLogicExecute, Action onComplete)
    {
        if (cardObject == null || playZoneTarget == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.SetAsLastSibling(); 

        // --- Complex DOTween Sequence Construction ---
        Sequence sequence = DOTween.Sequence();

        // 1. Fly up to detach from hand
        sequence.Append(
            cardRect.DOLocalMoveY(cardRect.localPosition.y + flyUpYOffset, playDuration * 0.2f).SetEase(Ease.OutSine)
        );
        
        // 2. Fly to the play zone target
        sequence.Append(
            cardRect.DOMove(playZoneTarget.position, playDuration * 0.6f)
                .SetEase(Ease.InOutSine)
        );
        
        // 3. Simultaneously straighten rotation
        sequence.Join(
            cardRect.DORotate(Vector3.zero, playDuration * 0.6f).SetEase(Ease.InOutSine)
        );

        // 4. Insert logic execution callback (triggers BattleManager logic mid-animation)
        sequence.InsertCallback(sequence.Duration() * 0.5f, () => 
        {
            onLogicExecute?.Invoke();
        });

        yield return sequence.WaitForCompletion();

        // 5. Short delay before flying to discard (to show effect finish)
        yield return new WaitForSeconds(0.1f);
        
        // 6. Fly to discard pile
        yield return DiscardCardSequence(cardObject);

        // 7. Sequence termination
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Animates the card to the discard pile target point and destroys it.
    /// 将卡牌动画地移动到弃牌堆目标点并销毁。
    /// </summary>
    public IEnumerator DiscardCardSequence(GameObject cardObject)
    {
        if (cardObject == null || discardZoneTarget == null)
        {
            Destroy(cardObject);
            yield break;
        }
        
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();

        // Animation: Scale down and fly towards discard pile
        cardRect.DOScale(Vector3.zero, playDuration * 0.2f).SetEase(Ease.InBack);
        cardRect.DOMove(discardZoneTarget.position, playDuration * 0.2f).SetEase(Ease.InSine);
        
        yield return new WaitForSeconds(playDuration * 0.2f);
        
        // Destroy the card object
        Destroy(cardObject); 
    }
}