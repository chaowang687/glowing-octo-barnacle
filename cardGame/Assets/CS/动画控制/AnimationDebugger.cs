using UnityEngine;
using UnityEditor;
using DG.Tweening;
public class AnimationDebugger : MonoBehaviour
{
    [Header("调试目标")]
    public GameObject enemyToDebug;
    
    [Header("手动触发动画")]
    public bool testAttack = false;
    public bool testHit = false;
    public bool testDeath = false;
    
    [Header("动画参数调试")]
    public string triggerToTest = "Attack";
    public bool sendTrigger = false;
    
    void Update()
    {
        if (enemyToDebug == null) return;
        
        if (testAttack)
        {
            testAttack = false;
            TestAttackAnimation();
        }
        
        if (testHit)
        {
            testHit = false;
            TestHitAnimation();
        }
        
        if (testDeath)
        {
            testDeath = false;
            TestDeathAnimation();
        }
        
        if (sendTrigger)
        {
            sendTrigger = false;
            TestCustomTrigger();
        }
    }
    // 修复：添加缺少的TestHitAnimation方法
    private void TestHitAnimation()
    {
        Debug.Log($"=== 测试受伤动画 ===");
        
        Animator animator = enemyToDebug.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Hit");
            Debug.Log($"触发Hit参数");
        }
        else
        {
            Debug.LogWarning("未找到Animator组件");
        }
    }
    
    // 修复：添加缺少的TestDeathAnimation方法
    private void TestDeathAnimation()
    {
        Debug.Log($"=== 测试死亡动画 ===");
        
        Animator animator = enemyToDebug.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Die");
            Debug.Log($"触发Die参数");
        }
        else
        {
            Debug.LogWarning("未找到Animator组件");
        }
    }
    
    private void TestAttackAnimation()
    {
        Debug.Log($"=== 测试攻击动画 ===");
        
        // 尝试所有可能的方法
        Debug.Log("方法1: 使用动画管理器");
        if (EnemyAnimationManager.Instance != null)
        {
            EnemyAnimationManager.Instance.PlayAttackAnimation(enemyToDebug);
        }
        
        Debug.Log("方法2: 直接调用Animator");
        Animator animator = enemyToDebug.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            Debug.Log($"直接触发Attack参数，Animator状态: enabled={animator.enabled}, hasController={animator.runtimeAnimatorController != null}");
        }
        
        Debug.Log("方法3: 检查动画参数");
        if (animator != null)
        {
            Debug.Log("可用参数列表:");
            foreach (var param in animator.parameters)
            {
                Debug.Log($"  - {param.name} (类型: {param.type})");
            }
        }
    }
    
    private void TestCustomTrigger()
    {
        Animator animator = enemyToDebug.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(triggerToTest);
            Debug.Log($"触发自定义参数: {triggerToTest}");
        }
    }
    
    [ContextMenu("检查敌人动画状态")]
    public void CheckEnemyAnimationStatus()
    {
        if (enemyToDebug == null)
        {
            Debug.LogError("请先设置 enemyToDebug");
            return;
        }
        
        Debug.Log($"=== 检查 {enemyToDebug.name} 动画状态 ===");
        
        // 1. 检查Animator组件
        Animator animator = enemyToDebug.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("❌ 没有找到Animator组件");
            return;
        }
        
        Debug.Log($"✅ 找到Animator: {animator.gameObject.name}");
        Debug.Log($"   - 启用状态: {animator.enabled}");
        Debug.Log($"   - 动画控制器: {animator.runtimeAnimatorController?.name ?? "None"}");
        Debug.Log($"   - 是否在播放动画: {animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0}");
        
        // 2. 检查参数
        Debug.Log("📋 Animator参数列表:");
        foreach (var param in animator.parameters)
        {
            Debug.Log($"   - {param.name} (类型: {param.type})");
        }
        
        // 3. 检查当前状态
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"📊 当前动画状态: {stateInfo.fullPathHash}");
        Debug.Log($"   - 是否在过渡: {animator.IsInTransition(0)}");
        
        // 4. 测试简单动画
        Debug.Log("🎬 测试简单缩放动画...");
        enemyToDebug.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.3f)
            .OnComplete(() => Debug.Log("✅ 简单动画测试完成"));
    }
}