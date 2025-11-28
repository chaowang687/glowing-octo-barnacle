using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCard", menuName = "Battle System/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName = "New Card";
    [TextArea]
    public string description = "Card description.";
    public Sprite artwork;

    [Header("Cost and Type")]
    public int energyCost = 1;
    public CardType type = CardType.Attack; // 假设 CardType 在 Enums.cs 或其他地方已定义

    [Header("Actions")]
    // 之前报错的 CardAction 列表
    public List<CardAction> actions = new List<CardAction>();

    // --- 核心方法 ---
    
    // 执行卡牌效果
    public void ExecuteEffects(CharacterBase source, CharacterBase target, CardSystem cardSystem)
    {
        if (cardSystem == null)
        {
            Debug.LogError("CardSystem is null during card execution.");
            return;
        }

        foreach (var action in actions)
        {
            List<CharacterBase> actualTargets = GetActualTargets(source, target, cardSystem, action.targetType);
            
            foreach (var actualTarget in actualTargets)
            {
                ApplyAction(source, actualTarget, action, cardSystem);
            }
        }
    }

    private List<CharacterBase> GetActualTargets(CharacterBase source, CharacterBase selectedTarget, CardSystem cardSystem, TargetType targetType)
    {
        List<CharacterBase> targets = new List<CharacterBase>();
        CharacterManager manager = cardSystem.GetComponent<CharacterManager>();

        if (manager == null) return targets;

        switch (targetType)
        {
            case TargetType.Self:
                targets.Add(source);
                break;
            case TargetType.SelectedEnemy:
            case TargetType.SelectedAlly:
            case TargetType.SelectedCharacter:
                if (selectedTarget != null) targets.Add(selectedTarget);
                break;
            case TargetType.AllEnemies:
                targets.AddRange(manager.GetAllEnemies());
                break;
            case TargetType.AllAllies:
                targets.AddRange(manager.GetAllHeroes());
                break;
            case TargetType.AllCharacters:
                targets.AddRange(manager.GetAllHeroes());
                targets.AddRange(manager.GetAllEnemies());
                break;
        }
        return targets;
    }

    private void ApplyAction(CharacterBase source, CharacterBase target, CardAction action, CardSystem cardSystem)
    {
        if (target == null) return;

        switch (action.effectType)
        {
            case EffectType.Attack:
                // 简化处理：应用伤害
                target.TakeDamage(action.value);
                Debug.Log($"{source.characterName} Attacks {target.characterName} for {action.value} damage.");
                break;
            case EffectType.Block:
                // 简化处理：应用格挡
                source.AddBlock(action.value);
                Debug.Log($"{source.characterName} gains {action.value} Block.");
                break;
            case EffectType.Heal:
                // 简化处理：治疗
                target.Heal(action.value);
                Debug.Log($"{source.characterName} Heals {target.characterName} for {action.value} HP.");
                break;
            case EffectType.DrawCard:
                // 简化处理：抽卡
                cardSystem.DrawCards(action.value);
                Debug.Log($"{source.characterName} draws {action.value} cards.");
                break;
            case EffectType.Energy:
                // 简化处理：获得能量
                cardSystem.GainEnergy(action.value);
                Debug.Log($"{source.characterName} gains {action.value} Energy.");
                break;
            // Buff/Debuff 逻辑需要一个完整的 StatusEffectSystem，这里仅作占位符
            case EffectType.ApplyBuff:
            case EffectType.ApplyDebuff:
                Debug.Log($"Applied status effect '{action.statusEffectName}' to {target.characterName} for {action.duration} turns.");
                break;
        }
    }
}

// 假设 CardType 尚未定义，先在此处定义一个简单的枚举
public enum CardType
{
    Attack,
    Skill,
    Power
}