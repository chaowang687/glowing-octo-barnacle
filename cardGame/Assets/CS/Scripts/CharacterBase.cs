using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 引入 Linq 用于字典操作

/// <summary>
/// 角色基类 (英雄或敌人)。包含核心属性和战斗方法，并全面支持状态效果。
/// </summary>
public class CharacterBase : MonoBehaviour
{
    [Header("Base Stats")]
    public string characterName = "Character";
    public int maxHp = 100;
    public int currentHp;
    public int block;
    
    // 状态效果列表：存储当前生效的状态效果及其层数
    protected Dictionary<CardEnums.StatusEffect, int> statusEffects = new Dictionary<CardEnums.StatusEffect, int>();

    private void Awake()
    {
        currentHp = maxHp;
    }

    // --- 状态效果处理 ---

    /// <summary>
    /// 获取指定状态效果的层数。
    /// </summary>
    public int GetStatusEffectAmount(CardEnums.StatusEffect effect)
    {
        return statusEffects.TryGetValue(effect, out int amount) ? amount : 0;
    }

    /// <summary>
    /// 应用状态效果 (Buff/Debuff)。(CardData.cs 依赖的方法)
    /// </summary>
    /// <param name="effect">状态效果枚举类型。</param>
    /// <param name="duration">持续回合数或层数。</param>
    public void ApplyStatusEffect(CardEnums.StatusEffect effect, int duration)
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
            if (effect == CardEnums.StatusEffect.Strength || effect == CardEnums.StatusEffect.Dexterity || effect == CardEnums.StatusEffect.Metallicize)
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
    
    // --- 回合钩子 (CharacterManager.cs 依赖的方法) ---

    /// <summary>
    /// 回合开始时执行的逻辑 (例如中毒伤害)。
    /// </summary>
    public void AtStartOfTurn()
    {
        // 1. 中毒 (Poison) 伤害
        int poisonAmount = GetStatusEffectAmount(CardEnums.StatusEffect.Poison);
        if (poisonAmount > 0)
        {
            // 中毒伤害不是攻击，不触发易伤
            TakeDamage(poisonAmount, false); 
            
            // 中毒层数减少 1
            if (statusEffects.ContainsKey(CardEnums.StatusEffect.Poison))
            {
                statusEffects[CardEnums.StatusEffect.Poison] = Mathf.Max(0, statusEffects[CardEnums.StatusEffect.Poison] - 1);
                if (statusEffects[CardEnums.StatusEffect.Poison] <= 0)
                {
                    statusEffects.Remove(CardEnums.StatusEffect.Poison);
                }
            }
            Debug.Log($"{characterName} 受到 {poisonAmount} 点中毒伤害。");
        }
    }

    /// <summary>
    /// 回合结束时执行的逻辑 (例如清除格挡, 金属化格挡)。
    /// </summary>
    public void AtEndOfTurn()
    {
        // 1. 清除格挡
        ClearBlock();
        
        // 2. 金属化 (Metallicize) - 获得格挡
        int metallicizeAmount = GetStatusEffectAmount(CardEnums.StatusEffect.Metallicize);
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
    /// 攻击目标，计算虚弱和力量修正。(CardData.cs 和 EnemyAI.cs 依赖的方法)
    /// </summary>
    public void PerformAttack(CharacterBase target, int baseDamage)
    {
        int finalDamage = baseDamage;
        
        // 1. 力量 (Strength) 修正 (攻击力 + 力量层数)
        finalDamage += GetStatusEffectAmount(CardEnums.StatusEffect.Strength);
        
        // 2. 虚弱 (Weak) 修正 (攻击者是施放者，伤害降低 25%)
        if (GetStatusEffectAmount(CardEnums.StatusEffect.Weak) > 0)
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
    public void TakeDamage(int amount, bool isAttack = true)
    {
        int damageTaken = amount;
        
        // 1. 易伤 (Vulnerable) 修正 (受到的攻击伤害增加 50%)
        if (isAttack && GetStatusEffectAmount(CardEnums.StatusEffect.Vulnerable) > 0)
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
    /// 获得格挡。计算敏捷和脆弱修正。(CardData.cs 和 EnemyAI.cs 依赖的方法)
    /// </summary>
    /// <param name="amount">格挡值。</param>
    public void AddBlock(int amount)
    {
        int finalBlock = amount;
        
        // 1. 敏捷 (Dexterity) 修正 (格挡值 + 敏捷层数)
        finalBlock += GetStatusEffectAmount(CardEnums.StatusEffect.Dexterity);
        
        // 2. 脆弱 (Frail) 修正 (获得的格挡减少 25%)
        if (GetStatusEffectAmount(CardEnums.StatusEffect.Frail) > 0)
        {
            finalBlock = (int)(finalBlock * 0.75f);
            Debug.Log($"{characterName} 处于脆弱状态，格挡值减少 25%。");
        }

        finalBlock = Mathf.Max(0, finalBlock);
        
        block += finalBlock;
        Debug.Log($"{characterName} 获得 {finalBlock} 最终格挡。总格挡: {block}");
    }

    /// <summary>
    /// 治疗 (CardData.cs 依赖的方法)。
    /// </summary>
    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        Debug.Log($"{characterName} 治疗 {amount}。当前 HP: {currentHp}");
    }
    
    /// <summary>
    /// 清除所有格挡 (通常在回合结束时调用)。
    /// </summary>
    public void ClearBlock()
    {
        block = 0;
        // 仅在 BattleManager 回合结束时清除格挡，不需要重复 Debug.Log
    }
    
    /// <summary>
    /// 角色死亡。
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log($"{characterName} 已被击败。");
        // TODO: 添加死亡动画或销毁逻辑
    }
}