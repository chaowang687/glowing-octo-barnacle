using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bag.UI
{
    /// <summary>
    /// 图鉴物品条目
    /// </summary>
    public class ItemCodexEntry : MonoBehaviour
    {
        #region 字段
        [Header("UI组件")]
        public Image iconImage; // 物品图标
        public TextMeshProUGUI nameText; // 物品名称
        public Image collectedIndicator; // 收集指示器
        public Button entryButton; // 条目按钮
        
        [Header("状态")]
        private CodexItem codexItem; // 关联的图鉴物品
        #endregion
        
        #region 事件
        /// <summary>
        /// 物品点击事件
        /// </summary>
        public System.Action<CodexItem> OnItemClicked;
        #endregion
        
        #region 生命周期
        private void Awake()
        {
            // 注册按钮点击事件
            if (entryButton != null)
            {
                entryButton.onClick.AddListener(OnButtonClicked);
            }
        }
        #endregion
        
        #region 公共方法
        /// <summary>
        /// 初始化物品条目
        /// </summary>
        /// <param name="item">物品数据</param>
        public void Initialize(CodexItem item)
        {
            codexItem = item;
            
            // 设置物品名称
            if (nameText != null)
            {
                nameText.text = item.itemName;
            }
            
            // 设置物品图标
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.gameObject.SetActive(item.icon != null);
            }
            
            // 更新收集状态
            UpdateCollectionStatus();
        }
        #endregion
        
        #region 私有方法
        /// <summary>
        /// 更新收集状态
        /// </summary>
        private void UpdateCollectionStatus()
        {
            if (collectedIndicator != null)
            {
                // 确保itemID不为空
                if (string.IsNullOrEmpty(codexItem.itemID))
                {
                    Debug.LogWarning($"物品 {codexItem.itemName} 的itemID为空，无法检查收集状态");
                    return;
                }
                
                bool isCollected = ItemCodexManager.Instance.IsCollected(codexItem.itemID);
                collectedIndicator.gameObject.SetActive(isCollected);
                
                // 根据收集状态调整样式
                if (isCollected)
                {
                    iconImage.color = Color.white;
                    nameText.color = Color.white;
                }
                else
                {
                    iconImage.color = Color.gray;
                    nameText.color = Color.gray;
                }
            }
        }
        
        /// <summary>
        /// 按钮点击事件处理
        /// </summary>
        private void OnButtonClicked()
        {
            OnItemClicked?.Invoke(codexItem);
        }
        #endregion
    }
}