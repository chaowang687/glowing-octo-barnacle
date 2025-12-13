using UnityEngine;
using System.Collections;


namespace ScavengingGame // 将整个类包裹在命名空间内
{

public class InventoryTester : MonoBehaviour
{
    public ItemData testItem;
    public EquipmentData testEquipment;
    
    void Start()
    {
        StartCoroutine(TestInventory());
    }
    
    IEnumerator TestInventory()
    {
        // 等待一帧确保所有管理器已初始化
        yield return null;
        
        if (GameStateManager.Instance == null || 
            GameStateManager.Instance.PlayerInventory == null)
        {
            Debug.LogError("GameStateManager 或 PlayerInventory 未找到");
            yield break;
        }
        
        InventoryManager inventory = GameStateManager.Instance.PlayerInventory;
        
        // 测试添加物品
        if (testItem != null)
        {
            bool added = inventory.AddItem(testItem);
            Debug.Log($"添加物品 {testItem.ItemName}: {(added ? "成功" : "失败")}");
        }
        
        // 测试添加装备
        if (testEquipment != null)
        {
            bool added = inventory.AddItem(testEquipment);
            Debug.Log($"添加装备 {testEquipment.ItemName}: {(added ? "成功" : "失败")}");
            
            // 如果添加成功，尝试装备
            if (added)
            {
                inventory.EquipItem(testEquipment);
            }
        }
        
        // 打印背包内容
        inventory.LogInventory();
        
        // 计算装备加成
        var bonuses = inventory.CalculateEquipmentBonuses();
        Debug.Log($"装备总加成 - 攻击: {bonuses.attack}, 防御: {bonuses.defense}");
    }
}
}