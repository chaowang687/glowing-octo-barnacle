using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    /// <summary>
    /// 挖掘数据：定义挖掘场景的配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewDigData", menuName = "SlayTheSpire/Dig Data")]
    public class DigData : ScriptableObject
    {
        [Header("基础信息")]
        public string digName; // 挖掘场景名称
        public NodeType nodeType = NodeType.Dig; // 对应的节点类型
        
        [Header("化石配置")]
    public List<FossilConfig> fossilConfigs = new List<FossilConfig>(); // 化石配置列表
    
    /// <summary>
    /// 化石配置：定义可生成的化石类型和生成规则
    /// </summary>
    [System.Serializable]
    public class FossilConfig
    {
        [Header("化石数据")]
        public FossilData fossilData; // 直接引用化石数据对象
        public string fossilDataPath; // 化石数据的资源路径，用于加载FossilData
        public float spawnChance = 1.0f; // 生成概率（0-1之间）
        
        [Header("生成限制")]
        public int maxPerScene = 1; // 每场景最大生成数量
        public bool allowRotation = true; // 是否允许旋转生成
    }
        
        [Header("生成配置")]
        public int minFossils = 1; // 最小生成化石数量
        public int maxFossils = 3; // 最大生成化石数量
        public int mapWidth = 10; // 挖掘地图宽度
        public int mapHeight = 15; // 挖掘地图高度
        
        [Header("奖励配置")]
        public int minGold = 5; // 最小金币奖励
        public int maxGold = 15; // 最大金币奖励
        public List<string> itemPoolIds = new List<string>(); // 可能掉落的物品池 ID
        
        /// <summary>
        /// 获取随机生成的化石数量
        /// </summary>
        public int GetRandomFossilCount() => Random.Range(minFossils, maxFossils + 1);
        
        /// <summary>
        /// 获取随机金币奖励
        /// </summary>
        public int GetRandomGold() => Random.Range(minGold, maxGold + 1);
    }
    
    [System.Serializable]
    public class FossilConfig
    {
        [Header("化石配置")]
        public string fossilName; // 化石名称
        public string fossilDataPath; // 化石数据的资源路径，用于加载FossilData
        public float spawnChance = 1.0f; // 生成概率（0-1之间）
        
        [Header("生成限制")]
        public int maxPerScene = 1; // 每场景最大生成数量
        public bool allowRotation = true; // 是否允许旋转生成
    }
}