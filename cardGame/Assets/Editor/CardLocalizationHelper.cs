using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 卡牌本地化辅助工具，用于为现有卡牌生成本地化键并批量更新
/// </summary>
public class CardLocalizationHelper
{
    /// <summary>
    /// 为所有卡牌生成本地化键
    /// </summary>
    [MenuItem("Tools/Card System/Generate Localization Keys For All Cards")]
    public static void GenerateLocalizationKeysForAllCards()
    {
        // 查找所有CardData资源
        string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
        List<string> csvLines = new List<string>();
        csvLines.Add("Key,zh-CN,en-US");
        
        int updatedCount = 0;
        int skippedCount = 0;
        
        foreach (string guid in cardGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            
            if (cardData == null) continue;
            
            // 跳过已经设置了本地化键的卡牌
            if (!string.IsNullOrEmpty(cardData.cardNameKey) && cardData.cardNameKey != "card_default_name" && 
                !string.IsNullOrEmpty(cardData.descriptionKey) && cardData.descriptionKey != "card_default_description")
            {
                skippedCount++;
                continue;
            }
            
            // 生成唯一键名
            string cardId = string.IsNullOrEmpty(cardData.cardID) ? cardData.name : cardData.cardID;
            string nameKey = $"card_{cardId}_name";
            string descKey = $"card_{cardId}_description";
            
            // 更新卡牌数据
            Undo.RecordObject(cardData, "Update Card Localization Keys");
            cardData.cardNameKey = nameKey;
            cardData.descriptionKey = descKey;
            
            // 添加到CSV
            csvLines.Add($"{nameKey},{cardData.cardName},{cardData.cardName}");
            csvLines.Add($"{descKey},{cardData.description},{cardData.description}");
            
            updatedCount++;
            Debug.Log($"Updated card: {cardData.name}, NameKey: {nameKey}, DescKey: {descKey}");
        }
        
        // 保存CSV文件
        string csvPath = "Assets/Export/CardLocalization.csv";
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
        File.WriteAllText(csvPath, string.Join("\n", csvLines), Encoding.UTF8);
        AssetDatabase.Refresh();
        
        Debug.Log($"Card localization keys generation completed!\nUpdated: {updatedCount} cards\nSkipped: {skippedCount} cards\nCSV saved to: {csvPath}");
    }
    
    /// <summary>
    /// 更新所有卡牌的本地化显示
    /// </summary>
    [MenuItem("Tools/Card System/Update All Card Localization")]
    public static void UpdateAllCardLocalization()
    {
        // 查找所有CardData资源
        string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
        int updatedCount = 0;
        
        foreach (string guid in cardGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            
            if (cardData == null) continue;
            
            // 确保卡牌有本地化键
            if (string.IsNullOrEmpty(cardData.cardNameKey) || cardData.cardNameKey == "card_default_name")
            {
                string cardId = string.IsNullOrEmpty(cardData.cardID) ? cardData.name : cardData.cardID;
                Undo.RecordObject(cardData, "Update Card Localization Key");
                cardData.cardNameKey = $"card_{cardId}_name";
                updatedCount++;
            }
            
            if (string.IsNullOrEmpty(cardData.descriptionKey) || cardData.descriptionKey == "card_default_description")
            {
                string cardId = string.IsNullOrEmpty(cardData.cardID) ? cardData.name : cardData.cardID;
                Undo.RecordObject(cardData, "Update Card Localization Description Key");
                cardData.descriptionKey = $"card_{cardId}_description";
                updatedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Updated localization keys for {updatedCount} cards!");
    }
    
    /// <summary>
    /// 导出所有卡牌的本地化文本到CSV
    /// </summary>
    [MenuItem("Tools/Card System/Export Card Localization To CSV")]
    public static void ExportCardLocalizationToCSV()
    {
        // 查找所有CardData资源
        string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
        List<string> csvLines = new List<string>();
        csvLines.Add("Key,zh-CN,en-US");
        
        foreach (string guid in cardGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            
            if (cardData == null) continue;
            
            // 添加名称和描述到CSV
            csvLines.Add($"{cardData.cardNameKey},{cardData.cardName},{cardData.cardName}");
            csvLines.Add($"{cardData.descriptionKey},{cardData.description},{cardData.description}");
        }
        
        // 保存CSV文件
        string csvPath = "Assets/Export/CardLocalization.csv";
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
        File.WriteAllText(csvPath, string.Join("\n", csvLines), Encoding.UTF8);
        AssetDatabase.Refresh();
        
        Debug.Log($"Card localization exported to CSV: {csvPath}");
    }
}
