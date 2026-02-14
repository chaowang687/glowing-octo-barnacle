using UnityEngine;
using System;
using DG.Tweening;
using System.Collections;
using SlayTheSpireMap;

// Hero.cs - 玩家英雄类
public class Hero : CharacterBase
{
    [Header("Hero Specific Stats")]
    public int baseEnergy = 3;
    public int currentEnergy = 3;
    
    [Header("Hero Abilities")]
    public bool hasShieldAbility = false;
    public bool hasDoubleAttack = false;
    
    [Header("Visual Effects")]
    public GameObject deathEffectPrefab;
    public AudioClip deathSound;
    public AudioClip attackSound;
    
    private AudioSource audioSource;
    
    // 修复：正确重写 Awake 方法
    protected override void Awake()
    {
        // 关键：不要调用 base.Awake()，因为它会强制重置血量
        // base.Awake(); 
        
        // 我们手动初始化必要的组件，但不重置数值
        // 使用基类提供的 protected 方法来初始化事件，避开编译错误
        InitializeEvents();
        
        // 关键：监听自身血量变化，实时同步到全局数据
        // 这样无论是战斗中扣血、回血，还是休息回血，都会被记录下来
        OnHealthChanged += SyncToGlobal;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    /// <summary>
    /// 英雄初始化
    /// </summary>

 // Hero.cs
public void SyncFromGlobal()
{
    if (GameDataManager.Instance == null) return;
    
    var data = GameDataManager.Instance.playerData;
    
    // 强制赋值给私有变量以确保安全
    this.maxHp = data.maxHealth;
    this.currentHp = data.health;
    
    Debug.Log($"[Hero] 同步成功: HP {currentHp}/{maxHp}");

    // 必须手动触发一次 UI 刷新
    GetComponentInChildren<CharacterUIDisplay>(true)?.Initialize(this);
}

// 实时同步血量到全局数据
private void SyncToGlobal(int current, int max)
{
    if (GameDataManager.Instance != null)
    {
        // 注意：这里我们只更新内存中的数值，避免频繁调用 PlayerPrefs.Save() 造成卡顿
        // GameDataManager.Health 的 setter 会调用 SaveGameData，如果觉得卡顿可以优化
        // 目前为了保证数据绝对安全，直接赋值即可
        GameDataManager.Instance.Health = current;
        Debug.Log($"[Hero] 血量变化已同步全局: {current}");
    }
}

    public void InitializeHero(string name, int maxHp, int energy = 3)
    {
        Initialize(name, maxHp);
        baseEnergy = energy;
        currentEnergy = energy;
        
        Debug.Log($"Hero {characterName} initialized with {maxHp} HP and {energy} energy");
    }




    private void OnDestroy()
    {
        OnHealthChanged -= SyncToGlobal;
    }

    /// <summary>
    /// 英雄死亡处理 - 现在正确重写 HandleDeath
    /// </summary>
    protected override void HandleDeath()
    {
        if (IsDead) return;
        
        Debug.Log($"Hero {characterName} is dying...");
        
        // 1. 调用基类的死亡处理
        base.HandleDeath();
        
        // 2. 英雄特有的死亡逻辑
        PlayHeroDeathAnimation();
        
        // 3. 英雄死亡时不立即销毁，而是隐藏
        StartCoroutine(HeroDeathSequence());
    }
    
    /// <summary>
    /// 播放英雄死亡动画
    /// </summary>
    private void PlayHeroDeathAnimation()
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
    /// 英雄死亡序列
    /// </summary>
    private IEnumerator HeroDeathSequence()
    {
        // 等待基类的淡出效果
        yield return new WaitForSeconds(1.5f);
        
        // 英雄死亡后可能需要保持游戏对象，但隐藏
        gameObject.SetActive(false);
        
        Debug.Log($"Hero {characterName} has died");
    }
    
    /// <summary>
    /// 重置英雄状态（用于新战斗或复活）
    /// </summary>
    public void ResetHero()
    {
        IsDead = false;
        currentHp = maxHp;
        CurrentBlock = 0;
        currentEnergy = baseEnergy;
        
        // 恢复显示
        gameObject.SetActive(true);
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.DOFade(1f, 0.5f);
        }
        
        Debug.Log($"Hero {characterName} has been reset");
    }
    
