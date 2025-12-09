using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; 
using UnityEngine.EventSystems; 
using System.Collections; 
using TMPro; 
using System; 

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    [Header("系统引用 (必须设置)")]
    public CardSystem cardSystem; 
    public CharacterManager characterManager; 

    [Header("UI Config")]
    public GameObject cardPrefab; 
    public Transform handContainer; 
    [Tooltip("手牌容器的 RectTransform，用于鼠标坐标转换 (必须设置)")]
    public RectTransform handContainerRect; 
    public Transform discardPileLocationTransform; 
    public Transform drawPileLocationTransform;    
    public Transform playCenterTransform; 

    public List<CardDisplay> handDisplays = new List<CardDisplay>(); 
    
    private List<(CardDisplay display, float centerX)> handLayoutReference = new List<(CardDisplay, float)>(); 

    [Header("回合状态")]
    public int CurrentRound { get; private set; } = 0; 
    public int cardsToDraw = 5; 

    [Header("手牌布局: 固定的弧度和间距")]
    [Range(600f, 1500f)]
    public float arcBaseWidth = 1000f; 
    
    [Range(50f, 500f)] 
    public float arcHeight = 250f; 
    
    [Range(100f, 300f)]
    public float cardSpacing = 175f; 
    
    [Header("动画参数")]
    [Range(0.1f, 1f)]
    public float repositionDuration = 0.3f; 
    
    [Range(0.05f, 0.5f)]
    public float drawDuration = 0.1f; 
    
    [Range(0.001f, 0.1f)]
    public float drawCardDelay = 0.01f; 
    
    [Range(0.05f, 0.5f)]
    public float playCardDuration = 0.1f; 
    
    [Range(0.05f, 0.5f)]
    public float centerIdleDuration = 0.12f; 
    
    [Range(0.05f, 0.5f)]
    public float postExecutionDelay = 0.1f; 
    
    [Range(0f, 50f)]
    public float temporaryDrawOffset = 20f;
    
    [Range(0f, 1f)]
    public float drawZSeparation = 0.1f; 
    
    [Header("动画缓动类型")]
    public Ease drawEaseType = Ease.OutQuad; 
    public Ease playToCenterEaseType = Ease.OutSine; 
    public Ease playToDiscardEaseType = Ease.InQuad; 
    
    [Header("手牌高亮与悬停参数")]
    [Range(0.1f, 1f)]
    public float hoverDelayDuration = 0.3f; 
    
    [Range(20f, 100f)]
    public float hoverTranslateY = 50f; 
    
    [Range(1f, 1.5f)]
    public float hoverScale = 1.1f; 
    
    [Range(0f, 100f)]
    public float extraSpacingOnHover = 50f;
    
    [Range(0f, 0.5f)]
    public float hoverToleranceFactor = 0.4f; 

    [Range(10f, 300f)]
    public float hoverToleranceY = 150f; 

    [Header("调试用默认资产")]
    public EnemyData defaultEnemyDataAsset; 
    public RoundBasedStrategy defaultEnemyStrategyAsset; 

    private CardDisplay highlightedCard = null; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (handContainer != null && handContainerRect == null)
        {
            handContainerRect = handContainer.GetComponent<RectTransform>();
        }
    }

    void Start()
    {
        if (cardSystem == null) cardSystem = FindFirstObjectByType<CardSystem>();
        if (characterManager == null) characterManager = FindFirstObjectByType<CharacterManager>();
        
        SetupMockCharactersIfNecessary();
        
        if (cardSystem != null) cardSystem.SetupDeck();
        
        StartBattle();
    }
    
    void Update()
    {
        if (handDisplays.Count > 0)
        {
             HandleHoverSelection();
        }
    }
    
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
        float hoverToleranceX = cardSpacing * hoverToleranceFactor;

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

        UpdateHandLayout(true);
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
    
    private void SetupMockCharactersIfNecessary()
    {
        if (characterManager == null) return;
        
        // 英雄 Mock Setup
        if (characterManager.GetActiveHero() == null)
        {
             GameObject heroObj = new GameObject("Mock Hero", typeof(CharacterBase));
             heroObj.hideFlags = HideFlags.DontSave; 
             
             CharacterBase heroChar = heroObj.GetComponent<CharacterBase>();
             heroChar.characterName = "Player Hero";
             heroChar.maxHp = 100;
             heroChar.currentHp = heroChar.maxHp;
             
             characterManager.activeHero = heroChar;
             characterManager.allHeroes.Add(heroChar);
             
             Debug.Log("Created Mock Hero for testing.");
        }

        // 敌人 Mock Setup
        if (characterManager.GetAllEnemies().Count == 0 && defaultEnemyDataAsset != null)
        {
            GameObject enemyObj = new GameObject("Mock Enemy 1", typeof(CharacterBase), typeof(EnemyAI));
            enemyObj.hideFlags = HideFlags.DontSave; 
            
            CharacterBase enemyChar = enemyObj.GetComponent<CharacterBase>();
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();

            // ⭐ 修复 UI 初始化 ⭐
            CharacterUIDisplay uiDisplay = enemyObj.AddComponent<CharacterUIDisplay>(); 
            
            if (enemyAI != null) 
            {
                enemyAI.enemyData = defaultEnemyDataAsset; 
                
                // 假设 roundBasedStrategy 缺失问题已解决
                // if (defaultEnemyStrategyAsset != null) { enemyAI.roundBasedStrategy = defaultEnemyStrategyAsset; } 

                enemyChar.characterName = defaultEnemyDataAsset.enemyName;
                enemyChar.maxHp = defaultEnemyDataAsset.maxHp;
                enemyChar.currentHp = defaultEnemyDataAsset.maxHp;
            }
            
            if (uiDisplay != null)
            {
                uiDisplay.Initialize(enemyChar); 
                Debug.Log($"LOG UI INIT: Successfully initialized UI for {enemyChar.characterName}.");
            }
            else
            {
                 Debug.LogError("UI INIT ERROR: Failed to add CharacterUIDisplay on Mock Enemy.");
            }
            
            characterManager.allEnemies.Add(enemyChar);
            Debug.Log("Created Mock Enemy for testing.");
        }
    }
    
    public void StartBattle()
    {
        CurrentRound = 1; 
        if (characterManager.GetActiveHero() != null)
        {
            CalculateAllEnemyIntents();
            StartNewTurn();
        }
        else
        {
             Debug.LogError("无法开始战斗：没有激活的主角 (activeHero)。");
        }
    }

    public void StartNewTurn()
    {
        if (characterManager != null)
        {
            characterManager.AtStartOfTurn(); 
        }

        if (cardSystem == null) return;
        
        cardSystem.ResetEnergy(); 
        DiscardHandDisplays(); 
        cardSystem.DiscardHand(); 
        
        DrawCards(cardsToDraw); 
        Debug.Log($"--- 玩家回合开始 (回合 {CurrentRound}) ---");
    }

    public void EndPlayerTurn()
    {
        Debug.Log("--- 玩家回合结束 ---");

        if (characterManager != null)
        {
            // ⭐ 核心修复 1/2: 在玩家回合结束时，清除敌人的格挡！ ⭐
            Debug.Log("LOG FLOW: 清除敌人回合 N 获得的格挡 (在玩家回合结束时)。");
            characterManager.DecrementSpecificGroupBlockDurations(characterManager.allEnemies);
            
            characterManager.AtEndOfTurn();
        }
        
        DiscardHandDisplays();
        cardSystem.DiscardHand(); 
        
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        Debug.Log($"--- 敌人回合开始 (回合 {CurrentRound}) ---");
        
        CharacterBase activeHero = characterManager.GetActiveHero();
        if (activeHero == null)
        {
            CheckBattleEnd();
            return;
        }
        Sequence enemyTurnSequence = DOTween.Sequence(); 
        
        if (characterManager != null)
        {
            characterManager.AtStartOfTurn();
        }

        foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0)) 
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null)
            {
                Sequence actionSequence = enemyAI.PerformAction(activeHero, CurrentRound);
                enemyTurnSequence.Append(actionSequence);
            }
        }
        
        enemyTurnSequence.OnComplete(() =>
        {
            // 延迟提供视觉缓冲
            DOVirtual.DelayedCall(0.5f, () => {
        
                Debug.Log($"LOG FLOW: 敌人行动序列完成，准备回合转换。"); 

                if (characterManager != null)
                {
                    // ⭐ 核心修复 2/2: 在敌人回合结束时，清除玩家格挡！ ⭐
                    Debug.Log("LOG FLOW: 清除玩家回合 N 获得的格挡 (在敌人回合结束时)。");
                    characterManager.DecrementSpecificGroupBlockDurations(characterManager.allHeroes);
                    
                    characterManager.AtEndOfTurn();
                }

                // ⭐ 修复回合数递增翻倍问题，只在这里递增一次 ⭐
                CurrentRound++; 
                Debug.Log($"LOG FLOW: 回合数递增完成，新的回合数: {CurrentRound}"); 
                
                CalculateAllEnemyIntents(); 
                
                CheckBattleEnd(); 

                StartNewTurn(); // 进入玩家新回合
                
            }).SetUpdate(true);
        });
    }

    private void CalculateAllEnemyIntents()
    {
       
        CharacterBase activeHero = characterManager.GetActiveHero();
        if (activeHero == null) return;

        foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0)) 
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null) 
            {
            enemyAI.CalculateIntent(activeHero, CurrentRound); 
            
            EnemyDisplay display = enemy.GetComponentInChildren<EnemyDisplay>();
            if (display != null)
            {
                display.RefreshIntent(enemyAI.nextIntent, enemyAI.intentValue);
                Debug.Log($"DEBUG: 意图刷新通知发送给 {enemy.characterName}。");
            }
            else
            {
                Debug.LogError($"无法在 {enemy.characterName} 上找到 EnemyDisplay 脚本！");
            }
            } 
        }
        Debug.Log($"LOG FLOW: 开始计算所有敌人意图，基于回合: {CurrentRound}"); 
    }
    
    public bool IsValidTarget(CardData card, CharacterBase target)
    {
        if (cardSystem == null) return false;
        return cardSystem.IsValidTarget(card, target); 
    }

    public void DrawCards(int count)
    {
        if (cardSystem == null || characterManager == null) return;
        
        List<CardData> drawnCardsData = cardSystem.DrawCards(count);

        Sequence drawSequence = DOTween.Sequence();
        
        for (int i = 0; i < drawnCardsData.Count; i++)
        {
            CardData drawnCard = drawnCardsData[i];
            
            GameObject cardObject = Instantiate(cardPrefab, drawPileLocationTransform.position, Quaternion.identity, handContainer);
            CardDisplay display = cardObject.GetComponent<CardDisplay>();
            
            if (display != null) display.Initialize(drawnCard, characterManager.GetActiveHero()); 
            handDisplays.Add(display);
            
            Transform cardTransform = display.transform;
            
            Vector3 tempDrawPos = drawPileLocationTransform.position + Vector3.up * temporaryDrawOffset + Vector3.forward * drawZSeparation * i;

            drawSequence.Append(
                cardTransform.DOMove(tempDrawPos, drawDuration * 0.5f) 
                    .SetEase(drawEaseType) 
                    .SetDelay(i * drawCardDelay)
            );

            drawSequence.Append(
                 cardTransform.DOMove(handContainer.position, drawDuration * 0.5f) 
                    .SetEase(drawEaseType) 
            );
        }
        
        drawSequence.OnComplete(() => UpdateHandLayout(true)); 
    }

    private void DiscardHandDisplays()
    {
        foreach (CardDisplay display in handDisplays)
        {
            if (display != null && display.gameObject != null)
            {
                Destroy(display.gameObject);
            }
        }
        handDisplays.Clear();
        handLayoutReference.Clear(); 
        highlightedCard = null;      
    }
    
    public bool TryPlayCard(CardData card, CharacterBase target, GameObject cardDisplayObject)
    {
        if (cardSystem == null || characterManager == null || card == null) return false;
        
        if (!cardSystem.CanPlayCard(card)) return false;
        
        CharacterBase actualTarget = target;
        
        if (cardSystem.CardNeedsSelectedTarget(card))
        {
            if (actualTarget == null)
            {
                CharacterBase firstEnemy = characterManager.GetAllEnemies().FirstOrDefault(e => e != null && e.currentHp > 0);
                
                if (firstEnemy != null)
                {
                    actualTarget = firstEnemy; 
                    Debug.Log($"自动锁定目标: {actualTarget.characterName}");
                }
            }
            
            if (actualTarget == null)
            {
                
                Debug.LogWarning($"卡牌 {card.cardName} 需要目标但场上没有存活的敌人。");
                return false;
            }
        }
        
        if (cardSystem.CardNeedsSelectedTarget(card) && !IsValidTarget(card, actualTarget)) return false;

        cardSystem.SpendEnergy(card.energyCost);
        Debug.Log($"成功打出 {card.cardName}，剩余能量: {cardSystem.CurrentEnergy}");

        
        CardDisplay displayToRemove = cardDisplayObject.GetComponent<CardDisplay>();
        if (displayToRemove != null)
        {
            handDisplays.Remove(displayToRemove);
            if (highlightedCard == displayToRemove) highlightedCard = null;
        }
        
        Transform targetTransform = actualTarget != null ? actualTarget.transform : handContainer; 

        AnimatePlayCard(
            card, 
            cardDisplayObject.transform, 
            targetTransform,            
            discardPileLocationTransform         
        );
        
        return true;
    }

    public void AnimatePlayCard(CardData card, Transform cardTransform, Transform targetTransform, Transform discardPileTransform)
    {
        float discardDuration = 0.2f;
        Transform centerPos = playCenterTransform != null ? playCenterTransform : handContainer;

        Sequence playSequence = DOTween.Sequence();

        playSequence.Append(
            cardTransform.DOMove(centerPos.position, playCardDuration)
                        .SetEase(playToCenterEaseType) 
        );
        
        playSequence.Join(cardTransform.DORotate(Vector3.zero, playCardDuration).SetEase(playToCenterEaseType));
        playSequence.Join(cardTransform.DOScale(Vector3.one, playCardDuration).SetEase(playToCenterEaseType));

        playSequence.AppendInterval(centerIdleDuration); 
        
        playSequence.AppendCallback(() => {
            Debug.Log($"卡牌效果 {card.cardName} 触发！");
            
            try
            {
                CharacterBase targetCharacter = targetTransform.GetComponent<CharacterBase>();
                
                CharacterBase source = characterManager.GetActiveHero();
                card.ExecuteEffects(source, targetCharacter, cardSystem); 
                
                cardSystem.PlayCard(card); 
                UpdateHandLayout(true); 
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error executing card effect or updating state for {card.cardName}: {ex.Message}");
            }
        });
        
        playSequence.AppendInterval(postExecutionDelay);

        playSequence.Append(
            cardTransform.DOMove(discardPileTransform.position, discardDuration) 
                        .SetEase(playToDiscardEaseType) 
        );
            
        playSequence.AppendCallback(() => {
            if (cardTransform != null && cardTransform.gameObject != null)
            {
                Destroy(cardTransform.gameObject);
            }
        });
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
    
    public void UpdateHandLayout(bool useAnimation) 
    { 
        if (handDisplays == null || handDisplays.Count == 0) 
        {
            handLayoutReference.Clear();
            return;
        }

        float duration = useAnimation ? this.repositionDuration : 0f;
        int count = handDisplays.Count;
        
        handLayoutReference.Clear(); 

        float targetLayoutWidth = (count > 1) ? (count - 1) * cardSpacing : 0f;
        float fixedArcWidth = arcBaseWidth;
        float fixedArcHeight = arcHeight;
        
        int highlightedIndex = -1;
        if (highlightedCard != null)
        {
            highlightedIndex = handDisplays.IndexOf(highlightedCard);
        }
        
        float gapSize = extraSpacingOnHover;

        for (int i = 0; i < count; i++)
        {
            CardDisplay display = handDisplays[i];
            if (display == null) continue;

            float idealX = i * cardSpacing - targetLayoutWidth / 2f; 

            float spreadOffset = 0f;
            if (highlightedIndex != -1)
            {
                if (i < highlightedIndex)
                {
                    spreadOffset = -gapSize / 2f; 
                }
                else if (i > highlightedIndex)
                {
                    spreadOffset = gapSize / 2f; 
                }
            }
            
            float finalTargetX = idealX + spreadOffset;

            handLayoutReference.Add((display, finalTargetX));

            float t = (finalTargetX + fixedArcWidth / 2f) / fixedArcWidth;
            t = Mathf.Clamp01(t); 

            (Vector3 targetPosition, Quaternion targetRotation) = CalculateBezierPoint(t, fixedArcWidth, fixedArcHeight);

            Vector3 finalTargetPosition = targetPosition;
            Vector3 finalTargetRotation = targetRotation.eulerAngles;
            Vector3 finalTargetScale = Vector3.one;

            if (i == highlightedIndex)
            {
                Vector3 liftDirection = targetRotation * Vector3.up; 
                finalTargetPosition += liftDirection * hoverTranslateY;
                finalTargetScale = Vector3.one * hoverScale;
            }
            
            display.transform.DOLocalMove(finalTargetPosition, duration).SetEase(Ease.OutQuad);
            display.transform.DOLocalRotate(finalTargetRotation, duration);
            display.transform.DOScale(finalTargetScale, duration); 
        }
    }

    public void CheckBattleEnd()
    {
        if (characterManager == null) return;
        
        bool allEnemiesDead = characterManager.GetAllEnemies().All(e => e.currentHp <= 0);
        
        if (allEnemiesDead)
        {
            Debug.Log("战斗胜利!");
        }
        else if (characterManager.GetActiveHero() == null || characterManager.GetActiveHero().currentHp <= 0)
        {
             Debug.Log("战斗失败!");
        }
    }
}