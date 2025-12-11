using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System; 
using DG.Tweening; 

// Character state type
public enum CharacterType
{
    Hero,
    Enemy
}

// Block entry structure
[Serializable]
public struct BlockEntry
{
    public int Amount;
    [Tooltip("Duration of the block in turns. 1 means it is cleared at the start of the next turn (i.e., lasts only the current turn).")]
    public int Duration; 
}

/// <summary>
/// Character base class (Hero or Enemy). Contains core attributes and combat methods, with full support for status effects.
/// </summary>
public class CharacterBase : MonoBehaviour
{
    // --- Event Definitions ---
    // Parameters: (currentHp, maxHp, damageTaken)
    public event Action<int, int, int> OnHpChanged; 
    
    public event Action OnCharacterDied; // Triggered when the character dies
    public event Action<int, int> OnHealthChanged; // (currentHp, maxHp) Used to notify UI (e.g., health bar)
    public event Action OnBlockChanged; // Used to notify UI of block changes

    public bool IsDead => currentHp <= 0; // Property to check if dead
    
    [Header("Base Stats")]
    public string characterName = "Character";
    
    public int maxHp = 100; // Max HP
    public int currentHp; // Current HP
    
    [Header("Block Persistence")]
    // Tracks all block entries (amount and duration)
    private List<BlockEntry> blockEntries = new List<BlockEntry>();
    
    // Read-only property: calculates the current total block value
    public int CurrentBlock { get { return blockEntries.Sum(e => e.Amount); } }
    
    public bool isDead = false; 
    
    // Status effect list (simplified using Dictionary<int, int> assuming StatusEffect is an enum)
    protected Dictionary<int, int> statusEffects = new Dictionary<int, int>(); 

    protected virtual void Awake()
    {
        currentHp = maxHp; 
        isDead = false;
        blockEntries.Clear(); // Clear the block list upon initialization
    }

    /// <summary>
    /// Initialization method required by GameFlowManager.
    /// </summary>
    public virtual void Initialize(string name, int maxHp, Sprite artwork)
    {
        this.maxHp = maxHp;
        this.currentHp = maxHp;
        this.characterName = name;
        OnHealthChanged?.Invoke(currentHp, this.maxHp);
    }
    
    // --- Status Effect Handling (simplified logic for compatibility) ---
    public int GetStatusEffectAmount(int effect) // StatusEffect effect
    {
        return statusEffects.TryGetValue(effect, out int amount) ? amount : 0;
    }
    
