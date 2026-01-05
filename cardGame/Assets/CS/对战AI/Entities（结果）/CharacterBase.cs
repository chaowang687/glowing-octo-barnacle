using UnityEngine;
using System;
using DG.Tweening;
using System.Collections;

public class CharacterBase : MonoBehaviour
{
    // 修复：将事件访问权限改为 protected，或者提供 protected 的触发方法
    // 为了不破坏外部订阅，我们保持 event 为 public，但提供 protected 的重置方法供子类调用
    
    public event Action<int, int> OnHealthChanged; 
    public event Action<int> OnBlockChanged;
    public event Action OnCharacterDied;

    // 新增：供子类初始化事件的方法
    protected void InitializeEvents()
    {
        OnHealthChanged = delegate { };
        OnBlockChanged = delegate { };
        OnCharacterDied = delegate { };
    }
    
    // 属性
    [SerializeField] protected string _characterName;
    [SerializeField] protected int _maxHp;
    [SerializeField] protected int _currentHp;
    [SerializeField] protected int _currentBlock;
    [SerializeField] public GameObject damagePopupPrefab;
    
    // 在 CharacterBase.cs 中添加
    [Header("UI 展现配置")]
    public Sprite characterSprite; // 对应报错的 characterSprite
    [TextArea]
    public string description;     // 对应报错的 description

    protected int blockDuration = 0;
    
    // 状态效果管理
    [System.Serializable]
    public class StatusEffect
    {
        public string effectName;
        public int amount;
        public int duration;
        
        public StatusEffect(string name, int amt, int dur)
        {
            effectName = name;
            amount = amt;
            duration = dur;
        }
    }
    
    protected System.Collections.Generic.List<StatusEffect> statusEffects = 
        new System.Collections.Generic.List<StatusEffect>();
    
    // 公共属性
    public string characterName 
    { 
        get => _characterName; 
        set => _characterName = value; 
    }
    
    public int maxHp 
    { 
        get => _maxHp; 
        set => _maxHp = value; 
    }
    
    public int currentHp 
    { 
        get => _currentHp; 
        set 
        {
            if (_currentHp != value)
            {
                int oldHp = _currentHp;
                _currentHp = Mathf.Clamp(value, 0, _maxHp);
                OnHealthChanged?.Invoke(_currentHp, _maxHp); // 触发事件
                
                // 计算伤害/治疗量
                int damageTaken = oldHp - _currentHp;
                
                if (_currentHp <= 0 && !IsDead)
                {
                    // 调用虚方法，允许子类重写死亡处理
                    HandleDeath();
                }
            }
        }
    }
    
    // 修复：添加 OnHpChanged 属性作为 OnHealthChanged 的别名（兼容性）
    public event Action<int, int> OnHpChanged
    {
        add { OnHealthChanged += value; }
        remove { OnHealthChanged -= value; }
    }
    
    // 修复：将 CurrentBlock 的 set 访问器改为 protected
    public int CurrentBlock 
    { 
        get => _currentBlock; 
        protected set // 允许子类设置
        {
            if (_currentBlock != value)
            {
                _currentBlock = value;
                OnBlockChanged?.Invoke(_currentBlock);
            }
        }
    }
    
    // 死亡标记
    public bool IsDead { get; protected set; } = false;
    
