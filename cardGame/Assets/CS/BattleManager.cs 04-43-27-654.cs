using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // 导入 DG.Tweening 命名空间以解决 CS0246 和 CS0103

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    [Header("系统引用 (必须设置)")]
    public CardSystem cardSystem; 
    public CharacterManager characterManager;

    [Header("UI Config")]
    public GameObject cardPrefab; 
    public Transform handContainer; 
    public Transform discardPileLocationTransform; 
    public Transform drawPileLocationTransform;    
    public Transform playCenterTransform; 

    // UI 显示列表
    public List<CardDisplay> handDisplays = new List<CardDisplay>(); 

    [Header("回合状态")]
    public int CurrentRound { get; private set; } = 0; 
    public int cardsToDraw = 5; 

    [Header("动画参数")]
    public float repositionDuration = 0.3f; 
    public float drawDuration = 0.1f; 
    public float drawCardDelay = 0.01f; 
    public float playCardDuration = 0.1f; 
    public float centerIdleDuration = 0.12f; 
    public float postExecutionDelay = 0.1f; 
    public float hoverTranslateY = 50f; 
    public float hoverScale = 1.1f; 
    
    // 修正 CS0103: The name 'playToCenterEaseType' does not exist in the current context
    public Ease playToCenterEaseType = Ease.OutQuad; 
    public Ease playToDiscardEaseType = Ease.InQuad; 
    
    [Header("调试用默认资产")]
    public EnemyData defaultEnemyDataAsset; 
    public RoundBasedStrategy defaultEnemyStrategyAsset; 

    private CardDisplay highlightedCard = null; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 修正 CS0618: 'Object.FindObjectOfType<T>()' is obsolete
        if (cardSystem == null) cardSystem = FindFirstObjectByType<CardSystem>();
        if (characterManager == null) characterManager = FindFirstObjectByType<CharacterManager>();
        
        SetupMockCharactersIfNecessary();
        
        if (cardSystem != null) cardSystem.SetupDeck();
        
        StartBattle();
    }
    
    private void SetupMockCharactersIfNecessary()
    {
        if (characterManager == null) return;
        
        // 确保有一个主角
        if (characterManager.GetActiveHero() == null)
        {
             GameObject heroObj = new GameObject("Mock Hero", typeof(CharacterBase));
             heroObj.hideFlags = HideFlags.DontSave; 
             CharacterBase heroChar = heroObj.GetComponent<CharacterBase>();
             heroChar.characterName = "Player Hero";
             heroChar.currentHp = heroChar.maxHp;
             characterManager.activeHero = heroChar;
             characterManager.allHeroes.Add(heroChar);
             Debug.Log("Created Mock Hero for testing.");
        }

        // 确保有敌人
        if (characterManager.GetAllEnemies().Count == 0 && defaultEnemyDataAsset != null)
        {
            GameObject enemyObj = new GameObject("Mock Enemy 1", typeof(CharacterBase), typeof(EnemyAI));
            enemyObj.hideFlags = HideFlags.DontSave; 
            
            CharacterBase enemyChar = enemyObj.GetComponent<CharacterBase>();
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
            
            if (enemyAI != null) 
            {
                enemyAI.enemyData = defaultEnemyDataAsset; 
                enemyChar.characterName = defaultEnemyDataAsset.enemyName;
                enemyChar.maxHp = defaultEnemyDataAsset.maxHp;
                enemyChar.currentHp = defaultEnemyDataAsset.maxHp;
            }
            
            characterManager.allEnemies.Add(enemyChar);
             Debug.Log("Created Mock Enemy for testing.");
        }
    }

    // --- 战斗流程控制 ---
    
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
        if (cardSystem == null) return;
        
        cardSystem.ResetEnergy(); 
        DiscardHandDisplays(); 
        cardSystem.DiscardHand(); 

        // 修正 CS0119: CardSystem.DrawCards(int) is a method, which is not valid in the given context
        DrawCards(cardsToDraw); 
        Debug.Log($"--- 玩家回合开始 (回合 {CurrentRound}) ---");
    }

    public void EndPlayerTurn()
    {
        Debug.Log("--- 玩家回合结束 ---");
        DiscardHandDisplays();
        cardSystem.DiscardHand(); 
        
        characterManager.ClearAllBlocks();
        
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

        foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0)) // 确保只对活着的敌人行动
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null) enemyAI.PerformAction(activeHero, CurrentRound);
        }
        
        CurrentRound++;
        
        // 敌人行动后，清除所有格挡
        characterManager.ClearAllBlocks(); 

        CalculateAllEnemyIntents(); 
        CheckBattleEnd(); 
        StartNewTurn();
    }

    private void CalculateAllEnemyIntents()
    {
        CharacterBase activeHero = characterManager.GetActiveHero();
        if (activeHero == null) return;

        foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0)) // 确保只计算活着的敌人意图
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null) enemyAI.CalculateIntent(activeHero, CurrentRound); 
        }
    }
    
    // --- 核心卡牌操作 (UI/动画) ---

    public bool IsValidTarget(CardData card, CharacterBase target)
    {
        if (cardSystem == null) return false;
        return cardSystem.IsValidTarget(card, target);
    }

    public void DrawCards(int count)
    {
        if (cardSystem == null || characterManager == null) return;
        
        // 修正: DrawCards(int) 返回 List<CardData>
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
            
            drawSequence.Append(
                // 动画从抽牌堆位置飞向手牌容器，UpdateHandLayout 负责最终位置
                cardTransform.DOMove(handContainer.position, drawDuration) 
                    .SetEase(Ease.OutQuad) 
                    .SetDelay(i * drawCardDelay)
            );
        }
        
        // 修正: UpdateHandLayout 现在在 BattleManager 中
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
    }
    
    // --- External Interface (TryPlayCard) ---
    
    public bool TryPlayCard(CardData card, CharacterBase target, GameObject cardDisplayObject)
    {
        if (cardSystem == null || characterManager == null || card == null) return false;
        
        if (!cardSystem.CanPlayCard(card)) return false;
        
        if (cardSystem.CardNeedsSelectedTarget(card) && target == null) return false; 
        
        if (cardSystem.CardNeedsSelectedTarget(card) && !IsValidTarget(card, target)) return false;

        cardSystem.SpendEnergy(card.energyCost);
        Debug.Log($"成功打出 {card.cardName}，剩余能量: {cardSystem.CurrentEnergy}");

        CardDisplay displayToRemove = cardDisplayObject.GetComponent<CardDisplay>();
        if (displayToRemove != null)
        {
            handDisplays.Remove(displayToRemove);
            if (highlightedCard == displayToRemove) highlightedCard = null;
        }
        
        // 确保 targetTransform 存在
        Transform targetTransform = target != null ? target.transform : handContainer; 

        AnimatePlayCard(
            card, 
            cardDisplayObject.transform, 
            targetTransform,            
            discardPileLocationTransform         
        );
        
        return true;
    }

    // --- 动画方法: 卡牌打出 ---
    
    public void AnimatePlayCard(CardData card, Transform cardTransform, Transform targetTransform, Transform discardPileTransform)
    {
        float discardDuration = 0.2f;
        Transform centerPos = playCenterTransform != null ? playCenterTransform : handContainer;

        Sequence playSequence = DOTween.Sequence();

        // 1. 飞向中心区域
        playSequence.Append(
            cardTransform.DOMove(centerPos.position, playCardDuration)
                        // 修正 CS0103: The name 'playToCenterEaseType' does not exist
                        .SetEase(playToCenterEaseType) 
        );
        
        playSequence.Join(cardTransform.DORotate(Vector3.zero, playCardDuration).SetEase(playToCenterEaseType));
        playSequence.Join(cardTransform.DOScale(Vector3.one, playCardDuration).SetEase(playToCenterEaseType));

        playSequence.AppendInterval(centerIdleDuration); 
        
        // 3. 核心结算
        playSequence.AppendCallback(() => {
            Debug.Log($"卡牌效果 {card.cardName} 触发！");
            
            try
            {
                CharacterBase targetCharacter = targetTransform.GetComponent<CharacterBase>();
                
                // 修正: target 可能是主角（自目标卡），也可能是选中的敌人
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

        // 4. 飞向弃牌堆
        playSequence.Append(
            cardTransform.DOMove(discardPileTransform.position, discardDuration) 
                        // 修正 CS0103: The name 'playToDiscardEaseType' does not exist
                        .SetEase(playToDiscardEaseType) 
        );
            
        // 5. 销毁 UI 对象
        playSequence.AppendCallback(() => {
            if (cardTransform != null && cardTransform.gameObject != null)
            {
                Destroy(cardTransform.gameObject);
            }
        });
    }
    
    // --- 布局控制 ---
    
    public void HighlightCard(CardDisplay card) 
    {
        if (card == highlightedCard) return;
        
        UnhighlightCard(highlightedCard); 
        highlightedCard = card;
        
        card.transform.SetAsLastSibling(); 
        DOTween.Kill(card.transform); // 停止所有移动，确保只执行抬升
        card.transform.DOLocalMoveY(card.transform.localPosition.y + hoverTranslateY, repositionDuration)
            .SetEase(Ease.OutQuad);
        card.transform.DOScale(Vector3.one * hoverScale, repositionDuration)
            .SetEase(Ease.OutQuad);

        UpdateHandLayout(true); 
    }

    public void UnhighlightCard(CardDisplay card) 
    {
        if (card == null || card != highlightedCard) return;

        DOTween.Kill(card.transform, true); // 强制停止所有动画
        
        // 恢复缩放
        card.transform.DOScale(Vector3.one, repositionDuration)
            .SetEase(Ease.OutQuad);
        
        highlightedCard = null;
        UpdateHandLayout(true); 
    }
    
    // 修正 CS0103: The name 'UpdateHandLayout' does not exist
    public void UpdateHandLayout(bool useAnimation) 
    { 
        if (handDisplays == null || handDisplays.Count == 0) return;

        float cardSpacing = 220f; 
        float baseRotation = 4f;  
        float arcDropFactor = 0.1f;
        float repositionDuration = useAnimation ? 0.3f : 0f;
        float extraSpacingOnHover = 50f;

        int count = handDisplays.Count;
        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            CardDisplay display = handDisplays[i];
            if (display == null) continue;

            float hoverOffset = 0f;

            if (highlightedCard != null)
            {
                int highlightedIndex = handDisplays.IndexOf(highlightedCard);
                // 在高亮卡牌之后的卡牌，需要额外偏移
                if (i > highlightedIndex)
                {
                    hoverOffset = extraSpacingOnHover;
                }
                // 高亮卡牌本身，需要向左偏移一半的额外空间
                else if (i == highlightedIndex)
                {
                    hoverOffset = extraSpacingOnHover / 2f; 
                }
            }
            

            float targetX = startX + i * cardSpacing + hoverOffset;
            float rotation = (i - (count - 1) / 2f) * baseRotation;
            float dropY = -Mathf.Abs(i - (count - 1) / 2f) * arcDropFactor * 50f;

            Vector3 targetPosition = new Vector3(targetX, dropY, 0f);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotation);

            if (display != highlightedCard)
            {
                // 非高亮卡牌进行动画
                display.transform.DOLocalMove(targetPosition, repositionDuration).SetEase(Ease.OutQuad);
                display.transform.DOLocalRotateQuaternion(targetRotation, repositionDuration);
                display.transform.DOScale(Vector3.one, repositionDuration);
            }
        }
    }

    // 解决 CharacterBase.cs 依赖的检查战斗是否结束方法
    public void CheckBattleEnd()
    {
        if (characterManager == null) return;
        
        // 检查敌人是否全部死亡
        bool allEnemiesDead = characterManager.GetAllEnemies().All(e => e.currentHp <= 0);
        
        if (allEnemiesDead)
        {
            Debug.Log("战斗胜利!");
            // 这里可以添加胜利 UI/场景切换逻辑
        }
        else if (characterManager.GetActiveHero() == null || characterManager.GetActiveHero().currentHp <= 0)
        {
             Debug.Log("战斗失败!");
             // 这里可以添加失败 UI/场景切换逻辑
        }
    }
}