using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Bag; // 确保引用了你的命名空间

public class AutomatedTest : MonoBehaviour
{
    [SerializeField] private ItemData[] testItems;
    [SerializeField] private float testDelay = 1f;
    [SerializeField] private bool runAutomatedTests = false;
    
    private List<ItemUI> spawnedItems = new List<ItemUI>();
    
    void Start() {
        if (runAutomatedTests) {
            StartCoroutine(RunAllTests());
        }
    }
    
    IEnumerator RunAllTests() {
        yield return new WaitForSeconds(1f);
        
        // 测试1：生成物品
        Debug.Log("=== 测试1：生成物品 ===");
        yield return StartCoroutine(TestSpawnItems());
        
        // 测试2：拖拽放置
        Debug.Log("=== 测试2：拖拽放置 ===");
        yield return StartCoroutine(TestDragAndDrop());
        
        // 测试3：存档/读档
        Debug.Log("=== 测试3：存档测试 ===");
        yield return StartCoroutine(TestSaveLoad());
        
        Debug.Log("=== 所有测试完成 ===");
    }
    
    IEnumerator TestSpawnItems() {
        Debug.Log("开始生成测试物品...");
        
        if (testItems == null || testItems.Length == 0) {
            Debug.LogError("请先在Inspector中设置Test Items数组！");
            yield break;
        }
        
        for(int i = 0; i < Mathf.Min(3, testItems.Length); i++) {
            // 使用InventoryManager的通用方法生成物品
            ItemUI ui = InventoryManager.Instance.SpawnItem(testItems[i], Vector2Int.zero, true);
            if (ui != null)
            {
                spawnedItems.Add(ui);
            }
            
            yield return new WaitForSeconds(testDelay);
        }
    }
    
    IEnumerator TestDragAndDrop() {
        Debug.Log("开始拖拽测试...");
        
        if (spawnedItems.Count == 0) {
            Debug.Log("没有物品可测试拖拽，跳过...");
            yield break;
        }
        
        // 模拟拖拽第一个物品到网格中
        ItemUI firstItem = spawnedItems[0];
        Debug.Log($"正在拖拽物品: {firstItem.itemInstance.data.itemName}");
        
        // 模拟开始拖拽
        firstItem.StartManualDrag();
        
        // 移动到网格中的一个位置 (比如位置(1,1))
        InventoryGrid grid = InventoryManager.Instance.CurrentGrid;
        Vector2 targetPos = grid.GetPositionFromGrid(1, 1);
        firstItem.GetComponent<RectTransform>().anchoredPosition = targetPos;
        
        yield return new WaitForSeconds(0.5f);
        
        // 模拟结束拖拽 - 尝试放置
        Vector2Int gridPos = grid.GetGridFromPosition(targetPos);
        bool canPlace = grid.CanPlace(gridPos.x, gridPos.y, 
            firstItem.itemInstance.CurrentWidth, firstItem.itemInstance.CurrentHeight);
        
        if (canPlace) {
            Debug.Log($"可以放置物品在 ({gridPos.x}, {gridPos.y})");
            grid.PlaceItem(firstItem.itemInstance, gridPos.x, gridPos.y);
            firstItem.SnapToGrid(grid, gridPos);
            Debug.Log("物品放置成功！");
        } else {
            Debug.Log($"无法放置物品在 ({gridPos.x}, {gridPos.y})");
        }
        
        yield return new WaitForSeconds(testDelay);
    }
    

    
    IEnumerator TestSaveLoad() {
        Debug.Log("开始存档/读档测试...");
        
        // 先保存当前状态
        Debug.Log("保存存档...");
        InventoryManager.Instance.SaveInventory();
        
        yield return new WaitForSeconds(0.5f);
        
        // 清空当前物品
        Debug.Log("清空当前物品...");
        foreach (var item in spawnedItems) {
            if (item != null) {
                Destroy(item.gameObject);
            }
        }
        spawnedItems.Clear();
        InventoryManager.Instance.ClearInventory(InventoryManager.Instance.CurrentGrid);
        
        // 重新加载
        Debug.Log("加载存档...");
        InventoryManager.Instance.LoadInventory(InventoryManager.Instance.CurrentGrid);
        
        // 检查加载的物品数量
        int loadedCount = InventoryManager.Instance.AllItemsInBag.Count;
        Debug.Log($"加载了 {loadedCount} 个物品");
        
        yield return new WaitForSeconds(testDelay);
    }
    
    // 可视化调试方法
    void OnDrawGizmos() {
        if (!Application.isPlaying) return;
        
        // 绘制背包网格
        InventoryGrid grid = InventoryManager.Instance.CurrentGrid;
        if (grid == null) return;
        
        Gizmos.color = Color.cyan;
        for (int x = 0; x < grid.width; x++) {
            for (int y = 0; y < grid.height; y++) {
                Vector3 worldPos = grid.transform.TransformPoint(
                    new Vector3(x * grid.cellSize + grid.cellSize/2, 
                               -y * grid.cellSize - grid.cellSize/2, 0));
                Gizmos.DrawWireCube(worldPos, new Vector3(grid.cellSize, grid.cellSize, 1));
            }
        }
    }
}