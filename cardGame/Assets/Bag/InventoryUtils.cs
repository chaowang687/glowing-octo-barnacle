using UnityEngine;

namespace Bag
{  // 将逻辑工具类移出 ItemInstance
    public static class InventoryLookup {
        public static bool IsAreaAvailable(ItemInstance[,] grid, int startX, int startY, int w, int h, int gridW, int gridH) {
            for (int x = startX; x < startX + w; x++) {
                for (int y = startY; y < startY + h; y++) {
                    // 修正：这里需要检查边界
                    if (x < 0 || y < 0 || x >= gridW || y >= gridH || grid[x, y] != null) return false;
                }
            }
            return true;
        }
    }
}
