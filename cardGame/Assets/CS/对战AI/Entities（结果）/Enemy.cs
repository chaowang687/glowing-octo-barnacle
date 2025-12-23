using UnityEngine;
using System;
using DG.Tweening;
using System.Collections;

// Enemy.cs - 敌人角色类
public class Enemy : CharacterBase
{
    [Header("Enemy Specific Stats")]
    public int enemyLevel = 1;
    public int baseDamage = 10;
    public int experienceReward = 50;
    public int goldReward = 25;
    
    [Header("Enemy Abilities")]
    public bool hasSpecialAbility = false;
    public string specialAbilityName = "";
    public int specialAbilityCooldown = 0;
    private int currentCooldown = 0;
    
    [Header("Visual Effects")]
    public GameObject deathEffectPrefab;
    public AudioClip deathSound;
    public AudioClip attackSound;
    
    private AudioSource audioSource;
    
    // 修复：正确重写 Awake 方法
    protected override void Awake()
    {
        // 调用基类的 Awake
        base.Awake();
        
        // 敌人特有的初始化
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        Debug.Log($"Enemy {characterName} initialized");
    }
    
    /// <summary>
    /// 敌人初始化
    /// </summary>
    public void InitializeEnemy(string name, int maxHp, int level = 1, int damage = 10)
    {
        Initialize(name, maxHp);
        enemyLevel = level;
        baseDamage = damage;
        experienceReward = level * 50;
        goldReward = level * 25;
        
        Debug.Log($"Enemy {characterName} (Level {level}) initialized with {maxHp} HP and {damage} damage");
    }
    
    /// <summary>
    /// 敌人死亡处理 - 正确重写 HandleDeath 方法
    /// </summary>
    protected override void HandleDeath()
    {
        if (IsDead) return;
        
        Debug.Log($"Enemy {characterName} is dying...");
        
        // 1. 调用基类的死亡处理
        base.HandleDeath();
        
        // 2. 敌人特有的死亡逻辑
        PlayEnemyDeathAnimation();
        
        // 3. 掉落奖励
        DropRewards();
        
        // 4. 通知战斗管理器
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.HandleDyingCharacterCleanup(this);
        }
    }
    
    /// <summary>
    /// 播放敌人死亡动画
    /// </summary>
    private void PlayEnemyDeathAnimation()
    {
        // 死亡特效
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 死亡音效
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
    
    /// <summary>
    /// 重写淡出协程，为敌人添加特殊效果
    /// </summary>
    protected override IEnumerator FadeOutCoroutine()
    {
        // 敌人可以有更戏剧性的死亡效果
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 先变红然后消失
            spriteRenderer.color = Color.red;
            
            float duration = 1.0f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                spriteRenderer.color = new Color(1f, 0f, 0f, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // 延迟后销毁
        yield return new WaitForSeconds(0.5f);
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 掉落奖励
    /// </summary>
    private void DropRewards()
    {
        Debug.Log($"{characterName} dropped {experienceReward} EXP and {goldReward} Gold");
        
        // 这里可以触发奖励UI显示
        // 例如：UIManager.Instance.ShowRewardPopup(experienceReward, goldReward);
        
        // 如果有特殊掉落
        if (UnityEngine.Random.value < 0.3f) // 30% 几率掉落物品
        {
            Debug.Log($"{characterName} dropped a special item!");
            // 触发物品掉落逻辑
        }
    }
    
    /// <summary>
    /// 敌人攻击 - 覆盖基类的攻击行为
    /// </summary>
    public override Sequence TakeDamage(int damage, bool isAttack = false)
    {
        // 敌人可以有特殊的受伤害反应
        Debug.Log($"{characterName} is taking {damage} damage");
        
        // 如果有特殊能力，可能在受到伤害时触发
        if (hasSpecialAbility && currentCooldown <= 0 && UnityEngine.Random.value < 0.2f)
        {
            TriggerSpecialAbility();
        }
        
        // 调用基类的受伤害处理
        return base.TakeDamage(damage, isAttack);
    }
    
    /// <summary>
    /// 触发特殊能力
    /// </summary>
    private void TriggerSpecialAbility()
    {
        if (string.IsNullOrEmpty(specialAbilityName)) return;
        
        Debug.Log($"{characterName} uses {specialAbilityName}!");
        
        // 根据能力名称执行不同效果
        switch (specialAbilityName.ToLower())
        {
            case "heal":
                Heal(maxHp / 4); // 恢复25%生命
                break;
            case "rage":
                // 增加攻击力
                baseDamage = (int)(baseDamage * 1.5f);
                Debug.Log($"{characterName} enrages! Damage increased to {baseDamage}");
                break;
            case "shield":
                // 增加格挡
                AddBlock(15, 2);
                Debug.Log($"{characterName} raises a shield!");
                break;
            default:
                Debug.Log($"{characterName} uses an unknown ability: {specialAbilityName}");
                break;
        }
        
        // 设置冷却时间
        currentCooldown = specialAbilityCooldown;
    }
    
    /// <summary>
    /// 敌人回合开始
    /// </summary>
    public override void AtStartOfTurn()
    {
        base.AtStartOfTurn();
        
        // 减少特殊能力冷却
        if (currentCooldown > 0)
        {
            currentCooldown--;
            Debug.Log($"{characterName}'s {specialAbilityName} cooldown: {currentCooldown} turns remaining");
        }
    }
    
    /// <summary>
    /// 敌人攻击玩家
    /// </summary>
    public Sequence PerformAttack(CharacterBase target)
    {
        if (target == null || target.IsDead)
        {
            Debug.LogWarning("Cannot attack null or dead target");
            return DOTween.Sequence();
        }
        
        Sequence attackSequence = DOTween.Sequence();
        
        Debug.Log($"{characterName} attacks {target.characterName} for {baseDamage} damage!");
        
        // 攻击音效
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // 攻击动画：冲向目标然后返回
        Vector3 originalPosition = transform.position;
        float attackDistance = 1.5f;
        
        attackSequence.Append(transform.DOMove(
            target.transform.position - (target.transform.position - originalPosition).normalized * attackDistance, 
            0.2f));
        attackSequence.AppendCallback(() => {
            // 应用伤害
            target.TakeDamage(baseDamage, true);
        });
        attackSequence.Append(transform.DOMove(originalPosition, 0.2f));
        
        return attackSequence;
    }
    
    /// <summary>
    /// 获取经验奖励
    /// </summary>
    public int GetExperienceReward()
    {
        return experienceReward;
    }
    
    /// <summary>
    /// 获取金币奖励
    /// </summary>
    public int GetGoldReward()
    {
        return goldReward;
    }
    
    /// <summary>
    /// 敌人逃跑（用于某些特殊敌人）
    /// </summary>
    public void Flee()
    {
        Debug.Log($"{characterName} flees from battle!");
        
        Sequence fleeSequence = DOTween.Sequence();
        fleeSequence.Append(transform.DOMove(transform.position + Vector3.right * 5f, 1f));
        fleeSequence.Join(transform.DOScale(Vector3.zero, 1f));
        fleeSequence.OnComplete(() => {
            // 逃跑后通知战斗管理器
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.HandleDyingCharacterCleanup(this);
            }
            Destroy(gameObject);
        });
    }
}