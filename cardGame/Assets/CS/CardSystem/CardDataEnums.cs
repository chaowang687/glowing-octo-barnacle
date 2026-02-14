// 注意：这个文件将 CardClass, Rarity, CardType 放入了命名空间 CardDataEnums
namespace CardDataEnums
{
    /// <summary>
    /// 卡牌的职业限制。
    /// </summary>
 

    public enum CardClass

    {

    Any, // 中立卡

    Ironclad, // 战士职业

    Silent, // 刺客职业

    Defect, // 机器人职业

    Watcher // 观者职业

    }
    // --- 卡牌相关枚举 ---
    public enum CardType
    {
        Attack,     
        Skill,      
        Power       
    }

    public enum Rarity
    {
        Common,     
        Uncommon,   
        Rare,       
        Special,    
        Boss        
    }
    
    public enum EffectType
    {
        None,       
        Attack,     
        Block,      
        Heal,       
        DrawCard,   
        Energy,     
        ApplyBuff,  
        StealBack, // 🏴‍☠️ 新增卡牌效果：夺回物资
        ApplyDebuff 
        
    }

    public enum TargetType
    {
        None,               
        Self,               
        SelectedEnemy,      
        SelectedAlly,       
        SelectedCharacter,  
        AllEnemies,         
        AllAllies,          
        AllCharacters       
    }

    // --- 状态效果枚举 ---
    public enum StatusEffect
    {
        None,               
        Weak,               
        Vulnerable,         
        Poison,             
        Strength,           
        Dexterity,          
        Regeneration,       
        Metallicize,        
        Frail               
    }
}