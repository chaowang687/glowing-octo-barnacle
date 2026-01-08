using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace Bag.UI
{
    /// <summary>
    /// 物品图鉴主界面
    /// </summary>
    public class ItemCodexUI : MonoBehaviour
    {
        #region 字段
        [Header("UI组件")]
        public Transform categoryPanel; // 分类面板
        public Transform itemGrid; // 物品网格
        public GameObject categoryButtonPrefab; // 分类按钮预制体
        public GameObject itemUIPrefab; // 物品条目预制体（使用ItemUI_Prefab）
        public Bag.InventoryGrid inventoryGrid; // 物品网格组件，用于获取cellSize
        public GameObject detailPanelPrefab; // 物品详情面板预制体
        private ItemDetailPanel currentDetailPanel; // 当前显示的详情面板实例
        public TextMeshProUGUI completionText; // 完成度文本
        public TMP_InputField searchInput; // 搜索输入框
        public Button closeButton; // 关闭按钮
        
        [Header("状态")]
        private string currentCategory = "All"; // 当前选中分类
        private string currentSearch = ""; // 当前搜索关键词
        private List<CodexItem> filteredItems; // 过滤后的物品列表
        
        // 物品数据映射，用于快速查找CodexItem
        private Dictionary<Bag.ItemUI, CodexItem> uiToCodexItemMap = new Dictionary<Bag.ItemUI, CodexItem>();
        #endregion
        
        #region 生命周期
        private void OnEnable()
        {
            InitializeCategories();
            UpdateItemGrid();
            UpdateCompletionText();
            
            // 监听收集状态变化
            ItemCodexManager.Instance.OnCollectionStatusChanged += UpdateItemGrid;
            ItemCodexManager.Instance.OnCollectionStatusChanged += UpdateCompletionText;
            
            // 监听搜索输入变化
            searchInput.onValueChanged.AddListener(OnSearchInputChanged);
            
            // 监听关闭按钮点击事件
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseCodex);
            }
        }
        
        private void OnDisable()
        {
            ItemCodexManager.Instance.OnCollectionStatusChanged -= UpdateItemGrid;
            ItemCodexManager.Instance.OnCollectionStatusChanged -= UpdateCompletionText;
            searchInput.onValueChanged.RemoveListener(OnSearchInputChanged);
            
            // 移除关闭按钮点击事件监听
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseCodex);
            }
        }
        #endregion
        
        #region 初始化方法
        /// <summary>
        /// 初始化分类按钮
        /// </summary>
        private void InitializeCategories()
        {
            // 清空现有分类按钮
            foreach (Transform child in categoryPanel)
            {
                Destroy(child.gameObject);
            }
            
            // 添加"全部"分类
            CreateCategoryButton("All", "全部");
            
            // 添加其他分类
            if (ItemCodexManager.Instance.codexData != null)
            {
                foreach (var category in ItemCodexManager.Instance.codexData.categories)
                {
                    CreateCategoryButton(category.name, category.displayName);
                }
            }
        }
        
        /// <summary>
        /// 创建分类按钮
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <param name="displayName">显示名称</param>
        private void CreateCategoryButton(string categoryName, string displayName)
        {
            GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryPanel);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (button != null && buttonText != null)
            {
                buttonText.text = displayName;
                button.onClick.AddListener(() => OnCategorySelected(categoryName));
                
                // 设置默认选中状态
                if (categoryName == currentCategory)
                {
                    button.interactable = false;
                }
            }
        }
        #endregion
        
        #region UI 更新方法
        /// <summary>
        /// 更新物品网格
        /// </summary>
        private void UpdateItemGrid()
        {
            // 清空现有物品条目
            foreach (Transform child in itemGrid)
            {
                Destroy(child.gameObject);
            }
            
            // 清空映射表
            uiToCodexItemMap.Clear();
            
            // 获取过滤后的物品列表
            filteredItems = GetFilteredItems();
            
            // 按物品ID排序（从小到大）
            filteredItems.Sort((a, b) => a.itemID.CompareTo(b.itemID));
            
            // 布局参数
            float spacing = 5f; // 物品间距
            float cellSize;
            
            // 参考背包系统的实现，使用InventoryGrid的cellSize
            if (inventoryGrid != null)
            {
                cellSize = inventoryGrid.cellSize;
                Debug.Log($"ItemCodexUI: 使用InventoryGrid的cellSize: {cellSize}");
            }
            else
            {
                // 如果没有找到InventoryGrid，使用默认值
                cellSize = 50f; // 默认值，与背包系统保持一致
                Debug.LogWarning("ItemCodexUI: 未找到InventoryGrid组件，使用默认cellSize: 50f");
            }
            
            // 获取物品网格容器的宽度，计算网格列数
            RectTransform gridRect = itemGrid.GetComponent<RectTransform>();
            float gridWidth = gridRect.rect.width;
            int gridCols = Mathf.FloorToInt(gridWidth / (cellSize + spacing));
            if (gridCols < 1) gridCols = 1; // 确保至少有1列
            
            // 初始化网格占用数组（使用布尔值表示是否被占用）
            int initialRows = Mathf.CeilToInt((float)filteredItems.Count / gridCols);
            if (initialRows < 1) initialRows = 1;
            bool[,] gridOccupied = new bool[gridCols, initialRows];
            
            // 遍历物品列表，创建物品条目
            foreach (var item in filteredItems)
            {
                GameObject entryObj = Instantiate(itemUIPrefab, itemGrid);
                Bag.ItemUI itemUI = entryObj.GetComponent<Bag.ItemUI>();
                
                if (itemUI != null)
                {
                    // 从Resources加载实际的ItemData
                    Bag.ItemData actualItemData = Resources.Load<Bag.ItemData>("Items/" + item.itemID);
                    
                    if (actualItemData == null)
                    {
                        // 如果资源加载失败，尝试其他路径
                        actualItemData = Resources.Load<Bag.ItemData>(item.itemID);
                        
                        if (actualItemData == null)
                        {
                            // 仍然失败，使用临时数据
                            Debug.LogWarning($"ItemCodexUI: 未找到物品资源 Items/{item.itemID} 或 {item.itemID}，使用临时数据");
                            actualItemData = ScriptableObject.CreateInstance<Bag.ItemData>();
                            actualItemData.itemName = item.itemName;
                            actualItemData.icon = item.icon;
                            actualItemData.width = 1;
                            actualItemData.height = 1;
                        }
                    }
                    
                    // 使用实际的ItemData创建ItemInstance
                    Bag.ItemInstance tempInstance = new Bag.ItemInstance(actualItemData);
                    
                    // 初始化ItemUI
                    itemUI.Initialize(tempInstance, cellSize);
                    // 强制调用一次UpdateVisual，确保使用正确的cellSize
                    itemUI.UpdateVisual();
                    
                    // 查找合适的网格位置
                    Vector2Int gridPos = FindAvailableGridPosition(gridOccupied, tempInstance.CurrentWidth, tempInstance.CurrentHeight);
                    
                    // 如果需要扩展网格
                    if (gridPos.y + tempInstance.CurrentHeight > gridOccupied.GetLength(1))
                    {
                        // 扩展网格行数
                        int newRows = gridPos.y + tempInstance.CurrentHeight;
                        bool[,] newGrid = new bool[gridCols, newRows];
                        
                        // 复制现有网格数据
                        for (int x = 0; x < gridCols; x++)
                        {
                            for (int y = 0; y < gridOccupied.GetLength(1); y++)
                            {
                                newGrid[x, y] = gridOccupied[x, y];
                            }
                        }
                        
                        gridOccupied = newGrid;
                    }
                    
                    // 标记网格为已占用
                    MarkGridOccupied(gridOccupied, gridPos.x, gridPos.y, tempInstance.CurrentWidth, tempInstance.CurrentHeight);
                    
                    // 设置物品位置
                    RectTransform itemRect = itemUI.GetComponent<RectTransform>();
                    if (itemRect != null)
                    {
                        float posX = gridPos.x * (cellSize + spacing);
                        float posY = -gridPos.y * (cellSize + spacing);
                        itemRect.anchoredPosition = new Vector2(posX, posY);
                    }
                    
                    // 保存映射关系
                    uiToCodexItemMap[itemUI] = item;
                    
                    // 添加鼠标悬停事件
                    AddItemHoverListeners(itemUI.gameObject, item);
                    
                    // 移除图鉴物品的拖拽功能，避免误操作将图鉴物品添加到背包
                    RemoveDragFunctionality(itemUI);
                    
                    // 根据收集状态设置显示效果
                    bool isCollected = ItemCodexManager.Instance.IsCollected(item.itemID);
                    SetItemUIVisualState(itemUI, isCollected);
                }
            }
        }
        
        /// <summary>
        /// 查找可用的网格位置
        /// </summary>
        private Vector2Int FindAvailableGridPosition(bool[,] grid, int width, int height)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            
            // 遍历网格，寻找可用位置
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // 检查当前位置是否可用
                    if (IsGridPositionAvailable(grid, x, y, width, height))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            
            // 如果没有找到可用位置，返回新行的第一个位置
            return new Vector2Int(0, rows);
        }
        
        /// <summary>
        /// 检查网格位置是否可用
        /// </summary>
        private bool IsGridPositionAvailable(bool[,] grid, int x, int y, int width, int height)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            
            // 检查是否超出边界
            if (x + width > cols || y + height > rows)
            {
                return false;
            }
            
            // 检查所有格子是否都可用
            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    if (grid[i, j])
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 标记网格为已占用
        /// </summary>
        private void MarkGridOccupied(bool[,] grid, int x, int y, int width, int height)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            
            // 扩展网格如果需要
            if (x + width > cols || y + height > rows)
            {
                return; // 超出边界，不标记
            }
            
            // 标记所有格子为已占用
            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    grid[i, j] = true;
                }
            }
        }
        
        /// <summary>
        /// 为ItemUI添加鼠标悬停监听器
        /// </summary>
        private void AddItemHoverListeners(GameObject itemObj, CodexItem codexItem)
        {
            // 查找或添加ItemHoverHandler组件
            ItemHoverHandler hoverHandler = itemObj.GetComponent<ItemHoverHandler>();
            if (hoverHandler == null)
            {
                hoverHandler = itemObj.AddComponent<ItemHoverHandler>();
            }
            
            // 设置回调函数
            hoverHandler.OnPointerEnterAction = () => ShowItemDetail(itemObj, codexItem);
            hoverHandler.OnPointerExitAction = HideItemDetail;
        }
        
        /// <summary>
        /// 物品悬停处理器组件
        /// </summary>
        private class ItemHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public System.Action OnPointerEnterAction;
            public System.Action OnPointerExitAction;
            
            public void OnPointerEnter(PointerEventData eventData)
            {
                OnPointerEnterAction?.Invoke();
            }
            
            public void OnPointerExit(PointerEventData eventData)
            {
                OnPointerExitAction?.Invoke();
            }
        }
        
        /// <summary>
        /// 移除物品UI的拖拽功能
        /// </summary>
        private void RemoveDragFunctionality(Bag.ItemUI itemUI)
        {
            if (itemUI != null)
            {
                itemUI.allowDrag = false;
                Debug.Log($"ItemCodexUI: 已禁用物品 {itemUI.itemInstance.data?.itemName} 的拖拽功能");
            }
        }
        
        /// <summary>
        /// 设置ItemUI的视觉状态（根据收集状态）
        /// </summary>
        private void SetItemUIVisualState(Bag.ItemUI itemUI, bool isCollected)
        {
            // 获取CanvasGroup组件
            CanvasGroup canvasGroup = itemUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = itemUI.gameObject.AddComponent<CanvasGroup>();
            }
            
            // 根据收集状态设置透明度和交互性
            if (isCollected)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.alpha = 0.5f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
        
        /// <summary>
        /// 更新完成度文本
        /// </summary>
        private void UpdateCompletionText()
        {
            if (completionText != null)
            {
                int collected = ItemCodexManager.Instance.GetCollectedCount();
                int total = ItemCodexManager.Instance.GetTotalItemCount();
                float percentage = ItemCodexManager.Instance.GetCompletionPercentage();
                
                completionText.text = $"图鉴完成度: {collected}/{total} ({percentage:F1}%)";
            }
        }
        #endregion
        
        #region 事件处理
        /// <summary>
        /// 分类选中事件
        /// </summary>
        /// <param name="category">选中的分类</param>
        private void OnCategorySelected(string category)
        {
            currentCategory = category;
            InitializeCategories(); // 重新创建分类按钮以更新选中状态
            UpdateItemGrid();
        }
        
        /// <summary>
        /// 搜索输入变化事件
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private void OnSearchInputChanged(string searchText)
        {
            currentSearch = searchText;
            UpdateItemGrid();
        }
        
        /// <summary>
        /// 显示物品详情
        /// </summary>
        /// <param name="itemObj">被悬停的物品UI对象</param>
        /// <param name="item">物品数据</param>
        public void ShowItemDetail(GameObject itemObj, CodexItem item)
        {
            // 销毁当前显示的详情面板实例（如果存在）
            if (currentDetailPanel != null)
            {
                Destroy(currentDetailPanel.gameObject);
                currentDetailPanel = null;
            }
            
            // 实例化新的详情面板预制体
                if (detailPanelPrefab != null)
                {
                    GameObject panelObj = Instantiate(detailPanelPrefab, itemGrid);
                    currentDetailPanel = panelObj.GetComponent<ItemDetailPanel>();
                    
                    // 设置详情面板不接收鼠标事件，避免干扰物品的悬停检测
                    SetRaycastTargetRecursive(panelObj, false);
                    
                    if (currentDetailPanel != null)
                    {
                        // 尝试从静态资源加载ItemData
                        Bag.ItemData itemData = Resources.Load<Bag.ItemData>("Items/" + item.itemID);
                        
                        if (itemData == null)
                        {
                            // 如果资源加载失败，尝试其他路径
                            itemData = Resources.Load<Bag.ItemData>(item.itemID);
                        }
                        
                        if (itemData != null)
                        {
                            // 如果成功加载到ItemData，使用静态资源配置
                            Debug.Log($"ItemCodexUI: 使用静态资源 ItemData 显示物品 {item.itemID} 的详情");
                            currentDetailPanel.ShowItem(itemData);
                        }
                        else
                        {
                            // 如果没有找到ItemData，使用原有CodexItem数据
                            Debug.Log($"ItemCodexUI: 使用 CodexItem 数据显示物品 {item.itemID} 的详情");
                            currentDetailPanel.ShowItem(item);
                        }
                        
                        // 将详情面板吸附到物品右侧
                        AttachDetailPanelToItem(itemObj, panelObj);
                    }
                    else
                    {
                        Debug.LogError("ItemCodexUI: 详情面板预制体中没有ItemDetailPanel组件");
                        Destroy(panelObj);
                    }
                }
        }
        
        /// <summary>
        /// 隐藏物品详情
        /// </summary>
        public void HideItemDetail()
        {
            // 销毁当前显示的详情面板实例（如果存在）
            if (currentDetailPanel != null)
            {
                Destroy(currentDetailPanel.gameObject);
                currentDetailPanel = null;
            }
        }
        
        /// <summary>
        /// 递归设置对象及其所有子对象的raycastTarget属性
        /// </summary>
        /// <param name="obj">要设置的对象</param>
        /// <param name="value">raycastTarget的值</param>
        private void SetRaycastTargetRecursive(GameObject obj, bool value)
        {
            if (obj == null)
                return;
            
            // 设置当前对象的raycastTarget
            Image image = obj.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = value;
            }
            
            // 设置TextMeshProUGUI的raycastTarget
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.raycastTarget = value;
            }
            
            // 递归设置所有子对象
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetRaycastTargetRecursive(obj.transform.GetChild(i).gameObject, value);
            }
        }
        
        /// <summary>
        /// 将详情面板吸附到物品右侧
        /// </summary>
        /// <param name="itemObj">被点击的物品UI对象</param>
        /// <param name="panelObj">详情面板对象</param>
        private void AttachDetailPanelToItem(GameObject itemObj, GameObject panelObj)
        {
            if (itemObj == null || panelObj == null)
                return;
            
            // 获取物品UI的RectTransform
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            if (itemRect == null)
                return;
            
            // 获取详情面板的RectTransform
            RectTransform detailRect = panelObj.GetComponent<RectTransform>();
            if (detailRect == null)
                return;
            
            // 获取物品网格的RectTransform
            RectTransform gridRect = itemGrid.GetComponent<RectTransform>();
            if (gridRect == null)
                return;
            
            // 设置面板的父对象为物品网格，确保它们在同一坐标系下
            detailRect.SetParent(gridRect);
            detailRect.localScale = Vector3.one;
            
            // 使用布局吸附方式，将面板放置在物品右侧
            // 计算物品右侧位置和中心位置
            Vector2 itemAnchoredPos = itemRect.anchoredPosition;
            float itemRight = itemAnchoredPos.x + itemRect.sizeDelta.x;
            float itemCenterY = itemAnchoredPos.y - (itemRect.sizeDelta.y / 2);
            
            // 设置面板位置：物品右侧，中心对齐
            float spacing = 5f; // 物品与面板之间的间距
            Vector2 detailAnchoredPos;
            
            // 面板左侧位置 = 物品右侧位置 + 间距
            detailAnchoredPos.x = itemRight + spacing;
            // 面板中心位置 = 物品中心位置
            detailAnchoredPos.y = itemCenterY + (detailRect.sizeDelta.y / 2);
            
            // 确保面板在网格可视区域内
            Rect gridBounds = gridRect.rect;
            float maxX = gridBounds.width - detailRect.sizeDelta.x;
            float maxY = gridBounds.height;
            float minY = 0f;
            
            detailAnchoredPos.x = Mathf.Min(detailAnchoredPos.x, maxX);
            detailAnchoredPos.y = Mathf.Clamp(detailAnchoredPos.y, minY, maxY);
            
            // 设置面板的anchoredPosition，实现吸附效果
            detailRect.anchoredPosition = detailAnchoredPos;
            
            // 确保面板层级在顶层，不被其他物品遮挡
            detailRect.SetAsLastSibling();
        }
        
        /// <summary>
        /// 关闭图鉴
        /// </summary>
        public void CloseCodex()
        {
            gameObject.SetActive(false);
        }
        #endregion
        
        #region 辅助方法
        /// <summary>
        /// 获取过滤后的物品列表
        /// </summary>
        /// <returns>过滤后的物品列表</returns>
        private List<CodexItem> GetFilteredItems()
        {
            List<CodexItem> result = new List<CodexItem>();
            
            // 检查ItemCodexManager实例
            if (ItemCodexManager.Instance == null)
            {
                Debug.LogWarning("ItemCodexUI: ItemCodexManager.Instance 为 null");
                return result;
            }
            
            // 检查codexData
            if (ItemCodexManager.Instance.codexData == null)
            {
                Debug.LogWarning("ItemCodexUI: ItemCodexManager.Instance.codexData 为 null");
                return result;
            }
            
            // 调试信息：检查物品数量和当前分类
            int totalItems = ItemCodexManager.Instance.codexData.allItems.Count;
            Debug.Log($"ItemCodexUI: 图鉴总物品数: {totalItems}, 当前分类: {currentCategory}, 当前搜索: {currentSearch}");
            
            // 分类过滤
            if (currentCategory == "All")
            {
                result.AddRange(ItemCodexManager.Instance.codexData.allItems);
                Debug.Log($"ItemCodexUI: 全部分类显示，显示物品数: {result.Count}");
            }
            else
            {
                foreach (var item in ItemCodexManager.Instance.codexData.allItems)
                {
                    if (!string.IsNullOrEmpty(item.category) && item.category == currentCategory)
                    {
                        result.Add(item);
                    }
                }
                Debug.Log($"ItemCodexUI: 分类 {currentCategory} 显示，显示物品数: {result.Count}");
            }
            
            // 搜索过滤
            if (!string.IsNullOrEmpty(currentSearch))
            {
                string searchLower = currentSearch.ToLower();
                int beforeSearchCount = result.Count;
                result = result.FindAll(item => 
                    item.itemName.ToLower().Contains(searchLower) || 
                    item.description.ToLower().Contains(searchLower));
                Debug.Log($"ItemCodexUI: 搜索过滤后，显示物品数: {result.Count} (过滤前: {beforeSearchCount})");
            }
            
            return result;
        }
        #endregion
    }
}