using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using CardDataEnums; // 假设这是您定义的枚举命名空间
using System; 
using DG.Tweening; // 引入 DOTween 命名空间

// ⭐ 新增：格挡条目结构，包含数量和持续回合数 ⭐
[Serializable]
public struct BlockEntry
{
    public int Amount;
    [Tooltip("格挡持续的回合数。1 表示在下一回合开始时清除 (即只持续当前回合)。")]
    public int Duration; 
}

/// <summary>
/// 角色基类 (英雄或敌人)。包含核心属性和战斗方法，并全面支持状态效果。
/// </summary>
public class CharacterBase : MonoBehaviour
{
    // ⭐ 核心事件：用于通知 UI 更新 (血条/格挡变化) ⭐
    public event Action<int, int> OnHealthChanged; // (currentHp, maxHp)
    public event System.Action OnBlockChanged;

    [Header("Base Stats")]
    public string characterName = "Character";
    
    public int maxHp = 100; // 最大生命值
    public int currentHp; // 当前生命值
    
    // ⭐ 替换：旧的 block 字段被移除 ⭐
    // public int block; 
    
    [Header("格挡持久化")]
    // 追踪所有格挡条目 (数量和持续回合)
    private List<BlockEntry> blockEntries = new List<BlockEntry>();
    
    // ⭐ 新属性：只读，计算当前总格挡值 ⭐
    public int CurrentBlock { get { return blockEntries.Sum(e => e.Amount); } }
    
    public bool isDead = false; 
    
    // 状态效果列表
    protected Dictionary<StatusEffect, int> statusEffects = new Dictionary<StatusEffect, int>();

    protected virtual void Awake()
    {
        currentHp = maxHp; 
        isDead = false;
        blockEntries.Clear(); // 初始化时清空格挡列表
    }

    /// <summary>
    /// GameFlowManager 依赖的初始化方法。
    /// </summary>
    public virtual void Initialize(string name, int maxHp, Sprite artwork)
    {
        this.maxHp = maxHp;
        this.currentHp = maxHp;
        this.characterName = name;
        OnHealthChanged?.Invoke(currentHp, this.maxHp);
    }
    
    // --- 状态效果处理 (保持不变) ---
    // ... (GetStatusEffectAmount, ApplyStatusEffect, DecreaseStatusDurations 保持不变) ...

    public int GetStatusEffectAmount(StatusEffect effect)
    {
        return statusEffects.TryGetValue(effect, out int amount) ? amount : 0;
    }
    
    public virtual void ApplyStatusEffect(StatusEffect effect, int duration) 
    {
        if (duration <= 0) return;
        
        if (statusEffects.ContainsKey(effect))
        {
            statusEffects[effect] += duration;
        }
        else
        {
            statusEffects.Add(effect, duration);
        }
        Debug.Log($"{characterName} 获得 {effect}，层数: {statusEffects[effect]}");
    }

    protected void DecreaseStatusDurations()
    {
        var keys = statusEffects.Keys.ToList();
        
        foreach (var effect in keys)
        {
            if (effect == StatusEffect.Strength || effect == StatusEffect.Dexterity || effect == StatusEffect.Metallicize)
            {
                continue; 
            }
            
            if (statusEffects.ContainsKey(effect) && statusEffects[effect] > 0)
            {
                statusEffects[effect]--;
                if (statusEffects[effect] <= 0)
                {
                    statusEffects.Remove(effect);
                }
            }
        }
    }


    // --- 回合钩子 ---

    public virtual void AtStartOfTurn()
    {
        if (isDead) return;
        
        int poisonAmount = GetStatusEffectAmount(StatusEffect.Poison);
        if (poisonAmount > 0)
        {
            TakeDamage(poisonAmount, isAttack: false); 
            
            if (statusEffects.ContainsKey(StatusEffect.Poison))
            {
                statusEffects[StatusEffect.Poison] = Mathf.Max(0, statusEffects[StatusEffect.Poison] - 1);
                if (statusEffects[StatusEffect.Poison] <= 0)
                {
                    statusEffects.Remove(StatusEffect.Poison);
                }
            }
            Debug.Log($"{characterName} 受到 {poisonAmount} 点中毒伤害。");
        }
    }

    public virtual void AtEndOfTurn() 
    {
        if (isDead) return;
        
        int metallicizeAmount = GetStatusEffectAmount(StatusEffect.Metallicize);
        if (metallicizeAmount > 0)
        {
            // ⭐ 调用新的 AddBlock，默认持续时间为 1 ⭐
            AddBlock(metallicizeAmount, duration: 1); 
            Debug.Log($"{characterName} 因金属化获得 {metallicizeAmount} 点格挡 (持续1回合)。");
        }
        
        DecreaseStatusDurations();
    }
    
