using UnityEngine;
using System.Collections.Generic;
using System;

namespace SlayTheSpireMap
{
    public class GameDataManager : MonoBehaviour
    {
        // 单例实例
        public static GameDataManager Instance { get; private set; }
        
        // 玩家状态数据（可序列化）
        [System.Serializable]
        public class PlayerStateData
        {
            public int health = 30;
            public int maxHealth = 30;
            public int gold = 100;
            public List<string> cardIds = new List<string>();
            public List<string> relicIds = new List<string>();
        }
        
        [Header("玩家数据")]
        [SerializeField] private PlayerStateData playerData = new PlayerStateData();
        
        [Header("地图进度")]
        public string currentNodeId = "";
        public List<string> completedNodeIds = new List<string>();
        public List<string> unlockedNodeIds = new List<string>();
        
        [Header("当前战斗数据")]
        public string battleNodeId = "";
        public EncounterData battleEncounterData;
        
        // 事件系统
        public static event Action OnPlayerDataChanged;
        public static event Action OnMapProgressChanged;
        
        #region 单例初始化和持久化
        
        void Awake()
        {
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadGameData(); // 加载存档数据
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            Debug.Log("GameDataManager 初始化完成");
        }
        
        void OnApplicationQuit()
        {
            SaveGameData(); // 退出游戏时自动保存
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGameData(); // 游戏暂停时保存（移动设备）
            }
        }
        
        #endregion
        
        #region 玩家数据访问器（属性）
        
        public int Health 
        { 
            get => playerData.health; 
            set 
            { 
                playerData.health = Mathf.Clamp(value, 0, playerData.maxHealth);
                OnPlayerDataChanged?.Invoke();
            }
        }
        
        public int MaxHealth 
        { 
            get => playerData.maxHealth; 
            set 
            { 
                playerData.maxHealth = Mathf.Max(1, value);
                if (playerData.health > playerData.maxHealth)
                    playerData.health = playerData.maxHealth;
                OnPlayerDataChanged?.Invoke();
            }
        }
        
        public int Gold 
        { 
            get => playerData.gold; 
            set 
            { 
                playerData.gold = Mathf.Max(0, value);
                OnPlayerDataChanged?.Invoke();
            }
        }
        
        public List<string> CardIds => new List<string>(playerData.cardIds);
        public List<string> RelicIds => new List<string>(playerData.relicIds);
        
        #endregion
        
        #region 玩家数据操作方法
        
        public void AddGold(int amount)
        {
            Gold += amount;
        }
        
        public void SpendGold(int amount)
        {
            Gold -= amount;
        }
        
        public void Heal(int amount)
        {
            Health += amount;
        }
        
        public void HealPercentage(int percentage)
        {
            int healAmount = Mathf.RoundToInt(MaxHealth * percentage / 100f);
            Heal(healAmount);
        }
        
        public void TakeDamage(int damage)
        {
            Health -= damage;
        }
        
        public void AddCard(string cardId)
        {
            if (!playerData.cardIds.Contains(cardId))
            {
                playerData.cardIds.Add(cardId);
                OnPlayerDataChanged?.Invoke();
            }
        }
        
        public void RemoveCard(string cardId)
        {
            if (playerData.cardIds.Remove(cardId))
            {
                OnPlayerDataChanged?.Invoke();
            }
        }
        
        public void AddRelic(string relicId)
        {
            if (!playerData.relicIds.Contains(relicId))
            {
                playerData.relicIds.Add(relicId);
                OnPlayerDataChanged?.Invoke();
            }
        }
        
        #endregion
        
        #region 地图进度管理
        
        public void SetCurrentNode(string nodeId)
        {
            currentNodeId = nodeId;
            OnMapProgressChanged?.Invoke();
        }
        
        public void CompleteNode(string nodeId)
        {
            if (!completedNodeIds.Contains(nodeId))
            {
                completedNodeIds.Add(nodeId);
                
                // 从解锁列表中移除已完成节点
                unlockedNodeIds.Remove(nodeId);
                
                OnMapProgressChanged?.Invoke();
            }
        }
        
        public void UnlockNode(string nodeId)
        {
            if (!unlockedNodeIds.Contains(nodeId) && !completedNodeIds.Contains(nodeId))
            {
                unlockedNodeIds.Add(nodeId);
                OnMapProgressChanged?.Invoke();
            }
        }
        
        public bool IsNodeCompleted(string nodeId)
        {
            return completedNodeIds.Contains(nodeId);
        }
        
