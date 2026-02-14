using UnityEngine;
using System;

/// <summary>
/// 死亡动画中继器。附加在带有 Animator 的父对象上。
/// 接收 Animator 事件，并将调用转发给子对象上的 EnemyDisplay 脚本。
/// </summary>
public class DeathRelay : MonoBehaviour
{
    // ⭐ 必须在 Inspector 中手动拖入子对象上的 EnemyDisplay 实例 ⭐
    public EnemyDisplay enemyDisplay;

    void Start()
    {
        // 查找子对象上的 EnemyDisplay 脚本（以防 Inspector 未设置）
        if (enemyDisplay == null)
        {
            // 假设 EnemyDisplay 在子对象上
            enemyDisplay = GetComponentInChildren<EnemyDisplay>();
            if (enemyDisplay == null)
            {
                Debug.LogError("DeathRelay 无法找到 EnemyDisplay 脚本。动画完成后的清理将失败！");
            }
        }
    }
 
    /// <summary>
    /// 这是 Animator Event 调用的公开方法。
    /// </summary>
    public void TriggerDeathComplete()
    {
        if (enemyDisplay != null)
        {
            // 修正：调用 EnemyDisplay 中用于触发事件的公共方法，
            // 而不是直接调用事件本身 (OnDeathAnimationComplete)。
            enemyDisplay.NotifyDeathAnimationCompleted();
        }
    }
}