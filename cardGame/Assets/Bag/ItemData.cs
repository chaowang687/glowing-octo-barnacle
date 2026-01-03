using UnityEngine;
using System.Collections.Generic;

namespace Bag
{
   [CreateAssetMenu]
public class ItemData : ScriptableObject {
    public string itemName;
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
