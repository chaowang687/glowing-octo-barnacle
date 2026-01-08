using UnityEngine;
using System.Collections.Generic;

namespace Bag
{
   [CreateAssetMenu]
public class ItemData : ScriptableObject {
    [Header("基本信息")]
    public string itemID; // 物品唯一标识符，与物品名称分开
    public string itemName; // 物品显示名称
    [TextArea] // 支持多行文本输入
    public string description; // 物品描述
    public int width = 1;
    public int height = 1;
    public Sprite icon;
    // 异形物品可以使用 bool[,] shape 数组定义
    public GameObject worldPrefab;
    
    [Header("效果配置")]
    [Tooltip("支持实现 IItemEffect 接口的 ScriptableObject")]
    public List<ScriptableObject> effects;

} 

}
