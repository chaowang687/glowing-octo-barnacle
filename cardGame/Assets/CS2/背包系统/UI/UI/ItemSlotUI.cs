using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace ScavengingGame
{
    /// <summary>
    /// 背包物品格子UI
    /// </summary>
    public class ItemSlotUI : MonoBehaviour, 
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("UI 组件")]
        public Image slotBackground;
        public Image iconImage;
        public TextMeshProUGUI countText;
        public TextMeshProUGUI slotNumberText;
        public GameObject equippedIndicator;
        
        [Header("颜色设置")]
        public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        public Color highlightColor = new Color(0.5f, 0.8f, 1f, 0.3f);
        public Color selectedColor = new Color(0, 0.5f, 1f, 0.5f);
        public Color equippedColor = new Color(0, 1f, 0, 0.3f);
        
        private int _slotIndex = -1;
        private ItemData _currentItem;
        private int _itemCount = 0;
        private bool _isEquipped = false;
        private bool _isHighlighted = false;
        private bool _isSelected = false;
        
        // 事件委托
        public event Action<int> OnSlotClicked;
        public event Action<int> OnSlotRightClicked;
        public event Action<int> OnPointerEnterSlot;
        public event Action<int> OnPointerExitSlot;
        
        // 属性
        public int SlotIndex => _slotIndex;
        public ItemData CurrentItem => _currentItem;
        public int ItemCount => _itemCount;
        public bool IsEquipped => _isEquipped;
        public bool IsEmpty => _currentItem == null;
        public bool IsSelected => _isSelected;
        
        /// <summary>
        /// 初始化格子
        /// </summary>
        public void Initialize(int index)
        {
            _slotIndex = index;
            Clear();
            
            if (slotNumberText != null)
            {
                slotNumberText.text = (index + 1).ToString();
            }
            
            UpdateVisualState();
        }
        
        public void StartDrag()
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
            
            if (countText != null)
            {
                countText.enabled = false;
            }
        }

        /// <summary>
        /// 结束拖拽（供外部调用）
        /// </summary>
        public void EndDrag(bool success)
        {
            if (!success)
            {
                // 拖拽失败，恢复显示
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                }
                
                if (countText != null)
                {
                    countText.enabled = true;
                }
            }
        }
        
        /// <summary>
        /// 设置物品
        /// </summary>
        public void SetItem(ItemData item, int count)
        {
            _currentItem = item;
            _itemCount = count;
            
            if (item != null)
            {
                if (iconImage != null)
                {
                    // 修复：使用 item.icon 而不是 item.Icon
                    iconImage.sprite = item.icon;
                    iconImage.color = Color.white;
                    iconImage.enabled = true;
                }
                
                if (countText != null)
                {
                   // === 核心逻辑：数字显隐只由数据决定 ===
                    bool shouldShow = count > 1;
                    countText.gameObject.SetActive(shouldShow); // 控制GameObject激活
                    if (shouldShow)
                    {
                        countText.text = count.ToString();
                        countText.alpha = 1f; // 确保完全显示
                    }
                }
            }
            else
            {
                Clear();
            }
            
            UpdateVisualState();
        }
        
        /// <summary>
        /// 设置装备状态
        /// </summary>
        public void SetEquipped(bool equipped)
        {
            _isEquipped = equipped;
            if (equippedIndicator != null)
            {
                equippedIndicator.SetActive(equipped);
            }
            UpdateVisualState();
        }
        
        /// <summary>
        /// 清空格子
        /// </summary>
        public void Clear()
        {
            _currentItem = null;
            _itemCount = 0;
            _isEquipped = false;
            
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            
            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
            
            if (equippedIndicator != null)
            {
                equippedIndicator.SetActive(false);
            }
            
            UpdateVisualState();
        }
        
        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisualState();
        }
        
        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
            UpdateVisualState();
        }
        
        /// <summary>
        /// 显示/隐藏物品
        /// </summary>
        public void ShowItem(bool show)
        {
            if (iconImage != null)
            {
                iconImage.enabled = show;
            }
            
            if (countText != null)
            {
                countText.enabled = show;
            }
        }
        
        /// <summary>
        /// 获取物品信息文本
        /// </summary>
        public string GetItemInfoText()
        {
            if (_currentItem == null) return "空";
            
            // 使用 itemName 而不是 ItemName
            string info = $"<b>{_currentItem.itemName}</b>\n";
            info += $"{_currentItem.description}\n";
            
            if (_currentItem is EquipmentData equipment)
            {
                // 修复：使用正确的字段名 - slotType 或 slot（我们提供了兼容性）
                EquipmentData.SlotType slotType = equipment.slotType;
                
                info += $"\n<color=#FF9900>装备部位: {EquipmentData.GetSlotDisplayName(slotType)}</color>\n";
                info += $"攻击: +{equipment.attackBonus}\n";
                info += $"防御: +{equipment.defenseBonus}\n";
                
                if (_isEquipped)
                {
                    info += "<color=#00FF00>✓ 已装备</color>";
                }
            }
            else
            {
                info += $"\n<color=#FF9900>类型: 消耗品</color>\n";
                info += $"数量: {_itemCount}";
            }
            
            return info;
        }
        
        /// <summary>
        /// 更新视觉状态
        /// </summary>
        private void UpdateVisualState()
        {
            if (slotBackground == null) return;
            
            if (_isSelected)
            {
                slotBackground.color = selectedColor;
            }
            else if (_isHighlighted)
            {
                slotBackground.color = highlightColor;
            }
            else if (_isEquipped)
            {
                slotBackground.color = equippedColor;
            }
            else if (_currentItem == null)
            {
                slotBackground.color = normalColor;
            }
            else
            {
                slotBackground.color = Color.clear;
            }
        }
        
        // ====================================================================
        // 鼠标事件
        // ====================================================================
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnSlotClicked?.Invoke(_slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnSlotRightClicked?.Invoke(_slotIndex);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHighlighted = true;
            UpdateVisualState();
            OnPointerEnterSlot?.Invoke(_slotIndex);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHighlighted = false;
            UpdateVisualState();
            OnPointerExitSlot?.Invoke(_slotIndex);
        }
    }
}