    protected virtual void Awake()
    {
        // 基类初始化
        if (_maxHp <= 0) _maxHp = 100;
        if (_currentHp <= 0) _currentHp = _maxHp;
        
        // 初始化事件委托，避免null
        OnHealthChanged = delegate { };
        OnBlockChanged = delegate { };
        OnCharacterDied = delegate { };
        
        // 自动加载并赋值伤害数字预制体
        if (damagePopupPrefab == null)
        {
            // 尝试从Resources加载
            damagePopupPrefab = Resources.Load<GameObject>("damage_num");
            
            // 如果Resources加载失败，尝试从Art文件夹查找
            if (damagePopupPrefab == null)
            {
                GameObject prefabInScene = GameObject.Find("damage_num");
                if (prefabInScene != null)
                {
                    damagePopupPrefab = prefabInScene;
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] 无法找到 damagePopupPrefab！请在Inspector中手动赋值。");
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化方法
    /// </summary>
    public void Initialize(string name, int maxHp, Sprite artwork = null)
    {
        characterName = name;
        this.maxHp = maxHp;
        currentHp = maxHp;
        CurrentBlock = 0;
        IsDead = false;
        
        // 清除所有状态效果
        statusEffects.Clear();
        
        Debug.Log($"{characterName} initialized with {maxHp} HP");
    }
    
    /// <summary>
    /// 受到伤害 - 现在是虚方法，可以重写
    /// </summary>
    public virtual Sequence TakeDamage(int damage, bool isAttack = false)
{
    if (IsDead) return DOTween.Sequence();
    
    Sequence damageSequence = DOTween.Sequence();
    
    // 计算实际伤害（考虑格挡）
    int actualDamage = damage;
    if (CurrentBlock > 0)
    {
        if (CurrentBlock >= damage)
        {
            // 格挡完全吸收伤害
            actualDamage = 0;
            CurrentBlock -= damage;
        }
        else
        {
            // 格挡部分吸收伤害
            actualDamage = damage - CurrentBlock;
            CurrentBlock = 0;
        }
    }
    
    // 问题在这里！立即扣血，没有等待动画
    if (actualDamage > 0)
    {
        currentHp -= actualDamage;  // <-- 立即扣血
        
        // 显示伤害数字
        if (damagePopupPrefab != null)
        {
            // 1. 在主画布下生成数字（不要直接 Instantiate 到世界坐标）
            // 假设你的怪物已经在主 Canvas 某处，我们直接让数字生成为怪物的兄弟节点或子节点
            GameObject popup = Instantiate(damagePopupPrefab, transform.parent);

            // 2. 设置位置：既然怪物也是 UI 元素，我们直接取怪物的 RectTransform 位置
            RectTransform enemyRect = GetComponent<RectTransform>();
            RectTransform popupRect = popup.GetComponent<RectTransform>();

            if (enemyRect != null && popupRect != null)
            {
                // 将数字置于怪物中心，向上偏移（像素单位，比如 100 像素）
                popupRect.anchoredPosition = enemyRect.anchoredPosition + new Vector2(0, 100f);
            }

            // 3. 初始化动画脚本
            var popupScript = popup.GetComponent<DamagePopupAnimation>();
            if (popupScript != null)
            {
                popupScript.Setup(actualDamage);
            }
        }
    }
    
    // 伤害动画序列（但血条已经扣过了）
    damageSequence.Append(transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f));
    damageSequence.AppendCallback(() => {
        string blockText = (damage - actualDamage > 0) ? $"（格挡吸收了 {damage - actualDamage} 点）" : "";
        Debug.Log($"{characterName} 受到 {damage} 点伤害{blockText}");
        
        if (currentHp <= 0)
        {
            Debug.Log($"{characterName} 被击败！");
        }
    });
    
    return damageSequence;
}
    
    /// <summary>
    /// 处理死亡 - 现在是受保护的虚方法，子类可以重写
    /// </summary>
    protected virtual void HandleDeath()
    {
        if (IsDead) return;
        
        IsDead = true;
        
        // 触发死亡事件
        OnCharacterDied?.Invoke();
        
        // 基础死亡效果
        Debug.Log($"{characterName} 死亡");
        
        // 简单淡出效果（子类可以重写为更复杂的动画）
        StartCoroutine(FadeOutCoroutine());
    }
    
    /// <summary>
    /// 淡出协程
    /// </summary>
    protected virtual IEnumerator FadeOutCoroutine()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            float duration = 1.0f;
            float elapsed = 0f;
            Color originalColor = spriteRenderer.color;
            
            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // 延迟后销毁（或只是隐藏）
        yield return new WaitForSeconds(0.5f);
        
        // 这里可以选择销毁或只是隐藏
        // Destroy(gameObject);
        // 或者隐藏，以便可能复活
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 添加格挡值并设置持续时间
    /// </summary>
    public void AddBlock(int amount, int duration = 1)
    {
        if (amount > 0 && !IsDead)
        {
            CurrentBlock += amount; // 现在可以设置，因为 set 是 protected
            blockDuration = Mathf.Max(blockDuration, duration);
            Debug.Log($"{characterName} 获得 {amount} 点格挡，持续 {blockDuration} 回合");
        }
    }
    
    /// <summary>
    /// 设置格挡值（直接设置，不添加）
    /// </summary>
    public void SetBlock(int amount)
    {
        if (!IsDead)
        {
            CurrentBlock = amount;
            Debug.Log($"{characterName} 格挡值设置为 {amount}");
        }
    }
    
    /// <summary>
    /// 递减格挡持续时间，如果到期则清除格挡
    /// </summary>
    public void DecrementBlockDuration()
    {
        if (blockDuration > 0)
        {
            blockDuration--;
            Debug.Log($"{characterName} 格挡持续时间减少至 {blockDuration} 回合");
            
            if (blockDuration <= 0)
            {
                ClearBlock();
            }
        }
    }
    
    /// <summary>
    /// 清除所有格挡
    /// </summary>
    public void ClearBlock()
    {
        if (CurrentBlock > 0)
        {
            Debug.Log($"{characterName} 的 {CurrentBlock} 点格挡被清除");
            CurrentBlock = 0;
            blockDuration = 0;
        }
    }
    
    /// <summary>
    /// 治疗
    /// </summary>
    public void Heal(int amount)
    {
        if (IsDead) return;
        
        int oldHp = currentHp;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        int healedAmount = currentHp - oldHp;
        
        if (healedAmount > 0)
        {
            Debug.Log($"{characterName} 恢复了 {healedAmount} 点生命值");
            
            // 治疗特效
            transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 1, 0.5f);
        }
    }
    
    // ===== 状态效果相关方法 =====
    
    /// <summary>
    /// 应用状态效果（修复：添加缺失的方法）
    /// </summary>
    public void ApplyStatusEffect(string effectName, int amount, int duration)
    {
        if (IsDead) return;
        
        // 检查是否已有相同效果
        StatusEffect existingEffect = statusEffects.Find(e => e.effectName == effectName);
        if (existingEffect != null)
        {
            // 更新现有效果
            existingEffect.amount = amount;
            existingEffect.duration = duration;
        }
        else
        {
            // 添加新效果
            statusEffects.Add(new StatusEffect(effectName, amount, duration));
        }
        
        Debug.Log($"{characterName} 获得状态效果: {effectName} ({amount}), 持续 {duration} 回合");
        
        // 立即应用某些效果
        if (effectName == "Poison" || effectName == "Burn")
        {
            // 中毒或燃烧立即造成伤害
            currentHp -= amount;
        }
    }
    
    /// <summary>
    /// 获取状态效果数值（修复：添加缺失的方法）
    /// </summary>
    public int GetStatusEffectAmount(string effectName)
    {
        StatusEffect effect = statusEffects.Find(e => e.effectName == effectName);
        return effect != null ? effect.amount : 0;
    }
    
    /// <summary>
    /// 移除状态效果
    /// </summary>
    public void RemoveStatusEffect(string effectName)
    {
        StatusEffect effect = statusEffects.Find(e => e.effectName == effectName);
        if (effect != null)
        {
            statusEffects.Remove(effect);
            Debug.Log($"{characterName} 移除了状态效果: {effectName}");
        }
    }
    
    /// <summary>
    /// 更新状态效果持续时间
    /// </summary>
    public void UpdateStatusEffects()
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = statusEffects[i];
            effect.duration--;
            
            if (effect.duration <= 0)
            {
                statusEffects.RemoveAt(i);
                Debug.Log($"{characterName} 的状态效果 {effect.effectName} 已过期");
            }
        }
    }
    
    /// <summary>
    /// 是否有指定状态效果
    /// </summary>
    public bool HasStatusEffect(string effectName)
    {
        return statusEffects.Exists(e => e.effectName == effectName);
    }
    
    // ===== 回合相关方法 =====
    
    /// <summary>
    /// 回合开始时的逻辑
    /// </summary>
    public virtual void AtStartOfTurn()
    {
        if (IsDead) return;
        
        Debug.Log($"{characterName} 回合开始");
        
        // 应用持续伤害效果
        foreach (StatusEffect effect in statusEffects)
        {
            if (effect.effectName == "Poison" || effect.effectName == "Burn")
            {
                currentHp -= effect.amount;
                Debug.Log($"{characterName} 受到 {effect.effectName} 效果，损失 {effect.amount} 生命值");
            }
        }
        
        // 更新状态效果持续时间
        UpdateStatusEffects();
    }
    
    /// <summary>
    /// 回合结束时的逻辑
    /// </summary>
    public virtual void AtEndOfTurn()
    {
        if (IsDead) return;
        
        Debug.Log($"{characterName} 回合结束");
        
        // 递减状态效果持续时间等
        DecrementBlockDuration();
    }
    
    /// <summary>
    /// 复活角色
    /// </summary>
    public virtual void Revive(int reviveHp = 0)
    {
        if (!IsDead) return;
        
        IsDead = false;
        currentHp = (reviveHp > 0) ? Mathf.Min(reviveHp, maxHp) : maxHp / 2;
        CurrentBlock = 0;
        blockDuration = 0;
        
        // 清除所有状态效果
        statusEffects.Clear();
        
        gameObject.SetActive(true);
        
        // 恢复显示
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        Debug.Log($"{characterName} 已复活，生命值: {currentHp}/{maxHp}");
    }
    
    /// <summary>
    /// 获取所有状态效果
    /// </summary>
    public System.Collections.Generic.List<StatusEffect> GetAllStatusEffects()
    {
        return statusEffects;
    }
}