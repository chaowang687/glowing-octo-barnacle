using UnityEngine;

/// <summary>
/// 核心游戏枚举定义，涵盖卡牌、效果、目标、职业、稀有度、状态和敌人意图。
/// 所有的枚举都被定义为公共静态类 CardEnums 的嵌套枚举，以确保命名空间的清晰和避免冲突。
/// </summary>
public static class CardEnums 
{
    // --- 卡牌核心属性 ---

    /// <summary>
    /// 卡牌所需职业类型 (匹配表格中的 RequiredClass)
    /// </summary>
    public enum CardClass
    {
        Any,        // 任意职业 (通用牌)
        Warrior,    // 战士
        Mage,       // 法师
        Rogue       // 游荡者
    }

    /// <summary>
    /// 卡牌的主要类型：攻击牌、技能牌、能力牌
    /// </summary>
    public enum CardType
    {
        Attack,     // 攻击牌
        Skill,      // 技能牌
        Power       // 能力牌 (通常永久生效)
    }

    /// <summary>
    /// 卡牌稀有度 (用于控制获取概率)
    /// </summary>
    public enum Rarity
    {
        Basic,      // 基础卡（初始牌组）
        Common,     // 常见
        Uncommon,   // 不常见
        Rare,       // 稀有
        Special     // 特殊（Boss或事件奖励）
    }

    // --- 效果与目标 ---

    /// <summary>
    /// 影响效果类型：攻击伤害、格挡、治疗、施加Buff/Debuff等
    /// </summary>
    public enum EffectType
    {
        None,       // 无效果
        Attack,     // 攻击伤害
        Block,      // 获得格挡
        Heal,       // 治疗
        DrawCard,   // 抽卡
        Energy,     // 获得能量
        ApplyBuff,  // 施加增益状态
        ApplyDebuff // 施加减益状态
    }

    /// <summary>
    /// 卡牌效果的目标
    /// </summary>
    public enum TargetType
    {
        None,               // 效果无目标 (如获得能量，抽卡)
        Self,               // 自身
        AllEnemies,         // 所有敌人
        SelectedEnemy,      // 选中的单个敌人
        AllAllies,          // 所有友军
        SelectedAlly,       // 选中的单个友军
        AllCharacters,      // 所有角色（友军+敌人）
        SelectedCharacter   // 选中的单个角色（友军或敌人）
    }

    /// <summary>
    /// 状态效果的类型 (Buff/Debuff 的具体名称)
    /// </summary>
    public enum StatusEffect
    {
        None,
        Strength,       // 力量（攻击伤害提升）
        Dexterity,      // 敏捷（格挡值提升）
        Vulnerable,     // 易伤（受到的伤害增加）
        Weak,           // 虚弱（造成的伤害降低）
        Poison,         // 中毒（持续伤害）
        Frail,          // 脆弱（格挡值降低）
        Metallicize     // 金属化（回合结束获得格挡）
    }

    // --- 敌人AI/意图 ---

    /// <summary>
    /// 敌人意图类型 (用于敌人的回合预告图标)
    /// </summary>
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
}