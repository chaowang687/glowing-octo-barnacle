using UnityEngine;
using System.Collections.Generic;

namespace ScavengingGame
{
    public class InventoryEquipmentManager : MonoBehaviour
    {
        [Header("装备槽位")]
        public EquipmentSlotUI weaponSlot;
        public EquipmentSlotUI armorSlot;
        public EquipmentSlotUI amulet1Slot;
        public EquipmentSlotUI amulet2Slot;
        
        [Header("状态显示")]
        public TMPro.TextMeshProUGUI attackBonusText;
        public TMPro.TextMeshProUGUI defenseBonusText;
        
        private InventoryUIMain _mainUI;
        private IInventoryService _inventoryService;
        private Dictionary<EquipmentData.SlotType, EquipmentSlotUI> _equipmentSlots;
        private bool _isInitialized = false;
        
        public void Initialize(InventoryUIMain mainUI, IInventoryService inventoryService)
        {
            if (_isInitialized) return;
            
            _mainUI = mainUI;
            _inventoryService = inventoryService;
            
            _equipmentSlots = new Dictionary<EquipmentData.SlotType, EquipmentSlotUI>
            {
                { EquipmentData.SlotType.Weapon, weaponSlot },
                { EquipmentData.SlotType.Armor, armorSlot },
                { EquipmentData.SlotType.Amulet1, amulet1Slot },
                { EquipmentData.SlotType.Amulet2, amulet2Slot }
            };
            
            foreach (var slot in _equipmentSlots.Values)
            {
                if (slot != null)
                {
                    slot.Initialize(inventoryService);
                    slot.OnSlotClicked += OnEquipmentSlotClicked;
                    slot.OnSlotRightClicked += OnEquipmentSlotRightClicked;
                    slot.OnDropItem += OnDropItemToEquipmentSlot;
                }
            }
            
            if (_inventoryService != null)
            {
                _inventoryService.OnEquipmentChanged += OnEquipmentChanged;
                _inventoryService.OnInventoryChanged += OnInventoryChanged;
            }
            
            RefreshEquipment();
            _isInitialized = true;
        }
        
        public Dictionary<EquipmentData.SlotType, EquipmentSlotUI> GetEquipmentSlots()
        {
            return _equipmentSlots;
        }
        
        public void RefreshEquipment()
        {
            if (_inventoryService == null) return;
            
            var equippedItems = _inventoryService.GetAllEquippedItems();
            
            foreach (var slotPair in _equipmentSlots)
            {
                var slotType = slotPair.Key;
                var slotUI = slotPair.Value;
                
                if (equippedItems != null && equippedItems.TryGetValue(slotType, out var equipment))
                {
                    slotUI?.SetEquipment(equipment);
                }
                else
                {
                    slotUI?.ClearSlot();
                }
            }
            
            UpdateBonusDisplay();
        }
        
        private void UpdateBonusDisplay()
        {
            if (_inventoryService == null || attackBonusText == null || defenseBonusText == null) return;
            
            var bonuses = _inventoryService.CalculateEquipmentBonuses();
            
            attackBonusText.text = $"攻击: +{bonuses.attack}";
            defenseBonusText.text = $"防御: +{bonuses.defense}";
        }
        
        #region 事件处理
        private void OnEquipmentChanged(EquipmentData.SlotType slotType, EquipmentData equipment)
        {
            RefreshEquipment();
        }
        
        private void OnInventoryChanged()
        {
            RefreshEquipment();
        }
        
        private void OnEquipmentSlotClicked(EquipmentData.SlotType slotType)
        {
            var equipment = _inventoryService?.GetEquippedItem(slotType);
            if (equipment != null)
                _mainUI?.OnEquipmentSlotClicked(slotType);
        }
        
        private void OnEquipmentSlotRightClicked(EquipmentData.SlotType slotType)
        {
            _inventoryService?.UnequipItem(slotType);
            _mainUI?.ForceRefresh();
        }
        
        private void OnDropItemToEquipmentSlot(EquipmentData.SlotType slotType, ItemData item)
        {
            if (item is EquipmentData equipment && equipment.Slot == slotType)
            {
                _inventoryService?.EquipItem(equipment);
                _mainUI?.ForceRefresh();
            }
        }
        #endregion
        
        void OnDestroy()
        {
            if (_inventoryService != null)
            {
                _inventoryService.OnEquipmentChanged -= OnEquipmentChanged;
                _inventoryService.OnInventoryChanged -= OnInventoryChanged;
            }
        }
    }
}