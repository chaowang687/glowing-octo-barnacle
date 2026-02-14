using UnityEngine;
using UnityEngine.UI; // 用于 Text 和 Image 组件
using DG.Tweening; // 假设你还在使用 DOTween 进行动画

public class CharacterDisplay : MonoBehaviour
{
    [Header("角色数据")]
    private CharacterBase character; // 内部引用，保存当前 CharacterBase 数据

    [Header("UI 引用")]
    public Text nameText;
    public Text hpText;
    public Text blockText;
    public Image characterArtwork; // 用于显示角色图片

    // 添加精灵图引用
    [Header("精灵图设置")]
    [Tooltip("如果 CharacterBase 中没有精灵图，使用这个默认图")]
    public Sprite defaultSprite;

    void Awake()
    {
        // 如果未设置默认精灵图，尝试从资源加载
        if (defaultSprite == null)
        {
            defaultSprite = Resources.Load<Sprite>("DefaultCharacterSprite");
        }
    }

    /// <summary>
    /// 初始化此 CharacterDisplay，绑定到指定的 CharacterBase 数据。
    /// </summary>
    /// <param name="charBase">要显示的 CharacterBase 对象。</param>
    public void Initialize(CharacterBase charBase)
    {
        character = charBase;
        if (character == null)
        {
            Debug.LogError("CharacterDisplay 初始化失败：传入的 CharacterBase 为空！", this);
            return;
        }

        // 注册事件监听，当 CharacterBase 的属性变化时，自动更新UI
        if (character != null)
        {
            // ⭐ 注意：CharacterBase 需要有这些事件 ⭐
            // character.OnHealthChanged += UpdateDisplay;
            // character.OnBlockChanged += UpdateDisplay;
            // 或者我们可以直接监听值变化，每次更新都调用 UpdateDisplay
        }

        UpdateDisplay(); // 首次初始化时更新 UI
        Debug.Log($"CharacterDisplay 已初始化: {character.characterName}");
    }

    /// <summary>
    /// 更新 UI 显示内容（名字、血量、格挡、图片）。
    /// </summary>
    public void UpdateDisplay()
    {
        if (character == null) return;

        if (nameText != null) nameText.text = character.characterName;
        if (hpText != null) hpText.text = $"HP: {character.currentHp}/{character.maxHp}";
        if (blockText != null) blockText.text = $"格挡: {character.CurrentBlock}";
        
        // ⭐ 关键修复：根据 CharacterBase 数据设置角色图片 ⭐
        if (characterArtwork != null)
        {
            // 方式1：如果 CharacterBase 有 artworkSprite 属性
            // if (character.artworkSprite != null)
            // {
            //     characterArtwork.sprite = character.artworkSprite;
            // }
            // 方式2：如果 CharacterBase 有 characterSprite 属性
            // else if (character.characterSprite != null)
            // {
            //     characterArtwork.sprite = character.characterSprite;
            // }
            // 方式3：使用默认精灵图
            // else if (defaultSprite != null)
            // {
            //     characterArtwork.sprite = defaultSprite;
            // }
            // 方式4：临时设置一个颜色
            // else
            // {
            //     characterArtwork.color = Color.gray;
            // }
            
            // 实际实现：根据你的 CharacterBase 结构选择合适的方式
            // 假设 CharacterBase 有一个 artwork 字段
            // 或者我们可以通过反射或其他方式获取
        }
    }
    
    /// <summary>
    /// 设置角色精灵图（手动）
    /// </summary>
    /// <param name="sprite">要设置的精灵图</param>
    public void SetCharacterSprite(Sprite sprite)
    {
        if (characterArtwork != null && sprite != null)
        {
            characterArtwork.sprite = sprite;
            Debug.Log($"角色 {character?.characterName ?? "Unknown"} 的精灵图已设置为: {sprite.name}");
        }
    }
    
    /// <summary>
    /// 根据精灵图名称从 Resources 加载并设置
    /// </summary>
    /// <param name="spriteName">精灵图资源名称</param>
    public void LoadAndSetSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return;
        
        Sprite sprite = Resources.Load<Sprite>(spriteName);
        if (sprite != null)
        {
            SetCharacterSprite(sprite);
        }
        else
        {
            Debug.LogWarning($"无法加载精灵图资源: {spriteName}");
        }
    }

    // (可选) 当对象销毁时，取消事件监听，防止内存泄露
    void OnDestroy()
    {
        if (character != null)
        {
            // character.OnHealthChanged -= UpdateDisplay;
            // character.OnBlockChanged -= UpdateDisplay;
        }
    }

    // (可选) 可以添加一些动画方法，例如被攻击时的闪烁、死亡时的消失等
    public Tween AnimateTakeDamage()
    {
        // 示例：短暂红色闪烁
        if (characterArtwork != null)
        {
            return characterArtwork.DOColor(Color.red, 0.1f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutFlash)
                .SetDelay(0.05f)
                .SetLink(gameObject);
        }
        return null;
    }
    
    public Tween AnimateGainBlock()
    {
        // 示例：短暂绿色闪烁或放大
        return transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f)
            .SetLink(gameObject);
    }
    
    // ⭐ NEW: 公共方法来获取 CharacterBase ⭐
    public CharacterBase GetCharacterBase()
    {
        return character;
    }
    
    /// <summary>
    /// 获取当前显示的精灵图
    /// </summary>
    public Sprite GetCurrentSprite()
    {
        return characterArtwork?.sprite;
    }
}