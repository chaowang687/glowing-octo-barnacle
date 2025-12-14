using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ScavengingGame
{
    /// <summary>
    /// 统一的拖拽检测器，处理所有拖拽开始事件
    /// </summary>
    public class InventoryDragDetector : MonoBehaviour, 
        IBeginDragHandler, 
        IDragHandler, 
        IEndDragHandler
    {
        private InventoryDragHandler _dragHandler;
        private ItemSlotUI _itemSlot;
        
        void Start()
        {
            _dragHandler = GetComponentInParent<InventoryDragHandler>();
            _itemSlot = GetComponent<ItemSlotUI>();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragHandler != null && _itemSlot != null && !_itemSlot.IsEmpty)
            {
                // 通知拖拽处理器开始拖拽
                _dragHandler.StartDragFromSlot(_itemSlot.SlotIndex);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            // 拖拽逻辑由InventoryDragHandler在Update中处理
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragHandler != null)
            {
                // 通知拖拽处理器结束拖拽
                _dragHandler.EndDrag();
            }
        }
    }
}