// InventoryManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScavengingGame
{
    public class InventoryManager : MonoBehaviour, IInventoryService
    {
        private Dictionary<string, ItemStack> _itemStacks = new Dictionary<string, ItemStack>();
        private Dictionary<int, string> _slotItemMap = new Dictionary<int, string>();
        private Dictionary<EquipmentData.SlotType, EquipmentData> _equippedItems = new Dictionary<EquipmentData.SlotType, EquipmentData>();
        
        [SerializeField] private int _defaultMaxStack = 99;
        private Dictionary<string, int> _maxStackConfig = new Dictionary<string, int>();
        
        public event Action<ItemData> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;
        public event Action<EquipmentData.SlotType, EquipmentData> OnEquipmentChanged;
        public event Action OnInventoryChanged;
        
     public string TakeRandomItem()
{
    if (_slotItemMap.Count == 0)
    {
        Debug.Log("<color=yellow>[BATTLE]</color> 玩家包里空空如也，海盗空手而归！");
        return null;
    }

    int targetSlotIndex = -1;
    string itemId = null;
    string itemDisplayName = null; // 新增：用于存储要显示的名称

    // 优先寻找装备
    var equipmentSlots = _slotItemMap.Where(kvp => 
    {
        string id = kvp.Value;
        return _itemStacks.ContainsKey(id) && _itemStacks[id].Item is EquipmentData;
    }).ToList();

    if (equipmentSlots.Count > 0)
    {
        var chosenKvp = equipmentSlots[UnityEngine.Random.Range(0, equipmentSlots.Count)];
        targetSlotIndex = chosenKvp.Key;
        itemId = chosenKvp.Value;
        itemDisplayName = _itemStacks[itemId].Item.itemName; // 提取名字
        Debug.Log("<color=orange>[AI决策]</color> 海盗盯上了你的装备！");
    }
    else
    {
        // 无装备则随机抢夺普通物资
        int[] keys = _slotItemMap.Keys.ToArray();
        targetSlotIndex = keys[UnityEngine.Random.Range(0, keys.Length)];
        itemId = _slotItemMap[targetSlotIndex];
        itemDisplayName = _itemStacks[itemId].Item.itemName; // 提取名字
    }

    if (targetSlotIndex != -1)
    {
        // 执行移除逻辑（此时仍需用 itemId 操作字典）
        RemoveItemAt(targetSlotIndex, _itemStacks[itemId].Count);
    }

    // ⭐ 关键：返回名字而不是 ID
    return itemDisplayName; 
}
        
        private void Start()
        {
            foreach (EquipmentData.SlotType slotType in Enum.GetValues(typeof(EquipmentData.SlotType)))
            {
                if (!_equippedItems.ContainsKey(slotType))
                {
                    _equippedItems[slotType] = null;
                }
            }
        }
        // --- InventoryManager.cs ---
        public static InventoryManager Instance { get; private set; }

        private void Awake()
        {
            // 如果没有实例，则指定自己并确保切换场景时不销毁
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 将该物体提升到 DontDestroyOnLoad 逻辑场景中
            }
            else
            {
                // 如果已经存在实例（比如从战斗回到地图），销毁重复的自己
                Destroy(gameObject);
            }
        }
        
        // ==================== 宝箱系统专用方法 ====================
        
        public bool HasSpaceForItem(string itemId, int amount = 1)
        {
            ItemData itemData = GetItemDataById(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"物品 {itemId} 不存在");
                return false;
            }
            
            if (itemData.maxStackSize > 1)
            {
                int existingSpace = 0;
                foreach (var stack in _itemStacks.Values)
                {
                    if (stack.Item.itemId == itemId && stack.Count < itemData.maxStackSize)
                    {
                        existingSpace += itemData.maxStackSize - stack.Count;
                    }
                }
                
                if (existingSpace >= amount)
                {
                    return true;
                }
                else if (existingSpace > 0)
                {
                    amount -= existingSpace;
                }
            }
            
            int slotsNeeded = Mathf.CeilToInt((float)amount / itemData.maxStackSize);
            int availableSlots = GetMaxCapacity() - GetCurrentCapacity();
            
            return availableSlots >= slotsNeeded;
        }
        
        public bool AddItemsBatch(List<ItemStack> itemsToAdd)
        {
            bool allSuccess = true;
            List<ItemStack> failedItems = new List<ItemStack>();
            
            foreach (var itemStack in itemsToAdd)
            {
                if (!AddItem(itemStack.Item, itemStack.Count))
                {
                    allSuccess = false;
                    failedItems.Add(itemStack);
                }
            }
            
            if (!allSuccess && failedItems.Count > 0)
            {
                Debug.LogWarning($"以下物品添加失败: {string.Join(", ", failedItems.Select(i => $"{i.Item.itemName} x{i.Count}"))}");
            }
            
            return allSuccess;
        }
        
        public int GetRemainingSpace()
        {
            return GetMaxCapacity() - GetCurrentCapacity();
        }
        
        // ==================== 物品基本操作 ====================
        
        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            
            if (IsFull())
            {
                Debug.LogWarning("库存已满");
                return false;
            }
            
            bool canStack = CanItemStack(item);
            string itemId = GetItemId(item);
            
            if (canStack && _itemStacks.ContainsKey(itemId))
            {
                var stack = _itemStacks[itemId];
                int maxStack = GetMaxStackCount(item);
                int availableSpace = maxStack - stack.Count;
                
                if (availableSpace >= amount)
                {
                    stack.Count += amount;
                    Debug.Log($"堆叠物品: {item.itemName} +{amount}, 总数: {stack.Count}");
                    
                    OnItemAdded?.Invoke(item);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                else
                {
                    stack.Count = maxStack;
                    Debug.Log($"物品堆叠已满: {item.itemName}, 剩余 {amount - availableSpace} 个无法添加");
                    
                    OnItemAdded?.Invoke(item);
                    OnInventoryChanged?.Invoke();
                    
                    if (amount - availableSpace > 0 && !IsFull())
                    {
                        return AddItem(item, amount - availableSpace);
                    }
                    return false;
                }
            }
            else
            {
                if (GetCurrentCapacity() + 1 > GetMaxCapacity())
                {
                    Debug.LogWarning("库存已满，无法添加新物品");
                    return false;
                }
                
                int emptySlot = FindEmptySlot();
                if (emptySlot == -1)
                {
                    Debug.LogWarning("没有可用槽位");
                    return false;
                }
                
                int actualAmount = canStack ? Math.Min(amount, GetMaxStackCount(item)) : 1;
                var newStack = new ItemStack(item, actualAmount, emptySlot);
                _itemStacks[itemId] = newStack;
                _slotItemMap[emptySlot] = itemId;
                
                Debug.Log($"添加新物品: {item.itemName} x{actualAmount} 到槽位 {emptySlot}");
                
                OnItemAdded?.Invoke(item);
                OnInventoryChanged?.Invoke();
                
                if (canStack && amount > actualAmount)
                {
                    return AddItem(item, amount - actualAmount);
                }
                
                return true;
            }
        }
        
        public bool RemoveItem(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
            
            if (!_itemStacks.ContainsKey(itemId))
            {
                Debug.LogWarning($"物品不存在: {itemId}");
                return false;
            }
            
            var stack = _itemStacks[itemId];
            
            if (stack.Count < amount)
            {
                Debug.LogWarning($"尝试移除 {amount} 个 {stack.Item.itemName}，但只有 {stack.Count} 个");
                return false;
            }
            
            stack.Count -= amount;
            
            if (stack.Count <= 0)
            {
                _itemStacks.Remove(itemId);
                _slotItemMap.Remove(stack.SlotIndex);
                Debug.Log($"移除物品: {stack.Item.itemName} (全部)");
            }
            else
            {
                Debug.Log($"移除物品: {stack.Item.itemName} x{amount}, 剩余: {stack.Count}");
            }
            
            OnItemRemoved?.Invoke(stack.Item, amount);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public bool RemoveItemAt(int index, int amount = 1)
        {
            if (!_slotItemMap.ContainsKey(index) || amount <= 0) return false;
            
            string itemId = _slotItemMap[index];
            return RemoveItem(itemId, amount);
        }
        
        public void UseItem(ItemData item)
        {
            if (item == null) return;
            
            string itemId = GetItemId(item);
            
            if (_itemStacks.ContainsKey(itemId))
            {
                var stack = _itemStacks[itemId];
                
                if (!(item is EquipmentData))
                {
                    if (stack.Count > 0)
                    {
                        stack.Count--;
                        Debug.Log($"使用物品: {item.itemName}, 剩余: {stack.Count}");
                        
                        if (stack.Count <= 0)
                        {
                            _itemStacks.Remove(itemId);
                            _slotItemMap.Remove(stack.SlotIndex);
                        }
                        
                        OnItemRemoved?.Invoke(item, 1);
                        OnInventoryChanged?.Invoke();
                    }
                }
                else
                {
                    Debug.Log($"尝试使用装备: {item.itemName}");
                }
            }
        }
        
        public int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            return GetItemCountById(GetItemId(item));
        }
        
        public int GetItemCountById(string itemId)
        {
            if (_itemStacks.ContainsKey(itemId))
            {
                return _itemStacks[itemId].Count;
            }
            return 0;
        }
        
        public List<ItemStack> GetAllItems()
        {
            return _itemStacks.Values.ToList();
        }
        
        public bool IsFull()
        {
            return GetCurrentCapacity() >= GetMaxCapacity();
        }
        
        public int GetCurrentCapacity()
        {
            return _itemStacks.Count;
        }
        
        public int GetMaxCapacity()
        {
            return 30;
        }
        
        public bool SwapItems(int sourceIndex, int targetIndex)
        {
            if (sourceIndex == targetIndex) return false;
            
            bool sourceHasItem = _slotItemMap.ContainsKey(sourceIndex);
            bool targetHasItem = _slotItemMap.ContainsKey(targetIndex);
            
            if (!sourceHasItem && !targetHasItem) return false;
            
            if (sourceHasItem && targetHasItem)
            {
                string sourceItemId = _slotItemMap[sourceIndex];
                string targetItemId = _slotItemMap[targetIndex];
                
                var sourceStack = _itemStacks[sourceItemId];
                var targetStack = _itemStacks[targetItemId];
                
                sourceStack.SlotIndex = targetIndex;
                targetStack.SlotIndex = sourceIndex;
                
                _slotItemMap[sourceIndex] = targetItemId;
                _slotItemMap[targetIndex] = sourceItemId;
                
                Debug.Log($"交换物品: 槽位{sourceIndex}和槽位{targetIndex}");
            }
            else if (sourceHasItem && !targetHasItem)
            {
                string sourceItemId = _slotItemMap[sourceIndex];
                var stack = _itemStacks[sourceItemId];
                
                stack.SlotIndex = targetIndex;
                _slotItemMap.Remove(sourceIndex);
                _slotItemMap[targetIndex] = sourceItemId;
                
                Debug.Log($"移动物品到空槽位: 从{sourceIndex}到{targetIndex}");
            }
            else if (!sourceHasItem && targetHasItem)
            {
                return SwapItems(targetIndex, sourceIndex);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public bool MergeItemStacks(int sourceIndex, int targetIndex)
        {
            if (sourceIndex == targetIndex) return false;
            
            if (!_slotItemMap.ContainsKey(sourceIndex) || !_slotItemMap.ContainsKey(targetIndex))
                return false;
            
            string sourceItemId = _slotItemMap[sourceIndex];
            string targetItemId = _slotItemMap[targetIndex];
            
            if (sourceItemId != targetItemId) return false;
            
            var sourceStack = _itemStacks[sourceItemId];
            var targetStack = _itemStacks[targetItemId];
            
            int maxStack = GetMaxStackCount(sourceStack.Item);
            int availableSpace = maxStack - targetStack.Count;
            
            if (availableSpace <= 0) return false;
            
            int transferAmount = Math.Min(sourceStack.Count, availableSpace);
            targetStack.Count += transferAmount;
            sourceStack.Count -= transferAmount;
            
            if (sourceStack.Count <= 0)
            {
                _itemStacks.Remove(sourceItemId);
                _slotItemMap.Remove(sourceIndex);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        // ==================== 装备操作 ====================
        
        public bool EquipItem(EquipmentData equipment)
        {
            if (equipment == null) return false;
            
            string itemId = GetItemId(equipment);
            
            if (!_itemStacks.ContainsKey(itemId))
            {
                Debug.LogWarning($"背包中没有装备: {equipment.itemName}");
                return false;
            }
            
            if (_equippedItems.ContainsKey(equipment.slotType))
            {
                UnequipItem(equipment.slotType);
            }
            
            var stack = _itemStacks[itemId];
            stack.Count--;
            
            if (stack.Count <= 0)
            {
                _itemStacks.Remove(itemId);
                _slotItemMap.Remove(stack.SlotIndex);
            }
            
            _equippedItems[equipment.slotType] = equipment;
            
            Debug.Log($"装备: {equipment.itemName}");
            
            OnEquipmentChanged?.Invoke(equipment.slotType, equipment);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
        public bool UnequipItem(EquipmentData.SlotType slotType)
        {
            if (!_equippedItems.ContainsKey(slotType))
            {
                Debug.LogWarning($"槽位 {slotType} 没有装备物品");
                return false;
            }
            
            if (IsFull())
            {
                Debug.LogWarning("背包已满，无法卸下装备");
                return false;
            }
            
            var equipment = _equippedItems[slotType];
            _equippedItems.Remove(slotType);
            AddItem(equipment);
            
            Debug.Log($"卸下: {equipment.itemName}");
            
            OnEquipmentChanged?.Invoke(slotType, null);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
        public EquipmentData GetEquippedItem(EquipmentData.SlotType slotType)
        {
            _equippedItems.TryGetValue(slotType, out EquipmentData equipment);
            return equipment;
        }
        
        public Dictionary<EquipmentData.SlotType, EquipmentData> GetAllEquippedItems()
        {
            return new Dictionary<EquipmentData.SlotType, EquipmentData>(_equippedItems);
        }
        
        public bool IsItemEquipped(EquipmentData equipment)
        {
            if (equipment == null) return false;
            
            _equippedItems.TryGetValue(equipment.slotType, out EquipmentData equipped);
            return equipped != null && equipped.itemName == equipment.itemName;
        }
        
        public (int attack, int defense) CalculateEquipmentBonuses()
        {
            int attackBonus = 0;
            int defenseBonus = 0;
            
            foreach (var equipment in _equippedItems.Values)
            {
                if (equipment != null)
                {
                    attackBonus += equipment.attackBonus;
                    defenseBonus += equipment.defenseBonus;
                }
            }
            
            return (attackBonus, defenseBonus);
        }
        
        // ==================== 辅助方法 ====================
        
        private string GetItemId(ItemData item)
        {
            return item.itemId ?? item.itemName;
        }
        
        private bool CanItemStack(ItemData item)
        {
            return !(item is EquipmentData);
        }
        
        private int GetMaxStackCount(ItemData item)
        {
            if (_maxStackConfig.ContainsKey(item.itemName))
            {
                return _maxStackConfig[item.itemName];
            }
            
            if (item is EquipmentData) return 1;
            return _defaultMaxStack;
        }
        
        private int FindEmptySlot()
        {
            for (int i = 0; i < GetMaxCapacity(); i++)
            {
                if (!_slotItemMap.ContainsKey(i))
                {
                    return i;
                }
            }
            return -1;
        }
        
        private ItemData GetItemDataById(string itemId)
        {
            Debug.LogWarning($"ItemDatabase 未找到，创建临时物品: {itemId}");
            return CreateTempItemData(itemId);
        }
        
        private ItemData CreateTempItemData(string itemId)
        {
            ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.itemId = itemId;
            itemData.itemName = itemId;
            itemData.maxStackSize = 99;

            // 添加默认图标（重要！）
            itemData.icon = Resources.Load<Sprite>("DefaultIcons/DefaultItem");
            return itemData;
        }
        
        public void LogInventory()
        {
            Debug.Log("=== 库存信息 ===");
            Debug.Log($"总容量: {GetCurrentCapacity()}/{GetMaxCapacity()}");
            
            var allItems = GetAllItems();
            if (allItems.Count == 0)
            {
                Debug.Log("背包为空");
            }
            else
            {
                Debug.Log($"物品堆叠数: {allItems.Count}");
                
                foreach (var stack in allItems)
                {
                    Debug.Log($"  槽位{stack.SlotIndex}: {stack.Item.itemName} x{stack.Count}");
                }
            }
            
            Debug.Log("=== 装备信息 ===");
            var equippedItems = GetAllEquippedItems();
            bool hasEquipped = false;
            
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null)
                {
                    hasEquipped = true;
                    Debug.Log($"  {GetSlotName(kvp.Key)}: {kvp.Value.itemName}");
                }
            }
            
            if (!hasEquipped)
            {
                Debug.Log("没有装备任何物品");
            }
            
            var bonuses = CalculateEquipmentBonuses();
            Debug.Log($"攻击加成: +{bonuses.attack}");
            Debug.Log($"防御加成: +{bonuses.defense}");
        }
        
        private string GetSlotName(EquipmentData.SlotType slotType)
        {
            switch (slotType)
            {
                case EquipmentData.SlotType.Weapon: return "weapon";
                case EquipmentData.SlotType.Armor: return "护甲";
                case EquipmentData.SlotType.Helmet: return "头盔";
                case EquipmentData.SlotType.Gloves: return "手套";
                case EquipmentData.SlotType.Boots: return "靴子";
                case EquipmentData.SlotType.Shield: return "盾牌";
                case EquipmentData.SlotType.Ring1: return "戒指1";
                case EquipmentData.SlotType.Ring2: return "戒指2";
                case EquipmentData.SlotType.Amulet1: return "护符1";
                case EquipmentData.SlotType.Amulet2: return "护符2";
                default: return "未知";
            }
        }
    }
}