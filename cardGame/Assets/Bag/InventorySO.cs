using UnityEngine;
using System.Collections.Generic;

namespace Bag
{
    [CreateAssetMenu(fileName = "NewInventorySO", menuName = "Bag/InventoryData")]
    public class InventorySO : ScriptableObject
    {
        // 真正的静态数据存储
        public List<ItemInstance> items = new List<ItemInstance>();

        public void AddItem(ItemInstance item)
        {
            if (!items.Contains(item)) items.Add(item);
        }

        public void RemoveItem(ItemInstance item)
        {
            if (items.Contains(item)) items.Remove(item);
        }

        public void Clear() => items.Clear();
    }
}