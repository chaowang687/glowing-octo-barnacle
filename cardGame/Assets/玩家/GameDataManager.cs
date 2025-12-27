using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // 必须添加这个引用
namespace SlayTheSpireMap
{
    public class GameDataManager : MonoBehaviour
    {



        public string characterName = "勇者"; // 添加这个字段
        public static GameDataManager Instance { get; private set; }
        


        // 玩家状态数据
        [System.Serializable]
        public class PlayerStateData
        {
            public string characterName = "铁甲卫士"; // 添加这一行
            public int health = 30;
            public int maxHealth = 30;
            public int gold = 100;
            public List<string> cardIds = new List<string>();
            public List<string> relicIds = new List<string>();
        }
        
        [Header("玩家数据")]
        public PlayerStateData playerData = new PlayerStateData(); // 改为 public
        
        [Header("地图进度")]
        public string currentNodeId = "";
        public List<string> completedNodeIds = new List<string>();
        public List<string> unlockedNodeIds = new List<string>();
        
        [Header("当前战斗数据")]
        public string battleNodeId = "";
        public EncounterData battleEncounterData;
        // 在 GameDataManager.cs 中添加或检查
        public void InitializeNewGame()
        {
            completedNodeIds.Clear();
            unlockedNodeIds.Clear();
            
            // 假设你的起始节点 ID 是 "StartNode"
            // 你需要确保在游戏开始时，第一层的节点是默认解锁的
            // 或者在 MapManager 生成地图时执行这个逻辑

            // 重置基础属性
                playerData.health = 80;
                playerData.maxHealth = 80;
                playerData.gold = 99;
                playerData.characterName = "铁甲卫士";

                // 配置初始卡包 (添加卡牌 ID)
                playerData.cardIds.Clear();
                for(int i = 0; i < 5; i++) playerData.cardIds.Add("Strike_R"); // 5张打击
                for(int i = 0; i < 4; i++) playerData.cardIds.Add("Defend_R"); // 4张防御
                playerData.cardIds.Add("Bash"); // 1张重击

                completedNodeIds.Clear();
                unlockedNodeIds.Clear();
                
                SaveGameData();
        }
        // 属性访问器
        public int Health 
        { 
            get => playerData.health; 
            set 
            { 
                playerData.health = Mathf.Clamp(value, 0, playerData.maxHealth);
                SaveGameData();
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
                SaveGameData();
            }
        }
        
        public int Gold 
        { 
            get => playerData.gold; 
            set 
            { 
                playerData.gold = Mathf.Max(0, value);
                SaveGameData();
            }
        }
        
        public List<string> CardIds => new List<string>(playerData.cardIds);
        public List<string> RelicIds => new List<string>(playerData.relicIds);
        
        #region 单例初始化和持久化
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadGameData();
                Debug.Log("GameDataManager 初始化完成");
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void OnApplicationQuit()
        {
            SaveGameData();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGameData();
        }
        
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
                SaveGameData();
            }
        }
        
        public void RemoveCard(string cardId)
        {
            playerData.cardIds.Remove(cardId);
            SaveGameData();
        }
        
        public void AddRelic(string relicId)
        {
            if (!playerData.relicIds.Contains(relicId))
            {
                playerData.relicIds.Add(relicId);
                SaveGameData();
            }
        }
        
        #endregion
        
        #region 地图进度管理
        
        public void SetCurrentNode(string nodeId)
        {
            currentNodeId = nodeId;
            SaveGameData();
        }
        
       public void CompleteNode(string nodeId)
{
    if (!completedNodeIds.Contains(nodeId))
    {
        completedNodeIds.Add(nodeId);
        
        // 查找该数据对象，解锁它的连线节点
        // 这里需要配合 MapManager 里的节点引用进行解锁
        MapNodeData nodeData = MapManager.Instance.allNodes.FirstOrDefault(n => n.nodeId == nodeId);
        if (nodeData != null)
        {
            foreach (var neighbor in nodeData.connectedNodes)
            {
                if (!unlockedNodeIds.Contains(neighbor.nodeId))
                {
                    unlockedNodeIds.Add(neighbor.nodeId);
                }
            }
        }
        SaveGameData();
    }
}
        
        public void UnlockNode(string nodeId)
        {
            if (!unlockedNodeIds.Contains(nodeId) && !completedNodeIds.Contains(nodeId))
            {
                unlockedNodeIds.Add(nodeId);
                SaveGameData();
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
            try
            {
                // 保存玩家基础数据
                PlayerPrefs.SetInt("PlayerHealth", playerData.health);
                PlayerPrefs.SetInt("PlayerMaxHealth", playerData.maxHealth);
                PlayerPrefs.SetInt("PlayerGold", playerData.gold);
                PlayerPrefs.SetString("CurrentNodeId", currentNodeId);
                
                // 保存列表数据
                SaveListToPlayerPrefs("PlayerCardIds", playerData.cardIds);
                SaveListToPlayerPrefs("PlayerRelicIds", playerData.relicIds);
                SaveListToPlayerPrefs("CompletedNodeIds", completedNodeIds);
                SaveListToPlayerPrefs("UnlockedNodeIds", unlockedNodeIds);
                
                PlayerPrefs.Save();
                Debug.Log("游戏数据已保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存游戏数据失败: {e.Message}");
            }
        }
        
        public void LoadGameData()
        {
            try
            {
                // 检查是否有存档
                if (!PlayerPrefs.HasKey("PlayerHealth"))
                {
                    Debug.Log("无存档数据，使用默认值");
                    ResetToDefault();
                    return;
                }
                
                // 加载玩家基础数据
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
                PrintDebugInfo();
            }
            catch (Exception e)
            {
                Debug.LogError($"加载游戏数据失败: {e.Message}");
                ResetToDefault();
            }
        }
        
        public void ResetToDefault()
        {
            playerData = new PlayerStateData()
            {
                health = 30,
                maxHealth = 30,
                gold = 100,
                cardIds = new List<string> { "Strike", "Defend", "Strike", "Defend", "Strike" },
                relicIds = new List<string>()
            };
            
            currentNodeId = "";
            completedNodeIds.Clear();
            unlockedNodeIds.Clear();
            
            ClearBattleData();
            
            SaveGameData();
            Debug.Log("游戏数据已重置为默认值");
        }
        
        #endregion
        
        #region 辅助方法
        
        private void SaveListToPlayerPrefs(string key, List<string> list)
        {
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
        
        public void PrintDebugInfo()
        {
            Debug.Log("=== 游戏数据调试 ===");
            Debug.Log($"生命: {Health}/{MaxHealth}");
            Debug.Log($"金币: {Gold}");
            Debug.Log($"卡牌: {playerData.cardIds.Count}张");
            Debug.Log($"遗物: {playerData.relicIds.Count}个");
            Debug.Log($"当前节点: {currentNodeId}");
            Debug.Log($"已完成节点: {completedNodeIds.Count}个");
            Debug.Log($"已解锁节点: {unlockedNodeIds.Count}个");
            Debug.Log("===================");
        }
        
        #endregion
    }
}