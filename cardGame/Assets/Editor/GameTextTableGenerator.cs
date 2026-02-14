using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// GameTextTable生成器，用于自动生成Main场景需要的GameTextTable条目
/// </summary>
public class GameTextTableGenerator
{
    /// <summary>
    /// 为Main场景生成GameTextTable条目 - 简化版
    /// </summary>
    [MenuItem("Tools/Localization/Generate GameTextTable for Main Scene")]
    public static void GenerateGameTextTableForMainScene()
    {
        // 直接调用CSV导出功能，让用户手动导入到Localization系统
        ExportMainSceneTextToCSV();
        
        Debug.Log("Main scene text exported to CSV. Please import the CSV file into Unity Localization system manually.");
    }
    
    /// <summary>
    /// 为TMP Text组件生成唯一键名
    /// </summary>
    /// <param name="textComponent">TMP Text组件</param>
    /// <returns>生成的键名</returns>
    private static string GenerateKey(TextMeshProUGUI textComponent)
    {
        // 使用GameObject路径作为键名基础
        string path = GetGameObjectPath(textComponent.gameObject);
        
        // 替换特殊字符
        path = path.Replace("/", "_");
        path = path.Replace(" ", "_");
        path = path.Replace(".", "_");
        
        // 添加Text后缀
        path += "_Text";
        
        // 确保键名不超过Unity限制
        if (path.Length > 128)
        {
            path = path.Substring(0, 128);
        }
        
        return path;
    }
    
    /// <summary>
    /// 获取GameObject的完整路径
    /// </summary>
    /// <param name="obj">GameObject</param>
    /// <returns>完整路径</returns>
    private static string GetGameObjectPath(GameObject obj)
    {
        StringBuilder path = new StringBuilder();
        Transform transform = obj.transform;
        
        while (transform != null)
        {
            if (path.Length > 0)
            {
                path.Insert(0, "/" + transform.name);
            }
            else
            {
                path.Append(transform.name);
            }
            transform = transform.parent;
        }
        
        return path.ToString();
    }
    
    /// <summary>
    /// 导出Main场景的文本到CSV文件
    /// </summary>
    [MenuItem("Tools/Localization/Export Main Scene Text to CSV")]
    public static void ExportMainSceneTextToCSV()
    {
        // 打开Main场景
        string mainScenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.OpenScene(mainScenePath);
        
        // 查找场景中的所有TMP Text组件
        TextMeshProUGUI[] textComponents = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);
        
        // 生成CSV内容
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine("Key,zh-CN,en-US");
        
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            // 跳过空文本
            if (string.IsNullOrEmpty(textComponent.text.Trim()))
                continue;
            
            // 生成唯一键名
            string key = GenerateKey(textComponent);
            string text = textComponent.text;
            
            // 添加到CSV
            csvContent.AppendLine($"{key},{EscapeCsvField(text)},{EscapeCsvField(text)}");
        }
        
        // 保存CSV文件
        string csvPath = "Assets/Export/MainSceneText.csv";
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
        File.WriteAllText(csvPath, csvContent.ToString(), Encoding.UTF8);
        
        AssetDatabase.Refresh();
        
        Debug.Log($"Exported {textComponents.Length} text entries to {csvPath}");
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
}
