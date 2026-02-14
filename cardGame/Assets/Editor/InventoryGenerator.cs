// 将此脚本放入文件夹 Assets/Editor
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGenerator : EditorWindow {
    private int gridWidth = 10;
    private int gridHeight = 8;
    private float cellSize = 50f;
    private bool generateBackground = true;
    
    [MenuItem("Tools/自动生成背包网格")]
    public static void ShowWindow() {
        GetWindow<InventoryGenerator>("背包网格生成器");
    }
    
    private void OnGUI() {
        GUILayout.Label("网格设置", EditorStyles.boldLabel);
        
        // 网格大小设置
        gridWidth = EditorGUILayout.IntField("网格宽度", gridWidth);
        gridHeight = EditorGUILayout.IntField("网格高度", gridHeight);
        cellSize = EditorGUILayout.FloatField("单元格大小 (px)", cellSize);
        generateBackground = EditorGUILayout.Toggle("生成背景格子", generateBackground);
        
        // 限制最小值
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(10f, cellSize);
        
        GUILayout.Space(10);
        
        // 生成按钮
        if (GUILayout.Button("生成背包网格")) {
            GenerateGrid();
        }
        
        // 生成提示
        GUILayout.Space(10);
        GUILayout.Label("提示：生成的网格将位于场景根目录", EditorStyles.miniLabel);
    }
    
    private void GenerateGrid() {
        GameObject root = new GameObject($"InventoryGrid_{gridWidth}x{gridHeight}");
        var grid = root.AddComponent<Bag.InventoryGrid>();
        
        // 设置网格属性
        grid.width = gridWidth;
        grid.height = gridHeight;
        grid.cellSize = cellSize;
        
        // 自动添加 GridLayoutGroup 来排列背景图块
        if (generateBackground) {
            var layout = root.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(grid.cellSize, grid.cellSize);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = grid.width;
            
            // 自动生成背景格子图片
            for (int i = 0; i < grid.width * grid.height; i++) {
                GameObject slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(root.transform);
                slot.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
        
        Debug.Log($"成功生成 {gridWidth}x{gridHeight} 背包网格！");
    }
}