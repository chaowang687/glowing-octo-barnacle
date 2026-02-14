using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ScavengingGame
{
    public class InventoryInfoManager : MonoBehaviour
    {
        [Header("信息面板")]
        public GameObject itemInfoPanel;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemDescriptionText;
        public TextMeshProUGUI itemStatsText;
        public TextMeshProUGUI itemQuantityText;
        public Image itemIcon;
        
        [Header("按钮")]
        public Button useButton;
        public Button equipButton;
        public Button dropButton;
        public Button closeButton;
        
        [Header("装备状态显示")]
        public GameObject equippedIndicator;
        public TextMeshProUGUI equippedText;
        
        private ItemData _currentItem;
        private IInventoryService _inventoryService;
        private bool _isInitialized = false;
        
        /// <summary>
        /// 初始化信息管理器
        /// </summary>
        public void Initialize(IInventoryService inventoryService)
        {
            if (_isInitialized) return;
            
            _inventoryService = inventoryService;
            
            // 设置按钮事件
            if (useButton != null)
                useButton.onClick.AddListener(OnUseButtonClick);
            
            if (equipButton != null)
                equipButton.onClick.AddListener(OnEquipButtonClick);
            
            if (dropButton != null)
                dropButton.onClick.AddListener(OnDropButtonClick);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(HideItemInfo);
            
            // 初始隐藏
            HideItemInfo();
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 显示物品信息
        /// </summary>
        public void ShowItemInfo(ItemData item)
        {
            if (item == null || itemInfoPanel == null) return;
            
            _currentItem = item;
            
            // 更新UI元素
            UpdateItemInfoUI();
            
            // 显示面板
            itemInfoPanel.SetActive(true);
            
            // 根据物品类型更新按钮状态
            UpdateButtonStates();
        }
        
        /// <summary>
        /// 更新物品信息UI
        /// </summary>
        private void UpdateItemInfoUI()
        {
            // 基本信息
            if (itemNameText != null)
                itemNameText.text = _currentItem.ItemName;
            
            if (itemDescriptionText != null)
                itemDescriptionText.text = _currentItem.Description;
            
            if (itemIcon != null)
            {
                itemIcon.sprite = _currentItem.Icon;
                itemIcon.color = Color.white;
            }
            
            // 数量信息
            if (_inventoryService != null && itemQuantityText != null)
            {
                int count = _inventoryService.GetItemCount(_currentItem);
                itemQuantityText.text = $"数量: {count}";
                itemQuantityText.gameObject.SetActive(count > 1);
            }
            
            // 装备信息（如果是装备）
            if (_currentItem is EquipmentData equipment)
            {
                UpdateEquipmentInfo(equipment);
            }
            else
            {
                // 消耗品信息
                if (itemStatsText != null)
                {
                    itemStatsText.text = "<color=#FF9900>类型: 消耗品</color>\n" +
                                        $"<color=#AAAAAA>右键点击使用</color>";
                }
                
                // 隐藏装备相关UI
                if (equippedIndicator != null)
                    equippedIndicator.SetActive(false);
                
                if (equippedText != null)
                    equippedText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 更新装备信息
        /// </summary>
        private void UpdateEquipmentInfo(EquipmentData equipment)
        {
            if (itemStatsText != null)
            {
                string slotName = GetSlotDisplayName(equipment.Slot);
                string statsText = $"<color=#FF9900>装备部位: {slotName}</color>\n" +
                                  $"攻击: +{equipment.AttackBonus}\n" +
                                  $"防御: +{equipment.DefenseBonus}\n" +
                                  $"<color=#AAAAAA>右键点击装备/卸下</color>";
                
                itemStatsText.text = statsText;
            }
            
            // 检查是否已装备
            if (_inventoryService != null && equippedIndicator != null && equippedText != null)
            {
                bool isEquipped = _inventoryService.IsItemEquipped(equipment);
                equippedIndicator.SetActive(isEquipped);
                equippedText.gameObject.SetActive(isEquipped);
                
                if (isEquipped)
                {
                    equippedText.text = "已装备";
                    equippedText.color = Color.green;
                }
            }
        }
        
        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            if (_currentItem == null) return;
            
            bool isEquipment = _currentItem is EquipmentData;
            
            // 使用按钮：只对消耗品显示
            if (useButton != null)
            {
                useButton.gameObject.SetActive(!isEquipment);
                useButton.interactable = !isEquipment;
            }
            
            // 装备按钮：只对装备显示
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(isEquipment);
                if (isEquipment)
                {
                    var equipment = _currentItem as EquipmentData;
                    bool isEquipped = _inventoryService?.IsItemEquipped(equipment) ?? false;
                    
                    // 根据装备状态更新按钮文本
                    TextMeshProUGUI buttonText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = isEquipped ? "卸下" : "装备";
                    }
                }
            }
            
            // 丢弃按钮：对所有物品显示
            if (dropButton != null)
            {
                dropButton.gameObject.SetActive(true);
                dropButton.interactable = true;
            }
        }
        
        /// <summary>
        /// 隐藏物品信息
        /// </summary>
        public void HideItemInfo()
        {
            if (itemInfoPanel != null)
                itemInfoPanel.SetActive(false);
            
            _currentItem = null;
        }
        
        #region 按钮事件处理
        private void OnUseButtonClick()
        {
            if (_currentItem == null || _inventoryService == null) return;
            
            Debug.Log($"使用物品: {_currentItem.ItemName}");
            _inventoryService.UseItem(_currentItem);
            
            // 隐藏面板
            HideItemInfo();
        }
        
        private void OnEquipButtonClick()
        {
            if (_currentItem == null || !(_currentItem is EquipmentData) || _inventoryService == null) return;
            
            var equipment = _currentItem as EquipmentData;
            bool isEquipped = _inventoryService.IsItemEquipped(equipment);
            
            if (isEquipped)
            {
                // 卸下装备
                Debug.Log($"卸下装备: {equipment.ItemName}");
                _inventoryService.UnequipItem(equipment.Slot);
            }
            else
            {
                // 装备物品
                Debug.Log($"装备物品: {equipment.ItemName}");
                _inventoryService.EquipItem(equipment);
            }
            
            // 更新UI
            UpdateButtonStates();
            UpdateItemInfoUI();
        }
        
        private void OnDropButtonClick()
        {
            if (_currentItem == null || _inventoryService == null) return;
            
            Debug.Log($"丢弃物品: {_currentItem.ItemName}");
            
            // 使用新的RemoveItem方法，通过物品名称移除
            _inventoryService.RemoveItem(_currentItem.ItemName, 1);
            
            // 隐藏面板
            HideItemInfo();
        }
        #endregion
        
        /// <summary>
        /// 获取槽位显示名称
        /// </summary>
        private string GetSlotDisplayName(EquipmentData.SlotType slotType)
        {
            switch (slotType)
            {
                case EquipmentData.SlotType.Weapon: return "weapon";
                case EquipmentData.SlotType.Armor: return "护甲";
                case EquipmentData.SlotType.Helmet: return "头盔";
                case EquipmentData.SlotType.Gloves: return "手套";
                case EquipmentData.SlotType.Boots: return "靴子";
                case EquipmentData.SlotType.Shield: return "盾牌";
                case EquipmentData.SlotType.Ring1: return "戒指1";
                case EquipmentData.SlotType.Ring2: return "戒指2";
                case EquipmentData.SlotType.Amulet1: return "护符1";
                case EquipmentData.SlotType.Amulet2: return "护符2";
                default: return "未知";
            }
        }
        
        /// <summary>
        /// 获取当前显示物品
        /// </summary>
        public ItemData GetCurrentItem()
        {
            return _currentItem;
        }
        
        /// <summary>
        /// 检查是否正在显示物品信息
        /// </summary>
        public bool IsShowingItemInfo()
        {
            return itemInfoPanel != null && itemInfoPanel.activeSelf;
        }
        
        /// <summary>
        /// 强制刷新当前显示的信息
        /// </summary>
        public void RefreshCurrentInfo()
        {
            if (_currentItem != null && IsShowingItemInfo())
            {
                UpdateItemInfoUI();
                UpdateButtonStates();
            }
        }
        
        void OnDestroy()
        {
            // 清理按钮事件
            if (useButton != null)
                useButton.onClick.RemoveAllListeners();
            
            if (equipButton != null)
                equipButton.onClick.RemoveAllListeners();
            
            if (dropButton != null)
                dropButton.onClick.RemoveAllListeners();
            
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
        }
    }
}