    public virtual void ApplyStatusEffect(int effect, int duration) // StatusEffect effect
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
        Debug.Log($"{characterName} gained StatusEffect({effect}), stacks: {statusEffects[effect]}");
    }

    protected void DecreaseStatusDurations()
    {
        var keys = statusEffects.Keys.ToList();
        
        foreach (var effect in keys)
        {
            // Assuming Strength/Dexterity/Metallicize have int values 1, 2, 3, and they do not decrease
            if (effect == 1 || effect == 2 || effect == 3) // StatusEffect.Strength, StatusEffect.Dexterity, StatusEffect.Metallicize
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

    // --- Turn Hooks ---

    public virtual void AtStartOfTurn()
    {
        if (IsDead) return;
        
        int poisonEffectId = 4; // Assuming StatusEffect.Poison = 4
        int poisonAmount = GetStatusEffectAmount(poisonEffectId);
        if (poisonAmount > 0)
        {
            TakeDamage(poisonAmount, isAttack: false)?.OnComplete(() =>
            {
                if (statusEffects.ContainsKey(poisonEffectId))
                {
                    statusEffects[poisonEffectId] = Mathf.Max(0, statusEffects[poisonEffectId] - 1);
                    if (statusEffects[poisonEffectId] <= 0)
                    {
                        statusEffects.Remove(poisonEffectId);
                    }
                }
                Debug.Log($"{characterName} took {poisonAmount} Poison damage, Poison stacks reduced.");
            });
        }
    }

    public virtual void AtEndOfTurn() 
    {
        if (IsDead) return;
        
        int metallicizeEffectId = 3; // Assuming StatusEffect.Metallicize = 3
        int metallicizeAmount = GetStatusEffectAmount(metallicizeEffectId);
        if (metallicizeAmount > 0)
        {
            // AddBlock, duration set to 2 (current turn + cleared at start of next turn)
            AddBlock(metallicizeAmount, duration: 2); 
            Debug.Log($"{characterName} gained {metallicizeAmount} Block from Metallicize (lasts 1 turn).");
        }
        
        DecrementBlockDuration(); // Decrement block duration at end of turn
        DecreaseStatusDurations(); // Decrement status duration at end of turn
    }
    
    /// <summary>
    /// Called at the end of the turn to decrease the duration of all non-permanent blocks and clear expired ones.
    /// </summary>
    public void DecrementBlockDuration()
    {
        if (IsDead) return;

        bool wasBlockCleared = false;
        
        for (int i = blockEntries.Count - 1; i >= 0; i--)
        {
            // Duration <= 0 means permanent block (e.g., Buffer effect), do not decrement
            if (blockEntries[i].Duration <= 0) continue; 
            
            // Decrement duration
            blockEntries[i] = new BlockEntry { 
                Amount = blockEntries[i].Amount, 
                Duration = blockEntries[i].Duration - 1 
            };
            
            // Check if it should be cleared
            if (blockEntries[i].Duration <= 0)
            {
                int clearedAmount = blockEntries[i].Amount;
                blockEntries.RemoveAt(i);
                wasBlockCleared = true;
                Debug.Log($"{characterName}'s {clearedAmount} Block cleared due to end of duration.");
            }
        }
        
        if (wasBlockCleared)
        {
            OnBlockChanged?.Invoke();
        }
    }
    
    // --- Core Combat Method: Damage Entry ---

    /// <summary>
    /// Primary damage receiving method, returns a DOTween Sequence for animation and async handling.
    /// </summary>
    public virtual Sequence TakeDamage(int amount, bool isAttack = true)
    {
        if (IsDead) 
        {
            Debug.Log($"[DAMAGE FLOW] {characterName} is already dead, ignoring damage.");
            return DOTween.Sequence();
        }
        if (amount <= 0)
        {
            Debug.Log($"[DAMAGE FLOW] Damage amount is 0 or less, ignoring.");
            return DOTween.Sequence();
        }

        // 1. Pure Data Calculation: Calculate final damage taken based on block/vulnerable, and consume block
        int finalDamageTaken = CalculateDamage(amount, isAttack);
        
        // 2. Animation Settlement: Execute UI animation, and perform actual HP deduction after animation completion
        return AnimateDamage(finalDamageTaken);
    }

    /// <summary>
    /// Pure Data Calculation: Calculates final damage taken based on vulnerable and block, and consumes block.
    /// </summary>
    /// <returns>The actual damage value taken.</returns>
    private int CalculateDamage(int amount, bool isAttack)
    {
        int damageTaken = amount;
        int initialTotalBlock = CurrentBlock; // Record initial total block

        int vulnerableEffectId = 5; // Assuming StatusEffect.Vulnerable = 5
        
        // 1. Vulnerable Correction
        if (isAttack && GetStatusEffectAmount(vulnerableEffectId) > 0)
        {
            // Vulnerable: damage increased by 50%
            damageTaken = (int)(damageTaken * 1.5f);
            Debug.Log($"[DAMAGE CALC] {characterName} is Vulnerable, damage corrected to {damageTaken}.");
        }
        
        // 2. Core Correction: Consume block from the block entry list
        if (CurrentBlock > 0)
        {
            Debug.Log($"[DAMAGE CALC] Attempting to consume {characterName}'s Block ({CurrentBlock}).");
            // Iterate backwards for safe removal
            for (int i = blockEntries.Count - 1; i >= 0 && damageTaken > 0; i--)
            {
                BlockEntry entry = blockEntries[i];
                
                if (damageTaken >= entry.Amount)
                {
                    // Damage is greater than block, consume the entire entry
                    damageTaken -= entry.Amount;
                    blockEntries.RemoveAt(i);
                }
                else
                {
                    // Damage is less than block, only reduce the entry amount
                    entry.Amount -= damageTaken;
                    damageTaken = 0;
                    blockEntries[i] = entry; // Update the struct in the list
                }
            }
            
            // Crucial: Trigger event after block consumption
            if (CurrentBlock != initialTotalBlock)
            {
                OnBlockChanged?.Invoke(); 
                Debug.Log($"[DAMAGE CALC] {characterName} Block consumed, triggering OnBlockChanged.");
            }
        }
        
        return damageTaken;
    }

    /// <summary>
    /// Damage settlement animation, and final HP deduction.
    /// </summary>
    private Sequence AnimateDamage(int finalDamage)
    {
        if (finalDamage <= 0) 
        {
            Debug.Log($"[DAMAGE FLOW] {characterName} final damage is 0, returning empty Sequence.");
            return DOTween.Sequence();
        }
        
        Sequence damageSequence = DOTween.Sequence();
        
        // Sample: Character color flash animation (add your SpriteRenderer or Image reference if needed)
        // damageSequence.Append(GetComponent<SpriteRenderer>().DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo));
        
        // Final Callback: Actually deduct HP and update UI
        damageSequence.AppendCallback(() => 
        {
            // Execute final HP deduction
            int previousHp = currentHp;
            currentHp -= finalDamage;
            currentHp = Mathf.Max(0, currentHp); 
            
            // Calculate actual HP deducted (for event notification)
            int actualDamageTaken = previousHp - currentHp;

            Debug.Log($"[DAMAGE FINISH] {characterName} took {finalDamage} final damage. HP remaining: {currentHp}.");
            
            // Trigger events to notify UI update
            OnHpChanged?.Invoke(currentHp, maxHp, actualDamageTaken);
            Debug.Log($"[DAMAGE FINISH] **OnHpChanged event sent from CharacterBase**."); 
            OnHealthChanged?.Invoke(currentHp, maxHp); 
            
            // Death Check
            if (currentHp <= 0)
            {
                Die();
            }
        });
        
        return damageSequence;
    }

    /// <summary>
    /// Gain block. Calculates Dexterity correction.
    /// </summary>
    public virtual void AddBlock(int amount, int duration = 2)
    {
        if (IsDead) return;
        
        int finalBlock = amount;
        
        int dexterityEffectId = 2; // Assuming StatusEffect.Dexterity = 2
        
        // Dexterity correction
        finalBlock += GetStatusEffectAmount(dexterityEffectId);
        finalBlock = Mathf.Max(0, finalBlock);
        
        if (finalBlock <= 0) return;
        
        // Core Correction: Add to blockEntries list
        blockEntries.Add(new BlockEntry { Amount = finalBlock, Duration = duration });
        
        OnBlockChanged?.Invoke(); 
        
        Debug.Log($"{characterName} gained {finalBlock} final Block. Total Block: {CurrentBlock}. Duration: {duration} turns.");
    }

    /// <summary>
    /// Heal.
    /// </summary>
    public virtual void Heal(int amount)
    {
        if (IsDead) return;
        
        int previousHp = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        int amountHealed = currentHp - previousHp;
        
        Debug.Log($"{characterName} healed {amountHealed}. Current HP: {currentHp}");
        
        // Healing also uses OnHpChanged event, but damage value is negative (represents recovery)
        OnHpChanged?.Invoke(currentHp, maxHp, -amountHealed);
        OnHealthChanged?.Invoke(currentHp, maxHp); 
    }
    
    /// <summary>
    /// Character death.
    /// This version ensures synchronous removal from BattleManager after a slight animation delay.
    /// </summary>
    public virtual void Die()
    {
        if (isDead) return; 
        
        isDead = true;
        Debug.Log($"[DEATH] {characterName} has been defeated. Starting synchronous cleanup process.");
        
        // 1. Immediately trigger the death event (EnemyDisplay starts animation)
        OnCharacterDied?.Invoke(); 
        
        // 2. ⭐ CORE FIX: Delay a short period (e.g., 0.5s) to allow the visual animation to start ⭐
        // After this delay, we assume the animation is playing and perform the crucial data cleanup.
        float deathAnimationDuration = 0.5f; // Adjust this value to match your shortest death animation time
        
        // Use DelayedCall to run the cleanup after the visual start
        DOVirtual.DelayedCall(deathAnimationDuration, () =>
        {
            Debug.Log($"[DEATH CLEANUP DELAYED] {characterName} delay complete. Performing data cleanup.");

            // 3. ⭐ CRUCIAL: Immediately notify BattleManager to remove the character from the active list ⭐
            if (BattleManager.Instance != null)
            {
                // We assume BattleManager has a method to remove the character and check end-of-battle
                // This call is SYNCHRONOUS and ensures the character is removed immediately from the active combatants.
                BattleManager.Instance.HandleDyingCharacterCleanup(this);
                Debug.Log($"[DEATH CLEANUP DELAYED] BattleManager notified to remove {characterName} from active list.");
            }

        }).SetId(this); // Set DOTween ID to manage/kill if needed
    }
    
    // Optional: Ensure all DOTween calls related to this character are stopped on destroy
    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}