using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;

/// <summary>
/// 本地化批量处理器，用于批量将所有本地化组件关联到GameTextTable
/// </summary>
public class LocalizationBatchProcessor : EditorWindow
{
    #region 字段
    /// <summary>
    /// 处理结果
    /// </summary>
    private struct ProcessResult
    {
        public int processedCount;
        public int successCount;
        public int failureCount;
    }

    /// <summary>
    /// 进度条
    /// </summary>
    private float progress;
    private string progressMessage;

    /// <summary>
    /// 处理结果
    /// </summary>
    private ProcessResult result;

    /// <summary>
    /// 日志
    /// </summary>
    private List<string> logs = new List<string>();
    private Vector2 logScrollPos;
    #endregion

    #region 编辑器菜单
    /// <summary>
    /// 添加编辑器菜单
    /// </summary>
    [MenuItem("Tools/Localization/Batch Process Localization Components")]
    public static void ShowWindow()
    {
        LocalizationBatchProcessor window = GetWindow<LocalizationBatchProcessor>(true, "Localization Batch Processor");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }
    #endregion

    #region GUI
    /// <summary>
    /// 绘制窗口GUI
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("本地化批量处理器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 处理选项
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("处理选项", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("处理当前场景中的所有LocalizeStringEvent组件"))
        {
            ProcessCurrentScene();
        }

        if (GUILayout.Button("处理所有场景中的LocalizeStringEvent组件"))
        {
            ProcessAllScenes();
        }

        if (GUILayout.Button("处理所有预制体中的LocalizeStringEvent组件"))
        {
            ProcessAllPrefabs();
        }

        if (GUILayout.Button("处理所有资源中的StringReference"))
        {
            ProcessAllStringReferences();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // 进度条
        if (progress > 0 && progress < 1)
        {
            EditorUtility.DisplayProgressBar("本地化批量处理", progressMessage, progress);
        }

        GUILayout.Space(10);

        // 处理结果
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("处理结果", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"处理数量: {result.processedCount}", EditorStyles.label);
        GUILayout.Label($"成功: {result.successCount}", EditorStyles.label);
        GUILayout.Label($"失败: {result.failureCount}", EditorStyles.label);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // 日志
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("日志", EditorStyles.boldLabel);
        GUILayout.Space(5);

        logScrollPos = GUILayout.BeginScrollView(logScrollPos, GUILayout.Height(200));
        foreach (string log in logs)
        {
            GUILayout.Label(log);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // 清空日志
        if (GUILayout.Button("清空日志"))
        {
            logs.Clear();
        }
    }
    #endregion

    #region 处理方法
    /// <summary>
    /// 处理当前场景
    /// </summary>
    private void ProcessCurrentScene()
    {
        logs.Clear();
        result = new ProcessResult();
        progress = 0;

        // 查找当前场景中的所有LocalizeStringEvent组件
        LocalizeStringEvent[] components = FindObjectsOfType<LocalizeStringEvent>();
        ProcessComponents(components, "当前场景");

        logs.Add($"当前场景处理完成，共处理 {result.processedCount} 个组件，成功 {result.successCount} 个，失败 {result.failureCount} 个");
        progress = 1;
        Repaint();
        
        // 关闭进度条
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 处理所有场景
    /// </summary>
    private void ProcessAllScenes()
    {
        logs.Clear();
        result = new ProcessResult();
        progress = 0;

        // 获取所有场景路径
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string scenePath = scenePaths[i];
            progress = (float)i / scenePaths.Length;
            progressMessage = $"正在处理场景: {Path.GetFileName(scenePath)}";
            Repaint();

            // 打开场景
            EditorSceneManager.OpenScene(scenePath);

            // 查找场景中的所有LocalizeStringEvent组件
            LocalizeStringEvent[] components = FindObjectsOfType<LocalizeStringEvent>();
            ProcessComponents(components, Path.GetFileNameWithoutExtension(scenePath));

            // 保存场景
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        logs.Add($"所有场景处理完成，共处理 {result.processedCount} 个组件，成功 {result.successCount} 个，失败 {result.failureCount} 个");
        progress = 1;
        Repaint();
        
        // 关闭进度条
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 处理所有预制体
    /// </summary>
    private void ProcessAllPrefabs()
    {
        logs.Clear();
        result = new ProcessResult();
        progress = 0;

        // 获取所有预制体路径
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            string prefabPath = prefabPaths[i];
            progress = (float)i / prefabPaths.Length;
            progressMessage = $"正在处理预制体: {Path.GetFileName(prefabPath)}";
            Repaint();

            // 加载预制体
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // 查找预制体中的所有LocalizeStringEvent组件
                LocalizeStringEvent[] components = prefab.GetComponentsInChildren<LocalizeStringEvent>(true);
                ProcessComponents(components, Path.GetFileNameWithoutExtension(prefabPath));

                // 保存预制体
                EditorUtility.SetDirty(prefab);
            }
        }

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        logs.Add($"所有预制体处理完成，共处理 {result.processedCount} 个组件，成功 {result.successCount} 个，失败 {result.failureCount} 个");
        progress = 1;
        Repaint();
        
        // 关闭进度条
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 处理所有StringReference
    /// </summary>
    private void ProcessAllStringReferences()
    {
        logs.Clear();
        result = new ProcessResult();
        progress = 0;

        // 获取所有包含StringReference的资产
        string[] assetPaths = AssetDatabase.FindAssets("t:Object")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !path.EndsWith(".unity") && !path.EndsWith(".prefab"))
            .ToArray();

        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetPath = assetPaths[i];
            progress = (float)i / assetPaths.Length;
            progressMessage = $"正在处理资产: {Path.GetFileName(assetPath)}";
            Repaint();

            // 加载资产
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                // 使用序列化方式查找StringReference
                SerializedObject serializedObject = new SerializedObject(asset);
                SerializedProperty property = serializedObject.GetIterator();

                bool hasChanges = false;
                while (property.Next(true))
                {
                    if (property.type == "StringReference")
                    {
                        SerializedProperty tableReferenceProperty = property.FindPropertyRelative("m_TableReference");
                        if (tableReferenceProperty != null)
                        {
                            // 将TableReference设置为GameTextTable
                            Undo.RecordObject(asset, "Update StringReference Table");
                            tableReferenceProperty.stringValue = "GameTextTable";
                            hasChanges = true;
                            result.processedCount++;
                            result.successCount++;
                            logs.Add($"更新资产 {Path.GetFileName(assetPath)} 中的StringReference");
                        }
                    }
                }

                if (hasChanges)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                }
            }
        }

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        logs.Add($"所有StringReference处理完成，共处理 {result.processedCount} 个，成功 {result.successCount} 个，失败 {result.failureCount} 个");
        progress = 1;
        Repaint();
        
