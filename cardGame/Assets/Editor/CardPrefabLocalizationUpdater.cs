using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>
/// 卡牌预制体本地化更新器，用于为卡牌预制体添加LocalizeStringEvent组件
/// </summary>
public class CardPrefabLocalizationUpdater
{
    /// <summary>
    /// 为卡牌预制体添加LocalizeStringEvent组件
    /// </summary>
    [MenuItem("Tools/Card System/Add LocalizeStringEvent to Card Prefab")]
    public static void AddLocalizeStringEventToCardPrefab()
    {
        // 查找卡牌预制体
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab Card");
        
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning("找不到卡牌预制体: " + prefabPath);
                continue;
            }
            
            // 检查是否已经有CardDisplay组件
            CardDisplay cardDisplay = prefab.GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                Debug.LogWarning("卡牌预制体没有CardDisplay组件: " + prefabPath);
                continue;
            }
            
            // 为名称文本添加LocalizeStringEvent组件
            if (cardDisplay.nameText != null)
            {
                LocalizeStringEvent nameLocalizeEvent = prefab.GetComponent<LocalizeStringEvent>();
                if (nameLocalizeEvent == null)
                {
                    nameLocalizeEvent = prefab.AddComponent<LocalizeStringEvent>();
                }
                
                // 设置引用
                cardDisplay.nameLocalizeEvent = nameLocalizeEvent;
            }
            
            // 为描述文本添加LocalizeStringEvent组件
            if (cardDisplay.descriptionText != null)
            {
                LocalizeStringEvent descriptionLocalizeEvent = prefab.GetComponent<LocalizeStringEvent>();
                if (descriptionLocalizeEvent == null)
                {
                    descriptionLocalizeEvent = prefab.AddComponent<LocalizeStringEvent>();
                }
                
                // 设置引用
                cardDisplay.descriptionLocalizeEvent = descriptionLocalizeEvent;
            }
            
            // 保存修改
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log("已更新卡牌预制体: " + prefabPath);
        }
        
        Debug.Log("卡牌预制体本地化组件添加完成");
    }
    
    /// <summary>
    /// 为指定的卡牌预制体添加LocalizeStringEvent组件
    /// </summary>
    [MenuItem("Tools/Card System/Add LocalizeStringEvent to Selected Card Prefab")]
    public static void AddLocalizeStringEventToSelectedCardPrefab()
    {
        // 检查是否选中了预制体
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("请选中一个卡牌预制体");
            return;
        }
        
        GameObject prefab = Selection.activeGameObject;
        
        // 检查是否是预制体
        if (PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            // 检查是否已经有CardDisplay组件
            CardDisplay cardDisplay = prefab.GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                Debug.LogWarning("卡牌预制体没有CardDisplay组件");
                return;
            }
            
            // 为名称文本添加LocalizeStringEvent组件
            if (cardDisplay.nameText != null)
            {
                LocalizeStringEvent nameLocalizeEvent = prefab.GetComponent<LocalizeStringEvent>();
                if (nameLocalizeEvent == null)
                {
                    nameLocalizeEvent = prefab.AddComponent<LocalizeStringEvent>();
                }
                
                // 设置引用
                cardDisplay.nameLocalizeEvent = nameLocalizeEvent;
            }
            
            // 为描述文本添加LocalizeStringEvent组件
            if (cardDisplay.descriptionText != null)
            {
                LocalizeStringEvent descriptionLocalizeEvent = prefab.GetComponent<LocalizeStringEvent>();
                if (descriptionLocalizeEvent == null)
                {
                    descriptionLocalizeEvent = prefab.AddComponent<LocalizeStringEvent>();
                }
                
                // 设置引用
                cardDisplay.descriptionLocalizeEvent = descriptionLocalizeEvent;
            }
            
            // 保存修改
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log("已更新卡牌预制体: " + prefab.name);
        }
        else
        {
            Debug.LogWarning("请选中一个预制体资产");
        }
    }
}
