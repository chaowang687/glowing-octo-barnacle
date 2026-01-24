using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bag
{
    /// <summary>
    /// 背包网格单元格组件，负责处理单个网格的点击和拖拽事件
    /// </summary>
    public class Slot : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public int gridX;
        public int gridY;
        public InventoryGrid inventoryGrid;
        
        private bool isDragging = false;
        private ItemInstance draggedItem;
        private Vector2 dragOffset;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // 设置为透明
            Image image = GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0, 0, 0, 0.1f); // 半透明黑色，便于调试
                image.raycastTarget = true; // 确保能接收射线检测
            }
        }
        
        /// <summary>
        /// 设置Slot的网格坐标
        /// </summary>
        public void SetGridPosition(int x, int y)
        {
            gridX = x;
            gridY = y;
        }
        
        /// <summary>
        /// 处理点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 右键旋转物品
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ItemInstance item = inventoryGrid.GetItemAt(gridX, gridY);
                if (item != null)
                {
                    InventoryManager.Instance.TryRotateItem(GetItemUI(item));
                }
            }
            // 左键选中物品
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                ItemInstance item = inventoryGrid.GetItemAt(gridX, gridY);
                if (item != null)
                {
                    ItemUI itemUI = GetItemUI(item);
                    if (itemUI != null)
                    {
                        InventoryManager.Instance.SelectedItem = itemUI;
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理鼠标按下事件（开始拖拽）
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            // 获取当前位置的物品
            draggedItem = inventoryGrid.GetItemAt(gridX, gridY);
            if (draggedItem != null)
            {
                isDragging = true;
                
                // 计算物品UI的位置和设置
                ItemUI itemUI = GetItemUI(draggedItem);
                if (itemUI != null)
                {
                    // 设置物品UI的拖拽状态为true，确保TryRotateItem方法能正确识别
                    itemUI.IsDragging = true;
                    
                    // 记录拖拽偏移
                    Vector2 localMousePos;
                    // 使用InventoryGrid的RectTransform来计算本地坐标，确保与拖拽过程中使用的坐标系一致
                    RectTransform gridRect = inventoryGrid.GetComponent<RectTransform>();
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        gridRect,
                        eventData.position,
                        eventData.pressEventCamera,
                        out localMousePos))
                    {
                        RectTransform itemRect = itemUI.GetComponent<RectTransform>();
                        dragOffset = itemRect.anchoredPosition - localMousePos;
                    }
                    
                    // 设置物品UI的CanvasGroup属性，确保拖拽时能正常显示和移动
                    CanvasGroup canvasGroup = itemUI.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.blocksRaycasts = false; // 不阻止射线检测，以便能检测到其他Slot
                        canvasGroup.alpha = 0.7f; // 拖拽时半透明效果
                    }
                    
                    // 将物品UI移动到最上层，确保拖拽时不会被其他UI遮挡
                    itemUI.transform.SetAsLastSibling();
                    
                    // 拖拽开始时，更新星星高亮状态，确保星星变为灰色
                    itemUI.UpdateStarHighlight();
                }
                
                // 从网格中移除物品
                inventoryGrid.RemoveItem(draggedItem);
                InventoryManager.Instance.RemoveFromTracker(draggedItem);
            }
        }
        
        /// <summary>
        /// 处理鼠标拖拽事件（拖拽过程中）
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging && draggedItem != null)
            {
                // 获取当前鼠标位置
                Vector2 localMousePos;
                // 使用InventoryGrid的RectTransform来计算本地坐标，而不是SlotContainer的
                RectTransform gridRect = inventoryGrid.GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gridRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localMousePos))
                {
                    // 计算物品新位置
                    Vector2 newPosition = localMousePos + dragOffset;
                    
                    // 更新物品UI位置
                    ItemUI itemUI = GetItemUI(draggedItem);
                    if (itemUI != null)
                    {
                        RectTransform itemRect = itemUI.GetComponent<RectTransform>();
                        itemRect.anchoredPosition = newPosition;
                    }
                    
                    // 更新放置预览
                    inventoryGrid.ShowPlacementPreview(draggedItem, newPosition);
                }
            }
        }
        
        /// <summary>
        /// 处理鼠标抬起事件（结束拖拽）
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDragging && draggedItem != null)
            {
                isDragging = false;
                
                // 清除放置预览
                inventoryGrid.ClearPreview();
                
                // 获取物品UI的位置，这与ShowPlacementPreview方法使用的坐标一致
                ItemUI itemUI = GetItemUI(draggedItem);
                if (itemUI != null)
                {
                    // 获取物品的anchoredPosition作为本地坐标，与ShowPlacementPreview方法使用的坐标一致
                    Vector2 localItemPos = itemUI.GetComponent<RectTransform>().anchoredPosition;
                    
                    // 获取物品的实际形状和尺寸
                    bool[,] shape = draggedItem.GetActualShape();
                    int shapeWidth = shape.GetLength(0);
                    int shapeHeight = shape.GetLength(1);
                    
                    // 计算放置预览时使用的坐标，确保与绿色可放置区域一致
                    // 这与ShowPlacementPreview方法使用的坐标计算逻辑完全一致
                    Vector2Int gridPos = inventoryGrid.GetGridFromPosition(localItemPos);
                    
                    // 确保在边界内，与ShowPlacementPreview方法的处理完全一致
                    gridPos.x = Mathf.Clamp(gridPos.x, 0, inventoryGrid.width - shapeWidth);
                    gridPos.y = Mathf.Clamp(gridPos.y, 0, inventoryGrid.height - shapeHeight);
                    
                    // 使用与预览完全一致的坐标作为最终放置位置
                    Vector2Int finalPos = gridPos;
                    
                    // 尝试放置物品
                    if (InventoryManager.Instance.TryPlace(draggedItem, finalPos.x, finalPos.y, inventoryGrid))
                    {
                        // 放置成功，更新物品UI位置
                        itemUI.SnapToGrid(inventoryGrid, finalPos);
                        
                        // 恢复物品UI的CanvasGroup属性
                        CanvasGroup canvasGroup = itemUI.GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            canvasGroup.blocksRaycasts = false; // 不拦截点击事件，让底层Slot处理
                            canvasGroup.alpha = 1f;
                        }
                        
                        // 更新物品UI的拖拽状态
                        itemUI.IsDragging = false;
                    }
                    else
                    {
                        // 放置失败，检查原位置是否可用
                        if (inventoryGrid.CanPlace(draggedItem, draggedItem.posX, draggedItem.posY))
                        {
                            // 原位置可用，返回原位置
                            inventoryGrid.PlaceItem(draggedItem, draggedItem.posX, draggedItem.posY);
                            InventoryManager.Instance.AddItemToBag(draggedItem);
                            
                            itemUI.SnapToGrid(inventoryGrid, new Vector2Int(draggedItem.posX, draggedItem.posY));
                            
                            // 恢复物品UI的CanvasGroup属性
                            CanvasGroup canvasGroup = itemUI.GetComponent<CanvasGroup>();
                            if (canvasGroup != null)
                            {
                                canvasGroup.blocksRaycasts = false; // 不拦截点击事件，让底层Slot处理
                                canvasGroup.alpha = 1f;
                            }
                            
                            // 更新物品UI的拖拽状态
                            itemUI.IsDragging = false;
                        }
                        else
                        {
                            // 原位置也不可用，寻找最近的可放置位置
                            Vector2Int nearestPos = InventoryManager.Instance.FindEmptySpace(inventoryGrid, draggedItem.data);
                            if (nearestPos.x >= 0 && nearestPos.y >= 0)
                            {
                                // 找到可用位置，放置到该位置
                                inventoryGrid.PlaceItem(draggedItem, nearestPos.x, nearestPos.y);
                                InventoryManager.Instance.AddItemToBag(draggedItem);
                                
                                itemUI.SnapToGrid(inventoryGrid, nearestPos);
                                
                                // 恢复物品UI的CanvasGroup属性
                                CanvasGroup canvasGroup = itemUI.GetComponent<CanvasGroup>();
                                if (canvasGroup != null)
                                {
                                    canvasGroup.blocksRaycasts = false; // 不拦截点击事件，让底层Slot处理
                                    canvasGroup.alpha = 1f;
                                }
                                
                                // 更新物品UI的拖拽状态
                                itemUI.IsDragging = false;
                            }
                            else
                            {
                                // 没有找到可用位置，销毁物品
                                // 更新物品UI的拖拽状态
                                itemUI.IsDragging = false;
                                
                                // 销毁物品UI
                                Destroy(itemUI.gameObject);
                            }
                        }
                    }
                }
                
                draggedItem = null;
            }
            
            // 无论拖拽结果如何，都更新所有物品的星星高亮状态
            InventoryManager.Instance.UpdateAllStarHighlights();
        }
        
        /// <summary>
        /// 获取物品对应的ItemUI
        /// </summary>
        private ItemUI GetItemUI(ItemInstance item)
        {
            // 遍历所有ItemUI，找到对应的物品
            ItemUI[] allItems = FindObjectsOfType<ItemUI>();
            foreach (ItemUI itemUI in allItems)
            {
                if (itemUI.itemInstance == item)
                {
                    return itemUI;
                }
            }
            return null;
        }
    }
}