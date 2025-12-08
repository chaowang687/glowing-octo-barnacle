using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using CardDataEnums; // 假设您已定义 StatusEffect 枚举
using System; // 引入 System 命名空间

/// <summary>
/// 角色基类 (英雄或敌人)。包含核心属性和战斗方法，并全面支持状态效果。
/// </summary>
public class CharacterBase : MonoBehaviour
{
    // ⭐ 核心：定义事件，用于通知 UI 更新 (血条变化) ⭐
    public event Action<int, int> OnHealthChanged;
    
    [Header("Base Stats")]
    public string characterName = "Character";
    
    // ⭐ 修正：统一使用字段 (解决 CS1061/CS0103) ⭐
    public int maxHp = 100; // 最大生命值
    public int currentHp; // 当前生命值
    
    public int block; // 格挡值
    public bool isDead = false; 
    
    // 状态效果列表：存储当前生效的状态效果及其层数
    protected Dictionary<StatusEffect, int> statusEffects = new Dictionary<StatusEffect, int>();

    protected virtual void Awake()
    {
        // 字段初始化
        currentHp = maxHp; 
        isDead = false;
        block = 0;
    }

    /// <summary>
    /// GameFlowManager 依赖的初始化方法。
    /// </summary>
    public virtual void Initialize(string name, int maxHp, Sprite artwork)
    {
        // ⭐ 修正：使用字段进行赋值 ⭐
        this.maxHp = maxHp;
        this.currentHp = maxHp;
        this.characterName = name;
        
        // 首次初始化时通知 UI
        OnHealthChanged?.Invoke(currentHp, this.maxHp);
    }
    
    // --- 状态效果处理 ---

    /// <summary>
    /// 获取指定状态效果的层数。
    /// </summary>
    public int GetStatusEffectAmount(StatusEffect effect)
    {
        return statusEffects.TryGetValue(effect, out int amount) ? amount : 0;
    }

    /// <summary>
    /// 应用状态效果 (Buff/Debuff)。
    /// </summary>
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

