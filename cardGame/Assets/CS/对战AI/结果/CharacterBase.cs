using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using CardDataEnums; // 导入卡牌枚举所在的命名空间

/// <summary>
/// 角色基类 (英雄或敌人)。包含核心属性和战斗方法，并全面支持状态效果。
/// </summary>
public class CharacterBase : MonoBehaviour
{
    // 将这些字段设置为 public 或 protected，以便子类访问
    [Header("Base Stats")]
    public string characterName = "Character";
    public int maxHp = 100; // 暴露给子类访问
    public int currentHp;    // 暴露给子类访问
    public int block;
    public bool isDead = false; // 暴露给子类访问
    
    // 状态效果列表：存储当前生效的状态效果及其层数
    protected Dictionary<StatusEffect, int> statusEffects = new Dictionary<StatusEffect, int>();

    protected virtual void Awake()
    {
        currentHp = maxHp;
        isDead = false;
        block = 0;
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
    /// <param name="effect">状态效果枚举类型。</param>
    /// <param name="duration">持续回合数或层数。</param>
    public virtual void ApplyStatusEffect(StatusEffect effect, int duration) // 方法名修正为 ApplyStatusEffect
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
        // 需要复制键列表，因为迭代时不能修改字典
        var keys = statusEffects.Keys.ToList();
        
        foreach (var effect in keys)
        {
            // 力量、敏捷、金属化被视为永久或回合后特殊处理的状态，不在此处自动减少
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
            // 中毒伤害不是攻击，不触发易伤
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
    public virtual void AtEndOfTurn() // 方法名修正为 AtEndOfTurn
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

        target.TakeDamage(finalDamage);
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
        
        // 1. 易伤 (Vulnerable) 修正 (受到的攻击伤害增加 50%)
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

        currentHp -= damageTaken;
        Debug.Log($"{characterName} 受到 {damageTaken} 最终伤害。HP 剩余: {currentHp}。格挡剩余: {block}");
        
        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 获得格挡。计算敏捷和虚弱修正。
    /// </summary>
    /// <param name="amount">格挡值。</param>
    public virtual void AddBlock(int amount)
    {
        if (isDead) return;
        
        int finalBlock = amount;
        
        // 1. 敏捷 (Dexterity) 修正
        finalBlock += GetStatusEffectAmount(StatusEffect.Dexterity);
        
        // 2. 虚弱 (Weak) 修正 (获得的格挡减少 25%)
        if (GetStatusEffectAmount(StatusEffect.Weak) > 0)
        {
            finalBlock = (int)(finalBlock * 0.75f);
            Debug.Log($"{characterName} 处于虚弱状态，格挡值减少 25%。");
        }

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
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        Debug.Log($"{characterName} 治疗 {amount}。当前 HP: {currentHp}");
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