    // ⭐ 核心新方法：回合结束时减少格挡持续时间并清除过期格挡 ⭐
    /// <summary>
    /// 在回合结束时调用，递减所有非永久格挡的持续时间，并清除持续时间耗尽的格挡。
    /// </summary>
    public void DecrementBlockDuration()
    {
        if (isDead) return;

        bool wasBlockCleared = false;
        
        for (int i = blockEntries.Count - 1; i >= 0; i--)
        {
            // Duration < 0 表示永久格挡 (例如 Slay the Spire 的 Buffer 效果)
            if (blockEntries[i].Duration <= 0) continue; 
            
            // 递减持续时间
            blockEntries[i] = new BlockEntry { 
                Amount = blockEntries[i].Amount, 
                Duration = blockEntries[i].Duration - 1 
            };
            
            // 检查是否应该清除
            if (blockEntries[i].Duration <= 0)
            {
                int clearedAmount = blockEntries[i].Amount;
                blockEntries.RemoveAt(i);
                wasBlockCleared = true;
                Debug.Log($"{characterName} 的 {clearedAmount} 格挡因持续时间结束而被清除。");
            }
        }
        
        if (wasBlockCleared)
        {
            OnBlockChanged?.Invoke();
        }
    }
/// <summary>
/// 伤害结算动画，并最终执行扣血。
/// </summary>
private Sequence AnimateDamage(int finalDamage)
{
    if (finalDamage <= 0) return DOTween.Sequence();
    
    // ⭐ DOTween 序列：实现角色闪烁、浮动伤害数字等 ⭐
    Sequence damageSequence = DOTween.Sequence();
    
    // 示例：角色颜色闪烁动画 (如果需要，请添加您的 SpriteRenderer 或 Image 引用)
    // damageSequence.Append(GetComponent<SpriteRenderer>().DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo));
    
    // 最终回调：真正扣除 HP 并更新 UI
    damageSequence.AppendCallback(() => 
    {
        currentHp -= finalDamage;
        currentHp = Mathf.Max(0, currentHp); 
        
        Debug.Log($"{characterName} 受到 {finalDamage} 最终伤害。HP 剩余: {currentHp}。");
        
        // 触发事件，通知 UI 更新
        OnHealthChanged?.Invoke(currentHp, maxHp); 
        
        if (currentHp <= 0)
        {
            Die();
        }
    });
    
    return damageSequence;
}

    // --- 核心战斗方法 ---

    // PerformAttack 方法保持不变 (因为它只调用 TakeDamage)

    // TakeDamage 方法保持不变 (因为它只调用 CalculateDamage 和 AnimateDamage)
    public virtual Sequence TakeDamage(int amount, bool isAttack = true)
    {
        if (isDead) return DOTween.Sequence();

        int finalDamageTaken = CalculateDamage(amount, isAttack);
        
        return AnimateDamage(finalDamageTaken);
    }

    /// <summary>
    /// 纯数据计算：根据易伤和格挡计算最终受到的伤害，并消耗格挡。
    /// </summary>
    /// <returns>实际受到的伤害值。</returns>
    private int CalculateDamage(int amount, bool isAttack)
    {
        int damageTaken = amount;
        int initialTotalBlock = CurrentBlock; // 记录初始总格挡

        // 1. 易伤 (Vulnerable) 修正
        if (isAttack && GetStatusEffectAmount(StatusEffect.Vulnerable) > 0)
        {
            damageTaken = (int)(damageTaken * 1.5f);
            Debug.Log($"{characterName} 处于易伤状态，受到伤害增加 50%。");
        }
        
        // 2. ⭐ 核心修正：消耗格挡列表中的格挡 ⭐
        if (CurrentBlock > 0)
        {
            for (int i = blockEntries.Count - 1; i >= 0 && damageTaken > 0; i--)
            {
                BlockEntry entry = blockEntries[i];
                
                if (damageTaken >= entry.Amount)
                {
                    // 伤害大于格挡，消耗整个条目
                    damageTaken -= entry.Amount;
                    blockEntries.RemoveAt(i);
                }
                else
                {
                    // 伤害小于格挡，只减少条目数量
                    entry.Amount -= damageTaken;
                    damageTaken = 0;
                    blockEntries[i] = entry; // 更新列表中的结构体
                }
            }
            
            // ⭐ 关键：格挡消耗后触发事件 ⭐
            if (CurrentBlock != initialTotalBlock)
            {
                OnBlockChanged?.Invoke(); 
            }
        }
        
        return damageTaken;
    }

    // AnimateDamage 方法保持不变

    /// <summary>
    /// 获得格挡。计算敏捷修正。
    /// </summary>
    /// <param name="amount">基础格挡值。</param>
    /// <param name="duration">格挡持续的回合数。默认 1 (下一回合开始清除)。</param>
    public virtual void AddBlock(int amount, int duration = 1)
    {
        if (isDead) return;
        
        int finalBlock = amount;
        
        finalBlock += GetStatusEffectAmount(StatusEffect.Dexterity);
        finalBlock = Mathf.Max(0, finalBlock);
        
        if (finalBlock <= 0) return;
        
        // ⭐ 核心修正：添加到 blockEntries 列表 ⭐
        blockEntries.Add(new BlockEntry { Amount = finalBlock, Duration = duration });
        
        OnBlockChanged?.Invoke(); 
        
        Debug.Log($"{characterName} 获得 {finalBlock} 最终格挡。总格挡: {CurrentBlock}。持续: {duration} 回合。");
    }

    // ⭐ 旧的 ClearBlock 方法被移除或标记为弃用，以防止错误调用。 ⭐
    // public virtual void ClearBlock() { /* 逻辑已转移到 DecrementBlockDuration */ }
    
    // ... Heal 和 Die 方法保持不变 ...

    /// <summary>
    /// 治疗。
    /// </summary>
    public virtual void Heal(int amount)
    {
        if (isDead) return;
        
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        
        Debug.Log($"{characterName} 治疗 {amount}。当前 HP: {currentHp}");
        
        OnHealthChanged?.Invoke(currentHp, maxHp); 
    }
    
    /// <summary>
    /// 角色死亡。
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"{characterName} 已被击败。");
        // TODO: 通知 BattleManager 死亡事件
    }
}