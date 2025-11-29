using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// FIX: 移除 using static CardEnums; 以避免 TargetType 的引用冲突，
//      改为在所有使用到 CardEnums.TargetType 和 CardEnums.EffectType 的地方使用完整前缀。

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
        // 确保 BattleManager 或其他管理器在调用 SetupDeck 之前初始化
        // 为了调试方便，我们在 Start 中调用
        // SetupDeck(); 
        ResetEnergy();
    }
    
    /// <summary>
    /// 初始化牌库：清空所有堆栈，将起始牌组放入主牌库并洗入抽牌堆。
    /// </summary>
    public void SetupDeck()
    {
        masterDeck.Clear();
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();

        masterDeck.AddRange(startingDeck);
        // 将主牌库洗牌并放入抽牌堆
        ShuffleMasterDeckIntoDrawPile();
        CurrentEnergy = maxEnergy;
    }

    /// <summary>
    /// 将主牌库洗牌并放入抽牌堆。
    /// </summary>
    private void ShuffleMasterDeckIntoDrawPile()
    {
        drawPile.AddRange(masterDeck.OrderBy(x => Random.value).ToList());
        Debug.Log($"Deck setup complete. Draw pile size: {drawPile.Count}");
    }

    /// <summary>
    /// 获得能量 (CardData.cs 依赖的方法)。
    /// </summary>
    public void GainEnergy(int amount)
    {
        CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
        Debug.Log($"Gained {amount} Energy. Current: {CurrentEnergy}");
    }

    /// <summary>
    /// 消耗能量。
    /// </summary>
    public void SpendEnergy(int amount)
    {
        CurrentEnergy -= amount;
        CurrentEnergy = Mathf.Max(0, CurrentEnergy);
    }

    /// <summary>
    /// 重置能量到最大值 (通常在回合开始时调用)。
    /// </summary>
    public void ResetEnergy()
    {
        CurrentEnergy = maxEnergy;
    }

    /// <summary>
    /// 抽卡逻辑 (CardData.cs 依赖的方法)。如果抽牌堆空了，则洗入弃牌堆。
    /// </summary>
    /// <param name="count">抽卡数量。</param>
    /// <returns>实际抽到的卡牌数据列表。</returns>
    public List<CardData> DrawCards(int count)
    {
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
                    Debug.Log("Both draw and discard piles are empty. Cannot draw more cards.");
                    break;
                }
            }
            
            if (drawPile.Count == 0) break; // 再次检查，防止洗牌后仍为空

            // 抽卡逻辑
            CardData card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);
            drawn.Add(card);
        }
        Debug.Log($"Drew {drawn.Count} cards. Hand size: {hand.Count}");
        return drawn;
    }

    /// <summary>
    /// 将手牌全部弃置。
    /// </summary>
    public void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
        Debug.Log($"Discarded hand. Discard pile size: {discardPile.Count}");
    }
    
    /// <summary>
    /// 将打出的卡牌移入弃牌堆。
    /// </summary>
    public void PlayCard(CardData card)
    {
        hand.Remove(card);
        // 通常在卡牌打出后，将其放入弃牌堆。
        discardPile.Add(card);
        Debug.Log($"{card.cardName} moved to discard pile.");
    }

    /// <summary>
    /// 将弃牌堆洗牌并放入抽牌堆。
    /// </summary>
    private void ShuffleDiscardIntoDrawPile()
    {
        Debug.Log("Shuffling discard pile into draw pile.");
        drawPile.AddRange(discardPile.OrderBy(x => Random.value).ToList());
        discardPile.Clear();
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
            a.targetType == CardEnums.TargetType.SelectedEnemy ||
            a.targetType == CardEnums.TargetType.SelectedAlly ||
            a.targetType == CardEnums.TargetType.SelectedCharacter
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
            Debug.LogError("CharacterManager not found via GetComponentInParent. Cannot validate target.");
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
            (a.targetType == CardEnums.TargetType.SelectedEnemy && isEnemy) ||
            (a.targetType == CardEnums.TargetType.SelectedAlly && isAlly) ||
            (a.targetType == CardEnums.TargetType.SelectedCharacter && (isEnemy || isAlly))
        );
    }
}