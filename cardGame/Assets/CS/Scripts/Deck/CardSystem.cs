using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CardDataEnums;

/// <summary>
/// 卡牌系统核心组件。负责能量管理、卡牌堆栈（抽牌、手牌、弃牌）操作以及卡牌的打出。
/// </summary>
public class CardSystem : MonoBehaviour
{
    [Header("Energy")]
    public int maxEnergy = 3;
    // CardDisplay.cs 和 BattleManager.cs 依赖的属性
    public int CurrentEnergy { get; private set; }
    
    [Header("Card Piles")]
    public List<CardData> masterDeck = new List<CardData>();
    public List<CardData> drawPile = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();

    [Header("Testing/Debug")]
    public List<CardData> startingDeck = new List<CardData>();

    private void Start()
    {
        // 仅重置能量，SetupDeck 在 BattleManager.Start 中调用
        ResetEnergy();
        Debug.Log("DEBUG: CardSystem Start - Energy reset.");
    }
    
    /// <summary>
    /// 初始化牌库：清空所有堆栈，将起始牌组放入主牌库并洗入抽牌堆。
    /// </summary>
    public void SetupDeck()
    {
        Debug.Log("--- DEBUG: SetupDeck 开始初始化 ---");
        
        masterDeck.Clear();
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();

        masterDeck.AddRange(startingDeck);
        Debug.Log($"DEBUG: 初始牌组加载完成，Master Deck size: {masterDeck.Count}");
        
        // 将主牌库洗牌并放入抽牌堆
        ShuffleMasterDeckIntoDrawPile();
        CurrentEnergy = maxEnergy;
        Debug.Log($"DEBUG: SetupDeck 完成. Draw Pile size: {drawPile.Count}. Max Energy: {maxEnergy}");
    }

    /// <summary>
    /// 将主牌库洗牌并放入抽牌堆。
    /// </summary>
    private void ShuffleMasterDeckIntoDrawPile()
    {
        drawPile.AddRange(masterDeck.OrderBy(x => Random.value).ToList());
        Debug.Log($"DEBUG: Master Deck 洗牌后放入 Draw Pile. Draw Pile 最终大小: {drawPile.Count}");
    }

    /// <summary>
    /// 获得能量 (CardData.cs 依赖的方法)。
    /// </summary>
    public void GainEnergy(int amount)
    {
        CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
        Debug.Log($"DEBUG: Gained {amount} Energy. Current: {CurrentEnergy}");
    }

    /// <summary>
    /// 消耗能量。
    /// </summary>
    public void SpendEnergy(int amount)
    {
        CurrentEnergy -= amount;
        CurrentEnergy = Mathf.Max(0, CurrentEnergy);
        Debug.Log($"DEBUG: Spent {amount} Energy. Remaining: {CurrentEnergy}");
    }

    /// <summary>
    /// 重置能量到最大值 (通常在回合开始时调用)。
    /// </summary>
    public void ResetEnergy()
    {
        CurrentEnergy = maxEnergy;
        Debug.Log($"DEBUG: Energy reset to Max Energy: {maxEnergy}");
    }

