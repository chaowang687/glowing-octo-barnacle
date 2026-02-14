using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ThemeSequence", menuName = "关卡/主题配置")]
public class ThemeSequenceSO : ScriptableObject
{
    [System.Serializable]
    public class LayerResource
    {
        [Tooltip("层名称 - Ground/Waves/Background")]
        public string layerName = "Ground";
        
        [Tooltip("该层的地面贴图")]
        public Sprite groundSprite;
        
        [Tooltip("该层使用的预制体")]
        public GameObject layerPrefab;
        
        [Tooltip("该层的装饰物数组")]
        public GameObject[] decorations;
        
        [Tooltip("该层的材质（可选）")]
        public Material material;
        
        [Tooltip("该层的主色调")]
        public Color tintColor = Color.white;
    }

    [System.Serializable]
    public class ThemeConfig
    {
        [Header("主题信息")]
        public string themeName = "New Theme";
        public int themeID = 0;
        
        [Header("节点标记")]
        public GameObject nodeMarkerPrefab;
        
        [Header("各层资源")]
        public LayerResource groundLayer;
        public LayerResource waveLayer;
        public LayerResource backgroundLayer;
        
        // 简化获取方法
        public LayerResource GetLayerResource(string layerType)
        {
            if (string.IsNullOrEmpty(layerType)) return groundLayer;

            if (layerType.Equals("Ground", System.StringComparison.OrdinalIgnoreCase)) return groundLayer;
            if (layerType.Equals("Waves", System.StringComparison.OrdinalIgnoreCase)) return waveLayer;
            if (layerType.Equals("Background", System.StringComparison.OrdinalIgnoreCase)) return backgroundLayer;
            return groundLayer;
        }
        
        // 向后兼容
        public Sprite groundSprite => groundLayer?.groundSprite;
        public GameObject[] decorations => groundLayer?.decorations;
    }

    public List<ThemeConfig> themes;
    
    // 根据关卡索引获取主题
    public ThemeConfig GetThemeForLevel(int levelIndex)
    {
        if (themes == null || themes.Count == 0)
        {
            Debug.LogError("主题列表为空！");
            return null;
        }
        
        return themes[levelIndex % themes.Count];
    }
}