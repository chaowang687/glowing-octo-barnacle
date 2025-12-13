using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScavengingGame;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI 组件")]
    public Image iconImage;
    public TextMeshProUGUI countText;
    public Button actionButton;
    public TextMeshProUGUI buttonText;
    
    [Header("颜色设置")]
    public Color normalColor = Color.white;
    public Color emptyColor = new Color(1, 1, 1, 0.3f);
    
    private ItemData _currentItem;
    private System.Action _equipmentCallback;
    private System.Action _useCallback;
    
    /// <summary>
    /// 设置物品显示
    /// </summary>
    public void SetItem(ItemData item, int count)
    {
        _currentItem = item;
        
        if (item == null)
        {
            ClearSlot();
            return;
        }
        
        // 设置图标
        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.color = normalColor;
        }
        
        // 设置数量
        if (countText != null)
        {
            bool showCount = count > 1;
            countText.gameObject.SetActive(showCount);
            if (showCount) countText.text = count.ToString();
        }
        
        // 设置按钮
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            
            if (item is EquipmentData)
            {
                // 装备
                if (buttonText != null) buttonText.text = "装备";
                actionButton.onClick.AddListener(() => _equipmentCallback?.Invoke());
            }
            else
            {
                // 消耗品
                if (buttonText != null) buttonText.text = "使用";
                actionButton.onClick.AddListener(() => _useCallback?.Invoke());
            }
            
            actionButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 清空格子
    /// </summary>
    public void ClearSlot()
    {
        _currentItem = null;
        
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = emptyColor;
        }
        
        if (countText != null)
            countText.gameObject.SetActive(false);
            
        if (actionButton != null)
            actionButton.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 设置装备回调
    /// </summary>
    public void SetEquipmentCallback(System.Action callback)
    {
        _equipmentCallback = callback;
    }
    
    /// <summary>
    /// 设置使用回调
    /// </summary>
    public void SetUseCallback(System.Action callback)
    {
        _useCallback = callback;
    }
    
    /// <summary>
    /// 获取当前物品
    /// </summary>
    public ItemData GetCurrentItem()
    {
        return _currentItem;
    }
}