using System;
using UnityEngine;

[Serializable]
public struct CardAction
{
    [Tooltip("效果类型：伤害、格挡、治疗等")]
    public EffectType effectType;

    [Tooltip("目标类型：自身、所有敌人、选中敌人等")]
    public TargetType targetType;

    [Tooltip("效果数值")]
    public int value;

    [Tooltip("如果效果是 Buff/Debuff，这里指定 Buff 名称或类型")]
    public string statusEffectName;

    [Tooltip("Buff/Debuff 持续回合数")]
    public int duration;
}