using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Linq;

// 假设 IntentType, IntentIconConfig, CharacterAnimatorController, CharacterBase, BattleManager 在其他地方定义

/// <summary>
/// 负责角色的视觉表现、动画触发、UI 意图和血条的显示。
/// </summary>
public class EnemyDisplay : MonoBehaviour
{
    // 在 EnemyDisplay.cs 中
    public Animator lootAnimator; // 拖入刚才那个节点的 Animator
    public TextMeshProUGUI lootText; // 拖入那个节点下的文本
    [Header("Loot Feedback")]
    public GameObject lootSuccessEffect; // 比如一个写着“抢到了！”的浮动图标预制体
    public AudioClip lootLaughSound;     // 海盗大笑的音效
    private AudioSource audioSource;
    // Public event for external scripts (如 DeathRelay.cs) 通知动画完成。
    public event Action OnDeathAnimationComplete;

    // 动画触发器的 Hash 值，用于性能优化
    private static readonly int UI_FLASH_HASH = Animator.StringToHash("Flash");
    private static readonly int UI_HIDE_HASH = Animator.StringToHash("HideUI");

    // 核心数据引用
    private CharacterBase character; 

    
    // ⭐ 关键修正 1：缓存角色名称，用于在 character 引用被销毁后仍能安全追踪和记录日志。 ⭐
    private string _dyingCharacterName = "Unknown Character"; 

    /// <summary>
    /// 公共属性，允许外部（如 BattleManager）安全读取该角色数据。
    /// </summary>
    public CharacterBase CharacterData => character; 

    // 角色模型动画控制器引用
    [Header("角色动画控制器引用")]
    [Tooltip("请将 CharacterAnimatorController 脚本拖拽到此处。")]
    public CharacterAnimatorController characterAnimController; 

    // UI 动画控制器引用 (用于血条闪烁、意图淡入淡出等)
    [Header("UI 动画控制器引用")]
    [Tooltip("请将控制意图/血条 UI 的 Animator 组件拖拽到此处。")]
    public Animator uiAnimator; 
    
    [Header("意图 UI 引用")]
    public GameObject intentUIRoot;
    public Image intentIcon;
    public TextMeshProUGUI intentValueText;
    public IntentIconConfig intentConfig; // 意图图标配置 ScriptableObject

    [Header("意图 UI 动画参数")]
    public float fadeDuration = 0.2f;

    private Sequence intentFadeSequence;
    // EnemyDisplay.cs
public void PlayLootAnimation(string itemName)
{
    // BroadcastMessage 会自动匹配参数名
    if (lootAnimator != null)
    {
        if (lootText != null) lootText.text = $"Pirates seized the {itemName}!";
        
        // 直接触发，因为物体一直是 Active 的，Animator 绝对能收到
        lootAnimator.SetTrigger("ShowLoot");
        Debug.Log($"[动画激活] 成功触发 ShowLoot，物品：{itemName}");
    }
}

