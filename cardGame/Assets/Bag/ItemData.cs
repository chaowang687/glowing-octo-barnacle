using UnityEngine;
using System.Collections.Generic;

namespace Bag
{
    /// <summary>
    /// 形状数据类，用于在Inspector中可视化编辑物品形状
    /// </summary>
    [System.Serializable]
    public class ShapeData
    {
        [Header("形状尺寸")]
        public int width = 2;
        public int height = 2;
        
        [Header("形状定义")]
        [Tooltip("使用一维数组表示形状，按行排列：[0,0][0,1][0,2][1,0][1,1][1,2]...")]
        public bool[] shapeArray = new bool[25];
        
        /// <summary>
        /// 获取二维形状数组
        /// </summary>
        public bool[,] GetShapeArray()
        {
            bool[,] shape = new bool[5, 5];
            
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    // 统一索引计算方式：y * 5 + x 表示按行排列（与ShapeDataDrawer一致）
                    int index = y * 5 + x;
                    if (index < shapeArray.Length)
                    {
                        shape[x, y] = shapeArray[index];
                    }
                }
            }
            
            return shape;
        }
    }

   [CreateAssetMenu(menuName = "Bag/ItemData", fileName = "New Item Data")]
public class ItemData : ScriptableObject {
    [Header("基本信息")]
    public string itemID; // 物品唯一标识符，与物品名称分开
    public string itemName; // 物品显示名称
    [TextArea] // 支持多行文本输入
    public string description; // 物品描述
    public int width = 1;
    public int height = 1;
    public Sprite icon;
    
    // 异形物品形状定义，支持更大尺寸的形状
    [Header("形状配置")]
    public ShapeData shapeData = new ShapeData {
        width = 2,
        height = 2,
        shapeArray = new bool[25] {
            true, true, false, false, false,  // 第1行
            true, false, false, false, false, // 第2行
            false, false, false, false, false, // 第3行
            false, false, false, false, false, // 第4行
            false, false, false, false, false  // 第5行
        }
    };
    
    public GameObject worldPrefab;
    
    [Header("效果配置")]
    [Tooltip("支持实现 IItemEffect 接口的 ScriptableObject")]
    public List<ScriptableObject> effects;

} 

}
