using UnityEngine;
using CardDataEnums; // 确保导入命名空间

/// <summary>
/// 敌人的核心脚本，继承自 CharacterBase。
/// 职责：执行 AI 决策并管理生命状态、目标。
/// 它将所有的决策和行动逻辑委托给 EnemyAI 组件。
/// </summary>
[RequireComponent(typeof(EnemyAI))] // 确保场景中挂载了 EnemyAI 脚本
public class Enemy : CharacterBase
{
    // 引用 AI 决策组件
    private EnemyAI enemyAI; 
    private CharacterBase targetHero; 
    
    // 跟踪当前战斗回合数 (通常由 BattleManager 提供)
    // 这里我们只是模拟一个回合计数器
    private int currentRound = 1; 

    protected override void Awake() 
    {
        // 1. 调用基类的初始化
        base.Awake();
        
        // 2. 获取 AI 组件
        enemyAI = GetComponent<EnemyAI>();
        if (enemyAI == null)
        {
            Debug.LogError("Enemy 脚本需要 EnemyAI 组件，但未找到!");
        }
        
        // 3. 设置初始属性 (可从 EnemyData 中加载)
        maxHp = 40; 
        currentHp = maxHp;
    }

    /// <summary>
    /// 在战斗开始时或回合开始前调用，用于设置目标。
    /// </summary>
    public void SetTarget(CharacterBase hero)
    {
        targetHero = hero;
    }

    // -------------------------------------------------------------------------
    // 外部调用方法 (通常由 BattleManager 调用)
    // -------------------------------------------------------------------------

    /// <summary>
    /// 告知 AI 确定下一回合的行动意图。
    /// 将决策工作委托给 EnemyAI。
    /// </summary>
    public void DetermineIntent()
    {
        if (isDead) return;
        
        // 委托给 EnemyAI 组件，让它根据策略计算意图
        enemyAI.CalculateIntent(targetHero, currentRound);
        
        // 注意：EnemyAI 内部已设置 nextIntent 和 intentValue
    }

    /// <summary>
    /// 执行敌人当前确定的意图。
    /// 将执行工作委托给 EnemyAI。
    /// </summary>
    public void ExecuteTurn()
    {
        if (isDead)
        {
            Debug.Log($"{characterName} 已死亡，跳过回合。");
            return;
        }

        if (targetHero == null)
        {
            Debug.LogError("敌人没有目标 (Hero)，无法执行回合。");
            return;
        }
        
        // 委托给 EnemyAI 组件执行行动
        enemyAI.PerformAction(targetHero, currentRound);
        
        // 调用基类的回合结束逻辑 (清除格挡、减少状态持续时间)
        base.AtEndOfTurn();
        
        // 模拟回合计数器递增
        currentRound++;
    }
    
    // -------------------------------------------------------------------------
    // 重写基类方法 (可选，仅用于添加自定义逻辑)
    // -------------------------------------------------------------------------

    protected override void Die()
    {
        base.Die(); 
        Debug.Log($"{characterName} 敌人被击败！");
    }
}