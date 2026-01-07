using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

/// <summary>
/// 本地化Key导出器，用于获取UI文本绑定的本地化Key并导出为CSV
/// </summary>
public class LocalizationKeyExporter
{
    /// <summary>
    /// 导出当前场景中所有UI文本的本地化Key到CSV
    /// </summary>
    [MenuItem("Tools/Localization/Export Localization Keys From Current Scene")]
    public static void ExportLocalizationKeysFromCurrentScene()
    {
        // 获取当前场景路径
        string scenePath = EditorSceneManager.GetActiveScene().path;
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        
        // 查找场景中的所有LocalizeStringEvent组件
        LocalizeStringEvent[] localizeEvents = GameObject.FindObjectsOfType<LocalizeStringEvent>(true);
        
        // 生成CSV内容
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine("Key,zh-CN,en-US,Scene");
        
        int exportedCount = 0;
        foreach (LocalizeStringEvent localizeEvent in localizeEvents)
        {
            // 获取绑定的TMP Text组件
            TextMeshProUGUI textComponent = localizeEvent.GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
                continue;
            
            // 使用反射获取本地化键值（兼容不同Unity版本）
            string entryReference = GetLocalizationKey(localizeEvent);
            
            // 跳过没有设置Entry Reference的组件
            if (string.IsNullOrEmpty(entryReference))
                continue;
            
            // 获取当前显示的文本
            string currentText = textComponent.text;
            
            // 添加到CSV
            csvContent.AppendLine($"{entryReference},{EscapeCsvField(currentText)},{EscapeCsvField(currentText)},{sceneName}");
            exportedCount++;
            
            Debug.Log($"Found localized text: {entryReference} = {currentText}");
        }
        
        // 保存CSV文件
        string csvPath = $"Assets/Export/LocalizationKeys_{sceneName}.csv";
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
        File.WriteAllText(csvPath, csvContent.ToString(), Encoding.UTF8);
        
        AssetDatabase.Refresh();
        
        Debug.Log($"Exported {exportedCount} localization keys from {sceneName} scene to {csvPath}");
    }
    
    /// <summary>
    /// 导出所有场景中UI文本的本地化Key到CSV
    /// </summary>
    [MenuItem("Tools/Localization/Export Localization Keys From All Scenes")]
    public static void ExportLocalizationKeysFromAllScenes()
    {
        // 获取所有场景路径
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/")) // 只处理项目Assets目录下的场景
            .ToArray();
        
        // 生成CSV内容
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine("Key,zh-CN,en-US,Scene");
        
        int totalExportedCount = 0;
        
        foreach (string scenePath in scenePaths)
        {
            // 打开场景
            EditorSceneManager.OpenScene(scenePath);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            
            // 查找场景中的所有LocalizeStringEvent组件
            LocalizeStringEvent[] localizeEvents = GameObject.FindObjectsOfType<LocalizeStringEvent>(true);
            
            int sceneExportedCount = 0;
            foreach (LocalizeStringEvent localizeEvent in localizeEvents)
            {
                // 获取绑定的TMP Text组件
                TextMeshProUGUI textComponent = localizeEvent.GetComponent<TextMeshProUGUI>();
                if (textComponent == null)
                    continue;
                
                // 使用反射获取本地化键值（兼容不同Unity版本）
                string entryReference = GetLocalizationKey(localizeEvent);
                
                // 跳过没有设置Entry Reference的组件
                if (string.IsNullOrEmpty(entryReference))
                    continue;
                
                // 获取当前显示的文本
                string currentText = textComponent.text;
                
                // 添加到CSV
                csvContent.AppendLine($"{entryReference},{EscapeCsvField(currentText)},{EscapeCsvField(currentText)},{sceneName}");
                sceneExportedCount++;
                totalExportedCount++;
            }
            
            Debug.Log($"Exported {sceneExportedCount} localization keys from {sceneName} scene");
        }
        
        // 保存CSV文件
        string csvPath = "Assets/Export/LocalizationKeys_AllScenes.csv";
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
        File.WriteAllText(csvPath, csvContent.ToString(), Encoding.UTF8);
        
        AssetDatabase.Refresh();
        
        // 重新打开之前的场景
        EditorSceneManager.OpenScene(scenePaths[0]);
        
        Debug.Log($"Exported total {totalExportedCount} localization keys from all scenes to {csvPath}");
    }
    
