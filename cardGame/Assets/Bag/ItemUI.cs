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
        // 添加一个公开属性判断是否在拖拽
        private bool isDragging = false;
        public bool IsDragging { get { return isDragging; } private set { isDragging = value; } }
        
        public ItemInstance itemInstance;
        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private Image iconImage;
        private Image glowImage;
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
            
            // 创建外发光效果
            CreateGlowEffect();
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
            
            IsDragging = true; // 标记开始拖拽
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
                
                // 更新预览 - 使用鼠标实际位置，而不是物品当前位置，确保判定准确
                grid.ShowPlacementPreview(itemInstance, mousePos);
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
            
            IsDragging = false; // 标记结束拖拽
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            // 清除所有预览
            if (originalGrid != null) originalGrid.ClearPreview();
            if (InventoryManager.Instance.CurrentGrid != null) 
                InventoryManager.Instance.CurrentGrid.ClearPreview();

            // 使用射线检测判断鼠标下方是否有网格
            InventoryGrid targetGrid = GetGridUnderMouse(eventData);

            if (targetGrid == null)
            {
                // 情况 A：释放位置在网格外部 -> 丢弃
                InventoryManager.Instance.DropItem(this);
            }
            else
            {
                // 情况 B：在网格内部 -> 执行原有的放置逻辑
                // 使用鼠标位置而不是物品当前位置来计算最终放置位置
                // 这样可以确保与预览高亮区一致
                Vector2 mousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out mousePos))
                {
                    // 应用偏移
                    mousePos += dragOffset;
                    
                    // 获取基于鼠标位置的网格坐标
                    Vector2Int targetPos = targetGrid.GetGridFromPosition(mousePos);
                    
                    // 确保在边界内
                    targetPos.x = Mathf.Clamp(targetPos.x, 0, targetGrid.width - itemInstance.CurrentWidth);
                    targetPos.y = Mathf.Clamp(targetPos.y, 0, targetGrid.height - itemInstance.CurrentHeight);
                    
                    // 尝试放置
                    if (InventoryManager.Instance.TryPlace(itemInstance, targetPos.x, targetPos.y, targetGrid))
                    {
                        // 确保物品UI的父对象是正确的ItemContainer
                        if (transform.parent != InventoryManager.Instance.itemContainer)
                        {
                            transform.SetParent(InventoryManager.Instance.itemContainer);
                        }
                        SnapToGrid(targetGrid, targetPos);
                        return;
                    }
                }

                // 放置失败，返回原位置
                if (originalGrid != null)
                {
                    // 确保物品UI的父对象是正确的ItemContainer
                    if (transform.parent != InventoryManager.Instance.itemContainer)
                    {
                        transform.SetParent(InventoryManager.Instance.itemContainer);
                    }
                    
                    if (originalGrid.CanPlace(originalPos.x, originalPos.y, itemInstance.CurrentWidth, itemInstance.CurrentHeight))
                    {
                        originalGrid.PlaceItem(itemInstance, originalPos.x, originalPos.y);
                        SnapToGrid(originalGrid, originalPos);
                        InventoryManager.Instance.AddItemToBag(itemInstance);
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
                        // 确保物品UI的父对象是正确的ItemContainer
                        if (transform.parent != InventoryManager.Instance.itemContainer)
                        {
                            transform.SetParent(InventoryManager.Instance.itemContainer);
                        }
                        
                        grid.PlaceItem(itemInstance, x, y);
                        SnapToGrid(grid, new Vector2Int(x, y));
                        InventoryManager.Instance.AddItemToBag(itemInstance);
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
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var res in results)
            {
                // 查找父级是否有 InventoryGrid 组件
                var grid = res.gameObject.GetComponentInParent<InventoryGrid>();
                if (grid != null) return grid;
            }
            
            return null;
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
            
            // 更新外发光效果的大小
            if (glowImage != null)
            {
                RectTransform glowRect = glowImage.rectTransform;
                if (glowRect != null)
                {
                    // 确保发光效果与物品UI大小一致
                    glowRect.anchorMin = Vector2.zero;
                    glowRect.anchorMax = Vector2.one;
                    glowRect.offsetMin = Vector2.zero;
                    glowRect.offsetMax = Vector2.zero;
                }
            }
            
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
            // 右键旋转物品
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // 请求管理器尝试旋转
                InventoryManager.Instance.TryRotateItem(this);
                return;
            }
            
            // 左键选中物品
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // 设置当前选中物品
                InventoryManager.Instance.SelectedItem = this;
                
                // 添加选中效果反馈
                canvasGroup.alpha = 0.7f;
                Invoke(nameof(ResetAlpha), 0.1f);
            }
        }
        
        /// <summary>
        /// 重置透明度
        /// </summary>
        private void ResetAlpha()
        {
            canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// 创建外发光效果
        /// </summary>
        private void CreateGlowEffect()
        {
            // 检查是否已经存在发光效果
            Transform glowTransform = transform.Find("Glow");
            if (glowTransform == null)
            {
                // 创建发光效果的Image对象
                GameObject glowObj = new GameObject("Glow");
                glowObj.transform.SetParent(transform);
                
                // 设置锚点，使其与父对象大小一致
                RectTransform glowRect = glowObj.AddComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowRect.offsetMin = Vector2.zero;
                glowRect.offsetMax = Vector2.zero;
                glowRect.localScale = Vector3.one;
                glowRect.localEulerAngles = Vector3.zero;
                
                // 创建Image组件
                glowImage = glowObj.AddComponent<Image>();
                glowImage.color = new Color(1f, 1f, 0f, 0.5f); // 黄色发光效果，半透明
                glowImage.type = Image.Type.Simple;
                glowImage.raycastTarget = false; // 不接收射线检测
                glowImage.preserveAspect = false; // 不保持宽高比，自适应父对象大小
                
                // 设置发光效果的层级，使其在图标后面
                glowObj.transform.SetSiblingIndex(0);
                
                // 初始隐藏发光效果
                HideGlow();
            }
            else
            {
                glowImage = glowTransform.GetComponent<Image>();
                HideGlow();
            }
        }
        
        /// <summary>
        /// 显示外发光效果
        /// </summary>
        public void ShowGlow()
        {
            if (glowImage != null)
            {
                glowImage.enabled = true;
                
                // 添加简单的闪烁动画
                StartCoroutine(GlowAnimation());
            }
        }
        
        /// <summary>
        /// 隐藏外发光效果
        /// </summary>
        public void HideGlow()
        {
            if (glowImage != null)
            {
                glowImage.enabled = false;
            }
        }
        
        /// <summary>
        /// 外发光动画
        /// </summary>
        private System.Collections.IEnumerator GlowAnimation()
        {
            if (glowImage == null) yield break;
            
            // 闪烁3次
            for (int i = 0; i < 3; i++)
            {
                // 渐亮
                for (float alpha = 0.3f; alpha <= 0.7f; alpha += 0.1f)
                {
                    glowImage.color = new Color(1f, 1f, 0f, alpha);
                    yield return new WaitForSeconds(0.1f);
                }
                
                // 渐暗
                for (float alpha = 0.7f; alpha >= 0.3f; alpha -= 0.1f)
                {
                    glowImage.color = new Color(1f, 1f, 0f, alpha);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // 恢复初始状态
            glowImage.color = new Color(1f, 1f, 0f, 0.5f);
            glowImage.enabled = false;
        }
        
        /// <summary>
        /// 执行视觉旋转（只处理数据和UI，不处理逻辑判断）
        /// </summary>
        public void DoVisualRotate() 
        { 
            if (itemInstance == null) return;

            // --- 核心数据变更 ---
            itemInstance.isRotated = !itemInstance.isRotated;

            // --- 视觉变更 ---
            float cellSize = InventoryManager.Instance.CurrentGrid?.cellSize ?? 50f;
            
            // 重新计算宽高
            int w = itemInstance.CurrentWidth;
            int h = itemInstance.CurrentHeight;
            
            rect.sizeDelta = new Vector2(w * cellSize, h * cellSize);
            
            // 更新外发光效果的大小
            if (glowImage != null)
            {
                RectTransform glowRect = glowImage.rectTransform;
                if (glowRect != null)
                {
                    // 确保发光效果与物品UI大小一致
                    glowRect.anchorMin = Vector2.zero;
                    glowRect.anchorMax = Vector2.one;
                    glowRect.offsetMin = Vector2.zero;
                    glowRect.offsetMax = Vector2.zero;
                }
            }

            // 图标处理
            if (iconImage != null) 
            { 
                RectTransform iconRect = iconImage.rectTransform;
                // 旋转 -90度 或 0度
                iconRect.localEulerAngles = itemInstance.isRotated ? new Vector3(0, 0, -90) : Vector3.zero;
                
                // 修正图标尺寸匹配父容器
                // 注意：如果 Icon 旋转了90度，它的 width 对应父物体的 height
                if (itemInstance.isRotated)
                {
                    iconRect.sizeDelta = new Vector2(h * cellSize, w * cellSize);
                }
                else
                {
                    iconRect.sizeDelta = new Vector2(w * cellSize, h * cellSize);
                }
            }
        }
        
        /// <summary>
        /// 补充：为了解决Unity EventSystem中 Drag 会吞掉 Click 的问题
        /// 如果想在拖拽过程中按键盘（如 'R' 键）旋转，需要在 Update 中监听
        /// </summary>
        private void Update() 
        { 
            if (Input.GetKeyDown(KeyCode.R)) 
            { 
                // 只有当前选中的物品或正在拖拽的物品才允许旋转
                // 1. 如果正在拖拽，直接旋转
                if (IsDragging)
                {
                    Debug.Log("ItemUI: 拖拽中按R键旋转物品");
                    InventoryManager.Instance.TryRotateItem(this);
                }
                // 2. 如果是当前选中的物品，直接旋转
                else if (InventoryManager.Instance.SelectedItem == this)
                {
                    Debug.Log("ItemUI: 选中状态按R键旋转物品");
                    InventoryManager.Instance.TryRotateItem(this);
                }
            } 
        }
    }
}