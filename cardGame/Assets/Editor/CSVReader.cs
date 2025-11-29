using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 简化的 CSV 文件读取器，用于解析 TextAsset 文件。
/// 此脚本已移至 Editor 目录，以解决编译顺序问题。
/// 假设 CSV 文件结构是：第一行是列头（Header），之后是数据行。
/// </summary>
public static class CSVReader
{
    /// <summary>
    /// 解析 TextAsset 中的 CSV 数据。
    /// </summary>
    /// <param name="csvFile">包含 CSV 数据的 TextAsset。</param>
    /// <returns>一个包含字典列表的列表，每个字典代表一行数据，键是列头。</returns>
    public static List<Dictionary<string, string>> ReadCSV(TextAsset csvFile)
    {
        List<Dictionary<string, string>> dataList = new List<Dictionary<string, string>>();
        
        if (csvFile == null)
        {
            Debug.LogError("CSVReader: Input TextAsset is null.");
            return dataList;
        }

        // 按行分割 CSV 文件内容
        // 注意：使用 System.Environment.NewLine 或 \r\n 更健壮，但对于大多数标准 CSV 文件，\n 即可
        string[] lines = csvFile.text.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length <= 1)
        {
            Debug.LogError("CSVReader: CSV file is empty or only contains headers.");
            return dataList;
        }

        // 第一行是列头
        string[] headers = lines[0].Trim().Split(',');

        // 遍历数据行 (从第二行开始)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue; // 跳过空行

            string[] values = line.Split(',');
            
            if (values.Length != headers.Length)
            {
                // 仅在数据缺失时发出警告，而不是在行末的逗号导致空字符串时
                if (values.Length < headers.Length) 
                {
                    Debug.LogWarning($"CSVReader: Line {i + 1} ('{line}') has {values.Length} columns, expected {headers.Length}. Skipping.");
                    continue;
                }
                // 尝试用更少的列数继续，这可能是由于数据格式不规范造成的
            }

            Dictionary<string, string> entry = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                string value = (j < values.Length) ? values[j].Trim() : string.Empty;
                entry.Add(headers[j].Trim(), value);
            }
            dataList.Add(entry);
        }

        Debug.Log($"CSVReader: Successfully parsed {dataList.Count} data entries.");
        return dataList;
    }
}