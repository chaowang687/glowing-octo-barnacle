using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

namespace ScavengingGame
{
    /// <summary>
    /// 装备槽位UI
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, 
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IDropHandler
    {
        [Header("UI 组件")]
        public EquipmentData.SlotType slotType;
        public Image slotBackground;
        public Image iconImage;
        public Image feedbackImage;
        public TextMeshProUGUI slotNameText;
        public GameObject highlightEffect;
        
        [Header("颜色设置")]
        public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public Color occupiedColor = new Color(0.4f, 0.8f, 0.4f, 0.5f);
        public Color highlightColor = new Color(0.6f, 0.6f, 1f, 0.3f);
        public Color cantDropColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        public Color dropSuccessColor = new Color(0.3f, 1f, 0.3f, 0.5f);
        
        [Header("默认图标")]
        public Sprite defaultIcon;
        
        private EquipmentData _currentEquipment;
        private IInventoryService _inventoryService;
        private bool _isHighlighted = false;
        private bool _isInitialized = false;
        
        // 事件
        public event Action<EquipmentData.SlotType> OnSlotClicked;
        public event Action<EquipmentData.SlotType> OnSlotRightClicked;
        public event Action<EquipmentData.SlotType, ItemData> OnDropItem;
        
        public EquipmentData CurrentEquipment => _currentEquipment;
        public bool IsEmpty => _currentEquipment == null;
        
        /// <summary>
        /// 初始化槽位
        /// </summary>
        public void Initialize(IInventoryService inventoryService)
        {
            if (_isInitialized) return;
            
            _inventoryService = inventoryService;
            InitializeSlot();
            _isInitialized = true;
        }
        
        /// <summary>
        /// 延迟初始化（用于编辑器测试）
        /// </summary>
        public void InitializeWithDelay()
        {
            if (_isInitialized) return;
            
            StartCoroutine(DelayedInitialize());
        }
        
        private IEnumerator DelayedInitialize()
        {
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (GameStateManager.Instance != null)
                {
                    var inventoryService = GameStateManager.Instance.PlayerInventory as IInventoryService;
                    if (inventoryService != null)
                    {
                        Initialize(inventoryService);
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.LogWarning($"EquipmentSlotUI 初始化失败: 无法获取 IInventoryService");
        }
        
        /// <summary>
        /// 初始化槽位
        /// </summary>
        private void InitializeSlot()
        {
            if (slotNameText != null)
            {
                slotNameText.text = GetSlotDisplayName();
            }
            
            if (feedbackImage != null)
            {
                feedbackImage.color = Color.clear;
            }
            
            ClearSlot();
        }
        
        void Start()
        {
            if (!_isInitialized)
            {
                InitializeWithDelay();
            }
        }
        
        /// <summary>
        /// 获取槽位显示名称
        /// </summary>
        private string GetSlotDisplayName()
        {
            switch (slotType)
            {
                case EquipmentData.SlotType.Weapon: return "Weapon";
                case EquipmentData.SlotType.Armor: return "护甲";
                case EquipmentData.SlotType.Amulet1: return "护符1";
                case EquipmentData.SlotType.Amulet2: return "护符2";
                default: return "装备槽";
            }
        }
        
        /// <summary>
        /// 设置装备
        /// </summary>
        public void SetEquipment(EquipmentData equipment)
        {
            _currentEquipment = equipment;
            
            if (equipment != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = equipment.Icon;
                    iconImage.color = Color.white;
                }
                
                if (slotBackground != null)
                {
                    slotBackground.color = occupiedColor;
                }
                
                UpdateSlotTooltip();
            }
            else
            {
                ClearSlot();
            }
        }
        
        /// <summary>
        /// 清空槽位
        /// </summary>
        public void ClearSlot()
        {
            _currentEquipment = null;
            
            if (iconImage != null)
            {
                iconImage.sprite = defaultIcon;
                iconImage.color = new Color(1, 1, 1, 0.3f);
            }
            
            if (slotBackground != null)
            {
                slotBackground.color = emptyColor;
            }
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }
        
        /// <summary>
        /// 尝试装备物品
        /// </summary>
        public bool TryEquipItem(ItemData item)
        {
            if (!_isInitialized || _inventoryService == null)
            {
                Debug.LogWarning("EquipmentSlotUI 未初始化或缺少库存服务");
                return false;
            }
            
            if (item is EquipmentData equipment && equipment.Slot == slotType)
            {
                bool success = _inventoryService.EquipItem(equipment);
                ShowFeedback(success ? dropSuccessColor : cantDropColor);
                return success;
            }
            
            ShowFeedback(cantDropColor);
            return false;
        }
        
        /// <summary>
        /// 简单版本的装备尝试（如果不需要返回值）
        /// </summary>
        public void TryEquipItemSimple(ItemData item)
        {
            if (item is EquipmentData equipment && equipment.Slot == slotType)
            {
                if (_inventoryService != null)
                {
                    try
                    {
                        _inventoryService.EquipItem(equipment);
                        ShowFeedback(dropSuccessColor);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"装备物品时发生错误: {e.Message}");
                        ShowFeedback(cantDropColor);
                    }
                }
            }
            else
            {
                ShowFeedback(cantDropColor);
            }
        }
        
        /// <summary>
        /// 卸下装备
        /// </summary>
        public void UnequipItem()
        {
            if (_currentEquipment == null || _inventoryService == null) return;
            
            _inventoryService.UnequipItem(slotType);
            ShowFeedback(Color.yellow);
        }
        
        /// <summary>
        /// 获取槽位信息文本
        /// </summary>
        public string GetSlotInfoText()
        {
            if (_currentEquipment != null)
            {
                return $"<b>{_currentEquipment.ItemName}</b>\n" +
                       $"装备部位: {GetSlotDisplayName()}\n" +
                       $"攻击: +{_currentEquipment.AttackBonus}\n" +
                       $"防御: +{_currentEquipment.DefenseBonus}\n" +
                       $"<color=#FF9900>右键点击卸下</color>";
            }
            else
            {
                return $"<b>{GetSlotDisplayName()}</b>\n" +
                       $"<color=#AAAAAA>空槽位</color>\n" +
                       $"<color=#FF9900>拖拽装备到此槽位或右键点击空位</color>";
            }
        }
        
        /// <summary>
        /// 更新槽位提示
        /// </summary>
        private void UpdateSlotTooltip()
        {
        }
        
        /// <summary>
        /// 显示反馈效果
        /// </summary>
        public void ShowFeedback(Color color, float duration = 0.5f)
        {
            if (feedbackImage == null) return;
            
            StopAllCoroutines();
            StartCoroutine(FeedbackCoroutine(color, duration));
        }
        
        /// <summary>
        /// 隐藏反馈效果
        /// </summary>
        public void HideFeedback()
        {
            if (feedbackImage == null) return;
            
            StopAllCoroutines();
            feedbackImage.color = Color.clear;
        }
        
        private IEnumerator FeedbackCoroutine(Color color, float duration)
        {
            if (feedbackImage == null) yield break;
            
            float elapsed = 0f;
            Color startColor = feedbackImage.color;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                feedbackImage.color = Color.Lerp(startColor, color, t);
                yield return null;
            }
            
            elapsed = 0f;
            startColor = feedbackImage.color;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                feedbackImage.color = Color.Lerp(startColor, Color.clear, t);
                yield return null;
            }
            
