
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; 
using UnityEngine.EventSystems; 
using System.Collections; 
using TMPro; 
using System; 
using ScavengingGame;

public class BattleManager : MonoBehaviour
{
    // 修复单例模式
    public static BattleManager Instance { get; private set; }
    
    [Header("战斗角色清理设置")]
    [Tooltip("角色死亡后，其游戏对象被销毁前的延迟时间（秒），用于播放死亡动画。")]
    public float characterDestroyDelay = 1.5f;
    
    // 战斗状态：用于追踪战斗是否结束
    public bool IsBattleOver { get; private set; } = false; 
    
    // VITAL: 回合锁，防止连续点击或异步逻辑冲突
    private bool isTurnInProgress = false; 
    
    // VITAL NEW: 打牌锁，防止连续打牌冲突
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
    public float repositionDuration = 0.3f; // 整理阶段的动画时长
    
    [Range(0.1f, 1f)]
    [Tooltip("打出卡牌后，剩余卡牌重新布局的动画时长。")]
    public float postPlayRepositionDuration = 0.2f; // NEW: 打牌后整理时长
    
    [Range(0.05f, 1f)]
    public float drawDuration = 0.5f; // 抽牌阶段单张卡牌的时长
    
    [Range(0.001f, 0.2f)]
    public float drawCardDelay = 0.08f; // 抽牌阶段的卡牌间隔
    
    [Range(0.05f, 0.5f)]
    public float playCardDuration = 0.1f; 
    
    [Range(0.05f, 0.5f)]
    public float centerIdleDuration = 0.12f; 
    
    [Range(0.05f, 0.5f)]
    public float postExecutionDelay = 0.1f; // Delay buffer for turn transition
    
    [Header("抽牌整理动画参数")]
    [Range(0.01f, 0.3f)]
    public float layoutCardDelay = 0.05f; // 整理阶段单张卡牌的间隔
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
        // 修复单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"销毁重复的BattleManager实例: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        if (handContainer != null && handContainerRect == null)
        {
            handContainerRect = handContainer.GetComponent<RectTransform>();
        }
        
        // 确保组件引用
        if (cardSystem == null) cardSystem = GetComponentInChildren<CardSystem>();
        if (characterManager == null) characterManager = CharacterManager.Instance;
    }
    
    [Header("调试用遭遇战数据")]
    public EnemyEncounterData testEncounterData; // 在 Inspector 中拖入你配置的多敌人遭遇战

    [Header("调试控制")]
    public bool autoStartTestBattle = false;



    void Start()
    {
        // 只有开启自动战斗模式时才会自动开始
        if (autoStartTestBattle)
        {
            // 优先使用配置的遭遇战数据
            if (testEncounterData != null)
            {
                Debug.Log($"自动开始遭遇战: {testEncounterData.encounterName}");
                InitializeBattle(testEncounterData);
            }
            else if (defaultEnemyDataAsset != null) 
            {
                Debug.Log("自动开始测试战斗（单个敌人）...");
                EnemyEncounterData testEncounter = ScriptableObject.CreateInstance<EnemyEncounterData>();
                testEncounter.enemyList = new List<EnemyData> { defaultEnemyDataAsset };
                testEncounter.encounterName = "测试遭遇战";
                InitializeBattle(testEncounter);
            }
        }
        else
        {
            Debug.Log("BattleManager: 自动战斗已禁用，等待外部调用");
        }
    }

    // 由GameStateManager调用以初始化战斗
    public void InitializeBattle(EnemyEncounterData encounterData)
    {
        // ⭐ 关键修复：检查 encounterData 是否为 null，如果是则使用默认值 ⭐
        if (encounterData == null) 
        {
            Debug.LogError("InitializeBattle: encounterData 为 null，尝试使用默认遭遇战数据");
            
            // 尝试使用 testEncounterData
            if (testEncounterData != null)
            {
                encounterData = testEncounterData;
                Debug.Log("InitializeBattle: 使用 testEncounterData");
            }
            // 如果 testEncounterData 也没有，尝试使用 defaultEnemyDataAsset 创建默认遭遇战
            else if (defaultEnemyDataAsset != null)
            {
                Debug.Log("InitializeBattle: 创建默认遭遇战数据");
                encounterData = ScriptableObject.CreateInstance<EnemyEncounterData>();
                encounterData.enemyList = new List<EnemyData> { defaultEnemyDataAsset };
                encounterData.encounterName = "默认遭遇战";
            }
            else
            {
                Debug.LogError("InitializeBattle: 没有可用的遭遇战数据！无法开始战斗。");
                return;
            }
        }

        // 1. 必须先初始化卡组 (如果还没初始化的话)
        if (cardSystem != null) 
        {
            cardSystem.SetupDeck(); // 确保抽牌堆里有牌
        }
        else
        {
            Debug.LogError("InitializeBattle: cardSystem 为 null");
            return;
        }

        // 2. 生成角色
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetupEncounter(encounterData);
        }
        else
        {
            Debug.LogError("InitializeBattle: GameFlowManager.Instance 为 null");
            return;
        }
        
        // 3. 启动战斗 (这会设置 Round = 1)
        StartBattle();
    }
    
    // 战斗结束时调用（胜利或失败）
    /// <summary>
