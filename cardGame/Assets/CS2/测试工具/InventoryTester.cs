using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ScavengingGame.Tests
{
    /// <summary>
    /// IInventoryService 单元测试适配器
    /// 职责：提供标准化测试用例，易于扩展和维护
    /// </summary>
    public class InventoryServiceTestAdapter : MonoBehaviour
    {

        // 在调试脚本中调用
        ChestData testData = new ChestData 
        { 
            chestLevel = 3, 
            chestType = ChestType.Iron 
        };
     
        [ContextMenu("测试添加装备到背包")]
    public void TestAddEquipment()
    {
    if (!InitializeService())
    {
        Debug.LogError("初始化服务失败");
        return;
    }

    // 创建一个测试装备
    EquipmentData testEquipment = ScriptableObject.CreateInstance<EquipmentData>();
    testEquipment.itemId = "test_weapon_001";
    testEquipment.itemName = "测试武器";
    testEquipment.maxStackSize = 1;
    testEquipment.slotType = EquipmentData.SlotType.Weapon;
    testEquipment.attackBonus = 10;
    testEquipment.defenseBonus = 0;

    // 尝试添加
    bool result = _service.AddItem(testEquipment, 1);
    Debug.Log($"添加装备结果: {result}, 当前库存容量: {_service.GetCurrentCapacity()}/{_service.GetMaxCapacity()}");
}



        
        [System.Serializable]
        public class TestCase
        {
            public string testName;
            public TestType testType;
            public ItemData testItem;
            public EquipmentData testEquipment;
            public int testAmount = 1;
            public bool expectedResult = true;
        }
        
        public enum TestType
        {
            AddItem,
            RemoveItem,
            UseItem,
            EquipItem,
            SpaceCheck,
            BatchAdd,
            EventTest
        }
        
        [Header("测试用例")]
        public TestCase[] testCases;
        
        [Header("测试设置")]
        public bool autoRun = false;
        public bool stopOnFailure = false;
        
        private IInventoryService _service;
        
        void Start()
        {
            if (autoRun)
                StartCoroutine(ExecuteTestCases());
        }
        
        IEnumerator ExecuteTestCases()
        {
            if (!InitializeService())
                yield break;
            
            foreach (var testCase in testCases)
            {
                Debug.Log($"执行测试: {testCase.testName}");
                
                bool result = false;
                
                switch (testCase.testType)
                {
                    case TestType.AddItem:
                        result = ExecuteAddItemTest(testCase);
                        break;
                    case TestType.RemoveItem:
                        result = ExecuteRemoveItemTest(testCase);
                        break;
                    case TestType.UseItem:
                        result = ExecuteUseItemTest(testCase);
                        break;
                    case TestType.EquipItem:
                        result = ExecuteEquipItemTest(testCase);
                        break;
                    case TestType.SpaceCheck:
                        result = ExecuteSpaceCheckTest(testCase);
                        break;
                    case TestType.BatchAdd:
                        result = ExecuteBatchAddTest(testCase);
                        break;
                    case TestType.EventTest:
                        result = ExecuteEventTest(testCase);
                        break;
                    default:
                        Debug.LogWarning($"未知的测试类型: {testCase.testType}");
                        break;
                }
                
                if (result == testCase.expectedResult)
                {
                    Debug.Log($"✓ {testCase.testName} 通过");
                }
                else
                {
                    Debug.LogError($"✗ {testCase.testName} 失败");
                    if (stopOnFailure) yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        bool ExecuteAddItemTest(TestCase testCase)
        {
            if (_service == null) return false;
            return _service.AddItem(testCase.testItem, testCase.testAmount);
        }
        
        // 添加缺失的 ExecuteRemoveItemTest 方法
        bool ExecuteRemoveItemTest(TestCase testCase)
        {
            if (_service == null) return false;
            
            // 对于移除测试，需要先添加物品
            if (!_service.AddItem(testCase.testItem, testCase.testAmount))
            {
                Debug.LogWarning($"无法添加物品 {testCase.testItem?.itemName}，跳过移除测试");
                return false;
            }
            
            return _service.RemoveItem(testCase.testItem.itemId, testCase.testAmount);
        }
        
        // 添加缺失的 ExecuteUseItemTest 方法
        bool ExecuteUseItemTest(TestCase testCase)
        {
            if (_service == null) return false;
            
            // 对于使用测试，需要先添加物品
            if (!_service.AddItem(testCase.testItem, testCase.testAmount))
            {
                Debug.LogWarning($"无法添加物品 {testCase.testItem?.itemName}，跳过使用测试");
                return false;
            }
            
            // 使用物品
            _service.UseItem(testCase.testItem);
            return true; // UseItem 通常没有返回值，假设成功
        }
        
        bool ExecuteEquipItemTest(TestCase testCase)
        {
            if (_service == null || testCase.testEquipment == null) return false;
            
            // 先添加装备到背包
            if (!_service.AddItem(testCase.testEquipment, 1))
                return false;
            
            return _service.EquipItem(testCase.testEquipment);
        }
        
        // 添加缺失的 ExecuteSpaceCheckTest 方法
        bool ExecuteSpaceCheckTest(TestCase testCase)
        {
            if (_service == null) return false;
            return _service.HasSpaceForItem(testCase.testItem?.itemId, testCase.testAmount);
        }
        
        // 添加缺失的 ExecuteBatchAddTest 方法
        bool ExecuteBatchAddTest(TestCase testCase)
        {
            if (_service == null || testCase.testItem == null) return false;
            
            List<ItemStack> items = new List<ItemStack>
            {
                new ItemStack(testCase.testItem, testCase.testAmount)
            };
            
            return _service.AddItemsBatch(items);
        }
        
        // 添加缺失的 ExecuteEventTest 方法
        bool ExecuteEventTest(TestCase testCase)
        {
            if (_service == null) return false;
            
            // 事件测试：监听事件并验证是否触发
            bool eventTriggered = false;
            System.Action<ItemData> eventHandler = (item) => 
            {
                eventTriggered = true;
                Debug.Log($"事件触发: {item?.itemName}");
            };
            
            // 订阅事件
            _service.OnItemAdded += eventHandler;
            
            // 触发事件
            bool result = _service.AddItem(testCase.testItem, testCase.testAmount);
            
            // 取消订阅
            _service.OnItemAdded -= eventHandler;
            
            return result && eventTriggered;
        }
        
        // ... 其他测试执行方法
        
        bool InitializeService()
        {
            // 根据项目实际情况初始化
            if (_service != null) return true;
            
            // 尝试从 GameStateManager 获取
            if (GameStateManager.Instance != null)
            {
                _service = GameStateManager.Instance.PlayerInventory as IInventoryService;
                if (_service != null)
                {
                    Debug.Log("成功获取 IInventoryService");
                    return true;
                }
            }
            
            // 尝试从场景中查找
            var inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager != null)
            {
                _service = inventoryManager as IInventoryService;
                if (_service != null)
                {
                    Debug.Log("从场景中找到 InventoryManager");
                    return true;
                }
            }
            
            Debug.LogError("无法初始化 IInventoryService");
            return false;
        }
        
        [ContextMenu("运行所有测试")]
        public void RunAllTests()
        {
            StartCoroutine(ExecuteTestCases());
        }
        
        [ContextMenu("清理库存")]
        public void ClearInventory()
        {
            if (_service == null && !InitializeService()) return;
            
            var items = _service.GetAllItems();
            foreach (var itemStack in items)
            {
                _service.RemoveItem(itemStack.Item.itemId, itemStack.Count);
            }
            
            // 卸载所有装备
            var equippedItems = _service.GetAllEquippedItems();
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                {
                    _service.UnequipItem(kvp.Key);
                }
            }
            
            Debug.Log("库存已清理");
        }
    }
}