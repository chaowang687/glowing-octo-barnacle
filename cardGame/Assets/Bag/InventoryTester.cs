using UnityEngine;
using Bag; // 确保引用了你的命名空间

public class InventoryTester : MonoBehaviour
{
    public ItemData testItemData; // 在 Inspector 中拖入一个 ItemData (ScriptableObject)
    public KeyCode spawnKey = KeyCode.Space;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;

    void Update()
    {
        // 1. 测试生成物品
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnTestItem();
        }

        // 2. 测试存档
        if (Input.GetKeyDown(saveKey))
        {
            InventoryManager.Instance.SaveInventory();
            Debug.Log("Inventory Saved!");
        }

        // 3. 测试读档
        if (Input.GetKeyDown(loadKey))
        {
            // 注意：Load 之前建议先清理当前场景已有的 ItemUI，防止重叠
            InventoryManager.Instance.LoadInventory(InventoryManager.Instance.CurrentGrid);
            Debug.Log("Inventory Loaded!");
        }
    }

    void SpawnTestItem()
    {
        if (testItemData == null) {
            Debug.LogError("请先在 Inspector 中拖入 Test Item Data!");
            return;
        }

        // 创建数据实例
        ItemInstance newItem = new ItemInstance(testItemData);

        // InventoryTester.cs 修改实例化这一行
        GameObject go = Instantiate(InventoryManager.Instance.itemPrefab, InventoryManager.Instance.itemContainer);
        // 2. 然后声明并获取 ui 变量 (关键：这一行必须在最前面)
        ItemUI ui = go.GetComponent<ItemUI>();
        
        // 3. 接着赋值数据
        ui.itemInstance = newItem;
        
        // 4. 最后调用你刚才写的 UpdateVisual
        ui.UpdateVisual();
        Debug.Log($"物品大小已设置为: {ui.GetComponent<RectTransform>().sizeDelta}");

        // 让新生成的物品跟随鼠标，或者直接放在 (0,0)
        Vector2 mouseLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            InventoryManager.Instance.CurrentGrid.transform as RectTransform, 
            Input.mousePosition, 
            null, 
            out mouseLocalPos);
        
        ui.GetComponent<RectTransform>().anchoredPosition = mouseLocalPos;
        InventoryManager.Instance.allItemsInBag.Add(newItem);
        // 【关键修改】不要手动调用 OnBeginDrag(null)

        // 如果你想让它一生成就粘在鼠标上，需要在 ItemUI 里加个 Public 方法
    ui.StartManualDrag();
    }
}