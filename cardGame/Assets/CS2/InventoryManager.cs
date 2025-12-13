using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections; // 添加这行用于协程

namespace ScavengingGame
{
    // 装备数据类，继承自 ItemData
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "Inventory/Equipment")]
    public class EquipmentData : ItemData
    {
        public enum SlotType { Weapon, Armor, Amulet1, Amulet2 }
        public SlotType Slot;
        [Header("属性加成")]
        public int AttackBonus;
        public int DefenseBonus;
        // ... 更多属性
    }


    /// <summary>
    /// 玩家背包与装备逻辑管理器。
    /// 实现了物品的堆叠计数、非堆叠物品列表以及装备逻辑。
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Header("背包配置")]
        public int MaxInventorySlots = 30; // 仅限制非堆叠物品（如装备）的槽位

        // 实际追踪物品名称和数量的字典
        private Dictionary<string, int> _itemCounts = new Dictionary<string, int>();
        
        // 物品数据缓存：物品名称 -> ItemData引用
        private Dictionary<string, ItemData> _itemDataCache = new Dictionary<string, ItemData>();
        
        // 装备栏：固定槽位
        private Dictionary<EquipmentData.SlotType, EquipmentData> _equippedItems = new Dictionary<EquipmentData.SlotType, EquipmentData>();

        // 物品栏：用于存储非堆叠物品和已获得的物品数据的引用
        [Header("Debug/Non-Stacked Items")]
        public List<ItemData> Items = new List<ItemData>(); 
        
        // 注意：Items 列表仅用于 Inspector 观察，不用于实际逻辑

        void Awake()
        {
            // 清理Items列表中的null元素（重要！）
            CleanNullItems();
            
            // 注册到全局状态管理器
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.PlayerInventory = this;
                Debug.Log($"[InventoryManager] 已注册到 GameStateManager");
            }
            else
            {
                Debug.LogWarning("[InventoryManager] GameStateManager 实例未找到，将在 Start 中重试");
                StartCoroutine(RegisterToGameStateManager());
            }
            
            // 初始化装备槽位
            InitializeEquipmentSlots();
            
            // 打印初始状态
            Debug.Log($"[InventoryManager] 初始化完成，背包槽位: {MaxInventorySlots}");
        }

        /// <summary>
        /// 清理Items列表中的null元素
        /// </summary>
        private void CleanNullItems()
        {
            if (Items == null)
            {
                Items = new List<ItemData>();
                return;
            }
            
            // 移除所有null元素
            int removedCount = Items.RemoveAll(item => item == null);
            if (removedCount > 0)
            {
                Debug.LogWarning($"[InventoryManager] 清理了 {removedCount} 个null物品");
            }
        }

        /// <summary>
        /// 延迟注册到GameStateManager
        /// </summary>
        private System.Collections.IEnumerator RegisterToGameStateManager()
        {
            // 等待0.5秒，确保GameStateManager已初始化
            yield return new WaitForSeconds(0.5f);
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.PlayerInventory = this;
                Debug.Log("[InventoryManager] 延迟注册成功");
            }
            else
            {
                Debug.LogError("[InventoryManager] GameStateManager 仍未找到，请检查场景设置");
            }
        }

        private void InitializeEquipmentSlots()
        {
            // 确保所有槽位类型都有初始值
            foreach (EquipmentData.SlotType slot in System.Enum.GetValues(typeof(EquipmentData.SlotType)))
            {
                _equippedItems[slot] = null;
            }
            Debug.Log("[InventoryManager] 装备槽位已初始化");
        }

        /// <summary>
        /// 添加物品到背包。
        /// </summary>
        public bool AddItem(ItemData item)
        {
            if (item == null) 
            {
                Debug.LogError("[Inventory] 尝试添加空物品到背包。");
                return false;
            }

            string itemName = item.ItemName;
            
            // 检查物品名称是否为空
            if (string.IsNullOrEmpty(itemName))
            {
                Debug.LogError("[Inventory] 物品名称为空，无法添加");
                return false;
            }
            
            int stackSize = item.StackSize;

            // 再次清理Items列表（安全措施）
            CleanNullItems();

            if (_itemCounts.ContainsKey(itemName))
            {
                // 已存在该物品，增加数量
                _itemCounts[itemName] += stackSize;
                Debug.Log($"[Inventory] 增加物品: {stackSize} x {itemName}. 总计: {_itemCounts[itemName]}");
                return true;
            }
            else
            {
                // 检查背包容量（只检查非堆叠物品）
                if (item.StackSize <= 1 && Items.Count >= MaxInventorySlots)
                {
                    Debug.LogWarning($"[Inventory] 背包已满，无法添加 {itemName} (当前: {Items.Count}/{MaxInventorySlots})");
                    return false;
                }
                
                // 添加到计数字典和缓存
                _itemCounts.Add(itemName, stackSize);
                _itemDataCache.Add(itemName, item);
                
                // 修复的关键：安全地检查是否已存在同名物品
                // 避免使用 LINQ 的 Any，因为它可能在 i 为 null 时崩溃
                bool alreadyInList = false;
                foreach (var existingItem in Items)
                {
                    // 检查 existingItem 是否为 null，然后比较名称
                    if (existingItem != null && existingItem.ItemName == itemName)
                    {
                        alreadyInList = true;
                        break;
                    }
                }
                
                // 如果不在列表中，才添加
                if (!alreadyInList)
                {
                    Items.Add(item);
                    Debug.Log($"[Inventory] 添加新物品: {stackSize} x {itemName}");
                }
                else
                {
                    Debug.Log($"[Inventory] 物品已存在于列表: {itemName}");
                }
                
                return true;
            }
        }

        /// <summary>
        /// 移除指定数量的物品。
        /// </summary>
        public bool RemoveItem(string itemName, int count = 1)
        {
            if (!_itemCounts.ContainsKey(itemName) || _itemCounts[itemName] < count)
            {
                Debug.LogWarning($"[Inventory] 无法移除 {count} x {itemName}。数量不足。");
                return false;
            }

            _itemCounts[itemName] -= count;
            Debug.Log($"[Inventory] 消耗物品: {count} x {itemName}. 剩余: {_itemCounts[itemName]}");

            if (_itemCounts[itemName] <= 0)
            {
                _itemCounts.Remove(itemName);
                _itemDataCache.Remove(itemName);
                
                // 从 Items 列表中移除 - 更安全的实现
                ItemData itemToRemove = null;
                foreach (var item in Items)
                {
                    if (item != null && item.ItemName == itemName)
                    {
                        itemToRemove = item;
                        break;
                    }
                }
                
                if (itemToRemove != null)
                {
                    Items.Remove(itemToRemove);
                    Debug.Log($"[Inventory] 从列表中移除: {itemName}");
                }
            }
            
            return true;
        }

        /// <summary>
        /// 移除指定物品。
        /// </summary>
        public bool RemoveItem(ItemData item, int count = 1)
        {
            if (item == null) return false;
            return RemoveItem(item.ItemName, count);
        }

        /// <summary>
        /// 获取物品数量。
        /// </summary>
        public int GetItemCount(string itemName)
        {
            if (_itemCounts.ContainsKey(itemName))
                return _itemCounts[itemName];
            return 0;
        }

        /// <summary>
        /// 获取物品数量。
        /// </summary>
        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            return GetItemCount(item.ItemName);
        }

        /// <summary>
        /// 装备物品。
        /// </summary>
        public void EquipItem(EquipmentData equipment)
        {
            if (equipment == null) return;

            // 检查是否拥有该装备
            if (!_itemCounts.ContainsKey(equipment.ItemName) || _itemCounts[equipment.ItemName] < equipment.StackSize)
            {
                Debug.LogWarning($"[Inventory] 无法装备 {equipment.ItemName}，背包中没有该物品");
                return;
            }

            // 移除一个（装备占用一个物品）
            if (!RemoveItem(equipment.ItemName, equipment.StackSize))
                return;

            // 处理旧装备
            EquipmentData oldEquipment = _equippedItems[equipment.Slot];
            if (oldEquipment != null)
            {
                // 将旧装备放回背包
                AddItem(oldEquipment);
            }
            
            // 装备新装备
            _equippedItems[equipment.Slot] = equipment;
            
            Debug.Log($"[Inventory] 装备 {equipment.ItemName} 到 {equipment.Slot}");
        }

        /// <summary>
        /// 卸下装备。
        /// </summary>
        public void UnequipItem(EquipmentData.SlotType slot)
        {
            if (_equippedItems.ContainsKey(slot) && _equippedItems[slot] != null)
            {
                EquipmentData equipment = _equippedItems[slot];
                _equippedItems[slot] = null;
                
                // 将装备放回背包
                AddItem(equipment);
                
                Debug.Log($"[Inventory] 卸下 {equipment.ItemName} 从 {slot}");
            }
        }

        /// <summary>
        /// 获取当前装备的装备。
        /// </summary>
        public EquipmentData GetEquippedItem(EquipmentData.SlotType slot)
        {
            if (_equippedItems.ContainsKey(slot))
                return _equippedItems[slot];
            return null;
        }

        /// <summary>
        /// 获取所有装备的装备。
        /// </summary>
        public Dictionary<EquipmentData.SlotType, EquipmentData> GetAllEquippedItems()
        {
            return new Dictionary<EquipmentData.SlotType, EquipmentData>(_equippedItems);
        }

        /// <summary>
        /// 计算装备总加成
        /// </summary>
        public (int attack, int defense) CalculateEquipmentBonuses()
        {
            int attackBonus = 0;
            int defenseBonus = 0;
            
            foreach (var equipment in _equippedItems.Values)
            {
                if (equipment != null)
                {
                    attackBonus += equipment.AttackBonus;
                    defenseBonus += equipment.DefenseBonus;
                }
            }
            
            return (attackBonus, defenseBonus);
        }

        /// <summary>
        /// 调试方法：打印当前背包所有物品的堆叠数量。
        /// </summary>
        public void LogInventory()
        {
            Debug.Log("--- 当前背包物品 (堆叠计数) ---");
            if (_itemCounts.Count == 0)
            {
                Debug.Log("背包是空的。");
                return;
            }
            foreach(var kvp in _itemCounts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
            Debug.Log("---------------------------------");
        }
        
        /// <summary>
        /// 调试：打印详细信息
        /// </summary>
        public void DebugPrint()
        {
            Debug.Log($"[InventoryManager 调试信息]");
            Debug.Log($"  背包槽位: {Items.Count}/{MaxInventorySlots}");
            Debug.Log($"  物品种类: {_itemCounts.Count}");
            Debug.Log($"  Items列表长度: {Items.Count}");
            Debug.Log($"  Items列表中null元素: {Items.Count(item => item == null)}");
            
            // 打印所有物品
            foreach (var kvp in _itemCounts)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value}");
            }
        }
    }
}