        // 关闭进度条
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 处理LocalizeStringEvent组件列表
    /// </summary>
    /// <param name="components">组件列表</param>
    /// <param name="context">上下文信息</param>
    private void ProcessComponents(LocalizeStringEvent[] components, string context)
    {
        foreach (LocalizeStringEvent component in components)
        {
            try
            {
                // 记录撤销操作
                Undo.RecordObject(component, "Update LocalizeStringEvent Table Reference");

                // 设置TableReference为GameTextTable
                component.StringReference.TableReference = "GameTextTable";

                // 保存更改
                EditorUtility.SetDirty(component);

                result.processedCount++;
                result.successCount++;
                logs.Add($"成功更新 {context} 中的 {component.gameObject.name}.{component.GetType().Name}");
            }
            catch (System.Exception e)
            {
                result.processedCount++;
                result.failureCount++;
                logs.Add($"失败更新 {context} 中的 {component.gameObject.name}.{component.GetType().Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 批量添加LocalizeStringEvent组件到所有TextMeshProUGUI
    /// </summary>
    [MenuItem("Tools/Localization/Batch Add LocalizeStringEvent Components")]
    public static void BatchAddLocalizeStringEventComponents()
    {
        // 获取当前场景中的所有TextMeshProUGUI组件
        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        int addedCount = 0;

        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            // 检查是否已经有LocalizeStringEvent组件
            if (textComponent.GetComponent<LocalizeStringEvent>() == null)
            {
                // 添加LocalizeStringEvent组件
                Undo.RecordObject(textComponent.gameObject, "Add LocalizeStringEvent Component");
                LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();
                
                // 设置StringReference
                localizeEvent.StringReference.TableReference = "GameTextTable";
                
                // 添加TextMeshProUGUI作为接收者
                localizeEvent.OnUpdateString.AddListener(textComponent.SetText);
                
                addedCount++;
            }
        }

        Debug.Log($"已成功为 {addedCount} 个TextMeshProUGUI组件添加了LocalizeStringEvent组件");
    }

    /// <summary>
    /// 批量添加LocalizeStringEvent组件到所有场景的TextMeshProUGUI
    /// </summary>
    [MenuItem("Tools/Localization/Batch Add LocalizeStringEvent Components To All Scenes")]
    public static void BatchAddLocalizeStringEventComponentsToAllScenes()
    {
        // 获取所有场景路径，并过滤掉Package目录下的场景
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/")) // 只处理项目Assets目录下的场景
            .ToArray();

        int totalAddedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string scenePath = scenePaths[i];
            Debug.Log($"正在处理场景: {scenePath}");

            try
            {
                // 打开场景
                EditorSceneManager.OpenScene(scenePath);

                // 获取场景中的所有TextMeshProUGUI组件
                TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
                int addedCount = 0;

                foreach (TextMeshProUGUI textComponent in textComponents)
                {
                    // 检查是否已经有LocalizeStringEvent组件
                    if (textComponent.GetComponent<LocalizeStringEvent>() == null)
                    {
                        // 添加LocalizeStringEvent组件
                        Undo.RecordObject(textComponent.gameObject, "Add LocalizeStringEvent Component");
                        LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();
                        
                        // 设置StringReference
                        localizeEvent.StringReference.TableReference = "GameTextTable";
                        
                        // 添加TextMeshProUGUI作为接收者
                        localizeEvent.OnUpdateString.AddListener(textComponent.SetText);
                        
                        addedCount++;
                    }
                }

                // 保存场景
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                totalAddedCount += addedCount;
                Debug.Log($"场景 {Path.GetFileName(scenePath)} 处理完成，添加了 {addedCount} 个LocalizeStringEvent组件");
            }
            catch (System.Exception e)
            {
                // 处理无法打开场景的情况
                skippedCount++;
                Debug.LogWarning($"跳过无法处理的场景: {scenePath}，错误: {e.Message}");
            }
        }

        Debug.Log($"所有场景处理完成，共添加了 {totalAddedCount} 个LocalizeStringEvent组件，跳过了 {skippedCount} 个场景");
    }

    /// <summary>
    /// 批量添加LocalizeStringEvent组件到所有预制体的TextMeshProUGUI
    /// </summary>
    [MenuItem("Tools/Localization/Batch Add LocalizeStringEvent Components To All Prefabs")]
    public static void BatchAddLocalizeStringEventComponentsToAllPrefabs()
    {
        // 获取所有预制体路径
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();

        int totalAddedCount = 0;

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            string prefabPath = prefabPaths[i];
            Debug.Log($"正在处理预制体: {Path.GetFileName(prefabPath)}");

            // 加载预制体
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // 获取预制体中的所有TextMeshProUGUI组件
                TextMeshProUGUI[] textComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                int addedCount = 0;

                foreach (TextMeshProUGUI textComponent in textComponents)
                {
                    // 检查是否已经有LocalizeStringEvent组件
                    if (textComponent.GetComponent<LocalizeStringEvent>() == null)
                    {
                        // 添加LocalizeStringEvent组件
                        Undo.RecordObject(prefab, "Add LocalizeStringEvent Component");
                        LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();
                        
                        // 设置StringReference
                        localizeEvent.StringReference.TableReference = "GameTextTable";
                        
                        // 添加TextMeshProUGUI作为接收者
                        localizeEvent.OnUpdateString.AddListener(textComponent.SetText);
                        
                        addedCount++;
                    }
                }

                totalAddedCount += addedCount;
                Debug.Log($"预制体 {Path.GetFileName(prefabPath)} 处理完成，添加了 {addedCount} 个LocalizeStringEvent组件");
            }
        }

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"所有预制体处理完成，共添加了 {totalAddedCount} 个LocalizeStringEvent组件");
    }

    #endregion

    #region Localize String Event 批量处理增强

    /// <summary>
    /// 批量添加Localize String Event组件到当前场景的所有TMP Text组件
    /// </summary>
    [MenuItem("Tools/Localization/Batch Add LocalizeStringEvent To All TMP Text")]
    public static void BatchAddLocalizeStringEventToAllTMPText()
    {
        // 获取当前场景中的所有TextMeshProUGUI组件
        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        int addedCount = 0;

        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            // 检查是否已经有LocalizeStringEvent组件
            if (textComponent.GetComponent<LocalizeStringEvent>() == null)
            {
                // 添加LocalizeStringEvent组件
                Undo.RecordObject(textComponent.gameObject, "Add LocalizeStringEvent Component");
                LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();
                
                // 设置StringReference
                localizeEvent.StringReference.TableReference = "GameTextTable";
                
                // 添加TextMeshProUGUI作为接收者
                localizeEvent.OnUpdateString.AddListener(textComponent.SetText);
                
                addedCount++;
            }
        }

        Debug.Log($"已成功为 {addedCount} 个TMP Text组件添加了LocalizeStringEvent组件");
    }

    /// <summary>
    /// 批量添加Localize String Event组件到所有场景的所有TMP Text组件
    /// </summary>
    [MenuItem("Tools/Localization/Batch Add LocalizeStringEvent To All TMP Text In All Scenes")]
    public static void BatchAddLocalizeStringEventToAllTMPTextInAllScenes()
    {
        // 获取所有场景路径，并过滤掉Package目录下的场景
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/")) // 只处理项目Assets目录下的场景
            .ToArray();

        int totalAddedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string scenePath = scenePaths[i];
            Debug.Log($"正在处理场景: {scenePath}");

            try
            {
                // 打开场景
                EditorSceneManager.OpenScene(scenePath);

                // 获取场景中的所有TextMeshProUGUI组件
                TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
                int addedCount = 0;

                foreach (TextMeshProUGUI textComponent in textComponents)
                {
                    // 检查是否已经有LocalizeStringEvent组件
                    if (textComponent.GetComponent<LocalizeStringEvent>() == null)
                    {
                        // 添加LocalizeStringEvent组件
                        Undo.RecordObject(textComponent.gameObject, "Add LocalizeStringEvent Component");
                        LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();
                        
                        // 设置StringReference
                        localizeEvent.StringReference.TableReference = "GameTextTable";
                        
                        // 添加TextMeshProUGUI作为接收者
                        localizeEvent.OnUpdateString.AddListener(textComponent.SetText);
                        
                        addedCount++;
                    }
                }

                // 保存场景
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                totalAddedCount += addedCount;
                Debug.Log($"场景 {Path.GetFileName(scenePath)} 处理完成，为 {addedCount} 个TMP Text组件添加了LocalizeStringEvent组件");
            }
            catch (System.Exception e)
            {
                // 处理无法打开场景的情况
                skippedCount++;
                Debug.LogWarning($"跳过无法处理的场景: {scenePath}，错误: {e.Message}");
            }
        }

        Debug.Log($"所有场景处理完成，共为 {totalAddedCount} 个TMP Text组件添加了LocalizeStringEvent组件，跳过了 {skippedCount} 个场景");
    }

    /// <summary>
    /// 批量添加Localize String Event组件到所有预制体的所有TMP Text组件
    /// </summary>
    [MenuItem("Tools/Localization/Batch Add LocalizeStringEvent To All TMP Text In All Prefabs")]
    public static void BatchAddLocalizeStringEventToAllTMPTextInAllPrefabs()
    {
        // 获取所有预制体路径
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();

        int totalAddedCount = 0;

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            string prefabPath = prefabPaths[i];
            Debug.Log($"正在处理预制体: {Path.GetFileName(prefabPath)}");

            // 加载预制体
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // 获取预制体中的所有TextMeshProUGUI组件
                TextMeshProUGUI[] textComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                int addedCount = 0;

                foreach (TextMeshProUGUI textComponent in textComponents)
                {
                    // 检查是否已经有LocalizeStringEvent组件
                    if (textComponent.GetComponent<LocalizeStringEvent>() == null)
                    {
                        // 添加LocalizeStringEvent组件
                        Undo.RecordObject(prefab, "Add LocalizeStringEvent Component");
                        LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();
                        
                        // 设置StringReference
                        localizeEvent.StringReference.TableReference = "GameTextTable";
                        
                        // 添加TextMeshProUGUI作为接收者
                        localizeEvent.OnUpdateString.AddListener(textComponent.SetText);
                        
                        addedCount++;
                    }
                }

                totalAddedCount += addedCount;
                Debug.Log($"预制体 {Path.GetFileName(prefabPath)} 处理完成，为 {addedCount} 个TMP Text组件添加了LocalizeStringEvent组件");
            }
        }

        // 刷新资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"所有预制体处理完成，共为 {totalAddedCount} 个TMP Text组件添加了LocalizeStringEvent组件");
    }

    #endregion
}