    /// <summary>
    /// 英雄使用技能
    /// </summary>
    public void UseSkill(int energyCost)
    {
        if (currentEnergy >= energyCost)
        {
            currentEnergy -= energyCost;
            Debug.Log($"Hero used skill, remaining energy: {currentEnergy}");
        }
        else
        {
            Debug.LogWarning($"Not enough energy to use skill. Current: {currentEnergy}, Required: {energyCost}");
        }
    }
    
    /// <summary>
    /// 恢复能量
    /// </summary>
    public void RestoreEnergy(int amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, baseEnergy);
        Debug.Log($"Hero restored {amount} energy. Total: {currentEnergy}");
    }
    
    /// <summary>
    /// 英雄的特殊攻击（如果有双倍攻击能力）
    /// </summary>
    public Sequence PerformHeroAttack(int damage, CharacterBase target)
    {
        if (target == null || target.IsDead)
        {
            Debug.LogWarning("Cannot attack null or dead target");
            return DOTween.Sequence();
        }
        
        Sequence attackSequence = DOTween.Sequence();
        
        if (hasDoubleAttack)
        {
            // 双倍攻击
            attackSequence.AppendCallback(() => Debug.Log($"{characterName} performs double attack!"));
            
            // 第一次攻击
            Vector3 originalPosition = transform.position;
            attackSequence.Append(transform.DOMove(target.transform.position + Vector3.left * 0.5f, 0.2f));
            attackSequence.AppendCallback(() => {
                if (target != null && !target.IsDead)
                    target.TakeDamage(damage / 2, true);
            });
            attackSequence.Append(transform.DOMove(originalPosition, 0.2f));
            
            // 第二次攻击
            attackSequence.Append(transform.DOMove(target.transform.position + Vector3.right * 0.5f, 0.2f));
            attackSequence.AppendCallback(() => {
                if (target != null && !target.IsDead)
                    target.TakeDamage(damage / 2, true);
            });
            attackSequence.Append(transform.DOMove(originalPosition, 0.2f));
        }
        else
        {
            // 普通攻击
            attackSequence.AppendCallback(() => Debug.Log($"{characterName} attacks!"));
            Vector3 originalPosition = transform.position;
            attackSequence.Append(transform.DOMove(target.transform.position, 0.2f));
            attackSequence.AppendCallback(() => {
                if (target != null && !target.IsDead)
                    target.TakeDamage(damage, true);
            });
            attackSequence.Append(transform.DOMove(originalPosition, 0.2f));
        }
        
        // 攻击音效
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        return attackSequence;
    }
    
    /// <summary>
    /// 英雄的特殊防御
    /// </summary>
    public void UseShield(int blockAmount)
    {
        if (hasShieldAbility)
        {
            AddBlock(blockAmount * 2, 2); // 英雄的盾牌效果更强
            Debug.Log($"{characterName} uses shield ability! Gained {blockAmount * 2} block");
        }
        else
        {
            AddBlock(blockAmount, 1);
            Debug.Log($"{characterName} gains {blockAmount} block");
        }
    }
    
    /// <summary>
    /// 重写回合开始逻辑
    /// </summary>
    public override void AtStartOfTurn()
    {
        base.AtStartOfTurn();
        
        // 英雄每回合恢复1点能量
        currentEnergy = Mathf.Min(currentEnergy + 1, baseEnergy);
        Debug.Log($"{characterName} gains 1 energy. Total: {currentEnergy}");
    }
    
    /// <summary>
    /// 检查是否有足够的能量
    /// </summary>
    public bool HasEnoughEnergy(int cost)
    {
        return currentEnergy >= cost;
    }
}