using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
namespace SlayTheSpireMap
{
    public class GameDataManager : MonoBehaviour
    {
        public string characterName = "勇者";
        public static GameDataManager Instance { get; private set; }

        [System.Serializable]
        public class PlayerStateData
        {
            public string characterName = "铁甲卫士";
            public string saveTime;
            public int health = 30;
            public int maxHealth = 30;
            public int gold = 100;
            public List<string> cardIds = new List<string>();
            public List<string> relicIds = new List<string>();
        }
        
        [Header("游戏配置")]
        public CharacterStarterData defaultCharacterData;

        [Header("玩家数据")]
        public PlayerStateData playerData = new PlayerStateData();
        
        [Header("地图进度")]
        public string currentNodeId = "";
        public List<string> completedNodeIds = new List<string>();
        public List<string> unlockedNodeIds = new List<string>();
        
        [Header("当前战斗数据")]
        public string battleNodeId = "";
        public EncounterData battleEncounterData;
        public DigData digData;
        
        public void InitializeNewGame()
        {
            ResetToDefault();
        }
        
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
                
                // 自动加载当前存档，这样场景切换时会自动加载背包数据
                // 只在非主菜单场景加载，主菜单场景由SaveSlotUI手动管理
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene != "MainMenu" && currentScene != "CharacterSelection")
                {
                    Debug.Log("GameDataManager 初始化完成，开始加载当前存档");
                    LoadGameData(0); // 加载当前存档
                }
                else
                {
                    Debug.Log("GameDataManager 初始化完成，当前是主菜单场景，不自动加载存档");
                }
                
                Application.quitting += OnApplicationQuitting;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void OnDestroy()
        {
            Application.quitting -= OnApplicationQuitting;
        }
        
        void OnApplicationQuit()
        {
            SaveGameData();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGameData();
        }
        
        void OnApplicationQuitting()
        {
            SaveGameData();
            Debug.Log("游戏数据已在退出时保存");
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
            playerData.cardIds.Add(cardId);
            SaveGameData();
            Debug.Log($"[GameDataManager] 获得卡牌: {cardId}。当前卡组数量: {playerData.cardIds.Count}");
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
                Debug.Log($"[GameDataManager] Added {nodeId} to Completed list.");
            }
            else
            {
                Debug.Log($"[GameDataManager] Node {nodeId} was already in Completed list.");
            }