        public bool IsNodeUnlocked(string nodeId)
        {
            return unlockedNodeIds.Contains(nodeId) || nodeId == currentNodeId;
        }
        
        #endregion
        
        #region 战斗数据管理
        
        public void SetBattleData(string nodeId, EncounterData encounter)
        {
            battleNodeId = nodeId;
            battleEncounterData = encounter;
        }
        
        public void ClearBattleData()
        {
            battleNodeId = "";
            battleEncounterData = null;
        }
        
        #endregion
        
        #region 数据持久化
        
        public void SaveGameData()
        {
            // 使用 PlayerPrefs 保存简单数据
            PlayerPrefs.SetInt("PlayerHealth", playerData.health);
            PlayerPrefs.SetInt("PlayerMaxHealth", playerData.maxHealth);
            PlayerPrefs.SetInt("PlayerGold", playerData.gold);
            PlayerPrefs.SetString("CurrentNodeId", currentNodeId);
            
            // 保存列表数据（转换为JSON）
            SaveListToPlayerPrefs("PlayerCardIds", playerData.cardIds);
            SaveListToPlayerPrefs("PlayerRelicIds", playerData.relicIds);
            SaveListToPlayerPrefs("CompletedNodeIds", completedNodeIds);
            SaveListToPlayerPrefs("UnlockedNodeIds", unlockedNodeIds);
            
            PlayerPrefs.Save();
            Debug.Log("游戏数据已保存");
        }
        
        public void LoadGameData()
        {
            // 如果无存档，使用默认值
            if (!PlayerPrefs.HasKey("PlayerHealth"))
            {
                Debug.Log("无存档数据，使用默认值");
                ResetToDefault();
                return;
            }
            
            // 加载简单数据
            playerData.health = PlayerPrefs.GetInt("PlayerHealth", 30);
            playerData.maxHealth = PlayerPrefs.GetInt("PlayerMaxHealth", 30);
            playerData.gold = PlayerPrefs.GetInt("PlayerGold", 100);
            currentNodeId = PlayerPrefs.GetString("CurrentNodeId", "");
            
            // 加载列表数据
            playerData.cardIds = LoadListFromPlayerPrefs("PlayerCardIds");
            playerData.relicIds = LoadListFromPlayerPrefs("PlayerRelicIds");
            completedNodeIds = LoadListFromPlayerPrefs("CompletedNodeIds");
            unlockedNodeIds = LoadListFromPlayerPrefs("UnlockedNodeIds");
            
            Debug.Log("游戏数据已加载");
        }
        
        public void ResetToDefault()
        {
            // 重置玩家数据
            playerData = new PlayerStateData()
            {
                health = 30,
                maxHealth = 30,
                gold = 100,
                cardIds = new List<string> { "Strike", "Defend", "Strike", "Defend", "Strike" },
                relicIds = new List<string>()
            };
            
            // 重置地图进度
            currentNodeId = "";
            completedNodeIds.Clear();
            unlockedNodeIds.Clear();
            
            // 清空战斗数据
            ClearBattleData();
            
            // 保存重置后的数据
            SaveGameData();
            
            Debug.Log("游戏数据已重置为默认值");
        }
        
        #endregion
        
        #region 辅助方法
        
        private void SaveListToPlayerPrefs(string key, List<string> list)
        {
            // 简单实现：将列表转换为逗号分隔的字符串
            string listString = string.Join(",", list.ToArray());
            PlayerPrefs.SetString(key, listString);
        }
        
        private List<string> LoadListFromPlayerPrefs(string key)
        {
            List<string> list = new List<string>();
            
            if (PlayerPrefs.HasKey(key))
            {
                string listString = PlayerPrefs.GetString(key, "");
                if (!string.IsNullOrEmpty(listString))
                {
                    string[] items = listString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    list.AddRange(items);
                }
            }
            
            return list;
        }
        
        #endregion
        
        #region 调试方法
        
        public void PrintDebugInfo()
        {
            Debug.Log("=== 游戏数据调试信息 ===");
            Debug.Log($"生命: {Health}/{MaxHealth}");
            Debug.Log($"金币: {Gold}");
            Debug.Log($"卡牌数量: {playerData.cardIds.Count}");
            Debug.Log($"遗物数量: {playerData.relicIds.Count}");
            Debug.Log($"当前节点: {currentNodeId}");
            Debug.Log($"已完成节点: {completedNodeIds.Count}个");
            Debug.Log($"已解锁节点: {unlockedNodeIds.Count}个");
            Debug.Log("========================");
        }
        
        #endregion
    }
}