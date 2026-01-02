using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))] // 关联你的 GridManager 脚本
public class GridEditorTool : Editor 
{
    // 可配置的生成参数
    private int generateWidth = 4;
    private int generateHeight = 5;
    
    public override void OnInspectorGUI() 
    {
        // 绘制默认的 Inspector 内容
        DrawDefaultInspector();

        GridManager manager = (GridManager)target;

        GUILayout.Space(10);
        GUILayout.Label("编辑器生成工具", EditorStyles.boldLabel);
        
        // 可配置的生成参数
        GUILayout.BeginHorizontal();
        GUILayout.Label("地块宽度:", GUILayout.Width(80));
        generateWidth = EditorGUILayout.IntField(generateWidth, GUILayout.Width(60));
        GUILayout.Label("地块高度:", GUILayout.Width(80));
        generateHeight = EditorGUILayout.IntField(generateHeight, GUILayout.Width(60));
        GUILayout.EndHorizontal();
        
        // 限制最小值
        generateWidth = Mathf.Max(1, generateWidth);
        generateHeight = Mathf.Max(1, generateHeight);

        GUILayout.Space(5);
        
        if (GUILayout.Button($"生成 {generateWidth}x{generateHeight} 地块")) 
        {
            GenerateStaticGrid(manager, generateWidth, generateHeight);
        }

        if (GUILayout.Button("清除所有地块")) 
        {
            ClearGrid(manager);
        }
        
        GUILayout.Space(10);
        
        // 快速生成预设尺寸
        GUILayout.Label("快速生成预设:", EditorStyles.miniBoldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("3x3")) GenerateStaticGrid(manager, 3, 3);
        if (GUILayout.Button("4x5")) GenerateStaticGrid(manager, 4, 5);
        if (GUILayout.Button("5x5")) GenerateStaticGrid(manager, 5, 5);
        if (GUILayout.Button("6x8")) GenerateStaticGrid(manager, 6, 8);
        GUILayout.EndHorizontal();
    }

    private void GenerateStaticGrid(GridManager manager, int w, int h) 
    {
        // 先清理旧的
        ClearGrid(manager);

        // 设置数据
        manager.width = w;
        manager.height = h;

        // 手动调用生成逻辑
        for (int x = 0; x < w; x++) 
        {
            for (int y = 0; y < h; y++) 
            {
                // 计算位置 (0,0) 在左上角
                Vector3 pos = new Vector3(x, -y, 0);
                
                // 实例化
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(manager.tilePrefab);
                tile.transform.position = pos;
                tile.transform.SetParent(manager.transform);
                tile.name = $"Tile_{x}_{y}";

                // 设置初始贴图
                if (manager.defaultTile != null) 
                {
                    tile.GetComponent<SpriteRenderer>().sprite = manager.defaultTile.defaultSprite;
                }
            }
        }
        
        Debug.Log($"成功在编辑器中生成了 {w}x{h} 地块！");
    }

    private void ClearGrid(GridManager manager) 
    {
        // 这种删除方式可以撤销 (Undo)
        for (int i = manager.transform.childCount - 1; i >= 0; i--) 
        {
            Undo.DestroyObjectImmediate(manager.transform.GetChild(i).gameObject);
        }
    }
}