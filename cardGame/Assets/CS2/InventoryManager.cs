using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            // 注册到全局状态管理器
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.PlayerInventory = this;
            }
        
        
            
            // 初始化装备槽位
            InitializeEquipmentSlots();
        }

        private void InitializeEquipmentSlots()
        {
            // 确保所有槽位类型都有初始值
            foreach (EquipmentData.SlotType slot in System.Enum.GetValues(typeof(EquipmentData.SlotType)))
            {
                _equippedItems[slot] = null;
            }
        }

        /// <summary>
        /// 添加物品到背包。
        /// </summary>
        public bool AddItem(ItemData item)
        {
            if (item == null) 
            {
                Debug.LogError("尝试添加空物品到背包。");
                return false;
            }

            string itemName = item.ItemName;
            int stackSize = item.StackSize;

            if (_itemCounts.ContainsKey(itemName))
            {
                _itemCounts[itemName] += stackSize;
            }
            else
            {
                // 检查背包容量（只检查非堆叠物品）
                if (item.StackSize <= 1 && Items.Count >= MaxInventorySlots)
                {
                    Debug.LogWarning($"背包已满，无法添加 {itemName}");
                    return false;
                }
                
                _itemCounts.Add(itemName, stackSize);
                _itemDataCache.Add(itemName, item);
                
                // 仅在首次获取时添加到 Items 列表
                if (!Items.Any(i => i.ItemName == itemName))
                {
                    Items.Add(item);
                }
            }

            Debug.Log($"[Inventory] 获得物品: {stackSize} x {itemName}. 总计: {_itemCounts[itemName]}");
            return true;
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
                
                // 从 Items 列表中移除
                ItemData itemToRemove = Items.FirstOrDefault(i => i.ItemName == itemName);
                if (itemToRemove != null)
                {
                    Items.Remove(itemToRemove);
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
                Debug.LogWarning($"无法装备 {equipment.ItemName}，背包中没有该物品");
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
            
            Debug.Log($"装备 {equipment.ItemName} 到 {equipment.Slot}");
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
                
                Debug.Log($"卸下 {equipment.ItemName} 从 {slot}");
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
    }
}