    // === Initialization and Reference Acquisition (已简化) ===
    public void ShowLootFeedback(string itemName)
    {
        // 弹出特效
        if (lootSuccessEffect != null)
        {
            GameObject effect = Instantiate(lootSuccessEffect, transform.position + Vector3.up * 2f, Quaternion.identity);
            effect.transform.DOMoveY(effect.transform.position.y + 1f, 1f);
            Destroy(effect, 1.2f);
        }

        // 播放音效
        if (audioSource != null && lootLaughSound != null)
        {
            audioSource.PlayOneShot(lootLaughSound);
        }
        
        Debug.Log($"海盗：哈哈哈，你的 {itemName} 归我了！");
    }
    void Awake()
    {
        Debug.Log($"[DEBUG TRACE: AWAKE] {gameObject.name} EnemyDisplay 开始执行 Awake。");

        // 检查角色模型动画控制器引用
        if (characterAnimController == null)
        {
            // 尝试从父对象查找
            characterAnimController = GetComponentInParent<CharacterAnimatorController>();

            if (characterAnimController == null)
            {
                Debug.LogError($"[DEBUG ERROR: INIT] {gameObject.name} EnemyDisplay 致命错误：未设置 CharacterAnimatorController 引用，且在父对象中也找不到！请检查 Inspector 或脚本挂载位置。", this);
            }
            else
            {
                Debug.Log($"[DEBUG SUCCESS: ANIMATOR INIT] CharacterAnimatorController 引用成功 (通过父对象动态查找)。");
            }
        }
        else
        {
            Debug.Log($"[DEBUG SUCCESS: ANIMATOR INIT] CharacterAnimatorController 引用成功 (Inspector 设置)。");
        }
        
        // 检查 UI 动画控制器引用 (UI Animator 通常是可选的，但建议检查)
        if (uiAnimator == null)
        {
            Debug.LogWarning($"[DEBUG WARNING: INIT] {gameObject.name} EnemyDisplay：UI Animator 引用为空，UI 动画可能无法触发。");
        }
        
        // 确保意图UI初始隐藏
        if (intentUIRoot != null)
        {
            intentUIRoot.SetActive(false);
        }
    }
    
    /// <summary>
    /// Initializes the Display and subscribes to CharacterBase events.
    /// </summary>
    public void Initialize(CharacterBase characterData)
    {
        Debug.Log($"[DEBUG TRACE: INIT] 尝试为 {characterData.characterName} 初始化 Display 并订阅事件...");
        
        // 确保取消订阅旧的角色事件，防止内存泄漏和重复触发
        if (this.character != null)
        {
            this.character.OnHpChanged -= HandleHit; 
            this.character.OnCharacterDied -= HandleDeath; 
        }

        // 移除旧的死亡动画完成订阅，防止多次触发销毁（确保只订阅一次）
        OnDeathAnimationComplete -= OnActualDeathAnimationComplete;
        OnDeathAnimationComplete += OnActualDeathAnimationComplete;

        this.character = characterData;
        
        if (this.character != null)
        {
            this.character.OnHpChanged += HandleHit; 
            this.character.OnCharacterDied += HandleDeath; 
            
            Debug.Log($"[DEBUG SUCCESS: INIT] {character.characterName} 已成功订阅 HP 变化、死亡事件和动画完成事件。");
        }
        else
        {
            Debug.LogError("[DEBUG ERROR: INIT] EnemyDisplay failed to subscribe: CharacterBase object is null.");
        }
    }

    /// <summary>
    /// 当死亡动画播放完毕后，由 NotifyDeathAnimationCompleted() 调用的最终处理方法。
    /// 此方法通知 BattleManager 执行销毁操作，并销毁自身 GameObject。
    /// </summary>
    private void OnActualDeathAnimationComplete()
    {
        Debug.Log($"[DEBUG FINAL CLEANUP] {_dyingCharacterName} 死亡动画完成，通知 BattleManager 销毁。");
        
        // 1. 通知 BattleManager 死亡流程结束 (BattleManager 会移除等待列表中的角色，并调用 CheckBattleEnd)
        // 关键：即使 this.character 引用已经被外部脚本破坏，我们仍然发送通知，
        // BattleManager 应该处理这个 null/destroyed 的引用，并解除等待状态。
        if (BattleManager.Instance != null)
        {
            // 传递 this.character，即使它可能已经被 Unity 标记为 destroyed/null
            BattleManager.Instance.HandleDeathAnimationComplete(this.character.gameObject);
            Debug.Log($"[DEBUG SEND: FINAL CLEANUP] 已强制尝试通知 BattleManager 死亡动画流程结束。");
        }
        
        // 2. 避免在销毁过程中仍触发事件
        OnDeathAnimationComplete -= OnActualDeathAnimationComplete;
        
        // 3. 销毁包含此显示组件的 GameObject 自身
        Destroy(gameObject); 
        Debug.Log($"[DEBUG FINAL CLEANUP] {_dyingCharacterName} 的 GameObject 已被销毁。");
    }
    
