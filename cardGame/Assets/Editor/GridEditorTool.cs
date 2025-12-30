using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))] // 关联你的 GridManager 脚本
public class GridEditorTool : Editor 
{
    public override void OnInspectorGUI() 
    {
        // 绘制默认的 Inspector 内容
        DrawDefaultInspector();

        GridManager manager = (GridManager)target;

        GUILayout.Space(10);
        GUILayout.Label("编辑器生成工具", EditorStyles.boldLabel);

        if (GUILayout.Button("生成 4x5 测试地块")) 
        {
            GenerateStaticGrid(manager, 4, 5);
        }

        if (GUILayout.Button("清除所有地块")) 
        {
            ClearGrid(manager);
        }
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