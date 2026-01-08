using UnityEngine;
using System.Collections.Generic;

namespace Bag
{
    [CreateAssetMenu(fileName = "ItemCodex", menuName = "Bag/Item Codex")]
    public class ItemCodexSO : ScriptableObject
    {
        /// <summary>
        /// 所有物品列表
        /// </summary>
        public List<CodexItem> allItems;
        
        /// <summary>
        /// 物品分类列表
        /// </summary>
        public List<ItemCategory> categories;
    }
    
    /// <summary>
    /// 图鉴物品数据
    /// </summary>
    [System.Serializable]
    public class CodexItem
    {
        /// <summary>
        /// 物品唯一ID
        /// </summary>
        public string itemID;
        
        /// <summary>
        /// 物品名称
        /// </summary>
        public string itemName;
        
        /// <summary>
        /// 物品描述
        /// </summary>
        [TextArea]
        public string description;
        
        /// <summary>
        /// 物品分类
        /// </summary>
        public string category;
        
        /// <summary>
        /// 物品图标
        /// </summary>
        public Sprite icon;
        
        /// <summary>
        /// 物品效果描述
        /// </summary>
        public List<string> effects;
    }
    
    /// <summary>
    /// 物品分类
    /// </summary>
    [System.Serializable]
    public class ItemCategory
    {
        /// <summary>
        /// 分类名称（用于内部识别）
        /// </summary>
        public string name;
        
        /// <summary>
        /// 显示名称（用于UI显示）
        /// </summary>
        public string displayName;
    }
}