/// 统一的战斗结束入口
/// </summary>
/// <param name="isVictory">是否胜利</param>

    
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
             characterManager.RegisterHero(heroChar);
             
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
            
            characterManager.RegisterEnemy(enemyChar);
            Debug.Log("Created Mock Enemy for testing.");
        }
    }
    
    public void StartBattle()
    {
        IsBattleOver = false; // Ensure battle state is reset
        CurrentRound = 1; 
        
        if (characterManager == null)
        {
            Debug.LogError("StartBattle: characterManager is null");
            return;
        }
        
        if (characterManager.GetActiveHero() != null)
        {
            CalculateAllEnemyIntents();
            StartPlayerTurn();
        }
        else
        {
             Debug.LogError("Cannot start battle: No active hero (activeHero).");
        }
    }

    public void StartPlayerTurn()
    {
        // Key: Check battle end immediately before starting the turn
        if (CheckBattleEnd()) return;
        // 调用 GameFlowManager 显示弹窗
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ShowPopup("Player round"); 
        }
        if (characterManager != null)
        {
            characterManager.AtStartOfTurn(); 
        }

        if (cardSystem == null) 
        {
            Debug.LogError("StartPlayerTurn: cardSystem is null");
            return;
        }
        
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
    // VITAL GUARD: 如果回合正在进行中或正在打牌，则忽略
    if (isTurnInProgress || isCardBeingPlayed)
    {
        Debug.LogWarning("Action in progress. Ignoring EndPlayerTurn.");
        return;
    }
    
    isTurnInProgress = true; // 立即加锁

    Debug.Log("--- Player Turn End ---");
    
    float discardDuration = 0.2f;
    List<CardDisplay> cardsToDiscard = handDisplays.ToList(); 
    bool cardsWereDiscarded = cardsToDiscard.Count > 0;

    // 1. 弃牌动画
    foreach (var display in cardsToDiscard) 
    {
        if (display != null && discardPileLocationTransform != null)
        {
            display.transform.DOMove(discardPileLocationTransform.position, discardDuration)
                .SetEase(playToDiscardEaseType)
                .OnComplete(() => Destroy(display.gameObject));
        }
    }
    
    DiscardHandDisplays();
    if (cardSystem != null) cardSystem.DiscardHand(); 
    
    float totalWaitTime = cardsWereDiscarded ? discardDuration + postExecutionDelay : postExecutionDelay;

    // 2. 动画完成后执行逻辑切换
    DOVirtual.DelayedCall(totalWaitTime, () => 
    {
        if (characterManager != null)
        {
            characterManager.DecrementSpecificGroupBlockDurations(characterManager.allEnemies);
            foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0))
            {
                enemy.AtEndOfTurn(); 
            }
        }
        
        // --- 核心修复点 1: CS1503 错误 ---
        // 错误原因：不能直接把 StartCoroutine(...) 传给 OnComplete 或 DelayedCall
        // 修复方法：使用 Lambda 表达式 () => StartCoroutine(...)
        DOVirtual.DelayedCall(postExecutionDelay, () => StartCoroutine(StartEnemyTurn()));
    });
}

