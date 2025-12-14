using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace ScavengingGame
{
    public class InventorySlotManager : MonoBehaviour
    {
        [Header("设置")]
        public int gridRows = 6;
        public int gridColumns = 5;
        
        private InventoryUIMain _mainUI;
        private IInventoryService _inventoryService;
        private List<ItemSlotUI> _itemSlots = new List<ItemSlotUI>();
        private int _selectedSlotIndex = -1;
        
        public void Initialize(InventoryUIMain mainUI, Transform itemGrid, 
                               GameObject slotPrefab, int totalSlots,
                               IInventoryService inventoryService)
        {
            _mainUI = mainUI;
            _inventoryService = inventoryService;
            CreateItemSlots(itemGrid, slotPrefab, totalSlots);
            
            if (_inventoryService != null)
            {
                _inventoryService.OnInventoryChanged += OnInventoryChanged;
            }
        }
        
        private void CreateItemSlots(Transform itemGrid, GameObject slotPrefab, int totalSlots)
        {
            if (slotPrefab == null || itemGrid == null) return;
            
            foreach (Transform child in itemGrid)
                Destroy(child.gameObject);
            _itemSlots.Clear();
            
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, itemGrid);
                ItemSlotUI slot = slotObj.GetComponent<ItemSlotUI>();
                
                if (slot != null)
                {
                    slot.Initialize(i);
                    slot.OnSlotClicked += OnSlotClicked;
                    slot.OnSlotRightClicked += OnSlotRightClicked;
                    _itemSlots.Add(slot);
                }
            }
        }
        
        public void RefreshSlots()
        {
            if (_inventoryService == null) return;
            
            foreach (var slot in _itemSlots)
                slot.Clear();
            
            var itemStacks = _inventoryService.GetAllItems();
            if (itemStacks == null) return;
            
            foreach (var stack in itemStacks)
            {
                if (stack.SlotIndex >= 0 && stack.SlotIndex < _itemSlots.Count)
                {
                    _itemSlots[stack.SlotIndex].SetItem(stack.Item, stack.Count);
                    
                    if (stack.Item is EquipmentData equipment)
                    {
                        bool isEquipped = _inventoryService.IsItemEquipped(equipment);
                        _itemSlots[stack.SlotIndex].SetEquipped(isEquipped);
                    }
                }
            }
        }
        
        #region 事件处理
        private void OnSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _itemSlots.Count) return;
            
            var slot = _itemSlots[slotIndex];
            var item = slot.CurrentItem;
            
            if (item != null)
            {
                if (_selectedSlotIndex != -1 && _selectedSlotIndex < _itemSlots.Count)
                    _itemSlots[_selectedSlotIndex].SetSelected(false);
                
                slot.SetSelected(true);
                _selectedSlotIndex = slotIndex;
                
                _mainUI?.OnItemSlotClicked(slotIndex, item);
            }
        }
        
        private void OnSlotRightClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _itemSlots.Count) return;
            
            var slot = _itemSlots[slotIndex];
            var item = slot.CurrentItem;
            
            _mainUI?.OnItemSlotRightClicked(slotIndex, item);
        }
        #endregion
        
        #region 公共方法
        public void SetSelectedSlot(int slotIndex)
        {
            if (slotIndex == _selectedSlotIndex) return;
            
            if (_selectedSlotIndex != -1 && _selectedSlotIndex < _itemSlots.Count)
                _itemSlots[_selectedSlotIndex].SetSelected(false);
            
            if (slotIndex >= 0 && slotIndex < _itemSlots.Count)
            {
                _itemSlots[slotIndex].SetSelected(true);
                _selectedSlotIndex = slotIndex;
            }
            else
            {
                _selectedSlotIndex = -1;
            }
        }
        
        public void ClearSelection()
        {
            if (_selectedSlotIndex != -1 && _selectedSlotIndex < _itemSlots.Count)
                _itemSlots[_selectedSlotIndex].SetSelected(false);
            
            _selectedSlotIndex = -1;
        }
        
        public ItemSlotUI GetSlot(int index)
        {
            if (index >= 0 && index < _itemSlots.Count)
                return _itemSlots[index];
            return null;
        }
        
        public List<ItemSlotUI> GetAllSlots()
        {
            return _itemSlots;
        }
        #endregion
        
        private void OnInventoryChanged()
        {
            RefreshSlots();
        }
        
        void OnDestroy()
        {
            if (_inventoryService != null)
            {
                _inventoryService.OnInventoryChanged -= OnInventoryChanged;
            }
        }
    }
}