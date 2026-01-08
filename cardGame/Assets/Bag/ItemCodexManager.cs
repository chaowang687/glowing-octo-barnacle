using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace Bag
{
    /// <summary>
    /// 物品图鉴管理器
    /// </summary>
    public class ItemCodexManager : MonoBehaviour
    {
        #region 单例模式
        public static ItemCodexManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadCollectedItems();
                
                // 监听背包物品变化事件
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.OnItemChanged += OnItemChanged;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion
        
        #region 字段
        /// <summary>
        /// 图鉴数据
        /// </summary>
        public ItemCodexSO codexData;
        
        /// <summary>
        /// 已收集物品ID集合
        /// </summary>
        private HashSet<string> collectedItems = new HashSet<string>();
        
        /// <summary>
        /// 存档文件路径
        /// </summary>
        private string saveFilePath => Path.Combine(Application.persistentDataPath, "codex_collections.json");
        #endregion
        
        #region 事件
        /// <summary>
        /// 收集状态变化事件
        /// </summary>
        public Action OnCollectionStatusChanged;
        #endregion
        
        #region 公共方法
        /// <summary>
        /// 添加物品到收集列表
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>是否成功添加到收集列表</returns>
        public bool AddToCollection(string itemID)
        {
            if (string.IsNullOrEmpty(itemID)) return false;
            
            if (collectedItems.Add(itemID))
            {
                SaveCollectedItems();
                OnCollectionStatusChanged?.Invoke();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 检查物品是否已收集
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>是否已收集</returns>
        public bool IsCollected(string itemID)
        {
            if (string.IsNullOrEmpty(itemID)) return false;
            return collectedItems.Contains(itemID);
        }
        
        /// <summary>
        /// 获取已收集物品数量
        /// </summary>
        /// <returns>已收集物品数量</returns>
        public int GetCollectedCount()
        {
            return collectedItems.Count;
        }
        
        /// <summary>
        /// 获取总物品数量
        /// </summary>
        /// <returns>总物品数量</returns>
        public int GetTotalItemCount()
        {
            return codexData != null ? codexData.allItems.Count : 0;
        }
        
        /// <summary>
        /// 获取图鉴完成度
        /// </summary>
        /// <returns>完成度百分比</returns>
        public float GetCompletionPercentage()
        {
            int total = GetTotalItemCount();
            if (total == 0) return 0;
            return (float)GetCollectedCount() / total * 100f;
        }
        #endregion
        
        #region 私有方法
        /// <summary>
        /// 处理背包物品变化
        /// </summary>
        /// <param name="item">物品实例</param>
        /// <param name="isAdded">是否是添加操作</param>
        private void OnItemChanged(ItemInstance item, bool isAdded)
        {
            if (isAdded && item != null && item.data != null)
            {
                // 使用itemID作为物品唯一标识符，而不是name
                string itemID = !string.IsNullOrEmpty(item.data.itemID) ? item.data.itemID : item.data.name;
                AddToCollection(itemID);
            }
        }
        
        /// <summary>
        /// 保存收集数据
        /// </summary>
        private void SaveCollectedItems()
        {
            try
            {
                var saveData = new CodexSaveData
                {
                    collectedItems = new List<string>(collectedItems)
                };
                
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"保存图鉴收集数据失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 加载收集数据
        /// </summary>
        private void LoadCollectedItems()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    var saveData = JsonUtility.FromJson<CodexSaveData>(json);
                    collectedItems = new HashSet<string>(saveData.collectedItems);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载图鉴收集数据失败: {e.Message}");
                collectedItems = new HashSet<string>();
            }
        }
        
        /// <summary>
        /// 同步背包中的所有物品到图鉴
        /// </summary>
        private void SyncWithInventory()
        {
            if (InventoryManager.Instance == null) return;
            
            Debug.Log("开始同步背包物品到图鉴...");
            int syncedCount = 0;
            
            foreach (var item in InventoryManager.Instance.AllItemsInBag)
            {
                if (item != null && item.data != null)
                {
                    // 使用itemID作为物品唯一标识符，而不是name
                    string itemID = !string.IsNullOrEmpty(item.data.itemID) ? item.data.itemID : item.data.name;
                    if (AddToCollection(itemID))
                    {
                        syncedCount++;
                        Debug.Log($"已同步物品到图鉴: {itemID}");
                    }
                }
            }
            
            Debug.Log($"背包物品同步完成，共同步 {syncedCount} 个物品到图鉴");
        }
        
        private void OnEnable()
        {
            // 确保与背包同步
            SyncWithInventory();
        }
        #endregion
    }
    
    /// <summary>
    /// 图鉴存档数据
    /// </summary>
    [System.Serializable]
    public class CodexSaveData
    {
        public List<string> collectedItems;
    }
}