    /// <summary>
    /// 回合结束时减少非永久性状态效果的层数。
    /// </summary>
    protected void DecreaseStatusDurations()
    {
        var keys = statusEffects.Keys.ToList();
        
        foreach (var effect in keys)
        {
            // 力量、敏捷、金属化被视为永久或回合后特殊处理的状态
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

    /// <summary>
    /// 回合开始时执行的逻辑 (例如中毒伤害)。
    /// </summary>
    public virtual void AtStartOfTurn()
    {
        if (isDead) return;
        
        // 1. 中毒 (Poison) 伤害
        int poisonAmount = GetStatusEffectAmount(StatusEffect.Poison);
        if (poisonAmount > 0)
        {
            // 调用 TakeDamage(amount, isAttack: false) 来处理中毒伤害
            TakeDamage(poisonAmount, isAttack: false); 
            
            // 中毒层数减少 1
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

    /// <summary>
    /// 回合结束时执行的逻辑 (例如清除格挡, 金属化格挡)。
    /// </summary>
    public virtual void AtEndOfTurn() 
    {
        if (isDead) return;

        // 1. 清除格挡
        ClearBlock();
        
        // 2. 金属化 (Metallicize) - 获得格挡
        int metallicizeAmount = GetStatusEffectAmount(StatusEffect.Metallicize);
        if (metallicizeAmount > 0)
        {
            AddBlock(metallicizeAmount);
            Debug.Log($"{characterName} 因金属化获得 {metallicizeAmount} 点格挡。");
        }
        
        // 3. 减少状态持续时间
        DecreaseStatusDurations();
    }
    
    // --- 核心战斗方法 ---

    /// <summary>
    /// 攻击目标，计算虚弱和力量修正。
    /// </summary>
    public virtual void PerformAttack(CharacterBase target, int baseDamage)
    {
        if (isDead) return;
        
        int finalDamage = baseDamage;
        
        // 1. 力量 (Strength) 修正
        finalDamage += GetStatusEffectAmount(StatusEffect.Strength);
        
        // 2. 虚弱 (Weak) 修正 (攻击者是施放者，伤害降低 25%)
        if (GetStatusEffectAmount(StatusEffect.Weak) > 0)
        {
            finalDamage = (int)(finalDamage * 0.75f);
            Debug.Log($"{characterName} 处于虚弱状态，伤害减少 25%。");
        }
        
        finalDamage = Mathf.Max(0, finalDamage);

        target.TakeDamage(finalDamage); // 调用 TakeDamage(int)
    }

    // ⭐ 修正：保留的 TakeDamage(int) 方法，委托给带参数的版本 ⭐
    /// <summary>
    /// 接收伤害逻辑。这是默认的攻击入口。
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        // 委托给带参数的版本，标记为攻击
        TakeDamage(damageAmount, isAttack: true); 
    }

    /// <summary>
    /// 接收伤害逻辑。计算易伤修正。
    /// </summary>
    /// <param name="amount">原始伤害值。</param>
    /// <param name="isAttack">是否为攻击伤害 (影响易伤计算)。</param>
    public virtual void TakeDamage(int amount, bool isAttack = true)
    {
        if (isDead) return;
        
        int damageTaken = amount;
        
        // 1. 易伤 (Vulnerable) 修正
        if (isAttack && GetStatusEffectAmount(StatusEffect.Vulnerable) > 0)
        {
            damageTaken = (int)(damageTaken * 1.5f);
            Debug.Log($"{characterName} 处于易伤状态，受到伤害增加 50%。");
        }
        
        // 2. 格挡抵消
        if (block > 0)
        {
            int damageAfterBlock = Mathf.Max(0, damageTaken - block);
            block = Mathf.Max(0, block - damageTaken);
            damageTaken = damageAfterBlock;
        }

        // ⭐ 修正：使用 currentHp 字段扣血 ⭐
        currentHp -= damageTaken;
        currentHp = Mathf.Max(0, currentHp); 
        
        Debug.Log($"{characterName} 受到 {damageTaken} 最终伤害。HP 剩余: {currentHp}。格挡剩余: {block}");
        
        // ⭐ 关键：触发事件，通知 UI 更新 ⭐
        OnHealthChanged?.Invoke(currentHp, maxHp); 
        
        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 获得格挡。计算敏捷修正。
    /// </summary>
    public virtual void AddBlock(int amount)
    {
        if (isDead) return;
        
        int finalBlock = amount;
        
        // 1. 敏捷 (Dexterity) 修正
        finalBlock += GetStatusEffectAmount(StatusEffect.Dexterity);
        
        // 2. 虚弱修正 (通常不影响格挡，这里注释掉，如需要请取消)
        // if (GetStatusEffectAmount(StatusEffect.Weak) > 0)
        // {
        //     finalBlock = (int)(finalBlock * 0.75f);
        // }

        finalBlock = Mathf.Max(0, finalBlock);
        
        block += finalBlock;
        Debug.Log($"{characterName} 获得 {finalBlock} 最终格挡。总格挡: {block}");
    }

    /// <summary>
    /// 治疗。
    /// </summary>
    public virtual void Heal(int amount)
    {
        if (isDead) return;
        
        // ⭐ 修正：使用 currentHp 和 maxHp 字段进行治疗 ⭐
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        
        Debug.Log($"{characterName} 治疗 {amount}。当前 HP: {currentHp}");
        
        // 触发 UI 更新
        OnHealthChanged?.Invoke(currentHp, maxHp); 
    }
    
    /// <summary>
    /// 清除所有格挡 (通常在回合结束时调用)。
    /// </summary>
    public void ClearBlock()
    {
        block = 0;
    }
    
    /// <summary>
    /// 角色死亡。
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"{characterName} 已被击败。");
        // TODO: 添加死亡动画或销毁逻辑
    }
}