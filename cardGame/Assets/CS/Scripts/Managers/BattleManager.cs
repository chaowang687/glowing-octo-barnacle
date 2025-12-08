using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // 导入 DG.Tweening 命名空间
using UnityEngine.EventSystems; // 用于 RectTransformUtility

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
    public RectTransform handContainerRect; // <-- 用于坐标转换
    public Transform discardPileLocationTransform; 
    public Transform drawPileLocationTransform;    
    public Transform playCenterTransform; 

    // UI 显示列表
    public List<CardDisplay> handDisplays = new List<CardDisplay>(); 
    
    // --- 悬停判定所需的稳定布局数据 ---
    // (卡牌实例, 理想布局下的中心X坐标)
    private List<(CardDisplay display, float centerX)> handLayoutReference = new List<(CardDisplay, float)>(); 

    [Header("回合状态")]
    public int CurrentRound { get; private set; } = 0; 
    public int cardsToDraw = 5; 

    // --- 1. 手牌布局与动画参数 ---
    [Header("手牌布局: 固定的弧度和间距")]
    [Range(600f, 1500f)]
    [Tooltip("手牌弧线的**固定**总宽度 (X轴跨度，决定弧度形状)")]
    public float arcBaseWidth = 1000f; 
    
    [Range(50f, 500f)] 
    [Tooltip("手牌弧线的高度，决定卡牌抬升的程度（固定弧度形状的一部分）")]
    public float arcHeight = 250f; 
    
    [Range(100f, 300f)]
    [Tooltip("卡牌之间的**固定**水平间距")]
    public float cardSpacing = 175f; 
    
    // --- 动画参数 (保持不变) ---
    [Header("动画参数")]
    [Range(0.1f, 1f)]
    [Tooltip("卡牌重新布局的动画时长")]
    public float repositionDuration = 0.3f; 
    
    [Range(0.05f, 0.5f)]
    [Tooltip("单张卡牌抽出的动画时长")]
    public float drawDuration = 0.1f; 
    
    [Range(0.001f, 0.1f)]
    [Tooltip("连续抽卡时的延迟间隔")]
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
    // --- 动画参数结束 ---

    // --- 2. 动画缓动类型 (保持不变) ---
    [Header("动画缓动类型")]
    public Ease drawEaseType = Ease.OutQuad; 
    public Ease playToCenterEaseType = Ease.OutSine; 
    public Ease playToDiscardEaseType = Ease.InQuad; 
    // --- 缓动类型结束 ---

    // --- 3. 手牌高亮与悬停参数 ---
    [Header("手牌高亮与悬停参数")]
    [Range(0.1f, 1f)]
    public float hoverDelayDuration = 0.3f; 
    
    [Range(20f, 100f)]
    [Tooltip("卡牌高亮时 Y 轴抬升的高度 (沿卡牌Local Y轴/法线方向)")]
    public float hoverTranslateY = 50f; 
    
    [Range(1f, 1.5f)]
    [Tooltip("卡牌高亮时的缩放比例")]
    public float hoverScale = 1.1f; 
    
    [Range(0f, 100f)]
    [Tooltip("卡牌被高亮时，周围卡牌的额外散开间距")]
    public float extraSpacingOnHover = 50f;
    
    [Range(0f, 0.5f)]
    [Tooltip("悬停检测的X轴容忍度，以卡牌间距的比例计算。0.5意味着检测范围是相邻卡牌中心点之间。")]
    public float hoverToleranceFactor = 0.4f; 

    // --- 新增：Y轴交互区域限制 ---
    [Range(10f, 300f)]
    [Tooltip("鼠标允许低于手牌弧线基线(Y=0)的垂直距离，用于定义交互区域的下限。")]
    public float hoverToleranceY = 150f; 
    // --- 高亮参数结束 ---

    [Header("调试用默认资产")]
    public EnemyData defaultEnemyDataAsset; 
    public RoundBasedStrategy defaultEnemyStrategyAsset; 

    private CardDisplay highlightedCard = null; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // 确保获取 RectTransform 引用
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
    
    /// <summary>
    /// 每帧持续检测鼠标位置，进行稳定的卡牌悬停判定。
    /// </summary>
    void Update()
    {
        // 只有当手牌中确实有卡牌时，才进行悬停判定
        if (handDisplays.Count > 0)
        {
             HandleHoverSelection();
        }
    }
    
    /// <summary>
    /// 核心悬停判定逻辑：基于稳定的布局参考 X 坐标和 Y 轴交互区域进行检测。
    /// </summary>
    private void HandleHoverSelection()
    {
        // 检查必要条件
        if (handLayoutReference.Count == 0 || handContainerRect == null)
        {
            SetHighlightedCard(null); 
            return;
        }

        // 1. 获取鼠标在手牌容器 **局部坐标系** 下的位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handContainerRect, 
            Input.mousePosition, 
            null, // 默认为 Screen Space - Overlay 或 World Space 且没有 Camera
            out Vector2 localMousePos
        );
        
        // 2. Y 轴交互区域限制检查
        // 假设卡牌弧线的基线在 Y=0 附近。
        // minYThreshold: 允许鼠标低于基线的距离 (由 hoverToleranceY 控制)
        float minYThreshold = -hoverToleranceY;
        // maxYThreshold: 卡牌弧线最高点 (arcHeight) 加上最大抬升高度 (hoverTranslateY) 加上额外容忍区
        // 这里为了简单，我们只检查上限，确保不选中离谱的高空区域。
        float maxYThreshold = arcHeight + hoverTranslateY + 50f; // 额外加 50f 容忍高亮抬升后的卡牌

        if (localMousePos.y < minYThreshold || localMousePos.y > maxYThreshold)
        {
            // 如果鼠标不在卡牌弧线的垂直交互区域内，则不选中任何卡牌
            SetHighlightedCard(null); 
            return;
        }

        // 3. 查找鼠标当前最接近的卡牌的中心X坐标 (X轴逻辑不变)
        CardDisplay bestMatch = null;
        float minDistance = float.MaxValue;
        
        // 判定区域容忍度：基于卡牌间距和容忍度因子
        float hoverToleranceX = cardSpacing * hoverToleranceFactor;

        foreach (var item in handLayoutReference)
        {
            // X轴距离
            float distanceX = Mathf.Abs(localMousePos.x - item.centerX);
            
            // 简单判定：只检查 X 轴，并检查是否在容忍范围内
            if (distanceX < hoverToleranceX && distanceX < minDistance)
            {
                minDistance = distanceX;
                bestMatch = item.display;
            }
        }
        
        // 4. 设置高亮卡牌
        SetHighlightedCard(bestMatch);
    }

    /// <summary>
    /// 统一设置高亮卡牌的方法，避免多次调用 UpdateHandLayout。
    /// </summary>
    private void SetHighlightedCard(CardDisplay newHighlightedCard)
    {
        if (highlightedCard == newHighlightedCard) return;

        highlightedCard = newHighlightedCard;
        
        // 如果有新高亮卡牌，提升其渲染层级，确保它在最上面
        if (highlightedCard != null)
        {
            highlightedCard.transform.SetAsLastSibling();
        }

        // 重新布局，应用高亮效果
        UpdateHandLayout(true);
    }
    
    // --- 公共接口 ---
    
    /// <summary>
    /// 供 CardDisplay 拖拽开始时调用，强制取消高亮状态。
    /// </summary>
    public void UnhighlightCard(CardDisplay card)
    {
        if (highlightedCard == card)
        {
            SetHighlightedCard(null);
        }
    }
    
    /// <summary>
    /// 供 CardDisplay 在点击时检查是否处于高亮状态。
    /// </summary>
    public CardDisplay GetHighlightedCard()
    {
        return highlightedCard;
    }
    
    // --- 战斗流程和辅助方法 (为了完整性保留) ---
    
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

        foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0)) 
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null) enemyAI.PerformAction(activeHero, CurrentRound);
        }
        
        CurrentRound++;
        
        characterManager.ClearAllBlocks(); 

        CalculateAllEnemyIntents(); 
        CheckBattleEnd(); 
        StartNewTurn();
    }

    private void CalculateAllEnemyIntents()
    {
       
        CharacterBase activeHero = characterManager.GetActiveHero();
        if (activeHero == null) return;

        foreach (var enemy in characterManager.GetAllEnemies().ToList().Where(e => e.currentHp > 0)) 
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            
            if (enemyAI != null) enemyAI.CalculateIntent(activeHero, CurrentRound); 
        }
    }
    
    public bool IsValidTarget(CardData card, CharacterBase target)
    {
        if (cardSystem == null) return false;
        // 假设 CardSystem 中有这个方法来判断目标合法性
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
            
            // 1. 实例化 CardDisplay
            GameObject cardObject = Instantiate(cardPrefab, drawPileLocationTransform.position, Quaternion.identity, handContainer);
            CardDisplay display = cardObject.GetComponent<CardDisplay>();
            
            if (display != null) display.Initialize(drawnCard, characterManager.GetActiveHero()); 
            handDisplays.Add(display);
            
            Transform cardTransform = display.transform;
            
            // 2. 抽卡动画：从牌堆飞出
            Vector3 tempDrawPos = drawPileLocationTransform.position + Vector3.up * temporaryDrawOffset + Vector3.forward * drawZSeparation * i;

            drawSequence.Append(
                cardTransform.DOMove(tempDrawPos, drawDuration * 0.5f) 
                    .SetEase(drawEaseType) 
                    .SetDelay(i * drawCardDelay)
            );

            // 3. 飞向手牌容器
            drawSequence.Append(
                 cardTransform.DOMove(handContainer.position, drawDuration * 0.5f) 
                    .SetEase(drawEaseType) 
            );
        }
        
        // 4. 动画完成后执行布局
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
        handLayoutReference.Clear(); // 清空布局参考
        highlightedCard = null;      // 清空高亮卡牌
    }
    
    // BattleManager.cs

    public bool TryPlayCard(CardData card, CharacterBase target, GameObject cardDisplayObject)
    {
        if (cardSystem == null || characterManager == null || card == null) return false;
        
        if (!cardSystem.CanPlayCard(card)) return false;
        
        // --- ⭐ 关键修改区域：自动目标锁定 ⭐
        CharacterBase actualTarget = target;
        
        // 1. 检查卡牌是否需要目标
        if (cardSystem.CardNeedsSelectedTarget(card))
        {
            // 2. 尝试从传入的 target 中获取（如果您还保留了拖拽功能）
            if (actualTarget == null)
            {
                // 3. 如果传入目标为空 (点击打出)，则自动查找第一个存活的敌人
                CharacterBase firstEnemy = characterManager.GetAllEnemies().FirstOrDefault(e => e != null && e.currentHp > 0);
                
                if (firstEnemy != null)
                {
                    actualTarget = firstEnemy; // 锁定第一个敌人
                    Debug.Log($"自动锁定目标: {actualTarget.characterName}");
                }
            }
            
            // 4. 再次检查：如果没有找到任何目标，则无法打出
            if (actualTarget == null)
            {
                
                Debug.LogWarning($"卡牌 {card.cardName} 需要目标但场上没有存活的敌人。");
                return false;
            }
        }
        // --- ⭐ 关键修改区域结束 ⭐
        
        // 5. 目标合法性检查 (使用 actualTarget)
        if (cardSystem.CardNeedsSelectedTarget(card) && !IsValidTarget(card, actualTarget)) return false;

        // 6. 执行消耗和动画
        cardSystem.SpendEnergy(card.energyCost);
        Debug.Log($"成功打出 {card.cardName}，剩余能量: {cardSystem.CurrentEnergy}");

        // ... (卡牌移除逻辑不变) ...
        
        CardDisplay displayToRemove = cardDisplayObject.GetComponent<CardDisplay>();
        if (displayToRemove != null)
        {
            handDisplays.Remove(displayToRemove);
            if (highlightedCard == displayToRemove) highlightedCard = null;
        }
        
        // ⭐ targetTransform 传入实际目标 ⭐
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

        // 1. 飞向中心区域
        playSequence.Append(
            cardTransform.DOMove(centerPos.position, playCardDuration)
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
                
                CharacterBase source = characterManager.GetActiveHero();
                card.ExecuteEffects(source, targetCharacter, cardSystem); // 假设 CardData.ExecuteEffects 存在
                
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
    
    /// <summary>
    /// 计算二次贝塞尔曲线上的点和切线角度。
    /// </summary>
    private (Vector3 position, Quaternion rotation) CalculateBezierPoint(float t, float width, float height)
    {
        // 1. 定义控制点 (P0, P1, P2) - 局部坐标
        Vector3 p0 = new Vector3(-width / 2f, 0f, 0f);
        Vector3 p1 = new Vector3(0f, height, 0f);
        Vector3 p2 = new Vector3(width / 2f, 0f, 0f);

        // 2. 计算曲线上的点 B(t)
        float oneMinusT = 1f - t;
        
        Vector3 position = 
            (oneMinusT * oneMinusT * p0) + 
            (2f * oneMinusT * t * p1) + 
            (t * t * p2);

        // 3. 计算切线向量 B'(t)
        Vector3 p0_p1 = p1 - p0; // P1 - P0
        Vector3 p1_p2 = p2 - p1; // P2 - P1

        Vector3 tangent = 
            (2f * oneMinusT * p0_p1) + 
            (2f * t * p1_p2);
        
        // 4. 计算切线与X轴的角度 (angleZ)
        float angleZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg; 
        
        // 5. Z轴旋转：Local Y 轴（卡牌顶部）垂直于切线（法线）
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleZ);

        return (position, rotation);
    }
    
    /// <summary>
    /// 重新计算手牌布局，并应用高亮/散开动画。
    /// </summary>
    public void UpdateHandLayout(bool useAnimation) 
    { 
        if (handDisplays == null || handDisplays.Count == 0) 
        {
            handLayoutReference.Clear();
            return;
        }

        float duration = useAnimation ? this.repositionDuration : 0f;
        int count = handDisplays.Count;
        
        // 清空并重建布局参考列表
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
                    spreadOffset = -gapSize / 2f; // 左侧卡牌向左推
                }
                else if (i > highlightedIndex)
                {
                    spreadOffset = gapSize / 2f; // 右侧卡牌向右推
                }
            }
            
            float finalTargetX = idealX + spreadOffset;

            // --- 关键：记录卡牌的稳定 X 轴中心位置用于 Update() 中的悬停判定 ---
            handLayoutReference.Add((display, finalTargetX));
            // ----------------------------------------------------------------

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

    // 解决 CharacterBase.cs 依赖的检查战斗是否结束方法
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