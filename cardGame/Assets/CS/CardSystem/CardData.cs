using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CardDataEnums; 

/// <summary>
/// ScriptableObject 存储卡牌数据与执行逻辑
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Battle System/Card Data")]
public class CardData : ScriptableObject
{
    [Header("核心信息")]
    public string cardID = "";
    public string cardName = "新卡牌"; // 备份字段，用于初始化本地化键
    [TextArea] public string description = "描述"; // 备份字段，用于初始化本地化键
    
    [Header("本地化设置")]
    public string cardNameKey = "card_default_name";
    public string descriptionKey = "card_default_description";
    
    public Sprite artwork;

    [Header("分类与稀有度")]
    public Rarity rarity = Rarity.Common;
    public CardClass requiredClass = CardClass.Any;
    
    [Header("成本与升级")]
    public int energyCost = 1;
    public CardType type = CardType.Attack;
    public bool isUpgraded = false;
    public CardData upgradedCardData;

    [Header("行动效果")]
    public List<CardAction> actions = new List<CardAction>();

    // =================================================================
    // 逻辑执行入口
    // =================================================================

    public void ExecuteAllActions(CharacterBase source, CharacterBase selectedTarget, CardSystem cardSystem)
    {
        if (cardSystem == null) return;
        cardSystem.StartCoroutine(ExecuteAllActionsCoroutine(source, selectedTarget, cardSystem));
       
    }

    private IEnumerator ExecuteAllActionsCoroutine(CharacterBase source, CharacterBase selectedTarget, CardSystem cardSystem)
    {
        foreach (var action in actions)
        {
            int count = Mathf.Max(1, action.repeatCount);
            
            for (int i = 0; i < count; i++)
            {
                // 1. 播放攻击冲锋动画 (解决报错)
                if (action.effectType == EffectType.Attack)
                {
                    // 检查 GameFlowManager 是否包含此方法。如果报错依然存在，说明你的 GameFlowManager 里确实没写这个函数
                    // 建议检查方法名是否拼写错误，或者是否应该在 source.PlayAttackAnimation() 中处理
                    /* if (GameFlowManager.Instance != null) {
                        GameFlowManager.Instance.PlayAttackAnimation(source.GetComponent<RectTransform>());
                    } 
                    */
                }

                // 2. 确定目标并结算
                List<CharacterBase> targets = GetActualTargets(source, selectedTarget, action.targetType);
                int finalValue = CalculateFinalValue(source, action);

                // 修正：抽牌和回能不需要“目标”，只要 action 类型匹配就执行
                // 之前的逻辑只判断了 "targets.Count == 0 && action.targetType != TargetType.None"，
                // 这意味着如果你设置了 TargetType.SelectedEnemy（为了打伤害），targets 列表不为空，
                // 但下方的 foreach 循环里又没有对 EffectType.DrawCard 做特殊处理，
                // 导致抽牌逻辑被错误地绑定在“对每个目标执行一次”里，或者因为目标类型判断失误而被跳过。
                
                // 拆分处理逻辑：
                // 全局效果（抽牌、能量）直接执行一次
                if (action.effectType == EffectType.DrawCard || action.effectType == EffectType.Energy)
                {
                    ApplyAction(source, null, action, cardSystem, finalValue);
                }
                // 指向性效果（伤害、格挡、Buff）对每个目标执行
                else if (targets.Count > 0)
                {
                    foreach (var t in targets)
                    {
                        ApplyAction(source, t, action, cardSystem, finalValue);
                    }
                }
                // 自我效果且未被 targets 覆盖的情况（如未指定目标但默认为 Self）
                else if (action.targetType == TargetType.Self || action.targetType == TargetType.None)
                {
                    ApplyAction(source, source, action, cardSystem, finalValue);
                }
                
                // 3. 连击间隔
                if (count > 1 && i < count - 1)
                {
                    yield return new WaitForSeconds(0.25f); 
                }
            }
        }
    }

    // =================================================================
    // 内部计算工具
    // =================================================================

    private int CalculateFinalValue(CharacterBase source, CardAction action)
    {
        int finalValue = action.value;
        if (action.scalesWithStatus && source != null)
        {
            if (action.effectType == EffectType.Attack)
                finalValue += source.GetStatusEffectAmount("Strength");
            else if (action.effectType == EffectType.Block)
                finalValue += source.GetStatusEffectAmount("Dexterity");
        }
        return Mathf.Max(0, finalValue);
    }

    private List<CharacterBase> GetActualTargets(CharacterBase source, CharacterBase selectedTarget, TargetType targetType)
    {
        List<CharacterBase> targets = new List<CharacterBase>();
        CharacterManager manager = CharacterManager.Instance;

        if (manager == null) return targets;

        switch (targetType)
        {
            case TargetType.Self: targets.Add(source); break;
            case TargetType.SelectedEnemy:
            case TargetType.SelectedCharacter: 
                if (selectedTarget != null) targets.Add(selectedTarget); 
                break;
            case TargetType.AllEnemies: targets.AddRange(manager.GetAllEnemies()); break;
            case TargetType.AllAllies: targets.AddRange(manager.GetAllHeroes()); break;
        }
        return targets;
    }

    private void ApplyAction(CharacterBase source, CharacterBase target, CardAction action, CardSystem cardSystem, int finalValue)
    {
        switch (action.effectType)
        {
            case EffectType.Attack:
                if (target != null) target.TakeDamage(finalValue, true);
                break;
            case EffectType.Block:
                // 格挡通常加给使用者自己，除非 targetType 指定了别人
                CharacterBase blockTarget = (action.targetType == TargetType.Self) ? source : target;
                if (blockTarget != null) blockTarget.AddBlock(finalValue);
                break;
            case EffectType.Heal:
                if (target != null) target.Heal(finalValue);
                break;
            case EffectType.ApplyBuff:
            case EffectType.ApplyDebuff:
                if (target != null)
                    target.ApplyStatusEffect(action.statusEffect.ToString(), action.value, action.duration);
                break;
            case EffectType.DrawCard:
                cardSystem.DrawCards(finalValue);
                break;
            case EffectType.Energy:
                cardSystem.GainEnergy(finalValue);
                break;
        }
    }
    // 在 CardData 类中添加此方法以解决 BattleManager 的报错
public void ExecuteEffects(CharacterBase source, CharacterBase target, CardSystem cardSystem)
{
    // 转发调用到协程启动方法
    ExecuteAllActions(source, target, cardSystem);
}

// 确保主执行逻辑名称一致

}