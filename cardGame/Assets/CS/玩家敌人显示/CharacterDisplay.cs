// CharacterDisplay.cs (新增文件)
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

    // (可选) 如果 CharacterDisplay 也需要处理高亮等UI交互，可以加上以下：
    // private RectTransform rectTransform;
    // private CanvasGroup canvasGroup;

    // (可选) 如果需要和 BattleUIController 交互，例如在悬停时改变显示
    // [HideInInspector] public BattleUIController uiController; 

    void Awake()
    {
        // (可选) 初始化 RectTransform 和 CanvasGroup
        // rectTransform = GetComponent<RectTransform>();
        // canvasGroup = GetComponent<CanvasGroup>();
        // if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
        // 假设 CharacterBase 有类似的事件，你需要根据实际情况添加
        // character.OnHealthChanged += UpdateDisplay; 
        // character.OnBlockChanged += UpdateDisplay;

        UpdateDisplay(); // 首次初始化时更新 UI
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
        
        // ⭐ TODO: 根据 CharacterBase 数据设置角色图片 ⭐
        // 这需要你的 CharacterBase 中有一个字段来存储角色图片（例如 Sprite 或 Texture）
        // if (characterArtwork != null && character.artworkSprite != null)
        // {
        //     characterArtwork.sprite = character.artworkSprite;
        // }
        // 如果没有图片，可以设置一个默认颜色或者保持透明
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
        return characterArtwork?.DOColor(Color.red, 0.1f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutFlash)
            .SetDelay(0.05f)
            .SetLink(gameObject);
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
}