using UnityEngine;

namespace ScavengingGame
{
    /// <summary>
    /// 物品数据定义（作为 ScriptableObject 以便在 Unity Editor 中创建）
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Scavenge/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("物品的名称，用于显示和日志记录")]
        public string ItemName = "Unknown Item";
        
        [TextArea]
        public string Description = "A generic scavenged item.";
        
        [Tooltip("物品在 UI 中显示的图标")]
        public Sprite Icon; 
        
        [Tooltip("物品的堆叠大小（例如：子弹数量）")]
        public int StackSize = 1;
    }
}