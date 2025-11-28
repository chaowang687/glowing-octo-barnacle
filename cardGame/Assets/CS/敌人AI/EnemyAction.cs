using System;
using UnityEngine;

[Serializable]
public struct EnemyAction
{
    [Tooltip("敌人的意图类型")]
    // 解决了 CS05017: IntentType does not contain a definition for 'NONE' 等错误，改为 NONE
    public IntentType intentType; 

    [Tooltip("意图数值（如伤害或格挡值）")]
    public int value;

    [Tooltip("此行动施加的 Buff/Debuff 名称或类型")]
    public string statusEffectName;

    [Tooltip("Buff/Debuff 持续回合数")]
    public int duration;
}