    // === External Active Call (Called by EnemyAI in PerformAction) ===

    /// <summary>
    /// 由 EnemyAI 在执行攻击意图时调用。
    /// </summary>
    public void TriggerAttackAnimation()
    {
        if (character == null) return;
        Debug.Log($"[DEBUG RECEIVE: ATTACK] {character.characterName} 收到攻击指令。");
        
        // 委托给角色模型控制器
        if (characterAnimController != null && !character.IsDead)
        {
            characterAnimController.TriggerAttackAnimation();
            Debug.Log($"[DEBUG SEND: ATTACK] Animator Trigger: Attack 已发送。");
        }
        // 处理错误（保持不变）
        else if (characterAnimController == null)
        {
            Debug.LogWarning($"[DEBUG WARNING: ATTACK] 无法触发攻击动画，控制器为空！");
        }
        else if (character.IsDead)
        {
             Debug.LogWarning($"[DEBUG WARNING: ATTACK] 无法触发攻击动画，角色已死亡。");
        }
    }

    // === Internal Event Response (Subscribed to CharacterBase Events) ===

    /// <summary>
    /// Responds to CharacterBase.OnHpChanged event (damage or healing).
    /// damageTaken > 0: damage; damageTaken < 0: heal.
    /// </summary>
    private void HandleHit(int currentHp, int maxHp, int damageTaken)
    {
        Debug.Log($"[DEBUG RECEIVE: HIT] HandleHit 方法被调用！实际伤害值: {damageTaken}。");
        
        if (character == null)
        {
            Debug.Log($"[DEBUG GUARD: HIT] 角色为空，退出 HandleHit。");
            return;
        }
        
        if (damageTaken > 0)
        {
            // 收到伤害
            
            // 1. 触发角色模型受伤动画
            if (characterAnimController != null)
            {
                characterAnimController.TriggerHitAnimation();
                Debug.Log($"[DEBUG SEND: HIT] 触发 Animator Trigger: Hit。");
            }

            // 2. 触发 UI 动画（例如：血条闪烁、伤害数字弹出）
            if (uiAnimator != null)
            {
                // ⭐ 优化：使用 Hash 值触发 UI 闪烁动画 ⭐
                uiAnimator.SetTrigger(UI_FLASH_HASH); 
            }
        }
        else if (damageTaken < 0)
        {
            // 收到治疗 (damageTaken 为负数)
            Debug.Log($"[DEBUG RECEIVE: HEAL] 收到治疗指令。治疗量: {-damageTaken}。");

            // 1. 触发角色模型治疗动画 (如果有)
            if (characterAnimController != null) 
            { 
                // 示例：characterAnimController.TriggerHealAnimation(); 
            }

            // 2. 触发 UI 治疗效果
            if (uiAnimator != null) 
            { 
                // 示例：uiAnimator.SetTrigger(Animator.StringToHash("HealEffect")); 
            }
        }
    }

