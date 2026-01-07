using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 卡牌本地化导入器，用于将CSV数据导入到Unity本地化表格
/// </summary>
public class CardLocalizationImporter
{
    /// <summary>
    /// 从CSV文件导入卡牌本地化数据到Unity本地化表格
    /// </summary>
    [MenuItem("Tools/Card System/Import Card Localization From CSV")]
    public static void ImportCardLocalizationFromCSV()
    {
        // CSV文件路径
        string csvPath = "Assets/Export/CardLocalization.csv";
        
        if (!File.Exists(csvPath))
        {
            Debug.LogError("CSV文件不存在: " + csvPath);
            return;
        }
        
        // 读取CSV内容
        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        
        if (lines.Length < 2)
        {
            Debug.LogError("CSV文件格式错误，至少需要两行");
            return;
        }
        
        // 解析表头
        string[] headers = lines[0].Split(',');
        if (headers.Length < 3)
        {
            Debug.LogError("CSV文件格式错误，需要至少包含Key和两种语言");
            return;
        }
        
        // 获取本地化设置
        LocalizationSettings localizationSettings = LocalizationSettings.Instance;
        if (localizationSettings == null)
        {
            Debug.LogError("LocalizationSettings.Instance 为 null");
            return;
        }
        
        // 获取本地化表格
        StringTableCollection stringTableCollection = null;
        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            if (collection.TableCollectionName == "GameTextTable")
            {
                stringTableCollection = collection;
                break;
            }
        }
        
        if (stringTableCollection == null)
        {
            Debug.LogError("找不到名为GameTextTable的本地化表格集合");
            return;
        }
        
        // 创建语言到StringTable的映射
        Dictionary<string, StringTable> languageToTable = new Dictionary<string, StringTable>();
        foreach (var tableEntry in stringTableCollection.StringTables)
        {
            string languageCode = tableEntry.LocaleIdentifier.Code;
            languageToTable[languageCode] = tableEntry;
        }
        
        // 导入数据
        int importedCount = 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] columns = line.Split(',');
            if (columns.Length < headers.Length)
            {
                Debug.LogWarning("第" + (i + 1) + "行格式错误: " + line);
                continue;
            }
            
            string key = columns[0];
            
            // 为每种语言导入数据
            for (int j = 1; j < headers.Length; j++)
            {
                string languageCode = headers[j];
                string value = columns[j];
                
                if (languageToTable.TryGetValue(languageCode, out StringTable table))
                {
                    // 尝试获取现有条目
                    var entry = table.GetEntry(key);
                    
                    if (entry != null)
                    {
                        // 如果键已存在，则更新值
                        entry.Value = value;
                        importedCount++;
                    }
                    else
                    {
                        // 否则，创建新的键值对
                        table.AddEntry(key, value);
                        importedCount++;
                    }
                }
                else
                {
                    Debug.LogWarning("不支持的语言: " + languageCode);
                }
            }
        }
        
        // 保存修改
        EditorUtility.SetDirty(stringTableCollection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("成功导入卡牌本地化数据，共导入" + importedCount + "条记录");
    }
    
    /// <summary>
    /// 从CSV文件导入卡牌本地化数据到Unity本地化表格（指定CSV文件）
    /// </summary>
    [MenuItem("Tools/Card System/Import Card Localization From CSV (Custom Path)")]
    public static void ImportCardLocalizationFromCSVCustomPath()
    {
        string csvPath = EditorUtility.OpenFilePanel("选择卡牌本地化CSV文件", "Assets/Export", "csv");
        
        if (string.IsNullOrEmpty(csvPath))
        {
            Debug.Log("用户取消了文件选择");
            return;
        }
        
        // 读取CSV内容
        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        
        if (lines.Length < 2)
        {
            Debug.LogError("CSV文件格式错误，至少需要两行");
            return;
        }
        
        // 解析表头
        string[] headers = lines[0].Split(',');
        if (headers.Length < 3)
        {
            Debug.LogError("CSV文件格式错误，需要至少包含Key和两种语言");
            return;
        }
        
        // 获取本地化设置
        LocalizationSettings localizationSettings = LocalizationSettings.Instance;
        if (localizationSettings == null)
        {
            Debug.LogError("LocalizationSettings.Instance 为 null");
            return;
        }
        
        // 获取本地化表格
        StringTableCollection stringTableCollection = null;
        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
        {
            if (collection.TableCollectionName == "GameTextTable")
            {
                stringTableCollection = collection;
                break;
            }
        }
        
        if (stringTableCollection == null)
        {
            Debug.LogError("找不到名为GameTextTable的本地化表格集合");
            return;
        }
        
        // 创建语言到StringTable的映射
        Dictionary<string, StringTable> languageToTable = new Dictionary<string, StringTable>();
        foreach (var tableEntry in stringTableCollection.StringTables)
        {
            string languageCode = tableEntry.LocaleIdentifier.Code;
            languageToTable[languageCode] = tableEntry;
        }
        
        // 导入数据
        int importedCount = 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] columns = line.Split(',');
            if (columns.Length < headers.Length)
            {
                Debug.LogWarning("第" + (i + 1) + "行格式错误: " + line);
                continue;
            }
            
            string key = columns[0];
            
            // 为每种语言导入数据
            for (int j = 1; j < headers.Length; j++)
            {
                string languageCode = headers[j];
                string value = columns[j];
                
                if (languageToTable.TryGetValue(languageCode, out StringTable table))
                {
                    // 尝试获取现有条目
                    var entry = table.GetEntry(key);
                    
                    if (entry != null)
                    {
                        // 如果键已存在，则更新值
                        entry.Value = value;
                        importedCount++;
                    }
                    else
                    {
                        // 否则，创建新的键值对
                        table.AddEntry(key, value);
                        importedCount++;
                    }
                }
                else
                {
                    Debug.LogWarning("不支持的语言: " + languageCode);
                }
            }
        }
        
        // 保存修改
        EditorUtility.SetDirty(stringTableCollection);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("成功导入卡牌本地化数据，共导入" + importedCount + "条记录");
    }
}
