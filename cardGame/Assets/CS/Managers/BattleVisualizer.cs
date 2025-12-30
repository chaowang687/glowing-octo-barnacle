using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class BattleVisualizer : MonoBehaviour
{
    public static BattleVisualizer Instance { get; private set; }

    [Header("UI Config")]
    [Tooltip("手牌容器的 RectTransform，用于鼠标坐标转换 (必须设置)")]
    public RectTransform handContainerRect; 

    [Header("手牌布局: 固定的弧度和间距")]
    [Range(600f, 1500f)]
    public float arcBaseWidth = 1000f; 
    
    [Range(50f, 500f)] 
    public float arcHeight = 250f; 
    
    [Range(100f, 300f)]
    public float maxCardSpacing = 175f; 
    [Range(50f, 150f)]
    public float minCardSpacing = 80f;  
    [Range(0.5f, 1.0f)]
    public float arcWidthFactor = 0.8f;  

    [Header("手牌高亮与悬停参数")]
    [Range(0.1f, 1f)]
    public float hoverDelayDuration = 0.3f; 
    
    [Range(20f, 100f)]
    public float hoverTranslateY = 50f; 
    
    [Range(1f, 1.5f)]
    public float hoverScale = 1.1f; 
    
    [Range(0f, 100f)]
    public float extraSpacingOnHover = 50f;
    
    [Range(0f, 1f)]
    public float hoverToleranceFactor = 0.4f; 

    [Range(10f, 300f)]
    public float hoverToleranceY = 150f; 

    [Header("Transform References")]
    public Transform discardPileLocationTransform; 
    public Transform drawPileLocationTransform;    
    public Transform playCenterTransform; 

    [Header("Animation Parameters")]
    [Range(0.1f, 1f)]
    public float repositionDuration = 0.3f; 
    
    [Range(0.1f, 1f)]
    public float postPlayRepositionDuration = 0.2f; 
    
    [Range(0.05f, 1f)]
    public float drawDuration = 0.5f; 
    
    [Range(0.001f, 0.2f)]
    public float drawCardDelay = 0.08f; 
    
    [Range(0.05f, 0.5f)]
    public float playCardDuration = 0.1f; 
    
    [Range(0.05f, 0.5f)]
    public float centerIdleDuration = 0.12f; 
    
    [Range(0.05f, 0.5f)]
    public float postExecutionDelay = 0.1f; 
    
    [Header("Draw Animation")]
    [Range(0.01f, 0.3f)]
    public float layoutCardDelay = 0.05f; 
    public Vector3 centralPileOffset = new Vector3(0f, 20f, -0.1f); 
    
    [Header("Discard Animation")]
    [Range(0.01f, 0.5f)]
    public float discardInterval = 0.05f;
    [Range(0.1f, 1f)]
    public float discardDuration = 0.3f;
    
    [Header("Ease Types")]
    public Ease drawEaseType = Ease.OutQuad; 
    public Ease playToCenterEaseType = Ease.OutSine; 
    public Ease playToDiscardEaseType = Ease.InQuad; 

    // Visual list of cards
    private List<CardDisplay> visualHandDisplays = new List<CardDisplay>();
    
    // Layout reference for hover detection
    private List<(CardDisplay display, float centerX)> handLayoutReference = new List<(CardDisplay, float)>(); 

    private CardDisplay highlightedCard = null; 

    // Layout data structure
    public struct CardLayoutData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float centerX; 
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Only handle hover if not dragging and if we have cards
        // Note: BattleManager used to check isTurnInProgress/isCardBeingPlayed. 
        // We should probably expose a property or check BattleManager.Instance
        if (visualHandDisplays.Count > 0)
        {
             // Simple check to avoid conflict with major game flow events if BattleManager is available
             if (BattleManager.Instance != null && !BattleManager.Instance.IsInteractionLocked())
             {
                 HandleHoverSelection();
             }
             else
             {
                 SetHighlightedCard(null);
             }
        }
    }

    public void RegisterCard(CardDisplay card)
    {
        if (!visualHandDisplays.Contains(card))
        {
            visualHandDisplays.Add(card);
            // Update layout immediately or wait for next frame?
            // Usually DrawCards handles animation, so we might just add it.
        }
    }

    public void UnregisterCard(CardDisplay card, bool updateLayout = true)
    {
        if (visualHandDisplays.Contains(card))
        {
            visualHandDisplays.Remove(card);
            if (highlightedCard == card) highlightedCard = null;
            
            if (updateLayout)
            {
                // Update layout to close the gap
                UpdateHandLayout(0.3f); 
            }
        }
    }
    
    public void ClearHand()
    {
        visualHandDisplays.Clear();
        handLayoutReference.Clear();
        highlightedCard = null;
    }

    public void AnimateCardDraw(List<CardDisplay> newlyDrawnDisplays, Vector3 drawPileLocalPos, Vector3 receiveCenterLocalPos, System.Action onComplete)
    {
        Sequence drawSequence = DOTween.Sequence();
        
        for (int i = 0; i < newlyDrawnDisplays.Count; i++)
        {
            CardDisplay display = newlyDrawnDisplays[i];
            float delay = i * drawCardDelay;

            Vector3 receiveTargetLocalPos = receiveCenterLocalPos + 
                new Vector3(centralPileOffset.x * i, centralPileOffset.y * i, centralPileOffset.z * i); 

            drawSequence.Insert(delay, 
                display.transform.DOLocalMove(receiveTargetLocalPos, drawDuration)
                    .SetEase(drawEaseType) 
            );
            
            drawSequence.Insert(delay, 
                display.transform.DOLocalRotate(Vector3.zero, drawDuration) 
                    .SetEase(drawEaseType)
            );
        }

        drawSequence.OnComplete(() => {
            
            // 1. Calculate final layout targets
            List<CardLayoutData> finalLayoutTargets = CalculateAllCurrentLayout(null);
            
            // 2. Start sequential layout animation
            Sequence layoutSequence = DOTween.Sequence();
            
            for (int i = 0; i < visualHandDisplays.Count; i++) 
            {
                CardDisplay display = visualHandDisplays[i];
                if (i >= finalLayoutTargets.Count) break;

                CardLayoutData targetData = finalLayoutTargets[i]; 
                
                float delay = i * layoutCardDelay; 

                layoutSequence.Insert(delay, 
                    display.transform.DOLocalMove(targetData.position, repositionDuration).SetEase(Ease.OutQuad)
                );
                layoutSequence.Insert(delay, 
                    display.transform.DOLocalRotate(targetData.rotation.eulerAngles, repositionDuration)
                );
                layoutSequence.Insert(delay, 
                    display.transform.DOScale(targetData.scale, repositionDuration)
                );
            }

            layoutSequence.OnComplete(() => {
                 UpdateHandLayout(0f); 
                 onComplete?.Invoke();
            });
        });
    }

    public void AnimatePlayCardSequence(CardData card, Transform cardTransform, Transform targetTransform, System.Action onEffectTrigger, System.Action onComplete)
    {
        if (cardTransform == null || discardPileLocationTransform == null)
        {
            Debug.LogError("AnimatePlayCardSequence: Transform references missing");
            onComplete?.Invoke();
            return;
        }

        float discardDuration = 0.2f;
        Transform centerPos = playCenterTransform != null ? playCenterTransform : handContainerRect;

        Sequence playSequence = DOTween.Sequence();

        // 1. Move to Center Position (Play Animation)
        playSequence.Append(
            cardTransform.DOMove(centerPos.position, playCardDuration)
                        .SetEase(playToCenterEaseType) 
        );
        
        playSequence.Join(cardTransform.DORotate(Vector3.zero, playCardDuration).SetEase(playToCenterEaseType));
        playSequence.Join(cardTransform.DOScale(Vector3.one, playCardDuration).SetEase(playToCenterEaseType));

        // 2. Idle for effect trigger
        playSequence.AppendInterval(centerIdleDuration); 
        
        // 3. Execute Card Effect (Callback)
        playSequence.AppendCallback(() => onEffectTrigger?.Invoke());
        
        playSequence.AppendInterval(postExecutionDelay);

        // 4. Move to Discard Pile
        playSequence.Append(
            cardTransform.DOMove(discardPileLocationTransform.position, discardDuration) 
                        .SetEase(playToDiscardEaseType) 
        );
            
        // 5. Destroy Object and Unlock
        playSequence.AppendCallback(() => {
            if (cardTransform != null && cardTransform.gameObject != null)
            {
                Destroy(cardTransform.gameObject);
            }
            onComplete?.Invoke();
        });
    }

    public void AnimateDiscardHand(List<CardDisplay> cardsToDiscard, System.Action onComplete)
    {
        if (cardsToDiscard.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        // 1. Lock all cards in their current visual state immediately
        // This prevents them from snapping to new layout positions when the hand is cleared logically
        foreach (var display in cardsToDiscard)
        {
            if (display != null)
            {
                DOTween.Kill(display.transform); // Stop any floating/hover animations
                display.enabled = false;         // Disable script interaction
            }
        }

        Sequence discardSequence = DOTween.Sequence();
        
        // Iterate from Right (End) to Left (Start)
        for (int i = cardsToDiscard.Count - 1; i >= 0; i--)
        {
            CardDisplay display = cardsToDiscard[i];
            if (display == null) continue;

            // Calculate delay based on reverse order
            int sequenceIndex = (cardsToDiscard.Count - 1) - i;
            float startTime = sequenceIndex * discardInterval;
            
            if (discardPileLocationTransform != null)
            {
                discardSequence.Insert(startTime, 
                    display.transform.DOMove(discardPileLocationTransform.position, discardDuration)
                    .SetEase(playToDiscardEaseType));
            }
            
            float destroyTime = startTime + discardDuration;
            CardDisplay captureDisplay = display; 
            discardSequence.InsertCallback(destroyTime, () => {
                 if (captureDisplay != null) Destroy(captureDisplay.gameObject);
            });
        }

        discardSequence.OnComplete(() => onComplete?.Invoke());
    }

    // --- Hover Logic ---

    // 更新 Hover 逻辑中对 cardSpacing 的引用
    private void HandleHoverSelection()
    {
        if (handLayoutReference.Count == 0 || handContainerRect == null)
        {
            SetHighlightedCard(null); 
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handContainerRect, 
            Input.mousePosition, 
            null, 
            out Vector2 localMousePos
        );
        
        float minYThreshold = -hoverToleranceY;
        float maxYThreshold = arcHeight + hoverTranslateY + 50f; 

        if (localMousePos.y < minYThreshold || localMousePos.y > maxYThreshold)
        {
            SetHighlightedCard(null); 
            return;
        }

        CardDisplay bestMatch = null;
        float minDistance = float.MaxValue;
        
        // 这里使用 maxCardSpacing 作为基础容差，确保能覆盖到
        float hoverToleranceX = maxCardSpacing * hoverToleranceFactor; 

        foreach (var item in handLayoutReference)
        {
            float distanceX = Mathf.Abs(localMousePos.x - item.centerX);
            
            if (distanceX < hoverToleranceX && distanceX < minDistance)
            {
                minDistance = distanceX;
                bestMatch = item.display;
            }
        }
        
        SetHighlightedCard(bestMatch);
    }

    private void SetHighlightedCard(CardDisplay newHighlightedCard)
    {
        if (highlightedCard == newHighlightedCard) return;

        highlightedCard = newHighlightedCard;
        
        if (highlightedCard != null)
        {
            highlightedCard.transform.SetAsLastSibling();
        }

        UpdateHandLayout(0.3f); // default duration
    }
    
    public void UnhighlightCard(CardDisplay card)
    {
        if (highlightedCard == card)
        {
            SetHighlightedCard(null);
        }
    }
    
    public CardDisplay GetHighlightedCard()
    {
        return highlightedCard;
    }

    // --- Layout Logic ---

    // 新增：强制刷新所有卡牌的 Sibling Index (渲染顺序)
    // 确保右边的卡牌压在左边的卡牌上面 (或者符合你的设计预期)
    private void RefreshSiblingIndices()
    {
        for (int i = 0; i < visualHandDisplays.Count; i++)
        {
            CardDisplay card = visualHandDisplays[i];
            if (card != null && card != highlightedCard) // 高亮卡牌保持最上层，不重置
            {
                card.transform.SetSiblingIndex(i);
            }
        }
        // 再次确保高亮卡在最上
        if (highlightedCard != null)
        {
            highlightedCard.transform.SetAsLastSibling();
        }
    }

    public void UpdateHandLayout(float duration) 
    { 
        if (visualHandDisplays == null || visualHandDisplays.Count == 0) return;

        // 每次更新布局时，也刷新渲染层级
        RefreshSiblingIndices();

        List<CardLayoutData> layoutData = CalculateAllCurrentLayout(highlightedCard);
        
        for (int i = 0; i < visualHandDisplays.Count; i++)
        {
            CardDisplay card = visualHandDisplays[i];
            if (card == null) continue;

            if (card.IsDragging) 
            {
                continue; 
            }

            if (i >= layoutData.Count) break;
            CardLayoutData targetData = layoutData[i];

            card.transform.DOLocalMove(targetData.position, duration).SetEase(Ease.OutQuad);
            card.transform.DOLocalRotate(targetData.rotation.eulerAngles, duration).SetEase(Ease.OutQuad);
            card.transform.DOScale(targetData.scale, duration).SetEase(Ease.OutQuad);
        }
    }

    private List<CardLayoutData> CalculateAllCurrentLayout(CardDisplay hoverCard)
    {
        List<CardLayoutData> layoutDataList = new List<CardLayoutData>();
        
        if (visualHandDisplays == null || visualHandDisplays.Count == 0) 
        {
            handLayoutReference.Clear();
            return layoutDataList;
        }
        
        int count = visualHandDisplays.Count;
        handLayoutReference.Clear(); 

        // --- 核心优化：动态计算间距 ---
        float fixedArcWidth = arcBaseWidth; 
        float availableWidth = fixedArcWidth * arcWidthFactor; // 实际可用的弧线宽度
        
        // 1. 尝试使用最大间距
        float currentSpacing = maxCardSpacing;
        float totalCardWidth = (count > 1) ? (count - 1) * currentSpacing : 0f;
        
        // 2. 如果总宽度超出了可用范围，则压缩间距
        if (totalCardWidth > availableWidth)
        {
            // 重新计算间距：总可用宽度 / (卡牌数 - 1)
            // 至少保证有 minCardSpacing
            currentSpacing = Mathf.Max(minCardSpacing, availableWidth / (count - 1));
            totalCardWidth = (count - 1) * currentSpacing;
        }
        
        // --- 间距计算结束 ---
        
        float fixedArcHeight = arcHeight;
        
        int highlightedIndex = -1;
        if (hoverCard != null)
        {
            highlightedIndex = visualHandDisplays.IndexOf(hoverCard);
        }
        
        float gapSize = extraSpacingOnHover;
        float totalGapOffset = (highlightedIndex != -1) ? gapSize : 0f;
        
        float adjustedTotalWidth = totalCardWidth + totalGapOffset;
        float currentLayoutStart = -adjustedTotalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            CardDisplay display = visualHandDisplays[i];
            if (display == null) continue;

            float currentXOffset = i * currentSpacing; // 使用动态计算的 currentSpacing
            
            if (highlightedIndex != -1 && i > highlightedIndex)
            {
                currentXOffset += gapSize;
            }

            float finalTargetX = currentLayoutStart + currentXOffset;
            
            handLayoutReference.Add((display, finalTargetX));

            // 将 X 坐标映射到贝塞尔曲线的 t 值 (0~1)
            // 注意：这里用 finalTargetX 相对于 arcBaseWidth 进行归一化
            float t = (finalTargetX + fixedArcWidth / 2f) / fixedArcWidth;
            t = Mathf.Clamp01(t); 

            (Vector3 targetPosition, Quaternion targetRotation) = CalculateBezierPoint(t, fixedArcWidth, fixedArcHeight);

            Vector3 finalTargetPosition = targetPosition;
            Vector3 finalTargetRotation = targetRotation.eulerAngles;
            Vector3 finalTargetScale = Vector3.one;

            finalTargetPosition.z = i * 0.001f; 

            if (i == highlightedIndex)
            {
                Vector3 liftDirection = targetRotation * Vector3.up; 
                finalTargetPosition += liftDirection * hoverTranslateY;
                finalTargetScale = Vector3.one * hoverScale;
            } 
            
            layoutDataList.Add(new CardLayoutData
            {
                position = finalTargetPosition,
                rotation = targetRotation,
                scale = finalTargetScale,
                centerX = finalTargetX
            });
        }
        
        return layoutDataList;
    }

    private (Vector3 position, Quaternion rotation) CalculateBezierPoint(float t, float width, float height)
    {
        Vector3 p0 = new Vector3(-width / 2f, 0f, 0f);
        Vector3 p1 = new Vector3(0f, height, 0f);
        Vector3 p2 = new Vector3(width / 2f, 0f, 0f);

        float oneMinusT = 1f - t;
        
        Vector3 position = 
            (oneMinusT * oneMinusT * p0) + 
            (2f * oneMinusT * t * p1) + 
            (t * t * p2);

        Vector3 p0_p1 = p1 - p0; 
        Vector3 p1_p2 = p2 - p1; 

        Vector3 tangent = 
            (2f * oneMinusT * p0_p1) + 
            (2f * t * p1_p2);
        
        float angleZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg; 
        
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleZ);

        return (position, rotation);
    }
}
