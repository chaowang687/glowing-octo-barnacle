using System;
using UnityEngine;


/// <summary>
/// 敌人行动的结构体，用于定义单个回合的动作。
/// </summary>
[Serializable]
public struct EnemyAction
{
    [Tooltip("敌人的意图类型")]
    public IntentType intentType; // 假设 IntentType 已在别处定义
    
    [Tooltip("意图数值（如伤害或格挡值）")]
    public int value;

    [Tooltip("此行动施加的 Buff/Debuff 名称或类型")]
    public string statusEffectName;

    [Tooltip("Buff/Debuff 持续回合数")]
    public int duration;
    
    [Tooltip("策略权重 (用于加权随机策略，即使不用也需保留以兼容 EnemyData)")]
    public float weight; 

    [Tooltip("额外的行动描述或效果（可选）")]
    public string description; 

    // 默认构造函数
    public EnemyAction(
        IntentType type = IntentType.NONE, 
        int val = 0, 
        string status = "", 
        int dur = 0,
        float w = 0f,
        string desc = ""
    )
    {
        intentType = type;
        value = val;
        statusEffectName = status;
        duration = dur;
        weight = w;
        description = desc;
    }
}