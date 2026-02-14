using UnityEngine;
using System.Collections.Generic;
using ScavengingGame;
using Bag;

[CreateAssetMenu(fileName = "NewFossil", menuName = "DigGame/Fossil")]
public class FossilData : ScriptableObject {
    public string fossilName;
    public Bag.ItemData rewardItem;
    
    // --- 添加这一行 ---
    public Sprite fossilSprite; 
    
    public List<Vector2Int> shapeOffsets = new List<Vector2Int>(); 
}