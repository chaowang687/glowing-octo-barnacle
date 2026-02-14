using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using CardDataEnums;

/// <summary>
/// 编辑器工具：用于从 CSV 文件导入卡牌数据，并生成 CardData ScriptableObject 资产。
/// 此脚本必须放在项目的 'Editor' 文件夹内。
/// </summary>
public class CardDataImporter : EditorWindow
{
    // 配置字段
    private TextAsset cardDataCSV;
    private string outputAssetPath = "Assets/CS/Resources/CardData/";

    // 用于跟踪 CSV 导入的状态
    private int cardsCreatedCount = 0;
    private int actionsCreatedCount = 0;

    [MenuItem("Tools/Card System/Import Card Data from CSV")]
    public static void ShowWindow()
    {
        // 创建并显示窗口
        EditorWindow.GetWindow(typeof(CardDataImporter), false, "Card Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Card Data CSV Importer", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        // 拖放区域选择 CSV 文件
        cardDataCSV = (TextAsset)EditorGUILayout.ObjectField(
            "Card Data CSV:", 
            cardDataCSV, 
            typeof(TextAsset), 
            false
        );

        // 输出路径配置
        outputAssetPath = EditorGUILayout.TextField("Output Path:", outputAssetPath);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Import Cards"))
        {
            if (cardDataCSV != null)
            {
                ImportCards();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a CSV TextAsset file.", "OK");
            }
        }
        
        EditorGUILayout.Space();
        GUILayout.Label($"Status: {cardsCreatedCount} Cards, {actionsCreatedCount} Actions created.");
    }

    private void ImportCards()
    {
        cardsCreatedCount = 0;
        actionsCreatedCount = 0;

        // 1. 确保输出路径存在
        if (!Directory.Exists(outputAssetPath))
        {
            Directory.CreateDirectory(outputAssetPath);
            Debug.Log($"Created directory: {outputAssetPath}");
        }

        // 2. 读取 CSV 数据
        List<Dictionary<string, string>> cardEntries = CSVReader.ReadCSV(cardDataCSV);
        
        if (cardEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("Import Failed", "No valid card entries found in CSV.", "OK");
            return;
        }

        // 3. 遍历每一行数据并创建 CardData 资产
        foreach (var entry in cardEntries)
        {
            ProcessCardEntry(entry);
        }

        // 4. 刷新 Unity 资产数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Import Complete", 
                                    $"Successfully imported {cardsCreatedCount} cards and {actionsCreatedCount} actions.", 
                                    "Finish");
    }

    /// <summary>
    /// 处理单个卡牌数据行，创建或更新 CardData 资产。
    /// 已更新以匹配用户提供的 CSV 列名 (Name, RequiredClass, EffectN_Type等)。
    /// </summary>
    private void ProcessCardEntry(Dictionary<string, string> entry)
    {
        string cardID = GetStringValue(entry, "CardID");
        if (string.IsNullOrEmpty(cardID))
        {
            Debug.LogError("Skipping card: CardID is missing or empty.");
            return;
        }

        string assetPath = $"{outputAssetPath}{cardID}.asset";
        CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);

        // 如果资产不存在，则创建新的
        if (cardData == null)
        {
            cardData = ScriptableObject.CreateInstance<CardData>();
            AssetDatabase.CreateAsset(cardData, assetPath);
            cardsCreatedCount++;
        }
        
        // 核心属性 - 使用用户表格中的列名
        cardData.cardID = cardID;
        // 注意：这里使用 Name 字段，而不是 CardName
        cardData.cardName = GetStringValue(entry, "Name", "New Card"); 
        // 成本字段
        cardData.energyCost = GetIntValue(entry, "Cost", 1); 

        // 确保 Description 等字段存在 (即使您的表格中省略了)
        cardData.description = GetStringValue(entry, "Description", "No description.");
        cardData.isUpgraded = GetBoolValue(entry, "IsUpgraded", false);

