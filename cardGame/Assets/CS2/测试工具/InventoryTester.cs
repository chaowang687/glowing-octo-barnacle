using UnityEngine;
using System.Collections;

namespace ScavengingGame
{
    public class InventoryTester : MonoBehaviour
    {
        [Header("测试物品")]
        public ItemData testItem;
        public EquipmentData testEquipment;
        
        [Header("测试设置")]
        public bool autoTest = true;
        public float testDelay = 1f;
        
        void Start()
        {
            if (autoTest)
            {
                StartCoroutine(TestInventory());
            }
        }
        
         IEnumerator TestInventory()
        {
            yield return new WaitForSeconds(testDelay);
            
            if (GameStateManager.Instance == null || 
                GameStateManager.Instance.PlayerInventory == null)
            {
                Debug.LogError("GameStateManager 或 PlayerInventory 未找到");
                yield break;
            }
            
            IInventoryService inventory = GameStateManager.Instance.PlayerInventory;
            
            // 测试添加物品
            if (testItem != null)
            {
                bool added = inventory.AddItem(testItem, 1);
                Debug.Log($"添加物品 {testItem.ItemName}: {(added ? "成功" : "失败")}");
            }
            else
            {
                Debug.LogWarning("测试物品未设置");
            }
            
            // 测试添加装备
            if (testEquipment != null)
            {
                bool added = inventory.AddItem(testEquipment, 1);
                Debug.Log($"添加装备 {testEquipment.ItemName}: {(added ? "成功" : "失败")}");
                
                // 如果添加成功，尝试装备（确保装备不为null）
                if (added)
                {
                    // 等待一帧，确保物品已添加
                    yield return null;
                    
                    // 尝试装备
                    bool equipped = inventory.EquipItem(testEquipment);
                    Debug.Log($"装备 {testEquipment.ItemName}: {(equipped ? "成功" : "失败")}");
                }
            }
            else
            {
                Debug.LogWarning("测试装备未设置");
            }
            
            // 打印背包内容 - 使用类型安全的方式
            if (inventory is InventoryManager inventoryManager)
            {
                inventoryManager.LogInventory();
            }
            else
            {
                // 备选方案：使用接口提供的信息
                var items = inventory.GetAllItems();
                Debug.Log($"背包中共有 {items.Count} 组物品");
                foreach (var stack in items)
                {
                    if (stack != null && stack.Item != null)
                        Debug.Log($"  {stack.Item.ItemName}: {stack.Count}个");
                }
            }
            
            // 计算装备加成
            var bonuses = inventory.CalculateEquipmentBonuses();
            Debug.Log($"装备总加成 - 攻击: {bonuses.attack}, 防御: {bonuses.defense}");
        }
        
        // 手动测试方法
        public void ManualTest()
        {
            StartCoroutine(TestInventory());
        }
    }
}