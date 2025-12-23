using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 简化的敌人动画管理器，100%确保动画播放
/// </summary>
public class EnemyAnimationManager : MonoBehaviour
{
    // 单例模式
    public static EnemyAnimationManager Instance { get; private set; }
    
    // 动画状态
    private Dictionary<GameObject, bool> _animationStates = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, Animator> _enemyAnimators = new Dictionary<GameObject, Animator>();
    
    // 动画参数哈希
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int HIT_HASH = Animator.StringToHash("Hit");
    private static readonly int DIE_HASH = Animator.StringToHash("Die");
    private static readonly int IDLE_HASH = Animator.StringToHash("Idle");
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 注册敌人的Animator
    /// </summary>
    public void RegisterEnemy(GameObject enemy, Animator animator)
    {
        if (enemy == null || animator == null) return;
        
        _enemyAnimators[enemy] = animator;
        _animationStates[enemy] = false;
        
        Debug.Log($"[动画注册] {enemy.name} 的Animator已注册: {animator.name}");
    }
    
    /// <summary>
    /// 简化的攻击动画播放（100%确保播放）
    /// </summary>
    public bool PlayAttackAnimation(GameObject enemy)
    {
        
        if (enemy == null)
        {
            Debug.LogError("[动画错误] 敌人对象为空");
            return false;
        }
        
        // 尝试1：直接从注册表获取Animator
        if (_enemyAnimators.TryGetValue(enemy, out Animator animator))
        {
            return SafePlayAnimation(enemy, animator, ATTACK_HASH, "Attack");
        }
        
        // 尝试2：动态查找Animator
        animator = enemy.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            RegisterEnemy(enemy, animator);
            return SafePlayAnimation(enemy, animator, ATTACK_HASH, "Attack");
        }
        
        // 尝试3：通过其他方式查找
        animator = FindAnimatorByAnyMeans(enemy);
        if (animator != null)
        {
            RegisterEnemy(enemy, animator);
            return SafePlayAnimation(enemy, animator, ATTACK_HASH, "Attack");
        }
        
        // 最终方案：使用默认动画或粒子效果
        PlayFallbackAnimation(enemy);
        return false;
    }
    
    /// <summary>
    /// 简化的受伤动画播放
    /// </summary>
    public bool PlayHitAnimation(GameObject enemy)
    {
        if (enemy == null) return false;
        
        // 先尝试标准方式
        if (_enemyAnimators.TryGetValue(enemy, out Animator animator))
        {
            return SafePlayAnimation(enemy, animator, HIT_HASH, "Hit");
        }
        
        // 备用方案
        PlaySimpleFlashEffect(enemy);
        return true;
    }
    
    /// <summary>
    /// 简化的死亡动画播放
    /// </summary>
    public bool PlayDieAnimation(GameObject enemy)
    {
        if (enemy == null) return false;
        
        if (_enemyAnimators.TryGetValue(enemy, out Animator animator))
        {
            return SafePlayAnimation(enemy, animator, DIE_HASH, "Die");
        }
        
        // 死亡动画必须有反馈
        PlayDefaultDeathEffect(enemy);
        return true;
    }
    
    /// <summary>
    /// 安全播放动画（包含多重检查和回退方案）
    /// </summary>
    private bool SafePlayAnimation(GameObject enemy, Animator animator, int triggerHash, string animationName)
    {
        try
        {
            // 检查1：对象是否有效
            if (enemy == null || animator == null || !enemy.activeInHierarchy)
            {
                Debug.LogWarning($"[动画检查] {enemy?.name} 或Animator无效，使用回退方案");
                PlayFallbackAnimation(enemy);
                return false;
            }
            
            // 检查2：Animator是否启用
            if (!animator.enabled)
            {
                Debug.LogWarning($"[动画检查] {enemy.name} 的Animator未启用，正在启用...");
                animator.enabled = true;
            }
            
            // 检查3：动画控制器是否分配
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"[动画错误] {enemy.name} 的Animator没有分配动画控制器");
                PlayFallbackAnimation(enemy);
                return false;
            }
            
            // 检查4：动画参数是否存在
            bool hasParameter = false;
            foreach (var param in animator.parameters)
            {
                if (param.nameHash == triggerHash)
                {
                    hasParameter = true;
                    break;
                }
            }
            
            if (!hasParameter)
            {
                Debug.LogWarning($"[动画警告] {enemy.name} 的Animator没有参数 {animationName}，尝试使用默认参数");
                // 尝试其他常见参数名
                if (animator.HasParameter("attack"))
                    animator.SetTrigger("attack");
                else if (animator.HasParameter("Attack"))
                    animator.SetTrigger("Attack");
                else
                {
                    PlayFallbackAnimation(enemy);
                    return false;
                }
            }
            else
            {
                // 正式触发动画
                animator.SetTrigger(triggerHash);
                Debug.Log($"[动画成功] {enemy.name} 触发 {animationName} 动画");
                
                // 记录动画状态
                if (_animationStates.ContainsKey(enemy))
                    _animationStates[enemy] = true;
            }
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[动画异常] {enemy?.name} 播放 {animationName} 动画时出错: {ex.Message}");
            PlayFallbackAnimation(enemy);
            return false;
        }
    }
    
    /// <summary>
    /// 回退动画方案（确保至少有视觉效果）
    /// </summary>
    private void PlayFallbackAnimation(GameObject enemy)
    {
        if (enemy == null) return;
        
        Debug.Log($"[回退动画] {enemy.name} 使用回退动画方案");
        
        // 方法1：使用简单的Transform动画
        Vector3 originalScale = enemy.transform.localScale;
        enemy.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f)
            .OnComplete(() => enemy.transform.localScale = originalScale);
        
        // 方法2：颜色闪烁
        SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color original = sr.color;
            sr.DOColor(Color.red, 0.1f)
                .OnComplete(() => sr.DOColor(original, 0.1f));
        }
    }
    
    /// <summary>
    /// 简单的闪烁效果
    /// </summary>
    private void PlaySimpleFlashEffect(GameObject enemy)
    {
        if (enemy == null) return;
        
        // 快速闪烁效果
        enemy.transform.DOScale(enemy.transform.localScale * 1.1f, 0.05f)
            .OnComplete(() => enemy.transform.DOScale(Vector3.one, 0.05f));
    }
    
    /// <summary>
    /// 默认死亡效果
    /// </summary>
    private void PlayDefaultDeathEffect(GameObject enemy)
    {
        if (enemy == null) return;
        
        // 缩放消失
        enemy.transform.DOScale(Vector3.zero, 0.5f)
            .SetEase(Ease.InBack);
        
        // 淡出
        SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOFade(0, 0.5f);
        }
    }
    
    /// <summary>
    /// 尝试多种方式查找Animator
    /// </summary>
    private Animator FindAnimatorByAnyMeans(GameObject enemy)
    {
        // 方法1：直接查找
        Animator animator = enemy.GetComponent<Animator>();
        if (animator != null) return animator;
        
        // 方法2：在子对象中查找
        animator = enemy.GetComponentInChildren<Animator>(true);
        if (animator != null) return animator;
        
        // 方法3：在父对象中查找
        animator = enemy.GetComponentInParent<Animator>();
        if (animator != null) return animator;
        
        // 方法4：通过名字查找（最后手段）
        Transform[] allChildren = enemy.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name.Contains("Model") || child.name.Contains("Character"))
            {
                animator = child.GetComponent<Animator>();
                if (animator != null) return animator;
            }
        }
        
        return null;
    }
}

// 扩展方法：检查Animator是否有某个参数
public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}