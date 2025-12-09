using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using CardDataEnums; // 引入 CardEnums 中的所有静态成员，简化代码

// 假设 CardAction 结构体已在 CardAction.cs 文件中定义

/// <summary>
/// ScriptableObject 用于存储单张卡牌的所有静态数据，
/// 并包含卡牌效果在战斗中执行的运行时逻辑。
/// 它依赖于 CardAction 结构体和 CharacterBase.cs。
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Battle System/Card Data")]
public class CardData : ScriptableObject
{
    // =================================================================
    // 静态数据 (卡牌设计字段)
    // =================================================================
    
    [Header("核心信息")]
    [Tooltip("卡牌的唯一标识符，例如 W_STRIKE")]
    public string cardID = "";
    public string cardName = "新卡牌"; // 这是卡牌的名称
    
    [TextArea]
    public string description = "描述";
    public Sprite artwork;

    [Header("分类与稀有度 (设计优化)")]
    public Rarity rarity = Rarity.Common; 
    
    // ⭐ 修正点：将 CardEnums.CardClass.Any 替换为 CardClass.Any ⭐
    public CardClass requiredClass = CardClass.Any;
    
    [Header("成本与升级 (设计优化)")]
    public int energyCost = 1;
    public CardType type = CardType.Attack; 

    [Tooltip("卡牌是否为升级版本")]
    public bool isUpgraded = false; 

    [Tooltip("指向这张卡牌的升级版本资产。")]
    public CardData upgradedCardData; 

    [Header("行动效果")]
    // CardAction 结构体现在已定义在文件顶部
    public List<CardAction> actions = new List<CardAction>();

    // =================================================================
    // 运行时逻辑 (执行效果)
    // =================================================================
    
    /// <summary>
    /// 获取当前卡牌数据（可能是基础版或升级版）。
    /// </summary>
    public CardData GetCardData(bool upgraded)
    {
        if (upgraded && upgradedCardData != null)
        {
            return upgradedCardData;
        }
        return this;
    }

    /// <summary>
    /// 执行卡牌效果的主入口。
    /// </summary>
    public void ExecuteEffects(CharacterBase source, CharacterBase target, CardSystem cardSystem)
    {
        if (cardSystem == null)
        {
            Debug.LogError("CardSystem is null during card execution.");
            return;
        }
        if (source == null)
        {
            Debug.LogError("Source CharacterBase is null during card execution.");
            return;
        }

        foreach (var action in actions)
        {
            // 1. 计算受状态效果修正后的最终值
            int finalValue = CalculateFinalValue(source, action);
            // 临时调试日志：检查计算结果
            if (action.effectType == EffectType.Attack)
            {
                Debug.Log($"Action Type: Attack. Calculated Final Value: {finalValue}.");
            }
            // 2. 确定实际目标列表
            // 注意：action.targetType 已经在 CardAction 结构体中明确为 CardEnums.TargetType
            List<CharacterBase> actualTargets = GetActualTargets(source, target, cardSystem, action.targetType);
            
            // 3. 应用效果
            // Note: 对于无目标效果 (如抽卡/能量)，actualTargets 为空，但效果在 ApplyAction 中处理
            if (action.targetType == TargetType.None)
            {
                ApplyAction(source, null, action, cardSystem, finalValue);
            }
            else
            {
                // 确保至少有一个目标，否则跳过
                // 只有 TargetType.Self 允许 target 为空列表但 source 存在
                if (actualTargets.Count == 0 && action.targetType != TargetType.Self)
                {
                    Debug.LogWarning($"Card {cardName}: Expected targets but none found for type {action.targetType}. Skipping action.");
                    continue;
                }
                
                foreach (var actualTarget in actualTargets)
                {
                    ApplyAction(source, actualTarget, action, cardSystem, finalValue);
                }
            }
        }
    }

    /// <summary>
    /// 计算行动的最终数值，包括力量/敏捷修正。
    /// </summary>
    private int CalculateFinalValue(CharacterBase source, CardAction action)
    {
        int finalValue = action.value;
        
        if (action.scalesWithStatus)
        {
            switch (action.effectType)
            {
                case EffectType.Attack:
                    // 攻击受力量(Strength)影响
                    // 假设 CharacterBase 有 GetStatusEffectAmount 方法
                    int strength = source.GetStatusEffectAmount(StatusEffect.Strength);
                    finalValue += strength;
                    break;
                case EffectType.Block:
                    // 格挡受敏捷(Dexterity)影响
                    int dexterity = source.GetStatusEffectAmount(StatusEffect.Dexterity);
                    finalValue += dexterity;
                    break;
                // 其他效果（如Heal）可以根据游戏规则添加其他状态修正
            }
        }
        
        // 确保伤害和格挡值不会低于零 (除非特定卡牌效果允许)
        if (action.effectType == EffectType.Attack || action.effectType == EffectType.Block)
        {
            finalValue = Mathf.Max(0, finalValue);
        }

        return finalValue;
    }

