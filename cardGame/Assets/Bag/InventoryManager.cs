using UnityEngine;
using System.Collections.Generic;



namespace Bag
{
public class InventoryManager : MonoBehaviour {
    public static InventoryManager Instance;
    // 【新增】用于记录当前背包内所有物品的列表，供存档系统使用
    public List<ItemInstance> allItemsInBag = new List<ItemInstance>();
    public GameObject itemPrefab; // 在 Inspector 中拖入预制体
    // 存储当前正在拖拽的物品
    public ItemUI CarriedItem; 
    public InventoryGrid CurrentGrid;
    // InventoryManager.cs 中添加一个引用
public Transform itemContainer;

    void Awake() { Instance = this; }

    // 尝试将物品放入网格
    public bool TryPlace(ItemInstance item, int x, int y, InventoryGrid targetGrid) {
        // 1. 检查空间是否足够
        if (targetGrid.CanPlace(x, y, item.CurrentWidth, item.CurrentHeight)) {
            targetGrid.PlaceItem(item, x, y);
            // 【新增】如果列表中没有，则添加
            if (!allItemsInBag.Contains(item)) {
                allItemsInBag.Add(item);
            }
            return true;
        }
        // 【新增】当物品被丢弃或移出时，记得从列表中移除
        
        // 2. 尝试交换逻辑
        ItemInstance overlapItem = targetGrid.GetOverlapItem(x, y, item.CurrentWidth, item.CurrentHeight);
        if (overlapItem != null) {
            // 移除旧物品，并把旧物品变成“被抓起”状态
            targetGrid.RemoveItem(overlapItem);
            targetGrid.PlaceItem(item, x, y);
            
            // 这里的核心：让 UI 层把 overlapItem 重新实例化并跟随鼠标
            PickUpItem(overlapItem); 
            return true;
        }

        return false;
    }

    public void RemoveFromTracker(ItemInstance item)
        {
            if (allItemsInBag.Contains(item)) {
                allItemsInBag.Remove(item);
            }
        }
    public void SaveInventory()
{
    InventorySaveData saveData = new InventorySaveData();
    
    // 假设你有一个列表记录了当前格子里所有的 ItemInstance
    foreach (var item in allItemsInBag) 
    {
        saveData.items.Add(new ItemSaveEntry {
            itemID = item.data.itemName, // 或者使用唯一的 GUID
            posX = item.posX,
            posY = item.posY,
            isRotated = item.isRotated
        });
    }

    string json = JsonUtility.ToJson(saveData, true);
    System.IO.File.WriteAllText(Application.persistentDataPath + "/inventory.json", json);
    Debug.Log("存档已保存至: " + Application.persistentDataPath);
}
public void LoadInventory(InventoryGrid grid)
{
    string path = Application.persistentDataPath + "/inventory.json";
    if (!System.IO.File.Exists(path)) return;

    string json = System.IO.File.ReadAllText(path);
    InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

    foreach (var entry in saveData.items)
    {
        // 1. 根据 ID 加载配置 (这里建议使用 Resources.Load 或 Addressables)
        ItemData data = Resources.Load<ItemData>("Items/" + entry.itemID);
        
        // 2. 创建实例
        ItemInstance newItem = new ItemInstance(data) {
            posX = entry.posX,
            posY = entry.posY,
            isRotated = entry.isRotated
        };

        // 3. 实例化 UI 并对齐
        GameObject go = Instantiate(itemPrefab, grid.transform);
        ItemUI ui = go.GetComponent<ItemUI>();
        ui.itemInstance = newItem;
        
        ui.Initialize(newItem, grid.cellSize); // 必须调用初始化
        grid.PlaceItem(newItem, entry.posX, entry.posY); // 注册到网格数组
        ui.SnapToGrid(grid, new Vector2Int(entry.posX, entry.posY)); // 视觉对齐
    }
}
    public void LoadInventory(List<ItemInstance> savedData, InventoryGrid mainGrid, Transform gridTransform) {
        foreach(var data in savedData) {
            GameObject go = Instantiate(itemPrefab, gridTransform);
            ItemUI ui = go.GetComponent<ItemUI>();
            ui.itemInstance = data;
            // 修正：调用 UI 的对齐方法
            ui.SnapToGrid(mainGrid, new Vector2Int(data.posX, data.posY));
        }
    }
        private void PickUpItem(ItemInstance item) {
        // 1. 查找该物品对应的 UI 对象
        // 技巧：实际开发中建议在 ItemInstance 里保存对应的 ItemUI 引用，或者通过 GameObject.Find 查找
        ItemUI targetUI = FindUIForItem(item); 
        
        if (targetUI != null) {
            // 2. 模拟拖拽开始的操作
            targetUI.OnBeginDrag(null); // 触发变透明、关闭射线检测
            
            // 3. 将该 UI 设为当前跟随鼠标的对象（需根据你的输入系统微调）
            // 这样在下一次 Update 时，这个被替换的物品就会跟着鼠标走
        }
    }

    // 辅助方法示例
    private ItemUI FindUIForItem(ItemInstance item) {
        ItemUI[] allUIs = FindObjectsOfType<ItemUI>();
        foreach(var ui in allUIs) {
            if(ui.itemInstance == item) return ui;
        }
        return null;
    }

}
}