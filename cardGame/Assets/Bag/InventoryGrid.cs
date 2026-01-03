using UnityEngine;
using UnityEngine.UI;

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
        
        [Header("视觉设置")]
        public RectTransform ghostPreview; // 预览用的 Image
        
        private ItemInstance[,] gridSlots;
        private Image previewImage;
        
        private void Awake() 
        {
            // 初始化网格数组
            gridSlots = new ItemInstance[width, height];
            
            // 初始化预览组件
            InitializePreview();
        }
        
        /// <summary>
        /// 初始化预览组件
        /// </summary>
        private void InitializePreview()
        {
            if (ghostPreview != null)
            {
                previewImage = ghostPreview.GetComponent<Image>();
                ghostPreview.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 清除放置预览
        /// </summary>
        public void ClearPreview() 
        {
            if (ghostPreview != null)
            {
                ghostPreview.gameObject.SetActive(false);
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
            int itemWidth = item.CurrentWidth;
            int itemHeight = item.CurrentHeight;
            
            // 确保在边界内
            gridPos.x = Mathf.Clamp(gridPos.x, 0, width - itemWidth);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, height - itemHeight);
            
            // 检查是否可以放置（只允许放在空格子上）
            bool canPlace = CanPlace(gridPos.x, gridPos.y, itemWidth, itemHeight);
            
            // 绘制预览（只有在空格子上才显示绿色预览）
            DrawPreviewGhost(gridPos, itemWidth, itemHeight, canPlace);
        }
        
        /// <summary>
        /// 绘制预览幽灵
        /// </summary>
        private void DrawPreviewGhost(Vector2Int pos, int width, int height, bool isValid) 
        {
            if (ghostPreview == null || previewImage == null) return;
            
            ghostPreview.gameObject.SetActive(true);
            
            // 设置大小
            ghostPreview.sizeDelta = new Vector2(width * cellSize, height * cellSize);
            
            // 设置位置
            ghostPreview.anchoredPosition = GetPositionFromGrid(pos.x, pos.y);
            
            // 设置颜色：合法为绿，非法为红
            previewImage.color = isValid ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
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
                if (gridSlots[i, j] != null) {
                    // 打印出到底是谁占了位置
                    Debug.Log($"格子 [{i},{j}] 已被 {gridSlots[i, j].data.itemName} 占用");
                    return false;
                }
            }
    }
            return true;
        }
        
        /// <summary>
        /// 放置物品到指定位置
        /// </summary>
        public void PlaceItem(ItemInstance item, int x, int y) {
            if (item == null || IsOutOfBounds(x, y, item.CurrentWidth, item.CurrentHeight)) return;
            
            // 在数组中登记
            for (int i = x; i < x + item.CurrentWidth; i++) {
                for (int j = y; j < y + item.CurrentHeight; j++) {
                    gridSlots[i, j] = item;
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
            
            // 检查物品位置是否有效
            if (IsOutOfBounds(item.posX, item.posY, item.CurrentWidth, item.CurrentHeight)) return;
            
            // 清除数组中的引用
            for (int i = item.posX; i < item.posX + item.CurrentWidth; i++) {
                for (int j = item.posY; j < item.posY + item.CurrentHeight; j++) {
                    gridSlots[i, j] = null;
                }
            }
        }
        
        /// <summary> 
        /// 检查物品旋转后是否合法（不修改实际数据） 
        /// </summary> 
        public bool CheckRotateValidity(ItemInstance item) 
        { 
            // 1. 获取旋转后的预期尺寸 
            // 注意：这里取反来模拟旋转后的宽 
            int newW = item.isRotated ? item.data.width : item.data.height; 
            int newH = item.isRotated ? item.data.height : item.data.width; 
    
            // 2. 暂时移除物品（为了避免检测时和自己当前的占用冲突） 
            RemoveItem(item); 
    
            // 3. 检查位置是否可用 
            bool canPlace = CanPlace(item.posX, item.posY, newW, newH); 
    
            // 4. 无论结果如何，先把物品放回去（保持状态原样） 
            // 注意：这时用 item.CurrentWidth/Height 还是原来的尺寸，因为我们还没改 isRotated 
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
                    ItemInstance itemAtPos = gridSlots[i, j];
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
            return gridSlots[x, y];
        }
        
        /// <summary>
        /// 清空网格数据
        /// </summary>
        public void ClearGrid() {
            gridSlots = new ItemInstance[width, height];
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

