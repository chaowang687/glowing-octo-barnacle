using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        SetupDeck();
        ResetEnergy();
    }
    
    public void SetupDeck()
    {
        masterDeck.Clear();
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();

        masterDeck.AddRange(startingDeck);
        // 修正 CS0103: The name 'ShuffleDrawPileIntoDrawPile' does not exist in the current context
        ShuffleMasterDeckIntoDrawPile();
        CurrentEnergy = maxEnergy;
    }

    // 新增方法: 将主牌库洗牌并放入抽牌堆
    private void ShuffleMasterDeckIntoDrawPile()
    {
        drawPile.AddRange(masterDeck.OrderBy(x => Random.value).ToList());
        Debug.Log($"Deck setup complete. Draw pile size: {drawPile.Count}");
    }

    // 解决 CardData.cs 依赖的方法
    public void GainEnergy(int amount)
    {
        CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
    }

    public void SpendEnergy(int amount)
    {
        CurrentEnergy -= amount;
        CurrentEnergy = Mathf.Max(0, CurrentEnergy);
    }

    public void ResetEnergy()
    {
        CurrentEnergy = maxEnergy;
    }

    // 解决 CardData.cs 依赖的方法
    public List<CardData> DrawCards(int count)
    {
        List<CardData> drawn = new List<CardData>();
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count > 0)
                {
                    // 修正 CS0103: The name 'ShuffleDiscardIntoDrawPile' does not exist
                    ShuffleDiscardIntoDrawPile();
                }
                else
                {
                    Debug.Log("Both draw and discard piles are empty.");
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
        return drawn;
    }

    public void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }
    
    public void PlayCard(CardData card)
    {
        hand.Remove(card);
        discardPile.Add(card);
    }

    // 新增方法: 将弃牌堆洗牌并放入抽牌堆
    private void ShuffleDiscardIntoDrawPile()
    {
        Debug.Log("Shuffling discard pile into draw pile.");
        drawPile.AddRange(discardPile.OrderBy(x => Random.value).ToList());
        discardPile.Clear();
    }

    public bool CanPlayCard(CardData card)
    {
        return CurrentEnergy >= card.energyCost && hand.Contains(card);
    }
    
    public bool CardNeedsSelectedTarget(CardData card)
    {
        return card.actions.Any(a => 
            a.targetType == TargetType.SelectedEnemy ||
            a.targetType == TargetType.SelectedAlly ||
            a.targetType == TargetType.SelectedCharacter
        );
    }

    public bool IsValidTarget(CardData card, CharacterBase target)
    {
        if (target == null) return false;
        
        CharacterManager manager = GetComponent<CharacterManager>();
        if (manager == null) return false;
        
        bool isEnemy = manager.GetAllEnemies().Contains(target);
        bool isAlly = manager.GetAllHeroes().Contains(target);

        // 简化验证：如果卡牌需要选中目标，检查目标是否匹配
        return card.actions.Any(a => 
            (a.targetType == TargetType.SelectedEnemy && isEnemy) ||
            (a.targetType == TargetType.SelectedAlly && isAlly) ||
            (a.targetType == TargetType.SelectedCharacter && (isEnemy || isAlly))
        );
    }
}

// 已移除重复的 CardType 枚举定义，因为它已