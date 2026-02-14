using UnityEngine;
using System.Collections.Generic;
using CardDataEnums;



/// <summary>
/// 卡牌行动结构体，定义了卡牌的单个效果。
/// 这是一个独立的文件，实现了单一责任原则。
/// 确保这个结构体是可序列化的，以便在 Unity Inspector 中编辑。
/// </summary>
[System.Serializable]
public struct CardAction
{
    [Tooltip("效果类型: 伤害, 格挡, 治疗, 施加增益/减益等.")]
    public EffectType effectType;
   

    [Tooltip("目标类型: 自身, 所有敌人, 选定敌人等.")]
    public TargetType targetType;

    [Tooltip("效果的基础值 (例如: 伤害量, 格挡量, 抽卡数)")]
    public int value;

    [Tooltip("效果重复执行的次数（例如：3点伤害打3次，这里填3）")]
    public int repeatCount; // 新增字段


    // --- Scaling Field (Crucial for Roguelike mechanics) ---
    [Tooltip("If checked, the value scales with the character's status (Strength for Attack, Dexterity for Block).")]
    public bool scalesWithStatus; 

    // --- Fields specific to Buff/Debuff ---
    
    [Tooltip("If the effect is Buff/Debuff, specifies the status type")]
    public StatusEffect statusEffect;

    [Tooltip("Duration of the Buff/Debuff in turns, or stack layers (often imported from Value)")]
    public int duration;
}