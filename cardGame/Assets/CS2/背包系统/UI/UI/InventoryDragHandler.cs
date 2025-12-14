
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace ScavengingGame
{
    public class InventoryDragHandler : MonoBehaviour
    {
        [Header("拖拽系统")]
        public Canvas dragCanvas;
        public Image dragImagePrefab;
        public float dragAlpha = 0.8f;
        
        [Header("拖拽设置")]
        public Color validDropColor = new Color(0.3f, 1f, 0.3f, 0.5f);
        public Color invalidDropColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        public float dropFeedbackDuration = 0.5f;
        
        [Header("拖拽阈值")]
        public float dragStartThreshold = 5f;
        private bool _isDragging = false;
        private bool _isPotentialDrag = false;
        private Vector2 _dragStartPosition;
        private int _potentialDragSlot = -1;
        
        private InventoryUIMain _mainUI;
        private IInventoryService _inventoryService;
        private Image _dragImage;
        private int _dragSourceIndex = -1;
        private ItemData _dragItem;
        private int _dragItemCount;
        private ItemSlotUI _dragSourceSlot;
        
        private List<ItemSlotUI> _itemSlots;
        private Dictionary<EquipmentData.SlotType, EquipmentSlotUI> _equipmentSlots;
        
        // 新增：用于跟踪拖拽目标
        private ItemSlotUI _currentDropTargetSlot;
        private EquipmentSlotUI _currentDropTargetEquipmentSlot;
        
        public void Initialize(InventoryUIMain mainUI, IInventoryService inventoryService)
        {
            _mainUI = mainUI;
            _inventoryService = inventoryService;
            
            if (dragCanvas != null && dragImagePrefab != null)
            {
                _dragImage = Instantiate(dragImagePrefab, dragCanvas.transform);
                _dragImage.gameObject.SetActive(false);
                _dragImage.raycastTarget = false; // 重要：避免遮挡射线检测
            }
            
            if (mainUI.slotManager != null)
            {
                _itemSlots = mainUI.slotManager.GetAllSlots();
            }
            
            if (mainUI.equipmentManager != null)
            {
                _equipmentSlots = mainUI.equipmentManager.GetEquipmentSlots();
            }
        }
        
        void Update()
        {
            HandlePotentialDrag();
            
            if (_isDragging)
            {
                UpdateDragPosition();
                UpdateDropTarget();
                
                // 新增：检测左键释放
                if (Input.GetMouseButtonUp(0))
                {
                    EndDrag();
                }
            }
        }
        
        #region 拖拽开始检测
        private void HandlePotentialDrag()
        {
            if (_isDragging) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                _dragStartPosition = Input.mousePosition;
                _potentialDragSlot = GetSlotUnderMouse();
                _isPotentialDrag = _potentialDragSlot >= 0;
            }
            
            if (_isPotentialDrag && Input.GetMouseButton(0))
            {
                Vector2 currentPos = Input.mousePosition;
                float distance = Vector2.Distance(_dragStartPosition, currentPos);
                
                if (distance >= dragStartThreshold)
                {
                    StartDragFromSlot(_potentialDragSlot);
                    _isPotentialDrag = false;
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                _isPotentialDrag = false;
            }
        }
        
        public void StartDragFromSlot(int slotIndex)
        {
            if (_isDragging || slotIndex < 0 || slotIndex >= _itemSlots.Count) return;
            
            var slot = _itemSlots[slotIndex];
            if (slot.CurrentItem == null || _dragImage == null) return;
            
            _isDragging = true;
            _dragSourceIndex = slotIndex;
            _dragSourceSlot = slot;
            _dragItem = slot.CurrentItem;
            _dragItemCount = slot.ItemCount;
            
            // 设置拖拽图像
            _dragImage.sprite = _dragItem.Icon;
            _dragImage.color = new Color(1, 1, 1, dragAlpha);
            _dragImage.gameObject.SetActive(true);
            _dragImage.rectTransform.sizeDelta = new Vector2(50, 50);
            
            // 隐藏原格子图标，但保持格子可见
            slot.iconImage.enabled = false;
            slot.countText.enabled = false;
            
            // 清除当前目标
            _currentDropTargetSlot = null;
            _currentDropTargetEquipmentSlot = null;
            
            Debug.Log($"开始拖拽: 槽位{slotIndex} - {_dragItem.ItemName}");
        }
        #endregion
        
        #region 拖拽过程处理
        private void UpdateDragPosition()
        {
            if (dragCanvas == null || _dragImage == null) return;
            
            // 将屏幕坐标转换为Canvas本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.transform as RectTransform,
                Input.mousePosition,
                dragCanvas.worldCamera,
                out Vector2 localPoint);
            
            // 设置拖拽图像位置
            _dragImage.rectTransform.anchoredPosition = localPoint;
            
            // 也可以使用世界坐标
            // _dragImage.transform.position = Input.mousePosition;
        }
        
        private void UpdateDropTarget()
        {
            if (!_isDragging) return;
            
            // 清除之前的目标高亮
            if (_currentDropTargetSlot != null)
            {
                _currentDropTargetSlot.SetHighlighted(false);
                _currentDropTargetSlot = null;
            }
            
            if (_currentDropTargetEquipmentSlot != null)
            {
                _currentDropTargetEquipmentSlot.SetHighlighted(false);
                _currentDropTargetEquipmentSlot = null;
            }
            
            // 获取当前鼠标位置下的槽位
            var dropTarget = GetDropTargetUnderMouse();
            
            if (dropTarget != null)
            {
                ItemSlotUI targetSlot = dropTarget.GetComponent<ItemSlotUI>();
                EquipmentSlotUI targetEquipSlot = dropTarget.GetComponent<EquipmentSlotUI>();
                
                if (targetSlot != null)
                {
                    _currentDropTargetSlot = targetSlot;
                    _currentDropTargetSlot.SetHighlighted(true);
                }
                else if (targetEquipSlot != null && _dragItem is EquipmentData equipment)
                {
                    if (targetEquipSlot.CanAcceptEquipment(equipment))
                    {
                        _currentDropTargetEquipmentSlot = targetEquipSlot;
                        _currentDropTargetEquipmentSlot.SetHighlighted(true);
                    }
                }
            }
        }
        
        private GameObject GetDropTargetUnderMouse()
        {
            if (EventSystem.current == null) return null;
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (RaycastResult result in results)
            {
                // 忽略拖拽图像自身
                if (result.gameObject == _dragImage?.gameObject) continue;
                
                if (result.gameObject.GetComponent<ItemSlotUI>() != null ||
                    result.gameObject.GetComponent<EquipmentSlotUI>() != null)
                {
                    return result.gameObject;
                }
            }
            
            return null;
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
        #endregion
        
        #region 拖拽结束处理
        public void EndDrag()
        {
            if (!_isDragging) return;
            
            bool success = false;
            
            // 检查是否有有效的放置目标
            if (_currentDropTargetSlot != null)
            {
                success = HandleItemSlotDrop(_currentDropTargetSlot.SlotIndex);
            }
            else if (_currentDropTargetEquipmentSlot != null && _dragItem is EquipmentData equipment)
            {
                success = HandleEquipmentSlotDrop(_currentDropTargetEquipmentSlot, equipment);
            }
            else
            {
                // 没有有效目标，返回物品
                ReturnDragItem();
                ShowDropFeedback(false);
            }
            
            if (success)
            {
                ShowDropFeedback(true);
            }
            
            CleanupDrag();
        }
        
        private bool HandleItemSlotDrop(int targetIndex)
        {
            if (_dragSourceIndex == targetIndex)
            {
                // 拖回原位置，直接返回
                ReturnDragItem();
                return true;
            }
            
            if (_inventoryService == null) 
            {
                ReturnDragItem();
                return false;
            }
            
            bool success = _inventoryService.SwapItems(_dragSourceIndex, targetIndex);
            
            if (success)
            {
                _mainUI?.ForceRefresh();
                return true;
            }
            else
            {
                ReturnDragItem();
                return false;
            }
        }
        
        private bool HandleEquipmentSlotDrop(EquipmentSlotUI equipSlot, EquipmentData equipment)
        {
            if (equipSlot.slotType != equipment.Slot)
            {
                ReturnDragItem();
                return false;
            }
            
            bool success = equipSlot.TryEquipItem(equipment);
            
            if (success)
            {
                _mainUI?.ForceRefresh();
                StartCoroutine(ShowEquipFeedbackCoroutine(true));
                return true;
            }
            else
            {
                ReturnDragItem();
                return false;
            }
        }
        
        private void ReturnDragItem()
        {
            if (_dragSourceSlot != null)
            {
                // 恢复原格子的显示
                _dragSourceSlot.iconImage.enabled = true;
                _dragSourceSlot.countText.enabled = true;
            }
        }
        
        private void CleanupDrag()
        {
            // 清除目标高亮
            if (_currentDropTargetSlot != null)
            {
                _currentDropTargetSlot.SetHighlighted(false);
            }
            
            if (_currentDropTargetEquipmentSlot != null)
            {
                _currentDropTargetEquipmentSlot.SetHighlighted(false);
            }
            
            // 重置状态
            _isDragging = false;
            _dragSourceIndex = -1;
            _dragSourceSlot = null;
            _dragItem = null;
            _dragItemCount = 0;
            _currentDropTargetSlot = null;
            _currentDropTargetEquipmentSlot = null;
            
            // 隐藏拖拽图像
            if (_dragImage != null)
            {
                _dragImage.gameObject.SetActive(false);
            }
        }
        #endregion
        
        #region 反馈效果
        private void ShowDropFeedback(bool success)
        {
            if (_mainUI == null) return;
            
            Color feedbackColor = success ? validDropColor : invalidDropColor;
            
            if (_dragImage != null)
            {
                StartCoroutine(FlashDragImageCoroutine(feedbackColor));
            }
        }
        
        private IEnumerator FlashDragImageCoroutine(Color flashColor)
        {
            if (_dragImage == null) yield break;
            
            Color originalColor = _dragImage.color;
            float elapsed = 0f;
            
            while (elapsed < dropFeedbackDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (dropFeedbackDuration / 2f);
                _dragImage.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }
            
            elapsed = 0f;
            while (elapsed < dropFeedbackDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (dropFeedbackDuration / 2f);
                _dragImage.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }
            
            _dragImage.color = originalColor;
        }
        
        private IEnumerator ShowEquipFeedbackCoroutine(bool success)
        {
            if (_equipmentSlots == null) yield break;
            
            Color feedbackColor = success ? validDropColor : invalidDropColor;
            
            foreach (var slot in _equipmentSlots.Values)
            {
                if (slot != null)
                {
                    slot.ShowFeedback(feedbackColor);
                }
            }
            
            yield return new WaitForSeconds(dropFeedbackDuration);
            
            foreach (var slot in _equipmentSlots.Values)
            {
                if (slot != null)
                {
                    slot.HideFeedback();
                }
            }
        }
        #endregion
        
        #region 公共方法
        public bool IsDragging()
        {
            return _isDragging;
        }
        
        public ItemData GetDraggedItem()
        {
            return _dragItem;
        }
        
        public void CancelDrag()
        {
            if (_isDragging)
            {
                ReturnDragItem();
                CleanupDrag();
            }
        }
        #endregion
    }
}
