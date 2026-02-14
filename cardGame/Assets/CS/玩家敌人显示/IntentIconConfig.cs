using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "IntentIconConfig", menuName = "Game/Intent Icon Config")]
public class IntentIconConfig : ScriptableObject
{
    // ⭐ 注意：IntentType 必须是您在项目中定义的枚举 ⭐
    [Serializable]
    public struct IntentIcon
    {
        public IntentType type;
        public Sprite iconSprite;
    }

    public List<IntentIcon> icons = new List<IntentIcon>();

    private Dictionary<IntentType, Sprite> lookup;

    // 在运行时将 List 转换为 Dictionary 以进行快速查找
    public Sprite GetIcon(IntentType type)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<IntentType, Sprite>();
            foreach (var item in icons)
            {
                // 确保不重复添加
                if (!lookup.ContainsKey(item.type))
                {
                    lookup.Add(item.type, item.iconSprite);
                }
            }
        }
        
        if (lookup.TryGetValue(type, out Sprite icon))
        {
            return icon;
        }
        
        // 返回默认图标或 null
        Debug.LogWarning($"Intent icon not found for type: {type}");
        return null;
    }
}