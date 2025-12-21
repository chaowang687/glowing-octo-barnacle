using UnityEngine;
using CardDataEnums; // 假设这是您定义的枚举命名空间

using System.Collections.Generic;

/// <summary>
/// 英雄角色类。继承自 CharacterBase。
/// 负责处理卡牌系统交互、能量管理和职业特性。
/// </summary>
public class Hero : CharacterBase
{
    [Header("Dependencies")]
    [Tooltip("Reference to the Card System in the combat.")]
    // NOTE: This CardSystem reference needs to be set up in the Unity Inspector.
    public CardSystem cardSystem; 

    [Header("Hero Specific Attributes")]
    [Tooltip("The class of the hero, used for card restrictions.")]
    public CardClass heroClass = CardClass.Ironclad; 
    
    [Tooltip("Base energy gained per turn.")]
    public int baseEnergy = 3;

    /// <summary>
    /// Hero character initialization. 使用 new 关键字隐藏基类的 Awake 方法。
    /// </summary>
    protected new void Awake() 
    {
        base.Awake(); // Calls CharacterBase's Awake() to set initial HP etc.
        characterName = "Hero"; // Default name for the player character
    }

    /// <summary>
    /// Handles the start of the Hero's turn (usually called by BattleManager).
    /// </summary>
    public void StartTurn()
    {
        // 1. CharacterBase handles AtStartOfTurn (e.g., Poison damage)
        AtStartOfTurn();
        
        if (cardSystem != null)
        {
            // 2. Reset energy
            cardSystem.ResetEnergy();
            
            // 3. Draw cards (assuming 5 cards per turn)
            cardSystem.DrawCards(5);
        }
    }

    /// <summary>
    /// Handles the end of the Hero's turn (usually called by BattleManager).
    /// </summary>
    public void EndTurn()
    {
        if (cardSystem != null)
        {
            // 1. Discard all cards remaining in hand
            cardSystem.DiscardHand();
        }
        
        // 2. CharacterBase handles AtEndOfTurn (e.g., Block clear, status decay, Metallicize)
        AtEndOfTurn();
    }

    /// <summary>
    /// Simple wrapper to apply status effect from a card.
    /// </summary>
    public void ApplyStatusEffectFromCard(StatusEffect effect, int amount, CharacterBase target)
    {
        // ⭐ 修正：由于 CharacterBase.ApplyStatusEffect 临时使用 int 作为第一个参数，
        // 我们需要将 StatusEffect 枚举转换为 int 以匹配签名。
        target.ApplyStatusEffect((int)effect, amount);
    }
    
    public override void Die()
    {

        // 调用基类方法，该方法现在只触发 OnCharacterDied 事件
        base.Die();
        Debug.Log("Hero has been defeated. Game Over.");
        // TODO: Trigger Game Over state
    }
}