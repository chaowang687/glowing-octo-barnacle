// GridItemSpawnTester.cs
using UnityEngine;
using Bag;

/// <summary>
/// 网格物品生成测试器，用于测试物品生成和背包功能
/// </summary>
public class GridItemSpawnTester : MonoBehaviour
{
    [Header("物品数据")]
    public ItemData[] itemDatas;  // 可以在Inspector中拖入多个ItemData
    
    [Header("生成设置")]
    public KeyCode spawnKey = KeyCode.Space;
    public KeyCode spawnRandomKey = KeyCode.R;
    public KeyCode spawnAtMouseKey = KeyCode.M;
    public KeyCode clearAllKey = KeyCode.C;
    public KeyCode debugKey = KeyCode.D;
    
    [Header("生成位置")]
    public bool spawnInGrid = true;
    public Vector2Int spawnGridPosition = new Vector2Int(0, 0);
    public bool autoFindSpace = true;
    
    [Header("性能设置")]
    [SerializeField] private bool showGUIPanel = true; // 是否显示GUI面板
    
    private void Update()
    {
        // 1. 生成预设物品
        if (Input.GetKeyDown(spawnKey))
        {
            if (itemDatas != null && itemDatas.Length > 0)
            {
                SpawnItemInGrid(itemDatas[0]);
            }
            else
            {
                Debug.LogWarning("请先在Inspector中设置ItemDatas数组！");
            }
        }
        
        // 2. 随机生成物品
        if (Input.GetKeyDown(spawnRandomKey))
        {
            if (itemDatas != null && itemDatas.Length > 0)
            {
                int randomIndex = Random.Range(0, itemDatas.Length);
                SpawnItemInGrid(itemDatas[randomIndex]);
            }
        }
        
        // 3. 在鼠标位置生成
        if (Input.GetKeyDown(spawnAtMouseKey))
        {
            if (itemDatas != null && itemDatas.Length > 0)
            {
                SpawnItemAtMouse(itemDatas[0]);
            }
        }
        
        // 4. 清空所有物品
        if (Input.GetKeyDown(clearAllKey))
        {
            ClearAllItems();
        }
        
        // 5. 调试信息
        if (Input.GetKeyDown(debugKey))
        {
            DebugInventoryInfo();
        }
    }
    
    /// <summary>
    /// 在网格中生成物品（自动寻找空位）
    /// </summary>
    /// <param name="itemData">物品数据</param>
    public void SpawnItemInGrid(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("ItemData为空！");
            return;
        }
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager未找到！");
            return;
        }
        
        // 使用InventoryManager的通用方法生成物品
        InventoryManager.Instance.SpawnItem(itemData, spawnGridPosition, autoFindSpace);
    }
    
    /// <summary>
    /// 在鼠标位置生成物品（立即进入拖拽状态）
    /// </summary>
    /// <param name="itemData">物品数据</param>
    public void SpawnItemAtMouse(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("ItemData为空！");
            return;
        }
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager未找到！");
            return;
        }
        
        // 使用InventoryManager的通用方法生成物品
        InventoryManager.Instance.SpawnItemAtMouse(itemData);
    }
    
    /// <summary>
    /// 清空所有物品
    /// </summary>
    public void ClearAllItems()
    {
        if (InventoryManager.Instance == null) return;
        
        // 销毁所有ItemUI
        ItemUI[] allItems = FindObjectsOfType<ItemUI>();
        int itemCount = 0;
        
        foreach (ItemUI item in allItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
                itemCount++;
            }
        }
        
        // 清空列表
        InventoryManager.Instance.allItemsInBag.Clear();
        
        // 清空网格数据（使用直接调用，而非反射）
        if (InventoryManager.Instance.CurrentGrid != null)
        {
            InventoryManager.Instance.CurrentGrid.ClearGrid();
        }
        
        Debug.Log($"已清空 {itemCount} 个物品");
    }
    
    /// <summary>
    /// 显示背包调试信息
    /// </summary>
    private void DebugInventoryInfo()
    {
        if (InventoryManager.Instance == null) return;
        
        Debug.Log("=== 背包调试信息 ===");
        Debug.Log($"物品数量: {InventoryManager.Instance.allItemsInBag.Count}");
        
        // 显示每个物品的信息
        foreach (ItemInstance item in InventoryManager.Instance.allItemsInBag)
        {
            if (item != null && item.data != null)
            {
                Debug.Log($"- {item.data.itemName}: 位置({item.posX},{item.posY}), " +
                         $"尺寸{item.CurrentWidth}x{item.CurrentHeight}, 旋转{item.isRotated}");
            }
        }
        
        // 显示网格占用情况
        if (InventoryManager.Instance.CurrentGrid != null)
        {
            InventoryGrid grid = InventoryManager.Instance.CurrentGrid;
            Debug.Log($"网格尺寸: {grid.width}x{grid.height}, 单元格大小: {grid.cellSize}");
        }
        
        Debug.Log("===================");
    }
    
    /// <summary>
    /// 可视化UI按钮（在场景中显示）
    /// </summary>
    private void OnGUI()
    {
        if (!showGUIPanel) return; // 可以关闭GUI面板以提高性能
        
        if (itemDatas == null || itemDatas.Length == 0) return;
        
        // 限制GUI渲染区域
        GUILayout.BeginArea(new Rect(10, 10, 200, 400));
        
        // 使用GUILayout.BeginScrollView添加滚动条
        GUILayout.BeginScrollView(Vector2.zero);
        
        GUILayout.Box("背包测试面板");
        
        GUILayout.Label("控制:");
        GUILayout.Label("空格: 生成第一个物品");
        GUILayout.Label("R: 随机生成物品");
        GUILayout.Label("M: 在鼠标位置生成");
        GUILayout.Label("C: 清空所有物品");
        GUILayout.Label("D: 显示调试信息");
        
        GUILayout.Space(10);
        GUILayout.Label("快速生成:");
        
        // 显示所有物品的生成按钮
        foreach (ItemData itemData in itemDatas)
        {
            if (itemData != null && GUILayout.Button($"生成 {itemData.itemName}"))
            {
                SpawnItemInGrid(itemData);
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label("当前物品:");
        GUILayout.Label($"数量: {InventoryManager.Instance?.allItemsInBag.Count}");
        
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}