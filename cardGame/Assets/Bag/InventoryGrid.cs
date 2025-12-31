
using UnityEngine;
namespace Bag
{
public class InventoryGrid : MonoBehaviour {
    public int width = 10;
    public int height = 8;
    public float cellSize = 50f;
    private ItemInstance[,] gridSlots;
    
    public RectTransform ghostPreview; // 在 Inspector 中拖入预览用的 Image

    void Awake() {
        gridSlots = new ItemInstance[width, height];
    }
    public void ClearPreview() => ghostPreview?.gameObject.SetActive(false);
// 报错修正：添加缺失的坐标转换方法
    public Vector2 GetPositionFromGrid(int x, int y) {
        return new Vector2(x * cellSize, -y * cellSize);
    }
    // 在 InventoryGrid 类中添加以下方法
    // 将本地坐标转换为网格坐标
    public Vector2Int GetGridFromPosition(Vector2 localPos) {
        // 假设 Pivot 在左上角 (0,1)
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(-localPos.y / cellSize);
        return new Vector2Int(x, y);
    }
    // 在 InventoryGrid.cs 中添加
    public void ShowPlacementPreview(ItemInstance item, Vector2 localPos) {
        Vector2Int gridPos = GetGridFromPosition(localPos);
        
        // 检查是否在边界内
        bool canPlace = !IsOutOfBounds(gridPos.x, gridPos.y, item.CurrentWidth, item.CurrentHeight);
        
        // 如果在边界内，进一步检查重叠
        if (canPlace) {
            // 这里可以调用之前的 GetOverlapItem 来判断是显示绿色（可放/交换）还是红色（不可放）
            ItemInstance overlap = GetOverlapItem(gridPos.x, gridPos.y, item.CurrentWidth, item.CurrentHeight);
            // 逻辑：如果没有重叠，或者只有一个重叠（触发交换），则设为绿色
            DrawPreviewGhost(gridPos, item.CurrentWidth, item.CurrentHeight, true); 
        } else {
            DrawPreviewGhost(gridPos, item.CurrentWidth, item.CurrentHeight, false);
        }
    }
    

    // 实际渲染可以使用一个简单的 UI Image，或者动态修改格子的颜色
    private void DrawPreviewGhost(Vector2Int pos, int w, int h, bool isValid) {
            // 建议在网格下预留一个名为 "Ghost" 的 UI 节点，动态修改它的 Size 和 Position
            
            if (ghostPreview == null) return;

            ghostPreview.gameObject.SetActive(true);
            // 设置大小
            ghostPreview.sizeDelta = new Vector2(w * cellSize, h * cellSize);
            // 设置位置
            ghostPreview.anchoredPosition = GetPositionFromGrid(pos.x, pos.y);
            // 设置颜色：合法为绿，非法为红
            ghostPreview.GetComponent<UnityEngine.UI.Image>().color = isValid ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
    
    }


    // 修正：ItemUI 中调用的是 GetPositionFromGrid(int, int, int, int)
    // 增加一个重载版本以匹配调用
    public Vector2 GetPositionFromGrid(int x, int y, int w, int h) {
        return new Vector2(x * cellSize, -y * cellSize);
    }
    // 边界检查方法
    public bool IsOutOfBounds(int x, int y, int w, int h) {
        return x < 0 || y < 0 || x + w > width || y + h > height;
    }
    public bool CanPlace(int x, int y, int w, int h) {
        if (x < 0 || y < 0 || x + w > width || y + h > height) return false;
        for (int i = x; i < x + w; i++) {
            for (int j = y; j < y + h; j++) {
                if (gridSlots[i, j] != null) return false;
            }
        }
        return true;
    }

    public void PlaceItem(ItemInstance item, int x, int y) {
        // 在数组中登记
        for (int i = x; i < x + item.CurrentWidth; i++) {
            for (int j = y; j < y + item.CurrentHeight; j++) {
                gridSlots[i, j] = item;
            }
        }
        item.posX = x;
        item.posY = y;
    }

    public void RemoveItem(ItemInstance item) {
        for (int i = item.posX; i < item.posX + item.CurrentWidth; i++) {
            for (int j = item.posY; j < item.posY + item.CurrentHeight; j++) {
                gridSlots[i, j] = null;
            }
        }
    }

    // 获取目标区域唯一的重叠物品
    public ItemInstance GetOverlapItem(int x, int y, int w, int h) {
        ItemInstance found = null;
        for (int i = x; i < x + w; i++) {
            for (int j = y; j < y + h; j++) {
                if (gridSlots[i, j] != null) {
                    if (found == null) found = gridSlots[i, j];
                    else if (found != gridSlots[i, j]) return null; // 超过一个物品，不可交换
                }
            }
        }
        return found;
    }
}

}
