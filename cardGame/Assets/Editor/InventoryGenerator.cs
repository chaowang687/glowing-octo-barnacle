// 将此脚本放入文件夹 Assets/Editor
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGenerator : EditorWindow {
    [MenuItem("Tools/自动生成背包网格")]
    public static void GenerateGrid() {
        GameObject root = new GameObject("InventoryGrid");
        var grid = root.AddComponent<Bag.InventoryGrid>();
        
        // 自动添加 GridLayoutGroup 来排列背景图块
        var layout = root.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(grid.cellSize, grid.cellSize);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = grid.width;

        // 自动生成背景格子图片
        for (int i = 0; i < grid.width * grid.height; i++) {
            GameObject slot = new GameObject("Slot_" + i);
            slot.transform.SetParent(root.transform);
            slot.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
    }
}