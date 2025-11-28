using UnityEngine;
using System.Collections.Generic;

public class CharacterBase : MonoBehaviour
{
    [Header("Base Stats")]
    public string characterName = "Character";
    public int maxHp = 100;
    public int currentHp;
    public int block;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        int damageTaken = amount;
        
        // 先用格挡抵消伤害
        if (block > 0)
        {
            damageTaken = Mathf.Max(0, amount - block);
            block = Mathf.Max(0, block - amount);
        }

        currentHp -= damageTaken;
        Debug.Log($"{characterName} takes {damageTaken} damage. HP remaining: {currentHp}. Block remaining: {block}");
        
        if (currentHp <= 0)
        {
            Die();
        }
        
        // 战斗结束后检查状态
        BattleManager.Instance?.CheckBattleEnd();
    }

    // CardData.cs 依赖的方法
    public void AddBlock(int amount)
    {
        block += amount;
        Debug.Log($"{characterName} gains {amount} block. Total block: {block}");
    }

    // CardData.cs 依赖的方法
    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        Debug.Log($"{characterName} heals for {amount}. Current HP: {currentHp}");
    }
    
    public void ClearBlock()
    {
        block = 0;
    }

    protected virtual void Die()
    {
        Debug.Log($"{characterName} has been defeated.");
        // 在这里可以添加死亡动画或销毁逻辑
    }
    
    // 敌人 AI 意图相关属性/方法
    // 仅在 EnemyAI 中使用，CharacterBase 仅作为基类
    // public IntentType nextIntent { get; set; }
    // public int intentValue { get; set; }
}