private IEnumerator StartEnemyTurn()
{
    if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ShowPopup("Enemy Turn");
        }
        yield return new WaitForSeconds(1.0f); // 给弹窗一点显示时间
    // Key: Check battle end immediately before starting the turn
    if (CheckBattleEnd()) 
    {
        isTurnInProgress = false; // VITAL: If battle ends here, release lock immediately
        yield break;
    }

    Debug.Log($"--- Enemy Turn Start (Round {CurrentRound}) ---");
    
    if (characterManager == null)
    {
        Debug.LogError("StartEnemyTurn: characterManager is null");
        isTurnInProgress = false;
        yield break;
    }
    
    CharacterBase activeHero = characterManager.GetActiveHero();
    if (activeHero == null)
    {
        CheckBattleEnd();
        isTurnInProgress = false; // VITAL: If hero dies, release lock
        yield break;
    }
    
    if (characterManager != null)
    {
        characterManager.AtStartOfTurn();
    }
    
    // 使用协程执行敌人回合，确保每个敌人依次攻击
    StartCoroutine(ExecuteEnemyTurnSequentially());
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

        StartPlayerTurn(); // Enter new player turn, which calls DrawCards
    }

    /// <summary>
    /// Receives a synchronous signal from CharacterBase, executed after the death animation starts.
    /// Performs immediate removal and flow check.
    /// </summary>
    public void HandleDyingCharacterCleanup(CharacterBase dyingCharacter)
    {
        if (dyingCharacter == null) return;

        // 修改：不需要手动从 ActiveEnemies 中移除，因为 GetActiveEnemies() 会实时计算
        // 我们只需要确保角色被正确标记为死亡（currentHp <= 0）即可
    
        Debug.Log($"[Death Cleanup] {dyingCharacter.characterName} death event captured.");
        
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
        
        if (deadCharacterObject == null)
        {
            Debug.LogWarning("HandleDeathAnimationComplete: deadCharacterObject is null");
            return;
        }
        
        Debug.Log($"[Death Animation Complete] Destroying object: {deadCharacterObject.name}");
        Destroy(deadCharacterObject);
    }
    
    /// <summary>
    /// Central cleanup point for when the battle ends.
    /// </summary>
    

    private void CalculateAllEnemyIntents()
    {
        if (characterManager == null)
        {
            Debug.LogError("CalculateAllEnemyIntents: characterManager is null");
            return;
        }
       
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
            Debug.LogError($"DrawCards: cardSystem or characterManager is null. cardSystem={cardSystem}, characterManager={characterManager}");
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
    public bool TryPlayCard(CardDisplay cardDisplay, CharacterBase target)
    {
        // 1. VITAL GUARD: 战斗或回合转换中不允许打牌
        if (isTurnInProgress || IsBattleOver) return false;
        
        // 2. NEW GUARD: 如果有卡牌正在播放动画，则阻止打出新卡
        if (isCardBeingPlayed)
        {
            Debug.LogWarning("Card animation is currently in progress. Cannot play a new card yet.");
            return false;
        }
        
        if (cardSystem == null || characterManager == null || cardDisplay == null) 
        {
            Debug.LogError($"TryPlayCard: 必要组件为空。cardSystem={cardSystem}, characterManager={characterManager}, cardDisplay={cardDisplay}");
            return false;
        }
        
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
        if (cardTransform == null || discardPileTransform == null)
        {
            Debug.LogError("AnimatePlayCard: cardTransform or discardPileTransform is null");
            isCardBeingPlayed = false; // 释放锁
            return;
        }
        
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
        // 1. 预检：如果没有手牌，直接返回
        if (handDisplays == null || handDisplays.Count == 0) return;

        // 2. 一次性计算出所有卡牌的目标布局数据
        List<CardLayoutData> layoutData = CalculateAllCurrentLayout(highlightedCard);
        
        // 3. 遍历手牌列表，应用布局
        for (int i = 0; i < handDisplays.Count; i++)
        {
            CardDisplay card = handDisplays[i];
            if (card == null) continue;

            // ⭐ 关键点：如果卡牌正在被拖拽，跳过布局计算，完全交给 CardDisplay 的 OnDrag 处理
            if (card.IsDragging) 
            {
                continue; 
            }

            // 4. 获取当前卡牌对应的布局目标 (确保索引安全)
            if (i >= layoutData.Count) break;
            CardLayoutData targetData = layoutData[i];

            // 5. 执行平滑动画
            // 使用 SetEase(Ease.OutQuad) 让手牌排列看起来更有弹性
            card.transform.DOLocalMove(targetData.position, duration).SetEase(Ease.OutQuad);
            card.transform.DOLocalRotate(targetData.rotation.eulerAngles, duration).SetEase(Ease.OutQuad);
            card.transform.DOScale(targetData.scale, duration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Checks if the battle has ended. If so, calls EndBattle.
    /// </summary>
    // 在 BattleManager.cs 中处理敌人回合
private IEnumerator ExecuteEnemyTurnSequentially()
{
    // 获取所有存活的敌人
    var enemies = characterManager.GetAllEnemies().Where(e => e.currentHp > 0).ToList();
    
    Debug.Log($"[战斗流程] 敌人回合开始，存活敌人数量: {enemies.Count}");
    
    // 依次执行每个敌人的行动
    foreach (var enemy in enemies) 
    {
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        
        if (enemyAI != null && enemy.currentHp > 0) 
        {
            Debug.Log($"[战斗流程] {enemy.characterName} 开始行动...");
            
            // 1. 计算意图
            enemyAI.CalculateIntent(characterManager.GetActiveHero(), CurrentRound); 
            
            // 2. 更新UI显示意图
            EnemyDisplay display = enemy.GetComponentInChildren<EnemyDisplay>();
            if (display != null)
            {
                display.RefreshIntent(enemyAI.nextIntent, enemyAI.intentValue);
            }
            
            // 3. 短暂延迟，让玩家看到意图
            yield return new WaitForSeconds(0.8f);
            
            // 4. 执行攻击
            Debug.Log($"[战斗流程] {enemy.characterName} 执行攻击...");
            
            // ⭐ 关键修改：确保获取英雄引用
            CharacterBase hero = characterManager.GetActiveHero();
            if (hero == null || hero.currentHp <= 0)
            {
                Debug.LogWarning($"[战斗流程] 英雄已死亡，跳过敌人行动");
                break;
            }
            
            // 执行攻击动作
            Sequence enemyAction = enemyAI.PerformAction(hero, CurrentRound);
            
            // 5. 关键：等待攻击动画和扣血完成
            if (enemyAction != null)
            {
                Debug.Log($"[战斗流程] {enemy.characterName} 等待动画完成...");
                
                // 等待动画序列完成
                yield return enemyAction.WaitForCompletion();
                
                Debug.Log($"[战斗流程] {enemy.characterName} 动画完成");
            }
            else
            {
                Debug.LogWarning($"[战斗流程] {enemy.characterName} 的 enemyAction 为空");
                // 即使没有序列，也要等待一个基本的动画时间
                yield return new WaitForSeconds(0.6f);
            }
            
            // 6. 攻击间隔
            yield return new WaitForSeconds(0.4f);
            
            // 7. 检查英雄是否死亡
            if (characterManager.GetActiveHero() == null || characterManager.GetActiveHero().currentHp <= 0)
            {
                Debug.Log("[战斗流程] 英雄在敌人攻击中死亡！");
                
                // 触发英雄死亡动画或效果
                if (characterManager.GetActiveHero() != null)
                {
                    CharacterBase deadHero = characterManager.GetActiveHero();
                    Debug.Log($"[战斗流程] 英雄 {deadHero.characterName} 已死亡");
                    
                    // 这里可以添加英雄死亡的特效或UI反馈
                    // 例如：英雄图像闪烁、变灰等
                }
                
                break;
            }
        }
    }
    
    Debug.Log($"[战斗流程] 所有敌人行动完成，准备回合转换。"); 

    if (characterManager != null)
    {
        // 敌人回合结束 -> 减少玩家的格挡持续时间
        Debug.Log("[战斗流程] 减少玩家格挡和状态持续时间。");
        characterManager.DecrementSpecificGroupBlockDurations(characterManager.allHeroes);
        
        if (characterManager.GetActiveHero() != null)
        {
            characterManager.GetActiveHero().AtEndOfTurn(); 
        }
    }
    
    // 延迟一点时间，让玩家看清结果
    yield return new WaitForSeconds(0.8f);
    
    OnEnemyTurnCleanupComplete();
}

  // 替换掉之前所有的 EndBattle 和 CheckBattleOver
public void EndBattle(bool isVictory)
{
    // 1. 防止重复进入逻辑
    if (IsBattleOver) return;
    IsBattleOver = true;

    // 2. 锁定操作：防止弹窗时还能点结束回合或出牌
    isTurnInProgress = false;
    isCardBeingPlayed = false;
    this.StopAllCoroutines(); 

    Debug.Log($"[战斗结束] 结果: {(isVictory ? "胜利" : "失败")}");

    // 3. UI 表现：触发弹窗
    if (GameFlowManager.Instance != null)
    {
        string msg = isVictory ? "Player Win" : "fail";
        GameFlowManager.Instance.ShowPopup(msg, 3.0f);
    }

    // 4. 数据结算：发放奖励并通知全局管理器
    List<ItemData> rewards = new List<ItemData>();
    if (isVictory)
    {
        // 这里可以执行你的奖励生成逻辑
        // rewards = GenerateBattleRewards();
    }

    if (GameStateManager.Instance != null)
    {
        GameStateManager.Instance.EndBattle(isVictory, rewards);
    }
}

public bool CheckBattleEnd()
{
    if (IsBattleOver) return true;
    if (characterManager == null) return false;

    // 根据存活情况计算结果
    bool allEnemiesDead = characterManager.ActiveEnemies.Count == 0;
    bool heroDead = characterManager.GetActiveHero() == null || characterManager.GetActiveHero().currentHp <= 0;

    if (allEnemiesDead)
    {
        EndBattle(true); // 统一调用上面那个带参数的方法
        return true;
    }
    else if (heroDead)
    {
        EndBattle(false);
        return true;
    }

    return false;
}




}
