using UnityEngine;

// 这是专门负责接收指令并触发角色 Animator 动画的脚本
public class CharacterAnimatorController : MonoBehaviour
{
    // === 外部设置：请将正确的 Animator 组件拖拽到此字段。如果 Animator 是动态生成的子对象，请保持此字段为空，脚本将自动查找。 ===
    [Tooltip("请将控制角色模型的 Animator 组件拖拽到此字段。如果 Animator 是动态生成的，请留空。")]
    public Animator characterAnimator; 
    
    // Animator Hashes (统一管理，提高性能)
    private readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private readonly int HitTriggerHash = Animator.StringToHash("Hit");
    private readonly int DieTriggerHash = Animator.StringToHash("Die");
    
    // 意图姿态 Bool Hash (用于控制 Idle <-> Intent Stance 的切换)
    private readonly int IsVisibleBoolHash = Animator.StringToHash("IsVisible"); 
    
    // Awake在所有脚本的Start之前调用，是初始化引用的最佳时机
    private void Awake()
    {
        characterAnimator = GetComponentInChildren<Animator>(true);
        Debug.Log("CAC Awake: Starting initialization sequence.");
        
        // 1. 优先检查 Inspector 中是否已手动设置引用
        if (characterAnimator == null)
        {
            // 2. 如果未设置 (针对动态生成的子物体)，则进行动态查找
            //    由于 CharacterAnimatorController 挂载在父对象，它会向下在子物体中查找 Animator 组件。
            //    注意：如果模型是动态加载的，此查找可能会失败！
            characterAnimator = GetComponentInChildren<Animator>(true);
            
            if (characterAnimator == null)
            {
                // 最终未找到：这通常意味着模型尚未加载。
                Debug.LogWarning("CAC Awake: Dynamic Animator lookup failed. Model/Animator might be loaded later. Relying on external script to call SetAnimator().", this);
            }
            else
            {
                 // 动态查找成功
                Debug.Log($"CAC Awake: Dynamic Animator lookup successful. Component found on object: {characterAnimator.gameObject.name}", this);
                InitializeAnimatorParameters();
            }
        }
        else
        {
            // Inspector 引用成功
            Debug.Log($"CAC Awake: Animator reference successfully initialized (Inspector set). Component on object: {characterAnimator.gameObject.name}", this);
            InitializeAnimatorParameters();
        }
    }

    /// <summary>
    /// 私有方法：初始化所有 Animator 参数的默认值。
    /// 确保角色默认处于 Idle 姿态，避免状态机进入未知状态。
    /// </summary>
    private void InitializeAnimatorParameters()
    {
        if (characterAnimator != null)
        {
            // 明确将 IsVisible 设置为 false，确保从 Idle 状态开始
            characterAnimator.SetBool(IsVisibleBoolHash, false);
            Debug.Log($"[ANIMATION INIT] Animator parameter IsVisible initialized to false.");
        }
    }

    /// <summary>
    /// 【重要】供外部脚本调用：在模型加载完成后，必须使用此方法将 Animator 引用传递给控制器。
    /// (例如：由负责加载模型的脚本调用)
    /// </summary>
    public void SetAnimator(Animator animator)
    {
        if (animator != null)
        {
            this.characterAnimator = animator;
            Debug.Log($"[ANIMATION INIT] External script successfully set the Animator reference to: {animator.gameObject.name}");
            
            // 链接成功后立即初始化参数
            InitializeAnimatorParameters();
        }
    }

    /// <summary>
    /// 触发 Hit (受伤) 动画。由外部脚本（如 EnemyDisplay）调用。
    /// </summary>
    public void TriggerHitAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(HitTriggerHash);
            Debug.Log($"[ANIMATION SEND] Successfully sent Trigger: Hit");
        }
        else
        {
            Debug.LogError("Cannot trigger Hit animation: Animator reference is null. (Load/Link Issue)", this);
        }
    }

    /// <summary>
    /// 触发 Attack (攻击) 动画。由外部脚本（如 EnemyDisplay）调用。
    /// </summary>
    public void TriggerAttackAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(AttackTriggerHash);
            Debug.Log($"[ANIMATION SEND] Successfully sent Trigger: Attack");
        }
        else
        {
            Debug.LogError("Cannot trigger Attack animation: Animator reference is null. (Load/Link Issue)", this);
        }
    }

    /// <summary>
    /// 触发 Die (死亡) 动画。由外部脚本（如 EnemyDisplay）调用。
    /// </summary>
    public void TriggerDieAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(DieTriggerHash);
            Debug.Log($"[ANIMATION SEND] Successfully sent Trigger: Die");
        }
        else
        {
            Debug.LogError("Cannot trigger Die animation: Animator reference is null. (Load/Link Issue)", this);
        }
    }
    
    /// <summary>
    /// 设置意图姿态的可见性布尔值。由 EnemyDisplay 调用。
    /// This method uses SetBool to drive smooth transitions between Idle and Intent Stance.
    /// </summary>
    /// <param name="isVisible">意图姿态是否可见/激活 (true = Intent Stance, false = Idle Stance)</param>
    public void SetIntentVisibility(bool isVisible)
    {
        if (characterAnimator != null)
        {
            // 使用布尔值控制 Idle <-> Intent Stance 之间的平滑过渡
            characterAnimator.SetBool(IsVisibleBoolHash, isVisible);
            Debug.Log($"[ANIMATION SEND] Successfully sent Bool: IsVisible = {isVisible}");
        }
        else
        {
            Debug.LogError("Cannot set Intent Visibility: Animator reference is null. (Load/Link Issue)", this);
        }
    }
}