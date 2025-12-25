using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    
    [Header("地图配置")]
    public List<MapNode> mapNodes;
    public MapNode currentNode;
    public string battleSceneName = "BattleScene"; // 战斗场景名称
    
    [Header("玩家进度")]
    public int currentMapLevel = 1;
    public List<MapNode> visitedNodes = new List<MapNode>();
    public List<EnemyEncounterData> availableEncounters = new List<EnemyEncounterData>();
    
    [Header("战斗配置")]
    public PlayerDeck currentDeck; // 玩家当前卡组
    public int playerHealth = 30;
    public int maxPlayerHealth = 30;
    public int playerGold = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保持地图管理器跨场景
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 初始化地图
        InitializeMap();
        
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void InitializeMap()
    {
        Debug.Log("[地图管理器] 初始化地图...");
        
        // 如果是第一次进入，设置起始节点
        if (currentNode == null && mapNodes.Count > 0)
        {
            currentNode = mapNodes[0];
            Debug.Log($"[地图管理器] 设置起始节点: {currentNode.nodeName}");
        }
        
        // 加载玩家进度
        LoadPlayerProgress();
    }
    
    /// <summary>
    /// 当玩家点击地图节点时调用
    /// </summary>
    /// <param name="node">点击的地图节点</param>
    public void OnMapNodeClicked(MapNode node)
    {
        if (node == null) return;
        
        Debug.Log($"[地图管理器] 玩家点击节点: {node.nodeName}");
        
        // 检查节点是否可以进入
        if (!CanEnterNode(node))
        {
            Debug.LogWarning($"[地图管理器] 无法进入节点: {node.nodeName}");
            // 可以显示提示UI
            return;
        }
        
        // 设置当前节点
        currentNode = node;
        visitedNodes.Add(node);
        
        // 根据节点类型处理
        switch (node.nodeType)
        {
            case MapNodeType.Battle:
                EnterBattleNode(node);
                break;
                
            case MapNodeType.Elite:
                EnterEliteBattleNode(node);
                break;
                
            case MapNodeType.Boss:
                EnterBossBattleNode(node);
                break;
                
            case MapNodeType.Shop:
                EnterShopNode(node);
                break;
                
            case MapNodeType.Rest:
                EnterRestNode(node);
                break;
                
            case MapNodeType.Event:
                EnterEventNode(node);
                break;
                
            case MapNodeType.Treasure:
                EnterTreasureNode(node);
                break;
        }
    }
    
    /// <summary>
    /// 进入普通战斗节点
    /// </summary>
    private void EnterBattleNode(MapNode node)
    {
        Debug.Log($"[地图管理器] 进入战斗节点: {node.nodeName}");
        
        // 从节点获取遭遇战数据
        EnemyEncounterData encounterData = node.GetEncounterData();
        
        if (encounterData == null)
        {
            Debug.LogError($"[地图管理器] 节点 {node.nodeName} 没有配置遭遇战数据！");
            return;
        }
        
        // 保存当前地图状态
        SavePlayerProgress();
        
        // 准备战斗数据
        PrepareForBattle(encounterData);
        
        // 加载战斗场景
        LoadBattleScene();
    }
    
    /// <summary>
    /// 进入精英战斗节点
    /// </summary>
    private void EnterEliteBattleNode(MapNode node)
    {
        Debug.Log($"[地图管理器] 进入精英战斗节点: {node.nodeName}");
        // 类似普通战斗，但可能更困难
        EnterBattleNode(node);
    }
    
    /// <summary>
    /// 进入Boss战斗节点
    /// </summary>
    private void EnterBossBattleNode(MapNode node)
    {
        Debug.Log($"[地图管理器] 进入Boss战斗节点: {node.nodeName}");
        // 类似普通战斗，但可能是Boss战
        EnterBattleNode(node);
    }
    
    /// <summary>
    /// 准备战斗
    /// </summary>
    private void PrepareForBattle(EnemyEncounterData encounterData)
    {
        // 保存遭遇战数据到GameStateManager，以便战斗场景读取
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetCurrentEncounter(encounterData);
            GameStateManager.Instance.SetPlayerHealth(playerHealth, maxPlayerHealth);
            GameStateManager.Instance.SetPlayerDeck(currentDeck);
        }
        
        // 也可以直接保存到静态变量
        PlayerPrefs.SetInt("PlayerHealth", playerHealth);
        PlayerPrefs.SetInt("PlayerMaxHealth", maxPlayerHealth);
    }
    
    /// <summary>
    /// 加载战斗场景
    /// </summary>
    private void LoadBattleScene()
    {
        Debug.Log($"[地图管理器] 加载战斗场景: {battleSceneName}");
        
        // 如果有场景过渡效果
        SceneTransition transition = FindObjectOfType<SceneTransition>();
        if (transition != null)
        {
            transition.TransitionToScene(battleSceneName);
        }
        else
        {
            // 直接加载场景
            SceneManager.LoadScene(battleSceneName);
        }
    }
    
    /// <summary>
    /// 从战斗返回地图时的回调
    /// </summary>
    public void OnBattleCompleted(bool isVictory)
    {
        Debug.Log($"[地图管理器] 战斗完成，结果: {(isVictory ? "胜利" : "失败")}");
        
        // 重新激活地图UI（如果需要）
        ShowMapUI(true);
        
        // 处理战斗结果
        ProcessBattleResult(isVictory);
        
        // 解锁相邻节点（如果胜利）
        if (isVictory && currentNode != null)
        {
            currentNode.isCompleted = true;
            UnlockAdjacentNodes(currentNode);
        }
        
        // 保存进度
        SavePlayerProgress();
        
        // 更新地图UI
        UpdateMapUI();
    }
    
    /// <summary>
    /// 处理战斗结果
    /// </summary>
    private void ProcessBattleResult(bool isVictory)
    {
        if (isVictory)
        {
            // 获得奖励
            int goldReward = Random.Range(10, 30);
            playerGold += goldReward;
            Debug.Log($"[地图管理器] 战斗胜利！获得 {goldReward} 金币，总计: {playerGold}");
            
            // 可能有几率获得新卡牌
            // if (Random.Range(0f, 1f) > 0.7f)
            // {
            //     CardData newCard = GetRandomCardReward();
            //     currentDeck.AddCard(newCard);
            //     Debug.Log($"[地图管理器] 获得新卡牌: {newCard.cardName}");
            // }
        }
        else
        {
            // 失败惩罚
            Debug.Log("[地图管理器] 战斗失败！");
            // 可能减少生命值或者回到上一个节点
        }
        
        // 更新玩家状态
        UpdatePlayerStatus();
    }
    
    /// <summary>
    /// 解锁相邻节点
    /// </summary>
    private void UnlockAdjacentNodes(MapNode node)
    {
        foreach (var adjacentNode in node.connectedNodes)
        {
            if (adjacentNode != null && !adjacentNode.isUnlocked)
            {
                adjacentNode.isUnlocked = true;
                Debug.Log($"[地图管理器] 解锁节点: {adjacentNode.nodeName}");
            }
        }
    }
    
    /// <summary>
    /// 检查是否可以进入节点
    /// </summary>
    private bool CanEnterNode(MapNode node)
    {
        if (node == null) return false;
        
        // 节点已解锁
        if (!node.isUnlocked)
        {
            Debug.Log($"[地图管理器] 节点 {node.nodeName} 未解锁");
            return false;
        }
        
        // 节点未完成
        if (node.isCompleted)
        {
            Debug.Log($"[地图管理器] 节点 {node.nodeName} 已完成");
            return false;
        }
        
        // 如果是战斗节点，检查玩家生命值
        if (node.nodeType == MapNodeType.Battle || 
            node.nodeType == MapNodeType.Elite || 
            node.nodeType == MapNodeType.Boss)
        {
            if (playerHealth <= 0)
            {
                Debug.Log("[地图管理器] 玩家生命值不足，无法进入战斗");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 场景加载完成时的回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[地图管理器] 场景加载完成: {scene.name}");
        
        if (scene.name == battleSceneName)
        {
            // 战斗场景加载完成
            HideMapUI();
        }
        else if (scene.name == "MapScene")
        {
            // 地图场景加载完成
            ShowMapUI(true);
            
            // 如果是从战斗返回，调用战斗完成回调
            bool justReturnedFromBattle = PlayerPrefs.GetInt("JustReturnedFromBattle", 0) == 1;
            bool battleResult = PlayerPrefs.GetInt("LastBattleResult", 0) == 1;
            
            if (justReturnedFromBattle)
            {
                OnBattleCompleted(battleResult);
                PlayerPrefs.SetInt("JustReturnedFromBattle", 0);
            }
        }
    }
    
    /// <summary>
    /// 保存玩家进度
    /// </summary>
    private void SavePlayerProgress()
    {
        PlayerPrefs.SetInt("PlayerGold", playerGold);
        PlayerPrefs.SetInt("PlayerHealth", playerHealth);
        PlayerPrefs.SetInt("PlayerMaxHealth", maxPlayerHealth);
        PlayerPrefs.SetInt("CurrentMapLevel", currentMapLevel);
        
        // 保存当前节点ID
        if (currentNode != null)
        {
            PlayerPrefs.SetString("CurrentNodeId", currentNode.nodeId);
        }
        
        PlayerPrefs.Save();
        Debug.Log("[地图管理器] 玩家进度已保存");
    }
    
    /// <summary>
    /// 加载玩家进度
    /// </summary>
    private void LoadPlayerProgress()
    {
        playerGold = PlayerPrefs.GetInt("PlayerGold", 0);
        playerHealth = PlayerPrefs.GetInt("PlayerHealth", 30);
        maxPlayerHealth = PlayerPrefs.GetInt("PlayerMaxHealth", 30);
        currentMapLevel = PlayerPrefs.GetInt("CurrentMapLevel", 1);
        
        string currentNodeId = PlayerPrefs.GetString("CurrentNodeId", "");
        if (!string.IsNullOrEmpty(currentNodeId))
        {
            currentNode = mapNodes.Find(n => n.nodeId == currentNodeId);
        }
        
        Debug.Log("[地图管理器] 玩家进度已加载");
    }
    
    private void HideMapUI()
    {
        // 隐藏地图UI
        MapUI mapUI = FindObjectOfType<MapUI>();
        if (mapUI != null)
        {
            mapUI.Hide();
        }
    }
    
    private void ShowMapUI(bool show)
    {
        // 显示地图UI
        MapUI mapUI = FindObjectOfType<MapUI>();
        if (mapUI != null)
        {
            mapUI.Show();
        }
    }
    
    private void UpdateMapUI()
    {
        // 更新地图UI
        MapUI mapUI = FindObjectOfType<MapUI>();
        if (mapUI != null)
        {
            mapUI.UpdateUI();
        }
    }
    
    private void UpdatePlayerStatus()
    {
        // 更新玩家状态UI
        PlayerStatusUI statusUI = FindObjectOfType<PlayerStatusUI>();
        if (statusUI != null)
        {
            statusUI.UpdateUI(playerHealth, maxPlayerHealth, playerGold);
        }
    }
    
    // 其他节点类型的方法（商店、休息、事件、宝藏）
    private void EnterShopNode(MapNode node) { /* 进入商店逻辑 */ }
    private void EnterRestNode(MapNode node) { /* 进入休息点逻辑 */ }
    private void EnterEventNode(MapNode node) { /* 进入事件逻辑 */ }
    private void EnterTreasureNode(MapNode node) { /* 进入宝藏逻辑 */ }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

// 地图节点类型枚举
public enum MapNodeType
{
    Battle,     // 普通战斗
    Elite,      // 精英战斗
    Boss,       // Boss战斗
    Shop,       // 商店
    Rest,       // 休息点
    Event,      // 随机事件
    Treasure    // 宝藏
}

// 地图节点类（需要在Inspector中配置）
[System.Serializable]
public class MapNode
{
    public string nodeId;
    public string nodeName;
    public MapNodeType nodeType;
    public bool isUnlocked;
    public bool isCompleted;
    public Vector2 position; // 在地图上的位置
    
    [Header("战斗配置（如果是战斗节点）")]
    public EnemyEncounterData encounterData;
    
    [Header("连接节点")]
    public List<MapNode> connectedNodes = new List<MapNode>();
    
    public EnemyEncounterData GetEncounterData()
    {
        return encounterData;
    }
}