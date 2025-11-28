using UnityEngine;

// 影响效果类型
public enum EffectType
{
    NONE,       // 无效果
    Attack,     // 攻击伤害
    Block,      // 获得格挡
    Heal,       // 治疗
    DrawCard,   // 抽卡
    Energy,     // 获得能量
    ApplyBuff,  // 施加增益
    ApplyDebuff // 施加减益
}

// 目标类型
public enum TargetType
{
    Self,               // 自身
    AllEnemies,         // 所有敌人
    SelectedEnemy,      // 选中的单个敌人
    AllAllies,          // 所有友军
    SelectedAlly,       // 选中的单个友军
    AllCharacters,      // 所有角色（友军+敌人）
    SelectedCharacter   // 选中的单个角色（友军或敌人）
}

// 敌人意图类型
public enum IntentType
{
    NONE,
    ATTACK,
    BLOCK,
    BUFF,
    DEBUFF,
    HEAL,
    SPECIAL
}