    /// <summary>
    /// 转义CSV字段
    /// </summary>
    /// <param name="field">字段值</param>
    /// <returns>转义后的字段值</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";
        
        // 如果包含逗号、引号或换行符，需要用引号包裹
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            // 替换引号为两个引号
            field = field.Replace("\"", "\"\"");
            // 用引号包裹
            field = $"\"{field}\"";
        }
        
        return field;
    }
    
    /// <summary>
    /// 使用反射获取本地化键值（兼容不同Unity版本）
    /// </summary>
    /// <param name="localizeEvent">LocalizeStringEvent组件</param>
    /// <returns>本地化键值</returns>
    private static string GetLocalizationKey(LocalizeStringEvent localizeEvent)
    {
        try
        {
            // 使用反射获取StringReference
            var stringReferenceField = typeof(LocalizeStringEvent).GetField("m_StringReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stringReferenceField != null)
            {
                var stringReference = stringReferenceField.GetValue(localizeEvent);
                if (stringReference != null)
                {
                    // 获取EntryReference字段
                    var entryReferenceField = stringReference.GetType().GetField("m_EntryReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (entryReferenceField != null)
                    {
                        var entryReference = entryReferenceField.GetValue(stringReference);
                        if (entryReference != null)
                        {
                            // 尝试获取键值
                            // 首先尝试获取StringId属性
                            var stringIdProperty = entryReference.GetType().GetProperty("StringId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (stringIdProperty != null)
                            {
                                var stringId = stringIdProperty.GetValue(entryReference);
                                if (stringId != null)
                                {
                                    return stringId.ToString();
                                }
                            }
                            
                            // 尝试获取KeyValue属性
                            var keyValueProperty = entryReference.GetType().GetProperty("KeyValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (keyValueProperty != null)
                            {
                                var keyValue = keyValueProperty.GetValue(entryReference);
                                if (keyValue != null)
                                {
                                    return keyValue.ToString();
                                }
                            }
                            
                            // 尝试获取m_KeyString字段
                            var keyStringField = entryReference.GetType().GetField("m_KeyString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (keyStringField != null)
                            {
                                var keyString = keyStringField.GetValue(entryReference);
                                if (keyString != null)
                                {
                                    return keyString.ToString();
                                }
                            }
                        }
                    }
                }
            }
            
            // 如果反射失败，尝试其他方法
            // 检查stringReference的公共属性
            var stringReferenceProp = typeof(LocalizeStringEvent).GetProperty("StringReference", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (stringReferenceProp != null)
            {
                var stringReference = stringReferenceProp.GetValue(localizeEvent);
                if (stringReference != null)
                {
                    // 尝试获取Table Entry Reference属性
                    var tableEntryReferenceProp = stringReference.GetType().GetProperty("TableEntryReference", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (tableEntryReferenceProp != null)
                    {
                        var tableEntryReference = tableEntryReferenceProp.GetValue(stringReference);
                        if (tableEntryReference != null)
                        {
                            return tableEntryReference.ToString();
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to get localization key: {e.Message}");
        }
        
        // 如果所有方法都失败，返回空字符串
        return string.Empty;
    }
    
    /// <summary>
    /// 获取所有带有LocalizeStringEvent的TMP Text组件
    /// </summary>
    [MenuItem("Tools/Localization/Find All Localized TMP Text")]
    public static void FindAllLocalizedTMPText()
    {
        // 查找场景中的所有LocalizeStringEvent组件
        LocalizeStringEvent[] localizeEvents = GameObject.FindObjectsOfType<LocalizeStringEvent>(true);
        
        Debug.Log($"Found {localizeEvents.Length} LocalizeStringEvent components in current scene:");
        
        foreach (LocalizeStringEvent localizeEvent in localizeEvents)
        {
            TextMeshProUGUI textComponent = localizeEvent.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                // 使用反射获取本地化键值（兼容不同Unity版本）
                string entryReference = GetLocalizationKey(localizeEvent);
                
                Debug.Log($"GameObject: {localizeEvent.gameObject.name}, Key: {entryReference}, Text: {textComponent.text}");
            }
        }
    }
}