    /// <summary>
    /// 抽卡逻辑 (CardData.cs 依赖的方法)。如果抽牌堆空了，则洗入弃牌堆。
    /// </summary>
    /// <param name="count">抽卡数量。</param>
    /// <returns>实际抽到的卡牌数据列表。</returns>
    public List<CardData> DrawCards(int count)
    {
        Debug.Log($"DEBUG: 开始尝试抽取 {count} 张卡牌...");
        List<CardData> drawn = new List<CardData>();
        
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count > 0)
                {
                    // 抽牌堆空了，洗入弃牌堆
                    ShuffleDiscardIntoDrawPile();
                }
                else
                {
                    Debug.Log("DEBUG: Both draw and discard piles are empty. Cannot draw more cards.");
                    break;
                }
            }
            
            if (drawPile.Count == 0) 
            {
                Debug.Log("DEBUG: No cards left in draw pile after shuffle attempt.");
                break; // 再次检查，防止洗牌后仍为空
            }

            // 抽卡逻辑
            CardData card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);
            drawn.Add(card);
            
            Debug.Log($"DEBUG: 成功抽取卡牌: {card.cardName}. Draw Pile 剩余: {drawPile.Count}");
        }
        
        Debug.Log($"DEBUG: DrawCards 结束. 实际抽取: {drawn.Count} 张. Hand size: {hand.Count}");
        return drawn;
    }

    /// <summary>
    /// 将手牌全部弃置。
    /// </summary>
    public void DiscardHand()
    {
        Debug.Log($"DEBUG: DiscardHand 开始. 手牌数量: {hand.Count}");
        discardPile.AddRange(hand);
        hand.Clear();
        Debug.Log($"DEBUG: Discarded hand. Discard pile size: {discardPile.Count}");
    }
    
    /// <summary>
    /// 将打出的卡牌移入弃牌堆。
    /// </summary>
    public void PlayCard(CardData card)
    {
        hand.Remove(card);
        // 通常在卡牌打出后，将其放入弃牌堆。
        discardPile.Add(card);
        Debug.Log($"DEBUG: {card.cardName} moved to discard pile. Hand size: {hand.Count}");
    }

    /// <summary>
    /// 将弃牌堆洗牌并放入抽牌堆。
    /// </summary>
    private void ShuffleDiscardIntoDrawPile()
    {
        Debug.Log($"DEBUG: Shuffling discard pile ({discardPile.Count} cards) into draw pile.");
        drawPile.AddRange(discardPile.OrderBy(x => Random.value).ToList());
        discardPile.Clear();
        Debug.Log($"DEBUG: Shuffle complete. New Draw Pile size: {drawPile.Count}");
    }

    /// <summary>
    /// 检查卡牌是否可打出 (能量和手牌中是否存在)。
    /// </summary>
    public bool CanPlayCard(CardData card)
    {
        return CurrentEnergy >= card.energyCost && hand.Contains(card);
    }
    
    /// <summary>
    /// 检查卡牌是否需要选中目标 (CardDisplay.cs 依赖的方法)。
    /// </summary>
    public bool CardNeedsSelectedTarget(CardData card)
    {
        // 使用完整的 CardEnums.TargetType
        return card.actions.Any(a => 
            a.targetType == TargetType.SelectedEnemy ||
            a.targetType == TargetType.SelectedAlly ||
            a.targetType == TargetType.SelectedCharacter
        );
    }

    /// <summary>
    /// 验证选中的目标是否符合卡牌要求。
    /// </summary>
    public bool IsValidTarget(CardData card, CharacterBase target)
    {
        if (target == null) return false;
        
        // 尝试获取 CharacterManager 实例 (假设它在父对象或同级对象上)
        CharacterManager manager = GetComponentInParent<CharacterManager>();
        if (manager == null) 
        {
            // 如果还是找不到，报错并返回
            Debug.LogError("FATAL: CharacterManager not found via GetComponentInParent. Cannot validate target.");
            return false;
        }
        
        // 使用 CharacterManager 中的方法判断目标身份
        // 这里的 CharacterBase 和 CharacterManager 依赖于您的其他文件定义
        // 假设 GetAllEnemies 和 GetAllHeroes 是 CharacterManager 中存在的方法
        bool isEnemy = manager.GetAllEnemies().Contains(target);
        bool isAlly = manager.GetAllHeroes().Contains(target);

        // 验证目标类型
        // 使用完整的 CardEnums.TargetType
        return card.actions.Any(a => 
            (a.targetType == TargetType.SelectedEnemy && isEnemy) ||
            (a.targetType == TargetType.SelectedAlly && isAlly) ||
            (a.targetType == TargetType.SelectedCharacter && (isEnemy || isAlly))
        );
    }
    
    // 能量获取器 (供 UIManager 和其他系统调用)
    public int GetCurrentEnergy() => CurrentEnergy;
    public int GetMaxEnergy() => maxEnergy;
}