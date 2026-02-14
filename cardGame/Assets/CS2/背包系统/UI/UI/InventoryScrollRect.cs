
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScavengingGame
{
    /// <summary>
    /// 自定义ScrollRect，忽略特定对象的拖拽
    /// </summary>
    public class InventoryScrollRect : ScrollRect
    {
        [Header("拖拽设置")]
        public bool ignoreDragFromSlots = true;  // 忽略物品格子的拖拽
        public float dragThreshold = 10f;        // 拖拽阈值
        
        private bool _isPotentialDrag = false;
        private Vector2 _dragStartPosition;
        private GameObject _dragStartObject;
        
        /// <summary>
        /// 重写拖拽开始，检查是否应该处理
        /// </summary>
        public override void OnBeginDrag(PointerEventData eventData)
        {
            _dragStartObject = eventData.pointerCurrentRaycast.gameObject;
            
            // 检查是否是从物品格子开始的拖拽
            if (ignoreDragFromSlots && IsItemSlot(_dragStartObject))
            {
                // 如果是物品格子，不处理拖拽
                return;
            }
            
            // 调用基类方法
            base.OnBeginDrag(eventData);
        }
        
        /// <summary>
        /// 重写拖拽过程
        /// </summary>
        public override void OnDrag(PointerEventData eventData)
        {
            // 检查是否是从物品格子开始的拖拽
            if (ignoreDragFromSlots && IsItemSlot(_dragStartObject))
            {
                // 如果是物品格子，不处理拖拽
                return;
            }
            
            // 调用基类方法
            base.OnDrag(eventData);
        }
        
        /// <summary>
        /// 重写拖拽结束
        /// </summary>
        public override void OnEndDrag(PointerEventData eventData)
        {
            // 检查是否是从物品格子开始的拖拽
            if (ignoreDragFromSlots && IsItemSlot(_dragStartObject))
            {
                // 如果是物品格子，不处理拖拽
                _dragStartObject = null;
                return;
            }
            
            // 调用基类方法
            base.OnEndDrag(eventData);
            _dragStartObject = null;
        }
        
        /// <summary>
        /// 检查是否是物品格子
        /// </summary>
        private bool IsItemSlot(GameObject obj)
        {
            if (obj == null) return false;
            
            // 检查对象本身或其父级是否有ItemSlotUI组件
            var slotUI = obj.GetComponent<ItemSlotUI>();
            if (slotUI != null) return true;
            
            // 检查父级
            var parentSlotUI = obj.GetComponentInParent<ItemSlotUI>();
            return parentSlotUI != null;
        }
        
        /// <summary>
        /// 在Update中检测潜在的拖拽
        /// </summary>
        void Update()
        {
            // 检测鼠标按下
            if (Input.GetMouseButtonDown(0))
            {
                _isPotentialDrag = true;
                _dragStartPosition = Input.mousePosition;
                
                // 检测点击的对象
                if (EventSystem.current != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current)
                    {
                        position = Input.mousePosition
                    };
                    
                    var results = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);
                    
                    if (results.Count > 0)
                    {
                        _dragStartObject = results[0].gameObject;
                    }
                }
            }
            
            // 检测鼠标移动
            if (_isPotentialDrag && Input.GetMouseButton(0))
            {
                float distance = Vector2.Distance(_dragStartPosition, Input.mousePosition);
                
                // 如果移动距离超过阈值，开始拖拽
                if (distance >= dragThreshold)
                {
                    _isPotentialDrag = false;
                    
                    // 如果是从物品格子开始的拖拽，不触发滚动
                    if (ignoreDragFromSlots && IsItemSlot(_dragStartObject))
                    {
                        return;
                    }
                }
            }
            
            // 检测鼠标抬起
            if (Input.GetMouseButtonUp(0))
            {
                _isPotentialDrag = false;
                _dragStartObject = null;
            }
        }
        
        /// <summary>
        /// 手动开始滚动（用于拖拽把手）
        /// </summary>
        public void StartManualDrag(Vector2 delta)
        {
            // 这个方法可以从拖拽把手调用
            velocity = delta * 100f;
        }
        
        /// <summary>
        /// 启用/禁用滚动
        /// </summary>
        public void SetScrollingEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                velocity = Vector2.zero;
            }
        }
    }
}
