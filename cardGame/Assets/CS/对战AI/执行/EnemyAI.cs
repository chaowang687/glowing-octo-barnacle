using UnityEngine;
using DG.Tweening; // ⭐ 引入 DOTween 命名空间 ⭐
using CardDataEnums; // 假设这是 IntentType 所在的命名空间

// 继承自 CharacterBase，但处理 AI 逻辑
public class EnemyAI : MonoBehaviour
{
    public EnemyDisplay display;
    // ⭐ 确保这行存在且是 public 的 ⭐
    public RoundBasedStrategy roundBasedStrategy;

    [Header("AI Data")]
    // 解决 BattleManager.cs 报错 CS1061: EnemyAI does not contain a definition for 'enemyData'
    public EnemyData enemyData; // 假设 EnemyData 是一个 ScriptableObject 或类

    [Header("Current Intent")]
    public IntentType nextIntent;
    public int intentValue;

    private CharacterBase self;
    private IEnemyIntentStrategy strategy; // 假设 IEnemyIntentStrategy 接口存在

    public void Initialize(EnemyData enemyData, object intentStrategy)
    {
        Debug.Log($"Enemy AI for {enemyData.enemyName} initialized with a strategy.");
        this.enemyData = enemyData; 
        
        // 核心修正：直接尝试将传入的 object 转换为接口 IEnemyIntentStrategy
        if (intentStrategy is IEnemyIntentStrategy strategyImpl)
        {
            this.strategy = strategyImpl;
        }
        else
        {
            Debug.LogError($"Enemy {enemyData.enemyName} 的 intentStrategy 无法转换为 IEnemyIntentStrategy。");
            this.strategy = null; 
        }
    }

    void Awake()
    {
        self = GetComponent<CharacterBase>();
    }

    void Start()
    {
        if (self == null)
        {
            Debug.LogError("EnemyAI must be attached to a CharacterBase.");
        }
    }

    /// <summary>
    /// 执行预先计算好的行动，并返回一个 DOTween 序列用于 BattleManager 等待。
    /// </summary>
    public Sequence PerformAction(CharacterBase hero, int currentRound)
    {
        Debug.Log($"LOG FLOW: Enemy {self.characterName} 执行行动，意图: {nextIntent}, 值: {intentValue}");
        // 如果无法执行，返回一个空的 Sequence 而不是直接返回
        if (self == null || hero == null || nextIntent == IntentType.NONE) return DOTween.Sequence();

        Sequence actionSequence = DOTween.Sequence();
        // ⭐ 修正：在序列开始时立即记录执行的意图 ⭐
            actionSequence.AppendCallback(() => {
                Debug.Log($"LOG EXECUTION: Enemy {self.characterName} 执行行动，意图: {nextIntent}, 值: {intentValue}");
            });
        // 根据预先计算的意图执行行动
        switch (nextIntent)
        {
            case IntentType.ATTACK:
                // 攻击：执行 TakeDamage 并等待其序列完成
                Sequence damageSequence = hero.TakeDamage(intentValue, isAttack: true);
                actionSequence.Append(damageSequence);
                
                // 打印日志作为序列回调的一部分
                actionSequence.AppendCallback(() => 
                {
                    Debug.Log($"{self.characterName} attacks {hero.characterName} for {intentValue} damage.");
                });
                break;

            case IntentType.BLOCK:
                // ⭐ 核心修正：调用新的 AddBlock 方法，并设置持续时间 ⭐
                int blockDuration = 1; // 默认格挡持续 1 回合 (下一玩家回合开始时清除)
                
                // 瞬时设置格挡值，UI 此时应该刷新
                self.AddBlock(intentValue, blockDuration);

                // 添加一个视觉动画，但不再依赖它的时间来控制回合流程
                actionSequence.Append(self.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1));
                
                // 打印日志作为序列回调的一部分
                actionSequence.AppendCallback(() =>
                {
                    Debug.Log($"{self.characterName} gains {intentValue} block. Duration: {blockDuration}");
                });
                break;

            default:
                // 其他意图，添加一个短暂的动画或延时
                actionSequence.AppendInterval(0.2f);
                actionSequence.AppendCallback(() => 
                {
                    Debug.Log($"{self.characterName} performs {nextIntent} action.");
                });
                break;
        }

        // 行动完成后清空意图 (确保在动画结束后执行)
        actionSequence.AppendCallback(() => {
            nextIntent = IntentType.NONE;
            intentValue = 0;
            // 刷新意图显示，假设 display.RefreshIntent 存在且可以接受 (NONE, 0)
            if (display != null)
            {
                display.RefreshIntent(IntentType.NONE, 0); 
            }
        });
        
        return actionSequence; // 返回序列
    }

    // 解决 BattleManager.cs 报错 CS1501: CalculateIntent overloads
    public void CalculateIntent(CharacterBase hero, int currentRound)
    {
        if (strategy == null)
        {
            Debug.LogWarning($"{self.characterName} has no valid intent strategy.");
            return;
        }

        EnemyAction nextAction = strategy.GetNextAction(hero, currentRound); // 假设 EnemyAction 结构存在

        // 更新意图显示数据
        nextIntent = nextAction.intentType;
        intentValue = nextAction.value;

        Debug.Log($"{self.characterName}'s next intent: {nextIntent} with value {intentValue}");
    }
}