        // 枚举属性
        cardData.rarity = GetEnumValue<Rarity>(entry, "Rarity", Rarity.Common);
        // 注意：这里使用 RequiredClass 字段，而不是 Class
        cardData.requiredClass = GetEnumValue<CardClass>(entry, "RequiredClass", CardClass.Any); 
        cardData.type = GetEnumValue<CardType>(entry, "Type", CardType.Attack);

        // 清空旧的 Actions 列表，准备填充新的
        cardData.actions.Clear();
        
        // --- 解析 CardActions (最多支持 3 个 Action) ---
        for (int i = 1; i <= 3; i++)
        {
            // 采用用户表格中的 EffectN_Type 格式
            string effectTypeKey = $"Effect{i}_Type";
            if (entry.ContainsKey(effectTypeKey) && !string.IsNullOrEmpty(entry[effectTypeKey]))
            {
                CardAction action = new CardAction();
                
                // 1. 解析 EffectType (必须存在)
                action.effectType = GetEnumValue<EffectType>(entry, effectTypeKey, EffectType.None);
                if (action.effectType == EffectType.None) continue; // 无效效果类型则跳过

                // 2. 解析 TargetType (采用用户表格中的 EffectN_Target 格式)
                string targetTypeKey = $"Effect{i}_Target";
                action.targetType = GetEnumValue<TargetType>(entry, targetTypeKey, TargetType.SelectedEnemy);
                
                // 3. 解析 Value (采用用户表格中的 EffectN_Value 格式)
                string valueKey = $"Effect{i}_Value";
                action.value = GetIntValue(entry, valueKey, 0);

                // --- 状态效果相关字段 (非必需，使用默认值) ---
                // ScalesWithStatus (默认为 false)
                string scalesKey = $"ScalesWithStatus{i}";
                action.scalesWithStatus = GetBoolValue(entry, scalesKey, false);

                // StatusEffect (默认为 None)
                string statusEffectKey = $"StatusEffect{i}";
                action.statusEffect = GetEnumValue<StatusEffect>(entry, statusEffectKey, StatusEffect.None);
                
                // Duration (默认为 Value)
                string durationKey = $"Duration{i}";
                action.duration = GetIntValue(entry, durationKey, action.value);


                cardData.actions.Add(action);
                actionsCreatedCount++;
            }
        }
        
        // 标记资产为已修改，以便保存
        EditorUtility.SetDirty(cardData);
        Debug.Log($"Processed Card: {cardID}");
    }

    // =================================================================
    // 助手方法：安全获取 CSV 值
    // =================================================================

    private string GetStringValue(Dictionary<string, string> entry, string key, string defaultValue = "")
    {
        if (entry.TryGetValue(key, out string value))
        {
            return value;
        }
        return defaultValue;
    }

    private int GetIntValue(Dictionary<string, string> entry, string key, int defaultValue = 0)
    {
        if (entry.TryGetValue(key, out string value) && int.TryParse(value, out int result))
        {
            return result;
        }
        return defaultValue;
    }

    private bool GetBoolValue(Dictionary<string, string> entry, string key, bool defaultValue = false)
    {
        if (entry.TryGetValue(key, out string value))
        {
            // 接受 "TRUE", "T", "1", "YES"
            return value.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("T", System.StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1") ||
                   value.Equals("YES", System.StringComparison.OrdinalIgnoreCase);
        }
        return defaultValue;
    }

    private T GetEnumValue<T>(Dictionary<string, string> entry, string key, T defaultValue) where T : System.Enum
    {
        if (entry.TryGetValue(key, out string value))
        {
            try
            {
                // 尝试解析，忽略大小写
                return (T)System.Enum.Parse(typeof(T), value, true);
            }
            catch
            {
                // 如果解析失败，可能是因为该列不存在或值无效。
                // 仅在调试时打印警告。
                // Debug.LogWarning($"Could not parse enum {typeof(T).Name} from value: '{value}' in key: {key}. Using default value: {defaultValue}.");
            }
        }
        return defaultValue;
    }
}