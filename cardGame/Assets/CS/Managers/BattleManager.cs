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
using SlayTheSpireMap;

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
    // handContainerRect moved to BattleVisualizer
    public Transform discardPileLocationTransform; 
    public Transform drawPileLocationTransform;    
    public Transform playCenterTransform; 

    public List<CardDisplay> handDisplays = new List<CardDisplay>(); 
    
    // Layout and Hover Logic moved to BattleVisualizer

    [Header("回合状态")]
    public int CurrentRound { get; private set; } = 0; 
    public int cardsToDraw = 5; 
    
    // Layout settings moved to BattleVisualizer
    
    [Header("动画参数")]
    // Animation settings moved to BattleVisualizer

    [Header("调试用默认资产")]
    public EnemyData defaultEnemyDataAsset; 
    public RoundBasedStrategy defaultEnemyStrategyAsset; 

    private CardDisplay highlightedCard = null; // Deprecated, use BattleVisualizer

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
        
        // handContainerRect assignment moved to BattleVisualizer
        
        // 确保组件引用
        if (cardSystem == null) cardSystem = GetComponentInChildren<CardSystem>();
        if (characterManager == null) characterManager = CharacterManager.Instance;
        
        ConfigureVisualizer();
    }
    
    private void ConfigureVisualizer()
    {
        if (BattleVisualizer.Instance != null)
        {
            if (handContainer != null) 
                BattleVisualizer.Instance.handContainerRect = handContainer.GetComponent<RectTransform>();
            
            // Assign Transform references to Visualizer
            BattleVisualizer.Instance.drawPileLocationTransform = drawPileLocationTransform;
            BattleVisualizer.Instance.discardPileLocationTransform = discardPileLocationTransform;
            BattleVisualizer.Instance.playCenterTransform = playCenterTransform;
        }
    }
    
    [Header("调试用遭遇战数据")]
    public EnemyEncounterData testEncounterData; // 在 Inspector 中拖入你配置的多敌人遭遇战

    [Header("调试控制")]
    public bool autoStartTestBattle = false;



    // BattleManager.cs
