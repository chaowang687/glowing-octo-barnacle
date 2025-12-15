/// <summary>
/// 核心战斗枚举，位于全局命名空间。
/// 包含效果类型、目标类型、状态效果和敌人意图。
/// 确保项目中只有这一个文件定义这些枚举，以避免 CS0101 错误。
/// </summary>

// 1. 卡牌/行动的效果类型 (EffectType)
public enum EffectType
{
    None,       // 无效果
    Attack,     // 造成伤害
    Block,      // 获得格挡
    Heal,       // 治疗生命值
    DrawCard,   // 抽卡
    Energy,     // 获得能量
    ApplyBuff,  // 施加增益状态 (如Strength)
    ApplyDebuff // 施加减益状态 (如Weak, Vulnerable)
}

// 2. 目标类型 (TargetType) (谁是卡牌效果的目标)
public enum TargetType
{
    None,              // 无目标 (如抽卡, 加能量)
    Self,              // 施法者自身
    SelectedEnemy,     // 选定的单个敌人
    SelectedAlly,      // 选定的单个盟友
    SelectedCharacter, // 选定的任意角色 (通常用于 UI 交互)
    AllEnemies,        // 所有敌人
    AllAllies,         // 所有盟友
    AllCharacters      // 战场上所有角色
}

// 3. 状态效果 (StatusEffect) (Buff/Debuff)
public enum StatusEffect
{
    None,
    Strength,      // 力量 (增加攻击伤害)
    Dexterity,     // 敏捷 (增加格挡值)
    Weak,          // 虚弱 (造成的伤害减少)
    Vulnerable,    // 易伤 (受到的伤害增加)
    Poison,        // 中毒 (回合结束时掉血)
    Metallicize    // 镀金 (回合结束时获得格挡)
    // 根据您的游戏需要，可在此处添加更多状态
}

// 4. 敌人意图类型 (IntentType) (用于 AI 显示)
public enum IntentType
{
    NONE,           // 未知或无行动
    ATTACK,         // 纯攻击
    BLOCK,          // 纯防御
    BUFF,           // 纯增益
    DEBUFF,         // 纯减益
    ATTACK_DEBUFF,  // 攻击并施加Debuff
    DEFEND_BUFF,    // 防御并施加Buff
    HEAL,           // 治疗自己或盟友
    SPECIAL         // 特殊行动 (通常是独特的或难以归类的)
}

public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum ChestType
    {
        Wooden = 0,
        Iron = 1,
        Gold = 2,
        Magic = 3,
        Boss = 4
    }