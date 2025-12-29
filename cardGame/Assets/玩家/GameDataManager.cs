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
        
        [Header("游戏配置")]
        public CharacterStarterData defaultCharacterData; // 静态配置资产
        // public int defaultMaxHealth = 80; // Deprecated
        // public int defaultGold = 99;      // Deprecated

        /* [Header("默认配置")]
        public List<string> defaultStartingDeckIds = new List<string> 
        { 
            "B_FRENZY", "B_FRENZY", "B_FRENZY", "B_FRENZY", 
            "B_BLOCK", "B_BLOCK", "B_BLOCK", "B_BLOCK", 
            "B_EXECUTE" 
        }; */

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
            ResetToDefault();
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
        
        [ContextMenu("Clear Save Data")]
        public void ClearSaveData()
        {
             PlayerPrefs.DeleteAll();
             ResetToDefault();
             Debug.Log("已手动清除存档并重置");
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
            // 修复：Slay the Spire 允许重复卡牌，所以移除 Contains 检查
            // if (!playerData.cardIds.Contains(cardId))
            {
                playerData.cardIds.Add(cardId);
                
                // 核心修复：更新内存后，必须同步更新 PlayerPrefs
                // SaveGameData() 内部会调用 SaveListToPlayerPrefs("PlayerCardIds", playerData.cardIds);
                SaveGameData();
                
                Debug.Log($"[GameDataManager] 获得卡牌: {cardId}。当前卡组数量: {playerData.cardIds.Count}");
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
            Debug.Log($"[GameDataManager] Request to complete node: {nodeId}");
            
            // 无论是否已经包含，都尝试执行完成逻辑，以防之前的逻辑未完整执行
            if (!completedNodeIds.Contains(nodeId))
            {
                completedNodeIds.Add(nodeId);
                Debug.Log($"[GameDataManager] Added {nodeId} to Completed list.");
            }
            else
            {
                Debug.Log($"[GameDataManager] Node {nodeId} was already in Completed list.");
            }

            // 安全检查：确保 MapManager 存在再尝试解锁邻居
            if (MapManager.Instance != null && MapManager.Instance.allNodes != null)
                {
                    Debug.Log($"[GameDataManager] CompleteNode: MapManager found. Processing node {nodeId}...");
                    // 根据地图实际连接关系解锁下一关
                    MapNodeData nodeData = MapManager.Instance.allNodes.FirstOrDefault(n => n.nodeId == nodeId);
                    if (nodeData != null)
                    {
                        // 1. 解锁所有邻居
                        foreach (var neighbor in nodeData.connectedNodes)
                        {
                            if (!unlockedNodeIds.Contains(neighbor.nodeId))
                            {
                                unlockedNodeIds.Add(neighbor.nodeId);
                                Debug.Log($"[GameDataManager] 节点完成，解锁邻居: {neighbor.nodeId}");
                            }
                        }

                        // 2. 核心修复：自动推进当前节点指针
                        // 如果当前完成的节点正是记录中的“当前节点”，则自动指向下一个
                        if (currentNodeId == nodeId)
                        {
                            if (nodeData.connectedNodes != null && nodeData.connectedNodes.Count > 0)
                            {
                                // 默认选中第一个邻居作为新的当前节点
                                // 玩家后续可以点击其他已解锁邻居来改变这个选择
                                currentNodeId = nodeData.connectedNodes[0].nodeId;
                                Debug.Log($"[GameDataManager] 进度推进：CurrentNodeId 已从 {nodeId} 更新为 {currentNodeId}");
                            }
                            else
                            {
                                Debug.LogError($"[GameDataManager] 节点 {nodeId} 没有连接的下游节点！无法推进进度。ConnectedNodes Count: {nodeData.connectedNodes?.Count ?? 0}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[GameDataManager] 完成的节点 {nodeId} 不是当前记录的节点 {currentNodeId}，不执行自动推进。");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[GameDataManager] 无法在 MapManager 中找到节点: {nodeId}，无法解锁邻居！");
                    }
                }
                else
                {
                    Debug.LogError($"[GameDataManager] MapManager.Instance 或 allNodes 为空！无法处理节点解锁逻辑。Instance: {MapManager.Instance}, AllNodes: {MapManager.Instance?.allNodes?.Length}");
                }
                SaveGameData();
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
                    Debug.Log("[GameDataManager] 无存档数据，使用默认值初始化");
                    ResetToDefault();
                    return;
                }
                
                Debug.Log("[GameDataManager] 发现现有存档，正在加载...");
                
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
            if (defaultCharacterData != null)
            {
                playerData = new PlayerStateData()
                {
                    characterName = defaultCharacterData.characterName,
                    health = defaultCharacterData.maxHealth,
                    maxHealth = defaultCharacterData.maxHealth,
                    gold = defaultCharacterData.startingGold,
                    cardIds = new List<string>(),
                    relicIds = new List<string>(defaultCharacterData.startingRelicIds)
                };

                // 从配置资产中提取卡牌文件名作为 ID
                foreach (var card in defaultCharacterData.startingCards)
                {
                    if (card != null)
                    {
                        playerData.cardIds.Add(card.name);
                    }
                }
                
                Debug.Log($"[GameDataManager] 已使用 CharacterStarterData 重置。卡组数量: {playerData.cardIds.Count}");
            }
            else
            {
                Debug.LogWarning("[GameDataManager] defaultCharacterData 未赋值！使用硬编码默认值作为后备。");
                // Fallback hardcoded defaults
                playerData = new PlayerStateData()
                {
                    characterName = "铁甲卫士",
                    health = 80,
                    maxHealth = 80,
                    gold = 99,
                    cardIds = new List<string> { "B_FRENZY", "B_FRENZY", "B_FRENZY", "B_FRENZY", "B_BLOCK", "B_BLOCK", "B_BLOCK", "B_BLOCK", "B_EXECUTE" },
                    relicIds = new List<string>()
                };
            }
            
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