using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ThemeSequence", menuName = "关卡/主题配置")]
public class ThemeSequenceSO : ScriptableObject
{
    [System.Serializable]
    public class ThemeConfig
    {
        public GameObject nodeMarkerPrefab; // 新增：每个节点的标志图标预制体
        public string themeName;
        public Sprite groundSprite;        // 该场景的无缝方图
        public GameObject[] decorations;  // 该场景的树木、房子等
    }

    public List<ThemeConfig> themes; // 列表顺序：草地、森林、小镇、沙漠、雪地
}