using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Bag
{
    /// <summary>
    /// 物品UI组件，负责处理物品的交互逻辑
    /// </summary>
    public class ItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private bool isDragging = false;
        public ItemInstance itemInstance;
        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private Image iconImage;
        private Canvas canvas;
        
        private InventoryGrid originalGrid;
        private Vector2Int originalPos;
        private Vector2 dragOffset;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
            
            // 确保CanvasGroup组件存在
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // 初始化CanvasGroup状态
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1f;
            
            // 缓存图标引用，避免重复查找
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
                
                // 初始化Icon的锚点设置为中心点，避免拉伸冲突
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                }
            }
        }

        private void Start()
        {
            UpdateVisual();
        }

        /// <summary>
        /// 获取对齐到网格的位置
        /// </summary>
        private Vector2 GetSnappedPosition(Vector2 currentPos, InventoryGrid grid, bool snapToCenter = false)
        {
            if (itemInstance == null) return currentPos;
            
            // 转换为网格坐标
            Vector2Int gridPos = grid.GetGridFromPosition(currentPos);
            
            // 确保在边界内
            gridPos.x = Mathf.Clamp(gridPos.x, 0, grid.width - itemInstance.CurrentWidth);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, grid.height - itemInstance.CurrentHeight);
            
            // 获取对齐后的位置
            Vector2 snappedPos = grid.GetPositionFromGrid(gridPos.x, gridPos.y);
            
            // 如果需要中心对齐（更友好）
            if (snapToCenter)
            {
                // 加上物品尺寸的一半偏移，让鼠标在物品中心
                snappedPos.x += (itemInstance.CurrentWidth * grid.cellSize) / 2;
                snappedPos.y -= (itemInstance.CurrentHeight * grid.cellSize) / 2;
            }
            
            return snappedPos;
        }

        /// <summary>
        /// 开始拖拽事件
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemInstance == null) return;
            
            isDragging = true;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.7f;
            transform.SetAsLastSibling();

            // 记录拖拽偏移（鼠标在物品上的位置）
            Vector2 localMousePos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localMousePos))
            {
                dragOffset = rect.anchoredPosition - localMousePos;
            }
            
            // 记录原始信息并从网格中移除占位
            originalGrid = GetGridUnderMouse(eventData);
            if (originalGrid != null)
            {
                originalPos = originalGrid.GetGridFromPosition(rect.anchoredPosition);
                originalGrid.RemoveItem(itemInstance);
                InventoryManager.Instance.RemoveFromTracker(itemInstance);
            }
        }

        /// <summary>
        /// 拖拽中事件
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (itemInstance == null) return;
            
            Vector2 mousePos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out mousePos))
            {
                return;
            }
            
            // 应用偏移
            mousePos += dragOffset;
            
            // 获取当前网格
            InventoryGrid grid = GetGridUnderMouse(eventData);
            
            if (grid != null)
            {
                // 智能吸附：当靠近网格时自动对齐
                Vector2 snappedPos = GetSnappedPosition(mousePos, grid, true);
                
                // 平滑过渡到吸附位置
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, snappedPos, 0.3f);
                
                // 更新预览
                grid.ShowPlacementPreview(itemInstance, rect.anchoredPosition);
            }
            else
            {
                // 不在网格上时直接跟随鼠标
                rect.anchoredPosition = mousePos;
                
                // 清除所有网格的预览
                if (originalGrid != null) originalGrid.ClearPreview();
                if (InventoryManager.Instance.CurrentGrid != null) 
                    InventoryManager.Instance.CurrentGrid.ClearPreview();
            }
        }

        /// <summary>
        /// 结束拖拽事件
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (itemInstance == null) return;
            
            isDragging = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            // 清除所有预览
            if (originalGrid != null) originalGrid.ClearPreview();
            if (InventoryManager.Instance.CurrentGrid != null) 
                InventoryManager.Instance.CurrentGrid.ClearPreview();

            // 检查是否拖拽到UI外（丢弃物品）
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                DropIntoWorld();
                return;
            }

            // 获取目标网格
            InventoryGrid targetGrid = GetGridUnderMouse(eventData);
            
            if (targetGrid != null)
            {
                Vector2Int targetPos = targetGrid.GetGridFromPosition(rect.anchoredPosition);
                
                // 确保在边界内
                targetPos.x = Mathf.Clamp(targetPos.x, 0, targetGrid.width - itemInstance.CurrentWidth);
                targetPos.y = Mathf.Clamp(targetPos.y, 0, targetGrid.height - itemInstance.CurrentHeight);
                
                // 尝试放置
                if (InventoryManager.Instance.TryPlace(itemInstance, targetPos.x, targetPos.y, targetGrid))
                {
                    SnapToGrid(targetGrid, targetPos);
                    return;
                }
            }

            // 放置失败，返回原位置
            if (originalGrid != null)
            {
                if (originalGrid.CanPlace(originalPos.x, originalPos.y, itemInstance.CurrentWidth, itemInstance.CurrentHeight))
                {
                    originalGrid.PlaceItem(itemInstance, originalPos.x, originalPos.y);
                    SnapToGrid(originalGrid, originalPos);
                    InventoryManager.Instance.allItemsInBag.Add(itemInstance);
                }
                else
                {
                    // 如果原位置已被占用，寻找最近的可放置位置
                    bool foundPlace = FindAndPlaceNearestPosition(originalGrid);
                    if (!foundPlace)
                    {
                        Debug.LogWarning("无法找到可放置位置，物品将被销毁");
                        Destroy(gameObject);
                    }
                }
            }
            else
            {
                // 如果物品是外部生成的且没有原始网格，销毁它
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 查找并放置到最近的可用位置
        /// </summary>
        private bool FindAndPlaceNearestPosition(InventoryGrid grid)
        {
            if (itemInstance == null) return false;
            
            // 在网格中寻找最近的可放置位置
            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    if (grid.CanPlace(x, y, itemInstance.CurrentWidth, itemInstance.CurrentHeight))
                    {
                        grid.PlaceItem(itemInstance, x, y);
                        SnapToGrid(grid, new Vector2Int(x, y));
                        InventoryManager.Instance.allItemsInBag.Add(itemInstance);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 将物品丢弃到世界中
        /// </summary>
        private void DropIntoWorld()
        {
            if (itemInstance == null || itemInstance.data == null) return;
            
            if (itemInstance.data.worldPrefab != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Instantiate(itemInstance.data.worldPrefab, hit.point + Vector3.up, Quaternion.identity);
                    
                    // 从管理器中移除物品
                    InventoryManager.Instance.RemoveFromTracker(itemInstance);
                    
                    // 销毁当前 UI
                    Destroy(gameObject);
                }
            }
            else
            {
                // 如果没有世界模型，返回原位置
                if (originalGrid != null)
                {
                    originalGrid.PlaceItem(itemInstance, originalPos.x, originalPos.y);
                    SnapToGrid(originalGrid, originalPos);
                }
            }
        }

        /// <summary>
        /// 获取鼠标下的网格
        /// </summary>
        private InventoryGrid GetGridUnderMouse(PointerEventData eventData)
        {
            // 1. 首先尝试从eventData获取
            if (eventData != null && eventData.pointerEnter != null)
            {
                return eventData.pointerEnter.GetComponentInParent<InventoryGrid>();
            }
            
            // 2. 使用射线检测获取
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;
            
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            
            foreach (RaycastResult result in raycastResults)
            {
                InventoryGrid grid = result.gameObject.GetComponentInParent<InventoryGrid>();
                if (grid != null)
                {
                    return grid;
                }
            }
            
            // 3. 最后使用全局网格作为备用
            return InventoryManager.Instance.CurrentGrid;
        }

        /// <summary>
        /// 将物品对齐到网格
        /// </summary>
        public void SnapToGrid(InventoryGrid grid, Vector2Int pos)
        {
            if (grid == null || itemInstance == null) return;
            
            rect.anchoredPosition = grid.GetPositionFromGrid(pos.x, pos.y);
        }

        /// <summary>
        /// 更新物品视觉表现
        /// </summary>
        public void UpdateVisual()
        {
            if (itemInstance == null || itemInstance.data == null) return;
            
            float cellSize = InventoryManager.Instance.CurrentGrid?.cellSize ?? 50f;
            
            // 使用CurrentWidth和CurrentHeight来计算旋转后的尺寸
            int currentWidth = itemInstance.CurrentWidth;
            int currentHeight = itemInstance.CurrentHeight;
            rect.sizeDelta = new Vector2(currentWidth * cellSize, currentHeight * cellSize);
            
            // 设置图标
            if (iconImage != null)
            {
                iconImage.sprite = itemInstance.data.icon;
                
                // 确保图标正确适应旋转后的尺寸
                // 检查图标Image组件的设置
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                
                // 设置图标旋转和大小
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null)
                {
                    // 旋转 Icon 角度
                    iconRect.localEulerAngles = itemInstance.isRotated ? new Vector3(0, 0, -90) : Vector3.zero;
                    
                    // 手动设置 Icon 的大小
                    if (itemInstance.isRotated)
                    {
                        iconRect.sizeDelta = new Vector2(currentHeight * cellSize, currentWidth * cellSize);
                    } else {
                        iconRect.sizeDelta = new Vector2(currentWidth * cellSize, currentHeight * cellSize);
                    }
                }
            }
        }

        /// <summary>
        /// 手动开始拖拽
        /// </summary>
        public void StartManualDrag()
        {
            isDragging = true;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.7f;
            originalGrid = null;
            dragOffset = Vector2.zero;
        }

        /// <summary>
        /// 初始化物品UI
        /// </summary>
        public void Initialize(ItemInstance item, float cellSize)
        {
            this.itemInstance = item;
            if (item == null) return;
            
            // 设置尺寸
            rect.sizeDelta = new Vector2(item.CurrentWidth * cellSize, item.CurrentHeight * cellSize);
            
            // 初始化Icon的旋转和大小
            if (iconImage != null)
            {
                iconImage.sprite = item.data.icon;
                
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null)
                {
                    // 旋转 Icon 角度
                    iconRect.localEulerAngles = item.isRotated ? new Vector3(0, 0, -90) : Vector3.zero;
                    
                    // 手动设置 Icon 的大小
                    if (item.isRotated)
                    {
                        iconRect.sizeDelta = new Vector2(item.CurrentHeight * cellSize, item.CurrentWidth * cellSize);
                    } else {
                        iconRect.sizeDelta = new Vector2(item.CurrentWidth * cellSize, item.CurrentHeight * cellSize);
                    }
                }
            }
        }


        
        /// <summary>
        /// 处理点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            // 添加点击效果反馈
            canvasGroup.alpha = 0.7f;
            Invoke(nameof(ResetAlpha), 0.1f);
            
            // 调用旋转方法
            RotateItem();
        }
        
        /// <summary>
        /// 重置透明度
        /// </summary>
        private void ResetAlpha()
        {
            canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// 旋转物品
        /// </summary>
        public void RotateItem() { 
            // 1. 切换旋转状态 
            if (itemInstance == null) return;
            itemInstance.isRotated = !itemInstance.isRotated; 
        
            // 2. 交换父物体(ItemUI)的宽高 
            float cellSize = InventoryManager.Instance.CurrentGrid?.cellSize ?? 50f;
            float newWidth = itemInstance.CurrentWidth * cellSize; 
            float newHeight = itemInstance.CurrentHeight * cellSize; 
            rect.sizeDelta = new Vector2(newWidth, newHeight); 
        
            // 3. 处理 Icon (子物体) 
            if (iconImage != null) { 
                RectTransform iconRect = iconImage.rectTransform; 
                
                // 【关键】旋转 Icon 角度 
                iconRect.localEulerAngles = itemInstance.isRotated ? new Vector3(0, 0, -90) : Vector3.zero; 
        
                // 【核心修复】旋转后，手动设置 Icon 的大小 
                // 如果不旋转，Icon 大小 = 正常宽高 
                // 如果旋转了，Icon 的宽度要设为父物体的高度，Icon 的高度要设为父物体的宽度 
                if (itemInstance.isRotated) { 
                    iconRect.sizeDelta = new Vector2(newHeight, newWidth); 
                } else { 
                    iconRect.sizeDelta = new Vector2(newWidth, newHeight); 
                } 
            } 
        }
    }
}