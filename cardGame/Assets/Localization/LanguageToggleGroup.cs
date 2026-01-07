using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 语言切换组控制器，兼容 Unity 官方 Localization 系统。
/// 建议将此脚本挂载在包含多个 Toggle 的父物体（ToggleGroup）上。
/// </summary>
[RequireComponent(typeof(ToggleGroup))]
public class LanguageToggleGroup : MonoBehaviour
{
    [Header("语言配置")]
    [Tooltip("对应 Toggle 的顺序，填入 Locale Code (例如: zh-Hans, en, zh-CN)")]
    public List<string> localeCodes = new List<string> { "zh-Hans", "en" };

    private Toggle[] toggles;
    private ToggleGroup group;
    private bool isInitializing = false;

    void Awake()
    {
        group = GetComponent<ToggleGroup>();
    }

    void Start()
    {
        // 获取子物体中所有的 Toggle
        toggles = GetComponentsInChildren<Toggle>();

        if (toggles.Length == 0)
        {
            Debug.LogError("[LanguageToggleGroup] 未在子物体中找到任何 Toggle 组件！请检查 Hierarchy 层级。");
            return;
        }

        // 初始化：根据当前系统语言选中对应的 Toggle
        StartCoroutine(InitializeSelection());
    }

    private IEnumerator InitializeSelection()
    {
        isInitializing = true;

        // 确保本地化系统初始化完成
        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            yield return LocalizationSettings.InitializationOperation;
        }

        // 获取当前选中的语言代码
        string currentCode = LocalizationSettings.SelectedLocale.Identifier.Code;
        Debug.Log($"[LanguageToggleGroup] 当前语言代码为: {currentCode}");

        for (int i = 0; i < toggles.Length; i++)
        {
            if (i < localeCodes.Count)
            {
                // 确保 Group 属性正确
                toggles[i].group = group;

                // 兼容性匹配逻辑
                bool isMatch = localeCodes[i] == currentCode || currentCode.StartsWith(localeCodes[i].Split('-')[0]);
                
                if (isMatch)
                {
                    toggles[i].SetIsOnWithoutNotify(true);
                    Debug.Log($"[LanguageToggleGroup] 初始化选中: {localeCodes[i]}");
                }
                else
                {
                    toggles[i].SetIsOnWithoutNotify(false);
                }

                // 额外保险：为每个 Toggle 添加监听，防止 Inspector 绑定失效
                int index = i;
                toggles[i].onValueChanged.RemoveAllListeners();
                toggles[i].onValueChanged.AddListener((isOn) => OnToggleSelected(isOn));
            }
        }

        isInitializing = false;
    }

    /// <summary>
    /// 核心切换逻辑：由 Toggle 的 OnValueChanged 触发
    /// </summary>
    public void OnToggleSelected(bool isOn)
    {
        // 初始化期间或非勾选动作（!isOn）时不执行
        if (isInitializing || !isOn) return;

        for (int i = 0; i < toggles.Length; i++)
        {
            // 找到当前处于 isOn 状态的那个 Toggle
            if (toggles[i].isOn)
            {
                if (i < localeCodes.Count)
                {
                    Debug.Log($"[LanguageToggleGroup] 触发切换: {localeCodes[i]}");
                    ChangeLanguage(localeCodes[i]);
                }
                break;
            }
        }
    }

    private void ChangeLanguage(string code)
    {
        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        
        if (locale != null)
        {
            if (LocalizationSettings.SelectedLocale != locale)
            {
                LocalizationSettings.SelectedLocale = locale;
                Debug.Log($"[LanguageToggleGroup] 切换成功: {code}");
            }
        }
        else
        {
            Debug.LogError($"[LanguageToggleGroup] 错误: Locale '{code}' 不存在。");
        }
    }
}