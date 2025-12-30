using UnityEngine;
using System.Collections.Generic;
using ScavengingGame;

[CreateAssetMenu(fileName = "NewFossil", menuName = "DigGame/Fossil")]
public class FossilData : ScriptableObject {
    public string fossilName;
    public ItemData rewardItem;
    
    // --- 添加这一行 ---
    public Sprite fossilSprite; 
    
    public List<Vector2Int> shapeOffsets = new List<Vector2Int>(); 
}