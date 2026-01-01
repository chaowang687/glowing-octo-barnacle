using System;
using System.Collections.Generic;

namespace Bag
{
    [Serializable]
    public class InventorySaveData
    {
        public List<ItemSaveEntry> items = new List<ItemSaveEntry>();
    }

    [Serializable]
    public class ItemSaveEntry
    {
        public string itemID; // 用于从资源库加载对应的 ItemData
        public int posX;
        public int posY;
        public bool isRotated;
    }
}