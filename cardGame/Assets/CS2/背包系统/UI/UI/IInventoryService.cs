
using System;
using System.Collections.Generic;

namespace ScavengingGame
{
    
    /// <summary>
    /// 库存服务接口，定义库存系统的核心功能
    /// </summary>
    public interface IInventoryService
    {
       
        // ==================== 物品基本操作 ====================
        
        /// <summary>
        /// 添加物品到库存
        /// </summary>
        bool AddItem(ItemData item, int amount = 1);
        
        /// <summary>
        /// 从库存移除物品（通过物品ID）
        /// </summary>
        bool RemoveItem(string itemId, int amount = 1);
        
        /// <summary>
        /// 从指定索引移除物品
        /// </summary>
        bool RemoveItemAt(int index, int amount = 1);
        
        /// <summary>
        /// 使用物品
        /// </summary>
        void UseItem(ItemData item);
        
        /// <summary>
        /// 获取物品数量
        /// </summary>
        int GetItemCount(ItemData item);
        
        /// <summary>
        /// 通过ID获取物品数量
        /// </summary>
        int GetItemCountById(string itemId);
        
        /// <summary>
        /// 获取所有物品（带堆叠信息）
        /// </summary>
        List<ItemStack> GetAllItems();
        
        /// <summary>
        /// 检查库存是否已满
        /// </summary>
        bool IsFull();
        
        /// <summary>
        /// 获取当前容量
        /// </summary>
        int GetCurrentCapacity();
        
        /// <summary>
        /// 获取最大容量
        /// </summary>
        int GetMaxCapacity();
        
        /// <summary>
        /// 交换两个格子的物品
        /// </summary>
        bool SwapItems(int sourceIndex, int targetIndex);
        
        /// <summary>
        /// 合并物品堆叠
        /// </summary>
        bool MergeItemStacks(int sourceIndex, int targetIndex);
        
        // ==================== 装备操作 ====================
        
        /// <summary>
        /// 装备物品
        /// </summary>
        bool EquipItem(EquipmentData equipment);
        
        /// <summary>
        /// 卸下装备
        /// </summary>
        bool UnequipItem(EquipmentData.SlotType slotType);
        
        /// <summary>
        /// 获取已装备的物品
        /// </summary>
        EquipmentData GetEquippedItem(EquipmentData.SlotType slotType);
        
        /// <summary>
        /// 获取所有已装备的物品
        /// </summary>
        Dictionary<EquipmentData.SlotType, EquipmentData> GetAllEquippedItems();
        
        /// <summary>
        /// 检查物品是否已装备
        /// </summary>
        bool IsItemEquipped(EquipmentData equipment);
        
        /// <summary>
        /// 计算装备加成
        /// </summary>
        (int attack, int defense) CalculateEquipmentBonuses();
        
        // ==================== 事件 ====================
        
        /// <summary>
        /// 当物品被添加时触发
        /// </summary>
        event Action<ItemData> OnItemAdded;
        
        /// <summary>
        /// 当物品被移除时触发
        /// </summary>
        event Action<ItemData, int> OnItemRemoved;
        
        /// <summary>
        /// 当装备状态改变时触发
        /// </summary>
        event Action<EquipmentData.SlotType, EquipmentData> OnEquipmentChanged;
        
        /// <summary>
        /// 当库存发生任何变化时触发
        /// </summary>
        event Action OnInventoryChanged;
    }
    
    /// <summary>
    /// 物品堆叠信息
    /// </summary>
    public class ItemStack
    {
        public ItemData Item { get; set; }
        public int Count { get; set; }
        public int SlotIndex { get; set; }
        
        public ItemStack(ItemData item, int count, int slotIndex = -1)
        {
            Item = item;
            Count = count;
            SlotIndex = slotIndex;
        }
    }
}
