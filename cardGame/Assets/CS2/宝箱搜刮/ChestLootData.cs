using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ChestData
{
    public int chestLevel;
    public ChestType chestType;
    public int maxLootSlots = 5;
    public List<LootItemData> lootItems = new List<LootItemData>();
}

