using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Linq;

// 假设 IntentType, IntentIconConfig, CharacterAnimatorController, CharacterBase, BattleManager 在其他地方定义

/// <summary>
/// 负责角色的视觉表现、UI 意图和血条的显示。
/// 【修改】不再直接触发动画，只负责UI和视觉效果
/// </summary>
public class EnemyDisplay : MonoBehaviour
{
    // 在 EnemyDisplay.cs 中
    public Animator lootAnimator; // 拖入刚才那个节点的 Animator
    public TextMeshProUGUI lootText; // 拖入那个节点下的文本
    [Header("Loot Feedback")]
    public GameObject lootSuccessEffect; // 比如一个写着"抢到了！"的浮动图标预制体
    public AudioClip lootLaughSound;     // 海盗大笑的音效
    private AudioSource audioSource;
    // Public event for external scripts (如 DeathRelay.cs) 通知动画完成。
    public event Action OnDeathAnimationComplete;

    // 动画触发器的 Hash 值，用于性能优化
    private static readonly int UI_FLASH_HASH = Animator.StringToHash("Flash");
    private static readonly int UI_HIDE_HASH = Animator.StringToHash("HideUI");

    // 核心数据引用
    private CharacterBase character; 

    // ⭐ 添加：记录上次的HP值，用于计算HP变化量
    private int _previousHp = 0;
    
    // ⭐ 关键修正 1：缓存角色名称，用于在 character 引用被销毁后仍能安全追踪和记录日志。 ⭐
    private string _dyingCharacterName = "Unknown Character"; 

    /// <summary>
    /// 公共属性，允许外部（如 BattleManager）安全读取该角色数据。
    /// </summary>
    public CharacterBase CharacterData => character; 

    // ⭐ 修改：动画控制器引用改为私有，EnemyDisplay不再直接控制动画 ⭐
    [Header("角色动画控制器引用")]
    [Tooltip("动画控制器引用 - 主要用于UI和视觉反馈")]
    private CharacterAnimatorController _characterAnimController; 
    
    [Header("精灵图显示")]
    public Image characterImage; // 用于显示敌人2D图像
    
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
    
    // ⭐ 新增：存储 EnemyData 引用 ⭐
    private EnemyData _enemyData;
    
    /// <summary>
    /// 设置精灵图
    /// </summary>
    public void SetArtwork(Sprite sprite)
    {
        if (characterImage != null && sprite != null)
        {
            characterImage.sprite = sprite;
            characterImage.gameObject.SetActive(true);
            Debug.Log($"[DEBUG] 设置敌人精灵图: {sprite.name}");
        }
        else if (characterImage == null)
        {
            Debug.LogWarning($"[DEBUG] characterImage 为空，无法设置精灵图");
        }
        else if (sprite == null)
        {
            Debug.LogWarning($"[DEBUG] 传入的精灵图为空");
        }
    }
    
    /// <summary>
    /// ⭐ 新增：设置动画控制器引用（由外部设置）⭐
    /// </summary>
    public void SetAnimatorController(CharacterAnimatorController animController)
    {
        _characterAnimController = animController;
        if (_characterAnimController != null)
        {
            Debug.Log($"[DEBUG] EnemyDisplay 已设置动画控制器: {_characterAnimController.gameObject.name}");
        }
    }
    
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

        // ⭐ 修改：不再在Awake中查找动画控制器，等待外部设置 ⭐
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
        
        // ⭐ 新增：尝试获取 AudioSource ⭐
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
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
            // ⭐ 记录初始HP值 ⭐
            _previousHp = this.character.currentHp;
            
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
    /// ⭐ 新增：增强版初始化，支持 EnemyData ⭐
    /// </summary>
    public void Initialize(CharacterBase characterData, EnemyData enemyData)
    {
        Debug.Log($"[DEBUG TRACE: INIT] 尝试为 {characterData.characterName} 初始化 Display (带 EnemyData)...");
        
        // 存储 EnemyData 引用
        _enemyData = enemyData;
        
        // 调用基础初始化
        Initialize(characterData);
        
        // ⭐ 设置精灵图（关键修复）⭐
        if (_enemyData != null && _enemyData.artwork != null)
        {
            SetArtwork(_enemyData.artwork);
            Debug.Log($"[DEBUG SETUP] 已为 {characterData.characterName} 设置精灵图: {_enemyData.artwork.name}");
        }
        else
        {
            Debug.LogWarning($"[DEBUG WARNING] 没有找到 EnemyData 或 artwork");
        }
        
        // ⭐ 不再在EnemyDisplay中设置动画控制器 ⭐
    }

    /// <summary>
    /// 当死亡动画播放完毕后，由 NotifyDeathAnimationCompleted() 调用的最终处理方法。
    /// </summary>
    private void OnActualDeathAnimationComplete()
    {
        Debug.Log($"[DEBUG FINAL CLEANUP] {_dyingCharacterName} 死亡动画完成，通知 BattleManager 销毁。");
        
        // 1. 通知 BattleManager 死亡流程结束
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.HandleDeathAnimationComplete(this.character.gameObject);
            Debug.Log($"[DEBUG SEND: FINAL CLEANUP] 已强制尝试通知 BattleManager 死亡动画流程结束。");
        }
        
        // 2. 避免在销毁过程中仍触发事件
        OnDeathAnimationComplete -= OnActualDeathAnimationComplete;
        
