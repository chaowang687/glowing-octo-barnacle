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
     [Header("战斗角色清理设置")]
    [Tooltip("角色死亡后，其游戏对象被销毁前的延迟时间（秒），用于播放死亡动画。")]
    public float characterDestroyDelay = 1.5f;
    public static BattleManager Instance { get; private set; }
    
    // 战斗状态：用于追踪战斗是否结束 (Battle State: Used to track if the battle is over)
    public bool IsBattleOver { get; private set; } = false; 
    
    // VITAL: 回合锁，防止连续点击或异步逻辑冲突 (End Turn Lock)
    private bool isTurnInProgress = false; 
    
    // VITAL NEW: 打牌锁，防止连续打牌冲突 (Play Card Lock)
    private bool isCardBeingPlayed = false; 

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
    
    // 布局参考数据，用于鼠标悬停检测
    private List<(CardDisplay display, float centerX)> handLayoutReference = new List<(CardDisplay, float)>(); 
    
    // 布局数据结构，用于计算目标位置
    private struct CardLayoutData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float centerX; // 用于碰撞检测
    }

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
    public float repositionDuration = 0.3f; // 整理阶段的动画时长 (Tidy phase duration, also used for hover)
    
    [Range(0.1f, 1f)]
    [Tooltip("打出卡牌后，剩余卡牌重新布局的动画时长。")]
    public float postPlayRepositionDuration = 0.2f; // NEW: 打牌后整理时长
    
    [Range(0.05f, 1f)]
    public float drawDuration = 0.5f; // 抽牌阶段单张卡牌的时长 (Draw phase single card duration)
    
    [Range(0.001f, 0.2f)]
    public float drawCardDelay = 0.08f; // 抽牌阶段的卡牌间隔 (Delay between drawing each card)
    
    [Range(0.05f, 0.5f)]
    public float playCardDuration = 0.1f; 
    
    [Range(0.05f, 0.5f)]
    public float centerIdleDuration = 0.12f; 
    
    [Range(0.05f, 0.5f)]
    public float postExecutionDelay = 0.1f; // Delay buffer for turn transition
    
    [Header("抽牌整理动画参数")]
    [Range(0.01f, 0.3f)]
    public float layoutCardDelay = 0.05f; // 整理阶段单张卡牌的间隔 (Delay between layouting each card)
    [Tooltip("卡牌在中央堆叠时，每张卡牌的X/Y/Z轴偏移量，用于形成可见的堆叠。")]
    public Vector3 centralPileOffset = new Vector3(0f, 20f, -0.1f); 
    
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
    
    [Range(0f, 1f)]
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
    
    public void RegisterDyingCharacter(CharacterBase character)
    {
        if (character == null)
        {
            Debug.LogError("Attempted to register a null character as dying.");
            return;
        }
        // 4. Destroy the character GameObject after a delay.
        Destroy(character.gameObject, characterDestroyDelay);
        Debug.Log($"Processing character death registration: {character.name}. Removing from active lists.");
    }

    void Start()
    {
        if (cardSystem == null) cardSystem = FindFirstObjectByType<CardSystem>();
        if (characterManager == null) characterManager = FindFirstObjectByType<CharacterManager>();
        
        SetupMockCharactersIfNecessary();
        
        if (cardSystem != null) cardSystem.SetupDeck();
        
        // StartBattle() handles the initial CurrentRound setup
        StartBattle();
    }
    
    void Update()
    {
        // 只有当回合和卡牌动画都没有进行时，才处理悬停
        if (handDisplays.Count > 0 && !isTurnInProgress && !isCardBeingPlayed) 
        {
             HandleHoverSelection();
        }
        else // VITAL: 如果锁住了，确保没有卡牌是高亮状态
        {
            SetHighlightedCard(null); 
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
            // Set the highlighted card to be the last sibling (on top)
            highlightedCard.transform.SetAsLastSibling();
        }

        // Use the default repositionDuration for hover layout animation
        UpdateHandLayout(repositionDuration); 
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
        
        // Hero Mock Setup
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

        // Enemy Mock Setup
        if (characterManager.GetAllEnemies().Count == 0 && defaultEnemyDataAsset != null)
        {
            GameObject enemyObj = new GameObject("Mock Enemy 1", typeof(CharacterBase), typeof(EnemyAI));
            enemyObj.hideFlags = HideFlags.DontSave; 
            
            CharacterBase enemyChar = enemyObj.GetComponent<CharacterBase>();
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();

            // FIX: Ensure UI is created and initialized
            EnemyDisplay uiDisplay = enemyObj.AddComponent<EnemyDisplay>(); 
            
            if (enemyAI != null) 
            {
                enemyAI.enemyData = defaultEnemyDataAsset; 
                
                enemyChar.characterName = defaultEnemyDataAsset.enemyName;
                enemyChar.maxHp = defaultEnemyDataAsset.maxHp;
                enemyChar.currentHp = enemyChar.maxHp;
            }
            
            if (uiDisplay != null)
            {
                uiDisplay.Initialize(enemyChar); 
                Debug.Log($"LOG UI INIT: Successfully initialized UI for {enemyChar.characterName}.");
            }
            else
            {
                 Debug.LogError("UI INIT ERROR: Failed to add EnemyDisplay on Mock Enemy. Death cleanup will fail.");
            }
            
            characterManager.allEnemies.Add(enemyChar);
            Debug.Log("Created Mock Enemy for testing.");
        }
    }
    
    public void StartBattle()
    {
        IsBattleOver = false; // Ensure battle state is reset
        CurrentRound = 1; 
        if (characterManager.GetActiveHero() != null)
        {
            CalculateAllEnemyIntents();
            StartNewTurn();
        }
        else
        {
             Debug.LogError("Cannot start battle: No active hero (activeHero).");
        }
    }

    public void StartNewTurn()
    {
        // Key: Check battle end immediately before starting the turn
        if (CheckBattleEnd()) return;

        if (characterManager != null)
        {
            characterManager.AtStartOfTurn(); 
        }

        if (cardSystem == null) return;
        
        cardSystem.ResetEnergy(); 
        DiscardHandDisplays(); 
        cardSystem.DiscardHand(); 
        
        // VITAL: DrawCards now handles the final turn lock release
        DrawCards(cardsToDraw); 
        Debug.Log($"--- Player Turn Start (Round {CurrentRound}) ---");
    }

    /// <summary>
    /// 结束玩家回合，确保在下一阶段开始前，卡牌飞到弃牌区的动画已完成。
    /// </summary>
    public void EndPlayerTurn()
    {
        // VITAL GUARD: 如果回合正在进行中，则忽略后续点击
        if (isTurnInProgress)
        {
            Debug.LogWarning("Turn is already in progress. Ignoring EndPlayerTurn call.");
            return;
        }
        
        // VITAL GUARD: 如果正在打牌动画中，也需要等待
        if (isCardBeingPlayed)
        {
             Debug.LogWarning("Card animation is currently in progress. Ignoring EndPlayerTurn call.");
             return;
        }
        
        isTurnInProgress = true; // VITAL LOCK: 立即设置回合锁

        Debug.Log("--- Player Turn End ---");
        
        float discardDuration = 0.2f;
        
        // 1. 确保在迭代时手牌列表稳定，并检查是否有卡牌需要弃掉
        List<CardDisplay> cardsToDiscard = handDisplays.ToList(); 
        bool cardsWereDiscarded = cardsToDiscard.Count > 0;

        // 2. 设置卡牌飞往弃牌区的动画
        foreach (var display in cardsToDiscard) 
        {
            if (display != null && discardPileLocationTransform != null)
            {
                // Animate to discard pile before destroying
                display.transform.DOMove(discardPileLocationTransform.position, discardDuration)
                    .SetEase(playToDiscardEaseType)
                    .OnComplete(() => Destroy(display.gameObject));
            }
        }
        
        // 3. 立即更新游戏逻辑状态：清空手牌
        DiscardHandDisplays();
        cardSystem.DiscardHand(); 
        
        // 4. 计算总等待时间：如果弃牌了，需要等待动画时长 + 缓冲；否则只等待缓冲。
        float totalWaitTime = cardsWereDiscarded ? discardDuration + postExecutionDelay : postExecutionDelay;

        // 5. 延迟调用，等待动画完成
        DOVirtual.DelayedCall(totalWaitTime, () => 
        {
            if (characterManager != null)
            {
                // 此时视觉动画已完成，可以执行回合结束的逻辑清理
                
                // Decrement duration for enemy blocks and states
                Debug.Log("LOG FLOW: Decrementing enemy block and status durations.");
                characterManager.DecrementSpecificGroupBlockDurations(characterManager.allEnemies);
                
                // Execute AtEndOfTurn logic for enemies only
                foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0))
                {
                    enemy.AtEndOfTurn(); 
                }
            }
            
            // 6. 启动敌方回合
            DOVirtual.DelayedCall(postExecutionDelay, StartEnemyTurn);
        });
    }

    private void StartEnemyTurn()
    {
        // Key: Check battle end immediately before starting the turn
        if (CheckBattleEnd()) 
        {
            isTurnInProgress = false; // VITAL: If battle ends here, release lock immediately
            return;
        }

        Debug.Log($"--- Enemy Turn Start (Round {CurrentRound}) ---");
        
        CharacterBase activeHero = characterManager.GetActiveHero();
        if (activeHero == null)
        {
            CheckBattleEnd();
            isTurnInProgress = false; // VITAL: If hero dies, release lock
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
            // Delay for visual buffer
            DOVirtual.DelayedCall(0.5f, () => {
        
                Debug.Log($"LOG FLOW: Enemy action sequence complete, preparing for turn transition."); 

                if (characterManager != null)
                {
                    // End of Enemy Turn -> Decrement Player Durations
                    Debug.Log("LOG FLOW: Decrementing player block and status durations.");
                    characterManager.DecrementSpecificGroupBlockDurations(characterManager.allHeroes);
                    
                    if (characterManager.GetActiveHero() != null)
                    {
                        characterManager.GetActiveHero().AtEndOfTurn(); 
                    }
                }
                
                DOVirtual.DelayedCall(postExecutionDelay, OnEnemyTurnCleanupComplete);
                
            }).SetUpdate(true);
        });
    }
    
    private void OnEnemyTurnCleanupComplete()
    {
        CurrentRound++; 
        Debug.Log($"LOG FLOW: Round count incremented: {CurrentRound}"); 
        
        CalculateAllEnemyIntents(); 
        
        if (CheckBattleEnd()) 
        {
            isTurnInProgress = false; // VITAL: If battle ends here, release lock immediately
            return;
        }

        StartNewTurn(); // Enter new player turn, which calls DrawCards
    }

    /// <summary>
    /// Receives a synchronous signal from CharacterBase, executed after the death animation starts.
    /// Performs immediate removal and flow check.
    /// </summary>
    public void HandleDyingCharacterCleanup(CharacterBase dyingCharacter)
    {
        if (dyingCharacter == null) return;

        // 1. Key: Immediately remove the character from CharacterManager's active list
        if (characterManager.ActiveEnemies.Contains(dyingCharacter)) 
        {
             characterManager.ActiveEnemies.Remove(dyingCharacter);
             Debug.Log($"[Death Cleanup] {dyingCharacter.characterName} synchronously removed from activeEnemies list.");
        }
        else if (characterManager.activeHero == dyingCharacter)
        {
             Debug.Log($"[Death Cleanup] Hero {dyingCharacter.characterName} death event captured.");
        }
        else
        {
             Debug.LogWarning($"[Death Cleanup] {dyingCharacter.characterName} not found in active lists.");
        }
        
        // 2. Immediately check if the battle should end
        CheckBattleEnd();
    }
    
    /// <summary>
    /// (Called by EnemyDisplay after death animation finishes)
    /// Responsible for final object destruction.
    /// </summary>
    public void HandleDeathAnimationComplete(GameObject deadCharacterObject)
    {
        // List removal and state cleanup done in HandleDyingCharacterCleanup.
        // This method handles the final destruction of the GameObject.
        
        Debug.Log($"[Death Animation Complete] Destroying object: {deadCharacterObject.name}");
        if (deadCharacterObject != null)
        {
            Destroy(deadCharacterObject);
        }
    }
    
    /// <summary>
    /// Central cleanup point for when the battle ends.
    /// </summary>
    public void EndBattle()
    {
        if (IsBattleOver) return; // Prevent double call

        IsBattleOver = true;
        isTurnInProgress = false; // VITAL: Release lock upon final battle end
        isCardBeingPlayed = false; // VITAL: Release card play lock upon final battle end
        
        // Add all end-of-battle logic here (rewards, UI change, scene loading).
        Debug.Log("[Battle State] Battle logic ended. IsBattleOver = true");
        
        // Example: Disable all card interaction and turn buttons
    }


    private void CalculateAllEnemyIntents()
    {
       
        CharacterBase activeHero = characterManager.GetActiveHero();
        if (activeHero == null) return;

        foreach (var enemy in characterManager.ActiveEnemies.ToList()) 
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null) 
            {
            enemyAI.CalculateIntent(activeHero, CurrentRound); 
            
            EnemyDisplay display = enemy.GetComponentInChildren<EnemyDisplay>();
            if (display != null)
            {
                display.RefreshIntent(enemyAI.nextIntent, enemyAI.intentValue);
                Debug.Log($"DEBUG: Intent refresh notification sent to {enemy.characterName}。");
            }
            else
            {
                Debug.LogError($"Could not find EnemyDisplay script on {enemy.characterName}!");
            }
            } 
        }
        Debug.Log($"LOG FLOW: Starting intent calculation for all enemies, based on round: {CurrentRound}"); 
    }
    
    // NOTE: This assumes CardSystem has a public method IsValidTarget(CardData card, CharacterBase target)
    public bool IsValidTarget(CardData card, CharacterBase target)
    {
        if (cardSystem == null) return false;
        return cardSystem.IsValidTarget(card, target); 
    }

    /// <summary>
    /// 核心发牌逻辑：分两阶段动画，实现抽牌到中央堆叠，再依次整理散开。
    /// VITAL: 此方法的最终 OnComplete 回调会释放回合锁。
    /// </summary>
    public void DrawCards(int count)
    {
        // 立即设置回合锁，防止在动画过程中结束回合
        isTurnInProgress = true; 
        
        if (cardSystem == null || characterManager == null) 
        {
            isTurnInProgress = false; // VITAL: If draw fails immediately, release lock
            return;
        }
        
        List<CardData> drawnCardsData = cardSystem.DrawCards(count);
        List<CardDisplay> newlyDrawnDisplays = new List<CardDisplay>(); 
        
        if (drawnCardsData.Count == 0) 
        {
            // If no cards are drawn, the entire animation sequence is skipped. 
            // We must release the lock immediately.
            isTurnInProgress = false; 
            return;
        }

        // 准备起点和终点
        Vector3 drawPileLocalPos = handContainer.InverseTransformPoint(drawPileLocationTransform.position);
        Vector3 receiveCenterLocalPos = new Vector3(0f, 0f, 0f); // 手牌容器中心

        foreach (CardData drawnCard in drawnCardsData)
        {
            GameObject cardObject = Instantiate(cardPrefab, handContainer);
            CardDisplay display = cardObject.GetComponent<CardDisplay>();
            
            if (display != null) display.Initialize(drawnCard, characterManager.GetActiveHero()); 
            handDisplays.Add(display);
            newlyDrawnDisplays.Add(display); 
            
            // 设置起点：卡牌区相对位置
            display.transform.localPosition = drawPileLocalPos;
            display.transform.localRotation = drawPileLocationTransform.localRotation;
            display.transform.localScale = Vector3.one; 
        }
        
        // --- 阶段 1: 抽牌到中央堆叠 (Draw to Center Pile) ---
        Sequence drawSequence = DOTween.Sequence();
        
        for (int i = 0; i < newlyDrawnDisplays.Count; i++)
        {
            CardDisplay display = newlyDrawnDisplays[i];
            float delay = i * drawCardDelay;

            // 聚集时的目标位置：中央点 + 可见X/Y/Z偏移，形成一个有层次的堆
            Vector3 receiveTargetLocalPos = receiveCenterLocalPos + 
                new Vector3(centralPileOffset.x * i, centralPileOffset.y * i, centralPileOffset.z * i); 

            // 动画从 drawPileLocalPos 飞到 receiveTargetLocalPos
            drawSequence.Insert(delay, 
                display.transform.DOLocalMove(receiveTargetLocalPos, drawDuration)
                    .SetEase(drawEaseType) 
            );
            
            // 动画旋转到平整
            drawSequence.Insert(delay, 
                display.transform.DOLocalRotate(Vector3.zero, drawDuration) 
                    .SetEase(drawEaseType)
            );
        }

        // --- 阶段 2: 依次整理到弧形布局 (Sequential Layout to Arc) ---
        drawSequence.OnComplete(() => {
            
            Debug.Log("[DrawCards] Stage 1 (Draw to Pile) complete. Starting Stage 2 (Tidy: Sequential Layout).");
            
            // 1. 计算所有卡牌的最终布局数据
            List<CardLayoutData> finalLayoutTargets = CalculateAllCurrentLayout(null);
            
            // 2. 开始整理序列
            Sequence layoutSequence = DOTween.Sequence();
            
            for (int i = 0; i < handDisplays.Count; i++) // Use all handDisplays for consistent index
            {
                CardDisplay display = handDisplays[i];
                // 找到对应的布局数据（由于此时没有悬停，布局数据就是按索引排列）
                CardLayoutData targetData = finalLayoutTargets[i]; 
                
                float delay = i * layoutCardDelay; // 依次分发的核心

                // 动画从当前位置（中央堆）飞到最终弧形位置
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
                 // 3. 整理完成后，必须更新 handLayoutReference for hover
                 // 此时调用 UpdateHandLayout(0f) 确保悬停检测数据是最新的，但使用 0 确保瞬时（因为动画已经完成）
                 UpdateHandLayout(0f); 
                 
                 // VITAL UNLOCK: 整个回合流程结束，释放锁
                 isTurnInProgress = false; 
                 
                 Debug.Log("[DrawCards] Stage 2 (Sequential Layout) complete. Hand ready. Turn Lock Released.");
            });
        });
    }
    
    /// <summary>
    /// 计算当前手牌中所有卡牌的目标位置、旋转和缩放数据。
    /// 同时更新 handLayoutReference 用于鼠标悬停检测。
    /// </summary>
    private List<CardLayoutData> CalculateAllCurrentLayout(CardDisplay hoverCard)
    {
        List<CardLayoutData> layoutDataList = new List<CardLayoutData>();
        
        if (handDisplays == null || handDisplays.Count == 0) 
        {
            handLayoutReference.Clear();
            return layoutDataList;
        }
        
        int count = handDisplays.Count;
        handLayoutReference.Clear(); 

        // 计算当前手牌需要的总宽度
        float totalCardWidth = (count > 1) ? (count - 1) * cardSpacing : 0f;
        
        // 固定的最大弧形宽度
        float fixedArcWidth = arcBaseWidth; 
        float fixedArcHeight = arcHeight;
        
        int highlightedIndex = -1;
        if (hoverCard != null)
        {
            // IMPORTANT FIX: Ensure IndexOf is used on the current list
            highlightedIndex = handDisplays.IndexOf(hoverCard);
        }
        
        float gapSize = extraSpacingOnHover;
        float totalGapOffset = (highlightedIndex != -1) ? gapSize : 0f;
        
        // 调整理想宽度以考虑悬停间距
        float adjustedTotalWidth = totalCardWidth + totalGapOffset;
        float currentLayoutStart = -adjustedTotalWidth / 2f;


        for (int i = 0; i < count; i++)
        {
            CardDisplay display = handDisplays[i];
            if (display == null) continue;

            float currentXOffset = i * cardSpacing;
            
            // 考虑悬停带来的间距扩散
            if (highlightedIndex != -1 && i > highlightedIndex)
            {
                 // 在悬停卡牌右侧的卡牌需要被推开
                currentXOffset += gapSize;
            }

            float finalTargetX = currentLayoutStart + currentXOffset;
            
            // 实时记录用于碰撞检测的中心X坐标
            handLayoutReference.Add((display, finalTargetX));

            // 将 X 坐标归一化到 Bezier 曲线的 [0, 1] 范围
            float t = (finalTargetX + fixedArcWidth / 2f) / fixedArcWidth;
            t = Mathf.Clamp01(t); 

            // 计算贝塞尔曲线上的位置和切线旋转
            (Vector3 targetPosition, Quaternion targetRotation) = CalculateBezierPoint(t, fixedArcWidth, fixedArcHeight);

            Vector3 finalTargetPosition = targetPosition;
            Vector3 finalTargetRotation = targetRotation.eulerAngles;
            Vector3 finalTargetScale = Vector3.one;

            // Z 轴用于防止卡牌重叠
            finalTargetPosition.z = i * 0.001f; 

            // 处理悬停抬升和缩放
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
                centerX = finalTargetX // 已经记录到 handLayoutReference，但保留在结构体中方便使用
            });
        }
        
        return layoutDataList;
    }


    private void DiscardHandDisplays()
    {
        // Only clear the list and reference, the visual animation (Destroy) is handled in EndPlayerTurn
        handDisplays.Clear();
        handLayoutReference.Clear(); 
        highlightedCard = null;      
    }
    
    /// <summary>
    /// 尝试打出卡牌。
    /// FIX: 签名已修改为接受唯一的 CardDisplay 对象，解决同名卡牌误打问题。
    /// </summary>
    /// <param name="cardDisplay">要打出的卡牌显示对象，用于唯一确定卡牌。</param>
    /// <param name="target">选定的目标角色，如果不需要目标则为 null。</param>
    /// <returns>如果打牌成功则返回 true，否则返回 false。</returns>
    public bool TryPlayCard(CardDisplay cardDisplay, CharacterBase target) // 仅接受 CardDisplay 和 Target
    {
        // 1. VITAL GUARD: 战斗或回合转换中不允许打牌
        if (isTurnInProgress || IsBattleOver) return false;
        
        // 2. NEW GUARD: 如果有卡牌正在播放动画，则阻止打出新卡
        if (isCardBeingPlayed)
        {
            Debug.LogWarning("Card animation is currently in progress. Cannot play a new card yet.");
            return false;
        }
        
        if (cardSystem == null || characterManager == null || cardDisplay == null) return false;
        
        // 从 CardDisplay 获取唯一的 CardData
        CardData card = cardDisplay.GetCardData();
        if (card == null)
        {
             Debug.LogError("The played CardDisplay does not contain valid CardData.");
             return false;
        }
        
        // Ensure card can be played (energy check)
        if (!cardSystem.CanPlayCard(card)) return false;
        
        CharacterBase actualTarget = target;
        
        if (cardSystem.CardNeedsSelectedTarget(card))
        {
            if (actualTarget == null)
            {
                // Auto-target the first living enemy
                CharacterBase firstEnemy = characterManager.GetAllEnemies().FirstOrDefault(e => e != null && e.currentHp > 0);
                
                if (firstEnemy != null)
                {
                    actualTarget = firstEnemy; 
                    Debug.Log($"Auto-targeting: {actualTarget.characterName}");
                }
            }
            
            if (actualTarget == null)
            {
                
                Debug.LogWarning($"Card {card.cardName} requires a target, but no living enemies were found.");
                return false;
            }
        }
        
        if (cardSystem.CardNeedsSelectedTarget(card) && !IsValidTarget(card, actualTarget)) return false;

        // *** FIX: 直接使用传入的 cardDisplay，避免查找重复卡牌的问题 ***
        CardDisplay displayToRemove = cardDisplay;

        // 3. VITAL LOCK: 通过所有检查后，设置打牌锁
        isCardBeingPlayed = true;

        // Spend Energy
        cardSystem.SpendEnergy(card.energyCost); 
        
        Debug.Log($"Successfully played {card.cardName}，remaining energy: {cardSystem.CurrentEnergy}");
        
        // 移除手牌列表中的引用 (必须成功，因为是从列表中传递的)
        handDisplays.Remove(displayToRemove);
        if (highlightedCard == displayToRemove) highlightedCard = null;

        
        Transform targetTransform = actualTarget != null ? actualTarget.transform : handContainer; 

        AnimatePlayCard(
            card, 
            displayToRemove.transform, // 使用传入的 CardDisplay 对象的 transform
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

        // 1. Move to Center Position (Play Animation)
        playSequence.Append(
            cardTransform.DOMove(centerPos.position, playCardDuration)
                        .SetEase(playToCenterEaseType) 
        );
        
        playSequence.Join(cardTransform.DORotate(Vector3.zero, playCardDuration).SetEase(playToCenterEaseType));
        playSequence.Join(cardTransform.DOScale(Vector3.one, playCardDuration).SetEase(playToCenterEaseType));

        // 2. Idle for effect trigger
        playSequence.AppendInterval(centerIdleDuration); 
        
        // 3. Execute Card Effect
        playSequence.AppendCallback(() => {
            Debug.Log($"Card effect {card.cardName} triggered!");
            
            try
            {
                CharacterBase targetCharacter = targetTransform.GetComponent<CharacterBase>();
                
                CharacterBase source = characterManager.GetActiveHero();
                card.ExecuteEffects(source, targetCharacter, cardSystem); 
                
                // Play card system logic
                cardSystem.PlayCard(card); 
                
                // NEW: Use the new postPlayRepositionDuration for an animated layout update
                UpdateHandLayout(postPlayRepositionDuration); 
                
                // Key: Check for immediate death after card effect execution
                CheckBattleEnd(); 
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error executing card effect or updating state for {card.cardName}: {ex.Message}");
            }
        });
        
        playSequence.AppendInterval(postExecutionDelay);

        // 4. Move to Discard Pile
        playSequence.Append(
            cardTransform.DOMove(discardPileTransform.position, discardDuration) 
                        .SetEase(playToDiscardEaseType) 
        );
            
        // 5. Destroy Object and Unlock
        playSequence.AppendCallback(() => {
            if (cardTransform != null && cardTransform.gameObject != null)
            {
                Destroy(cardTransform.gameObject);
            }
            
            // VITAL UNLOCK: 释放卡牌播放锁，必须在动画和销毁后
            isCardBeingPlayed = false;
            Debug.Log("Card Play Lock Released.");
        });
    }
    
    private (Vector3 position, Quaternion rotation) CalculateBezierPoint(float t, float width, float height)
    {
        Vector3 p0 = new Vector3(-width / 2f, 0f, 0f);
        Vector3 p1 = new Vector3(0f, height, 0f);
        Vector3 p2 = new Vector3(width / 2f, 0f, 0f);

        float oneMinusT = 1f - t;
        
        // Position on the Quadratic Bezier curve
        Vector3 position = 
            (oneMinusT * oneMinusT * p0) + 
            (2f * oneMinusT * t * p1) + 
            (t * t * p2);

        // Tangent calculation for rotation
        Vector3 p0_p1 = p1 - p0; 
        Vector3 p1_p2 = p2 - p1; 

        Vector3 tangent = 
            (2f * oneMinusT * p0_p1) + 
            (2f * t * p1_p2);
        
        float angleZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg; 
        
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleZ);

        return (position, rotation);
    }
    
    /// <summary>
    /// Updates the layout of the hand cards.
    /// </summary>
    /// <param name="duration">The duration of the animation. Use 0 for instant repositioning.</param>
    public void UpdateHandLayout(float duration) 
    { 
        // 1. Calculate all layout data and update hover reference
        List<CardLayoutData> layoutData = CalculateAllCurrentLayout(highlightedCard);
        
        if (handDisplays.Count == 0) return;

        // 2. Apply layout data (hover or post-play adjustment)
        for (int i = 0; i < handDisplays.Count; i++)
        {
            CardDisplay display = handDisplays[i];
            CardLayoutData targetData = layoutData[i];

            // Execute animation or instant setting
            display.transform.DOLocalMove(targetData.position, duration).SetEase(Ease.OutQuad);
            display.transform.DOLocalRotate(targetData.rotation.eulerAngles, duration);
            display.transform.DOScale(targetData.scale, duration); 
        }
    }

    /// <summary>
    /// Checks if the battle has ended. If so, calls EndBattle.
    /// </summary>
    public bool CheckBattleEnd()
    {
        if (IsBattleOver) return true; // Battle already over

        if (characterManager == null) return false; 
        
        // Victory Condition: All enemies in the ActiveEnemies list are dead
        bool allEnemiesDead = characterManager.ActiveEnemies.Count == 0;
        
        // Defeat Condition: Hero is dead
        bool heroDead = characterManager.GetActiveHero() == null || characterManager.GetActiveHero().currentHp <= 0;

        if (allEnemiesDead)
        {
            Debug.Log("[Battle End] VICTORY!");
            EndBattle(); 
            // TODO: Trigger victory handling logic
            return true;
        }
        else if (heroDead)
        {
             Debug.Log("[Battle End] DEFEAT!");
             EndBattle(); 
             // TODO: Trigger defeat handling logic
             return true;
        }
        
        // Battle continues
        return false;
    }
    
}