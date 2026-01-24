using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Bag
{
    /// <summary>
    /// 背包网格组件，负责管理网格数据和物品放置逻辑
    /// </summary>
    public class InventoryGrid : MonoBehaviour 
    {
        public int width = 10;
        public int height = 8;
        public float cellSize = 50f;
        public GameObject slotPrefab; // Slot预制体
        
        private ItemInstance[,] gridItems;
        private List<Slot> slots = new List<Slot>();
        private RectTransform gridRect;
        private GridLayoutGroup gridLayout;
        
        // Slot容器，用于存放所有生成的Slot
        private GameObject slotContainer;
    private InventoryGridShapePreview shapePreview;
    
    private void Start()
    {
        // 获取形状预览组件
        shapePreview = GetComponent<InventoryGridShapePreview>();
        if (shapePreview == null)
        {
            // 如果没有形状预览组件，自动添加
            shapePreview = gameObject.AddComponent<InventoryGridShapePreview>();
        }
    }
        
        private void Awake() 
    {
        // 初始化网格数组
        gridItems = new ItemInstance[width, height];
        
        // 获取RectTransform
        gridRect = GetComponent<RectTransform>();
        
        // 创建Slot容器
        CreateSlotContainer();
        
        // 生成Slot
        GenerateSlots();
    }    
    
    /// <summary>
    /// 创建Slot容器
    /// </summary>
    private void CreateSlotContainer()
    {
        // 查找或创建Slot容器
        slotContainer = transform.Find("SlotContainer")?.gameObject;
        if (slotContainer == null)
        {
            slotContainer = new GameObject("SlotContainer");
            slotContainer.transform.SetParent(transform);
            
            // 设置Slot容器的RectTransform
            RectTransform slotContainerRect = slotContainer.AddComponent<RectTransform>();
            slotContainerRect.anchorMin = Vector2.zero;
            slotContainerRect.anchorMax = Vector2.one;
            slotContainerRect.offsetMin = Vector2.zero;
            slotContainerRect.offsetMax = Vector2.zero;
            slotContainerRect.localScale = Vector3.one;
            
            // 添加GridLayoutGroup组件
            gridLayout = slotContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
            gridLayout.spacing = Vector2.zero;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = width;
        }
    }
    
    /// <summary>
    /// 生成Slot
    /// </summary>
    private void GenerateSlots()
    {
        // 清理现有Slot
        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
        slots.Clear();
        
        // 如果没有Slot预制体，创建一个默认的
        if (slotPrefab == null)
        {
            slotPrefab = new GameObject("DefaultSlot");
            slotPrefab.AddComponent<RectTransform>();
            Image image = slotPrefab.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.1f);
            slotPrefab.AddComponent<Slot>();
        }
        
        // 生成Slot
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer.transform);
                Slot slot = slotObj.GetComponent<Slot>();
                if (slot != null)
                {
                    slot.SetGridPosition(x, y);
                    slot.inventoryGrid = this;
                    slots.Add(slot);
                }
            }
        }
    }
    
    /// <summary>
    /// 清理Slot
    /// </summary>
    private void ClearSlots()
    {
        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
        slots.Clear();
    }
        
        /// <summary>
        /// 清除放置预览
        /// </summary>
        public void ClearPreview() 
        {
            if (shapePreview != null)
            {
                shapePreview.HidePreview();
            }
        }
        
        /// <summary>
        /// 将网格坐标转换为本地坐标
        /// </summary>
        /// <param name="x">网格X坐标</param>
        /// <param name="y">网格Y坐标</param>
        /// <returns>本地坐标</returns>
        public Vector2 GetPositionFromGrid(int x, int y) 
        {
            return new Vector2(x * cellSize, -y * cellSize);
        }
        
        /// <summary>
        /// 将网格坐标转换为本地坐标（带物品尺寸）
        /// </summary>
        /// <param name="x">网格X坐标</param>
        /// <param name="y">网格Y坐标</param>
        /// <param name="w">物品宽度</param>
        /// <param name="h">物品高度</param>
        /// <returns>本地坐标</returns>
        public Vector2 GetPositionFromGrid(int x, int y, int w, int h) 
        {
            // 对于Pivot在左上角的情况，物品位置就是左上角坐标
            return new Vector2(x * cellSize, -y * cellSize);
        }
        
        /// <summary>
        /// 将本地坐标转换为网格坐标
        /// </summary>
        /// <param name="localPos">本地坐标</param>
        /// <returns>网格坐标</returns>
        public Vector2Int GetGridFromPosition(Vector2 localPos) 
        {
            // 假设 Pivot 在左上角 (0,1)
            // 对于X轴：正常计算
            int x = Mathf.FloorToInt(localPos.x / cellSize);
            
            // 对于Y轴：修复前两行无法触发的问题
            // 添加一个小的偏移量，确保前两行能正确触发
            float yPos = (-localPos.y / cellSize) + 0.1f;
            int y = Mathf.FloorToInt(yPos);
            
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// 显示物品放置预览
        /// </summary>
        /// <param name="item">物品实例</param>
        /// <param name="localPos">本地坐标</param>
        public void ShowPlacementPreview(ItemInstance item, Vector2 localPos) 
        {
            if (item == null) return;
            
            Vector2Int gridPos = GetGridFromPosition(localPos);
            
            // 获取实际形状和尺寸
            bool[,] shape = item.GetActualShape();
            int shapeWidth = shape.GetLength(0);
            int shapeHeight = shape.GetLength(1);
            
            // 确保在边界内
            gridPos.x = Mathf.Clamp(gridPos.x, 0, width - shapeWidth);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, height - shapeHeight);
            
            // 检查是否可以放置
            bool canPlace = CanPlace(item, gridPos.x, gridPos.y);
            
            // 使用形状预览组件显示预览
            if (shapePreview != null)
            {
                shapePreview.ShowShapePreview(item, gridPos, canPlace);
            }
        }
        
        /// <summary>
        /// 检查位置是否超出边界
        /// </summary>
        public bool IsOutOfBounds(int x, int y, int w, int h) {
            return x < 0 || y < 0 || x + w > this.width || y + h > this.height;
        }
        
        /// <summary>
        /// 检查物品是否可以放置在指定位置
        /// </summary>
        public bool CanPlace(int x, int y, int w, int h) {
            if (IsOutOfBounds(x, y, w, h)) 
            {
                Debug.Log($"越界失败: {x},{y} Size:{w}x{h}");
                return false;
            }
            
            for (int i = x; i < x + w; i++) {
                for (int j = y; j < y + h; j++) {
                    if (gridItems[i, j] != null) {
                        // 打印出到底是谁占了位置
                        Debug.Log($"格子 [{i},{j}] 已被 {gridItems[i, j].data.itemName} 占用");
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// 检查异形物品是否可以放置在指定位置
        /// </summary>
        public bool CanPlace(ItemInstance item, int x, int y) {
            // 获取旋转后的实际形状和尺寸
            bool[,] shape = item.GetActualShape();
            int shapeWidth = shape.GetLength(0);
            int shapeHeight = shape.GetLength(1);
            
            // 检查边界
            if (IsOutOfBounds(x, y, shapeWidth, shapeHeight)) {
                Debug.Log($"异形物品越界失败: {x},{y} Size:{shapeWidth}x{shapeHeight}");
                return false;
            }
            
            // 检查每个占用格子
            for (int i = 0; i < shapeWidth; i++) {
                for (int j = 0; j < shapeHeight; j++) {
                    if (shape[i, j]) {
                        int gridX = x + i;
                        int gridY = y + j;
                        if (gridItems[gridX, gridY] != null) {
                            Debug.Log($"异形物品格子 [{gridX},{gridY}] 已被 {gridItems[gridX, gridY].data.itemName} 占用");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// 检查物品星星槽位的相邻情况
        /// </summary>
        public Dictionary<Vector2Int, ItemInstance> CheckStarAdjacency(ItemInstance item) {
            Dictionary<Vector2Int, ItemInstance> adjacentItems = new Dictionary<Vector2Int, ItemInstance>();
            
            // 保护检查：确保gridItems已初始化
            if (gridItems == null) {
                Debug.LogWarning("[InventoryGrid] gridItems数组未初始化，无法检查星星槽位相邻情况");
                return adjacentItems;
            }
            
            // 保护检查：确保item不为空
            if (item == null) {
                Debug.LogWarning("[InventoryGrid] 传入的item为空，无法检查星星槽位相邻情况");
                return adjacentItems;
            }
            
            List<Vector2Int> starPositions = item.GetStarPositions();
            
            // 调试：打印当前物品的标签
            string itemTags = item.data.tags != null ? string.Join(", ", item.data.tags) : "无标签";
            Debug.Log($"[InventoryGrid] 检查物品 {item.data.itemName} 的星星槽位相邻情况，物品标签: {itemTags}");
            
            foreach (Vector2Int starPos in starPositions) {
                Debug.Log($"[InventoryGrid] 检查星星位置: {starPos}");
                if (IsValidPosition(starPos)) {
                    ItemInstance adjacentItem = gridItems[starPos.x, starPos.y];
                    if (adjacentItem != null && adjacentItem != item) {
                        Debug.Log($"[InventoryGrid] 星星位置 {starPos} 有相邻物品: {adjacentItem.data.itemName}");
                        
                        // 检查相邻物品是否有相关标签
                        bool hasRelatedTag = false;
                        
                        // 调试：打印相邻物品的标签
                        string adjacentTags = adjacentItem.data.tags != null ? string.Join(", ", adjacentItem.data.tags) : "无标签";
                        Debug.Log($"[InventoryGrid] 相邻物品 {adjacentItem.data.itemName} 标签: {adjacentTags}");
                        
                        // 简化检查：只有当两个物品都有标签且至少有一个相同标签时才认为相关
                        if (adjacentItem.data.tags != null && adjacentItem.data.tags.Count > 0 && 
                            item.data.tags != null && item.data.tags.Count > 0) {
                            
                            Debug.Log($"[InventoryGrid] 开始检查标签匹配");
                            foreach (string tag in adjacentItem.data.tags) {
                                if (item.data.tags.Contains(tag)) {
                                    hasRelatedTag = true;
                                    Debug.Log($"[InventoryGrid] 标签匹配成功: {tag}");
                                    break;
                                }
                            }
                        } else {
                            Debug.Log("[InventoryGrid] 物品没有标签或标签为空，不匹配");
                        }
                        
                        if (hasRelatedTag) {
                            adjacentItems.Add(starPos, adjacentItem);
                            Debug.Log($"[InventoryGrid] 添加相邻物品到结果: {starPos} -> {adjacentItem.data.itemName}");
                        } else {
                            Debug.Log($"[InventoryGrid] 标签不匹配，不添加到结果");
                        }
                    } else {
                        if (adjacentItem == item) {
                            Debug.Log($"[InventoryGrid] 星星位置 {starPos} 是物品自身，跳过");
                        } else {
                            Debug.Log($"[InventoryGrid] 星星位置 {starPos} 没有物品");
                        }
                    }
                } else {
                    Debug.Log($"[InventoryGrid] 星星位置 {starPos} 无效");
                }
            }
            
            Debug.Log($"[InventoryGrid] 最终相邻物品数量: {adjacentItems.Count}");
            return adjacentItems;
        }
        
        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        private bool IsValidPosition(Vector2Int pos) {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }
        
        /// <summary>
        /// 放置物品到指定位置
        /// </summary>
        public void PlaceItem(ItemInstance item, int x, int y) {
            if (item == null) return;
            
            // --- 新增保护代码 ---
            if (gridItems == null) {
                Debug.LogWarning("gridItems 为空，正在紧急初始化...");
                gridItems = new ItemInstance[width, height];
            }
            // ------------------
            
            // 检查位置是否可用，包括边界和碰撞检测
            if (!CanPlace(item, x, y)) {
                Debug.LogWarning($"无法放置物品 {item.data.itemName} 到位置 ({x},{y})，位置已被占用或越界");
                return;
            }
            
            // 获取旋转后的实际形状和尺寸
            bool[,] shape = item.GetActualShape();
            int shapeWidth = shape.GetLength(0);
            int shapeHeight = shape.GetLength(1);
            
            // 在数组中登记，只占用实际形状为true的格子
            for (int i = 0; i < shapeWidth; i++) {
                for (int j = 0; j < shapeHeight; j++) {
                    if (shape[i, j]) {
                        int gridX = x + i;
                        int gridY = y + j;
                        gridItems[gridX, gridY] = item;
                    }
                }
            }
            
            // 更新物品位置信息
            item.posX = x;
            item.posY = y;
        }
        
        /// <summary>
        /// 从网格中移除物品
        /// </summary>
        public void RemoveItem(ItemInstance item) {
            if (item == null) return;
            
            // 遍历整个网格，清除所有指向该物品的引用
            // 这样可以避免因旋转状态变化导致的清除不完整问题
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    if (gridItems[i, j] == item) {
                        gridItems[i, j] = null;
                    }
                }
            }
        }
        
        /// <summary> 
        /// 检查物品旋转后是否合法（不修改实际数据） 
        /// </summary> 
        public bool CheckRotateValidity(ItemInstance item) 
        { 
            // 1. 保存当前旋转状态 
            int currentRotation = item.rotation; 
    
            // 2. 暂时移除物品（为了避免检测时和自己当前的占用冲突） 
            RemoveItem(item); 
    
            // 3. 模拟旋转后的状态 
            item.rotation = (currentRotation + 90) % 360; 
    
            // 4. 检查位置是否可用 
            bool canPlace = CanPlace(item, item.posX, item.posY); 
    
            // 5. 恢复原始旋转状态 
            item.rotation = currentRotation; 
    
            // 6. 无论结果如何，先把物品放回去（保持状态原样） 
            PlaceItem(item, item.posX, item.posY); 
    
            return canPlace; 
        }
        
        /// <summary>
        /// 获取目标区域唯一的重叠物品
        /// </summary>
        public ItemInstance GetOverlapItem(int x, int y, int w, int h) {
            if (IsOutOfBounds(x, y, w, h)) return null;
            
            ItemInstance found = null;
            for (int i = x; i < x + w; i++) {
                for (int j = y; j < y + h; j++) {
                    ItemInstance itemAtPos = gridItems[i, j];
                    if (itemAtPos != null) {
                        if (found == null) {
                            found = itemAtPos;
                        } else if (found != itemAtPos) {
                            return null; // 超过一个物品，不可交换
                        }
                    }
                }
            }
            return found;
        }
        
        /// <summary>
        /// 检查指定位置是否有物品
        /// </summary>
        /// <param name="x">网格X坐标</param>
        /// <param name="y">网格Y坐标</param>
        /// <returns>物品实例</returns>
        public ItemInstance GetItemAt(int x, int y) {
            if (x < 0 || x >= width || y < 0 || y >= height) return null;
            return gridItems[x, y];
        }
        
        /// <summary>
        /// 清空网格数据
        /// </summary>
        public void ClearGrid() {
            gridItems = new ItemInstance[width, height];
        }
        
        /// <summary>
        /// 获取网格中指定物品的位置
        /// </summary>
        /// <param name="item">物品实例</param>
        /// <returns>网格坐标</returns>
        public Vector2Int GetItemPosition(ItemInstance item) {
            if (item == null) return new Vector2Int(-1, -1);
            return new Vector2Int(item.posX, item.posY);
        }
    }
}