        // 3. 销毁包含此显示组件的 GameObject 自身
        Destroy(gameObject); 
        Debug.Log($"[DEBUG FINAL CLEANUP] {_dyingCharacterName} 的 GameObject 已被销毁。");
    }
    
    // === Internal Event Response (Subscribed to CharacterBase Events) ===

    /// <summary>
    /// Responds to CharacterBase.OnHpChanged event (damage or healing).
    /// ⭐ 修改：不再触发受伤动画，只做UI反馈 ⭐
    /// </summary>
    private void HandleHit(int currentHp, int maxHp)
    {
        // ⭐ 计算HP变化量 ⭐
        int hpChange = _previousHp - currentHp;
        _previousHp = currentHp; // 更新记录的HP值
        
        Debug.Log($"[DEBUG RECEIVE: HIT] HandleHit 方法被调用！HP变化: {hpChange}，当前HP: {currentHp}/{maxHp}。");
        
        if (character == null)
        {
            Debug.Log($"[DEBUG GUARD: HIT] 角色为空，退出 HandleHit。");
            return;
        }
        
        if (hpChange > 0)
        {
            // 收到伤害
            
            // ⭐ 修改：不再触发角色模型受伤动画，由EnemyAI或CharacterAnimatorController处理 ⭐
            // 只做UI和视觉反馈
            
            // 触发 UI 动画
            if (uiAnimator != null)
            {
                uiAnimator.SetTrigger(UI_FLASH_HASH); 
            }
            
            // 精灵图闪烁效果
            if (characterImage != null)
            {
                StartCoroutine(FlashSprite(Color.red, 0.1f));
            }
        }
        else if (hpChange < 0)
        {
            // 收到治疗
            int healAmount = -hpChange;
            Debug.Log($"[DEBUG RECEIVE: HEAL] 收到治疗指令。治疗量: {healAmount}。");

            // 治疗效果
            if (characterImage != null)
            {
                StartCoroutine(FlashSprite(Color.green, 0.1f));
            }
        }
    }
    
    /// <summary>
    /// ⭐ 新增：精灵图闪烁效果 ⭐
    /// </summary>
    private IEnumerator FlashSprite(Color flashColor, float duration)
    {
        if (characterImage == null) yield break;
        
        Color originalColor = characterImage.color;
        characterImage.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        characterImage.color = originalColor;
    }

    /// <summary>
    /// Responds to CharacterBase.OnCharacterDied event.
    /// ⭐ 修改：不再触发死亡动画，只做UI反馈 ⭐
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
        
        // ⭐ 修改：不再触发角色模型死亡动画，由EnemyAI处理 ⭐
        
        // 触发 UI 死亡/隐藏动画
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger(UI_HIDE_HASH); 
        }
        
        // 精灵图淡出效果
        if (characterImage != null)
        {
            StartCoroutine(FadeOutSprite(1.0f));
        }

        // 追踪日志: 取消订阅 CharacterBase 事件
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
    /// ⭐ 新增：精灵图淡出效果 ⭐
    /// </summary>
    private IEnumerator FadeOutSprite(float duration)
    {
        if (characterImage == null) yield break;
        
        float elapsedTime = 0f;
        Color originalColor = characterImage.color;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            characterImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }

    /// <summary>
    /// Public wrapper method called by DeathRelay.cs (or directly by Unity Animation Event)
    /// to notify that the death animation has completed.
    /// </summary>
    public void NotifyDeathAnimationCompleted() 
    {
        Debug.Log($"[DEBUG RECEIVE: DEATH RELAY] 收到死亡动画完成通知。追踪角色: {_dyingCharacterName}");
        
        // Invoke the event for subscribers
        OnDeathAnimationComplete?.Invoke();
        Debug.Log($"[DEBUG SEND: DEATH RELAY] OnDeathAnimationComplete 事件已触发，准备最终清理。");
    }
    
    // === Intent UI Logic (FadeIn / FadeOut) ===
    
    /// <summary>
    /// 刷新敌人的意图UI，并更新Animator中的IsVisible Bool。
    /// ⭐ 修改：只设置UI，不控制动画 ⭐
    /// </summary>
    public void RefreshIntent(IntentType type, int value)
    {
        Debug.Log($"[DEBUG RECEIVE: INTENT] 收到意图刷新指令。Type: {type}, Value: {value}");
        
        if (intentConfig == null || intentUIRoot == null) return;
        if (character != null && character.IsDead) return; // 死亡后不再刷新意图

        // 确保清除上一个 Sequence，防止冲突
        if (intentFadeSequence != null)
        {
            intentFadeSequence.Kill(); 
        }

        if (type == IntentType.NONE)
        {
            // 使用 DOTween 确保 UI 物体最终被禁用
            intentFadeSequence = DOTween.Sequence()
                .AppendInterval(fadeDuration)
                .AppendCallback(() => intentUIRoot.SetActive(false));
            Debug.Log("[DEBUG SEND: INTENT] 意图设置为NONE，触发淡出逻辑。");
        }
        else
        {
            // 更新 UI 文本和图标
            intentIcon.sprite = intentConfig.GetIcon(type);
            intentValueText.text = value.ToString();
            intentUIRoot.SetActive(true);
            
            Debug.Log($"[DEBUG SEND: INTENT] 意图刷新完成。");
        }
    }
    
    // ⭐ 新增：获取敌人数据 ⭐
    public EnemyData GetEnemyData()
    {
        return _enemyData;
    }
    
    // ⭐ 新增：获取精灵图 ⭐
    public Sprite GetArtwork()
    {
        return characterImage?.sprite;
    }
    
    // ⭐ 新增：获取角色名称 ⭐
    public string GetCharacterName()
    {
        return character?.characterName ?? _dyingCharacterName;
    }
    
    // ⭐ 新增：获取动画控制器（给外部使用）⭐
    public CharacterAnimatorController GetAnimatorController()
    {
        return _characterAnimController;
    }
}