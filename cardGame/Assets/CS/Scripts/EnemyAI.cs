using UnityEngine;

// 继承自 CharacterBase，但处理 AI 逻辑
public class EnemyAI : MonoBehaviour
{
    [Header("AI Data")]
    // 解决 BattleManager.cs 报错 CS1061: EnemyAI does not contain a definition for 'enemyData'
    public EnemyData enemyData;
    
    [Header("Current Intent")]
    public IntentType nextIntent;
    public int intentValue;
    
    private CharacterBase self;
    private IEnemyIntentStrategy strategy;

    void Awake()
    {
        self = GetComponent<CharacterBase>();
    }

    void Start()
    {
        if (enemyData != null && enemyData.intentStrategy != null)
        {
            // 尝试将 ScriptableObject 转换为 IEnemyIntentStrategy 接口
            strategy = enemyData.intentStrategy as IEnemyIntentStrategy;
        }

        if (self == null)
        {
            Debug.LogError("EnemyAI must be attached to a CharacterBase.");
        }
    }

    // 解决 BattleManager.cs 报错 CS1501: PerformAction overloads
    public void PerformAction(CharacterBase hero, int currentRound)
    {
        if (self == null || hero == null || nextIntent == IntentType.NONE) return;

        // 在执行行动前，清除敌人的上一回合格挡（如果它没有格挡意图，格挡值应该清零）
        // 这一步通常在回合开始时由 BattleManager 处理，但在敌人行动前清空确保逻辑正确。
        // **注意：我们假定 BattleManager 在回合结束时统一清除所有角色的格挡。
        // self.ClearBlock(); 

        // 根据预先计算的意图执行行动
        switch (nextIntent)
        {
            case IntentType.ATTACK:
                hero.TakeDamage(intentValue);
                Debug.Log($"{self.characterName} attacks {hero.characterName} for {intentValue} damage.");
                break;
            case IntentType.BLOCK:
                // BLOCK 意图会添加格挡，不会清空
                self.AddBlock(intentValue);
                Debug.Log($"{self.characterName} gains {intentValue} block.");
                break;
            // 其他意图如 BUFF, DEBUFF, HEAL 需要更复杂的系统支持
            default:
                Debug.Log($"{self.characterName} performs {nextIntent} action.");
                break;
        }

        // 行动完成后清空意图
        nextIntent = IntentType.NONE; 
        intentValue = 0;
    }

    // 解决 BattleManager.cs 报错 CS1501: CalculateIntent overloads
    public void CalculateIntent(CharacterBase hero, int currentRound)
    {
        if (strategy == null)
        {
            Debug.LogWarning($"{self.characterName} has no valid intent strategy.");
            return;
        }

        EnemyAction nextAction = strategy.GetNextAction(hero, currentRound);
        
        // 更新意图显示数据
        nextIntent = nextAction.intentType; 
        intentValue = nextAction.value;
        
        Debug.Log($"{self.characterName}'s next intent: {nextIntent} with value {intentValue}");
    }
}