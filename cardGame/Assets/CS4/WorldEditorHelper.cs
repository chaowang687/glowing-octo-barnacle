using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class WorldEditorHelper : MonoBehaviour
{
    public GameObject segmentPrefab;
    public ThemeSequenceSO themeSO; 
    
    [Header("引用 Controller 获取层配置")]
    public InfiniteCarouselController controller; // 必须关联场景中的 Controller

    public void LayoutWorld()
    {
        // 1. 检查配置
        if (themeSO == null || themeSO.themes.Count == 0)
        {
            Debug.LogError("未关联 ThemeSO，请先在 Inspector 中检查！");
            return;
        }

        if (controller == null)
        {
            Debug.LogError("请关联场景中的 InfiniteCarouselController 以获取三层配置！");
            return;
        }

        // 2. 安全清理
        UnityEditor.Selection.activeGameObject = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }

        // 3. 遍历 Controller 中定义的所有层 (Ground, Waves, Background 等)
        foreach (var layerConfig in controller.layers)
        {
            CreateEditorLayer(layerConfig);
        }

        // 4. 保存更改
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        
        Debug.Log("三层编辑器地块生成完成！已匹配 Controller 的层配置。");
    }

    private void CreateEditorLayer(InfiniteCarouselController.LayerConfig config)
    {
        int totalSegments = controller.totalSegments;
        float baseRadius = controller.radius;
        float angleStep = 360f / totalSegments;
        
        // 默认预览第一个主题
        var currentTheme = themeSO.themes[0];

        for (int i = 0; i < totalSegments; i++)
        {
            // A. 实例化
            GameObject prefab = config.overridePrefab != null ? config.overridePrefab : segmentPrefab;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            var item = go.GetComponent<WorldSegmentItem>();

            // B. 设置名称和初始化 (非常重要，影响 Refresh 内部的层识别)
            go.name = $"{config.layerName}_{config.isMainLogicLayer}_{config.themeIndexOffset}_{config.syncWithCurrentLevel}";
            item.Initialize(config.layerName, config.isMainLogicLayer, config.themeIndexOffset, config.syncWithCurrentLevel);
            item.parallaxMultiplier = config.speedMultiplier;

            // C. 排序层级预览
            if (item.groundRenderer != null)
                item.groundRenderer.sortingOrder += config.sortingOrderOffset;

            // D. 缩放匹配
            go.transform.localScale = new Vector3(go.transform.localScale.x * controller.overlapFactor, go.transform.localScale.y, 1);

            // E. 刷新资源
            float startAngle = i * angleStep;
            float currentRadius = baseRadius + config.radiusOffset;
            
            // 获取该层对应的资源并刷新
            var layerResource = currentTheme.GetLayerResource(config.layerName);
            item.RefreshWithLayerResource(startAngle, currentRadius, layerResource, currentTheme.nodeMarkerPrefab);
        }
    }
}

[CustomEditor(typeof(WorldEditorHelper))]
public class WorldEditorHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        WorldEditorHelper helper = (WorldEditorHelper)target;
        
        if (helper.controller == null)
        {
            EditorGUILayout.HelpBox("必须关联 Controller 才能读取三层 Layer 配置！", MessageType.Error);
        }

        if (GUILayout.Button("一键生成三层编辑器预览"))
        {
            helper.LayoutWorld();
        }
    }
}