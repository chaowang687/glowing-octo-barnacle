using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CardDataEnums;
using SlayTheSpireMap;

/// <summary>
/// 卡牌系统核心组件。负责能量管理、卡牌堆栈（抽牌、手牌、弃牌）操作以及卡牌的打出。
/// </summary>
public class CardSystem : MonoBehaviour
{
    // CardSystem.cs (顶部)
    public event System.Action OnEnergyChanged;
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
        
        // 修复：如果 masterDeck 已经被外部（如 LoadDeckFromGlobal）填充，则不要用 startingDeck 覆盖它
        if (masterDeck.Count == 0)
        {
            if (startingDeck.Count > 0)
            {
                masterDeck.Clear();
                masterDeck.AddRange(startingDeck);
                Debug.Log($"DEBUG: 使用默认 Starting Deck 初始化. Size: {masterDeck.Count}");
            }
            else
            {
                Debug.LogWarning("DEBUG: Master Deck 为空且 Starting Deck 为空！");
            }
        }
        else
        {
            Debug.Log($"DEBUG: 保留现有 Master Deck. Size: {masterDeck.Count}");
        }
        
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();
        
        // 将主牌库洗牌并放入抽牌堆
        ShuffleMasterDeckIntoDrawPile();
        CurrentEnergy = maxEnergy;
        Debug.Log($"DEBUG: SetupDeck 完成. Draw Pile size: {drawPile.Count}. Max Energy: {maxEnergy}");
    }
    // CardSystem.cs 内部
        public void LoadDeckFromGlobal()
        {
            masterDeck.Clear();
            
            if (GameDataManager.Instance == null)
            {
                Debug.LogWarning("GameDataManager Instance 为空，无法加载全局卡组。将使用默认卡组。");
                SetupDeck();
                return;
            }

            List<string> cardIds = GameDataManager.Instance.playerData.cardIds;
            Debug.Log($"LoadDeckFromGlobal: 尝试加载 {cardIds.Count} 张卡牌");

            foreach (string originalId in cardIds)
            {
                // Legacy ID Mapping (Migration Fix)
                string id = originalId;
                if (id == "Strike_R") id = "B_FRENZY";
                else if (id == "Defend_R") id = "B_BLOCK";
                else if (id == "Bash") id = "B_EXECUTE";

                if (id != originalId)
                {
                    Debug.Log($"[CardSystem] Migrating Legacy Card ID: {originalId} -> {id}");
                }

                // 尝试路径 1: Resources/Cards/ID
                CardData card = Resources.Load<CardData>($"Cards/{id}");
                
                // 尝试路径 2: Resources/CardData/ID (Importer 默认路径)
                if (card == null)
                {
                    card = Resources.Load<CardData>($"CardData/{id}");
                }

                // 尝试路径 3: 暴力遍历所有 CardData 查找文件名或 cardID 匹配的卡牌
                // 这可以解决 ID 与文件名不一致，或路径不对的问题
                if (card == null)
                {
                    CardData[] allCards = Resources.LoadAll<CardData>("");
                    // 先按 cardID 匹配
                    card = allCards.FirstOrDefault(c => c.cardID == id);
                    // 再按文件名匹配
                    if (card == null)
                    {
                        card = allCards.FirstOrDefault(c => c.name == id);
                    }
                }

                if (card != null)
                {
                    masterDeck.Add(card);
                    Debug.Log($"[CardSystem] 成功加载卡牌: {card.cardName} (ID: {id})");
                }
                else
                {
                    Debug.LogWarning($"[CardSystem] 找不到卡牌资源: {id}. 已尝试直接路径及全局搜索。请检查 Resources 下的文件名或 cardID 配置。");
                }
            }
            
            // Failsafe: 如果加载后的卡组数量过少 (例如因为资源丢失)，且原本期望的卡牌数量较多，则尝试使用默认卡组填充
            if (masterDeck.Count < 3 && cardIds.Count >= 3)
            {
                Debug.LogError($"[CardSystem] 严重警告: 尝试加载 {cardIds.Count} 张卡牌，但仅成功加载 {masterDeck.Count} 张。可能存在资源丢失或 ID 不匹配。正在回退到默认卡组。");
                masterDeck.Clear();
                // 尝试加载新的默认卡牌
                string[] defaultBackups = new string[] { "B_FRENZY", "B_BLOCK", "B_EXECUTE" };
                foreach(var backupId in defaultBackups)
                {
                    CardData c = Resources.Load<CardData>($"Cards/{backupId}");
                    if(c != null) masterDeck.Add(c);
                }
                // 补充到一定数量
                while(masterDeck.Count < 8)
                {
                     CardData c = Resources.Load<CardData>("Cards/B_FRENZY");
                     if(c!=null) masterDeck.Add(c);
                     else break;
                }
            }
            
            // 初始化抽牌堆等逻辑
            SetupDeck(); 
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
        // ⭐ 触发事件 ⭐
        OnEnergyChanged?.Invoke();
    }

    /// <summary>
    /// 消耗能量。
    /// </summary>
    public void SpendEnergy(int amount)
    {
        CurrentEnergy -= amount;
        CurrentEnergy = Mathf.Max(0, CurrentEnergy);
        Debug.Log($"DEBUG: Spent {amount} Energy. Remaining: {CurrentEnergy}");
        // ⭐ 触发事件 ⭐
        OnEnergyChanged?.Invoke();
    }

    /// <summary>
    /// 重置能量到最大值 (通常在回合开始时调用)。
    /// </summary>
    public void ResetEnergy()
    {
        CurrentEnergy = maxEnergy;
        Debug.Log($"DEBUG: Energy reset to Max Energy: {maxEnergy}");
        // ⭐ 触发事件 ⭐
        OnEnergyChanged?.Invoke();
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
        
        // --- 核心修复：通知 BattleManager 更新 UI ---
        // 之前只更新了 CardSystem 的内部数据 (hand 列表)，
        // 但没有触发 BattleManager 生成对应的 CardDisplay 预制体，所以你看不到手牌增加。
        if (BattleManager.Instance != null && drawn.Count > 0)
        {
            BattleManager.Instance.ProcessDrawnCards(drawn);
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