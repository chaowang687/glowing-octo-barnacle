using UnityEngine;
using DG.Tweening;
using CardDataEnums; // 假设这是 IntentType 所在的命名空间
using ScavengingGame;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public EnemyDisplay display;
    // ⭐ 确保这行存在且是 public 的 ⭐
    public RoundBasedStrategy roundBasedStrategy;

    [SerializeField] 
    private EnemyDisplay enemyDisplay;
    
    [Header("AI Data")]
    public EnemyData enemyData;

    [Header("Current Intent")]
    public IntentType nextIntent;
    public int intentValue;

    private CharacterBase self;
    private IEnemyIntentStrategy strategy;
    
    // ⭐ 新增：动画控制器引用 ⭐
    private CharacterAnimatorController _animController;

    private void Awake()
    {
        self = GetComponent<CharacterBase>();
        if (self == null)
        {
            Debug.LogError("EnemyAI must be attached to a CharacterBase.");
        }
        
        // 尝试查找 EnemyDisplay
        if (enemyDisplay == null)
        {
            enemyDisplay = GetComponentInChildren<EnemyDisplay>();
        }
        
        // 如果还是为空，尝试从其他组件查找
        if (enemyDisplay == null)
        {
            enemyDisplay = FindFirstObjectByType<EnemyDisplay>();
        }
        
        // ⭐ 新增：查找动画控制器 ⭐
        _animController = GetComponentInChildren<CharacterAnimatorController>();
        if (_animController == null)
        {
            Debug.LogWarning($"EnemyAI: CharacterAnimatorController not found for {self?.characterName}");
        }
    }

    void Start()
    {
        if (self == null)
        {
            Debug.LogError("EnemyAI: CharacterBase component not found.");
            return;
        }
        
        if (enemyDisplay == null)
        {
            Debug.LogWarning($"EnemyAI on {self.characterName} cannot find EnemyDisplay reference. Some visual effects may not work.");
        }
        
        // ⭐ 新增：确保EnemyDisplay有动画控制器引用 ⭐
        if (enemyDisplay != null && _animController != null)
        {
            enemyDisplay.SetAnimatorController(_animController);
        }
    }

    /// <summary>
    /// 设置 EnemyDisplay 引用（修复空引用问题）
    /// </summary>
    public void SetEnemyDisplay(EnemyDisplay display)
    {
        if (display == null)
        {
            Debug.LogWarning($"Attempting to set null EnemyDisplay for {self?.characterName}");
            return;
        }
        
        this.enemyDisplay = display;
        this.display = display; // 也设置公共字段
        Debug.Log($"EnemyDisplay 引用已设置给 {self?.characterName}");
        
        // ⭐ 新增：如果还没有动画控制器，尝试从EnemyDisplay获取 ⭐
        if (_animController == null && display.GetAnimatorController() != null)
        {
            _animController = display.GetAnimatorController();
            Debug.Log($"从EnemyDisplay获取动画控制器: {_animController.gameObject.name}");
        }
    }
    
    /// <summary>
    /// ⭐ 新增：设置动画控制器 ⭐
    /// </summary>
    public void SetAnimatorController(CharacterAnimatorController animController)
    {
        _animController = animController;
        if (_animController != null)
        {
            Debug.Log($"EnemyAI 已设置动画控制器: {_animController.gameObject.name}");
            
            // 如果EnemyDisplay存在，也给它设置
            if (enemyDisplay != null)
            {
                enemyDisplay.SetAnimatorController(_animController);
            }
        }
    }

    public void Initialize(EnemyData enemyData, object intentStrategy)
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemyAI.Initialize: enemyData is null");
            return;
        }
        
        Debug.Log($"Enemy AI for {enemyData.enemyName} initialized with a strategy.");
        this.enemyData = enemyData; 
        
        // 核心修正：直接尝试将传入的 object 转换为接口 IEnemyIntentStrategy
        if (intentStrategy is IEnemyIntentStrategy strategyImpl)
        {
            this.strategy = strategyImpl;
            Debug.Log($"[EnemyAI] Strategy loaded successfully for {enemyData.enemyName}: {strategyImpl.GetType().Name}");
        }
        else
        {
            Debug.LogError($"Enemy {enemyData.enemyName} 的 intentStrategy 无法转换为 IEnemyIntentStrategy。Actual type: {intentStrategy?.GetType().Name ?? "null"}");
            // 创建默认策略
            this.strategy = CreateDefaultStrategy();
        }
        
        // 确保 EnemyDisplay 引用已设置
        if (enemyDisplay == null)
        {
            enemyDisplay = GetComponentInChildren<EnemyDisplay>();
        }
        
        if (enemyDisplay != null && self != null)
        {
            // ⭐ 修改：传递EnemyData ⭐
            enemyDisplay.Initialize(self, enemyData);
            
            // ⭐ 新增：如果动画控制器为空，尝试查找并设置 ⭐
            if (_animController == null)
            {
                _animController = GetComponentInChildren<CharacterAnimatorController>();
                if (_animController != null)
                {
                    enemyDisplay.SetAnimatorController(_animController);
                }
            }
        }
    }
    
    /// <summary>
    /// 创建默认策略（避免空引用）
    /// </summary>
    private IEnemyIntentStrategy CreateDefaultStrategy()
    {
        // 返回一个简单的默认策略
        return new SimpleAttackStrategy();
    }

    /// <summary>
    /// 执行预先计算好的行动，并返回一个 DOTween 序列用于 BattleManager 等待。
    /// ⭐ 修改：通过动画控制器触发动画 ⭐
    /// </summary>
    public Sequence PerformAction(CharacterBase hero, int currentRound)
    {
        if (self == null || hero == null)
        {
            Debug.LogError($"PerformAction: self or hero is null. self={self}, hero={hero}");
            return DOTween.Sequence();
        }
        
        Debug.Log($"LOG FLOW: Enemy {self.characterName} 执行行动，意图: {nextIntent}, 值: {intentValue}");
        
        if (nextIntent == IntentType.NONE) 
        {
            Debug.LogWarning($"{self.characterName} has no intent to perform.");
            return DOTween.Sequence();
        }

        Sequence actionSequence = DOTween.Sequence();
        
        // ⭐ 修正：在序列开始时立即记录执行的意图 ⭐
        actionSequence.AppendCallback(() => {
            Debug.Log($"LOG EXECUTION: Enemy {self.characterName} 执行行动，意图: {nextIntent}, 值: {intentValue}");
        });
        
        // 根据预先计算的意图执行行动
        switch (nextIntent)
        {
            case IntentType.Loot:
                actionSequence.Append(self.transform.DOMove(hero.transform.position, 0.4f));
                actionSequence.AppendCallback(() => {
                    if (InventoryManager.Instance != null)
                    {
                        string stolenName = InventoryManager.Instance.TakeRandomItem();
                        Debug.Log($"[AI] 尝试抢夺，拿到ID: {stolenName}");
                        if (!string.IsNullOrEmpty(stolenName)) 
                        {
                            if (display != null) 
                            {
                                // 传给 display 播放动画
                                display.PlayLootAnimation(stolenName); 
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("InventoryManager.Instance is null, cannot loot.");
                    }
                });
                break;

            case IntentType.Escape:
                actionSequence.Append(self.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)); // 缩小消失
                actionSequence.AppendCallback(() => {
                    if (BattleManager.Instance != null)
                    {
                        BattleManager.Instance.HandleDyingCharacterCleanup(self);
                    }
                    Destroy(gameObject);
                });
                break;
                
            case IntentType.ATTACK:
                // ⭐ 核心修正：通过动画控制器触发攻击动画 ⭐
                actionSequence.AppendCallback(() =>
                {
                     Debug.Log($"[ANIMATION DEBUG] {self.characterName} 尝试触发攻击动画");
                    
                    // 方法1：通过动画控制器
                    if (_animController != null)
                    {
                        Debug.Log($"[ANIMATION DEBUG] 通过 CharacterAnimatorController 触发攻击动画");
                        _animController.TriggerAttackAnimation();
                    }
                    // 方法2：通过EnemyDisplay的动画控制器（备用）
                    else if (display != null && display.GetAnimatorController() != null)
                    {
                        Debug.Log($"[ANIMATION DEBUG] 通过 EnemyDisplay 的动画控制器触发攻击动画");
                        display.GetAnimatorController().TriggerAttackAnimation();
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot trigger attack animation: AnimatorController is null for {self.characterName}");
                    }
                });
                
                // 攻击动画通常需要时间，所以我们添加一个短暂的延时来模拟动画播放时间
                actionSequence.AppendInterval(0.5f); // 假设攻击动画持续 0.5 秒
                
                // 攻击：执行 TakeDamage 并等待其序列完成
                if (hero != null)
                {
                    Sequence damageSequence = hero.TakeDamage(intentValue, isAttack: true);
                    actionSequence.Append(damageSequence);
                }
                
                // 打印日志作为序列回调的一部分
                actionSequence.AppendCallback(() => 
                {
                    Debug.Log($"{self.characterName} attacks {hero?.characterName} for {intentValue} damage.");
                });
                break;

            case IntentType.BLOCK:
                // ⭐ 核心修正：调用新的 AddBlock 方法，并设置持续时间 ⭐
                int blockDuration = 2; // 默认格挡持续 1 回合 (下一玩家回合开始时清除)
                
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
            else if (enemyDisplay != null)
            {
                enemyDisplay.RefreshIntent(IntentType.NONE, 0);
            }
        });
        
        return actionSequence;
    }

    // 解决 BattleManager.cs 报错 CS1501: CalculateIntent overloads
    public void CalculateIntent(CharacterBase hero, int currentRound)
    {
        if (strategy == null)
        {
            Debug.LogError($"{self?.characterName} has no valid intent strategy (strategy is null).");
            return;
        }

        if (hero == null)
        {
            Debug.LogError("CalculateIntent: hero is null");
            return;
        }

        EnemyAction nextAction = strategy.GetNextAction(hero, currentRound);
        
        if (nextAction.intentType == IntentType.NONE && nextAction.value == 0)
        {
             Debug.LogWarning($"[EnemyAI] Strategy returned NONE intent for round {currentRound}. This might be unintended.");
        }

        // 更新意图显示数据
        nextIntent = nextAction.intentType;
        intentValue = nextAction.value;

        Debug.Log($"{self?.characterName}'s next intent: {nextIntent} with value {intentValue}");
    }

    /// <summary>
    /// ⭐ 新增：触发受伤动画 ⭐
    /// </summary>
    public void TriggerHitAnimation()
    {
        if (_animController != null)
        {
            _animController.TriggerHitAnimation();
            Debug.Log($"{self?.characterName} 触发受伤动画");
        }
        else
        {
            Debug.LogWarning($"无法触发受伤动画: 动画控制器为空 ({self?.characterName})");
        }
    }
    
    /// <summary>
    /// ⭐ 新增：触发死亡动画 ⭐
    /// </summary>
    public void TriggerDieAnimation()
    {
        if (_animController != null)
        {
            _animController.TriggerDieAnimation();
            Debug.Log($"{self?.characterName} 触发死亡动画");
        }
        else
        {
            Debug.LogWarning($"无法触发死亡动画: 动画控制器为空 ({self?.characterName})");
        }
    }
    
    /// <summary>
    /// ⭐ 新增：设置意图姿态可见性 ⭐
    /// </summary>
    public void SetIntentVisibility(bool isVisible)
    {
        if (_animController != null)
        {
            _animController.SetIntentVisibility(isVisible);
            Debug.Log($"{self?.characterName} 设置意图姿态可见性: {isVisible}");
        }
    }

    /// <summary>
    /// 执行当前预定的意图动作。
    /// 由 BattleManager 在敌人回合轮流调用。
    /// </summary>
    public IEnumerator ExecuteIntent()
    {
        if (self == null)
        {
            Debug.LogError("ExecuteIntent: CharacterBase is null");
            yield break;
        }
        
        // 1. 逻辑触发：根据当前的 IntentType 执行实际效果（扣血、加盾、偷东西）
        Debug.Log($"{self.characterName} 正在执行意图...");

        // 2. 动画触发：通过动画控制器播放对应的动画
        if (_animController != null && nextIntent == IntentType.ATTACK)
        {
            _animController.TriggerAttackAnimation();
        }

        // 3. 关键：等待动画或逻辑完成
        // yield return 的时间应该稍微长于动画时间，确保玩家能看清
        yield return new WaitForSeconds(1.0f); 

        // 4. 行动结束，可以在这里重置意图或进行清理
        Debug.Log($"{self.characterName} 行动结束");
    }
}

// 简单的默认策略实现
public class SimpleAttackStrategy : IEnemyIntentStrategy
{
    public EnemyAction GetNextAction(CharacterBase hero, int currentRound)
    {
        // 默认总是攻击，造成5点伤害
        return new EnemyAction
        {
            intentType = IntentType.ATTACK,
            value = 5
        };
    }
}