    /// <summary>
    /// 根据 TargetType 确定实际的目标列表。
    /// </summary>
    private List<CharacterBase> GetActualTargets(CharacterBase source, CharacterBase selectedTarget, CardSystem cardSystem, TargetType targetType)
    {
        List<CharacterBase> targets = new List<CharacterBase>();
        
        // 尝试从 CardSystem 获取 CharacterManager
        // 假设 CharacterManager 是一个单例，或者 CardSystem 知道如何获取它
        CharacterManager manager = CharacterManager.Instance; 
        
        // 如果 CardSystem 挂载在 BattleManager 上，可以使用 GetComponent
        // CharacterManager manager = cardSystem.GetComponent<CharacterManager>(); 

        if (manager == null)
        {
            Debug.LogError("CharacterManager instance not found. Cannot determine All/Enemy targets.");
            return targets;
        }

        switch (targetType)
        {
            case TargetType.Self:
                targets.Add(source);
                break;
            case TargetType.SelectedEnemy:
            case TargetType.SelectedAlly:
            case TargetType.SelectedCharacter:
                // 如果是需要选中目标的类型，则只添加选中的目标
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
            case TargetType.None:
                // 无目标，列表为空
                break;
        }
        return targets;
    }

    /// <summary>
    /// 对目标应用单个 CardAction 效果。
    /// </summary>
    private void ApplyAction(CharacterBase source, CharacterBase target, CardAction action, CardSystem cardSystem, int finalValue)
    {
        // 对于需要角色的动作，如果目标为空，则返回 (TargetType.None 除外)
        if (target == null && action.targetType != TargetType.None && action.targetType != TargetType.Self) return;

        string sourceName = source.characterName; 
        string targetName = target != null ? target.characterName : "System"; 

        switch (action.effectType)
        {
            case EffectType.Attack:
                // ⭐ 核心修正 1：使用目标 (target) 的 TakeDamage 方法替代 PerformAttack ⭐
                if (target == null) return;
                // TakeDamage 负责计算格挡和触发动画
                target.TakeDamage(finalValue, isAttack: true);
                
                Debug.Log($"{sourceName} Attacks {targetName} for {finalValue} damage (Base: {action.value}, via {cardName}).");
                break;
            case EffectType.Block:
                // ⭐ 核心修正 2：使用新的 AddBlock 签名，默认 duration = 1 ⭐
                if (source == null) return;
                source.AddBlock(finalValue, 1);
                
                Debug.Log($"{sourceName} gains {finalValue} Block (Base: {action.value}, via {cardName}).");
                break;
            case EffectType.Heal:
                if (target == null) return;
                target.Heal(finalValue); 
                Debug.Log($"{sourceName} Heals {targetName} for {finalValue} HP (via {cardName}).");
                break;
            case EffectType.DrawCard:
                // 假设 CardSystem 有 DrawCards 方法
                cardSystem.DrawCards(finalValue);
                Debug.Log($"{sourceName} draws {finalValue} cards (via {cardName}).");
                break;
            case EffectType.Energy:
                // 假设 CardSystem 有 GainEnergy 方法
                cardSystem.GainEnergy(finalValue);
                Debug.Log($"{sourceName} gains {finalValue} Energy (via {cardName}).");
                break;
            case EffectType.ApplyBuff:
            case EffectType.ApplyDebuff:
                if (target == null) return;
                // 假设 target 有 ApplyStatusEffect 方法
                target.ApplyStatusEffect(action.statusEffect, action.duration); 
                string effectType = (action.effectType == EffectType.ApplyBuff) ? "Buff" : "Debuff";
                Debug.Log($"{sourceName} applies {effectType}: {action.statusEffect} ({action.duration} turns) to {targetName} (via {cardName}).");
                break;
            case EffectType.None: 
                // 无效果，跳过
                break;
        }
    }
}