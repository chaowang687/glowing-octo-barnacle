using UnityEngine;
using CardDataEnums;

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
    /// Hero character initialization.
    /// </summary>
    protected new void Awake() // <-- 移除 override
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
        target.ApplyStatusEffect(effect, amount);
    }
    
    protected override void Die()
    {
        base.Die();
        Debug.Log("Hero has been defeated. Game Over.");
        // TODO: Trigger Game Over state
    }
}