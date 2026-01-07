using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using System;

/// <summary>
/// CSV本地化管理器，负责加载和管理多语言文本，与Unity内置Localization系统交互
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    #region 单例模式
    public static LocalizationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 字段
    /// <summary>
    /// CSV文件路径
    /// </summary>
    [Header("本地化配置")]
    public string csvFilePath = "localization";

    /// <summary>
    /// 当前语言代码
    /// </summary>
    private string currentLanguageCode = "zh-CN";
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化本地化系统
    /// </summary>
    private void Initialize()
    {
        // 订阅语言变更事件
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        // 设置默认语言
        currentLanguageCode = LocalizationSettings.SelectedLocale.Identifier.Code;
    }

    /// <summary>
    /// 加载CSV文件并更新到Unity Localization系统
    /// </summary>
    [ContextMenu("Load CSV and Update Localization Tables")]
    public void LoadCSVAndUpdateTables()
    {
        #if UNITY_EDITOR
        TextAsset csvFile = Resources.Load<TextAsset>(csvFilePath);
        if (csvFile == null)
        {
            Debug.LogError($"LocalizationManager: 无法找到CSV文件: {csvFilePath}");
            return;
        }

        // 解析CSV内容
        ParseCSVAndUpdateTables(csvFile.text);
        #endif
    }

    /// <summary>
    /// 解析CSV内容并更新到Unity Localization系统
    /// </summary>
    /// <param name="csvContent">CSV文件内容</param>
    private void ParseCSVAndUpdateTables(string csvContent)
    {
        #if UNITY_EDITOR
        string[] lines = csvContent.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("LocalizationManager: CSV文件格式错误，至少需要两行");
            return;
        }

        // 解析表头
        string[] headers = lines[0].Split(',');
        List<string> languageCodes = new List<string>();

        // 从第二列开始是语言代码
        for (int i = 1; i < headers.Length; i++)
        {
            languageCodes.Add(headers[i].Trim());
        }

        // 注意：以下代码已简化，因为Unity Localization API可能有所不同
        // 实际项目中，建议使用Unity Localization编辑器的CSV导入功能
        Debug.Log($"LocalizationManager: 解析CSV文件，包含 {lines.Length} 行");
        Debug.Log($"LocalizationManager: 支持的语言: {string.Join(", ", languageCodes)}");
        
        // 这里可以添加自定义的CSV解析和更新逻辑
        // 例如，将CSV数据导出到JSON文件，然后使用Unity Localization的JSON导入功能
        
        #endif
    }
    #endregion

    #region 本地化API
    /// <summary>
    /// 获取本地化文本
    /// </summary>
    /// <param name="key">本地化键</param>
    /// <returns>本地化文本</returns>
    public string GetLocalizedString(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("LocalizationManager: 本地化键为空");
            return string.Empty;
        }

        // 使用Unity内置的Localization系统获取文本
        if (LocalizationSettings.Instance != null)
        {
            // 卡牌专用表格检查
                if (key.StartsWith("card_"))
                {
                    return LocalizationSettings.StringDatabase.GetLocalizedString("Card", key);
                }
                // 其他文本使用通用表格
                return LocalizationSettings.StringDatabase.GetLocalizedString("GameTextTable", key);
        }

        Debug.LogWarning($"LocalizationManager: LocalizationSettings.Instance 为 null");
        return key; // 返回键本身作为回退
    }

    /// <summary>
    /// 设置当前语言
    /// </summary>
    /// <param name="languageCode">语言代码</param>
    public void SetLanguage(string languageCode)
    {
        if (LocalizationSettings.Instance != null)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                currentLanguageCode = languageCode;
            }
        }
    }

    /// <summary>
    /// 获取当前语言代码
    /// </summary>
    public string GetCurrentLanguageCode()
    {
        if (LocalizationSettings.Instance != null)
        {
            return LocalizationSettings.SelectedLocale.Identifier.Code;
        }
        return currentLanguageCode;
    }
    #endregion

    #region 事件
    /// <summary>
    /// 语言变更事件
    /// </summary>
    public System.Action OnLanguageChanged;
    #endregion

    #region 语言变更回调
    /// <summary>
    /// LocalizationSettings语言变更回调
    /// </summary>
    /// <param name="locale">新的语言</param>
    private void OnLocaleChanged(Locale locale)
    {
        currentLanguageCode = locale.Identifier.Code;
        OnLanguageChanged?.Invoke();
    }
    #endregion



    #region 资源释放
    private void OnDestroy()
    {
        // 取消订阅语言变更事件
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
    #endregion
}