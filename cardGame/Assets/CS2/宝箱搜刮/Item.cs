using UnityEngine;

namespace ScavengingGame
{
    [RequireComponent(typeof(SpriteRenderer))]  // 自动添加 SpriteRenderer
    public class Item : MonoBehaviour 
    {
        [Tooltip("该物品实例引用的静态数据 (ItemData ScriptableObject)。")]
        [SerializeField] 
        private ItemData itemData; 
        
        private SpriteRenderer spriteRenderer;  // SpriteRenderer 引用
        
        // 公共属性
        public ItemData Data => itemData; 
        public ItemRarity Rarity => itemData != null ? itemData.rarity : ItemRarity.Common;

        // ==================== 初始化 ====================
        
        void Awake()
        {
            // 1. 获取 SpriteRenderer 组件
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"物品 {gameObject.name} 缺少 SpriteRenderer 组件！", this);
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // 2. 如果已经在 Inspector 中设置了 itemData，更新显示
            if (itemData != null)
            {
                UpdateVisuals();
            }
        }
        
        void Start()
        {
            // 二次验证，确保显示正确
            if (spriteRenderer.sprite == null && itemData != null)
            {
                UpdateVisuals();
            }
        }

        // ==================== 数据设置 ====================
        
        public void SetData(ItemData data)
        {
            if (data == null)
            {
                Debug.LogError("试图使用空的 ItemData 设置 Item 组件。");
                return;
            }
            
            itemData = data;
            UpdateVisuals();  // 关键：设置数据后立即更新显示
            
            Debug.Log($"设置物品数据: {data.itemName}");
        }

        // ==================== 视觉更新 ====================
        
        private void UpdateVisuals()
        {
            if (spriteRenderer == null)
            {
                Debug.LogWarning("SpriteRenderer 为空，尝试重新获取");
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null) return;
            }
            
            if (itemData == null)
            {
                Debug.LogWarning("ItemData 为空，无法更新视觉");
                spriteRenderer.sprite = null;
                return;
            }
            
            if (itemData.icon == null)
            {
                Debug.LogWarning($"ItemData {itemData.itemName} 的图标为空");
                spriteRenderer.sprite = null;
                spriteRenderer.color = new Color(1, 0, 1, 1); // 洋红色占位
            }
            else
            {
                spriteRenderer.sprite = itemData.icon;
                spriteRenderer.color = Color.white;
                
                // 设置排序图层（如果需要）
                spriteRenderer.sortingLayerName = "Items";
                spriteRenderer.sortingOrder = 10;
                
                Debug.Log($"更新物品视觉: {itemData.itemName}, 图标: {itemData.icon.name}");
            }
        }

        // ==================== 调试方法 ====================
        
        void OnValidate()
        {
            // 在 Inspector 中更改时立即预览
            if (Application.isPlaying) return;
            
            if (itemData != null)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = itemData.icon;
                }
            }
        }
        
        // ==================== 拾取方法 ====================
        
        public void Pickup()
        {
            // 先验证数据
            if (itemData == null)
            {
                Debug.LogError($"物品 {gameObject.name} 没有设置 ItemData！", this);
                return;
            }
            
            // 查找库存管理器
            InventoryManager inventory = FindObjectOfType<InventoryManager>();
            if (inventory == null)
            {
                Debug.LogError("找不到 InventoryManager！");
                return;
            }
            
            // 尝试添加到库存
            bool success = inventory.AddItem(itemData, 1);
            
            if (success)
            {
                Debug.Log($"成功拾取: {itemData.itemName}");
                
                // 播放音效
                if (itemData.pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
                }
                
                // 销毁物品
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning($"拾取失败: {itemData.itemName} (库存可能已满)");
            }
        }
        
        // ==================== 可视化编辑器扩展 ====================
        
        #if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // 在场景视图中显示拾取范围
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
        #endif
    }
}