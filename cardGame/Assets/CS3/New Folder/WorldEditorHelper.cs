using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class WorldEditorHelper : MonoBehaviour
{
    public GameObject segmentPrefab;
    public ThemeSequenceSO themeSO; // 关键：在这里拖入你的主题配置文件
    public int totalSegments = 18;
    public float radius = 8f;

    public void LayoutWorld()
    {
        // 1. 检查配置：如果没有主题配置，自动填充会失败
        if (themeSO == null || themeSO.themes.Count == 0)
        {
            Debug.LogError("未关联 ThemeSO 或主题列表为空，请先在 Inspector 中检查！");
            return;
        }

        // 2. 取消选中，防止销毁物体时 Inspector 报错
        UnityEditor.Selection.activeGameObject = null;
        
        // 3. 安全清理旧地块
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            Undo.DestroyObjectImmediate(child);
        }

        float angleStep = 360f / totalSegments;
        
        // 4. 开始循环生成
        for (int i = 0; i < totalSegments; i++)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(segmentPrefab, transform);
            var item = go.GetComponent<WorldSegmentItem>();
            
            float startAngle = i * angleStep;

            // 【核心逻辑】：计算该地块应该对应哪个主题
            // 模仿 Controller 的逻辑，取当前索引对应的主题配置
            int themeIdx = (i / totalSegments) % themeSO.themes.Count;
            ThemeSequenceSO.ThemeConfig currentTheme = themeSO.themes[themeIdx];

            // 调用 Refresh 时传入主题，触发 WorldSegmentItem 内部的自动随机分布逻辑
            item.Refresh(startAngle, radius, currentTheme);
            
            go.name = $"Segment_{i}";
        }

        // 5. 标记场景已更改，确保能按 Command+S 保存生成的装饰
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        
        Debug.Log("地块已自动铺设装饰完成！现在可以使用 WorldTileEditor 手动修整了。");
    }
}

[CustomEditor(typeof(WorldEditorHelper))]
public class WorldEditorHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        WorldEditorHelper helper = (WorldEditorHelper)target;
        
        if (helper.themeSO == null)
        {
            EditorGUILayout.HelpBox("请先关联 Theme SO 资源文件，否则无法自动生成装饰。", MessageType.Warning);
        }

        if (GUILayout.Button("一键生成编辑器地块 (自动填充装饰)"))
        {
            helper.LayoutWorld();
        }
    }
}