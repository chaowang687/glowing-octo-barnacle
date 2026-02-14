using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Bag
{
    /// <summary>
    /// 形状预览组件，用于显示异形物品的放置预览
    /// </summary>
    public class InventoryGridShapePreview : MonoBehaviour
    {
        [Header("预览设置")]
        public Transform previewParent; // 预览格子的父对象
        public GameObject previewCellPrefab; // 预览格子预制体
        public Color validColor = new Color(0, 1, 0, 0.3f); // 可放置颜色
        public Color invalidColor = new Color(1, 0, 0, 0.3f); // 不可放置颜色
        public Color emptyColor = new Color(0, 0, 0, 0); // 未占用格子颜色
        
        private Dictionary<Vector2Int, GameObject> previewCells = new Dictionary<Vector2Int, GameObject>();
        private InventoryGrid grid;
        
        private void Awake()
        {
            grid = GetComponent<InventoryGrid>();
            
            // 确保预览父对象存在
            if (previewParent == null)
            {
                previewParent = new GameObject("PreviewCells").transform;
                previewParent.SetParent(transform);
                previewParent.localPosition = Vector3.zero;
                previewParent.localRotation = Quaternion.identity;
                previewParent.localScale = Vector3.one;
            }
        }
        
        /// <summary>
        /// 显示异形物品放置预览
        /// </summary>
        public void ShowShapePreview(ItemInstance item, Vector2Int gridPos, bool isValid)
        {
            if (item == null)
            {
                HidePreview();
                return;
            }
            
            // 如果没有预览预制体，创建一个基本的Image
            if (previewCellPrefab == null)
            {
                CreateDefaultPreviewCellPrefab();
            }
            
            // 获取实际形状
            bool[,] shape = item.GetActualShape();
            int shapeWidth = shape.GetLength(0);
            int shapeHeight = shape.GetLength(1);
            
            // 首先隐藏所有旧的预览格子
            HidePreview();
            
            // 生成新的预览格子
            for (int x = 0; x < shapeWidth; x++)
            {
                for (int y = 0; y < shapeHeight; y++)
                {
                    Vector2Int localPos = new Vector2Int(x, y);
                    Vector2Int worldPos = gridPos + localPos;
                    
                    // 检查是否在网格边界内
                    bool isInBounds = worldPos.x >= 0 && worldPos.x < grid.width && worldPos.y >= 0 && worldPos.y < grid.height;
                    
                    // 只有占用的格子才需要生成预览
                    if (shape[x, y])
                    {
                        // 创建或获取预览格子
                        GameObject cellObj;
                        if (!previewCells.TryGetValue(localPos, out cellObj))
                        {
                            cellObj = Instantiate(previewCellPrefab, previewParent);
                            previewCells[localPos] = cellObj;
                            
                            // 设置格子大小
                            RectTransform cellRect = cellObj.GetComponent<RectTransform>();
                            if (cellRect != null)
                            {
                                cellRect.sizeDelta = new Vector2(grid.cellSize, grid.cellSize);
                            }
                        }
                        
                        // 设置格子位置
                        RectTransform cellTransform = cellObj.GetComponent<RectTransform>();
                        if (cellTransform != null)
                        {
                            // 获取格子左下角位置
                            Vector2 cellBottomLeft = grid.GetPositionFromGrid(worldPos.x, worldPos.y);
                            
                            // 计算格子中心位置，确保预览格子居中显示
                            Vector2 cellCenter = cellBottomLeft + new Vector2(grid.cellSize / 2f, -grid.cellSize / 2f);
                            cellTransform.anchoredPosition = cellCenter;
                        }
                        
                        // 设置格子颜色
                        Image cellImage = cellObj.GetComponent<Image>();
                        if (cellImage != null)
                        {
                            // 占用格子
                            if (isInBounds && isValid)
                            {
                                cellImage.color = validColor;
                            }
                            else
                            {
                                cellImage.color = invalidColor;
                            }
                        }
                        
                        // 显示格子
                        cellObj.SetActive(true);
                    }
                }
            }
        }
        
        /// <summary>
        /// 隐藏预览
        /// </summary>
        public void HidePreview()
        {
            foreach (var cell in previewCells.Values)
            {
                if (cell != null)
                {
                    cell.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 清除所有预览格子
        /// </summary>
        public void ClearPreview()
        {
            foreach (var cell in previewCells.Values)
            {
                if (cell != null)
                {
                    Destroy(cell);
                }
            }
            previewCells.Clear();
        }
        
        /// <summary>
        /// 创建默认的预览格子预制体
        /// </summary>
        private void CreateDefaultPreviewCellPrefab()
        {
            // 创建一个新的GameObject作为预览预制体
            GameObject prefab = new GameObject("PreviewCell");
            
            // 显式添加RectTransform组件（新建GameObject默认只有Transform）
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(grid.cellSize, grid.cellSize);
            
            // 设置锚点和Pivot，确保预览格子居中显示在格子内
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // 添加Image组件
            Image image = prefab.AddComponent<Image>();
            image.color = validColor;
            image.raycastTarget = false; // 确保预览格子不接收射线检测，不干扰Slot的点击判定
            
            // 设置为预制体
            previewCellPrefab = prefab;
            
            // 设置为不保存到场景
            prefab.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
