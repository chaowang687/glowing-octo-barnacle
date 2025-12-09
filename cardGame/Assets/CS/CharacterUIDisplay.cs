using UnityEngine;
using UnityEngine.UI;
using System.Linq; 
using System; 
using TMPro; // ⭐ 核心修正：导入 TextMeshPro 命名空间 ⭐

public class CharacterUIDisplay : MonoBehaviour
{
    // UI 组件引用 (例如：血条 Image 或 Slider)
    [Header("UI Components")]
    public Slider hpSlider;
    public TextMeshProUGUI nameText; // ⭐ 修正为 TMP ⭐
    public TextMeshProUGUI hpValueText; // ⭐ 修正为 TMP ⭐

    // ⭐ 格挡 UI 引用 ⭐
    [Header("格挡 UI 引用")]
    [Tooltip("格挡图标和数值的根 GameObject，用于整体显示/隐藏。")]
    public GameObject blockDisplayRoot; 
    [Tooltip("显示格挡图标的 Image 组件。")]
    public Image blockIconImage; 
    [Tooltip("显示格挡数值的 TextMeshProUGUI 组件")]
    public TextMeshProUGUI blockValueText; // ⭐ 修正为 TMP ⭐

    [Header("资产引用")]
    [Tooltip("用于格挡 UI 的 Sprite 资产（蓝色盾牌）。")]
    public Sprite blockSprite; 

    private CharacterBase _targetCharacter; 

    /// <summary>
    /// 绑定 UI 视图到角色数据，并订阅生命值和格挡变化事件。
    /// </summary>
    public void Initialize(CharacterBase character)
    {
        if (character == null) return; 
        
        _targetCharacter = character;
        Debug.Log($"UI Display bound to character: {character.characterName}");

        // 1. 首次初始化 HP
        if (hpSlider != null)
        {
            hpSlider.maxValue = character.maxHp;
            hpSlider.value = character.currentHp;
        }
        if (hpValueText != null)
        {
            hpValueText.text = $"{character.currentHp}/{character.maxHp}";
        }

        // 2. 订阅事件 (View 绑定到 Model)
        character.OnHealthChanged += UpdateHealthBar;
        character.OnBlockChanged += RefreshBlockDisplay;
        
        // 首次初始化格挡显示
        RefreshBlockDisplay(); 

        // 3. 销毁监听器
        DestroyListener listener = character.gameObject.AddComponent<DestroyListener>();
        listener.onDestroy += OnTargetDestroyed;

        // 绑定名称显示
        if (nameText != null)
        {
            nameText.text = character.characterName;
        }
    }

    /// <summary>
    /// 事件触发时调用的函数：更新血条。
    /// </summary>
    private void UpdateHealthBar(int currentHp, int maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHp; 
        }
        if (hpValueText != null)
        {
            hpValueText.text = $"{currentHp}/{maxHp}";
        }
    }
    
    /// <summary>
    /// 刷新格挡 UI 的显示（包含图标和数值）。
    /// </summary>
    public void RefreshBlockDisplay()
    {
        if (_targetCharacter == null) return;
        
        int currentBlock = _targetCharacter.CurrentBlock;
        
        if (currentBlock > 0)
        {
            // 有格挡时：显示根对象，并设置图标和数值
            if (blockDisplayRoot != null)
            {
                blockDisplayRoot.SetActive(true);
            }
            
            // ⭐ 修正：使用 TextMeshProUGUI.text 属性 ⭐
            if (blockValueText != null)
            {
                blockValueText.text = currentBlock.ToString();
            }
            
            // 设置图标 
            if (blockIconImage != null && blockSprite != null)
            {
                blockIconImage.sprite = blockSprite;
                blockIconImage.gameObject.SetActive(true); 
            }
        }
        else
        {
            // 无格挡时：隐藏根对象
            if (blockDisplayRoot != null)
            {
                blockDisplayRoot.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 目标角色游戏对象销毁时调用，用于取消订阅。
    /// </summary>
    private void OnTargetDestroyed()
    {
        if (_targetCharacter != null)
        {
            _targetCharacter.OnHealthChanged -= UpdateHealthBar;
            _targetCharacter.OnBlockChanged -= RefreshBlockDisplay;
            Debug.Log($"Successfully unsubscribed {_targetCharacter.characterName}'s events.");
        }
    }

    // 推荐：如果 CharacterUIDisplay 对象本身被销毁，也要取消订阅
    private void OnDestroy()
    {
        if (_targetCharacter != null)
        {
             _targetCharacter.OnHealthChanged -= UpdateHealthBar;
             _targetCharacter.OnBlockChanged -= RefreshBlockDisplay;
        }
    }
}