            feedbackImage.color = Color.clear;
        }
        
        // ====================================================================
        // 鼠标事件
        // ====================================================================
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnSlotClicked?.Invoke(slotType);
                
                if (_currentEquipment == null)
                {
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnSlotRightClicked?.Invoke(slotType);
                
                if (_currentEquipment != null)
                {
                    UnequipItem();
                }
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHighlighted = true;
            
            if (slotBackground != null)
            {
                slotBackground.color = highlightColor;
            }
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(true);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHighlighted = false;
            
            if (_currentEquipment != null && slotBackground != null)
            {
                slotBackground.color = occupiedColor;
            }
            else if (slotBackground != null)
            {
                slotBackground.color = emptyColor;
            }
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }
        
        // ====================================================================
        // 拖放事件
        // ====================================================================
        
        public void OnDrop(PointerEventData eventData)
        {
            ItemSlotUI draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
            if (draggedSlot == null || draggedSlot.CurrentItem == null) return;
            
            OnDropItem?.Invoke(slotType, draggedSlot.CurrentItem);
            
            bool success = TryEquipItem(draggedSlot.CurrentItem);
            
            if (success)
            {
            }
        }
        
        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted);
            }
            
            if (highlighted && slotBackground != null)
            {
                slotBackground.color = highlightColor;
            }
            else if (slotBackground != null)
            {
                if (_currentEquipment != null)
                {
                    slotBackground.color = occupiedColor;
                }
                else
                {
                    slotBackground.color = emptyColor;
                }
            }
        }
        
        /// <summary>
        /// 检查是否接受某装备
        /// </summary>
        public bool CanAcceptEquipment(EquipmentData equipment)
        {
            return equipment != null && equipment.Slot == slotType;
        }
        
        /// <summary>
        /// 显示接受拖拽的提示
        /// </summary>
        public void ShowDropHint(bool canDrop)
        {
            if (slotBackground != null)
            {
                slotBackground.color = canDrop ? highlightColor : cantDropColor;
            }
        }
        
        /// <summary>
        /// 隐藏拖拽提示
        /// </summary>
        public void HideDropHint()
        {
            if (_currentEquipment != null && slotBackground != null)
            {
                slotBackground.color = occupiedColor;
            }
            else if (slotBackground != null)
            {
                slotBackground.color = emptyColor;
            }
        }
    }
}