            if (MapManager.Instance != null && MapManager.Instance.allNodes != null)
                {
                    Debug.Log($"[GameDataManager] CompleteNode: MapManager found. Processing node {nodeId}...");
                    MapNodeData nodeData = MapManager.Instance.allNodes.FirstOrDefault(n => n.nodeId == nodeId);
                    if (nodeData != null)
                    {
                        foreach (var neighbor in nodeData.connectedNodes)
                        {
                            if (!unlockedNodeIds.Contains(neighbor.nodeId))
                            {
                                unlockedNodeIds.Add(neighbor.nodeId);
                                Debug.Log($"[GameDataManager] 节点完成，解锁邻居: {neighbor.nodeId}");
                            }
                        }

                        if (currentNodeId == nodeId)
                        {
                            if (nodeData.connectedNodes != null && nodeData.connectedNodes.Count > 0)
                            {
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
            digData = null;
        }
        
        #endregion
        
        #region 数据持久化
        
        public void SaveGameData(int slotIndex = 0)
        {
            Debug.Log($"[GameDataManager] SaveGameData - 开始保存游戏数据，槽位: {slotIndex}");
            try
            {
                playerData.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                GameSaveData saveData = new GameSaveData();
                saveData.playerData = playerData;
                saveData.currentNodeId = currentNodeId;
                saveData.completedNodeIds = completedNodeIds;
                saveData.unlockedNodeIds = unlockedNodeIds;
                
                string fileName = slotIndex == 0 ? "save_current.json" : $"save_{slotIndex}.json";
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                string json = JsonUtility.ToJson(saveData, true);
                System.IO.File.WriteAllText(savePath, json);
                
                Debug.Log($"[GameDataManager] SaveGameData - 成功保存玩家数据到: {savePath}");
                
                // 保存背包数据
                if (Bag.InventoryManager.Instance != null)
                {
                    Debug.Log($"[GameDataManager] SaveGameData - 调用InventoryManager保存背包数据，槽位: {slotIndex}");
                    Bag.InventoryManager.Instance.SaveInventory(slotIndex);
                }
                else
                {
                    Debug.LogWarning("[GameDataManager] SaveGameData - InventoryManager.Instance为null，无法保存背包数据");
                }
                
                PlayerPrefs.Save();
                
                Debug.Log($"[GameDataManager] SaveGameData - 游戏数据已保存到存档槽 {slotIndex}: {savePath}");
                Debug.Log($"[GameDataManager] SaveGameData - 存档时间: {playerData.saveTime}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDataManager] SaveGameData - 保存游戏数据失败: {e.Message}");
            }
        }
        
        [System.Serializable]
        private class GameSaveData
        {
            public PlayerStateData playerData;
            public string currentNodeId;
            public List<string> completedNodeIds;
            public List<string> unlockedNodeIds;
        }
        
        private void SaveInventoryData()
        {
            try
            {
                if (Bag.InventoryManager.Instance != null)
                {
                    Bag.InventoryManager.Instance.SaveInventory();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"保存背包数据失败: {e.Message}");
            }
        }
        
        private void LoadInventoryData(int slotIndex = 0)
        {
            Debug.Log($"[GameDataManager] LoadInventoryData - 开始加载背包数据，槽位: {slotIndex}");
            try
            {
                if (Bag.InventoryManager.Instance != null)
                {
                    Debug.Log($"[GameDataManager] LoadInventoryData - InventoryManager.Instance存在，调用LoadInventoryData，槽位: {slotIndex}");
                    Bag.InventoryManager.Instance.LoadInventoryData(slotIndex);
                    Debug.Log("[GameDataManager] LoadInventoryData - 背包数据已通过 InventoryManager 加载");
                }
                else
                {
                    Debug.LogWarning("[GameDataManager] LoadInventoryData - InventoryManager.Instance为null，无法加载背包数据");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDataManager] 加载背包数据失败: {e.Message}");
                Debug.LogError($"[GameDataManager] 异常堆栈: {e.StackTrace}");
            }
        }
        
        public void LoadGameData(int slotIndex = 0)
        {
            Debug.Log($"[GameDataManager] LoadGameData - 开始加载游戏数据，槽位: {slotIndex}");
            string fileName = slotIndex == 0 ? "save_current.json" : $"save_{slotIndex}.json";
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            Debug.Log($"[GameDataManager] LoadGameData - 查找存档文件: {savePath}");
            
            if (!System.IO.File.Exists(savePath))
            {
                Debug.LogError($"[GameDataManager] 尝试加载槽位 {slotIndex} 失败：文件不存在！路径: {savePath}");
                // 如果是从主菜单点进来的，这里不应该重置数据，应该报错让玩家知道
                return;
            }

            try
            {
                Debug.Log($"[GameDataManager] LoadGameData - 读取存档文件");
                string jsonContent = System.IO.File.ReadAllText(savePath);
                Debug.Log($"[GameDataManager] LoadGameData - 读取到JSON数据: {jsonContent}");
                
                GameSaveData loadedSaveData = JsonUtility.FromJson<GameSaveData>(jsonContent);
                Debug.Log($"[GameDataManager] LoadGameData - 解析成功");
                
                // 覆盖当前内存数据
                playerData = loadedSaveData.playerData;
                currentNodeId = loadedSaveData.currentNodeId;
                completedNodeIds = loadedSaveData.completedNodeIds;
                unlockedNodeIds = loadedSaveData.unlockedNodeIds;
                
                // 加载背包数据
                Debug.Log($"[GameDataManager] LoadGameData - 调用LoadInventoryData，槽位: {slotIndex}");
                LoadInventoryData(slotIndex);
                
                Debug.Log($"<color=green>[GameDataManager] 成功从槽位 {slotIndex} 加载存档数据</color>");
                PrintDebugInfo();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameDataManager] JSON解析失败: {e.Message}");
                Debug.LogError($"[GameDataManager] 异常堆栈: {e.StackTrace}");
                // 这里不应该重置数据，而是让玩家知道加载失败
            }
        }
        
        public PlayerStateData LoadPlayerDataForSlot(int slotIndex)
        {
            try
            {
                string fileName = $"save_{slotIndex}.json";
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                
                // 优先加载指定槽位的存档
                if (System.IO.File.Exists(savePath))
                {
                    string jsonContent = System.IO.File.ReadAllText(savePath);
                    GameSaveData loadedSave = JsonUtility.FromJson<GameSaveData>(jsonContent);
                    
                    return loadedSave.playerData;
                }
                
                // 如果指定槽位存档不存在，尝试加载当前存档
                string currentSavePath = System.IO.Path.Combine(Application.persistentDataPath, "save_current.json");
                if (System.IO.File.Exists(currentSavePath))
                {
                    string jsonContent = System.IO.File.ReadAllText(currentSavePath);
                    GameSaveData currentSave = JsonUtility.FromJson<GameSaveData>(jsonContent);
                    return currentSave.playerData;
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载存档槽 {slotIndex} 数据失败: {e.Message}");
                return null;
            }
        }
        
        public bool HasSaveData(int slotIndex)
        {
            string fileName = $"save_{slotIndex}.json";
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            // 检查指定槽位存档是否存在
            if (System.IO.File.Exists(savePath))
            {
                return true;
            }
            
            // 如果指定槽位存档不存在，检查当前存档是否存在
            string currentSavePath = System.IO.Path.Combine(Application.persistentDataPath, "save_current.json");
            return System.IO.File.Exists(currentSavePath);
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
            
            try
            {
                if (Bag.InventoryManager.Instance != null)
                {
                    Bag.InventoryManager.Instance.ClearInventory(null);
                    Debug.Log("背包数据已清空");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"清理背包数据时出错: {e.Message}");
            }
            
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
