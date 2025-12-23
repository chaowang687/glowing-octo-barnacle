using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq; 
using TMPro;
using System.Collections; // ⭐ 添加 System.Collections 命名空间 ⭐

public class CharacterUIDisplay : MonoBehaviour
{
    private CharacterBase character;
    
    // UI 组件引用 (例如：血条 Image 或 Slider)
    [Header("UI Components")]
    public Slider hpSlider;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpValueText;

    // ⭐ 格挡 UI 引用 ⭐
    [Header("格挡 UI 引用")]
    [Tooltip("格挡图标和数值的根 GameObject，用于整体显示/隐藏。")]
    public GameObject blockDisplayRoot; 
    [Tooltip("显示格挡图标的 Image 组件。")]
    public Image blockIconImage; 
    [Tooltip("显示格挡数值的 TextMeshProUGUI 组件")]
    public TextMeshProUGUI blockValueText;

    [Header("资产引用")]
    [Tooltip("用于格挡 UI 的 Sprite 资产（蓝色盾牌）。")]
    public Sprite blockSprite; 

    private CharacterBase _targetCharacter;
    private bool _isSubscribed = false;

    /// <summary>
    /// 绑定 UI 视图到角色数据，并订阅生命值和格挡变化事件。
    /// </summary>
    public void Initialize(CharacterBase character)
    {
        if (character == null) 
        {
            Debug.LogError("CharacterUIDisplay.Initialize: character is null");
            return;
        }
        
        // 如果已经订阅了另一个角色的事件，先取消订阅
        if (_isSubscribed && _targetCharacter != null && _targetCharacter != character)
        {
            UnsubscribeEvents();
        }
        
        this.character = character;
        _targetCharacter = character;
        
        Debug.Log($"UI Display bound to character: {character.characterName}");

        // 1. 首次初始化 HP
        if (hpSlider != null)
        {
            hpSlider.maxValue = character.maxHp;
            hpSlider.value = character.currentHp;
            Debug.Log($"血条Slider初始化: 最大值={character.maxHp}, 当前值={character.currentHp}");
        }
        if (hpValueText != null)
        {
            hpValueText.text = $"{character.currentHp}/{character.maxHp}";
        }

        // 2. 订阅事件 (View 绑定到 Model)
        SubscribeEvents();
        
        // 首次初始化格挡显示
        RefreshBlockDisplay(character.CurrentBlock);

        // 3. 绑定名称显示
        if (nameText != null)
        {
            nameText.text = character.characterName;
        }
    }
    
    /// <summary>
    /// 订阅角色事件
    /// </summary>
    private void SubscribeEvents()
    {
        if (_targetCharacter == null) return;
        
        _targetCharacter.OnHealthChanged += UpdateHealthBar;
        _targetCharacter.OnBlockChanged += RefreshBlockDisplay;
        _targetCharacter.OnCharacterDied += HandleDeath;
        
        _isSubscribed = true;
        Debug.Log($"已订阅 {_targetCharacter.characterName} 的事件");
    }
    
    /// <summary>
    /// 处理角色死亡
    /// </summary>
    private void HandleDeath()
    {
        if (character == null) return;
        
        Debug.Log($"{character.characterName} 正在处理死亡显示...");
        
        // 死亡时取消订阅事件
        UnsubscribeEvents();
        
        // 这里可以添加死亡动画，比如淡出效果
        if (this != null && gameObject != null)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }
    
    /// <summary>
    /// 淡出并销毁 UI 的协程
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        float fadeDuration = 1.0f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = 1f - (elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 销毁 UI 对象
        Destroy(gameObject);
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
    /// <param name="currentBlock">当前格挡值</param>
    public void RefreshBlockDisplay(int currentBlock)
    {
        if (currentBlock > 0)
        {
            // 有格挡时：显示根对象，并设置图标和数值
            if (blockDisplayRoot != null)
            {
                blockDisplayRoot.SetActive(true);
            }
            
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
        
        Debug.Log($"格挡UI刷新: {_targetCharacter?.characterName ?? "Unknown"} 格挡值: {currentBlock}");
    }

    /// <summary>
    /// 统一取消订阅所有事件
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (_targetCharacter != null && _isSubscribed)
        {
            _targetCharacter.OnHealthChanged -= UpdateHealthBar;
            _targetCharacter.OnBlockChanged -= RefreshBlockDisplay;
            _targetCharacter.OnCharacterDied -= HandleDeath;
            
            _isSubscribed = false;
            Debug.Log($"Successfully unsubscribed {_targetCharacter.characterName}'s events.");
        }
    }

    /// <summary>
    /// 无参版本的刷新格挡UI（用于直接调用）
    /// </summary>
    public void RefreshBlockDisplay()
    {
        if (_targetCharacter != null)
        {
            RefreshBlockDisplay(_targetCharacter.CurrentBlock);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}