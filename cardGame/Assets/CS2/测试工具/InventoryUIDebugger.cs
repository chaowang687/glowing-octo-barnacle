using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace ScavengingGame
{
    public class InventoryUIDebugger : MonoBehaviour
    {
        [Header("UI 组件")]
        public GameObject inventoryPanel;
        public Transform slotsContainer;
        
        [Header("调试设置")]
        public bool autoCheckOnStart = true;
        public bool logEmptySlots = false;
        
        private List<ItemSlotUI> slotUIs = new List<ItemSlotUI>();
        private InventoryManager inventoryManager;
        
        void Start()
        {
            if (autoCheckOnStart)
            {
                StartCoroutine(DelayedCheck());
            }
        }
        
        System.Collections.IEnumerator DelayedCheck()
        {
            yield return new WaitForSeconds(1f);
            CheckInventoryUI();
        }
        
        [ContextMenu("检查背包UI")]
        public void CheckInventoryUI()
        {
            Debug.Log("=== 开始检查背包UI ===");
            
            // 1. 检查背包面板
            if (inventoryPanel == null)
            {
                inventoryPanel = GameObject.Find("InventoryPanel");
                if (inventoryPanel == null)
                {
                    Debug.LogError("未找到背包面板 (InventoryPanel)");
                    return;
                }
            }
            
            Debug.Log($"背包面板: {inventoryPanel.name}, 激活状态: {inventoryPanel.activeSelf}");
            
            // 2. 检查槽位容器
            if (slotsContainer == null)
            {
                slotsContainer = inventoryPanel.transform.Find("SlotsContainer");
                if (slotsContainer == null)
                {
                    Debug.LogError("未找到槽位容器 (SlotsContainer)");
                    return;
                }
            }
            
            Debug.Log($"槽位容器: {slotsContainer.name}, 子物体数量: {slotsContainer.childCount}");
            
            // 3. 收集所有ItemSlotUI组件
            slotUIs.Clear();
            for (int i = 0; i < slotsContainer.childCount; i++)
            {
                ItemSlotUI slotUI = slotsContainer.GetChild(i).GetComponent<ItemSlotUI>();
                if (slotUI != null)
                {
                    slotUIs.Add(slotUI);
                }
            }
            
            Debug.Log($"找到 {slotUIs.Count} 个ItemSlotUI组件");
            
            // 4. 检查每个槽位
            int emptySlots = 0;
            int occupiedSlots = 0;
            
            foreach (var slotUI in slotUIs)
            {
                if (slotUI.IsEmpty)
                {
                    emptySlots++;
                    if (logEmptySlots)
                    {
                        Debug.Log($"槽位 {slotUI.SlotIndex}: 空");
                    }
                }
                else
                {
                    occupiedSlots++;
                    Debug.Log($"槽位 {slotUI.SlotIndex}: {slotUI.CurrentItem.itemName} x{slotUI.ItemCount} (图标: {slotUI.CurrentItem.icon})");
                    
                    // 检查图标是否显示
                    Image iconImage = slotUI.GetComponentInChildren<Image>();
                    if (iconImage != null)
                    {
                        Debug.Log($"  图标组件: sprite={iconImage.sprite}, enabled={iconImage.enabled}, color={iconImage.color}");
                    }
                }
            }
            
            Debug.Log($"空槽位: {emptySlots}, 占用槽位: {occupiedSlots}");
            
            // 5. 检查背包管理器
            if (inventoryManager == null)
            {
                inventoryManager = FindFirstObjectByType<InventoryManager>();
            }
            
            if (inventoryManager != null)
            {
                Debug.Log($"找到InventoryManager: {inventoryManager.name}");
                inventoryManager.LogInventory();
                
                // 比较UI状态和实际库存状态
                var inventoryItems = inventoryManager.GetAllItems();
                Debug.Log($"实际库存物品数: {inventoryItems.Count}");
                
                foreach (var stack in inventoryItems)
                {
                    Debug.Log($"  库存槽位{stack.SlotIndex}: {stack.Item.itemName} x{stack.Count}");
                    
                    // 检查UI是否显示正确
                    if (stack.SlotIndex >= 0 && stack.SlotIndex < slotUIs.Count)
                    {
                        var uiSlot = slotUIs[stack.SlotIndex];
                        if (uiSlot.CurrentItem == null)
                        {
                            Debug.LogError($"  UI槽位{stack.SlotIndex}没有显示物品，但库存中有 {stack.Item.itemName}");
                        }
                        else if (uiSlot.CurrentItem.itemName != stack.Item.itemName)
                        {
                            Debug.LogError($"  UI槽位{stack.SlotIndex}显示 {uiSlot.CurrentItem.itemName}，但库存中是 {stack.Item.itemName}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("未找到InventoryManager");
            }
            
            Debug.Log("=== 背包UI检查完成 ===");
        }
        
        [ContextMenu("强制刷新所有槽位")]
        public void ForceRefreshAllSlots()
        {
            if (inventoryManager == null)
            {
                inventoryManager = FindFirstObjectByType<InventoryManager>();
                if (inventoryManager == null)
                {
                    Debug.LogError("未找到InventoryManager");
                    return;
                }
            }
            
            // 获取所有物品
            var items = inventoryManager.GetAllItems();
            
            // 重置所有槽位
            foreach (var slotUI in slotUIs)
            {
                slotUI.Clear();
            }
            
            // 重新设置物品
            foreach (var stack in items)
            {
                if (stack.SlotIndex >= 0 && stack.SlotIndex < slotUIs.Count)
                {
                    slotUIs[stack.SlotIndex].SetItem(stack.Item, stack.Count);
                }
            }
            
            Debug.Log($"强制刷新了 {items.Count} 个物品到 {slotUIs.Count} 个槽位");
        }
        
        [ContextMenu("测试添加物品到UI")]
        public void TestAddItemToUI()
        {
            // 创建一个测试物品
            ItemData testItem = ScriptableObject.CreateInstance<ItemData>();
            testItem.itemId = "debug_item_1";
            testItem.itemName = "调试物品";
            testItem.description = "这是一个用于调试的物品";
            testItem.maxStackSize = 10;
            
            // 尝试加载一个图标
            testItem.icon = Resources.Load<Sprite>("Icons/TestIcon");
            if (testItem.icon == null)
            {
                Debug.LogWarning("无法加载测试图标，创建默认图标");
                // 创建一个简单的彩色方块作为图标
                Texture2D tex = new Texture2D(64, 64);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        tex.SetPixel(x, y, Color.red);
                    }
                }
                tex.Apply();
                testItem.icon = Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            }
            
            // 添加到第一个槽位
            if (slotUIs.Count > 0)
            {
                slotUIs[0].SetItem(testItem, 5);
                Debug.Log($"在槽位0添加了测试物品: {testItem.itemName} x5 (图标: {testItem.icon})");
            }
        }
    }
}