using UnityEngine;
using Bag;

public class ShapeTest : MonoBehaviour
{
    void Start()
    {
        // 创建一个T型物品数据
        ItemData tShapeItem = ScriptableObject.CreateInstance<ItemData>();
        tShapeItem.itemID = "T_Shape_Test";
        tShapeItem.itemName = "T型测试物品";
        tShapeItem.description = "这是一个T型的测试物品";
        tShapeItem.width = 3;
        tShapeItem.height = 3;
        
        // 设置T型形状（3x3）
        tShapeItem.shapeData.width = 3;
        tShapeItem.shapeData.height = 3;
        tShapeItem.shapeData.shapeArray = new bool[25] {
            // 第1行
            false, true, false, false, false,
            // 第2行
            true,  true, true,  false, false,
            // 第3行
            false, true, false, false, false,
            // 第4-5行（未使用）
            false, false, false, false, false,
            false, false, false, false, false
        };
        
        // 创建物品实例
        ItemInstance itemInstance = new ItemInstance(tShapeItem);
        
        // 测试旋转功能
        Debug.Log("=== 测试T型物品旋转 ===");
        TestRotation(itemInstance);
        
        Debug.Log("=== 测试完成 ===");
    }
    
    void TestRotation(ItemInstance item)
    {
        // 测试不同旋转角度的形状
        int[] rotations = { 0, 90, 180, 270 };
        
        foreach (int rotation in rotations)
        {
            item.rotation = rotation;
            bool[,] shape = item.GetActualShape();
            int width = shape.GetLength(0);
            int height = shape.GetLength(1);
            
            Debug.Log($"旋转角度: {rotation}度，形状尺寸: {width}x{height}");
            
            // 打印形状
            for (int j = 0; j < height; j++)
            {
                string row = "";
                for (int i = 0; i < width; i++)
                {
                    row += shape[i, j] ? "# " : ". ";
                }
                Debug.Log(row);
            }
            Debug.Log("");
        }
    }
}