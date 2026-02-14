using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace ScavengingGame
{
    public class InventoryInputHandler : MonoBehaviour
    {
        [Header("快捷键设置")]
        public KeyCode toggleInventoryKey = KeyCode.I;
        public KeyCode closeInventoryKey = KeyCode.Escape;
        public KeyCode quickUseKey = KeyCode.U;
        public KeyCode quickDropKey = KeyCode.Q;
        
        [Header("鼠标操作")]
        public bool enableMouseWheelScrolling = true;
        public float scrollSpeed = 100f;
        public bool enableRightClickUse = true;
        public bool enableDoubleClick = true;
        public float doubleClickTime = 0.3f;
        
        [Header("拖拽设置")]
        public bool enableDragAndDrop = true;
        
        private InventoryUIMain _mainUI;
        private InventorySlotManager _slotManager;
        private InventoryDragHandler _dragHandler;
        private IInventoryService _inventoryService;
        
        private float _lastClickTime = 0f;
        private int _lastClickedSlot = -1;
        
        public void Initialize(InventoryUIMain mainUI)
        {
            _mainUI = mainUI;
            _slotManager = mainUI?.slotManager;
            _dragHandler = mainUI?.GetComponent<InventoryDragHandler>();
            _inventoryService = mainUI?.GetInventoryService();
        }
        
        void Update()
        {
            if (_mainUI == null) return;
            
            HandleGlobalShortcuts();
            
            if (_mainUI.gameObject.activeSelf)
            {
                HandleInventoryInput();
            }
        }
        
        #region 全局快捷键
        private void HandleGlobalShortcuts()
        {
            if (Input.GetKeyDown(toggleInventoryKey))
            {
                _mainUI.ToggleInventory();
            }
            
            if (Input.GetKeyDown(quickUseKey))
            {
                TryQuickUseItem();
            }
            
            if (Input.GetKeyDown(quickDropKey))
            {
                TryQuickDropItem();
            }
        }
        
        private void TryQuickUseItem()
        {
            if (_inventoryService == null) return;
            
            var items = _inventoryService.GetAllItems();
            foreach (var itemStack in items)
            {
                if (!(itemStack.Item is EquipmentData))
                {
                    _inventoryService.UseItem(itemStack.Item);
                    break;
                }
            }
        }
        
        private void TryQuickDropItem()
        {
            if (_inventoryService == null || _lastClickedSlot == -1) return;
            
            var slot = _slotManager?.GetSlot(_lastClickedSlot);
            if (slot != null && slot.CurrentItem != null)
            {
                _inventoryService.RemoveItem(slot.CurrentItem.ItemName, 1);
                Debug.Log("快速丢弃物品: " + slot.CurrentItem.ItemName);
            }
        }
        #endregion
        
        #region 库存内输入处理
        private void HandleInventoryInput()
        {
            if (Input.GetKeyDown(closeInventoryKey))
            {
                _mainUI.CloseInventory();
            }
            
            if (enableMouseWheelScrolling)
            {
                HandleMouseWheelScrolling();
            }
            
            if (enableRightClickUse && Input.GetMouseButtonDown(1))
            {
                int slotIndex = GetSlotUnderMouse();
                if (slotIndex >= 0)
                {
                    HandleSlotClick(slotIndex, PointerEventData.InputButton.Right);
                }
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                int slotIndex = GetSlotUnderMouse();
                if (slotIndex >= 0)
                {
                    HandleSlotClick(slotIndex, PointerEventData.InputButton.Left);
                }
            }
            
            HandleNumberKeys();
            HandleArrowKeys();
        }
        
        private void HandleMouseWheelScrolling()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Debug.Log($"鼠标滚轮: {scroll}");
            }
        }
        
        private void HandleSlotClick(int slotIndex, PointerEventData.InputButton button)
        {
            if (_slotManager == null) return;
            
            var slot = _slotManager.GetSlot(slotIndex);
            if (slot == null) return;
            
            if (button == PointerEventData.InputButton.Left && enableDoubleClick)
            {
                float currentTime = Time.time;
                if (currentTime - _lastClickTime < doubleClickTime && 
                    _lastClickedSlot == slotIndex)
                {
                    OnSlotDoubleClicked(slotIndex, slot.CurrentItem);
                    _lastClickTime = 0f;
                    return;
                }
                
                _lastClickTime = currentTime;
                _lastClickedSlot = slotIndex;
            }
            
            if (button == PointerEventData.InputButton.Left)
            {
                slot.OnPointerClick(new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left
                });
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                slot.OnPointerClick(new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Right
                });
            }
        }
        
        private void OnSlotDoubleClicked(int slotIndex, ItemData item)
        {
            if (item == null) return;
            
            Debug.Log($"双击格子 {slotIndex}: {item.ItemName}");
            
            if (_inventoryService == null) return;
            
            if (item is EquipmentData equipment)
            {
                bool isEquipped = _inventoryService.IsItemEquipped(equipment);
                if (isEquipped)
                {
                    _inventoryService.UnequipItem(equipment.Slot);
                }
                else
                {
                    _inventoryService.EquipItem(equipment);
                }
                _mainUI?.ForceRefresh();
            }
            else
            {
                _inventoryService.UseItem(item);
                _mainUI?.ForceRefresh();
            }
        }
        
        private int GetSlotUnderMouse()
        {
            if (EventSystem.current == null) return -1;
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (RaycastResult result in results)
            {
                ItemSlotUI slot = result.gameObject.GetComponent<ItemSlotUI>();
                if (slot != null)
                {
                    return slot.SlotIndex;
                }
            }
            
            return -1;
        }
        
        private void HandleNumberKeys()
        {
            if (_slotManager == null) return;
            
            var slots = _slotManager.GetAllSlots();
            if (slots == null) return;
            
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    int slotIndex = i;
                    if (slotIndex < slots.Count)
                    {
                        var slot = _slotManager.GetSlot(slotIndex);
                        if (slot != null)
                        {
                            _mainUI?.OnItemSlotClicked(slotIndex, slot.CurrentItem);
                        }
                    }
                }
            }
        }
        
        private void HandleArrowKeys()
        {
            if (_slotManager == null) return;
            
            var slots = _slotManager.GetAllSlots();
            if (slots == null) return;
            
            int currentIndex = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].IsSelected)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            if (currentIndex == -1 && slots.Count > 0)
            {
                currentIndex = 0;
            }
            
            bool moved = false;
            int newIndex = currentIndex;
            
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                newIndex--;
                moved = true;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                newIndex++;
                moved = true;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                newIndex -= 5;
                moved = true;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                newIndex += 5;
                moved = true;
            }
            
            if (newIndex < 0) newIndex = 0;
            if (newIndex >= slots.Count) newIndex = slots.Count - 1;
            
            if (moved && newIndex >= 0 && newIndex < slots.Count)
            {
                var slot = _slotManager.GetSlot(newIndex);
                if (slot != null)
                {
                    _mainUI?.OnItemSlotClicked(newIndex, slot.CurrentItem);
                }
            }
        }
        #endregion
        
        #region 公共方法
        public void SetShortcut(KeyCode toggleKey, KeyCode closeKey)
        {
            toggleInventoryKey = toggleKey;
            closeInventoryKey = closeKey;
        }
        
        public void EnableDragAndDrop(bool enable)
        {
            enableDragAndDrop = enable;
        }
        
        public void SetDoubleClickTime(float time)
        {
            doubleClickTime = time;
        }
        
        public int GetSelectedSlotIndex()
        {
            return _lastClickedSlot;
        }
        
        public void ClearSelection()
        {
            _lastClickedSlot = -1;
            _lastClickTime = 0f;
        }
        #endregion
        
        void OnDestroy()
        {
            ClearSelection();
        }
    }
}