private IEnumerator Start()
    {
        Debug.Log("[Battle] 正在初始化战斗场景...");

        // 1. 【核心修复】不再使用 Mock Hero，而是从全局数据生成真正的英雄
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SpawnHeroFromData(); 
            
            // 生成敌人
            var baseEncounter = GameDataManager.Instance.battleEncounterData;
            var encounter = baseEncounter as EnemyEncounterData;
            
            if (encounter != null)
            {
                GameFlowManager.Instance.InitializeBattleFromData(encounter);
            }
            else
            {
                if (baseEncounter == null)
                    Debug.LogError("[Battle] GameDataManager.battleEncounterData 为空！请检查地图节点点击逻辑。");
                else
                    Debug.LogError($"[Battle] 遭遇战数据类型不匹配！期望 EnemyEncounterData，实际是 {baseEncounter.GetType().Name}。请确保 MapNodeData 配置的是 EnemyEncounterData 类型的资产。");
            }
        }
        else
        {
             Debug.LogError("[Battle] 找不到 GameFlowManager 实例！");
        }

        // 2. 等待一帧，让注册和实例化完成
        yield return new WaitForEndOfFrame();

    // 3. 【修正类型转换】获取真正的 Hero 实例
    // 这里使用 (Hero) 或 as Hero 强转，因为现在生成的是真正的英雄 Prefab
    Hero hero = characterManager.GetActiveHero() as Hero;
    
    if (hero != null)
    {
        hero.SyncFromGlobal(); // 此时同步 30 血量
        hero.GetComponentInChildren<CharacterUIDisplay>(true)?.Initialize(hero);
        Debug.Log($"[Battle] 英雄 {hero.characterName} 加载成功，当前血量: {hero.currentHp}");
    }
    else
    {
        Debug.LogError("[Battle] 严重错误：未能找到英雄实例！请检查 SpawnHeroFromData 是否正常运行。");
    }

    // 4. 加载卡组
    cardSystem.LoadDeckFromGlobal();
    
    // Debug: 打印当前抽牌堆内容，确认新卡是否加入
    if (cardSystem != null)
    {
        Debug.Log($"[Battle] 卡组加载完成。Master Deck: {cardSystem.masterDeck.Count} 张, Draw Pile: {cardSystem.drawPile.Count} 张。");
        string deckList = string.Join(", ", cardSystem.drawPile.Select(c => c.cardName));
        Debug.Log($"[Battle] 当前抽牌堆: {deckList}");
    }

    // Ensure round starts at 1 if not initialized
    if (CurrentRound <= 0) CurrentRound = 1;

    // 5. 开启回合
    yield return new WaitForSeconds(0.5f);
    
    // Ensure intents are calculated for the first round
    CalculateAllEnemyIntents();
    
    StartPlayerTurn(); 
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
        // Hover selection moved to BattleVisualizer
    }
    
    public bool IsInteractionLocked()
    {
        return isTurnInProgress || isCardBeingPlayed;
    }

    // Proxy methods for CardDisplay compatibility
    public void UpdateHandLayout(float duration)
    {
        if (BattleVisualizer.Instance != null)
            BattleVisualizer.Instance.UpdateHandLayout(duration);
    }

    public void UnhighlightCard(CardDisplay card)
    {
        if (BattleVisualizer.Instance != null)
            BattleVisualizer.Instance.UnhighlightCard(card);
    }

    public CardDisplay GetHighlightedCard()
    {
        return BattleVisualizer.Instance != null ? BattleVisualizer.Instance.GetHighlightedCard() : null;
    }
    
    // Removed local HandleHoverSelection, SetHighlightedCard implementation
    
    private void SetupMockCharactersIfNecessary()
    {
   
        
      

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
    
    List<CardDisplay> cardsToDiscard = handDisplays.ToList(); 
    bool cardsWereDiscarded = cardsToDiscard.Count > 0;

    // 1. 弃牌动画 (Delegate to Visualizer)
    if (BattleVisualizer.Instance != null)
    {
        BattleVisualizer.Instance.AnimateDiscardHand(cardsToDiscard, () => {
             // Animation complete logic could be here, but we use DelayedCall below for legacy timing consistency
        });
    }
    else
    {
        // Fallback destruction
        foreach(var c in cardsToDiscard) if(c!=null) Destroy(c.gameObject);
    }
    
    DiscardHandDisplays();
    if (cardSystem != null) cardSystem.DiscardHand(); 
    
    float discardDuration = 0.2f; // Approximate duration from Visualizer
    float delay = (BattleVisualizer.Instance != null) ? BattleVisualizer.Instance.postExecutionDelay : 0.1f;
    float totalWaitTime = cardsWereDiscarded ? discardDuration + delay : delay;

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
        float finalDelay = (BattleVisualizer.Instance != null) ? BattleVisualizer.Instance.postExecutionDelay : 0.1f;
        DOVirtual.DelayedCall(finalDelay, () => StartCoroutine(StartEnemyTurn()));
    });
}
// BattleManager.cs

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
    
    // 获取角色组件
    CharacterBase deadCharacter = deadCharacterObject.GetComponent<CharacterBase>();
    if (deadCharacter != null)
    {
        Debug.Log($"[死亡动画完成] {deadCharacter.characterName} 的动画播放完毕");
        
        // 从管理器中移除角色
        if (characterManager != null)
        {
            characterManager.UnregisterCharacter(deadCharacter);
        }
        
        // ⭐⭐ 关键修改：在死亡动画完成后检查战斗是否结束
        // 延迟一小段时间，确保角色完全移除
        StartCoroutine(CheckBattleEndAfterDeathAnimation());
    }
    
    // 销毁游戏对象
    Destroy(deadCharacterObject);
}
   private IEnumerator CheckBattleEndAfterDeathAnimation()
    {
        // 等待一帧，确保角色已经正确从管理器中移除
        yield return null;
        
        // 现在检查战斗是否结束
        CheckBattleEndDelayed();
    }


    public void CheckBattleEndDelayed()
    {
        if (IsBattleOver) return;
        
        StartCoroutine(CheckBattleEndDelayedCoroutine());
    }

    private IEnumerator CheckBattleEndDelayedCoroutine()
    {
        // 等待一小段时间，确保所有死亡动画都开始播放
        yield return new WaitForSeconds(0.5f);
        
        // 然后检查战斗是否结束
        CheckBattleEnd();
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
            
            // Register with Visualizer
            if (BattleVisualizer.Instance != null)
                BattleVisualizer.Instance.RegisterCard(display);

            // 设置起点：卡牌区相对位置
            display.transform.localPosition = drawPileLocalPos;
            display.transform.localRotation = drawPileLocationTransform.localRotation;
            display.transform.localScale = Vector3.one; 
        }
        
        // --- 阶段 1: 抽牌到中央堆叠 (Draw to Center Pile) ---
        if (BattleVisualizer.Instance != null)
        {
            BattleVisualizer.Instance.AnimateCardDraw(newlyDrawnDisplays, drawPileLocalPos, receiveCenterLocalPos, () => {
                isTurnInProgress = false; 
                Debug.Log("[DrawCards] Animation complete. Turn Lock Released.");
            });
        }
        else
        {
             isTurnInProgress = false;
        }
    }
    
    // CalculateAllCurrentLayout moved to BattleVisualizer
    
    private void DiscardHandDisplays()
    {
        // Only clear the list and reference, the visual animation (Destroy) is handled in EndPlayerTurn
        foreach(var display in handDisplays)
        {
             if (BattleVisualizer.Instance != null)
                BattleVisualizer.Instance.UnregisterCard(display);
        }
        handDisplays.Clear();
        // handLayoutReference.Clear(); // Moved
        // highlightedCard = null;      // Moved logic
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
        
        // VITAL FIX: 立即通知 Visualizer 移除卡牌，触发剩余卡牌的重新布局填补空缺
        if (BattleVisualizer.Instance != null)
        {
             BattleVisualizer.Instance.UnregisterCard(displayToRemove, true);
        }

        Transform targetTransform = actualTarget != null ? actualTarget.transform : handContainer; 

        if (BattleVisualizer.Instance != null)
        {
            BattleVisualizer.Instance.AnimatePlayCardSequence(card, displayToRemove.transform, targetTransform, 
                // Effect Callback
                () => {
                    Debug.Log($"Card effect {card.cardName} triggered!");
                    try
                    {
                        CharacterBase targetCharacter = targetTransform.GetComponent<CharacterBase>();
                        CharacterBase source = characterManager.GetActiveHero();
                        card.ExecuteEffects(source, targetCharacter, cardSystem); 
                        
                        cardSystem.PlayCard(card); 
                        
                        // Use Visualizer duration
                        float duration = BattleVisualizer.Instance.postPlayRepositionDuration;
                        UpdateHandLayout(duration); 
                        CheckBattleEnd(); 
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error executing card effect or updating state for {card.cardName}: {ex.Message}");
                    }
                },
                // Completion Callback
                () => {
                    isCardBeingPlayed = false;
                    Debug.Log("Card Play Lock Released.");
                }
            );
        }
        else
        {
             Debug.LogError("BattleVisualizer instance missing. Cannot animate play card.");
             isCardBeingPlayed = false;
        }
        
        return true;
    }

    // AnimatePlayCardSequence moved to BattleVisualizer
    
    // CalculateBezierPoint moved to BattleVisualizer
    
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

    /// <summary>
    /// 从资源中加载所有卡牌数据，并随机选择指定数量。
    /// </summary>
    private List<CardData> GetRandomRewardCards(int count)
    {
        List<CardData> allCards = new List<CardData>();
        
        // 加载 Resources/Cards 和 Resources/CardData 下的所有卡牌
        allCards.AddRange(Resources.LoadAll<CardData>("Cards"));
        allCards.AddRange(Resources.LoadAll<CardData>("CardData"));
        
        // 去重 (如果有重复 ID)
        allCards = allCards.GroupBy(c => c.cardID).Select(g => g.First()).ToList();
        
        if (allCards.Count == 0)
        {
            Debug.LogWarning("No cards found in Resources/Cards or Resources/CardData!");
            return new List<CardData>();
        }

        // 随机洗牌并取前 count 个
        // 简单随机算法
        List<CardData> selected = allCards.OrderBy(x => UnityEngine.Random.value).Take(count).ToList();
        
        return selected;
    }

    public void EndBattle(bool isVictory)
    {
        // 1. 防止重复进入逻辑
        if (IsBattleOver) return;
        IsBattleOver = true;

        // 2. 锁定操作：防止弹窗时还能点结束回合或出牌
        isTurnInProgress = false;
        isCardBeingPlayed = false;
        this.StopAllCoroutines(); 

        if (isVictory)
        {
            Debug.Log("Victory! Processing rewards...");
            
            int gold = 50; // 示例奖励
            
            // 1. 生成 3 张随机卡牌选项
            List<CardData> rewardOptions = GetRandomRewardCards(3);
            
            // 2. 显示奖励面板，并等待玩家选择
            if (BattleUIManager.Instance != null)
            {
                // 确保引用 cardPrefab (如果 UIManager 没有赋值)
                if (BattleUIManager.Instance.cardPrefab == null)
                {
                    BattleUIManager.Instance.cardPrefab = this.cardPrefab;
                }
                
                BattleUIManager.Instance.ShowRewardDisplay(gold, rewardOptions, (selectedCard) => {
                    // 3. 玩家选择卡牌后的回调
                    Debug.Log($"[BattleManager] Callback Received for card: {selectedCard.cardName}");
                    
                    // 保存结果 (金币 + 选中卡牌)
                    if (BattleDataManager.Instance != null)
                    {
                        // 关键修复：保存 selectedCard.name (文件名) 而不是 cardID
                        // 这样 Resources.Load<CardData>($"Cards/{name}") 才能正确找到文件
                        BattleDataManager.Instance.SaveBattleResult(true, gold, selectedCard.name);
                        Debug.Log($"[BattleManager] 调用 BattleDataManager.SaveBattleResult。Card Name: {selectedCard.name}");
                    }
                    else
                    {
                        Debug.LogError("[BattleManager] BattleDataManager.Instance 为 null！无法保存奖励。");
                    }
                    
                    // 显示最终胜利结算 (包含继续按钮)
                    // GameFlowManager.Instance.ShowVictoryPanel("VICTORY");
                    // 修复：移除此调用，因为 ShowVictoryPanel 会再次调用 ShowRewardDisplay 导致死循环刷新。
                    // UI 的后续流程（显示继续按钮）已由 BattleUIManager.ShowRewardDisplay 内部处理。
                });
            }
            else
            {
                // Fallback if UI Manager missing
                Debug.LogError("BattleUIManager missing, auto-selecting first reward.");
                if (rewardOptions.Count > 0)
                {
                    string autoSelectedCardName = rewardOptions[0].name;
                    if (BattleDataManager.Instance != null)
                    {
                        // 同样修复 Fallback 逻辑
                        BattleDataManager.Instance.SaveBattleResult(true, gold, autoSelectedCardName);
                    }
                    else if (GameDataManager.Instance != null)
                    {
                        Debug.LogWarning("[BattleManager] BattleDataManager missing (Auto-Select). Saving directly to GameDataManager.");
                        GameDataManager.Instance.AddGold(gold);
                        GameDataManager.Instance.AddCard(autoSelectedCardName);
                        
                        if (characterManager != null && characterManager.GetActiveHero() != null)
                            GameDataManager.Instance.Health = characterManager.GetActiveHero().currentHp;
                            
                        if (!string.IsNullOrEmpty(GameDataManager.Instance.battleNodeId))
                            GameDataManager.Instance.CompleteNode(GameDataManager.Instance.battleNodeId);
                            
                        GameDataManager.Instance.SaveGameData();
                        GameDataManager.Instance.ClearBattleData();
                    }
                }
                // 同上，防止循环调用
                // GameFlowManager.Instance.ShowVictoryPanel("VICTORY");
                
                // 如果没有 UI，直接触发继续
                GameFlowManager.Instance.OnVictoryContinue();
            }
        }
        else
        {
            Debug.Log("Defeat! Processing loss...");
            if (BattleDataManager.Instance != null)
            {
                // 失败不保存奖励，但可能需要更新状态
                BattleDataManager.Instance.SaveBattleResult(false, 0, null);
            }
            GameFlowManager.Instance.ShowDefeatPanel("DEFEAT");
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