    /// <summary>
    /// Responds to CharacterBase.OnCharacterDied event.
    /// </summary>
    private void HandleDeath()
    {
        Debug.Log($"[DEBUG RECEIVE: DEATH] 收到 OnCharacterDied 事件。");
        
        // ⭐ 关键修正 1.1：在 character 引用被破坏前，缓存其名称 ⭐
        if (this.character != null)
        {
            _dyingCharacterName = this.character.characterName;
        }

        // ⭐ 关键：通知 BattleManager 角色已进入死亡流程，需要等待清理。 ⭐
        if (BattleManager.Instance != null && character != null)
        {
            BattleManager.Instance.RegisterDyingCharacter(this.character);
            Debug.Log($"[DEBUG SEND: DEATH REG] 已通知 BattleManager {_dyingCharacterName} 开始死亡流程。");
        }
        
        // 触发角色模型死亡动画
        if (characterAnimController != null)
        {
            characterAnimController.TriggerDieAnimation();
            Debug.Log($"[DEBUG SEND: DEATH] 触发 Animator Trigger: Die。");
        }
        
        // 触发 UI 死亡/隐藏动画 (如果有)
        if (uiAnimator != null)
        {
            // ⭐ 优化：使用 Hash 值触发 UI 隐藏动画 ⭐
            uiAnimator.SetTrigger(UI_HIDE_HASH); 
        }

        // 追踪日志: 取消订阅 CharacterBase 事件 (防止重复触发动画)
        if (this.character != null)
        {
            this.character.OnHpChanged -= HandleHit; 
            this.character.OnCharacterDied -= HandleDeath; 
            Debug.Log($"[DEBUG CLEANUP: DEATH] 死亡后，已取消订阅 HP 和死亡事件。");
        }
        
        // Immediately clean up Intent UI
        if (intentFadeSequence != null)
        {
            intentFadeSequence.Kill();
        }
        if (intentUIRoot != null)
        {
            intentUIRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Public wrapper method called by DeathRelay.cs (or directly by Unity Animation Event)
    /// to notify that the death animation has completed.
    /// </summary>
    public void NotifyDeathAnimationCompleted() 
    {
        Debug.Log($"[DEBUG RECEIVE: DEATH RELAY] 收到死亡动画完成通知。追踪角色: {_dyingCharacterName}");
        
        // ⭐ 关键修正 2：移除导致卡死的 null 检查！强制执行清理事件。 ⭐
        // 即使 character 引用失效，也必须让 BattleManager 收到通知以解除战斗等待。
        // 原来的行：if (character == null) return; 已移除
        
        // Invoke the event for subscribers (BattleManager is the ultimate subscriber via OnActualDeathAnimationComplete)
        OnDeathAnimationComplete?.Invoke();
        Debug.Log($"[DEBUG SEND: DEATH RELAY] OnDeathAnimationComplete 事件已触发，准备最终清理。");
    }
    
    // === Intent UI Logic (FadeIn / FadeOut) ===
    
    /// <summary>
    /// 刷新敌人的意图UI，并更新Animator中的IsVisible Bool。
    /// </summary>
    public void RefreshIntent(IntentType type, int value)
    {
        Debug.Log($"[DEBUG RECEIVE: INTENT] 收到意图刷新指令。Type: {type}, Value: {value}");
        
        if (intentConfig == null || intentUIRoot == null || characterAnimController == null) return;
        if (character != null && character.IsDead) return; // 死亡后不再刷新意图

        // 确保清除上一个 Sequence，防止冲突
        if (intentFadeSequence != null)
        {
            intentFadeSequence.Kill(); 
        }

        if (type == IntentType.NONE)
        {
            // 1. 通知角色模型隐藏意图姿态
            characterAnimController.SetIntentVisibility(false);

            // 2. 触发 UI 自身的淡出动画（如果需要）
            if (uiAnimator != null)
            {
                // 示例：uiAnimator.SetTrigger(Animator.StringToHash("FadeOut")); 
            }
            
            // 3. 使用 DOTween 确保 UI 物体最终被禁用
            intentFadeSequence = DOTween.Sequence()
                .AppendInterval(fadeDuration)
                .AppendCallback(() => intentUIRoot.SetActive(false));
            Debug.Log("[DEBUG SEND: INTENT] 意图设置为NONE，触发淡出逻辑。");

        }
        else
        {
            // 1. 通知角色模型显示意图姿态
            characterAnimController.SetIntentVisibility(true);
            
            // 2. 更新 UI 文本和图标
            intentIcon.sprite = intentConfig.GetIcon(type);
            intentValueText.text = value.ToString();

            intentUIRoot.SetActive(true);
            
            // 3. 触发 UI 自身的淡入动画（如果需要）
            if (uiAnimator != null)
            {
                // 示例：uiAnimator.SetTrigger(Animator.StringToHash("FadeIn")); 
            }
            
            Debug.Log($"[DEBUG SEND: INTENT] 意图刷新完成。");
        }
    }
}