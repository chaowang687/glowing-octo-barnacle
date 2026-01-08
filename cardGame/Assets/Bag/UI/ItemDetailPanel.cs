using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Bag.UI
{
    /// <summary>
    /// 物品详情面板
    /// </summary>
    public class ItemDetailPanel : MonoBehaviour
    {
        #region 字段
        [Header("UI组件")]
        public TextMeshProUGUI itemNameText; // 物品名称
        public TextMeshProUGUI descriptionText; // 物品描述
        public Image iconImage; // 物品图标
        public Transform effectsContainer; // 效果容器
        public GameObject effectItemPrefab; // 效果条目预制体
        public GameObject collectedIndicator; // 已收集指示器
        public GameObject notCollectedIndicator; // 未收集指示器
        #endregion
        
        #region 公共方法
        /// <summary>
        /// 显示物品详情（使用CodexItem数据）
        /// </summary>
        /// <param name="item">物品数据</param>
        public void ShowItem(CodexItem item)
        {
            if (item == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // 设置物品名称
            if (itemNameText != null)
            {
                itemNameText.text = item.itemName;
            }
            
            // 设置物品描述
            if (descriptionText != null)
            {
                descriptionText.text = item.description;
            }
            
            // 设置物品图标
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.gameObject.SetActive(item.icon != null);
            }
            
            // 更新收集状态
            UpdateCollectionStatus(item.itemID);
            
            // 更新效果列表
            UpdateEffects(item.effects);
            
            // 显示面板
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 显示物品详情（使用静态资源ItemData）
        /// </summary>
        /// <param name="itemData">物品数据ScriptableObject</param>
        public void ShowItem(Bag.ItemData itemData)
        {
            if (itemData == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // 设置物品名称
            if (itemNameText != null)
            {
                itemNameText.text = itemData.itemName;
            }
            
            // 设置物品描述
            if (descriptionText != null)
            {
                // 使用ItemData中的description字段
                string description = itemData.description;
                
                // 如果描述为空，使用默认描述
                if (string.IsNullOrEmpty(description))
                {
                    description = "这是一个物品。";
                }
                
                descriptionText.text = description;
            }
            
            // 设置物品图标
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.gameObject.SetActive(itemData.icon != null);
            }
            
            // 更新收集状态
            UpdateCollectionStatus(itemData.name); // 使用ItemData的name作为唯一标识符
            
            // 更新效果列表
            List<string> effects = new List<string>();
            if (itemData.effects != null && itemData.effects.Count > 0)
            {
                foreach (var effect in itemData.effects)
                {
                    effects.Add(effect.name); // 使用效果对象的名称作为效果描述
                }
            }
            UpdateEffects(effects);
            
            // 显示面板
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 关闭详情面板
        /// </summary>
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
        #endregion
        
        #region 私有方法
        /// <summary>
        /// 更新收集状态
        /// </summary>
        /// <param name="itemID">物品ID</param>
        private void UpdateCollectionStatus(string itemID)
        {
            bool isCollected = ItemCodexManager.Instance.IsCollected(itemID);
            
            if (collectedIndicator != null)
            {
                collectedIndicator.SetActive(isCollected);
            }
            
            if (notCollectedIndicator != null)
            {
                notCollectedIndicator.SetActive(!isCollected);
            }
        }
        
        /// <summary>
        /// 更新效果列表
        /// </summary>
        /// <param name="effects">效果列表</param>
        private void UpdateEffects(List<string> effects)
        {
            // 清空现有效果
            foreach (Transform child in effectsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 创建效果条目
            if (effects != null && effects.Count > 0)
            {
                foreach (var effect in effects)
                {
                    GameObject effectObj = Instantiate(effectItemPrefab, effectsContainer);
                    TextMeshProUGUI effectText = effectObj.GetComponent<TextMeshProUGUI>();
                    
                    if (effectText != null)
                    {
                        effectText.text = effect;
                    }
                }
            }
        }
        #endregion
    }
}